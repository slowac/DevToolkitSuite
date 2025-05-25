using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

namespace DevToolkit_Suite
{
    public class ScriptableObjectBrowserWindow : EditorWindow
    {
        private Vector2 scroll;
        private Dictionary<Type, List<ScriptableObject>> soByType;
        private Dictionary<ScriptableObject, List<string>> soUsage = new();
        private string search = "";
        private Type selectedType;
        private ScriptableObject selectedObject;
        private List<string> ignoredNamespaces = new List<string> {
            "UnityEngine", "UnityEditor", "TMPro", "Cinemachine"
        };

        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle searchBoxStyle;
        private static GUIStyle typeButtonStyle;
        private static GUIStyle instanceBoxStyle;
        private static GUIStyle separatorStyle;
        private static GUIStyle statsBoxStyle;
        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 typesScrollPosition = Vector2.zero;
        private Vector2 instancesScrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/ScriptableObject Browser", false, 12)]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableObjectBrowserWindow>("ScriptableObject Browser");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            RefreshList();
        }

        private void OnFocus()
        {
            // Auto-refresh on focus if enabled
            if (EditorPrefs.GetBool("SOBrowser_AutoRefreshOnFocus", true))
            {
                RefreshList();
            }
        }

        private void RefreshList()
        {
            soByType = new Dictionary<Type, List<ScriptableObject>>();
            soUsage.Clear();
            string[] guids = AssetDatabase.FindAssets("t:ScriptableObject");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj == null) continue;

                Type type = obj.GetType();
                if (ignoredNamespaces.Any(ns => type.Namespace != null && type.Namespace.StartsWith(ns))) continue;

