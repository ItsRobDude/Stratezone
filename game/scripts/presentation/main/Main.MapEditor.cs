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
    private bool _mapEditorEnabled;
    private string _lastMapEditorExportSummary = "No export yet.";
    private MapEditorSavePreview? _pendingMapEditorSave;
    private string _mapEditorSavePreviewSummary = string.Empty;

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
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.92f)
        };
        _uiLayoutRoot.AddChild(_mapEditorPanel);

        _mapEditorLabel = new Label
        {
            Name = "MapEditorLabel",
            Position = new Vector2(12, 10),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _mapEditorPanel.AddChild(_mapEditorLabel);
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
                if (_mapEditorOverlay.IsDragging)
                {
                    _mapEditorOverlay.DragTo(GetGlobalMousePosition());
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
                    if (!_mapEditorOverlay.TryBeginDrag(GetGlobalMousePosition()))
                    {
                        _mapEditorOverlay.LogWarning("mouse_select", $"No marker or region selected at {GetGlobalMousePosition()}.");
                    }

                    RefreshMapEditorPanel();
                }
                else
                {
                    _mapEditorOverlay.EndDrag();
                    RefreshMapEditorPanel();
                }

                return true;
            }

            if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
            {
                if (_mapEditorOverlay.TrySelectMarkerAt(GetGlobalMousePosition()))
                {
                    StartMapEditorViewAtSelectedMarker();
                }
                else
                {
                    _lastActionMessage = "Right-click a marker to re-init the mission view there. Use E to copy selected JSON.";
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
            if (_mapEditorPanel is not null)
            {
                _mapEditorPanel.Visible = true;
            }

            _lastActionMessage = "Map editor active. Simulation paused; close editor to re-init play from current edits.";
            RefreshMapEditorPanel();
            return;
        }

        ReinitializeMapEditorMissionFromEdits(null);
        _mapEditorEnabled = false;
        _mapEditorOverlay?.SetEditorEnabled(false);
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = false;
        }

        ClearPendingMapEditorSave();
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
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = false;
        }

        ClearPendingMapEditorSave();
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

        _mapEditorLabel.Text =
            "F5 Map Editor/Tuner\n" +
            $"{_mapEditorOverlay.SelectedSummary}\n" +
            $"{_mapEditorOverlay.SelectedInspector}\n" +
            "Tab/Q: cycle | Left-drag: move center | Arrows: nudge 10 | P: pathing | Shift+F5: re-init\n" +
            "Ctrl+S: preview/write JSON | E: copy selected | J: copy all | Right-click marker: start view here\n" +
            _lastMapEditorExportSummary +
            MapEditorSavePanelText();
    }

    private void ApplyMapEditorPanelScale()
    {
        if (_mapEditorPanel is null || _mapEditorLabel is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        var panelSize = new Vector2(720, 330) * _uiScale;
        _mapEditorPanel.Size = panelSize;
        _mapEditorPanel.Position = new Vector2(16, Mathf.Max(16.0f, viewportSize.Y - panelSize.Y - 16.0f));
        _mapEditorLabel.Size = new Vector2(696, 310) * _uiScale;
        _mapEditorLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(12 * _uiScale));
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
