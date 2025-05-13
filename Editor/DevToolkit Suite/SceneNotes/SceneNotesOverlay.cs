using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Scene Notes")]
public class SceneNotesOverlay : Overlay
{
    private SceneNoteCategory selectedCategory = SceneNoteCategory.Note;

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement
        {
            style =
            {
                paddingTop = 6,
                paddingBottom = 6,
                paddingLeft = 6,
                paddingRight = 6
            }
        };

        // Dropdown: Category seçimi
        var categoryField = new EnumField("Category", selectedCategory);
        categoryField.RegisterValueChangedCallback(evt =>
        {
            selectedCategory = (SceneNoteCategory)evt.newValue;
        });
        root.Add(categoryField);

        // Add Note butonu
        var addButton = new Button(() =>
        {
            var selected = Selection.activeGameObject;

            if (selected != null)
            {
                GameObject go = new GameObject("SceneNote");
                go.transform.SetParent(selected.transform);
                go.transform.localPosition = Vector3.zero;

                var sceneNote = go.AddComponent<SceneNote>();
                sceneNote.noteText = "";
                sceneNote.isCompleted = false;
                sceneNote.category = selectedCategory;

                Undo.RegisterCreatedObjectUndo(go, "Create Scene Note");
                Selection.activeObject = go;
            }
            else
            {
                SceneNoteUtility.CreateNoteAtSceneView("", false, selectedCategory);
            }
        })
        {
            text = "Add Note"
        };
        root.Add(addButton);

        // Refresh butonu
        var refreshButton = new Button(() =>
        {
            SceneView.RepaintAll();
        })
        {
            text = "Refresh"
        };
        root.Add(refreshButton);

        return root;
    }
}
