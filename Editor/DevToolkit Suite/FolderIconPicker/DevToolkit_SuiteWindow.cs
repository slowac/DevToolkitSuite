using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DevToolkit_Suite
{
    public class DevToolkit_SuiteWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private Dictionary<string, Texture2D> colorIcons;
        private Dictionary<string, Texture2D> customIcons;

        private Texture2D headerIcon;
        private Texture2D customIcon;
        private Texture2D colorIcon;

        private Object selectedFolder;

        [MenuItem("Tools/DevToolkit Suite/Folder Icon Picker",false,25)]
        public static void ShowWindow()
        {
            var window = GetWindow<DevToolkit_SuiteWindow>("Folder Icon Picker");
            window.minSize = new Vector2(600, 600);
        }

        private void OnEnable()
        {
            FolderIconManager.BuildIconDictionaries(out colorIcons, out customIcons);

            headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/custom_icon.png");
            customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/custom_icon.png");
            colorIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/color_icon.png");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (headerIcon != null)
                GUILayout.Label(headerIcon, GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Label("Folder Icon Picker", EditorStyles.largeLabel);
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                GUILayout.Label("Drop Folder", GUILayout.Width(80));
                selectedFolder = EditorGUILayout.ObjectField(selectedFolder, typeof(DefaultAsset), false);
            }

            EditorGUILayout.Space(12);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            EditorGUILayout.BeginHorizontal();

            // Left: Custom Icons
            EditorGUILayout.BeginVertical();
            DrawGridSection("Custom Icons", customIcons, customIcon);
            EditorGUILayout.EndVertical();

            // Right: Color Icons
            EditorGUILayout.BeginVertical();
            DrawGridSection("Color Icons", colorIcons, colorIcon);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Reset Icon", GUILayout.Width(120), GUILayout.Height(28)))
                {
                    if (selectedFolder != null)
                    {
                        FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                    }
                }
                if (GUILayout.Button("Reset All", GUILayout.Width(120), GUILayout.Height(28)))
                {
                    FolderIconManager.ClearAllIcons();
                }
                GUILayout.FlexibleSpace();
            }
        }

        private void DrawGridSection(string title, Dictionary<string, Texture2D> iconDict, Texture2D sectionIcon)
        {
            GUILayout.BeginHorizontal();
            if (sectionIcon != null)
                GUILayout.Label(sectionIcon, GUILayout.Width(18), GUILayout.Height(18));
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.EndHorizontal();

            int columns = 3;
            int count = 0;

            foreach (var kvp in iconDict)
            {
                if (count % columns == 0) EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical(GUILayout.Width(90));
                if (kvp.Key == "Default")
                {
                    if (GUILayout.Button(kvp.Value, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        if (selectedFolder != null)
                        {
                            FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                        }
                    }
                }
                else
                {
                    if (GUILayout.Button(kvp.Value, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        if (selectedFolder != null)
                        {
                            FolderIconManager.AssignFolderIcon(AssetDatabase.GetAssetPath(selectedFolder), kvp.Key);
                        }
                    }
                }
                GUILayout.Label(kvp.Key, EditorStyles.miniLabel, GUILayout.Width(90), GUILayout.Height(16));
                EditorGUILayout.EndVertical();

                count++;
                if (count % columns == 0) EditorGUILayout.EndHorizontal();
            }
            if (count % columns != 0) EditorGUILayout.EndHorizontal();
        }
    }

    public static class FolderIconManager
    {
        private static Dictionary<string, Texture2D> iconDict = new();

        public static void BuildIconDictionaries(out Dictionary<string, Texture2D> colorIcons, out Dictionary<string, Texture2D> customIcons)
        {
            colorIcons = new Dictionary<string, Texture2D>();
            customIcons = new Dictionary<string, Texture2D>();

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (path.Contains("ColorIcons"))
                    colorIcons[Path.GetFileNameWithoutExtension(path)] = tex;
                else if (path.Contains("CustomFolderIcons"))
                    customIcons[Path.GetFileNameWithoutExtension(path)] = tex;

                iconDict[Path.GetFileNameWithoutExtension(path)] = tex;
            }
        }

        public static void AssignFolderIcon(string folderPath, string iconName)
        {
            string folderName = Path.GetFileName(folderPath);
            EditorPrefs.SetString($"EPU_Icon_{folderName}", iconName);
            EditorApplication.RepaintProjectWindow(); // <-- burada
        }

        public static void ClearFolderIcon(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);

            // 1. EditorPrefs temizliği
            EditorPrefs.DeleteKey($"EPU_Icon_{folderName}");

            // 2. AutoFolderCreator'dan gelen icon varsa onu da sil
            string assetPath = $"Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/AutoFolderCreator/Data/{folderName}.asset";
            if (File.Exists(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            // 3. Dictionary'yi sıfırla ve görseli güncelle
            FolderIconRenderer.RefreshDictionary();
            AssetDatabase.Refresh();
            EditorApplication.RepaintProjectWindow();
        }

        public static void ClearAllIcons()
        {
            var folderNames = EditorPrefs.GetString("EPU_AllFolderNames", "").Split(';');
            foreach (var folderName in folderNames)
            {
                if (string.IsNullOrEmpty(folderName)) continue;

                // 1. Pref key sil
                EditorPrefs.DeleteKey($"EPU_Icon_{folderName}");

                // 2. AutoFolderCreator'dan gelen .asset dosyasını sil
                string autoFolderAsset = $"Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/AutoFolderCreator/Data/{folderName}.asset";
                if (File.Exists(autoFolderAsset))
                {
                    AssetDatabase.DeleteAsset(autoFolderAsset);
                }
            }

            // 3. AutoFolderCreator içindeki tüm .asset dosyalarını da ekstra kontrol et
            string autoDataDir = "Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/AutoFolderCreator/Data";
            if (Directory.Exists(autoDataDir))
            {
                string[] leftoverAssets = Directory.GetFiles(autoDataDir, "*.asset", SearchOption.TopDirectoryOnly);
                foreach (var asset in leftoverAssets)
                {
                    AssetDatabase.DeleteAsset(asset.Replace("\\", "/"));
                }
            }

            EditorPrefs.DeleteKey("EPU_AllFolderNames");

            FolderIconRenderer.RefreshDictionary();
            AssetDatabase.Refresh();
            EditorApplication.RepaintProjectWindow();
        }


        public static bool TryGetFolderIcon(string folderName, out Texture2D icon)
        {
            icon = null;
            string iconName = EditorPrefs.GetString($"EPU_Icon_{folderName}", "");
            if (string.IsNullOrEmpty(iconName)) return false;
            icon = iconDict.ContainsKey(iconName) ? iconDict[iconName] : null;

            // store name for ClearAll
            var all = new HashSet<string>(EditorPrefs.GetString("EPU_AllFolderNames", "").Split(';'));
            if (!all.Contains(folderName))
            {
                all.Add(folderName);
                EditorPrefs.SetString("EPU_AllFolderNames", string.Join(";", all));
            }

            return icon != null;
        }
    }
}
