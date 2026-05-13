using Stratezone.Simulation.Tools;

public partial class Main
{
    private void BuildObjectInspector(MapEditorObject mapObject)
    {
        AddInspectorLineEdit("ID", mapObject.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)), "Map object id.");
        AddReadOnlyText("Type", mapObject.ObjectType, "Map object type.");
        AddReadOnlyText("Shape", mapObject.Shape, "Map object shape.");
        AddVectorEdit("Center", mapObject.Center, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)), "Map object center.");
        AddReadOnlyText(
            mapObject.Shape == "circle" ? "Radius" : "Size",
            mapObject.Shape == "circle"
                ? FormatEditorFloat(mapObject.Radius)
                : $"{FormatEditorFloat(mapObject.Size.X)} x {FormatEditorFloat(mapObject.Size.Y)}",
            "Map object dimensions.");
        AddReadOnlyText("Max HP", FormatEditorFloat(mapObject.MaxHealth), "Map object health.");
        AddInspectorLineEdit("Tags", string.Join(", ", mapObject.Tags), text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedTags(ParseTags(text))), "Comma-separated object tags.");
    }
}
