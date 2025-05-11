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

        [MenuItem("Tools/DevToolkit Suite/Scene & Level Info Panel")]
        public static void ShowWindow()
        {
            GetWindow<SceneLevelInfoPanel>("Scene Info").minSize = new Vector2(420, 520);
        }

        private void OnEnable()
        {
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

            EditorGUILayout.Space();
            GUILayout.Label("🌍 Scene & Level Info", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("🔄 Refresh", GUILayout.Width(100), GUILayout.Height(26)))
            {
                AnalyzeScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            DrawFilterControls();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (showPrefabs) DrawCategory("📦 Prefab Counts", prefabCounts.Select((kv, i) => (label: $"{kv.Key} x{kv.Value}", obj: (GameObject)null)).ToList());
            if (showTriggers) DrawFilteredList("🎯 Triggers", triggers);
            if (showColliders) DrawFilteredList("🧱 Colliders", colliders);
            if (showRigidbodies) DrawFilteredList("⚙️ Rigidbodies", rigidbodies);
            if (showPrefabs) DrawFilteredList("📦 Prefabs", prefabs);
            if (showBrokenLinks) DrawBrokenLinks();

            EditorGUILayout.EndScrollView();
        }

        private void InitStyles()
        {
            GUI.skin.label.fontSize = 12;
            GUI.skin.label.fontStyle = FontStyle.Bold;
        }

        private void DrawFilterControls()
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label("🔍 Search:", GUILayout.Width(60));
            searchQuery = EditorGUILayout.TextField(searchQuery);
            GUILayout.FlexibleSpace();
            showTriggers = EditorGUILayout.ToggleLeft("Triggers", showTriggers, GUILayout.Width(80));
            showColliders = EditorGUILayout.ToggleLeft("Colliders", showColliders, GUILayout.Width(90));
            showRigidbodies = EditorGUILayout.ToggleLeft("Rigidbodies", showRigidbodies, GUILayout.Width(100));
            showPrefabs = EditorGUILayout.ToggleLeft("Prefabs", showPrefabs, GUILayout.Width(90));
            showBrokenLinks = EditorGUILayout.ToggleLeft("Broken Links", showBrokenLinks, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategory(string title, List<(string label, GameObject obj)> entries)
        {
            EditorGUILayout.Space();
            GUILayout.Label(title, EditorStyles.largeLabel);

            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("None found.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            foreach (var entry in entries)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(entry.label);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawFilteredList(string title, List<GameObject> list)
        {
            EditorGUILayout.Space();
            GUILayout.Label(title, EditorStyles.largeLabel);

            var filtered = string.IsNullOrEmpty(searchQuery) ? list : list.Where(go => go.name.ToLower().Contains(searchQuery.ToLower())).ToList();

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("None found.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            foreach (var go in filtered)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(go.name, "Click to highlight in hierarchy"));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(go);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawBrokenLinks()
        {
            EditorGUILayout.Space();
            GUILayout.Label("❌ Broken Links", EditorStyles.largeLabel);

            if (brokenLinks.Count == 0)
            {
                EditorGUILayout.HelpBox("No broken references found.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("box");
            foreach (var pair in brokenLinks)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(pair.Key.name + " - " + pair.Value);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(pair.Key);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }
    }
}
