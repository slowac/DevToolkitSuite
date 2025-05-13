using UnityEditor;
using UnityEngine;

public static class SceneNoteIconLoader
{
    public static Texture2D LoadIcon(SceneNote note)
    {
        if (note == null) return null;

        string iconName = GetIconName(note);
        string path = $"Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/SceneNotes/Editor Default Resources/{iconName}.png";

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static string GetIconName(SceneNote note)
    {
        if (note.isCompleted)
            return "note_iscompleted";

        return note.category switch
        {
            SceneNoteCategory.Idea => "note_idea",
            SceneNoteCategory.Reminder => "note_reminder",
            SceneNoteCategory.Bug => "note_bug",
            SceneNoteCategory.Warning => "note_warning",
            _ => "note_icon" // default Note
        };
    }
}
