using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle folderItemBoxStyle;
        private static GUIStyle separatorStyle;
        private static GUIStyle statsBoxStyle;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/Favorite Folders",false,24)]
        public static void ShowWindow()
        {
            var window = GetWindow<FavoriteFoldersWindow>("Favorite Folders");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            LoadFavorites();
            
            // Try to load custom icon first, then fallback to Unity built-in icons
            pingIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FavoriteFolder/Icons/UI/ping_icon.png");
            
            if (pingIcon == null)
            {
                // Try Unity's built-in icons as fallback
                pingIcon = EditorGUIUtility.IconContent("d_ViewToolOrbit").image as Texture2D;
                if (pingIcon == null)
                {
                    pingIcon = EditorGUIUtility.IconContent("ViewToolOrbit").image as Texture2D;
                }
                if (pingIcon == null)
                {
                    pingIcon = EditorGUIUtility.IconContent("Search Icon").image as Texture2D;
                }
                
                Debug.Log("FavoriteFoldersWindow: Using Unity built-in icon as fallback for ping icon.");
            }
            else
            {
                Debug.Log("FavoriteFoldersWindow: Custom ping icon loaded successfully.");
            }
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
            // Legacy styles (keeping for compatibility)
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

            if (folderItemBoxStyle == null)
            {
                folderItemBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2),
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

        private void OnGUI()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("⭐ Favorite Folders", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Statistics Overview Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Favorites Statistics", sectionLabelStyle);
            DrawFavoriteStatistics();
            EditorGUILayout.EndVertical();

            // Quick Actions Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("⚡ Quick Actions", sectionLabelStyle);
            EditorGUILayout.Space(5);
            DrawQuickActions();
            EditorGUILayout.EndVertical();

            // Favorite Folders List Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📁 Favorite Folders", sectionLabelStyle);
            EditorGUILayout.Space(5);
            DrawFavoritesList();
            EditorGUILayout.EndVertical();

            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        private void DrawFavoriteStatistics()
        {
            EditorGUILayout.BeginVertical(statsBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Multi-tier responsive breakpoints
            bool isVeryNarrow = position.width < 350;
            bool isNarrow = position.width < 450;

            int totalFavorites = favoritePaths?.Count ?? 0;
            int validFavorites = 0;
            int invalidFavorites = 0;

            if (favoritePaths != null)
            {
                foreach (var path in favoritePaths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                        validFavorites++;
                    else
                        invalidFavorites++;
                }
            }

            // Add icon status indicator
            EditorGUILayout.BeginHorizontal();
            if (pingIcon != null)
            {
                GUILayout.Label(pingIcon, GUILayout.Width(16), GUILayout.Height(16));
                EditorGUILayout.LabelField("✅ Icons Ready", statLabelStyle, GUILayout.Width(80));
            }
            else
            {
                EditorGUILayout.LabelField("📁 Text Mode", statLabelStyle);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            if (isVeryNarrow)
            {
                // Stack all stats vertically for very narrow windows
                EditorGUILayout.LabelField($"📁 Total: {totalFavorites}", statLabelStyle);
                EditorGUILayout.LabelField($"✅ Valid: {validFavorites}", statLabelStyle);
                if (invalidFavorites > 0)
                    EditorGUILayout.LabelField($"❌ Invalid: {invalidFavorites}", statLabelStyle);
            }
            else if (isNarrow)
            {
                // 2x2 grid for narrow windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Total: {totalFavorites}", statLabelStyle);
                EditorGUILayout.LabelField($"✅ Valid: {validFavorites}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                if (invalidFavorites > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"❌ Invalid: {invalidFavorites}", statLabelStyle);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                // Single row for wider windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Total: {totalFavorites}", statLabelStyle);
                EditorGUILayout.LabelField($"✅ Valid: {validFavorites}", statLabelStyle);
                if (invalidFavorites > 0)
                    EditorGUILayout.LabelField($"❌ Invalid: {invalidFavorites}", statLabelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickActions()
        {
            // Multi-tier responsive button layout
            bool isVeryNarrow = position.width < 380;
            bool isNarrow = position.width < 500;
            
            if (isVeryNarrow)
            {
                // Stack buttons vertically for very narrow windows
                if (GradientButton("📂 Add Current Selection", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                    {
                        AddToFavorites(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Selection", "Please select a valid folder in the Project window.", "OK");
                    }
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("🗑️ Clear Invalid", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    CleanupInvalidFavorites();
                }
            }
            else if (isNarrow)
            {
                // Stack buttons vertically with full text for narrow windows
                if (GradientButton("📂 Add Current Selection", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                    {
                        AddToFavorites(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Selection", "Please select a valid folder in the Project window.", "OK");
                    }
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("🗑️ Clear Invalid Favorites", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    CleanupInvalidFavorites();
                }
            }
            else
            {
                // Horizontal layout for wider windows
                EditorGUILayout.BeginHorizontal();
                
                if (GradientButton("📂 Add Current Selection", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                    {
                        AddToFavorites(path);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Invalid Selection", "Please select a valid folder in the Project window.", "OK");
                    }
                }
                
                GUILayout.Space(10);
                
                if (GradientButton("🗑️ Clear Invalid Favorites", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    CleanupInvalidFavorites();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFavoritesList()
        {
            if (favoritePaths == null || favoritePaths.Count == 0)
            {
                EditorGUILayout.HelpBox("⭐ No folders added to favorites yet.\n\nTo add folders:\n• Select a folder in Project window\n• Use 'Add Current Selection' button above\n• Or right-click → Favorites → Add to Favorites", MessageType.Info);
                return;
            }

            foreach (var path in favoritePaths.ToArray())
            {
                EditorGUILayout.BeginVertical(folderItemBoxStyle);
                
                // Check if folder still exists
                bool isValid = AssetDatabase.IsValidFolder(path);
                
                // Responsive layout for folder items with proper button visibility
                bool isVeryNarrow = position.width < 380;
                bool isNarrow = position.width < 550;
                
                EditorGUILayout.BeginHorizontal();
                
                // Status icon - always visible
                EditorGUILayout.LabelField(isValid ? "📁" : "❌", GUILayout.Width(25));
                
                // Path label with smart truncation
                string displayPath = path;
                if (isVeryNarrow && path.Length > 25)
                {
                    displayPath = "..." + path.Substring(path.Length - 22);
                }
                else if (isNarrow && path.Length > 35)
                {
                    displayPath = "..." + path.Substring(path.Length - 32);
                }
                else if (path.Length > 50)
                {
                    displayPath = "..." + path.Substring(path.Length - 47);
                }
                
                // Path label with flexible width
                GUIStyle currentPathStyle = isValid ? pathLabelStyle : GetInvalidPathStyle();
                EditorGUILayout.LabelField(displayPath, currentPathStyle);
                
                GUILayout.FlexibleSpace();
                
                // Buttons section - always on the right
                if (isVeryNarrow)
                {
                    // Very narrow: Use ping icon if available, fallback to emoji
                    GUIContent pingContent = pingIcon != null ? new GUIContent(pingIcon, "Ping folder in Project window") : new GUIContent("📍", "Ping folder in Project window");
                    if (GUILayout.Button(pingContent, GUILayout.Width(28), GUILayout.Height(22)))
                    {
                        if (isValid)
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                            if (obj != null) 
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Path", $"Folder no longer exists:\n{path}", "OK");
                        }
                    }
                    
                    if (GUILayout.Button(new GUIContent("✕", "Remove from favorites"), GUILayout.Width(28), GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Favorite", $"Remove this folder from favorites?\n\n{path}", "Remove", "Cancel"))
                        {
                            favoritePaths.Remove(path);
                            SaveFavorites();
                        }
                    }
                }
                else if (isNarrow)
                {
                    // Narrow: Short text buttons with icon if available
                    GUIContent pingContent = pingIcon != null ? new GUIContent(pingIcon, "Ping folder") : new GUIContent("Ping", "Ping folder in Project window");
                    if (GUILayout.Button(pingContent, GUILayout.Width(45), GUILayout.Height(22)))
                    {
                        if (isValid)
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                            if (obj != null) 
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Path", $"Folder no longer exists:\n{path}", "OK");
                        }
                    }
                    
                    if (GUILayout.Button(new GUIContent("Remove", "Remove from favorites"), GUILayout.Width(60), GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Favorite", $"Remove this folder from favorites?\n\n{path}", "Remove", "Cancel"))
                        {
                            favoritePaths.Remove(path);
                            SaveFavorites();
                        }
                    }
                }
                else
                {
                    // Wide: Full text buttons with icon
                    string pingText = pingIcon != null ? "Ping" : "📍 Ping";
                    GUIContent pingContent = pingIcon != null ? new GUIContent(pingText, pingIcon, "Ping folder in Project window") : new GUIContent(pingText, "Ping folder in Project window");
                    if (GUILayout.Button(pingContent, GUILayout.Width(70), GUILayout.Height(22)))
                    {
                        if (isValid)
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                            if (obj != null) 
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Invalid Path", $"Folder no longer exists:\n{path}", "OK");
                        }
                    }
                    
                    if (GUILayout.Button(new GUIContent("✕ Remove", "Remove from favorites"), GUILayout.Width(80), GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("Remove Favorite", $"Remove this folder from favorites?\n\n{path}", "Remove", "Cancel"))
                        {
                            favoritePaths.Remove(path);
                            SaveFavorites();
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
        }

        private GUIStyle GetInvalidPathStyle()
        {
            return new GUIStyle(pathLabelStyle)
            {
                normal = { textColor = Color.red }
            };
        }

        private void CleanupInvalidFavorites()
        {
            if (favoritePaths == null) return;
            
            var validPaths = new List<string>();
            foreach (var path in favoritePaths)
            {
                if (AssetDatabase.IsValidFolder(path))
                    validPaths.Add(path);
            }
            
            int removedCount = favoritePaths.Count - validPaths.Count;
            favoritePaths = validPaths;
            SaveFavorites();
            
            if (removedCount > 0)
            {
                EditorUtility.DisplayDialog("Cleanup Complete", $"Removed {removedCount} invalid favorite folder(s).", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Cleanup Needed", "All favorite folders are valid.", "OK");
            }
        }

        public static void AddToFavorites(string folderPath)
        {
            var paths = new List<string>(EditorPrefs.GetString("EPU_FavoriteFolders", "").Split('|'));
            if (!paths.Contains(folderPath))
            {
                paths.Add(folderPath);
                EditorPrefs.SetString("EPU_FavoriteFolders", string.Join("|", paths));

                // Silently refresh any open windows without affecting focus or bringing them forward
                RefreshOpenWindows();
            }
        }

        public static void RemoveFromFavorites(string folderPath)
        {
            var paths = new List<string>(EditorPrefs.GetString("EPU_FavoriteFolders", "").Split('|'));
            if (paths.Contains(folderPath))
            {
                paths.Remove(folderPath);
                EditorPrefs.SetString("EPU_FavoriteFolders", string.Join("|", paths));

                // Silently refresh any open windows without affecting focus or bringing them forward
                RefreshOpenWindows();
            }
        }

        private static void RefreshOpenWindows()
        {
            // Find all open instances of FavoriteFoldersWindow without affecting focus
            var openWindows = Resources.FindObjectsOfTypeAll<FavoriteFoldersWindow>();
            foreach (var window in openWindows)
            {
                if (window != null)
                {
                    window.LoadFavorites();
                }
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
