// SceneLevelInfoPanel - Detailed Scene & Level Analysis Panel with Filters, Search & Missing Link Detection
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace DevToolkit_Suite
{
    public class SceneLevelInfoPanel : EditorWindow
    {
        private Vector2 scroll;
        private Dictionary<string, int> prefabCounts;
        private List<GameObject> triggers;
        private List<GameObject> colliders;
        private List<GameObject> rigidbodies;
        private List<GameObject> prefabs;
        private Dictionary<GameObject, string> brokenLinks;

        private bool showTriggers = true;
        private bool showColliders = true;
        private bool showRigidbodies = true;
        private bool showPrefabs = true;
        private bool showBrokenLinks = true;
        private string searchQuery = "";

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

        [MenuItem("Tools/DevToolkit Suite/Scene & Level Info Panel",false,36)]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneLevelInfoPanel>("Scene Info");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            AnalyzeScene();
        }

        private void AnalyzeScene()
        {
            prefabCounts = new Dictionary<string, int>();
            triggers = new List<GameObject>();
            colliders = new List<GameObject>();
            rigidbodies = new List<GameObject>();
            prefabs = new List<GameObject>();
            brokenLinks = new Dictionary<GameObject, string>();

            Scene scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            foreach (var root in roots)
            {
                foreach (var go in root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject))
                {
                    var prefabType = PrefabUtility.GetPrefabAssetType(go);
                    if (prefabType != PrefabAssetType.NotAPrefab)
                    {
                        string name = go.name;
                        if (!prefabCounts.ContainsKey(name))
                            prefabCounts[name] = 0;
                        prefabCounts[name]++;
                        prefabs.Add(go);
                    }

                    if (go.TryGetComponent(out Collider col))
                    {
                        colliders.Add(go);
                        if (col.isTrigger)
                            triggers.Add(go);
                    }

                    if (go.TryGetComponent(out Rigidbody rb))
                    {
                        rigidbodies.Add(go);
                    }

                    foreach (var comp in go.GetComponents<Component>())
                    {
                        if (comp == null)
                        {
                            if (!brokenLinks.ContainsKey(go))
                                brokenLinks[go] = "Missing MonoBehaviour";
                            continue;
                        }

                        SerializedObject so = new SerializedObject(comp);
                        SerializedProperty prop = so.GetIterator();
                        while (prop.NextVisible(true))
                        {
                            if (prop.propertyType == SerializedPropertyType.ObjectReference &&
                                prop.objectReferenceValue == null &&
                                prop.objectReferenceInstanceIDValue != 0)
                            {
                                if (!brokenLinks.ContainsKey(go))
                                {
                                    brokenLinks[go] = $"{comp.GetType().Name}: {prop.displayName}";
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (prefabCounts == null) AnalyzeScene();

            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("🌍 Scene & Level Info", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Statistics Overview Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Scene Statistics", sectionLabelStyle);
            DrawModernStatsOverview();
            EditorGUILayout.EndVertical();

            // Search & Filter Controls Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔍 Search & Filters", sectionLabelStyle);
            EditorGUILayout.Space(5);
            DrawModernFilterControls();
            EditorGUILayout.EndVertical();

            // Analysis Results
            if (showPrefabs) DrawModernPrefabCounts();
            if (showTriggers) DrawModernFilteredList("🎯 Triggers", triggers);
            if (showColliders) DrawModernFilteredList("🧱 Colliders", colliders);
            if (showRigidbodies) DrawModernFilteredList("⚙️ Rigidbodies", rigidbodies);
            if (showBrokenLinks) DrawModernBrokenLinks();

            // End scroll view
            EditorGUILayout.EndScrollView();
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
        }

        private void DrawModernStatsOverview()
        {
            EditorGUILayout.BeginVertical(statsBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Responsive stats layout
            bool isVeryNarrow = position.width < 380;
            bool isNarrow = position.width < 500;

            if (isVeryNarrow)
            {
                // Stack all stats vertically for very narrow windows
                EditorGUILayout.LabelField($"📦 Prefabs: {prefabCounts.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"🎯 Triggers: {triggers.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"🧱 Colliders: {colliders.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"⚙️ Rigidbodies: {rigidbodies.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"❌ Broken Links: {brokenLinks.Count}", statLabelStyle);
                
                EditorGUILayout.Space(3);
                if (GradientButton("🔄 Refresh", gradientTex, gradientButtonStyle))
                {
                    AnalyzeScene();
                }
            }
            else if (isNarrow)
            {
                // 2x3 grid layout for narrow windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📦 Prefabs: {prefabCounts.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"🎯 Triggers: {triggers.Count}", statLabelStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🧱 Colliders: {colliders.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"⚙️ Rigidbodies: {rigidbodies.Count}", statLabelStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"❌ Broken Links: {brokenLinks.Count}", statLabelStyle);
                GUILayout.FlexibleSpace();
                if (GradientButton("🔄 Refresh", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                {
                    AnalyzeScene();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Original horizontal layout for wider windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📦 Prefabs: {prefabCounts.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"🎯 Triggers: {triggers.Count}", statLabelStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🧱 Colliders: {colliders.Count}", statLabelStyle);
                EditorGUILayout.LabelField($"⚙️ Rigidbodies: {rigidbodies.Count}", statLabelStyle);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"❌ Broken Links: {brokenLinks.Count}", statLabelStyle);
                
                GUILayout.FlexibleSpace();
                if (GradientButton("🔄 Refresh", gradientTex, gradientButtonStyle, GUILayout.Width(100)))
                {
                    AnalyzeScene();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernFilterControls()
        {
            // Search bar
            EditorGUILayout.LabelField("Search:", EditorStyles.miniLabel);
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Height(22));
            
            EditorGUILayout.Space(5);
            
            // Filter toggles - responsive layout with multiple breakpoints
            bool isVeryNarrow = position.width < 400;
            bool isNarrow = position.width < 550;
            bool isMedium = position.width < 700;

            if (isVeryNarrow)
            {
                // Stack all vertically for very narrow windows
                showTriggers = EditorGUILayout.Toggle("🎯 Triggers", showTriggers);
                showColliders = EditorGUILayout.Toggle("🧱 Colliders", showColliders);
                showRigidbodies = EditorGUILayout.Toggle("⚙️ Rigidbodies", showRigidbodies);
                showPrefabs = EditorGUILayout.Toggle("📦 Prefabs", showPrefabs);
                showBrokenLinks = EditorGUILayout.Toggle("❌ Broken Links", showBrokenLinks);
            }
            else if (isNarrow)
            {
                // 2x3 grid layout for narrow windows
                EditorGUILayout.BeginHorizontal();
                showTriggers = EditorGUILayout.Toggle("🎯 Triggers", showTriggers);
                showColliders = EditorGUILayout.Toggle("🧱 Colliders", showColliders);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                showRigidbodies = EditorGUILayout.Toggle("⚙️ Rigidbodies", showRigidbodies);
                showPrefabs = EditorGUILayout.Toggle("📦 Prefabs", showPrefabs);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                showBrokenLinks = EditorGUILayout.Toggle("❌ Broken Links", showBrokenLinks);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (isMedium)
            {
                // 3+2 layout for medium windows
                EditorGUILayout.BeginHorizontal();
                showTriggers = EditorGUILayout.Toggle("🎯 Triggers", showTriggers);
                showColliders = EditorGUILayout.Toggle("🧱 Colliders", showColliders);
                showRigidbodies = EditorGUILayout.Toggle("⚙️ Rigidbodies", showRigidbodies);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                showPrefabs = EditorGUILayout.Toggle("📦 Prefabs", showPrefabs);
                showBrokenLinks = EditorGUILayout.Toggle("❌ Broken Links", showBrokenLinks);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // All in one row for wide windows
                EditorGUILayout.BeginHorizontal();
                showTriggers = EditorGUILayout.Toggle("🎯 Triggers", showTriggers);
                showColliders = EditorGUILayout.Toggle("🧱 Colliders", showColliders);
                showRigidbodies = EditorGUILayout.Toggle("⚙️ Rigidbodies", showRigidbodies);
                showPrefabs = EditorGUILayout.Toggle("📦 Prefabs", showPrefabs);
                showBrokenLinks = EditorGUILayout.Toggle("❌ Broken Links", showBrokenLinks);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawModernPrefabCounts()
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📦 Prefab Counts", sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (prefabCounts.Count == 0)
            {
                EditorGUILayout.HelpBox("📂 No prefabs found in scene.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var kvp in prefabCounts.OrderBy(x => x.Key))
            {
                EditorGUILayout.BeginVertical(itemBoxStyle);
                
                // Responsive layout for prefab items
                bool isNarrow = position.width < 450;
                if (isNarrow)
                {
                    // Stack vertically for narrow windows
                    EditorGUILayout.LabelField($"📄 {kvp.Key}", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Count: x{kvp.Value}", GUILayout.Width(80));
                    GUILayout.FlexibleSpace();
                    
                    if (GradientButton("🔍 Find All", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    {
                        var matching = prefabs.Where(p => p.name == kvp.Key).ToArray();
                        Selection.objects = matching;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Horizontal layout for wider windows
                    EditorGUILayout.BeginHorizontal();
                    
                    // Calculate dynamic width based on window size
                    float nameWidth = Mathf.Min(200, position.width * 0.4f);
                    EditorGUILayout.LabelField($"📄 {kvp.Key}", EditorStyles.boldLabel, GUILayout.Width(nameWidth));
                    EditorGUILayout.LabelField($"x{kvp.Value}", GUILayout.Width(50));
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GradientButton("🔍 Find All", gradientTex, gradientButtonStyle, GUILayout.Width(100)))
                    {
                        var matching = prefabs.Where(p => p.name == kvp.Key).ToArray();
                        Selection.objects = matching;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernFilteredList(string title, List<GameObject> list)
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField(title, sectionLabelStyle);
            EditorGUILayout.Space(5);

            var filtered = string.IsNullOrEmpty(searchQuery) ? list : list.Where(go => go.name.ToLower().Contains(searchQuery.ToLower())).ToList();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox($"📂 No {title.ToLower().Replace("🎯", "").Replace("🧱", "").Replace("⚙️", "").Trim()} found matching criteria.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var go in filtered)
            {
                if (go == null) continue;

                EditorGUILayout.BeginVertical(itemBoxStyle);
                
                // Responsive layout for list items
                bool isNarrow = position.width < 400;
                if (isNarrow)
                {
                    // Stack vertically for narrow windows
                    EditorGUILayout.LabelField(new GUIContent($"🎮 {go.name}", "Object in scene hierarchy"), EditorStyles.boldLabel);
                    if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Height(22)))
                    {
                        EditorGUIUtility.PingObject(go);
                        Selection.activeObject = go;
                    }
                }
                else
                {
                    // Horizontal layout for wider windows
                    EditorGUILayout.BeginHorizontal();
                    
                    // Calculate dynamic width for object name
                    float nameWidth = Mathf.Min(250, position.width * 0.6f);
                    EditorGUILayout.LabelField(new GUIContent($"🎮 {go.name}", "Object in scene hierarchy"), EditorStyles.boldLabel, GUILayout.Width(nameWidth));
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    {
                        EditorGUIUtility.PingObject(go);
                        Selection.activeObject = go;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernBrokenLinks()
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("❌ Broken Links", sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (brokenLinks.Count == 0)
            {
                EditorGUILayout.HelpBox("✅ No broken references found in scene.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            foreach (var pair in brokenLinks)
            {
                if (pair.Key == null) continue;

                EditorGUILayout.BeginVertical(itemBoxStyle);
                
                // Responsive layout for broken links
                bool isVeryNarrow = position.width < 350;
                bool isNarrow = position.width < 500;
                
                if (isVeryNarrow)
                {
                    // Stack all vertically for very narrow windows
                    EditorGUILayout.LabelField($"⚠️ {pair.Key.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"🔗 {pair.Value}", EditorStyles.miniLabel);
                    if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Height(22)))
                    {
                        EditorGUIUtility.PingObject(pair.Key);
                        Selection.activeObject = pair.Key;
                    }
                }
                else if (isNarrow)
                {
                    // Two row layout for narrow windows
                    EditorGUILayout.LabelField($"⚠️ {pair.Key.name}", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"🔗 {pair.Value}", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Width(70)))
                    {
                        EditorGUIUtility.PingObject(pair.Key);
                        Selection.activeObject = pair.Key;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // Original horizontal layout for wider windows
                    EditorGUILayout.BeginHorizontal();
                    
                    float nameWidth = Mathf.Min(150, position.width * 0.3f);
                    EditorGUILayout.LabelField($"⚠️ {pair.Key.name}", EditorStyles.boldLabel, GUILayout.Width(nameWidth));
                    EditorGUILayout.LabelField($"🔗 {pair.Value}", EditorStyles.miniLabel);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GradientButton("📍 Ping", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
                    {
                        EditorGUIUtility.PingObject(pair.Key);
                        Selection.activeObject = pair.Key;
                    }

                    EditorGUILayout.EndHorizontal();
                }
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
