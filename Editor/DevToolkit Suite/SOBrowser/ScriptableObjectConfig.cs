using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DevToolkit_Suite
{
    public class ScriptableObjectConfig : EditorWindow
    {
        private List<string> ignoredNamespaces = new List<string>();
        private string newLineContent;
        private ScriptableObjectBrowserWindow parentWindow;
        
        // Additional configuration options
        private bool showInstanceCounts = true;
        private bool autoRefreshOnFocus = true;
        private bool showFullNamespaces = false;
        private bool enableUsageScanning = true;
        private int maxPreviewHeight = 300;
        
        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle configBoxStyle;
        private static GUIStyle separatorStyle;
        private Vector2 scrollPosition = Vector2.zero;

        public static void ShowWindow(ScriptableObjectBrowserWindow parent)
        {
            var win = GetWindow<ScriptableObjectConfig>("SO Browser Configuration");
            win.parentWindow = parent;
            win.minSize = new Vector2(450, 600);
        }

        private void OnEnable()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            // Load ignored namespaces
            ignoredNamespaces = new List<string>
            {
                "UnityEngine",
                "UnityEditor", 
                "TMPro",
                "Cinemachine"
            };

            newLineContent = string.Join("\n", ignoredNamespaces);
            
            // Load other settings from EditorPrefs
            showInstanceCounts = EditorPrefs.GetBool("SOBrowser_ShowInstanceCounts", true);
            autoRefreshOnFocus = EditorPrefs.GetBool("SOBrowser_AutoRefreshOnFocus", true);
            showFullNamespaces = EditorPrefs.GetBool("SOBrowser_ShowFullNamespaces", false);
            enableUsageScanning = EditorPrefs.GetBool("SOBrowser_EnableUsageScanning", true);
            maxPreviewHeight = EditorPrefs.GetInt("SOBrowser_MaxPreviewHeight", 300);
        }

        private void SaveConfiguration()
        {
            // Parse and save ignored namespaces
            ignoredNamespaces = newLineContent.Split('\n')
                .Select(n => n.Trim())
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();

            // Save settings to EditorPrefs
            EditorPrefs.SetBool("SOBrowser_ShowInstanceCounts", showInstanceCounts);
            EditorPrefs.SetBool("SOBrowser_AutoRefreshOnFocus", autoRefreshOnFocus);
            EditorPrefs.SetBool("SOBrowser_ShowFullNamespaces", showFullNamespaces);
            EditorPrefs.SetBool("SOBrowser_EnableUsageScanning", enableUsageScanning);
            EditorPrefs.SetInt("SOBrowser_MaxPreviewHeight", maxPreviewHeight);

            // Apply to parent window
            parentWindow?.SetIgnoredNamespaces(ignoredNamespaces);
            
            // Force parent window to repaint to reflect changes
            if (parentWindow != null)
            {
                parentWindow.Repaint();
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
            EditorGUILayout.LabelField("⚙️ ScriptableObject Browser Configuration", headerLabelStyle);
            EditorGUILayout.Space(8);

            // Begin scroll view for all content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(5);

            // Display Options Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎨 Display Options", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawDisplayOptions();
            EditorGUILayout.EndVertical();

            // Behavior Settings Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("⚡ Behavior Settings", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawBehaviorSettings();
            EditorGUILayout.EndVertical();

            // Namespace Filtering Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔍 Namespace Filtering", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawNamespaceFiltering();
            EditorGUILayout.EndVertical();

            // Preview Settings Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("👁️ Preview Settings", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawPreviewSettings();
            EditorGUILayout.EndVertical();

            // Action Buttons Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("💾 Actions", sectionLabelStyle);
            EditorGUILayout.Space(3);
            DrawActionButtons();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10); // Bottom padding
            EditorGUILayout.EndScrollView();
        }

        private void DrawDisplayOptions()
        {
            EditorGUILayout.BeginVertical(configBoxStyle);
            
            // Show instance counts toggle
            EditorGUILayout.BeginHorizontal();
            showInstanceCounts = EditorGUILayout.Toggle(showInstanceCounts, GUILayout.Width(20));
            EditorGUILayout.LabelField("📊 Show instance counts in type list", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            if (!showInstanceCounts)
            {
                EditorGUILayout.HelpBox("Instance counts will be hidden from the type list", MessageType.Info);
            }

            EditorGUILayout.Space(5);

            // Show full namespaces toggle
            EditorGUILayout.BeginHorizontal();
            showFullNamespaces = EditorGUILayout.Toggle(showFullNamespaces, GUILayout.Width(20));
            EditorGUILayout.LabelField("🏷️ Show full namespace in type names", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            if (showFullNamespaces)
            {
                EditorGUILayout.HelpBox("Type names will show as 'Namespace.ClassName' instead of just 'ClassName'", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBehaviorSettings()
        {
            EditorGUILayout.BeginVertical(configBoxStyle);
            
            // Auto refresh toggle
            EditorGUILayout.BeginHorizontal();
            autoRefreshOnFocus = EditorGUILayout.Toggle(autoRefreshOnFocus, GUILayout.Width(20));
            EditorGUILayout.LabelField("🔄 Auto-refresh when window gains focus", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            if (!autoRefreshOnFocus)
            {
                EditorGUILayout.HelpBox("You'll need to manually refresh to see new ScriptableObjects", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // Usage scanning toggle
            EditorGUILayout.BeginHorizontal();
            enableUsageScanning = EditorGUILayout.Toggle(enableUsageScanning, GUILayout.Width(20));
            EditorGUILayout.LabelField("🧩 Enable scene usage scanning", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            if (!enableUsageScanning)
            {
                EditorGUILayout.HelpBox("Scene usage scanning will be disabled for better performance", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawNamespaceFiltering()
        {
            EditorGUILayout.BeginVertical(configBoxStyle);
            
            EditorGUILayout.LabelField("🚫 Ignored Namespaces", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("ScriptableObjects from these namespaces will be hidden:", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);
            
            newLineContent = EditorGUILayout.TextArea(newLineContent, GUILayout.Height(120));
            
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📝 Reset to Defaults", GUILayout.Height(25)))
            {
                newLineContent = "UnityEngine\nUnityEditor\nTMPro\nCinemachine";
            }
            
            if (GUILayout.Button("🗑️ Clear All", GUILayout.Height(25)))
            {
                newLineContent = "";
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox("💡 Tip: One namespace per line. Use this to hide Unity's built-in ScriptableObjects and focus on your project's objects.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawPreviewSettings()
        {
            EditorGUILayout.BeginVertical(configBoxStyle);
            
            EditorGUILayout.LabelField("📏 Maximum Preview Height:", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.BeginHorizontal();
            maxPreviewHeight = EditorGUILayout.IntSlider(maxPreviewHeight, 150, 600);
            EditorGUILayout.LabelField("px", GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox($"Preview area will be limited to {maxPreviewHeight}px height for better performance", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(configBoxStyle);
            
            // Save and Apply buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GradientButton("💾 Save & Apply", gradientTex, gradientButtonStyle, GUILayout.Height(35)))
            {
                SaveConfiguration();
                EditorUtility.DisplayDialog("Configuration Saved", "Settings have been saved and applied to the ScriptableObject Browser.", "OK");
                Close();
            }
            
            GUILayout.Space(10);
            
            if (GradientButton("❌ Cancel", gradientTex, gradientButtonStyle, GUILayout.Height(35)))
            {
                Close();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(8);
            
            // Additional actions
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🔄 Reset All to Defaults", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Reset Configuration", 
                    "Are you sure you want to reset all settings to their default values?", 
                    "Reset", "Cancel"))
                {
                    ResetToDefaults();
                }
            }
            
            if (GUILayout.Button("📋 Export Settings", GUILayout.Height(28)))
            {
                ExportSettings();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }

        private void ResetToDefaults()
        {
            showInstanceCounts = true;
            autoRefreshOnFocus = true;
            showFullNamespaces = false;
            enableUsageScanning = true;
            maxPreviewHeight = 300;
            newLineContent = "UnityEngine\nUnityEditor\nTMPro\nCinemachine";
        }

        private void ExportSettings()
        {
            string settings = $@"# ScriptableObject Browser Configuration
# Generated on {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}

ShowInstanceCounts: {showInstanceCounts}
AutoRefreshOnFocus: {autoRefreshOnFocus}
ShowFullNamespaces: {showFullNamespaces}
EnableUsageScanning: {enableUsageScanning}
MaxPreviewHeight: {maxPreviewHeight}

IgnoredNamespaces:
{newLineContent}";

            string path = EditorUtility.SaveFilePanel("Export SO Browser Settings", "", "SOBrowserConfig.txt", "txt");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, settings);
                EditorUtility.DisplayDialog("Export Complete", $"Settings exported to:\n{path}", "OK");
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

            if (configBoxStyle == null)
            {
                configBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 10, 10),
                    margin = new RectOffset(2, 2, 2, 2),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
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

        private bool GradientButton(string text, Texture2D hoverTex, GUIStyle style, params GUILayoutOption[] options)
        {
            GUIContent content = new GUIContent(text);
            
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