                if (!soByType.ContainsKey(type)) soByType[type] = new List<ScriptableObject>();
                soByType[type].Add(obj);
            }
        }

        private void OnGUI()
        {
            if (soByType == null) RefreshList();

            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🧩 ScriptableObject Browser", headerLabelStyle);
            EditorGUILayout.Space(8);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(5);

            // Statistics Overview Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Browser Statistics", sectionLabelStyle);
            DrawStatistics();
            EditorGUILayout.EndVertical();

            // Search & Actions Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔍 Search & Actions", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawSearchAndActions();
            EditorGUILayout.EndVertical();

            // ScriptableObject Types Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📁 ScriptableObject Types", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawTypesList();
            EditorGUILayout.EndVertical();

            // Selected Type Instances Section
            if (selectedType != null && soByType.ContainsKey(selectedType))
            {
                EditorGUILayout.BeginVertical(modernBoxStyle);
                EditorGUILayout.LabelField($"📄 Instances of {selectedType.Name}", sectionLabelStyle);
                EditorGUILayout.Space(3);
                DrawInstancesList();
                EditorGUILayout.EndVertical();
            }

            // Preview Section
            if (selectedObject != null)
            {
                EditorGUILayout.BeginVertical(modernBoxStyle);
                EditorGUILayout.LabelField("👁️ Object Preview", sectionLabelStyle);
                EditorGUILayout.Space(3);
                DrawObjectPreview();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10); // Bottom padding
            EditorGUILayout.EndScrollView();
        }

        private void ScanSceneUsage()
        {
            // Check if usage scanning is enabled
            if (!EditorPrefs.GetBool("SOBrowser_EnableUsageScanning", true))
            {
                EditorUtility.DisplayDialog("Usage Scanning Disabled", 
                    "Scene usage scanning is disabled in configuration.\n\nEnable it in the Configuration window to use this feature.", 
                    "OK");
                return;
            }

            soUsage.Clear();
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.StartsWith("Packages/"))
                .ToArray();

            string currentScene = SceneManager.GetActiveScene().path;
            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                if (scene.isDirty)
                {
                    bool shouldSave = EditorUtility.DisplayDialog(
                        "Unsaved Scene Changes",
                        $"Scene '{Path.GetFileName(path)}' has unsaved changes. Save before scanning?",
                        "Save and Continue", "Cancel");

                    if (!shouldSave)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                        continue;
                    }

                    EditorSceneManager.SaveScene(scene);
                }

                var gos = scene.GetRootGameObjects();
                foreach (var go in gos)
                {
                    var allComponents = go.GetComponentsInChildren<MonoBehaviour>(true);
                    foreach (var comp in allComponents)
                    {
                        if (comp == null) continue;
                        var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        foreach (var field in fields)
                        {
                            if (!typeof(ScriptableObject).IsAssignableFrom(field.FieldType)) continue;
                            var value = field.GetValue(comp) as ScriptableObject;
                            if (value != null)
                            {
                                if (!soUsage.ContainsKey(value))
                                    soUsage[value] = new List<string>();
                                if (!soUsage[value].Contains(path))
                                    soUsage[value].Add(path);
                            }
                        }
                    }
                }
                if (scene.path != currentScene)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
            if (!string.IsNullOrEmpty(currentScene))
            {
                EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);
            }
        }

        public void SetIgnoredNamespaces(List<string> namespaces)
        {
            ignoredNamespaces = namespaces;
            RefreshList();
        }

        private void InitStyles()
        {
            if (headerLabelStyle == null)
            {
                headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    margin = new RectOffset(0, 0, 10, 15)
                };
            }

            if (sectionLabelStyle == null)
            {
                sectionLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) },
                    margin = new RectOffset(5, 0, 8, 5)
                };
            }

            if (modernBoxStyle == null)
            {
                modernBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(15, 15, 12, 12),
                    margin = new RectOffset(5, 5, 5, 8),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.25f, 0.25f, 0.25f, 0.8f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (searchBoxStyle == null)
            {
                searchBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (typeButtonStyle == null)
            {
                typeButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    hover = { textColor = new Color(0.7f, 0.9f, 1f) },
                    fontSize = 12,
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }

            if (instanceBoxStyle == null)
            {
                instanceBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(2, 2, 1, 1),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.18f, 0.18f, 0.18f, 0.9f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (statsBoxStyle == null)
            {
                statsBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(5, 5, 5, 10),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.15f, 0.3f, 0.15f, 0.9f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (separatorStyle == null)
            {
                separatorStyle = new GUIStyle()
                {
                    normal = { background = CreateSolidTexture(new Color(0.4f, 0.4f, 0.4f, 0.5f)) },
                    margin = new RectOffset(10, 10, 5, 5),
                    fixedHeight = 1
                };
            }

            if (gradientButtonStyle == null)
            {
                gradientButtonStyle = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white },
                    hover = { textColor = Color.white },
                    active = { textColor = Color.white },
                    focused = { textColor = Color.white },
                    fontSize = 12,
                    padding = new RectOffset(8, 8, 6, 6),
                    margin = new RectOffset(3, 3, 3, 3)
                };
            }

            if (gradientTex == null)
            {
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
            }
        }

        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(statsBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                margin = new RectOffset(0, 10, 0, 0)
            };

            // Calculate statistics
            int totalTypes = soByType?.Count ?? 0;
            int totalInstances = soByType?.Values.Sum(list => list.Count) ?? 0;
            int usageScanned = soUsage?.Count ?? 0;

            // Responsive layout for statistics
            float windowWidth = position.width;
            
            if (windowWidth < 450)
            {
                // Narrow: Single compact line
                EditorGUILayout.LabelField($"📁 {totalTypes} Types | 📄 {totalInstances} Objects | 🧩 {usageScanned} Scanned", statLabelStyle);
            }
            else if (windowWidth < 600)
            {
                // Medium: 2-row layout
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Types: {totalTypes}", statLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"📄 Objects: {totalInstances}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🧩 Usage Scanned: {usageScanned}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Wide: Single row
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Types: {totalTypes}", statLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"📄 Objects: {totalInstances}", statLabelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField($"🧩 Usage Scanned: {usageScanned}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSearchAndActions()
        {
            // Search field
            EditorGUILayout.BeginVertical(searchBoxStyle);
            EditorGUILayout.LabelField("🔍 Search Types:", EditorStyles.miniLabel);
            EditorGUILayout.Space(2);
            search = EditorGUILayout.TextField(search, GUILayout.Height(20));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Action buttons with responsive layout
            bool isNarrow = position.width < 500;
            
            if (isNarrow)
            {
                // Stack buttons vertically
                if (GradientButton("🔄 Refresh List", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    RefreshList();
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("🧩 Scan Scene Usage", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    ScanSceneUsage();
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("⚙️ Configuration", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    ScriptableObjectConfig.ShowWindow(this);
                }
            }
            else
            {
                // Horizontal layout
                EditorGUILayout.BeginHorizontal();
                
                if (GradientButton("🔄 Refresh", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    RefreshList();
                }
                
                GUILayout.Space(5);
                
                if (GradientButton("🧩 Scan Usage", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    ScanSceneUsage();
                }
                
                GUILayout.Space(5);
                
                if (GradientButton("⚙️ Config", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    ScriptableObjectConfig.ShowWindow(this);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawTypesList()
        {
            if (soByType == null || soByType.Count == 0)
            {
                EditorGUILayout.HelpBox("📁 No ScriptableObject types found.\n\nEither no ScriptableObjects exist in your project, or all are filtered by namespace settings.", MessageType.Info);
                return;
            }

            // Filter types based on search
            var filteredTypes = soByType.Keys.Where(type => 
                string.IsNullOrEmpty(search) || type.Name.ToLower().Contains(search.ToLower())
            ).OrderBy(k => k.Name).ToList();

            if (filteredTypes.Count == 0)
            {
                EditorGUILayout.HelpBox($"🔍 No types found matching '{search}'", MessageType.Info);
                return;
            }

            // Responsive list height
            float listHeight = Mathf.Min(200, filteredTypes.Count * 32 + 10);
            
            typesScrollPosition = EditorGUILayout.BeginScrollView(typesScrollPosition, GUILayout.Height(listHeight));
            
            foreach (var type in filteredTypes)
            {
                EditorGUILayout.BeginHorizontal(instanceBoxStyle);
                
                // Type button with optional count and namespace
                bool showInstanceCounts = EditorPrefs.GetBool("SOBrowser_ShowInstanceCounts", true);
                bool showFullNamespaces = EditorPrefs.GetBool("SOBrowser_ShowFullNamespaces", false);
                
                string typeName = showFullNamespaces ? type.FullName : type.Name;
                int instanceCount = soByType[type].Count;
                
                string buttonText = showInstanceCounts ? 
                    $"📄 {typeName} ({instanceCount})" : 
                    $"📄 {typeName}";
                
                bool isSelected = selectedType == type;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.8f);
                }
                
                if (GUILayout.Button(buttonText, typeButtonStyle, GUILayout.Height(28)))
                {
                    selectedType = type;
                    selectedObject = null;
                }
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawInstancesList()
        {
            var list = soByType[selectedType];
            
            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox($"📄 No instances of {selectedType.Name} found.", MessageType.Info);
                return;
            }

            // Responsive list height
            float listHeight = Mathf.Min(250, list.Count * 35 + 10);
            
            instancesScrollPosition = EditorGUILayout.BeginScrollView(instancesScrollPosition, GUILayout.Height(listHeight));
            
            foreach (var item in list)
            {
                EditorGUILayout.BeginVertical(instanceBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                // Object button
                bool isSelected = selectedObject == item;
                if (isSelected)
                {
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
                }
                
                if (GUILayout.Button($"📄 {item.name}", typeButtonStyle, GUILayout.Height(26)))
                {
                    selectedObject = item;
                    EditorGUIUtility.PingObject(item);
                    Selection.activeObject = item;
                }
                
                if (isSelected)
                {
                    GUI.backgroundColor = Color.white;
                }

                // Usage info with responsive display
                if (soUsage.TryGetValue(item, out var sceneList))
                {
                    GUIStyle usageStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontStyle = FontStyle.Bold,
                        fontSize = 9,
                        normal = { textColor = new Color(0.7f, 1f, 0.7f) },
                        alignment = TextAnchor.MiddleRight
                    };

                    bool isNarrow = position.width < 500;
                    string usageText = isNarrow ? $"🧩 {sceneList.Count}" : $"🧩 {sceneList.Count} scene(s)";
                    string tooltip = $"Referenced in:\n• " + string.Join("\n• ", sceneList.Select(Path.GetFileNameWithoutExtension));
                    
                    GUILayout.Label(new GUIContent(usageText, tooltip), usageStyle, GUILayout.Width(isNarrow ? 40 : 100));
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawObjectPreview()
        {
            if (selectedObject == null) return;

            EditorGUILayout.BeginVertical(instanceBoxStyle);
            
            // Object info header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"👁️ {selectedObject.name}", EditorStyles.boldLabel);
            
            if (GUILayout.Button("📍 Ping", GUILayout.Width(60), GUILayout.Height(20)))
            {
                EditorGUIUtility.PingObject(selectedObject);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Object preview with configurable height for responsive design
            int maxPreviewHeight = EditorPrefs.GetInt("SOBrowser_MaxPreviewHeight", 300);
            float previewHeight = Mathf.Min(maxPreviewHeight, position.height * 0.4f);
            
            EditorGUILayout.BeginVertical(GUILayout.Height(previewHeight));
            Editor editor = Editor.CreateEditor(selectedObject);
            if (editor != null)
            {
                editor.OnInspectorGUI();
            }
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndVertical();
        }

        private bool GradientButton(string text, Texture2D hoverTex, GUIStyle style, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            return GUILayout.Button(content, buttonStyle, options);
        }

        private Texture2D CreateHorizontalGradient(int width, int height, Color left, Color right)
        {
            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            for (int x = 0; x < width; x++)
            {
                Color col = Color.Lerp(left, right, x / (float)(width - 1));
                for (int y = 0; y < height; y++) tex.SetPixel(x, y, col);
            }
            tex.Apply();
            return tex;
        }

        private Texture2D CreateSolidTexture(Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
