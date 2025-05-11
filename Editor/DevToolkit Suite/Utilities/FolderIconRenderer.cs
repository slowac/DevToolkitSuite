using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace DevToolkit_Suite
{
    [InitializeOnLoad]
    public static class FolderIconRenderer
    {
        private static Dictionary<string, Texture2D> iconByFolder = new Dictionary<string, Texture2D>();

        static FolderIconRenderer()
        {
            RefreshDictionary();
            EditorApplication.projectWindowItemOnGUI += DrawFolderIcon;
        }

        public static void RefreshDictionary()
        {
            iconByFolder.Clear();

            string[] guids = AssetDatabase.FindAssets("t:FolderIconSO", new[] { "Packages/com.ogbcrew.devtoolkitsuite/Editor/DevToolkit Suite/AutoFolderCreator/Data" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<FolderIconSO>(path);
                if (so == null || so.icon == null) continue;

                foreach (var folderName in so.folderNames)
                {
                    if (!iconByFolder.ContainsKey(folderName))
                    {
                        iconByFolder.Add(folderName, so.icon);
                    }
                }
            }
        }

        private static void DrawFolderIcon(string guid, Rect rect)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path)) return;

            string folderName = Path.GetFileName(path);
            if (!iconByFolder.TryGetValue(folderName, out Texture2D icon)) return;
            if (Event.current.type != EventType.Repaint) return;

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
}
