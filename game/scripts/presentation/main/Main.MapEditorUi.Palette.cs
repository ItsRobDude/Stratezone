using Godot;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private void BuildMapEditorPalette()
    {
        if (_mapEditorPaletteList is null)
        {
            return;
        }

        ClearChildren(_mapEditorPaletteList);
        _mapEditorToolButtons.Clear();
        AddToolButton("select", L("ui.editor.tool.select.tooltip"), MapEditorToolMode.Select);
        AddToolButton("marker", L("ui.editor.tool.marker.tooltip"), MapEditorToolMode.AddMarker);
        AddToolButton("rect", L("ui.editor.tool.rect.tooltip"), MapEditorToolMode.AddRectRegion);
        AddToolButton("circle", L("ui.editor.tool.circle.tooltip"), MapEditorToolMode.AddCircleRegion);
        AddToolButton("delete", L("ui.editor.tool.delete.tooltip"), MapEditorToolMode.Delete);

        AddPaletteSpacer();
        _mapEditorSnapToggle = AddPaletteIconButton("snap", L("ui.editor.tool.snap.tooltip"), () =>
        {
            ToggleMapEditorSnap();
        });

        AddPaletteSpacer();
        AddPaletteIconButton("undo", L("ui.editor.tool.undo.tooltip"), UndoMapEditor);
        AddPaletteIconButton("redo", L("ui.editor.tool.redo.tooltip"), RedoMapEditor);
        AddPaletteIconButton("duplicate", L("ui.editor.tool.duplicate.tooltip"), DuplicateMapEditorSelection);
        AddPaletteIconButton("delete", L("ui.editor.tool.delete_preview.tooltip"), BeginMapEditorDelete);
    }

    private void AddToolButton(string iconId, string tooltip, MapEditorToolMode mode)
    {
        var button = AddPaletteIconButton(iconId, tooltip, () => SetMapEditorToolMode(mode));
        _mapEditorToolButtons[mode] = button;
    }

    private MapEditorIconButton AddPaletteIconButton(string iconId, string tooltip, Action action)
    {
        var button = new MapEditorIconButton();
        button.Configure(iconId, tooltip, active: false, action);
        button.ApplyUiScale(_uiScale);
        _mapEditorPaletteList?.AddChild(button);
        return button;
    }

    private void AddPaletteSpacer()
    {
        _mapEditorPaletteList?.AddChild(new Control
        {
            CustomMinimumSize = new Vector2(1.0f, 8.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private void SetMapEditorToolMode(MapEditorToolMode mode)
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        _mapEditorOverlay.SetToolMode(mode);
        ApplyMapEditorCursor(mode);
        _lastActionMessage = $"Map editor tool: {mode}.";
        ClearMapEditorPendingDecisions();
        RefreshMapEditorPanel();
    }

    private void ToggleMapEditorSnap()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        _mapEditorOverlay.ToggleSnap();
        _lastActionMessage = _mapEditorOverlay.SnapEnabled ? "Map editor snap enabled." : "Map editor snap disabled.";
        RefreshMapEditorPanel();
    }

    private void UndoMapEditor()
    {
        if (_mapEditorOverlay?.Undo() == true)
        {
            ClearMapEditorPendingDecisions();
            ClearPendingMapEditorSave("Map editor save preview cleared after undo.");
        }

        RefreshMapEditorPanel();
    }

    private void RedoMapEditor()
    {
        if (_mapEditorOverlay?.Redo() == true)
        {
            ClearMapEditorPendingDecisions();
            ClearPendingMapEditorSave("Map editor save preview cleared after redo.");
        }

        RefreshMapEditorPanel();
    }

    private void DuplicateMapEditorSelection()
    {
        ApplyMapEditorResult(_mapEditorOverlay?.DuplicateSelected());
    }

    private void BeginMapEditorDelete()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        _pendingMapEditorDelete = _mapEditorOverlay.CreateDeletePreview();
        _lastActionMessage = _pendingMapEditorDelete is null
            ? "Select a marker, region, or map object before deleting."
            : _pendingMapEditorDelete.Summary;
        RefreshMapEditorPanel();
    }

    private void ConfirmMapEditorDelete()
    {
        ApplyMapEditorResult(_mapEditorOverlay?.DeleteSelected());
        _pendingMapEditorDelete = null;
    }

    private void ApplyMapEditorResult(MapEditorEditResult? result)
    {
        if (result is null)
        {
            return;
        }

        _lastActionMessage = result.Warnings.Count > 0
            ? $"{result.Message} {string.Join(" ", result.Warnings)}"
            : result.Message;
        if (result.Changed)
        {
            ClearMapEditorPendingDecisions();
            ClearPendingMapEditorSave("Map editor save preview cleared after edit.");
        }

        RefreshMapEditorPanel();
    }
}
