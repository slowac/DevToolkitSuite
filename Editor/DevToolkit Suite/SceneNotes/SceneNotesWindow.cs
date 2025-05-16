using UnityEditor;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class SceneNotesWindow : EditorWindow
{

    private void OnEnable()
    {
        EditorApplication.hierarchyChanged += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= Repaint;
    }


    [MenuItem("Tools/DevToolkit Suite/Scene Notes")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneNotesWindow>("Scene Notes");
        window.minSize = new Vector2(400, 500);
    }

    private Vector2 scroll;
    private string searchText = "";
    private bool onlyIncomplete = false;
    private bool toggleVisibility = false;
    private bool showCategoryDropdown = false;
    private List<SceneNoteCategory> categoryFilter = new();

    void OnGUI()
    {
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        GUILayout.Space(10);
        GUILayout.Label("📋 Scene Notes", headerStyle);
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField("Search", searchText);
        toggleVisibility = GUILayout.Toggle(toggleVisibility, "Toggle Visibility", GUILayout.Width(130));
        onlyIncomplete = GUILayout.Toggle(onlyIncomplete, "Only Incomplete", GUILayout.Width(130));

        if (GUILayout.Button("Select Category", GUILayout.Width(130)))
        {
            showCategoryDropdown = !showCategoryDropdown;
        }
        EditorGUILayout.EndHorizontal();

        if (showCategoryDropdown)
        {
            EditorGUILayout.BeginVertical("box");
            foreach (SceneNoteCategory cat in Enum.GetValues(typeof(SceneNoteCategory)))
            {
                bool selected = categoryFilter.Contains(cat);
                bool toggle = GUILayout.Toggle(selected, cat.ToString());
                if (toggle && !selected) categoryFilter.Add(cat);
                if (!toggle && selected) categoryFilter.Remove(cat);
            }
            EditorGUILayout.EndVertical();
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        SceneNote[] notes = FindObjectsOfType<SceneNote>();

        foreach (var group in notes
                     .Where(n => string.IsNullOrEmpty(searchText) || n.noteText.ToLower().Contains(searchText.ToLower()))
                     .Where(n => !onlyIncomplete || !n.isCompleted)
                     .Where(n => categoryFilter.Count == 0 || categoryFilter.Contains(n.category))
                     .GroupBy(n => n.category))
        {
            GUIStyle categoryHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(4, 4, 8, 4)
            };

            GUILayout.Label(group.Key.ToString(), categoryHeaderStyle);

            foreach (var note in group.ToList())
            {
                note.gameObject.hideFlags = toggleVisibility ? HideFlags.None : HideFlags.HideInHierarchy;

                if (DrawNoteCard(note))
                    continue;

                GUILayout.Space(8);
            }
        }

        EditorGUILayout.EndScrollView();

        if (toggleVisibility)
            EditorApplication.RepaintHierarchyWindow();
    }

    private bool DrawNoteCard(SceneNote note)
    {
        bool deleted = false;

        GUIStyle boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 2, 2)
        };

        GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };

        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            fontSize = 12
        };

        GUIStyle completedStyle = new GUIStyle(EditorStyles.toggle);
        GUI.backgroundColor = note.isCompleted ? new Color(0.3f, 0.8f, 0.3f) : Color.white;

        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.BeginHorizontal();

        note.isCompleted = EditorGUILayout.Toggle(note.name, note.isCompleted, completedStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Focus", GUILayout.Width(60)))
        {
            Bounds bounds = new Bounds(note.transform.position, Vector3.one * 2f);
            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            Undo.DestroyObjectImmediate(note.gameObject);
            deleted = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);

        if (!deleted)
        {
            note.noteText = EditorGUILayout.TextArea(note.noteText, textAreaStyle, GUILayout.MinHeight(40));
        }

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = Color.white;
        return deleted;
    }
}
