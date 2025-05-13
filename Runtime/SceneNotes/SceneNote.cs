using UnityEngine;

public class SceneNote : MonoBehaviour
{
    [TextArea]
    public string noteText;

    public bool isCompleted = false;

    public SceneNoteCategory category = SceneNoteCategory.Note;
}
