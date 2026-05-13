using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private const float MapEditorNudgeWorldUnits = 10.0f;

    private MapEditorOverlay? _mapEditorOverlay;
    private Panel? _mapEditorPanel;
    private Label? _mapEditorLabel;
    private Panel? _mapEditorPalettePanel;
    private VBoxContainer? _mapEditorPaletteList;
    private Panel? _mapEditorInspectorPanel;
    private VBoxContainer? _mapEditorInspectorList;
    private PopupMenu? _mapEditorContextMenu;
    private readonly Dictionary<MapEditorToolMode, MapEditorIconButton> _mapEditorToolButtons = [];
    private readonly List<string> _mapEditorMissionPickerIds = [];
    private OptionButton? _mapEditorMissionPicker;
    private MapEditorIconButton? _mapEditorSnapToggle;
    private bool _mapEditorEnabled;
    private string _lastMapEditorExportSummary = "No export yet.";
    private MapEditorSavePreview? _pendingMapEditorSave;
    private MapEditorDeletePreview? _pendingMapEditorDelete;
    private string? _pendingMapEditorMissionSwitchId;
    private string _mapEditorSavePreviewSummary = string.Empty;
    private bool _refreshingMapEditorUi;
    private bool _mapEditorFocusIdOnRefresh;

    private void SetupMapEditorOverlay()
    {
        if (_worldRoot is null || _uiLayoutRoot is null)
        {
            return;
        }

        _mapEditorOverlay = new MapEditorOverlay
        {
            Name = "MapEditorOverlay",
            ZIndex = 35,
            Visible = false
        };
        _worldRoot.AddChild(_mapEditorOverlay);
        UpdateMapEditorOverlayData();

        _mapEditorPanel = new Panel
        {
            Name = "MapEditorPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _uiLayoutRoot.AddChild(_mapEditorPanel);

        _mapEditorLabel = new Label
        {
            Name = "MapEditorLabel",
            ThemeTypeVariation = "LabelSecondary",
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _mapEditorPanel.AddChild(_mapEditorLabel);
        SetupMapEditorToolPanels();
        SetupMapEditorContextMenu();
        ApplyMapEditorPanelScale();
        RefreshMapEditorPanel();
    }

    private void UpdateMapEditorOverlayData()
    {
        try
        {
            _mapEditorOverlay?.Load(_activeMission, _simulation?.Map, _catalog, _simulation);
        }
        catch (Exception exception)
        {
            LogMapEditorException("update_overlay_data", exception);
        }
    }

    private bool HandleMapEditorKey(InputEventKey keyEvent)
    {
        try
        {
            var keycode = keyEvent.Keycode;
            if (keycode == Key.F5)
            {
                if (_mapEditorEnabled && keyEvent.ShiftPressed)
                {
                    ReinitializeMapEditorMissionFromEdits(null);
                    return true;
                }

                ToggleMapEditor();
                return true;
            }

            if (!_mapEditorEnabled)
            {
                return false;
            }

            if (keycode == Key.S && (keyEvent.CtrlPressed || keyEvent.MetaPressed))
            {
                HandleMapEditorSaveShortcut();
                return true;
            }

            if (keycode == Key.Z && (keyEvent.CtrlPressed || keyEvent.MetaPressed))
            {
                if (keyEvent.ShiftPressed)
                {
                    RedoMapEditor();
                }
                else
                {
                    UndoMapEditor();
                }

                return true;
            }

            if (keycode == Key.Y && (keyEvent.CtrlPressed || keyEvent.MetaPressed))
            {
                RedoMapEditor();
                return true;
            }

            switch (keycode)
            {
                case Key.Escape:
                    if (_pendingMapEditorSave is not null)
                    {
                        ClearPendingMapEditorSave("Save preview canceled.");
                        RefreshMapEditorPanel();
                        return true;
                    }

                    ToggleMapEditor();
                    return true;
                case Key.R:
                    ClearPendingMapEditorSave("Map editor reloaded from current mission data.");
                    UpdateMapEditorOverlayData();
                    _lastActionMessage = "Map editor reloaded from current mission data.";
                    RefreshMapEditorPanel();
                    return true;
                case Key.P:
                    ToggleMapEditorPathingLayer();
                    return true;
                case Key.G:
                    ToggleMapEditorSnap();
                    return true;
                case Key.Delete:
                    BeginMapEditorDelete();
                    return true;
                case Key.E:
                    ExportMapEditorSelection();
                    return true;
                case Key.J:
                    ExportMapEditorAll();
                    return true;
                case Key.Tab:
                    return CycleMapEditorSelection(forward: true);
                case Key.Q:
                    return CycleMapEditorSelection(forward: false);
                case Key.Up:
                    return NudgeMapEditorSelection(new Vector2(0, -MapEditorNudgeWorldUnits));
                case Key.Down:
                    return NudgeMapEditorSelection(new Vector2(0, MapEditorNudgeWorldUnits));
                case Key.Left:
                    return NudgeMapEditorSelection(new Vector2(-MapEditorNudgeWorldUnits, 0));
                case Key.Right:
                    return NudgeMapEditorSelection(new Vector2(MapEditorNudgeWorldUnits, 0));
                default:
                    return true;
            }
        }
        catch (Exception exception)
        {
            LogMapEditorException($"key_{keyEvent.Keycode}", exception);
            return true;
        }
    }

    private bool HandleMapEditorMouse(InputEvent inputEvent)
    {
        try
        {
            if (!_mapEditorEnabled || _mapEditorOverlay is null)
            {
                return false;
            }

            if (inputEvent is InputEventMouseMotion)
            {
                var beforeRevision = _mapEditorOverlay.Revision;
                _mapEditorOverlay.UpdatePointerAction(GetGlobalMousePosition());
                if (_mapEditorOverlay.Revision != beforeRevision)
                {
                    ClearMapEditorPendingDecisions();
                    ClearPendingMapEditorSave("Map editor save preview cleared after edit.");
                    RefreshMapEditorPanel();
                }

                return true;
            }

            if (inputEvent is not InputEventMouseButton mouseButton)
            {
                return true;
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    var beforeRevision = _mapEditorOverlay.Revision;
                    if (!_mapEditorOverlay.BeginPointerAction(GetGlobalMousePosition(), out var deletePreview))
                    {
                        _mapEditorOverlay.LogWarning("mouse_select", $"No marker or region selected at {GetGlobalMousePosition()}.");
                    }

                    if (deletePreview is not null)
                    {
                        _pendingMapEditorDelete = deletePreview;
                    }

                    if (_mapEditorOverlay.Revision != beforeRevision)
                    {
                        ClearMapEditorPendingDecisions();
                        ClearPendingMapEditorSave("Map editor save preview cleared after edit.");
                    }

                    RefreshMapEditorPanel();
                }
                else
                {
                    var beforeRevision = _mapEditorOverlay.Revision;
                    _mapEditorOverlay.EndPointerAction(GetGlobalMousePosition());
                    if (_mapEditorOverlay.Revision != beforeRevision)
                    {
                        ClearMapEditorPendingDecisions();
                        ClearPendingMapEditorSave("Map editor save preview cleared after edit.");
                    }

                    RefreshMapEditorPanel();
                }

                return true;
            }

            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                if (_mapEditorOverlay.SelectAt(GetGlobalMousePosition()))
                {
                    ShowMapEditorContextMenu();
                }
                else
                {
                    _lastActionMessage = "Right-click a marker, region, or map object to open editor actions.";
                    RefreshMapEditorPanel();
                }

                return true;
            }

            return false;
        }
        catch (Exception exception)
        {
            LogMapEditorException("mouse_input", exception);
            return true;
        }
    }

    private void ToggleMapEditor()
    {
        if (!_mapEditorEnabled)
        {
            _mapEditorEnabled = true;
            _placementBuildingId = null;
            _placementGhost?.Clear();
            _selectionBoxView?.Clear();
            _mapEditorOverlay?.SetEditorEnabled(true);
            ApplyMapEditorModePresentation(enabled: true);
            if (_mapEditorPanel is not null)
            {
                _mapEditorPanel.Visible = true;
            }

            SetMapEditorToolPanelsVisible(true);
            _lastActionMessage = "Map editor active. Simulation paused; close editor to re-init play from current edits.";
            RefreshMapEditorPanel();
            return;
        }

        ReinitializeMapEditorMissionFromEdits(null);
        _mapEditorEnabled = false;
        _mapEditorOverlay?.SetEditorEnabled(false);
        ApplyMapEditorModePresentation(enabled: false);
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = false;
        }

        SetMapEditorToolPanelsVisible(false);
        ClearPendingMapEditorSave();
        ClearMapEditorPendingDecisions();
        _lastActionMessage = "Map editor closed; mission reinitialized from current edits.";
        RefreshMapEditorPanel();
    }

    private void ToggleMapEditorPathingLayer()
    {
        if (_mapEditorOverlay is null)
        {
            LogMapEditorWarning("toggle_pathing", "Map editor overlay is missing; cannot toggle pathing layer.");
            return;
        }

        _mapEditorOverlay.SetPathingDebugEnabled(!_mapEditorOverlay.PathingDebugEnabled);
        _lastActionMessage = _mapEditorOverlay.PathingDebugEnabled
            ? "Map editor pathing layer on."
            : "Map editor pathing layer off.";
        RefreshMapEditorPanel();
    }

    private void HandleMapEditorSaveShortcut()
    {
        if (_mapEditorOverlay is null)
        {
            LogMapEditorWarning("save", "Map editor overlay is missing; cannot save.");
            return;
        }

        var validationIssues = _mapEditorOverlay.ValidateContent();
        var validationErrors = validationIssues.Where(issue => issue.Severity == MapEditorValidationSeverity.Error).ToArray();
        if (validationErrors.Length > 0)
        {
            _lastMapEditorExportSummary = $"Save blocked: {validationErrors.Length} editor validation error(s). Fix the validation panel before writing JSON.";
            _lastActionMessage = _lastMapEditorExportSummary;
            _mapEditorSavePreviewSummary = string.Join(System.Environment.NewLine, validationErrors.Take(8).Select(issue => $"ERROR {issue.Code}: {issue.Message}"));
            _pendingMapEditorSave = null;
            RefreshMapEditorPanel();
            return;
        }

        if (_pendingMapEditorSave is not null)
        {
            if (_pendingMapEditorSave.SessionRevision != _mapEditorOverlay.Revision)
            {
                ClearPendingMapEditorSave("Save preview expired after editor changes. Press Ctrl+S again to preview the new diff.");
                RefreshMapEditorPanel();
                return;
            }

            try
            {
                var result = MapEditorPersistence.WritePreview(_pendingMapEditorSave);
                _lastMapEditorExportSummary = result.Message;
                _lastActionMessage = result.Message;
                _pendingMapEditorSave = null;
                ClearMapEditorPendingDecisions();
                _mapEditorSavePreviewSummary = string.Empty;
                GD.Print($"Map editor save: {result.Message}");
            }
            catch (Exception exception)
            {
                LogMapEditorException("save_write", exception);
            }

            RefreshMapEditorPanel();
            return;
        }

        try
        {
            var preview = _mapEditorOverlay.CreateSavePreview(_gameRoot);
            if (!preview.HasChanges)
            {
                _lastMapEditorExportSummary = "No map editor changes to save.";
                _lastActionMessage = _lastMapEditorExportSummary;
                _mapEditorSavePreviewSummary = string.Empty;
                RefreshMapEditorPanel();
                return;
            }

            _pendingMapEditorSave = preview;
            _lastMapEditorExportSummary = "Save preview ready. Press Ctrl+S again to write; Esc cancels.";
            _lastActionMessage = _lastMapEditorExportSummary;
            _mapEditorSavePreviewSummary = CompactDiffForPanel(preview.DiffText);
            GD.Print($"Map editor save preview:{System.Environment.NewLine}{preview.DiffText}");
        }
        catch (Exception exception)
        {
            LogMapEditorException("save_preview", exception);
        }

        RefreshMapEditorPanel();
    }

    private void StartMapEditorViewAtSelectedMarker()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        var markerPosition = _mapEditorOverlay.SelectedMarkerWorldPosition();
        var markerId = _mapEditorOverlay.SelectedId ?? "selected marker";
        if (markerPosition is null)
        {
            LogMapEditorWarning("start_view", "Select a mission marker before starting the editor view there.");
            return;
        }

        ReinitializeMapEditorMissionFromEdits(markerPosition.Value);
        _mapEditorEnabled = false;
        _mapEditorOverlay?.SetEditorEnabled(false);
        ApplyMapEditorModePresentation(enabled: false);
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = false;
        }

        SetMapEditorToolPanelsVisible(false);
        ClearPendingMapEditorSave();
        ClearMapEditorPendingDecisions();
        _lastActionMessage = $"Mission reinitialized from edits; camera centered on {markerId}.";
    }

    private void ReinitializeMapEditorMissionFromEdits(Vector2? cameraFocus)
    {
        if (!TryCreateEditedMissionRuntimeInputs(out var mission, out var map))
        {
            return;
        }

        ClearWorldViews();
        _selectedUnitEntityIds.Clear();
        _selectedBuildingEntityId = null;
        _placementBuildingId = null;
        _placementGhost?.Clear();
        SetupSimulation(mission, map);
        if (_mapEditorEnabled && _simulation is not null)
        {
            _simulation.PlayerFog.EditorReveal = true;
        }

        ResetCameraToMissionStart();
        if (cameraFocus is not null)
        {
            FocusCameraOn(cameraFocus.Value);
        }

        SyncWorldViews();
        UpdateHud();
        _lastMapEditorExportSummary = cameraFocus is null
            ? "Mission reinitialized from in-memory map editor edits."
            : $"Mission reinitialized from edits; camera centered on {_mapEditorOverlay?.SelectedId ?? "selected marker"}.";
        _lastActionMessage = _lastMapEditorExportSummary;
        _pendingMapEditorSave = null;
        _mapEditorSavePreviewSummary = string.Empty;
        RefreshMapEditorPanel();
    }

    private void ApplyMapEditorModePresentation(bool enabled)
    {
        if (_gameHudRoot is not null)
        {
            _gameHudRoot.Visible = !enabled;
        }

        if (_simulation is not null)
        {
            _simulation.PlayerFog.EditorReveal = enabled;
        }

        foreach (var unitView in _simUnitViews.Values)
        {
            unitView.LabelsSuppressed = enabled;
            unitView.ShowAlwaysOnLabel = !enabled && _debugLabelsEnabled;
        }

        foreach (var buildingView in _buildingViews.Values)
        {
            buildingView.LabelsSuppressed = enabled;
            buildingView.ShowAlwaysOnLabel = !enabled && _debugLabelsEnabled;
        }

        if (enabled)
        {
            ApplyMapEditorCursor(_mapEditorOverlay?.ToolMode ?? MapEditorToolMode.Select);
        }
        else
        {
            ApplyMapEditorCursor(MapEditorToolMode.Select);
        }

        if (_missionCalloutView is not null)
        {
            _missionCalloutView.Visible = !enabled;
        }

        _fogOfWarView?.QueueRedraw();
        SyncWorldViews();
    }

    private void ApplyMapEditorCursor(MapEditorToolMode mode)
    {
        var cursor = mode switch
        {
            MapEditorToolMode.AddMarker or MapEditorToolMode.AddRectRegion or MapEditorToolMode.AddCircleRegion => Input.CursorShape.Cross,
            MapEditorToolMode.Delete => Input.CursorShape.Forbidden,
            _ => Input.CursorShape.Arrow
        };
        Input.SetDefaultCursorShape(cursor);
        DisplayServer.CursorSetShape(cursor switch
        {
            Input.CursorShape.Cross => DisplayServer.CursorShape.Cross,
            Input.CursorShape.Forbidden => DisplayServer.CursorShape.Forbidden,
            _ => DisplayServer.CursorShape.Arrow
        });
        if (_uiLayoutRoot is not null)
        {
            _uiLayoutRoot.MouseDefaultCursorShape = cursor switch
            {
                Input.CursorShape.Cross => Control.CursorShape.Cross,
                Input.CursorShape.Forbidden => Control.CursorShape.Forbidden,
                _ => Control.CursorShape.Arrow
            };
        }
    }

    private bool TryCreateEditedMissionRuntimeInputs(out MissionDefinition mission, out MapDefinition map)
    {
        mission = null!;
        map = null!;
        if (_mapEditorOverlay is null || _activeMission is null || _simulation?.Map is null)
        {
            LogMapEditorWarning("reinit_edits", "Map editor cannot reinitialize because mission or map data is missing.");
            return false;
        }

        try
        {
            mission = _mapEditorOverlay.ApplyEditsToMission(_activeMission);
            map = _mapEditorOverlay.ApplyEditsToMap(_simulation.Map);
            return true;
        }
        catch (Exception exception)
        {
            LogMapEditorException("reinit_edits", exception);
            return false;
        }
    }

    private bool NudgeMapEditorSelection(Vector2 delta)
    {
        if (_mapEditorOverlay is null)
        {
            return true;
        }

        if (_mapEditorOverlay.NudgeSelected(delta))
        {
            ClearMapEditorPendingDecisions();
            ClearPendingMapEditorSave("Map editor save preview cleared after edit.");
            RefreshMapEditorPanel();
        }

        return true;
    }

    private bool CycleMapEditorSelection(bool forward)
    {
        if (_mapEditorOverlay is null)
        {
            LogMapEditorWarning("cycle_selection", "Map editor overlay is missing; cannot cycle selection.");
            return true;
        }

        if (_mapEditorOverlay.CycleSelection(forward))
        {
            ClearMapEditorPendingDecisions();
            RefreshMapEditorPanel();
        }

        return true;
    }

    private void ExportMapEditorSelection()
    {
        if (_mapEditorOverlay is null)
        {
            LogMapEditorWarning("export_selection", "Map editor overlay is missing; cannot export selection.");
            return;
        }

        var snippet = _mapEditorOverlay.ExportSelectedSnippet();
        CopyMapEditorExport("selection", snippet);
        RefreshMapEditorPanel();
    }

    private void ExportMapEditorAll()
    {
        if (_mapEditorOverlay is null)
        {
            LogMapEditorWarning("export_all", "Map editor overlay is missing; cannot export all snippets.");
            return;
        }

        var snippet = _mapEditorOverlay.ExportAllSnippets();
        CopyMapEditorExport("full", snippet);
        RefreshMapEditorPanel();
    }

    private void CopyMapEditorExport(string label, string snippet)
    {
        GD.Print($"Map editor {label} export:{System.Environment.NewLine}{snippet}");
        if (snippet == "No map editor selection.")
        {
            _lastMapEditorExportSummary = "No export: select a marker, region, or object first.";
            _lastActionMessage = _lastMapEditorExportSummary;
            _mapEditorOverlay?.LogWarning($"export_{label}", _lastMapEditorExportSummary);
            return;
        }

        try
        {
            DisplayServer.ClipboardSet(snippet);
            _lastMapEditorExportSummary = $"Last {label} export copied to clipboard and printed to console.";
            _lastActionMessage = _lastMapEditorExportSummary;
            _pendingMapEditorSave = null;
            _mapEditorSavePreviewSummary = string.Empty;
        }
        catch (Exception exception)
        {
            LogMapEditorException($"clipboard_{label}", exception);
        }
    }

    private string MapEditorSavePanelText()
    {
        return string.IsNullOrWhiteSpace(_mapEditorSavePreviewSummary)
            ? string.Empty
            : $"{System.Environment.NewLine}{_mapEditorSavePreviewSummary}";
    }

    private static string CompactDiffForPanel(string diff)
    {
        var lines = diff.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        const int maxLines = 18;
        var visible = lines.Take(maxLines).ToArray();
        var suffix = lines.Length > maxLines
            ? $"{System.Environment.NewLine}... full diff printed to console ..."
            : string.Empty;
        return "Pending save diff:\n" + string.Join(System.Environment.NewLine, visible) + suffix;
    }

    private void ClearPendingMapEditorSave(string? message = null)
    {
        var hadPendingSave = _pendingMapEditorSave is not null || !string.IsNullOrWhiteSpace(_mapEditorSavePreviewSummary);
        _pendingMapEditorSave = null;
        _mapEditorSavePreviewSummary = string.Empty;
        if (hadPendingSave && !string.IsNullOrWhiteSpace(message))
        {
            _lastMapEditorExportSummary = message;
            _lastActionMessage = message;
        }
    }

    private void RefreshMapEditorPanel()
    {
        if (_mapEditorLabel is null || _mapEditorOverlay is null)
        {
            return;
        }

        RefreshMapEditorToolPanels();
        var missionName = _localization?.ContentName(_activeMissionId, _activeMission?.DisplayName ?? _activeMissionId)
            ?? _activeMission?.DisplayName
            ?? _activeMissionId;
        var unsavedLabel = _mapEditorOverlay.Revision == 0
            ? "No unsaved edits"
            : $"Edits: {_mapEditorOverlay.Revision} unsaved";
        _mapEditorLabel.Text =
            $"F5 closes editor   |   Mission: {missionName}   |   {unsavedLabel}";
    }

    private void LogMapEditorWarning(string operation, string message)
    {
        _mapEditorOverlay?.LogWarning(operation, message);
        if (_mapEditorOverlay is null)
        {
            GD.PushWarning($"[MapEditor:Warning] op={operation} message=\"{message}\"");
        }

        _lastMapEditorExportSummary = message;
        _lastActionMessage = message;
    }

    private void LogMapEditorException(string operation, Exception exception)
    {
        var message = $"Map editor {operation} failed: {exception.GetType().Name}: {exception.Message}";
        _mapEditorOverlay?.LogException(operation, exception, message);
        if (_mapEditorOverlay is null)
        {
            GD.PushError($"[MapEditor:Error] op={operation} message=\"{message}\"");
        }

        _lastMapEditorExportSummary = message;
        _lastActionMessage = message;
        RefreshMapEditorPanel();
    }
}
