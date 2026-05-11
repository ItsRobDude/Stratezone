using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private const float MapEditorNudgeWorldUnits = 10.0f;

    private MapEditorOverlay? _mapEditorOverlay;
    private Panel? _mapEditorPanel;
    private Label? _mapEditorLabel;
    private bool _mapEditorEnabled;
    private string _lastMapEditorExportSummary = "No export yet.";

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

    private bool HandleMapEditorKey(Key keycode)
    {
        try
        {
            if (keycode == Key.F5)
            {
                ToggleMapEditor();
                return true;
            }

            if (!_mapEditorEnabled)
            {
                return false;
            }

            switch (keycode)
            {
                case Key.Escape:
                    ToggleMapEditor();
                    return true;
                case Key.R:
                    UpdateMapEditorOverlayData();
                    _lastActionMessage = "Map editor reloaded from current mission data.";
                    RefreshMapEditorPanel();
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
            LogMapEditorException($"key_{keycode}", exception);
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
                ExportMapEditorSelection();
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
        _mapEditorEnabled = !_mapEditorEnabled;
        if (_mapEditorEnabled)
        {
            _placementBuildingId = null;
            _placementGhost?.Clear();
            _selectionBoxView?.Clear();
        }

        _mapEditorOverlay?.SetEditorEnabled(_mapEditorEnabled);
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = _mapEditorEnabled;
        }

        _lastActionMessage = _mapEditorEnabled
            ? "Map editor active. Left-drag markers/regions, arrows nudge, E copies selection, J copies all, R reloads."
            : "Map editor closed.";
        RefreshMapEditorPanel();
    }

    private bool NudgeMapEditorSelection(Vector2 delta)
    {
        if (_mapEditorOverlay is null)
        {
            return true;
        }

        if (_mapEditorOverlay.NudgeSelected(delta))
        {
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
            _lastMapEditorExportSummary = "No export: select a marker or region first.";
            _lastActionMessage = _lastMapEditorExportSummary;
            _mapEditorOverlay?.LogWarning($"export_{label}", _lastMapEditorExportSummary);
            return;
        }

        try
        {
            DisplayServer.ClipboardSet(snippet);
            _lastMapEditorExportSummary = $"Last {label} export copied to clipboard and printed to console.";
            _lastActionMessage = _lastMapEditorExportSummary;
        }
        catch (Exception exception)
        {
            LogMapEditorException($"clipboard_{label}", exception);
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
            "Tab/Q: cycle selection | Left-drag: move center | Arrows: nudge 10\n" +
            "E/right-click: copy selected | J: copy all | R: reload | Esc/F5: close\n" +
            _lastMapEditorExportSummary;
    }

    private void ApplyMapEditorPanelScale()
    {
        if (_mapEditorPanel is null || _mapEditorLabel is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        var panelSize = new Vector2(560, 148) * _uiScale;
        _mapEditorPanel.Size = panelSize;
        _mapEditorPanel.Position = new Vector2(16, Mathf.Max(16.0f, viewportSize.Y - panelSize.Y - 16.0f));
        _mapEditorLabel.Size = new Vector2(536, 128) * _uiScale;
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
