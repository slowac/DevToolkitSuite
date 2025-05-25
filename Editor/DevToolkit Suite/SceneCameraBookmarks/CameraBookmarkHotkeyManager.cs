using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CameraBookmarkHotkeyManager
{
    static CameraBookmarkHotkeyManager()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        Event e = Event.current;
        
        if (e.type == EventType.KeyDown && !e.control && !e.alt && !e.shift)
        {
            foreach (var bookmark in CameraBookmarkStorage.bookmarks)
            {
                if (bookmark.hotkey != KeyCode.None && e.keyCode == bookmark.hotkey)
                {
                    NavigateToBookmark(bookmark);
                    e.Use(); // Consume the event
                    break;
                }
            }
        }
    }

    public static void NavigateToBookmark(CameraBookmark bookmark)
    {
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.LookAt(bookmark.position, bookmark.rotation);
            
            // Optional: Show a brief notification
            sceneView.ShowNotification(new GUIContent($"📍 Navigated to: {bookmark.name}"));
        }
    }
} 