using UnityEditor;
using UnityEngine;

public static class SceneNoteMenu
{
    [MenuItem("GameObject/Scene Note/Create Note", false, 10)]
    static void CreateSceneNote(MenuCommand menuCommand)
    {
        if (menuCommand.context is GameObject parentGO)
        {
            // Yeni obje oluştur, parent'a ata
            GameObject go = new GameObject("SceneNote");
            go.transform.SetParent(parentGO.transform);
            go.transform.localPosition = Vector3.zero;

            var sceneNote = go.AddComponent<SceneNote>();
            sceneNote.noteText = "";
            sceneNote.isCompleted = false;
            sceneNote.category = SceneNoteCategory.Note;

            Undo.RegisterCreatedObjectUndo(go, "Create Scene Note");
            Selection.activeObject = go;
        }
        else
        {
            // Boş sahneye tıklandıysa klasik konumlandırma
            SceneNoteUtility.CreateNoteAtSceneView("New Note", false, SceneNoteCategory.Note);
        }
    }
}
