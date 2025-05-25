using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DevToolkit_Suite
{
    public class ScriptComponentAnalyzerWindow : EditorWindow
    {
        private Vector2 scroll;
        private Dictionary<string, List<GameObject>> scriptUsage;
        private List<GameObject> missingScripts;
        private List<string> unusedScripts;
        private Dictionary<string, int> componentTypeCounts;
        private Dictionary<string, List<GameObject>> componentUsage;
        private string search = "";
        private bool onlyActiveScene = true;

        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle itemBoxStyle;
        private static GUIStyle separatorStyle;
        private static GUIStyle statsBoxStyle;
        private Vector2 scrollPosition = Vector2.zero;

        // Legacy styles for backward compatibility
        private GUIStyle sectionHeaderStyle;
        private GUIStyle boxHeaderStyle;
        private GUIStyle statsTextStyle;
        private Texture2D statsBarBg;

        private Dictionary<string, string> componentTooltips = new Dictionary<string, string>
        {
            { "TMP_Dropdown", "Dropdown UI element from TextMeshPro package." },
            { "TMP_InputField", "Text input field from TextMeshPro." },
            { "TextMeshProUGUI", "Text rendering component for UI from TextMeshPro." },
            { "Volume", "Post-processing volume controller." },
            { "TextMeshPro", "Standalone text rendering from TextMeshPro namespace." },
            { "TMP_SubMesh", "Sub-mesh for modular TextMeshPro elements." },
            { "TMP_SubMeshUI", "UI version of TMP SubMesh component." },
            { "TMP_ScrollbarEventHandler", "TMP event component for scroll behavior." },
            { "TMP_SelectionCaret", "TMP component to show caret while selecting text." },
            { "TMP_SpriteAnimator", "Sprite-based animation system for TMP fonts." },
            { "DropdownItem", "Custom dropdown item or legacy UI dropdown list element." },
            { "TextContainer", "Legacy text bounding box from TMP or old TMPro versions." }
        };

        [MenuItem("Tools/DevToolkit Suite/Script & Component Analyzer",false,37)]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptComponentAnalyzerWindow>("Script Analyzer");
            window.minSize = new Vector2(450, 500);
        }

        private void OnEnable()
        {
            statsBarBg = EditorGUIUtility.isProSkin ? EditorGUIUtility.Load("builtin skins/darkskin/images/project.png") as Texture2D : EditorGUIUtility.Load("builtin skins/lightskin/images/project.png") as Texture2D;
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            ScanScene();
        }

        private void InitStyles()
        {
            // Modern styles
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

            if (itemBoxStyle == null)
            {
                itemBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 2, 2),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
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

            // Legacy styles for backward compatibility
            sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.cyan : Color.blue }
            };

            boxHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };

            statsTextStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
            };
        }

        private void ScanScene()
        {
            scriptUsage = new Dictionary<string, List<GameObject>>();
            missingScripts = new List<GameObject>();
            unusedScripts = new List<string>();
            componentTypeCounts = new Dictionary<string, int>();
            componentUsage = new Dictionary<string, List<GameObject>>();

            var scenes = new List<Scene>();

            if (onlyActiveScene)
            {
                scenes.Add(SceneManager.GetActiveScene());
                ProcessScenes(scenes);
            }
            else
            {
                string currentScenePath = SceneManager.GetActiveScene().path;
                string[] scenePaths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

                if (EditorSceneManager.GetSceneByPath(currentScenePath).isDirty)
                {
                    if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        Debug.Log("Scene save canceled. Aborting scan.");
                        return;
                    }
                }

                foreach (string scenePath in scenePaths)
                {
                    Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    scenes.Add(openedScene);
                }

                ProcessScenes(scenes);

                foreach (var scene in scenes)
                {
                    if (scene.path != currentScenePath)
                        EditorSceneManager.CloseScene(scene, true);
                }

                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            DetectUnusedMonoBehaviours();
        }

        private void ProcessScenes(List<Scene> scenes)
        {
            foreach (var scene in scenes)
            {
                if (!scene.isLoaded) continue;

                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    foreach (var comp in rootObj.GetComponentsInChildren<Component>(true))
                    {
                        if (comp == null || comp.GetType() == null)
                        {
                            if (!missingScripts.Contains(rootObj))
                                missingScripts.Add(rootObj);
                            continue;
                        }

                        string compName = comp.GetType().Name;

                        if (comp is MonoBehaviour mono)
                        {
                            if (!scriptUsage.ContainsKey(compName))
                                scriptUsage[compName] = new List<GameObject>();
                            scriptUsage[compName].Add(mono.gameObject);
                        }

                        if (!componentTypeCounts.ContainsKey(compName))
                            componentTypeCounts[compName] = 0;
                        componentTypeCounts[compName]++;

                        if (!componentUsage.ContainsKey(compName))
                            componentUsage[compName] = new List<GameObject>();
                        componentUsage[compName].Add(comp.gameObject);
                    }
                }
            }
        }

        private void DetectUnusedMonoBehaviours()
        {
            var monoScriptTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(t => t.IsSubclassOf(typeof(MonoBehaviour)) && !t.IsAbstract && t.Namespace != null && !t.Namespace.StartsWith("Unity"));

            foreach (var type in monoScriptTypes)
            {
                if (!scriptUsage.ContainsKey(type.Name))
                {
                    unusedScripts.Add(type.Name);
                }
            }
        }

        private void OnGUI()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🧠 Script & Component Analyzer", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Statistics Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Statistics Overview", sectionLabelStyle);
            DrawModernStatsBar();
            EditorGUILayout.EndVertical();

            // Search & Controls Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔍 Search & Controls", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            // Search bar
            EditorGUILayout.LabelField("Search:", EditorStyles.miniLabel);
            search = EditorGUILayout.TextField(search, GUILayout.Height(22));
            
            EditorGUILayout.Space(5);
            
            // Options and buttons
            bool isNarrow = position.width < 500;
            if (isNarrow)
            {
                // Stack vertically for narrow windows
                onlyActiveScene = EditorGUILayout.Toggle("🎯 Only Active Scene", onlyActiveScene);
                EditorGUILayout.Space(3);
                
                if (GradientButton("🔄 Scan Scene", gradientTex, gradientButtonStyle))
                {
                    ScanScene();
                }
                EditorGUILayout.Space(2);
                if (GradientButton("📄 Export CSV", gradientTex, gradientButtonStyle))
                {
                    ExportStats();
                }
            }
            else
            {
                // Horizontal layout for wider windows
                EditorGUILayout.BeginHorizontal();
                onlyActiveScene = EditorGUILayout.Toggle("🎯 Only Active Scene", onlyActiveScene);
                GUILayout.FlexibleSpace();
                
                if (GradientButton("🔄 Scan Scene", gradientTex, gradientButtonStyle, GUILayout.Width(120)))
                {
                    ScanScene();
                }
                if (GradientButton("📄 Export CSV", gradientTex, gradientButtonStyle, GUILayout.Width(120)))
                {
                    ExportStats();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            // Analysis Results
            DrawModernSection("📋 Used Scripts", scriptUsage, true);
            DrawModernMissingSection();
            DrawModernListSection("⚠️ Unused MonoBehaviours", unusedScripts);
            DrawModernComponentTypes();

            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        private void ExportStats()
        {
            string path = EditorUtility.SaveFilePanel("Export Script Stats", "", "ScriptComponentStats.csv", "csv");
            if (string.IsNullOrEmpty(path)) return;

            var lines = new List<string> { "Category,Name,Count" };

            foreach (var kvp in scriptUsage)
                lines.Add($"Used Script,{kvp.Key},{kvp.Value.Count}");
            foreach (var name in unusedScripts)
                lines.Add($"Unused Script,{name},0");
            foreach (var kvp in componentTypeCounts)
                lines.Add($"Component Type,{kvp.Key},{kvp.Value}");

            System.IO.File.WriteAllLines(path, lines);
            EditorUtility.RevealInFinder(path);
        }

        private void DrawModernStatsBar()
        {
            int totalScripts = scriptUsage.Count;
            int totalObjects = scriptUsage.Sum(s => s.Value.Count);
            int missingCount = missingScripts.Count;
            int unusedCount = unusedScripts.Count;

            EditorGUILayout.BeginVertical(statsBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📈 Total Scripts: {totalScripts}", statLabelStyle);
            EditorGUILayout.LabelField($"🎯 Objects: {totalObjects}", statLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"❌ Missing: {missingCount}", statLabelStyle);
            EditorGUILayout.LabelField($"⚠️ Unused: {unusedCount}", statLabelStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatsBar()
        {
            int totalScripts = scriptUsage.Count;
            int totalObjects = scriptUsage.Sum(s => s.Value.Count);
            int missingCount = missingScripts.Count;

            Rect rect = EditorGUILayout.BeginHorizontal("box", GUILayout.Height(30));

            // Eğer arkaplan texture yüklenemediyse, düz bir renk kullan
            if (statsBarBg != null)
            {
                GUI.DrawTexture(rect, statsBarBg, ScaleMode.StretchToFill);
            }
            else
            {
                EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.15f, 0.15f, 0.15f) : new Color(0.85f, 0.85f, 0.85f));
            }

            GUILayout.Label($"Total Scripts: {totalScripts}    |    Objects With Scripts: {totalObjects}    |    Missing Scripts: {missingCount}", statsTextStyle);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSection(string title, Dictionary<string, List<GameObject>> data, bool showCount)
        {
            GUILayout.Label(title, sectionHeaderStyle);

            if (data.Count == 0)
            {
                EditorGUILayout.HelpBox("No data found.", MessageType.Info);
                return;
            }

            foreach (var entry in data.OrderBy(e => e.Key))
            {
                if (!string.IsNullOrEmpty(search) && !entry.Key.ToLower().Contains(search.ToLower()))
                    continue;

                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(entry.Key, GUILayout.Width(200));
                if (showCount)
                    GUILayout.Label($"({entry.Value.Count})", GUILayout.Width(40));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Select", GUILayout.Width(60)))
                    Selection.objects = entry.Value.ToArray();

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawMissingSection()
        {
            GUILayout.Label("❌ Missing Scripts", sectionHeaderStyle);

            if (missingScripts.Count == 0)
            {
                EditorGUILayout.HelpBox("No missing scripts found.", MessageType.Info);
                return;
            }

            foreach (var obj in missingScripts)
            {
                if (obj == null) continue;

                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(obj.name, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawListSection(string title, List<string> items)
        {
            GUILayout.Label(title, sectionHeaderStyle);

            if (items.Count == 0)
            {
                EditorGUILayout.HelpBox("None found.", MessageType.Info);
                return;
            }

            foreach (var name in items.OrderBy(n => n))
            {
                string tooltip = componentTooltips.ContainsKey(name)
                    ? componentTooltips[name]
                    : $"MonoBehaviour '{name}' appears unused in current scenes.";

                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(new GUIContent(name, tooltip), GUILayout.Width(200));

                if (componentUsage.ContainsKey(name))
                {
                    GUILayout.Label($"{componentUsage[name].Count} object(s)", GUILayout.Width(100));
                    if (GUILayout.Button("Select Objects", GUILayout.Width(120)))
                    {
                        Selection.objects = componentUsage[name].ToArray();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawComponentTypes()
        {
            GUILayout.Label("🔍 Component Type Counts", sectionHeaderStyle);

            if (componentTypeCounts.Count == 0)
            {
                EditorGUILayout.HelpBox("No components found.", MessageType.Info);
                return;
            }

            foreach (var entry in componentTypeCounts.OrderBy(e => e.Key))
            {
                EditorGUILayout.BeginHorizontal("box");
                GUILayout.Label(entry.Key, GUILayout.Width(200));
                GUILayout.Label($"({entry.Value})", GUILayout.Width(50));

                GUILayout.FlexibleSpace();

                if (componentUsage.ContainsKey(entry.Key))
                {
                    if (GUILayout.Button("Select Objects", GUILayout.Width(100)))
                    {
                        Selection.objects = componentUsage[entry.Key].ToArray();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // Modern versions of drawing methods
        private void DrawModernSection(string title, Dictionary<string, List<GameObject>> data, bool showCount)
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField(title, sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (data.Count == 0)
            {
                EditorGUILayout.HelpBox("📂 No data found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var entry in data.OrderBy(e => e.Key))
            {
                if (!string.IsNullOrEmpty(search) && !entry.Key.ToLower().Contains(search.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical(itemBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                // Script name and count
                EditorGUILayout.LabelField($"📄 {entry.Key}", EditorStyles.boldLabel, GUILayout.Width(200));
                if (showCount)
                    EditorGUILayout.LabelField($"({entry.Value.Count})", GUILayout.Width(50));

                GUILayout.FlexibleSpace();

                if (GradientButton("🎯 Select", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    Selection.objects = entry.Value.ToArray();

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernMissingSection()
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("❌ Missing Scripts", sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (missingScripts.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ No missing scripts found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var obj in missingScripts)
            {
                if (obj == null) continue;

                EditorGUILayout.BeginVertical(itemBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"⚠️ {obj.name}", EditorStyles.boldLabel, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
                
                if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernListSection(string title, List<string> items)
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField(title, sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (items.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ None found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var name in items.OrderBy(n => n))
            {
                if (!string.IsNullOrEmpty(search) && !name.ToLower().Contains(search.ToLower()))
                    continue;

                string tooltip = componentTooltips.ContainsKey(name)
                    ? componentTooltips[name]
                    : $"MonoBehaviour '{name}' appears unused in current scenes.";

                EditorGUILayout.BeginVertical(itemBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField(new GUIContent($"⚠️ {name}", tooltip), EditorStyles.boldLabel, GUILayout.Width(200));

                if (componentUsage.ContainsKey(name))
                {
                    EditorGUILayout.LabelField($"({componentUsage[name].Count})", GUILayout.Width(50));
                    GUILayout.FlexibleSpace();
                    
                    if (GradientButton("🎯 Select", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    {
                        Selection.objects = componentUsage[name].ToArray();
                    }
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernComponentTypes()
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔍 Component Type Counts", sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (componentTypeCounts.Count == 0)
            {
                EditorGUILayout.HelpBox("📂 No components found.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var entry in componentTypeCounts.OrderBy(e => e.Key))
            {
                if (!string.IsNullOrEmpty(search) && !entry.Key.ToLower().Contains(search.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical(itemBoxStyle);
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"🔧 {entry.Key}", EditorStyles.boldLabel, GUILayout.Width(200));
                EditorGUILayout.LabelField($"({entry.Value})", GUILayout.Width(50));

                GUILayout.FlexibleSpace();

                if (componentUsage.ContainsKey(entry.Key))
                {
                    if (GradientButton("🎯 Select", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    {
                        Selection.objects = componentUsage[entry.Key].ToArray();
                    }
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // Helper methods for modern UI
        private bool GradientButton(string text, Texture2D hoverTex, GUIStyle style, params GUILayoutOption[] options)
        {
            // Simplified version that works better with EditorGUILayout
            GUIContent content = new GUIContent(text);
            
            // Use a consistent style for buttons
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
