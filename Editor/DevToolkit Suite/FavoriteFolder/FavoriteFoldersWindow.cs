using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
//slowac
namespace DevToolkit_Suite
{
    public class FavoriteFoldersWindow : EditorWindow
    {
        private Vector2 scroll;
        private List<string> favoritePaths;
        private GUIStyle boxStyle;
        private GUIStyle pathLabelStyle;
        private GUIStyle headerStyle;
        private GUIStyle pingButtonStyle;
        private Texture2D pingIcon;

        [MenuItem("Tools/DevToolkit Suite/Favorite Folders")]
        public static void ShowWindow()
        {
            var window = GetWindow<FavoriteFoldersWindow>("Favorite Folders");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            LoadFavorites();
            pingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FavoriteFolder/Icons/UI/ping_icon.png");
        }

        public void LoadFavorites()
        {
            string data = EditorPrefs.GetString("EPU_FavoriteFolders", "");
            favoritePaths = new List<string>(data.Split('|'));
            favoritePaths.RemoveAll(string.IsNullOrEmpty);
            Repaint();
        }

        private void SaveFavorites()
        {
            EditorPrefs.SetString("EPU_FavoriteFolders", string.Join("|", favoritePaths));
        }

        private void InitStyles()
        {
            if (boxStyle == null)
                boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 6, 6),
                    margin = new RectOffset(4, 4, 2, 2)
                };

            if (pathLabelStyle == null)
                pathLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 12,
                    wordWrap = true,
                    margin = new RectOffset(8, 0, 4, 4)
                };

            if (headerStyle == null)
                headerStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 16,
                    fontStyle = FontStyle.Bold
                };

            if (pingButtonStyle == null)
                pingButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(4, 4, 4, 4),
                    margin = new RectOffset(0, 8, 0, 0),
                    fixedWidth = 28,
                    fixedHeight = 22
                };
        }

        private void OnGUI()
        {
            InitStyles();

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            GUILayout.Label("⭐ Favorite Folders", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            scroll = EditorGUILayout.BeginScrollView(scroll);

            if (favoritePaths.Count == 0)
            {
                GUILayout.Label("No folders added to favorites.", EditorStyles.centeredGreyMiniLabel);
            }

            foreach (var path in favoritePaths.ToArray())
            {
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = Color.white;
                if (GUILayout.Button(pingIcon != null ? pingIcon : GUIContent.none.image, pingButtonStyle))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.LabelField(path, pathLabelStyle);

                if (GUILayout.Button("✕", GUILayout.Width(22), GUILayout.Height(22)))
                {
                    favoritePaths.Remove(path);
                    SaveFavorites();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        public static void AddToFavorites(string folderPath)
        {
            var paths = new List<string>(EditorPrefs.GetString("EPU_FavoriteFolders", "").Split('|'));
            if (!paths.Contains(folderPath))
            {
                paths.Add(folderPath);
                EditorPrefs.SetString("EPU_FavoriteFolders", string.Join("|", paths));

                var window = GetWindow<FavoriteFoldersWindow>();
                if (window != null)
                    window.LoadFavorites();
            }
        }

        public static void RemoveFromFavorites(string folderPath)
        {
            var paths = new List<string>(EditorPrefs.GetString("EPU_FavoriteFolders", "").Split('|'));
            if (paths.Contains(folderPath))
            {
                paths.Remove(folderPath);
                EditorPrefs.SetString("EPU_FavoriteFolders", string.Join("|", paths));

                var window = GetWindow<FavoriteFoldersWindow>();
                if (window != null)
                    window.LoadFavorites();
            }
        }

        [MenuItem("Assets/Favorites/Add to Favorites", true)]
        private static bool ValidateAddToFavorites()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) &&
                   !EditorPrefs.GetString("EPU_FavoriteFolders", "").Contains(path);
        }

        [MenuItem("Assets/Favorites/Add to Favorites")]
        private static void AddToFavoritesContext()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            AddToFavorites(path);
        }

        [MenuItem("Assets/Favorites/Remove from Favorites", true)]
        private static bool ValidateRemoveFromFavorites()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            return !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path) &&
                   EditorPrefs.GetString("EPU_FavoriteFolders", "").Contains(path);
        }

        [MenuItem("Assets/Favorites/Remove from Favorites")]
        private static void RemoveFromFavoritesContext()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            RemoveFromFavorites(path);
        }
    }
}