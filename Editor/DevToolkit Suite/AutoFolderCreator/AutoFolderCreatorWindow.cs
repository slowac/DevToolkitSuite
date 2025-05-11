using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DevToolkit_Suite
{
    public class AutoFolderCreatorWindow : EditorWindow
    {
        private Vector2 scroll;
        private Dictionary<string, Texture2D> folderIcons;
        private Dictionary<string, bool> folderToggles;
        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        [MenuItem("Tools/DevToolkit Suite/Auto Folder Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoFolderCreatorWindow>("Auto Folder Creator");
            window.minSize = new Vector2(420, 500);
            window.LoadIcons();
        }

        private void LoadIcons()
        {
            folderIcons = new Dictionary<string, Texture2D>();
            folderToggles = new Dictionary<string, bool>();

            string iconsPath = "Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/CustomFolderIcons";
            string[] iconPaths = Directory.GetFiles(iconsPath, "*.png", SearchOption.TopDirectoryOnly);

            foreach (string path in iconPaths)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.ToLower() == "default") continue;

                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                folderIcons[name] = icon;
                folderToggles[name] = false;
            }
        }

        private void InitStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label);
                labelStyle.alignment = TextAnchor.LowerCenter;
                labelStyle.wordWrap = true;
                labelStyle.fontSize = 11;
                labelStyle.fontStyle = FontStyle.Bold;
                labelStyle.normal.textColor = Color.white;
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box);
                boxStyle.padding = new RectOffset(6, 6, 6, 6);
                boxStyle.margin = new RectOffset(6, 6, 6, 6);
            }
        }

        private void OnGUI()
        {
            if (folderIcons == null) LoadIcons();
            InitStyles();

            GUILayout.Space(10);
            GUILayout.Label("📁 Select Folders to Create", EditorStyles.boldLabel);
            GUILayout.Space(10);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            float itemWidth = 130f;
            float availableWidth = position.width - 30; // scroll payı düş
            int columnCount = Mathf.Max(1, Mathf.FloorToInt(availableWidth / itemWidth));
            int total = folderIcons.Count;

            List<string> keys = new List<string>(folderIcons.Keys);

            for (int i = 0; i < total; i += columnCount)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                for (int j = 0; j < columnCount; j++)
                {
                    int index = i + j;
                    if (index >= total) break;

                    DrawFolderTile(keys[index]);
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);
            GUI.backgroundColor = Color.gray;
            if (GUILayout.Button("Create Selected Folders", GUILayout.Height(30)))
            {
                CreateFolders();
            }
        }

        private void DrawFolderTile(string key)
        {
            GUILayout.BeginVertical(boxStyle, GUILayout.Width(120), GUILayout.Height(120));
            GUILayout.FlexibleSpace();

            // 🔄 Ortalamayı garantilemek için yatay flexible layout kullan
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            float boxSize = 90f;
            Rect bgRect = GUILayoutUtility.GetRect(boxSize, boxSize, GUILayout.ExpandWidth(false));

            // Arka plan
            Color bg = folderToggles[key] ? new Color(0.2f, 0.5f, 1f, 0.25f) : new Color(0.15f, 0.15f, 0.15f, 0.15f);
            EditorGUI.DrawRect(bgRect, bg);

            // İkon ortalama
            float iconSize = 64f;
            Rect iconRect = new Rect(
                bgRect.x + (bgRect.width - iconSize) / 2f,
                bgRect.y + 12,
                iconSize,
                iconSize
            );
            GUI.DrawTexture(iconRect, folderIcons[key], ScaleMode.ScaleToFit);

            // Tıklanabilir alan
            if (GUI.Button(bgRect, GUIContent.none, GUIStyle.none))
            {
                folderToggles[key] = !folderToggles[key];
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Alt etiket
            GUILayout.Label(key, labelStyle);
            GUILayout.EndVertical();
        }

        private void CreateFolders()
        {
            foreach (var entry in folderToggles)
            {
                if (!entry.Value) continue;

                string folderName = entry.Key;
                string folderPath = "Assets/" + folderName;

                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder("Assets", folderName);
                }

                AssignIconToFolder(folderPath, folderName);
            }

            AssetDatabase.Refresh();
        }

        private void AssignIconToFolder(string path, string name)
        {
            string dirPath = "Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/AutoFolderCreator/Data";
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
                AssetDatabase.Refresh();
            }

            string soPath = $"{dirPath}/{name}.asset";
            var folderIconSO = AssetDatabase.LoadAssetAtPath<FolderIconSO>(soPath);

            if (folderIconSO == null)
            {
                folderIconSO = ScriptableObject.CreateInstance<FolderIconSO>();
                folderIconSO.icon = folderIcons[name];
                folderIconSO.folderNames = new List<string> { name };
                AssetDatabase.CreateAsset(folderIconSO, soPath);
            }
            else if (!folderIconSO.folderNames.Contains(name))
            {
                folderIconSO.folderNames.Add(name);
                EditorUtility.SetDirty(folderIconSO);
            }

            AssetDatabase.SaveAssets();
            FolderIconRenderer.RefreshDictionary();
        }
    }
}
