using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DevToolkit_Suite
{
    public class AutoFolderCreatorWindow : EditorWindow
    {
        private Vector2 scroll;
        private Dictionary<string, Texture2D> folderIcons;
        private Dictionary<string, bool> folderToggles;
        private GUIStyle labelStyle;
        private GUIStyle boxStyle;

        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle folderTileBoxStyle;
        private static GUIStyle separatorStyle;
        private static GUIStyle statsBoxStyle;
        private Vector2 scrollPosition = Vector2.zero;

        private void OnEnable()
        {
            LoadIcons();
        }

        [MenuItem("Tools/DevToolkit Suite/Auto Folder Creator",false,23)]
        public static void ShowWindow()
        {
            var window = GetWindow<AutoFolderCreatorWindow>("Auto Folder Creator");
            window.minSize = new Vector2(360, 480); // Optimal size for 2-column layout
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
            // Legacy styles (keeping for compatibility)
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

            if (folderTileBoxStyle == null)
            {
                folderTileBoxStyle = new GUIStyle("box")
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

        private void OnGUI()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 50);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("📁 Auto Folder Creator", headerLabelStyle);
            EditorGUILayout.Space(8);

            // Begin scroll view for all content with better spacing
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(5);

            // Statistics Overview Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📊 Project Statistics", sectionLabelStyle);
            DrawProjectStatistics();
            EditorGUILayout.EndVertical();

            // Folder Selection Section  
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📂 Available Folder Templates", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawResponsiveFolderGrid();
            EditorGUILayout.EndVertical();

            // Action Buttons Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎮 Actions", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawResponsiveActionButtons();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10); // Bottom padding

            // End scroll view
            EditorGUILayout.EndScrollView();
        }

        private void DrawProjectStatistics()
        {
            EditorGUILayout.BeginVertical(statsBoxStyle);
            
            GUIStyle statLabelStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                margin = new RectOffset(0, 10, 0, 0)
            };

            // Calculate statistics
            int totalTemplates = folderIcons?.Count ?? 0;
            int selectedTemplates = folderToggles?.Values.Count(x => x) ?? 0;
            int existingFolders = CountExistingFolders();
            int availableFolders = totalTemplates - existingFolders;

            // Better responsive layout with proper space utilization
            float windowWidth = position.width;
            
            if (windowWidth < 400)
            {
                // Very narrow: Single column, compact text
                EditorGUILayout.LabelField($"📁 {totalTemplates} | ✅ {selectedTemplates} | 📂 {existingFolders} | 🆕 {availableFolders}", statLabelStyle);
            }
            else if (windowWidth < 550)
            {
                // Narrow: 2x2 grid layout
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Templates: {totalTemplates}", statLabelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField($"✅ Selected: {selectedTemplates}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📂 Existing: {existingFolders}", statLabelStyle, GUILayout.Width(120));
                EditorGUILayout.LabelField($"🆕 Available: {availableFolders}", statLabelStyle);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Wide: Single row with even distribution
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📁 Templates: {totalTemplates}", statLabelStyle, GUILayout.Width(110));
                EditorGUILayout.LabelField($"✅ Selected: {selectedTemplates}", statLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"📂 Existing: {existingFolders}", statLabelStyle, GUILayout.Width(100));
                EditorGUILayout.LabelField($"🆕 Available: {availableFolders}", statLabelStyle, GUILayout.Width(110));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private int CountExistingFolders()
        {
            if (folderIcons == null) return 0;
            
            int count = 0;
            foreach (var key in folderIcons.Keys)
            {
                if (AssetDatabase.IsValidFolder($"Assets/{key}"))
                    count++;
            }
            return count;
        }

        private void DrawResponsiveFolderGrid()
        {
            if (folderIcons == null || folderIcons.Count == 0)
            {
                EditorGUILayout.HelpBox("📂 No folder templates found.\n\nMake sure the CustomFolderIcons directory contains valid icon files.", MessageType.Info);
                return;
            }

            // Calculate available width
            float windowWidth = position.width;
            float boxPadding = 30; // modernBoxStyle padding
            float scrollbarSpace = 16; // Scrollbar estimate
            float availableWidth = windowWidth - boxPadding - scrollbarSpace;
            
            // Fixed tile size approach - maintain consistent, readable size
            int tileSize = 90; // Fixed tile size - good balance of visibility and space efficiency
            int spacing = 8; // Consistent spacing
            
            // Calculate how many columns fit with the fixed tile size
            int columns = Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (tileSize + spacing)));
            
            // Limit maximum columns for better aesthetics and usability
            columns = Mathf.Min(columns, 6);
            
            // Ensure we have at least 2 columns if space allows
            if (columns == 1 && availableWidth >= (2 * tileSize + spacing))
            {
                columns = 2;
            }

            List<string> keys = new List<string>(folderIcons.Keys);
            int total = keys.Count;

            // Grid container
            EditorGUILayout.BeginVertical();
            
            // Draw grid without centering to avoid layout issues
            for (int i = 0; i < total; i += columns)
            {
                EditorGUILayout.BeginHorizontal();
                
                for (int j = 0; j < columns; j++)
                {
                    int index = i + j;
                    if (index >= total) break;

                    DrawModernFolderTile(keys[index], tileSize);
                    
                    // Add spacing between tiles (except after last tile in row)
                    if (j < columns - 1 && index + 1 < total)
                    {
                        GUILayout.Space(spacing);
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Add vertical spacing between rows
                if (i + columns < total)
                {
                    EditorGUILayout.Space(spacing);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawModernFolderTile(string key, int tileSize)
        {
            bool isSelected = folderToggles.ContainsKey(key) && folderToggles[key];
            bool folderExists = AssetDatabase.IsValidFolder($"Assets/{key}");
            
            // Enhanced visual styling based on state with better contrast
            Color bgColor;
            Color borderColor;
            Color hoverColor;
            
            if (folderExists)
            {
                bgColor = new Color(0.1f, 0.3f, 0.1f, 0.8f); // Stronger green for existing
                borderColor = new Color(0.3f, 0.7f, 0.3f);
                hoverColor = new Color(0.15f, 0.4f, 0.15f, 0.9f);
            }
            else if (isSelected)
            {
                bgColor = new Color(0.1f, 0.4f, 0.8f, 0.8f); // Stronger blue for selected
                borderColor = new Color(0.3f, 0.6f, 1f);
                hoverColor = new Color(0.15f, 0.5f, 0.9f, 0.9f);
            }
            else
            {
                bgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Darker background
                borderColor = new Color(0.4f, 0.4f, 0.4f);
                hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            }

            // Create a container with proper spacing and fixed width
            int labelHeight = tileSize < 75 ? 14 : (tileSize < 90 ? 16 : 18);
            int totalHeight = tileSize + labelHeight + 4;
            
            EditorGUILayout.BeginVertical(GUILayout.Width(tileSize), GUILayout.Height(totalHeight), GUILayout.ExpandWidth(false));
            
            // Main tile area with exact size
            Rect tileRect = GUILayoutUtility.GetRect(tileSize, tileSize, GUILayout.ExpandWidth(false), GUILayout.ExpandHeight(false));
            
            // Add hover effect
            bool isHovering = tileRect.Contains(Event.current.mousePosition);
            Color currentBgColor = isHovering && !folderExists ? hoverColor : bgColor;
            
            // Draw background with border
            EditorGUI.DrawRect(tileRect, borderColor);
            
            // Inner rect for actual tile
            Rect innerRect = new Rect(tileRect.x + 1, tileRect.y + 1, tileRect.width - 2, tileRect.height - 2);
            EditorGUI.DrawRect(innerRect, currentBgColor);
            
            // Draw icon with better sizing
            if (folderIcons.ContainsKey(key) && folderIcons[key] != null)
            {
                float iconSize = tileSize * 0.5f; // Better proportions
                Rect iconRect = new Rect(
                    tileRect.x + (tileRect.width - iconSize) / 2f,
                    tileRect.y + (tileRect.height - iconSize) / 2f,
                    iconSize,
                    iconSize
                );
                GUI.DrawTexture(iconRect, folderIcons[key], ScaleMode.ScaleToFit);
            }
            
            // Enhanced status overlay with better positioning
            float overlaySize = tileSize < 75 ? 10 : 12;
            if (folderExists)
            {
                Rect overlayRect = new Rect(tileRect.x + tileRect.width - overlaySize - 2, tileRect.y + 2, overlaySize, overlaySize);
                EditorGUI.DrawRect(overlayRect, new Color(0.1f, 0.7f, 0.1f, 0.95f));
                GUI.Label(overlayRect, "✓", new GUIStyle { 
                    fontSize = overlaySize < 11 ? 7 : 8, 
                    normal = { textColor = Color.white }, 
                    alignment = TextAnchor.MiddleCenter 
                });
            }
            else if (isSelected)
            {
                Rect overlayRect = new Rect(tileRect.x + tileRect.width - overlaySize - 2, tileRect.y + 2, overlaySize, overlaySize);
                EditorGUI.DrawRect(overlayRect, new Color(0.1f, 0.4f, 0.8f, 0.95f));
                GUI.Label(overlayRect, "●", new GUIStyle { 
                    fontSize = overlaySize < 11 ? 7 : 8, 
                    normal = { textColor = Color.white }, 
                    alignment = TextAnchor.MiddleCenter 
                });
            }
            
            // Handle click with better feedback
            if (GUI.Button(tileRect, GUIContent.none, GUIStyle.none))
            {
                if (!folderExists)
                {
                    folderToggles[key] = !folderToggles[key];
                    GUI.FocusControl(null);
                    Repaint(); // Force repaint to show selection change immediately
                }
            }
            
            // Enhanced label with better responsive sizing
            GUIStyle tileLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                wordWrap = true,
                fontSize = tileSize < 70 ? 7 : (tileSize < 85 ? 8 : 9),
                fontStyle = FontStyle.Bold,
                normal = { 
                    textColor = folderExists ? new Color(0.6f, 1f, 0.6f) : 
                               (isSelected ? new Color(0.6f, 0.8f, 1f) : 
                               new Color(0.9f, 0.9f, 0.9f))
                }
            };
            
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField(key, tileLabelStyle, GUILayout.Height(labelHeight), GUILayout.Width(tileSize));
            EditorGUILayout.EndVertical();
        }

        private void DrawResponsiveActionButtons()
        {
            // Quick selection buttons with better spacing
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("⚡ Quick Selection:", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            
            // More refined responsive button layout
            bool isVeryNarrow = position.width < 370;
            bool isNarrow = position.width < 500;
            
            if (isVeryNarrow)
            {
                // Stack buttons vertically for very narrow windows
                if (GradientButton("✅ Select All", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    SelectAll();
                }
                
                EditorGUILayout.Space(3);
                
                if (GradientButton("❌ Clear All", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    ClearAll();
                }
            }
            else if (isNarrow)
            {
                // Horizontal layout for narrow windows
                EditorGUILayout.BeginHorizontal();
                if (GradientButton("✅ Select All", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    SelectAll();
                }
                
                GUILayout.Space(5);
                
                if (GradientButton("❌ Clear All", gradientTex, gradientButtonStyle, GUILayout.Height(28)))
                {
                    ClearAll();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // Full horizontal layout for wider windows
                EditorGUILayout.BeginHorizontal();
                if (GradientButton("✅ Select All Available", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    SelectAll();
                }
                
                GUILayout.Space(10);
                
                if (GradientButton("❌ Clear Selection", gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                {
                    ClearAll();
                }
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
            
            // Enhanced separator
            EditorGUILayout.Space(10);
            GUILayout.Box("", separatorStyle, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(10);
            
            // Main action section with better layout
            int selectedCount = folderToggles?.Values.Count(x => x) ?? 0;
            int availableCount = folderToggles?.Keys.Where(k => !AssetDatabase.IsValidFolder($"Assets/{k}")).Count() ?? 0;
            
            EditorGUILayout.BeginVertical();
            
            // Show selection summary
            if (availableCount > 0)
            {
                string summaryText = isVeryNarrow ? 
                    $"{selectedCount}/{availableCount} selected" : 
                    $"📊 {selectedCount} of {availableCount} available folders selected";
                EditorGUILayout.LabelField(summaryText, EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(5);
            }
            
            if (selectedCount > 0)
            {
                string buttonText = isVeryNarrow ? 
                    $"📁 Create ({selectedCount})" : 
                    $"📁 Create {selectedCount} Folder{(selectedCount > 1 ? "s" : "")}";
                    
                if (GradientButton(buttonText, gradientTex, gradientButtonStyle, GUILayout.Height(36)))
                {
                    CreateFolders();
                }
            }
            else
            {
                if (availableCount == 0)
                {
                    EditorGUILayout.HelpBox("✅ All folder templates already exist in your project!", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("🎯 Select folder templates above to create them in your project.", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void SelectAll()
        {
            if (folderToggles == null) return;
            
            var keys = folderToggles.Keys.ToList();
            foreach (var key in keys)
            {
                // Only select folders that don't already exist
                if (!AssetDatabase.IsValidFolder($"Assets/{key}"))
                {
                    folderToggles[key] = true;
                }
            }
        }

        private void ClearAll()
        {
            if (folderToggles == null) return;
            
            var keys = folderToggles.Keys.ToList();
            foreach (var key in keys)
            {
                folderToggles[key] = false;
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

        // Modern UI utility methods
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
