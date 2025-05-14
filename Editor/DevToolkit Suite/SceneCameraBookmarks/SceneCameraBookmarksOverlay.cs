#if UNITY_2021_2_OR_NEWER
using UnityEditor.Overlays;
using UnityEngine;
using UnityEditor; 
using UnityEngine.UIElements;
using System.Collections.Generic;

[Overlay(typeof(SceneView), "Scene Camera Bookmarks")]
public class SceneCameraBookmarksOverlay : Overlay
{
    private static List<CameraBookmark> bookmarks => CameraBookmarkStorage.bookmarks;

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement
        {
            style = { paddingTop = 6, paddingBottom = 6, paddingLeft = 6, paddingRight = 6 }
        };

        var nameField = new TextField("Name") { value = "" };
        root.Add(nameField);

        var addButton = new Button(() =>
        {
            SceneCameraBookmarksAccessor.AddBookmark(nameField.value);
            nameField.value = "";
            SceneView.RepaintAll();
        })
        {
            text = "Add Bookmark"
        };
        root.Add(addButton);

        if (bookmarks.Count > 0)
        {
            foreach (var bm in bookmarks)
            {
                var card = new VisualElement
                {
                    style = {
                        paddingTop = 4,
                        paddingBottom = 4,
                        paddingLeft = 6,
                        paddingRight = 6,
                        marginBottom = 4,
                        backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f),
                        flexDirection = FlexDirection.Row
                    }
                };

                var label = new Label(bm.name)
                {
                    style = {
                        flexGrow = 1,
                        unityFontStyleAndWeight = FontStyle.Bold,
                        fontSize = 12,
                        color = Color.white
                    }
                };

                var goButton = new Button(() =>
                {
                    SceneView.lastActiveSceneView.LookAt(bm.position, bm.rotation);
                }) { text = "Go" };

                card.Add(label);
                card.Add(goButton);

                root.Add(card);
            }
        }

        return root;
    }
}

public static class SceneCameraBookmarksAccessor
{
    public static void AddBookmark(string name)
    {
        var view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            CameraBookmarkStorage.bookmarks.Add(new CameraBookmark
            {
                name = string.IsNullOrWhiteSpace(name) ? $"Bookmark {CameraBookmarkStorage.bookmarks.Count + 1}" : name,
                position = view.camera.transform.position,
                rotation = view.camera.transform.rotation
            });
        }
    }
}
#endif
