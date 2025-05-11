using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class CustomFolder
{
    static CustomFolder()
    {
        EditorApplication.projectWindowItemOnGUI += DrawCustomFolderIcons;
    }

    private static void DrawCustomFolderIcons(string guid, Rect rect)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

        string folderName = Path.GetFileName(path);
        if (!DevToolkit_Suite.FolderIconManager.TryGetFolderIcon(folderName, out Texture2D icon)) return;
        if (icon == null || Event.current.type != EventType.Repaint) return;

        /*Rect imageRect = rect.height > 20
            ? new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.width + 2)
            : new Rect(rect.x + 2, rect.y - 1, rect.height + 2, rect.height + 2);*/

        Rect imageRect;

        if (rect.height > 20)
        {
            imageRect = new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.width + 2);
        }
        else if (rect.x > 20)
        {
            imageRect = new Rect(rect.x - 1, rect.y - 1, rect.height + 2, rect.height + 2);
        }
        else
        {
            imageRect = new Rect(rect.x + 2, rect.y - 1, rect.height + 2, rect.height + 2);
        }

        GUI.DrawTexture(imageRect, icon);
    }
}
