using Godot;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private void BuildRegionInspector(MapEditorRegion region)
    {
        AddInspectorLineEdit("ID", region.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)), "Terrain region id.");
        AddRegionTypeEdit(region);
        AddShapeEdit(region.Shape);
        AddVectorEdit("Center", region.Center, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)), "Region center.");
        if (region.Shape == "circle")
        {
            AddFloatEdit("Radius", region.Radius, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionRadius(value)), "Circle radius.");
        }
        else
        {
            AddVectorEdit("Size", region.Size, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionSize(vector)), "Rectangle size.");
        }

        AddFlagEdit("Blocks Move", region.BlocksMovement, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(value, region.BlocksBuilding, region.AllowsBuilding)));
        AddFlagEdit("Blocks Build", region.BlocksBuilding, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(region.BlocksMovement, value, region.AllowsBuilding)));
        AddFlagEdit("Allows Build", region.AllowsBuilding, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(region.BlocksMovement, region.BlocksBuilding, value)));
        AddInspectorLineEdit("Tags", string.Join(", ", region.Tags), text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedTags(ParseTags(text))), "Comma-separated region tags.");
    }

    private void AddRegionTypeEdit(MapEditorRegion region)
    {
        var option = new OptionButton
        {
            TooltipText = "Apply a terrain-region type preset.",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        foreach (var regionType in MapEditorSession.KnownRegionTypes)
        {
            option.AddItem(regionType);
        }

        option.AddItem("Other...");
        var selectedIndex = MapEditorSession.KnownRegionTypes
            .Select((value, index) => (value, index))
            .FirstOrDefault(item => item.value == region.RegionType).index;
        if (MapEditorSession.KnownRegionTypes.Contains(region.RegionType, StringComparer.Ordinal))
        {
            option.Select(selectedIndex);
        }
        else
        {
            option.Select(MapEditorSession.KnownRegionTypes.Count);
        }

        option.ItemSelected += index =>
        {
            if (_refreshingMapEditorUi || _mapEditorOverlay is null || index >= MapEditorSession.KnownRegionTypes.Count)
            {
                return;
            }

            ApplyMapEditorResult(_mapEditorOverlay.SetSelectedRegionType(MapEditorSession.KnownRegionTypes[(int)index]));
        };
        AddInspectorRow("Type", option);
        AddInspectorLineEdit("Custom", region.RegionType, text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionType(text)), "Custom terrain-region type.");
    }

    private void AddShapeEdit(string shape)
    {
        var option = new OptionButton
        {
            TooltipText = "Change terrain-region shape.",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        option.AddItem("rect");
        option.AddItem("circle");
        option.Select(shape == "circle" ? 1 : 0);
        option.ItemSelected += index =>
        {
            if (_refreshingMapEditorUi || _mapEditorOverlay is null)
            {
                return;
            }

            ApplyMapEditorResult(_mapEditorOverlay.SetSelectedRegionShape(index == 1 ? "circle" : "rect"));
        };
        AddInspectorRow("Shape", option);
    }

    private void AddFloatEdit(string label, float value, Action<float> commit, string tooltip)
    {
        AddInspectorLineEdit(label, FormatEditorFloat(value), text =>
        {
            if (TryParseEditorFloat(text, out var parsed))
            {
                commit(parsed);
            }
            else
            {
                _lastActionMessage = $"{label} requires a numeric value.";
                RefreshMapEditorPanel();
            }
        }, tooltip);
    }

    private void AddFlagEdit(string label, bool value, Action<bool> commit)
    {
        var checkBox = new CheckBox
        {
            Text = string.Empty,
            ButtonPressed = value,
            TooltipText = label,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        checkBox.Toggled += enabled =>
        {
            if (!_refreshingMapEditorUi)
            {
                commit(enabled);
            }
        };
        AddInspectorRow(label, checkBox);
    }
}
