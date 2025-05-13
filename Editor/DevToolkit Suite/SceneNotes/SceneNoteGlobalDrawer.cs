using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneNoteGlobalDrawer
{
    static SceneNoteGlobalDrawer()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sceneView)
    {
        SceneNote[] notes = Object.FindObjectsOfType<SceneNote>();
        Camera cam = sceneView.camera;

        foreach (var note in notes)
        {
            if (note == null || note.gameObject == null)
                continue;

            float distance = Vector3.Distance(cam.transform.position, note.transform.position);
            Vector2 screenPos = HandleUtility.WorldToGUIPoint(note.transform.position);

            Handles.BeginGUI();

            if (distance < 20f)
            {
                GUIStyle style = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    normal = { textColor = Color.white },
                    padding = new RectOffset(10, 10, 6, 6)
                };

                float maxWidth = 160f;
                Vector2 size = style.CalcSize(new GUIContent(note.noteText));
                size.x = Mathf.Min(size.x, maxWidth);
                size.y = style.CalcHeight(new GUIContent(note.noteText), maxWidth);

                GUI.Box(new Rect(screenPos.x - size.x / 2f, screenPos.y - size.y / 2f, size.x, size.y), note.noteText, style);
            }
            else
            {
                // ✅ Kategoriden ikon yükle
                Texture2D noteIcon = SceneNoteIconLoader.LoadIcon(note);
                if (noteIcon != null)
                {
                    float iconSize = 24f;
                    GUI.DrawTexture(
                        new Rect(screenPos.x - iconSize / 2f, screenPos.y - iconSize / 2f, iconSize, iconSize),
                        noteIcon,
                        ScaleMode.ScaleToFit,
                        true
                    );
                }
            }

            Handles.EndGUI();
        }
    }
}
