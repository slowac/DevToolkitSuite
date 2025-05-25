using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace DevToolkit_Suite
{
    public class EditorWelcomeWindow : EditorWindow
    {
        private const string SessionKey = "DevToolkitSuite_WelcomeShown";
        
        // Tab management
        private enum Tab { Overview, GetStarted, WhatsNew, Feedback }
        private Tab currentTab = Tab.Overview;
        
        // Modern UI styling
        private GUIStyle gradientButtonStyle;
        private Texture2D gradientTex;
        private static GUIStyle headerLabelStyle;
        private static GUIStyle sectionLabelStyle;
        private static GUIStyle modernBoxStyle;
        private static GUIStyle tabButtonStyle;
        private static GUIStyle tabActiveButtonStyle;
        private static GUIStyle cardBoxStyle;
        private static GUIStyle separatorStyle;
        private Vector2 scrollPosition = Vector2.zero;

        [MenuItem("Tools/DevToolkit Suite/Welcome", false, 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorWelcomeWindow>("DevToolkit Suite");
            window.minSize = new Vector2(480, 600);
            window.Show();
        }

        [InitializeOnLoadMethod]
        private static void AutoShow()
        {
            const string prefsKey = "DevToolkitSuite_WelcomeWindow_Shown";

            if (!EditorPrefs.GetBool(prefsKey, false))
            {
                EditorApplication.delayCall += () =>
                {
                    if (!EditorPrefs.GetBool(prefsKey, false))
                    {
                        EditorWelcomeWindow.ShowWindow();
                        EditorPrefs.SetBool(prefsKey, true);
                    }
                };
            }
        }

        private void OnEnable()
        {
            InitStyles();
            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
        }

        private void OnGUI()
        {
            InitStyles();

            if (gradientTex == null)
                gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

            // Beautiful header with gradient background
            Rect headerRect = new Rect(0, 0, position.width, 60);
            GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
            
            EditorGUILayout.Space(15);
            
            // Centered title section only (within gradient)
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.LabelField("🚀 DevToolkit Suite", new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black },
                margin = new RectOffset(10, 10, 5, 2)
            });
            
            EditorGUILayout.LabelField("Ultimate Unity Editor Enhancement", new GUIStyle(EditorStyles.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black },
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(10, 10, 0, 5)
            });
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(15);

            // Centered logo with matching background
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Icons/Logo/dts_icon.png");
            if (logo != null)
            {
                // Create background container for logo
                EditorGUILayout.BeginVertical(new GUIStyle("box")
                {
                    normal = { background = CreateSolidTexture(new Color(0.15f, 0.15f, 0.15f, 1f)) },
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(12, 12, 12, 12),
                    margin = new RectOffset(0, 0, 0, 0)
                });
                
                // Larger logo size
                Rect logoRect = GUILayoutUtility.GetRect(80, 80, GUILayout.Width(80), GUILayout.Height(80));
                GUI.DrawTexture(logoRect, logo, ScaleMode.ScaleToFit);
                
                EditorGUILayout.EndVertical();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(0);

            // Tab Navigation
            DrawTabNavigation();

            // Begin scroll view for content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            // Draw current tab content
            switch (currentTab)
            {
                case Tab.Overview:
                    DrawOverviewTab();
                    break;
                case Tab.GetStarted:
                    DrawGetStartedTab();
                    break;
                case Tab.WhatsNew:
                    DrawWhatsNewTab();
                    break;
                case Tab.Feedback:
                    DrawFeedbackTab();
                    break;
            }

            EditorGUILayout.Space(20);
            EditorGUILayout.EndScrollView();

            // Footer
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Created with ❤️ by OGB CREW", new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f) },
                alignment = TextAnchor.MiddleCenter
            });
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
        }

        private void DrawTabNavigation()
        {
            EditorGUILayout.Space(5);
            
            bool isVeryNarrow = position.width < 350;
            
            if (isVeryNarrow)
            {
                // Stack tabs vertically only for very narrow windows
                EditorGUILayout.BeginVertical(modernBoxStyle);
                
                if (TabButton("🏠 Overview", currentTab == Tab.Overview))
                    currentTab = Tab.Overview;
                    
                if (TabButton("🎯 Get Started", currentTab == Tab.GetStarted))
                    currentTab = Tab.GetStarted;
                    
                if (TabButton("✨ What's New", currentTab == Tab.WhatsNew))
                    currentTab = Tab.WhatsNew;
                    
                if (TabButton("💬 Feedback", currentTab == Tab.Feedback))
                    currentTab = Tab.Feedback;
                    
                EditorGUILayout.EndVertical();
            }
            else
            {
                // Horizontal tabs for most windows
                EditorGUILayout.BeginHorizontal(modernBoxStyle);
                
                if (TabButton("🏠 Overview", currentTab == Tab.Overview))
                    currentTab = Tab.Overview;
                    
                if (TabButton("🎯 Get Started", currentTab == Tab.GetStarted))
                    currentTab = Tab.GetStarted;
                    
                if (TabButton("✨ What's New", currentTab == Tab.WhatsNew))
                    currentTab = Tab.WhatsNew;
                    
                if (TabButton("💬 Feedback", currentTab == Tab.Feedback))
                    currentTab = Tab.Feedback;
                    
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(8);
        }

        private bool TabButton(string text, bool isActive)
        {
            var style = isActive ? tabActiveButtonStyle : tabButtonStyle;
            return GUILayout.Button(text, style, GUILayout.Height(32));
        }

        private void DrawOverviewTab()
        {
            // Welcome Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🌟 Welcome to DevToolkit Suite", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Your all-in-one solution to speed up Unity project management, organization, and debugging. Explore powerful tools, bookmark scenes, manage folders, and boost productivity like never before.", 
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                });
            EditorGUILayout.EndVertical();

            // Quick Links Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🔗 Quick Links", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            bool isNarrow = position.width < 500;
            
            var links = new[]
            {
                ("📚 Documentation", "https://ogb-crew.gitbook.io/devtoolkit-suite", "doc.png"),
                ("🌐 Official Website", "https://ogbcrew.com", "website.png"),
                ("💻 GitHub Repository", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite", "github.png"),
                ("📱 Follow on Instagram", "https://www.instagram.com/crewogb/", "instagram.png"),
                ("🎮 Our Games", "https://ogbcrew.com", "games.png")
            };

            // Always stack links vertically for better visibility
            foreach (var (text, url, icon) in links)
            {
                if (IconButton(text, icon, gradientTex, gradientButtonStyle, GUILayout.Height(32)))
                {
                    Application.OpenURL(url);
                }
                EditorGUILayout.Space(3);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawGetStartedTab()
        {
            // Introduction Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🚀 Ready to Boost Your Productivity?", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Click any tool below to launch it immediately and start exploring the powerful features of DevToolkit Suite.", 
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                });
            EditorGUILayout.EndVertical();

            // Tools Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🛠️ Available Tools", sectionLabelStyle);
            EditorGUILayout.Space(5);

            var tools = new[]
            {
                ("📝 PlayerPrefs Editor", "Edit and manage PlayerPrefs with a visual interface", "Tools/DevToolkit Suite/PlayerPrefs Browser", "playerprefs.png"),
                ("📷 Scene Camera Bookmarks", "Save and restore camera positions in scene view", "Tools/DevToolkit Suite/Scene Camera Bookmarks", "camera.png"),
                ("📁 Folder Icon Picker", "Customize project folder icons for better organization", "Tools/DevToolkit Suite/Folder Icon Picker", "folder.png"),
                ("🧩 ScriptableObject Browser", "Browse and manage ScriptableObjects in your project", "Tools/DevToolkit Suite/ScriptableObject Browser", "scriptable.png"),
                ("📋 Scene Notes", "Add contextual notes to your scenes", "Tools/DevToolkit Suite/Scene Notes", "note.png"),
                ("📂 Auto Folder Creator", "Automatically generate project folder structures", "Tools/DevToolkit Suite/Auto Folder Creator", "folder.png"),
                ("🎵 Audio Tools", "Batch process and edit audio clips", "Tools/DevToolkit Suite/AudioClip", "note.png")
            };

            foreach (var (name, description, menuPath, iconFile) in tools)
            {
                EditorGUILayout.BeginVertical(cardBoxStyle);
                
                EditorGUILayout.BeginHorizontal();
                
                // Tool icon
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{iconFile}");
                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(24), GUILayout.Height(24));
                    GUILayout.Space(8);
                }
                
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.LabelField(name, new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) }
                });
                
                EditorGUILayout.LabelField(description, new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                });
                
                EditorGUILayout.EndVertical();
                
                if (GUILayout.Button("▶", GUILayout.Width(30), GUILayout.Height(35)))
                {
                    EditorApplication.ExecuteMenuItem(menuPath);
                }
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawWhatsNewTab()
        {
            // Version Header
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("✨ What's New", sectionLabelStyle);
            EditorGUILayout.Space(3);
            
            EditorGUILayout.LabelField("Version 1.3.0 — December 2024", new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            });
            EditorGUILayout.EndVertical();

            // Updates Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🎉 Latest Updates", sectionLabelStyle);
            EditorGUILayout.Space(5);

            var updates = new[]
            {
                ("✅ Modernized Welcome Window with responsive design", "New sleek interface with gradient headers", "check.png"),
                ("🎨 Enhanced ScriptableObject Browser UI", "Beautiful sectioned layout with configuration options", "scriptable.png"),
                ("🔧 Advanced configuration system", "Customize behavior and appearance settings", "scriptable.png"),
                ("📱 Full responsive design support", "Adapts perfectly to any window size", "website.png"),
                ("🎵 New Audio processing tools", "Batch process and edit audio clips with waveform preview", "note.png"),
                ("🚀 Performance optimizations", "Faster loading and smoother interactions", "check.png"),
                ("📚 Complete documentation update", "Comprehensive guides and tutorials", "book.png")
            };

            foreach (var (title, description, iconFile) in updates)
            {
                EditorGUILayout.BeginVertical(cardBoxStyle);
                
                EditorGUILayout.BeginHorizontal();
                
                // Update icon
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{iconFile}");
                if (icon != null)
                {
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
                    GUILayout.Space(8);
                }
                
                EditorGUILayout.BeginVertical();
                
                EditorGUILayout.LabelField(title, new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) }
                });
                
                EditorGUILayout.LabelField(description, new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                });
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFeedbackTab()
        {
            // Feedback Header
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("💬 We'd Love Your Feedback!", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Tell us what you love, what you'd improve, or what features you'd like to see. Help us shape the future of DevToolkit Suite!", 
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                });
            EditorGUILayout.EndVertical();

            // Feedback Options
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("📮 Get in Touch", sectionLabelStyle);
            EditorGUILayout.Space(5);

            bool isNarrow = position.width < 500;

            var feedbackOptions = new[]
            {
                ("📧 Send Feedback Email", "mailto:info@ogbcrew.com", "mail.png"),
                ("🐛 Report a Bug", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite/issues", "bug.png"),
                ("💡 Suggest a Feature", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite/discussions", "idea.png"),
                ("⭐ Rate on Asset Store", "https://ogbcrew.com", "games.png"),
                ("💬 Join Our Community", "https://discord.gg/ogbcrew", "github.png")
            };

            if (isNarrow)
            {
                // Stack buttons vertically
                foreach (var (text, url, icon) in feedbackOptions)
                {
                    if (IconButton(text, icon, gradientTex, gradientButtonStyle, GUILayout.Height(30)))
                    {
                        Application.OpenURL(url);
                    }
                    EditorGUILayout.Space(3);
                }
            }
            else
            {
                // Two-column layout
                for (int i = 0; i < feedbackOptions.Length; i += 2)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    if (IconButton(feedbackOptions[i].Item1, feedbackOptions[i].Item3, gradientTex, gradientButtonStyle, GUILayout.Height(32)))
                    {
                        Application.OpenURL(feedbackOptions[i].Item2);
                    }
                    
                    if (i + 1 < feedbackOptions.Length)
                    {
                        GUILayout.Space(10);
                        if (IconButton(feedbackOptions[i + 1].Item1, feedbackOptions[i + 1].Item3, gradientTex, gradientButtonStyle, GUILayout.Height(32)))
                        {
                            Application.OpenURL(feedbackOptions[i + 1].Item2);
                        }
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndVertical();

            // Appreciation Section
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField("🙏 Thank You!", sectionLabelStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.LabelField("Your feedback helps us create better tools for the Unity community. Every suggestion, bug report, and feature request makes DevToolkit Suite better for everyone.", 
                new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontSize = 13,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                });
            EditorGUILayout.EndVertical();
        }

        private void InitStyles()
        {
            if (headerLabelStyle == null)
            {
                headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
                    margin = new RectOffset(0, 0, 5, 10)
                };
            }

            if (sectionLabelStyle == null)
            {
                sectionLabelStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
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

            if (cardBoxStyle == null)
            {
                cardBoxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2),
                    normal = { 
                        background = CreateSolidTexture(new Color(0.2f, 0.2f, 0.2f, 0.9f)),
                        textColor = Color.white 
                    },
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (tabButtonStyle == null)
            {
                tabButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    normal = { textColor = new Color(0.8f, 0.8f, 0.8f) },
                    hover = { textColor = Color.white },
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
                };
            }

            if (tabActiveButtonStyle == null)
            {
                tabActiveButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    normal = { 
                        textColor = Color.white,
                        background = CreateSolidTexture(new Color(0.3f, 0.6f, 1f, 0.8f))
                    },
                    hover = { textColor = Color.white },
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(2, 2, 2, 2)
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

        private bool IconButton(string text, string iconFile, Texture2D hoverTex, GUIStyle style, params GUILayoutOption[] options)
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{iconFile}");
            
            // Don't include icon in GUIContent to avoid duplication
            GUIContent content = new GUIContent(text);
            
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(icon != null ? 30 : 8, 8, 6, 6),
                margin = new RectOffset(20, 20, 2, 2) // Shorter from sides
            };

            // Use the margin for shorter buttons from sides
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20); // Left margin
            
            Rect buttonRect = GUILayoutUtility.GetRect(content, buttonStyle, options);
            bool clicked = GUI.Button(buttonRect, content, buttonStyle);
            
            GUILayout.Space(20); // Right margin
            EditorGUILayout.EndHorizontal();
            
            // Draw icon separately for better control
            if (icon != null)
            {
                Rect iconRect = new Rect(buttonRect.x + 8, buttonRect.y + (buttonRect.height - 20) / 2, 20, 20);
                GUI.DrawTexture(iconRect, icon);
            }
            
            return clicked;
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