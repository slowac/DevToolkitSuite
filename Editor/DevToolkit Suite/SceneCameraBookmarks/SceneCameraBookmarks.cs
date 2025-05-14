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

    [MenuItem("Tools/DevToolkit Suite/Scene Camera Bookmarks")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneCameraBookmarks>("Scene Camera Bookmarks");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter
        };

        GUILayout.Space(10);
        GUILayout.Label("🎥 Scene Camera Bookmarks", titleStyle);
        GUILayout.Space(15);

        // Add Section
        EditorGUILayout.BeginHorizontal("box");
        GUILayout.Label("Name", GUILayout.Width(40));
        newBookmarkName = EditorGUILayout.TextField(newBookmarkName, GUILayout.Height(22));

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Current View", GUILayout.Height(22), GUILayout.Width(150)))
        {
            AddCurrentView();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Bookmarks List
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (bookmarks.Count == 0)
        {
            EditorGUILayout.HelpBox("No bookmarks yet. Add one using the button above.", MessageType.Info);
        }

        for (int i = 0; i < bookmarks.Count; i++)
        {
            var bookmark = bookmarks[i];

            GUIStyle boxStyle = new GUIStyle("box")
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(6, 6, 6, 6)
            };

            GUIStyle headerStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label($"📍 {bookmark.name}", headerStyle);
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f); // Aqua
            if (GUILayout.Button("Go To", GUILayout.Height(24), GUILayout.Width(70)))
            {
                SceneView.lastActiveSceneView.LookAt(bookmark.position, bookmark.rotation);
            }

            GUI.backgroundColor = Color.yellow;
            if (renamingIndex == i)
            {
                renamingBuffer = EditorGUILayout.TextField(renamingBuffer, GUILayout.Width(120), GUILayout.Height(22));

                if (GUILayout.Button("Save", GUILayout.Height(22), GUILayout.Width(50)))
                {
                    bookmark.name = renamingBuffer;
                    renamingIndex = -1;
                }

                if (GUILayout.Button("Cancel", GUILayout.Height(22), GUILayout.Width(60)))
                {
                    renamingIndex = -1;
                }
            }
            else
            {
                if (GUILayout.Button("Rename", GUILayout.Height(24), GUILayout.Width(80)))
                {
                    renamingIndex = i;
                    renamingBuffer = bookmark.name;
                }
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Delete", GUILayout.Height(24), GUILayout.Width(70)))
            {
                bookmarks.RemoveAt(i);
                i--;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
                continue;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();
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
        }
    }
}
