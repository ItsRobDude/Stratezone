using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private const float MapEditorNudgeWorldUnits = 10.0f;

    private MapEditorOverlay? _mapEditorOverlay;
    private Panel? _mapEditorPanel;
    private Label? _mapEditorLabel;
    private bool _mapEditorEnabled;

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
        _mapEditorOverlay?.Load(_activeMission, _simulation?.Map, _catalog, _simulation);
    }

    private bool HandleMapEditorKey(Key keycode)
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

    private bool HandleMapEditorMouse(InputEvent inputEvent)
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
                _mapEditorOverlay.TryBeginDrag(GetGlobalMousePosition());
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
            ? "Map editor active. Left-drag markers/regions, arrows nudge, E exports selection, J exports all, R reloads."
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

    private void ExportMapEditorSelection()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        var snippet = _mapEditorOverlay.ExportSelectedSnippet();
        GD.Print($"Map editor selection export:{System.Environment.NewLine}{snippet}");
        _lastActionMessage = "Map editor selection exported to the Godot console.";
        RefreshMapEditorPanel();
    }

    private void ExportMapEditorAll()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        var snippet = _mapEditorOverlay.ExportAllSnippets();
        GD.Print($"Map editor full export:{System.Environment.NewLine}{snippet}");
        _lastActionMessage = "Map editor full export printed to the Godot console.";
        RefreshMapEditorPanel();
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
            "Left-drag: move marker/region center | Arrows: nudge 10\n" +
            "E/right-click: export selected | J: export all | R: reload | Esc/F5: close";
    }

    private void ApplyMapEditorPanelScale()
    {
        if (_mapEditorPanel is null || _mapEditorLabel is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        var panelSize = new Vector2(520, 124) * _uiScale;
        _mapEditorPanel.Size = panelSize;
        _mapEditorPanel.Position = new Vector2(16, Mathf.Max(16.0f, viewportSize.Y - panelSize.Y - 16.0f));
        _mapEditorLabel.Size = new Vector2(496, 104) * _uiScale;
        _mapEditorLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(12 * _uiScale));
    }
}
