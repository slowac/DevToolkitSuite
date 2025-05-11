using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DevToolkit_Suite
{
    public class ScriptableObjectConfig: EditorWindow
    {
        private List<string> ignoredNamespaces = new List<string>();
        private string newLineContent;
        private ScriptableObjectBrowserWindow parentWindow;

        public static void ShowWindow(ScriptableObjectBrowserWindow parent)
        {
            var win = GetWindow<ScriptableObjectConfig>("Edit Config");
            win.parentWindow = parent;
        }

        private void OnEnable()
        {
            ignoredNamespaces = new List<string>
            {
                "UnityEngine",
                "UnityEditor",
                "TMPro",
                "Cinemachine"
            };

            newLineContent = string.Join("\n", ignoredNamespaces);
        }

        private void OnGUI()
        {
            GUILayout.Label("Ignored namespaces", EditorStyles.boldLabel);
            newLineContent = EditorGUILayout.TextArea(newLineContent, GUILayout.Height(150));

            if (GUILayout.Button("Save"))
            {
                ignoredNamespaces = newLineContent.Split('\n')
                    .Select(n => n.Trim())
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                parentWindow?.SetIgnoredNamespaces(ignoredNamespaces);
                Close();
            }
        }
    }
}
