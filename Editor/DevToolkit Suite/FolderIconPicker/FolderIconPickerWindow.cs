using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace DevToolkit_Suite
{
    public class FolderIconPickerWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private Dictionary<string, Texture2D> colorIcons;
        private Dictionary<string, Texture2D> customIcons;

        private Texture2D headerIcon;
        private Texture2D customIcon;
        private Texture2D colorIcon;

        private Object selectedFolder;

        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle iconGridBoxStyle;
        private static GUIStyle separatorStyle;
        private static GUIStyle folderBoxStyle;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/Folder Icon Picker",false,25)]
        public static void ShowWindow()
        {
            var window = GetWindow<FolderIconPickerWindow>("Folder Icon Picker");
            window.minSize = new Vector2(320, 400);
        }

        private void OnEnable()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            FolderIconManager.BuildIconDictionaries(out colorIcons, out customIcons);

            headerIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/custom_icon.png");
            customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/custom_icon.png");
            colorIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/FolderIconPicker/Icons/UI/color_icon.png");
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
            EditorGUILayout.LabelField("📁 Folder Icon Picker", headerLabelStyle);
            EditorGUILayout.Space(10);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Statistics Overview Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Icon Library", sectionLabelStyle);
            DrawIconStatistics();
            EditorGUILayout.EndVertical();

            // Folder Selection Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📂 Target Folder", sectionLabelStyle);
            EditorGUILayout.Space(5);
            DrawFolderSelection();
            EditorGUILayout.EndVertical();

            // Icon Selection Section
            DrawResponsiveIconGrids();

            // Action Buttons Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎮 Actions", sectionLabelStyle);
            EditorGUILayout.Space(5);
            DrawResponsiveActionButtons();
            EditorGUILayout.EndVertical();

            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        private void DrawIconStatistics()
        {
            EditorGUILayout.BeginVertical(folderBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            // Multi-tier responsive breakpoints
            bool isVeryNarrow = position.width < 350;
            bool isNarrow = position.width < 450;
            bool isMedium = position.width < 600;

            int customCount = customIcons?.Count ?? 0;
            int colorCount = colorIcons?.Count ?? 0;
            int totalCount = customCount + colorCount;

            if (isVeryNarrow)
            {
                // Stack all stats vertically for very narrow windows
                EditorGUILayout.LabelField($"🎨 Custom: {customCount}", statLabelStyle);
                EditorGUILayout.LabelField($"🌈 Color: {colorCount}", statLabelStyle);
                EditorGUILayout.LabelField($"📦 Total: {totalCount}", statLabelStyle);
            }
            else if (isNarrow)
            {
                // 2x2 grid for narrow windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🎨 Custom: {customCount}", statLabelStyle);
                EditorGUILayout.LabelField($"🌈 Color: {colorCount}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📦 Total: {totalCount}", statLabelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Single row for wider windows
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🎨 Custom: {customCount}", statLabelStyle);
                EditorGUILayout.LabelField($"🌈 Color: {colorCount}", statLabelStyle);
                EditorGUILayout.LabelField($"📦 Total: {totalCount}", statLabelStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFolderSelection()
        {
            EditorGUILayout.BeginVertical(folderBoxStyle);
            
            // Responsive folder selection layout
            bool isVeryNarrow = position.width < 350;
            
            if (isVeryNarrow)
            {
                EditorGUILayout.LabelField("📁 Select folder to customize:", EditorStyles.miniLabel);
                EditorGUILayout.Space(3);
                selectedFolder = EditorGUILayout.ObjectField("", selectedFolder, typeof(DefaultAsset), false, GUILayout.Height(22));
            }
            else
            {
                EditorGUILayout.LabelField("📁 Drop or select a folder to customize its icon:", EditorStyles.miniLabel);
                EditorGUILayout.Space(3);
                selectedFolder = EditorGUILayout.ObjectField("Target Folder", selectedFolder, typeof(DefaultAsset), false, GUILayout.Height(22));
            }
            
            if (selectedFolder != null)
            {
                EditorGUILayout.Space(3);
                string folderPath = AssetDatabase.GetAssetPath(selectedFolder);
                EditorGUILayout.LabelField($"📂 {folderPath}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawResponsiveIconGrids()
        {
            // Always stack sections vertically (one under the other)
            DrawResponsiveGridSection("🎨 Custom Icons", customIcons);
            DrawResponsiveGridSection("🌈 Color Icons", colorIcons);
        }

        private void DrawResponsiveGridSection(string title, Dictionary<string, Texture2D> iconDict)
        {
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField(title, sectionLabelStyle);
            EditorGUILayout.Space(5);

            if (iconDict == null || iconDict.Count == 0)
            {
                EditorGUILayout.HelpBox("📂 No icons found in this category.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginVertical(iconGridBoxStyle);

            // Dynamic grid calculation to maximize space utilization
            float availableWidth = position.width - 60; // Account for margins and padding
            
            int iconSize;
            int itemWidth;
            int spacing = 5; // Space between items
            
            // Determine optimal icon size based on window width
            if (availableWidth < 400)
            {
                iconSize = 48;
                itemWidth = 70;
            }
            else if (availableWidth < 600)
            {
                iconSize = 56;
                itemWidth = 80;
            }
            else
            {
                iconSize = 64;
                itemWidth = 90;
            }
            
            // Calculate maximum columns that can fit
            int columns = Mathf.Max(2, Mathf.FloorToInt((availableWidth + spacing) / (itemWidth + spacing)));
            
            // Ensure we don't exceed reasonable limits
            columns = Mathf.Min(columns, 8); // Max 8 columns for very wide windows

            int count = 0;

            foreach (var kvp in iconDict)
            {
                if (count % columns == 0) EditorGUILayout.BeginHorizontal();

                EditorGUILayout.BeginVertical(GUILayout.Width(itemWidth));
                
                // Icon button with adaptive sizing
                GUIStyle iconButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    padding = new RectOffset(2, 2, 2, 2)
                };
                
                bool buttonPressed = false;
                if (kvp.Key == "Default")
                {
                    buttonPressed = GUILayout.Button(kvp.Value, iconButtonStyle, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }
                else
                {
                    buttonPressed = GUILayout.Button(kvp.Value, iconButtonStyle, GUILayout.Width(iconSize), GUILayout.Height(iconSize));
                }

                if (buttonPressed)
                {
                    if (selectedFolder != null)
                    {
                        if (kvp.Key == "Default")
                        {
                            FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                        }
                        else
                        {
                            FolderIconManager.AssignFolderIcon(AssetDatabase.GetAssetPath(selectedFolder), kvp.Key);
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Folder Selected", "Please select a folder first before choosing an icon.", "OK");
                    }
                }
                
                // Icon name label with adaptive sizing
                GUIStyle iconLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    fontSize = (availableWidth < 400) ? 9 : 10
                };
                
                int labelHeight = (availableWidth < 400) ? 16 : 20;
                GUILayout.Label(kvp.Key, iconLabelStyle, GUILayout.Width(itemWidth), GUILayout.Height(labelHeight));
                EditorGUILayout.EndVertical();

                count++;
                if (count % columns == 0) EditorGUILayout.EndHorizontal();
            }
            
            // Close last row if needed
            if (count % columns != 0) 
            {
                // Add flexible space to fill remaining columns
                while (count % columns != 0)
                {
                    GUILayout.Space(itemWidth);
                    count++;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        private void DrawResponsiveActionButtons()
        {
            // Multi-tier responsive button layout
            bool isVeryNarrow = position.width < 380;
            bool isNarrow = position.width < 500;
            
            if (isVeryNarrow)
            {
                // Stack buttons vertically with full width for very narrow windows
                if (GradientButton("🔄 Reset Selected", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    if (selectedFolder != null)
                    {
                        FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Selection", "Please select a folder first.", "OK");
                    }
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("🗑️ Reset All", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Reset All Icons", "Are you sure you want to reset all folder icons?", "Yes", "Cancel"))
                    {
                        FolderIconManager.ClearAllIcons();
                    }
                }
            }
            else if (isNarrow)
            {
                // Stack buttons vertically with normal text for narrow windows
                if (GradientButton("🔄 Reset Selected Icon", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    if (selectedFolder != null)
                    {
                        FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Selection", "Please select a folder first.", "OK");
                    }
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("🗑️ Reset All Icons", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Reset All Icons", "Are you sure you want to reset all folder icons?", "Yes", "Cancel"))
                    {
                        FolderIconManager.ClearAllIcons();
                    }
                }
            }
            else
            {
                // Horizontal layout for wider windows
                EditorGUILayout.BeginHorizontal();
                
                if (GradientButton("🔄 Reset Selected Icon", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    if (selectedFolder != null)
                    {
                        FolderIconManager.ClearFolderIcon(AssetDatabase.GetAssetPath(selectedFolder));
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("No Selection", "Please select a folder first.", "OK");
                    }
                }
                
                GUILayout.Space(10);
                
                if (GradientButton("🗑️ Reset All Icons", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Reset All Icons", "Are you sure you want to reset all folder icons?", "Yes", "Cancel"))
                    {
                        FolderIconManager.ClearAllIcons();
                    }
                }
                
                EditorGUILayout.EndHorizontal();
            }
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

            if (iconGridBoxStyle == null)
            {
                iconGridBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin = new RectOffset(5, 5, 5, 8),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (folderBoxStyle == null)
            {
                folderBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 8, 8),
                    margin = new RectOffset(5, 5, 5, 8),
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
