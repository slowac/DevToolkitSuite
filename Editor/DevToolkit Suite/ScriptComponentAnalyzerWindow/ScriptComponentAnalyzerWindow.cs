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

        [MenuItem("Tools/DevToolkit Suite/Script & Component Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<ScriptComponentAnalyzerWindow>("Script Analyzer");
        }

        private void OnEnable()
        {
            statsBarBg = EditorGUIUtility.isProSkin ? EditorGUIUtility.Load("builtin skins/darkskin/images/project.png") as Texture2D : EditorGUIUtility.Load("builtin skins/lightskin/images/project.png") as Texture2D;
            ScanScene();
        }

        private void InitStyles()
        {
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
            EditorGUILayout.Space();
            GUILayout.Label("🧠 Script & Component Analyzer", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
            EditorGUILayout.Space(4);
            DrawStatsBar();
            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            search = EditorGUILayout.TextField("Search", search);
            onlyActiveScene = EditorGUILayout.ToggleLeft("Only Active Scene", onlyActiveScene, GUILayout.Width(150));
            if (GUILayout.Button("Scan", GUILayout.Width(80)))
            {
                ScanScene();
            }
            if (GUILayout.Button("Export CSV", GUILayout.Width(100)))
            {
                ExportStats();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSection("📋 Used Scripts", scriptUsage, true);
            EditorGUILayout.Space(10);
            DrawMissingSection();
            EditorGUILayout.Space(10);
            DrawListSection("⚠️ Unused MonoBehaviours", unusedScripts);
            EditorGUILayout.Space(10);
            DrawComponentTypes();
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
    }
}
