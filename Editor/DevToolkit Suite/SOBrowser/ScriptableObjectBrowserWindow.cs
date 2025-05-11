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

        [MenuItem("Tools/DevToolkit Suite/ScriptableObject Browser", false,1)]
        public static void ShowWindow()
        {
            GetWindow<ScriptableObjectBrowserWindow>("ScriptableObject Browser");
        }

        private void OnEnable()
        {
            RefreshList();
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

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Select ScriptableObject class", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Scan Usage", GUILayout.Width(100)))
            {
                ScanSceneUsage();
            }
            if (GUILayout.Button("Config", GUILayout.Width(60)))
            {
                ScriptableObjectConfig.ShowWindow(this);
            }
            EditorGUILayout.EndHorizontal();

            search = EditorGUILayout.TextField(search);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var type in soByType.Keys.OrderBy(k => k.Name))
            {
                if (!string.IsNullOrEmpty(search) && !type.Name.ToLower().Contains(search.ToLower())) continue;
                if (GUILayout.Button(type.Name, GUILayout.Height(30)))
                {
                    selectedType = type;
                    selectedObject = null;
                }
            }
            EditorGUILayout.EndScrollView();

            if (selectedType != null && soByType.ContainsKey(selectedType))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Instances of {selectedType.Name}", EditorStyles.boldLabel);

                var list = soByType[selectedType];
                foreach (var item in list)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(item.name, GUILayout.Height(25)))
                    {
                        selectedObject = item;
                        EditorGUIUtility.PingObject(item);
                    }

                    if (soUsage.TryGetValue(item, out var sceneList))
                    {
                        GUIStyle usageStyle = new GUIStyle(EditorStyles.label)
                        {
                            fontStyle = FontStyle.Bold,
                            fontSize = 11,
                            normal = { textColor = new Color(0.65f, 0.65f, 0.65f) },
                            alignment = TextAnchor.MiddleRight
                        };

                        string usageText = $"🧩 Used in {sceneList.Count} scene(s)";
                        string tooltip = $"This ScriptableObject is referenced in:\n• " + string.Join("\n• ", sceneList.Select(Path.GetFileNameWithoutExtension));
                        GUILayout.Label(new GUIContent(usageText, tooltip), usageStyle, GUILayout.MaxWidth(200));
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (selectedObject != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                Editor editor = Editor.CreateEditor(selectedObject);
                editor.OnInspectorGUI();
            }
        }

        private void ScanSceneUsage()
        {
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
    }
}
