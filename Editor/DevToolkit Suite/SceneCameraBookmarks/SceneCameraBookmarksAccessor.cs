using UnityEditor;
using UnityEngine;

public static class SceneCameraBookmarksAccessor
{
    public static void AddBookmark(string name)
    {
        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            CameraBookmarkStorage.bookmarks.Add(new CameraBookmark
            {
                name = string.IsNullOrWhiteSpace(name)
                    ? $"Bookmark {CameraBookmarkStorage.bookmarks.Count + 1}"
                    : name,
                position = view.camera.transform.position,
                rotation = view.camera.transform.rotation
            });

            SceneCameraBookmarksOverlay.Refresh(); // Overlay'i yenile
            SceneCameraBookmarks.ForceRepaint();
        }
    }
}
