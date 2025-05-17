using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class EditorWelcomeWindow : EditorWindow
{
    private const string SessionKey = "EditorPackUltimate_WelcomeShown";
    private VisualElement contentArea;

    [MenuItem("Tools/DevToolkit Suite/Welcome", false, 0)]
    public static void ShowWindow()
    {
        var window = GetWindow<EditorWelcomeWindow>("Welcome");
        window.minSize = new Vector2(560, 600);
        window.maxSize = new Vector2(560, 600);
        window.Show();
    }

    [InitializeOnLoadMethod]
    private static void AutoShow()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            ShowWindow();
            SessionState.SetBool(SessionKey, true);
        }
    }

    private void OnEnable()
    {
        // Do not set up UI before assets are ready, run in next frame
        EditorApplication.delayCall += InitializeUI;
    }

    private void InitializeUI()
    {
        if (this == null) return; // pencere kapanmışsa

        var root = rootVisualElement;
        root.Clear();

        root.style.backgroundColor = new Color(0.13f, 0.13f, 0.13f);
        root.style.flexDirection = FlexDirection.Column;
        root.style.justifyContent = Justify.FlexStart;
        root.style.alignItems = Align.Stretch;
        root.style.paddingTop = 10;
        root.style.flexGrow = 1;

        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>("Packages/com.ogbcrew.devtoolkitsuite/Icons/Logo/dts_icon.png");
        if (logo != null)
        {
            var logoImage = new Image
            {
                image = logo,
                style = {
                width = 120,
                height = 120,
                alignSelf = Align.Center,
                marginBottom = 10
            }
            };
            root.Add(logoImage);
        }

        var tabBar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 6 } };
        root.Add(tabBar);

        void AddTab(string title, System.Action onClick)
        {
            var button = new Button(onClick) { text = title };
            button.style.flexGrow = 1;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.fontSize = 12;
            button.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            button.style.color = Color.white;
            button.style.height = 28;
            button.style.marginRight = 2;
            tabBar.Add(button);
        }

        contentArea = new ScrollView { style = { flexGrow = 1, paddingTop = 10, paddingLeft = 16, paddingRight = 16 } };
        root.Add(contentArea);

        var footer = new Label("Created by OGB CREW")
        {
            style = {
            unityTextAlign = TextAnchor.MiddleCenter,
            fontSize = 11,
            color = new Color(0.6f, 0.6f, 0.6f),
            marginTop = 6,
            marginBottom = 8
        }
        };
        root.Add(footer);

        AddTab("Overview", () => LoadOverviewTab());
        AddTab("Get Started", () => LoadGetStartedTab());
        AddTab("What's New", () => LoadWhatsNewTab());
        AddTab("Feedback", () => LoadFeedbackTab());

        LoadOverviewTab();
    }

    private void LoadOverviewTab()
    {
        contentArea.Clear();

        contentArea.Add(new Label("Welcome to Editor Pack Ultimate")
        {
            style = {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 18,
                color = Color.white,
                unityTextAlign = TextAnchor.MiddleCenter,
                marginBottom = 4
            }
        });

        contentArea.Add(new Label("Editor Pack Ultimate is your all-in-one solution to speed up Unity project management, organization, and debugging.\n\nExplore tools, bookmark your scene, manage folders and boost productivity like never before.")
        {
            style = {
                unityTextAlign = TextAnchor.MiddleCenter,
                fontSize = 13,
                color = new Color(0.85f, 0.85f, 0.85f),
                whiteSpace = WhiteSpace.Normal,
                marginBottom = 10,
                maxWidth = 480,
                alignSelf = Align.Center
            }
        });

        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Column;
        buttonContainer.style.maxWidth = 320;
        buttonContainer.style.alignSelf = Align.Center;
        buttonContainer.style.marginTop = 10;

        AddIconButton(buttonContainer, "Documentation", "https://ogb-crew.gitbook.io/devtoolkit-suite", "Packages/com.ogbcrew.devtoolkitsuite/Icons/doc.png");
        AddIconButton(buttonContainer, "Official Website", "https://ogbcrew.com", "Packages/com.ogbcrew.devtoolkitsuite/Icons/website.png");
        AddIconButton(buttonContainer, "GitHub Repository", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite", "Packages/com.ogbcrew.devtoolkitsuite/Icons/github.png");
        AddIconButton(buttonContainer, "Follow on Instagram", "https://www.instagram.com/crewogb/", "Packages/com.ogbcrew.devtoolkitsuite/Icons/instagram.png");
        AddIconButton(buttonContainer, "Our Games", "https://ogbcrew.com", "Packages/com.ogbcrew.devtoolkitsuite/Icons/games.png");

        contentArea.Add(buttonContainer);
    }

    private void LoadGetStartedTab()
    {
        contentArea.Clear();

        var container = new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Column,
                backgroundColor = new Color(0.1f, 0.1f, 0.1f),
                paddingTop = 24,
                paddingBottom = 24,
                paddingLeft = 28,
                paddingRight = 28,
                borderTopLeftRadius = 12,
                borderTopRightRadius = 12,
                borderBottomLeftRadius = 12,
                borderBottomRightRadius = 12,
                marginTop = 14,
                marginBottom = 10,
                alignItems = Align.Stretch
            }
        };

        var header = new Label("Try It Now")
        {
            style = {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 24,
                color = Color.white,
                marginBottom = 20,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
        container.Add(header);

        var tools = new (string name, string icon, string path)[]
        {
            ("PlayerPrefs Editor", "playerprefs.png", "Tools/DevToolkit Suite/PlayerPrefs Editor"),
            ("Scene Camera Bookmarks", "camera.png", "Tools/DevToolkit Suite/Scene Camera Bookmarks"),
            ("Folder Icon Picker", "folder.png", "Tools/DevToolkit Suite/Folder Icon Picker"),
            ("ScriptableObject Browser", "scriptable.png", "Tools/DevToolkit Suite/ScriptableObject Browser"),
            ("Scene Notes", "note.png", "Tools/DevToolkit Suite/Scene Notes")
        };

        foreach (var (name, icon, menuPath) in tools)
        {
            var card = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 14,
                    paddingRight = 14,
                    marginBottom = 10
                }
            };

            card.RegisterCallback<MouseEnterEvent>(_ => card.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f));
            card.RegisterCallback<MouseLeaveEvent>(_ => card.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f));
            card.RegisterCallback<MouseDownEvent>(_ => EditorApplication.ExecuteMenuItem(menuPath));

            var iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{icon}");
            if (iconTex != null)
            {
                card.Add(new Image
                {
                    image = iconTex,
                    style = {
                        width = 20,
                        height = 20,
                        marginRight = 14
                    }
                });
            }

            card.Add(new Label(name)
            {
                style = {
                    fontSize = 15,
                    color = Color.white,
                    flexGrow = 1
                }
            });

            var arrow = new Label("›")
            {
                style = {
                    fontSize = 18,
                    color = new Color(0.5f, 0.5f, 0.5f),
                    unityTextAlign = TextAnchor.MiddleRight
                }
            };
            card.Add(arrow);

            container.Add(card);
        }

        contentArea.Add(container);
    }

    private void LoadWhatsNewTab()
    {
        contentArea.Clear();

        var section = new VisualElement
        {
            style = {
                backgroundColor = new Color(0.1f, 0.1f, 0.1f),
                paddingTop = 24,
                paddingBottom = 24,
                paddingLeft = 28,
                paddingRight = 28,
                borderTopLeftRadius = 12,
                borderTopRightRadius = 12,
                borderBottomLeftRadius = 12,
                borderBottomRightRadius = 12,
                marginTop = 14,
                marginBottom = 10,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };

        section.Add(new Label("What's New")
        {
            style = {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 24,
                color = Color.white,
                marginBottom = 8
            }
        });

        section.Add(new Label("Version 1.3.0 — May 2025")
        {
            style = {
                fontSize = 14,
                color = Color.gray,
                marginBottom = 20
            }
        });

        string[,] updates = new string[,]
        {
            {"New Welcome Window with tab navigation", "check.png"},
            {"Added full documentation links", "book.png"},
            {"Scene Camera Bookmarks overlay auto-refresh", "camera.png"},
            {"Scene Notes now support categories and icons", "note.png"},
            {"Folder Icon Picker supports 50+ icons", "folder.png"}
        };

        for (int i = 0; i < updates.GetLength(0); i++)
        {
            var row = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 10,
                    paddingTop = 8,
                    paddingBottom = 8,
                    paddingLeft = 12,
                    paddingRight = 12,
                    backgroundColor = new Color(0.17f, 0.17f, 0.17f),
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                }
            };

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{updates[i, 1]}");
            if (icon != null)
            {
                row.Add(new Image
                {
                    image = icon,
                    style = {
                        width = 20,
                        height = 20,
                        marginRight = 12
                    }
                });
            }

            row.Add(new Label(updates[i, 0])
            {
                style = {
                    fontSize = 14,
                    color = new Color(0.95f, 0.95f, 0.95f),
                    flexGrow = 1
                }
            });

            section.Add(row);
        }

        contentArea.Add(section);
    }

    private void LoadFeedbackTab()
    {
        contentArea.Clear();

        var section = new VisualElement
        {
            style = {
                backgroundColor = new Color(0.1f, 0.1f, 0.1f),
                paddingTop = 24,
                paddingBottom = 24,
                paddingLeft = 28,
                paddingRight = 28,
                borderTopLeftRadius = 12,
                borderTopRightRadius = 12,
                borderBottomLeftRadius = 12,
                borderBottomRightRadius = 12,
                marginTop = 14,
                marginBottom = 10,
                alignItems = Align.Center
            }
        };

        var header = new Label("We'd Love Your Feedback!")
        {
            style = {
                unityFontStyleAndWeight = FontStyle.Bold,
                fontSize = 24,
                color = Color.white,
                marginBottom = 10,
                unityTextAlign = TextAnchor.MiddleCenter
            }
        };
        section.Add(header);

        var subtitle = new Label("Tell us what you love, what you'd improve, or what you wish existed. Help us shape the future of Editor Pack Ultimate.")
        {
            style = {
                fontSize = 13,
                color = new Color(0.85f, 0.85f, 0.85f),
                marginBottom = 24,
                unityTextAlign = TextAnchor.MiddleCenter,
                whiteSpace = WhiteSpace.Normal,
                maxWidth = 440
            }
        };
        section.Add(subtitle);

        var buttonContainer = new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Column,
                alignItems = Align.Stretch,
                maxWidth = 320,
                alignSelf = Align.Center
            }
        };

        AddInteractiveButton(buttonContainer, "Send Feedback Mail", "mailto:info@ogbcrew.com", "mail.png");
        AddInteractiveButton(buttonContainer, "Report a Bug", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite/issues", "bug.png");
        AddInteractiveButton(buttonContainer, "Suggest a Feature", "https://github.com/slowac/com.ogbcrew.devtoolkitsuite/discussions", "idea.png");

        section.Add(buttonContainer);
        contentArea.Add(section);
    }

    private void AddInteractiveButton(VisualElement parent, string text, string url, string iconFile)
    {
        var row = new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = 6,
                marginBottom = 6,
                backgroundColor = new Color(0.18f, 0.18f, 0.18f),
                height = 40,
                paddingLeft = 12,
                paddingRight = 12,
                borderBottomLeftRadius = 6,
                borderBottomRightRadius = 6,
                borderTopLeftRadius = 6,
                borderTopRightRadius = 6
                // transitionDuration = ...
            }
        };

        row.RegisterCallback<MouseEnterEvent>(_ => row.style.backgroundColor = new Color(0.24f, 0.24f, 0.24f));
        row.RegisterCallback<MouseLeaveEvent>(_ => row.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f));
        row.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(url));

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.ogbcrew.devtoolkitsuite/Icons/{iconFile}");
        if (icon != null)
        {
            row.Add(new Image
            {
                image = icon,
                style = {
                    width = 20,
                    height = 20,
                    marginRight = 12
                }
            });
        }

        row.Add(new Label(text)
        {
            style = {
                fontSize = 14,
                color = Color.white,
                flexGrow = 1
            }
        });

        parent.Add(row);
    }

    private void AddIconButton(VisualElement parent, string text, string url, string iconPath)
    {
        var row = new VisualElement
        {
            style = {
                flexDirection = FlexDirection.Row,
                alignItems = Align.Center,
                marginTop = 6,
                marginBottom = 2,
                backgroundColor = new Color(0.2f, 0.2f, 0.2f),
                height = 32,
                maxWidth = 320,
                paddingLeft = 10,
                paddingRight = 10,
                borderBottomLeftRadius = 4,
                borderBottomRightRadius = 4,
                borderTopLeftRadius = 4,
                borderTopRightRadius = 4
            }
        };

        row.RegisterCallback<MouseDownEvent>(_ => Application.OpenURL(url));

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon != null)
        {
            row.Add(new Image
            {
                image = icon,
                style = {
                    width = 18,
                    height = 18,
                    marginRight = 10
                }
            });
        }

        row.Add(new Label(text)
        {
            style = {
                fontSize = 13,
                color = Color.white,
                flexGrow = 1
            }
        });

        parent.Add(row);
    }
}