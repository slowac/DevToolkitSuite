#if UNITY_2021_2_OR_NEWER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Scene Camera Bookmarks")]
public class SceneCameraBookmarksOverlay : Overlay
{
    private static SceneCameraBookmarksOverlay instance;
    private VisualElement contentRoot;

    public override VisualElement CreatePanelContent()
    {
        instance = this;
        contentRoot = new VisualElement();
        Refresh();
        return contentRoot;
    }

    private VisualElement BuildContent()
    {
        var root = new VisualElement
        {
            style = {
                paddingTop = 6,
                paddingBottom = 6,
                paddingLeft = 6,
                paddingRight = 6
            }
        };

        var nameField = new TextField("Name") { value = "" };
        root.Add(nameField);

        var addButton = new Button(() =>
        {
            SceneCameraBookmarksAccessor.AddBookmark(nameField.value);
            nameField.value = "";
            Refresh(); // Paneli yeniden oluþtur
        })
        {
            text = "Add Bookmark"
        };
        root.Add(addButton);

        if (CameraBookmarkStorage.bookmarks.Count > 0)
        {
            foreach (var bm in CameraBookmarkStorage.bookmarks)
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
                })
                {
                    text = "Go"
                };

                card.Add(label);
                card.Add(goButton);

                root.Add(card);
            }
        }

        return root;
    }

    public static void Refresh()
    {
        if (instance == null || instance.contentRoot == null)
            return;

        instance.contentRoot.Clear();
        instance.contentRoot.Add(instance.BuildContent());
    }
}
#endif
