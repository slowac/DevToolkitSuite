using UnityEditor;
using UnityEngine;

public class SceneNotesWindow : EditorWindow
{
    [MenuItem("Tools/Editor Pack Ultimate/Scene Notes")]
    public static void ShowWindow()
    {
        var window = GetWindow<SceneNotesWindow>("Scene Notes");
        window.minSize = new Vector2(400, 500);
    }

    private Vector2 scroll;

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

        scroll = EditorGUILayout.BeginScrollView(scroll);

        SceneNote[] notes = FindObjectsOfType<SceneNote>();
        foreach (var note in notes)
        {
            DrawNoteCard(note);
            GUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawNoteCard(SceneNote note)
    {
        // Kart çerçevesi
        GUIStyle boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(4, 4, 2, 2)
        };

        // Başlık + ikon
        GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };

        // Yazı stili
        GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea)
        {
            wordWrap = true,
            fontSize = 12
        };

        // Renkli durum kutusu
        GUIStyle completedStyle = new GUIStyle(EditorStyles.toggle);
        GUI.backgroundColor = note.isCompleted ? new Color(0.3f, 0.8f, 0.3f) : Color.white;

        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.BeginHorizontal();
        note.isCompleted = EditorGUILayout.Toggle(note.name, note.isCompleted, completedStyle);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Focus", GUILayout.Width(60)))
        {
            Bounds bounds = new Bounds(note.transform.position, Vector3.one * 2f); // Küçük bir alan
            SceneView.lastActiveSceneView.Frame(bounds, false);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {
            Undo.DestroyObjectImmediate(note.gameObject);
            return;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(4);
        note.noteText = EditorGUILayout.TextArea(note.noteText, textAreaStyle, GUILayout.MinHeight(40));

        EditorGUILayout.EndVertical();

        // Renk sıfırla
        GUI.backgroundColor = Color.white;
    }
}
