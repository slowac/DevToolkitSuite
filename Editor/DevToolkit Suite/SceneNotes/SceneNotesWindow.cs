using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class SceneNotesWindow : EditorWindow
{
    // Modern UI styling
    private GUIStyle gradientButtonStyle;
    private Texture2D gradientTex;
    private static GUIStyle headerLabelStyle;
    private static GUIStyle sectionLabelStyle;
    private static GUIStyle modernBoxStyle;
    private static GUIStyle noteCardStyle;
    private static GUIStyle separatorStyle;

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += Repaint;
        InitStyles();

        if (gradientTex == null)
            gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= Repaint;
    }


    [MenuItem("Tools/DevToolkit Suite/Scene Notes")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneNotesWindow>("Scene Notes");
        window.minSize = new Vector2(320, 400); // Reduced minimum width
    }

    private Vector2 scroll;
    private string searchText = "";
    private bool onlyIncomplete = false;
    private bool toggleVisibility = false;
    private bool showCategoryDropdown = false;
    private List<SceneNoteCategory> categoryFilter = new();

    void OnGUI()
    {
        InitStyles();

        if (gradientTex == null)
            gradientTex = CreateHorizontalGradient(256, 32, new Color(0f, 0.686f, 0.972f), new Color(0.008f, 0.925f, 0.643f));

        // Beautiful header with gradient background
        Rect headerRect = new Rect(0, 0, position.width, 50);
        GUI.DrawTexture(headerRect, gradientTex, ScaleMode.StretchToFill);
        EditorGUILayout.Space(15);
        EditorGUILayout.LabelField("📋 Scene Notes", headerLabelStyle);
        EditorGUILayout.Space(10);

        // Begin scroll view for all content
        scroll = EditorGUILayout.BeginScrollView(scroll);

        // Search & Filter Section
        EditorGUILayout.BeginVertical(modernBoxStyle);
        EditorGUILayout.LabelField("🔍 Search & Filters", sectionLabelStyle);
        EditorGUILayout.Space(5);
        
        // Search bar - always full width
        EditorGUILayout.LabelField("Search:", EditorStyles.miniLabel);
        searchText = EditorGUILayout.TextField(searchText, GUILayout.Height(22));
        
        EditorGUILayout.Space(5);
        
        // Filter toggles - stack vertically on narrow windows
        bool isNarrow = position.width < 380;
        if (isNarrow)
        {
            // Stack vertically for narrow windows
            toggleVisibility = EditorGUILayout.Toggle("👁️ Toggle Visibility", toggleVisibility);
            onlyIncomplete = EditorGUILayout.Toggle("📝 Only Incomplete", onlyIncomplete);
        }
        else
        {
            // Horizontal layout for wider windows
            EditorGUILayout.BeginHorizontal();
            toggleVisibility = EditorGUILayout.Toggle("👁️ Toggle Visibility", toggleVisibility);
            onlyIncomplete = EditorGUILayout.Toggle("📝 Only Incomplete", onlyIncomplete);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);
        if (GradientButton("🏷️ Select Categories", gradientTex, gradientButtonStyle))
        {
            showCategoryDropdown = !showCategoryDropdown;
        }

        if (showCategoryDropdown)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(noteCardStyle);
            EditorGUILayout.LabelField("📂 Categories", EditorStyles.boldLabel);
            foreach (SceneNoteCategory cat in Enum.GetValues(typeof(SceneNoteCategory)))
            {
                bool selected = categoryFilter.Contains(cat);
                bool toggle = EditorGUILayout.Toggle($"• {cat}", selected);
                if (toggle && !selected) categoryFilter.Add(cat);
                if (!toggle && selected) categoryFilter.Remove(cat);
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        // Scene Notes Section
        EditorGUILayout.BeginVertical(modernBoxStyle);
        EditorGUILayout.LabelField("📝 Scene Notes", sectionLabelStyle);
        EditorGUILayout.Space(5);

        SceneNote[] notes = FindObjectsOfType<SceneNote>();
        var filteredNotes = notes
            .Where(n => string.IsNullOrEmpty(searchText) || n.noteText.ToLower().Contains(searchText.ToLower()))
            .Where(n => !onlyIncomplete || !n.isCompleted)
            .Where(n => categoryFilter.Count == 0 || categoryFilter.Contains(n.category))
            .ToArray();

        if (filteredNotes.Length == 0)
        {
            EditorGUILayout.HelpBox("📋 No notes found matching your criteria. Create some SceneNote objects in your scene!", MessageType.Info);
        }
        else
        {
            foreach (var group in filteredNotes.GroupBy(n => n.category))
            {
                // Category header with modern styling
                GUIStyle categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.7f, 0.9f, 1f) },
                    margin = new RectOffset(5, 5, 10, 5)
                };

                EditorGUILayout.LabelField($"🏷️ {group.Key}", categoryHeaderStyle);
                EditorGUILayout.Space(5);

                foreach (var note in group.ToList())
                {
                    note.gameObject.hideFlags = toggleVisibility ? HideFlags.None : HideFlags.HideInHierarchy;

                    if (DrawNoteCard(note))
                        continue;

                    EditorGUILayout.Space(5);
                }

                // Add separator between categories
                if (group != filteredNotes.GroupBy(n => n.category).Last())
                {
                    EditorGUILayout.Space(5);
                    GUILayout.Box("", separatorStyle, GUILayout.ExpandWidth(true));
                    EditorGUILayout.Space(5);
                }
            }
        }

        EditorGUILayout.EndVertical();

        // Show notes count
        if (filteredNotes.Length > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(modernBoxStyle);
            EditorGUILayout.LabelField($"📊 Total Notes: {filteredNotes.Length} | Completed: {filteredNotes.Count(n => n.isCompleted)}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // End scroll view
        EditorGUILayout.EndScrollView();

        if (toggleVisibility)
            EditorApplication.RepaintHierarchyWindow();
    }

    private bool DrawNoteCard(SceneNote note)
    {
        bool deleted = false;

        // Check if we need narrow layout early
        bool isNarrowCard = position.width < 350;

        // Modern note card styling
        GUIStyle noteHeaderStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13,
            normal = { textColor = note.isCompleted ? new Color(0.5f, 0.8f, 0.5f) : new Color(0.9f, 0.9f, 0.9f) }
        };

        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            fontSize = 11,
            normal = { textColor = Color.white }
        };

        // Apply completion styling to the entire card
        Color cardBgColor = note.isCompleted ? 
            new Color(0.15f, 0.35f, 0.15f, 0.9f) : 
            new Color(0.2f, 0.2f, 0.2f, 0.9f);

        GUIStyle completedCardStyle = new GUIStyle(noteCardStyle)
        {
            normal = { background = CreateSolidTexture(cardBgColor) }
        };

        EditorGUILayout.BeginVertical(completedCardStyle);
        
        // Header row with checkbox and note name - adapt to width
        if (isNarrowCard)
        {
            // Stack header and buttons vertically for narrow cards
            string statusIcon = note.isCompleted ? "✅" : "⭕";
            note.isCompleted = EditorGUILayout.Toggle($"{statusIcon} {note.name}", note.isCompleted, noteHeaderStyle);
        }
        else
        {
            // Horizontal layout for wider cards
            EditorGUILayout.BeginHorizontal();
            
            // Completion checkbox with emoji
            string statusIcon = note.isCompleted ? "✅" : "⭕";
            note.isCompleted = EditorGUILayout.Toggle($"{statusIcon} {note.name}", note.isCompleted, noteHeaderStyle);
            
            GUILayout.FlexibleSpace();
        }

        // Action buttons - adapt to window width
        if (isNarrowCard)
        {
            // Stack buttons vertically for very narrow windows
            if (GradientButton("🎯 Focus", gradientTex, gradientButtonStyle, GUILayout.Height(22)))
            {
                Bounds bounds = new Bounds(note.transform.position, Vector3.one * 2f);
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }

            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("🗑️ Delete", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog("Delete Note", 
                    $"Are you sure you want to delete note '{note.name}'?", 
                    "Delete", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(note.gameObject);
                    deleted = true;
                }
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            // Horizontal layout for wider windows
            if (GradientButton("🎯 Focus", gradientTex, gradientButtonStyle, GUILayout.Width(80)))
            {
                Bounds bounds = new Bounds(note.transform.position, Vector3.one * 2f);
                SceneView.lastActiveSceneView.Frame(bounds, false);
            }

            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("🗑️ Delete", GUILayout.Height(24), GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Delete Note", 
                    $"Are you sure you want to delete note '{note.name}'?", 
                    "Delete", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(note.gameObject);
                    deleted = true;
                }
            }
            GUI.backgroundColor = Color.white;
        }

        // End horizontal layout only if we started one
        if (!isNarrowCard)
        {
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(5);

        // Note text area
        if (!deleted)
        {
            note.noteText = EditorGUILayout.TextArea(note.noteText, textAreaStyle, GUILayout.MinHeight(50));
        }

        EditorGUILayout.EndVertical();
        return deleted;
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

        if (noteCardStyle == null)
        {
            noteCardStyle = new GUIStyle("box")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(5, 5, 3, 3),
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
}
