using UnityEditor;
using UnityEngine;

public static class SceneNoteUtility
{
    /// <summary>
    /// SceneView bakış yönüne göre not oluşturur
    /// </summary>
    public static GameObject CreateNoteAtSceneView(string noteText, bool isCompleted, SceneNoteCategory category)
    {
        Vector3 position = GetSpawnPositionFromSceneView();
        return CreateNote(noteText, isCompleted, category, position);
    }

    /// <summary>
    /// Mouse altına raycast ile not yerleştirir (UI üzerinden çağrılabilir)
    /// </summary>
    public static GameObject CreateNoteUnderCursor(string noteText, bool isCompleted, SceneNoteCategory category)
    {
        Vector3 position = GetRaycastHitPosition() ?? GetSpawnPositionFromSceneView(); // Fallback
        return CreateNote(noteText, isCompleted, category, position);
    }

    /// <summary>
    /// Belirli pozisyona özel note oluştur
    /// </summary>
    public static GameObject CreateNote(string noteText, bool isCompleted, SceneNoteCategory category, Vector3 position)
    {
        GameObject go = new GameObject("SceneNote");
        SceneNote sceneNote = go.AddComponent<SceneNote>();

        sceneNote.noteText = noteText;
        sceneNote.isCompleted = isCompleted;
        sceneNote.category = category;
        go.transform.position = position;

        Undo.RegisterCreatedObjectUndo(go, "Create Scene Note");
        Selection.activeObject = go;

        return go;
    }

    /// <summary>
    /// SceneView kamerasının baktığı yöne göre not spawn pozisyonu
    /// </summary>
    public static Vector3 GetSpawnPositionFromSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;

        if (sceneView == null)
            return Vector3.zero;

        if (sceneView.in2DMode)
        {
            Vector3 pos = sceneView.pivot;
            pos.z = 0f;
            return pos;
        }
        else
        {
            return sceneView.pivot + sceneView.rotation * Vector3.forward * 10f;
        }
    }

    /// <summary>
    /// Mouse altındaki yüzeyi raycast ile tespit eder
    /// </summary>
    public static Vector3? GetRaycastHitPosition()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null || sceneView.camera == null)
            return null;

        Event e = Event.current;
        if (e == null)
            return null;

        Vector2 mousePos = e.mousePosition;
        mousePos.y = sceneView.camera.pixelHeight - mousePos.y; // UI → ekran dönüşümü

        Ray ray = sceneView.camera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return hit.point;
        }

        return null;
    }
}
