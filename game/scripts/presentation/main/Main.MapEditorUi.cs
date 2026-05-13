using Godot;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private const int ContextDeleteId = 1;
    private const int ContextDuplicateId = 2;
    private const int ContextChangeRegionTypeId = 3;
    private const int ContextRenameId = 4;
    private const int ContextStartHereId = 5;

    private void SetupMapEditorToolPanels()
    {
        if (_uiLayoutRoot is null)
        {
            return;
        }

        _mapEditorPalettePanel = new Panel
        {
            Name = "MapEditorPalettePanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _uiLayoutRoot.AddChild(_mapEditorPalettePanel);

        _mapEditorPaletteList = new VBoxContainer
        {
            Name = "MapEditorPaletteList",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorPalettePanel.AddChild(_mapEditorPaletteList);

        _mapEditorInspectorPanel = new Panel
        {
            Name = "MapEditorInspectorPanel",
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _uiLayoutRoot.AddChild(_mapEditorInspectorPanel);

        _mapEditorInspectorList = new VBoxContainer
        {
            Name = "MapEditorInspectorList",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorInspectorPanel.AddChild(_mapEditorInspectorList);

        BuildMapEditorPalette();
    }

    private void SetupMapEditorContextMenu()
    {
        if (_uiLayoutRoot is null)
        {
            return;
        }

        _mapEditorContextMenu = new PopupMenu
        {
            Name = "MapEditorContextMenu"
        };
        _mapEditorContextMenu.IdPressed += OnMapEditorContextAction;
        _uiLayoutRoot.AddChild(_mapEditorContextMenu);
    }

    private void RefreshMapEditorToolPanels()
    {
        if (_mapEditorOverlay is null || _refreshingMapEditorUi)
        {
            return;
        }

        _refreshingMapEditorUi = true;
        try
        {
            foreach (var pair in _mapEditorToolButtons)
            {
                if (pair.Value is MapEditorIconButton iconButton)
                {
                    iconButton.SetActive(pair.Key == _mapEditorOverlay.ToolMode);
                }
                else
                {
                    pair.Value.ButtonPressed = pair.Key == _mapEditorOverlay.ToolMode;
                }
            }

            _mapEditorSnapToggle?.SetActive(_mapEditorOverlay.SnapEnabled);
            RefreshMissionPickerSelection();
            RefreshMapEditorInspector();
        }
        finally
        {
            _refreshingMapEditorUi = false;
        }
    }

    private void ApplyMapEditorPanelScale()
    {
        if (_mapEditorPanel is null || _mapEditorLabel is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        var margin = 16.0f * _uiScale;

        _mapEditorPanel.AnchorLeft = 0.0f;
        _mapEditorPanel.AnchorTop = 1.0f;
        _mapEditorPanel.AnchorRight = 0.0f;
        _mapEditorPanel.AnchorBottom = 1.0f;
        _mapEditorPanel.OffsetLeft = margin;
        _mapEditorPanel.OffsetRight = margin + (560.0f * _uiScale);
        _mapEditorPanel.OffsetTop = -48.0f * _uiScale;
        _mapEditorPanel.OffsetBottom = -12.0f * _uiScale;
        _mapEditorLabel.Position = new Vector2(12.0f, 8.0f) * _uiScale;
        _mapEditorLabel.Size = new Vector2(536.0f, 22.0f) * _uiScale;

        if (_mapEditorPalettePanel is not null)
        {
            _mapEditorPalettePanel.AnchorLeft = 0.0f;
            _mapEditorPalettePanel.AnchorTop = 0.0f;
            _mapEditorPalettePanel.AnchorRight = 0.0f;
            _mapEditorPalettePanel.AnchorBottom = 1.0f;
            _mapEditorPalettePanel.OffsetLeft = margin;
            _mapEditorPalettePanel.OffsetRight = margin + (64.0f * _uiScale);
            _mapEditorPalettePanel.OffsetTop = 96.0f * _uiScale;
            _mapEditorPalettePanel.OffsetBottom = -64.0f * _uiScale;
            if (_mapEditorPaletteList is not null)
            {
                _mapEditorPaletteList.Position = new Vector2(8.0f, 8.0f) * _uiScale;
                _mapEditorPaletteList.Size = new Vector2(48.0f, Mathf.Max(120.0f, viewportSize.Y - (176.0f * _uiScale)));
                foreach (var button in _mapEditorPaletteList.GetChildren().OfType<MapEditorIconButton>())
                {
                    button.ApplyUiScale(_uiScale);
                }
            }
        }

        if (_mapEditorInspectorPanel is not null)
        {
            _mapEditorInspectorPanel.AnchorLeft = 1.0f;
            _mapEditorInspectorPanel.AnchorTop = 0.0f;
            _mapEditorInspectorPanel.AnchorRight = 1.0f;
            _mapEditorInspectorPanel.AnchorBottom = 1.0f;
            _mapEditorInspectorPanel.OffsetLeft = -296.0f * _uiScale;
            _mapEditorInspectorPanel.OffsetRight = -16.0f * _uiScale;
            _mapEditorInspectorPanel.OffsetTop = 96.0f * _uiScale;
            _mapEditorInspectorPanel.OffsetBottom = -16.0f * _uiScale;
            if (_mapEditorInspectorList is not null)
            {
                _mapEditorInspectorList.Position = new Vector2(10.0f, 10.0f) * _uiScale;
                _mapEditorInspectorList.Size = new Vector2(260.0f, Mathf.Max(120.0f, viewportSize.Y - (132.0f * _uiScale)));
            }
        }
    }

    private void SetMapEditorToolPanelsVisible(bool visible)
    {
        if (_mapEditorPalettePanel is not null)
        {
            _mapEditorPalettePanel.Visible = visible;
        }

        if (_mapEditorInspectorPanel is not null)
        {
            _mapEditorInspectorPanel.Visible = visible;
        }
    }

    private void ClearMapEditorPendingDecisions()
    {
        _pendingMapEditorDelete = null;
        _pendingMapEditorMissionSwitchId = null;
    }

    private static void ClearChildren(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
