using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class SceneCameraBookmarks : EditorWindow
{
    private static List<CameraBookmark> bookmarks => CameraBookmarkStorage.bookmarks;

    private Vector2 scroll;
    private string newBookmarkName = "";
    private int renamingIndex = -1;
    private string renamingBuffer = "";
    private int hotkeyEditingIndex = -1;

    private static SceneCameraBookmarks currentInstance;

    // Modern UI styling
    private GUIStyle gradientButtonStyle;
    private Texture2D gradientTex;
    private static GUIStyle headerLabelStyle;
    private static GUIStyle sectionLabelStyle;
    private static GUIStyle modernBoxStyle;
    private static GUIStyle bookmarkBoxStyle;
    private static GUIStyle separatorStyle;
    private Vector2 scrollPosition = Vector2.zero;

    private void OnEnable()
    {
        currentInstance = this;
        InitStyles();

        if (gradientTex == null)
            gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
    }


    [MenuItem("Tools/DevToolkit Suite/Scene Camera Bookmarks")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneCameraBookmarks>("Scene Camera Bookmarks");
        window.minSize = new Vector2(400, 500);
        currentInstance = window;
    }

    public static void ForceRepaint()
    {
        currentInstance?.Repaint();
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
        EditorGUILayout.LabelField("🎥 Scene Camera Bookmarks", headerLabelStyle);
        EditorGUILayout.Space(10);

        // Begin scroll view for all content
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Add New Bookmark Section
        EditorGUILayout.BeginVertical(modernBoxStyle);
        EditorGUILayout.LabelField("➕ Add New Bookmark", sectionLabelStyle);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
        newBookmarkName = EditorGUILayout.TextField(newBookmarkName, GUILayout.Height(22));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        if (GradientButton("📍 Add Current View", gradientTex, gradientButtonStyle))
        {
            AddCurrentView();
        }
        EditorGUILayout.EndVertical();

        // Bookmarks List Section
        EditorGUILayout.BeginVertical(modernBoxStyle);
        EditorGUILayout.LabelField("📚 Saved Bookmarks", sectionLabelStyle);
        EditorGUILayout.Space(5);

        if (bookmarks.Count == 0)
        {
            EditorGUILayout.HelpBox("📂 No bookmarks saved yet. Add your first bookmark using the section above!", MessageType.Info);
        }

        for (int i = 0; i < bookmarks.Count; i++)
        {
            var bookmark = bookmarks[i];

            EditorGUILayout.BeginVertical(bookmarkBoxStyle);
            
            // Bookmark header with icon and name
            GUIStyle bookmarkHeaderStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.7f, 0.9f, 1f) }
            };
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📍 {bookmark.name}", bookmarkHeaderStyle);
            GUILayout.FlexibleSpace();
            
            // Show hotkey if assigned
            if (bookmark.hotkey != KeyCode.None)
            {
                GUIStyle hotkeyStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField($"[{bookmark.hotkey}]", hotkeyStyle, GUILayout.Width(60));
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // Responsive action buttons layout
            bool useVerticalLayout = position.width < 600; // Switch to vertical layout if window is narrow
            
            if (useVerticalLayout)
            {
                // Vertical layout for narrow windows
                DrawBookmarkActionsVertical(bookmark, i);
            }
            else
            {
                // Horizontal layout for wide windows
                DrawBookmarkActionsHorizontal(bookmark, i);
            }
            EditorGUILayout.EndVertical();

            if (i < bookmarks.Count - 1)
                EditorGUILayout.Space(3);
        }

        EditorGUILayout.EndVertical(); // End bookmarks list section

        // Show bookmark count and hotkey info
        if (bookmarks.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField($"📊 Total Bookmarks: {bookmarks.Count}", EditorStyles.miniLabel);
            
            // Count hotkeys assigned
            int hotkeyCount = 0;
            foreach (var bookmark in bookmarks)
            {
                if (bookmark.hotkey != KeyCode.None) hotkeyCount++;
            }
            
            if (hotkeyCount > 0)
            {
                EditorGUILayout.LabelField($"⌨️ Hotkeys Assigned: {hotkeyCount}", EditorStyles.miniLabel);
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("💡 Press assigned hotkeys in Scene View to navigate quickly!", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("💡 Assign hotkeys to bookmarks for instant navigation!", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }

        // End scroll view
        EditorGUILayout.EndScrollView();
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

        if (bookmarkBoxStyle == null)
        {
            bookmarkBoxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(8, 8, 4, 4),
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
        Rect rect = GUILayoutUtility.GetRect(content, style, options);
        Event e = Event.current;

        bool isHovering = rect.Contains(e.mousePosition);
        bool isClicked = false;

        if (e.type == EventType.Repaint)
        {
            // Hover effect with gradient, default button otherwise
            if (isHovering)
            {
                GUI.DrawTexture(rect, hoverTex, ScaleMode.StretchToFill);
                GUI.Label(rect, content, style); // just draw the text
            }
            else
            {
                GUI.Button(rect, content, GUI.skin.button); // Unity's default style
            }
        }

        // Cursor
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

        // Click detection
        if (e.type == EventType.MouseDown && isHovering && e.button == 0)
        {
            isClicked = true;
            GUI.FocusControl(null);
            e.Use();
        }

        return isClicked;
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

    private void DrawBookmarkActionsHorizontal(CameraBookmark bookmark, int i)
    {
        EditorGUILayout.BeginHorizontal();

        // Go To button
        if (GradientButton("🎯 Go To", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
        {
            SceneView.lastActiveSceneView.LookAt(bookmark.position, bookmark.rotation);
        }

        // Handle different states
        if (renamingIndex == i)
        {
            renamingBuffer = EditorGUILayout.TextField(renamingBuffer, GUILayout.Height(22));

            if (GradientButton("💾 Save", gradientTex, gradientButtonStyle, GUILayout.Width(70)))
            {
                bookmark.name = renamingBuffer;
                renamingIndex = -1;
                SceneCameraBookmarksOverlay.Refresh();
            }

            if (GradientButton("❌ Cancel", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
            {
                renamingIndex = -1;
            }
        }
        else if (hotkeyEditingIndex == i)
        {
            EditorGUILayout.LabelField("Hotkey:", GUILayout.Width(50));
            
            KeyCode newHotkey = (KeyCode)EditorGUILayout.EnumPopup(bookmark.hotkey, GUILayout.Width(100));
            
            if (newHotkey != bookmark.hotkey)
            {
                ProcessHotkeyChange(bookmark, newHotkey);
            }

            if (GradientButton("✅ Done", gradientTex, gradientButtonStyle, GUILayout.Width(60)))
            {
                hotkeyEditingIndex = -1;
            }
        }
        else
        {
            if (GradientButton("✏️ Rename", gradientTex, gradientButtonStyle, GUILayout.Width(90)))
            {
                renamingIndex = i;
                renamingBuffer = bookmark.name;
            }

            string hotkeyButtonText = bookmark.hotkey != KeyCode.None ? $"🔑 {bookmark.hotkey}" : "🔑 Set Key";
            if (GradientButton(hotkeyButtonText, gradientTex, gradientButtonStyle, GUILayout.Width(100)))
            {
                hotkeyEditingIndex = i;
            }

            if (bookmark.hotkey != KeyCode.None)
            {
                if (GradientButton("🚫 Clear", gradientTex, gradientButtonStyle, GUILayout.Width(70)))
                {
                    bookmark.hotkey = KeyCode.None;
                    SceneCameraBookmarksOverlay.Refresh();
                }
            }
        }

        GUILayout.FlexibleSpace();

        // Delete button
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Delete", GUILayout.Height(24), GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Delete Bookmark", 
                $"Are you sure you want to delete bookmark '{bookmark.name}'?", 
                "Delete", "Cancel"))
            {
                bookmarks.RemoveAt(i);
                SceneCameraBookmarksOverlay.Refresh();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                return;
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawBookmarkActionsVertical(CameraBookmark bookmark, int i)
    {
        // First row: Navigation and primary actions
        EditorGUILayout.BeginHorizontal();
        
        if (GradientButton("🎯 Go To", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
        {
            SceneView.lastActiveSceneView.LookAt(bookmark.position, bookmark.rotation);
        }
        
        // Delete button (always visible in vertical layout)
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("🗑️ Delete", GUILayout.Height(24), GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Delete Bookmark", 
                $"Are you sure you want to delete bookmark '{bookmark.name}'?", 
                "Delete", "Cancel"))
            {
                bookmarks.RemoveAt(i);
                SceneCameraBookmarksOverlay.Refresh();
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
                return;
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();

        // Second row: Edit functions
        if (renamingIndex == i)
        {
            EditorGUILayout.BeginHorizontal();
            renamingBuffer = EditorGUILayout.TextField(renamingBuffer, GUILayout.Height(22));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GradientButton("💾 Save", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
            {
                bookmark.name = renamingBuffer;
                renamingIndex = -1;
                SceneCameraBookmarksOverlay.Refresh();
            }
            if (GradientButton("❌ Cancel", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
            {
                renamingIndex = -1;
            }
            EditorGUILayout.EndHorizontal();
        }
        else if (hotkeyEditingIndex == i)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Hotkey:", GUILayout.Width(90));
            KeyCode newHotkey = (KeyCode)EditorGUILayout.EnumPopup(bookmark.hotkey);
            EditorGUILayout.EndHorizontal();
            
            if (newHotkey != bookmark.hotkey)
            {
                ProcessHotkeyChange(bookmark, newHotkey);
            }

            EditorGUILayout.BeginHorizontal();
            if (GradientButton("✅ Done", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
            {
                hotkeyEditingIndex = -1;
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (GradientButton("✏️ Rename", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
            {
                renamingIndex = i;
                renamingBuffer = bookmark.name;
            }

            string hotkeyButtonText = bookmark.hotkey != KeyCode.None ? $"🔑 {bookmark.hotkey}" : "🔑 Set Key";
            if (GradientButton(hotkeyButtonText, gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
            {
                hotkeyEditingIndex = i;
            }
            EditorGUILayout.EndHorizontal();

            if (bookmark.hotkey != KeyCode.None)
            {
                EditorGUILayout.BeginHorizontal();
                if (GradientButton("🚫 Clear Hotkey", gradientTex, gradientButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    bookmark.hotkey = KeyCode.None;
                    SceneCameraBookmarksOverlay.Refresh();
                }
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void ProcessHotkeyChange(CameraBookmark bookmark, KeyCode newHotkey)
    {
        bool isAlreadyAssigned = false;
        if (newHotkey != KeyCode.None)
        {
            foreach (var otherBookmark in bookmarks)
            {
                if (otherBookmark != bookmark && otherBookmark.hotkey == newHotkey)
                {
                    isAlreadyAssigned = true;
                    break;
                }
            }
        }
        
        if (!isAlreadyAssigned)
        {
            bookmark.hotkey = newHotkey;
            hotkeyEditingIndex = -1;
            SceneCameraBookmarksOverlay.Refresh();
        }
        else
        {
            EditorUtility.DisplayDialog("Hotkey Conflict", 
                $"The key '{newHotkey}' is already assigned to another bookmark.", "OK");
        }
    }

    private void AddCurrentView()
    {
        if (string.IsNullOrWhiteSpace(newBookmarkName))
            newBookmarkName = "Bookmark " + (bookmarks.Count + 1);

        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            bookmarks.Add(new CameraBookmark
            {
                name = newBookmarkName,
                position = view.camera.transform.position,
                rotation = view.camera.transform.rotation
            });

            newBookmarkName = "";

            Repaint(); // update this window
            SceneCameraBookmarksOverlay.Refresh(); // update overlay
        }
    }
}
