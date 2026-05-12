using System.Globalization;
using Godot;
using Stratezone.Simulation;
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
            Position = new Vector2(10, 10),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorPalettePanel.AddChild(_mapEditorPaletteList);
        BuildMapEditorPalette();

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
            Position = new Vector2(10, 10),
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorInspectorPanel.AddChild(_mapEditorInspectorList);
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

    private void BuildMapEditorPalette()
    {
        if (_mapEditorPaletteList is null)
        {
            return;
        }

        ClearChildren(_mapEditorPaletteList);
        _mapEditorToolButtons.Clear();
        AddPaletteLabel("Mission");
        _mapEditorMissionPicker = new OptionButton
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorMissionPickerIds.Clear();
        if (_catalog is not null)
        {
            foreach (var mission in _catalog.Missions.Values.OrderBy(mission => mission.DisplayName, StringComparer.Ordinal))
            {
                _mapEditorMissionPickerIds.Add(mission.Id);
                _mapEditorMissionPicker.AddItem(mission.DisplayName);
            }
        }

        _mapEditorMissionPicker.ItemSelected += OnMapEditorMissionPicked;
        _mapEditorPaletteList.AddChild(_mapEditorMissionPicker);

        AddPaletteLabel("Tools");
        AddToolButton("Select", MapEditorToolMode.Select);
        AddToolButton("Add Marker", MapEditorToolMode.AddMarker);
        AddToolButton("Add Rect Region", MapEditorToolMode.AddRectRegion);
        AddToolButton("Add Circle Region", MapEditorToolMode.AddCircleRegion);
        AddToolButton("Delete", MapEditorToolMode.Delete);

        _mapEditorSnapToggle = new CheckBox
        {
            Text = "Snap 10u (G)",
            ButtonPressed = true,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        _mapEditorSnapToggle.Toggled += enabled =>
        {
            if (_refreshingMapEditorUi || _mapEditorOverlay is null)
            {
                return;
            }

            _mapEditorOverlay.SetSnapEnabled(enabled);
            _lastActionMessage = enabled ? "Map editor snap enabled." : "Map editor snap disabled.";
            RefreshMapEditorPanel();
        };
        _mapEditorPaletteList.AddChild(_mapEditorSnapToggle);

        AddPaletteLabel("Actions");
        AddPaletteActionButton("Undo", UndoMapEditor);
        AddPaletteActionButton("Redo", RedoMapEditor);
        AddPaletteActionButton("Duplicate", DuplicateMapEditorSelection);
        AddPaletteActionButton("Delete...", BeginMapEditorDelete);
    }

    private void AddPaletteLabel(string text)
    {
        _mapEditorPaletteList?.AddChild(new Label
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private void AddToolButton(string text, MapEditorToolMode mode)
    {
        if (_mapEditorPaletteList is null)
        {
            return;
        }

        var button = new Button
        {
            Text = text,
            ToggleMode = true,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        button.Pressed += () => SetMapEditorToolMode(mode);
        _mapEditorPaletteList.AddChild(button);
        _mapEditorToolButtons[mode] = button;
    }

    private void AddPaletteActionButton(string text, Action action)
    {
        if (_mapEditorPaletteList is null)
        {
            return;
        }

        var button = new Button
        {
            Text = text,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        button.Pressed += action;
        _mapEditorPaletteList.AddChild(button);
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
                pair.Value.ButtonPressed = pair.Key == _mapEditorOverlay.ToolMode;
            }

            if (_mapEditorSnapToggle is not null)
            {
                _mapEditorSnapToggle.ButtonPressed = _mapEditorOverlay.SnapEnabled;
            }

            RefreshMissionPickerSelection();
            RefreshMapEditorInspector();
        }
        finally
        {
            _refreshingMapEditorUi = false;
        }
    }

    private void RefreshMissionPickerSelection()
    {
        if (_mapEditorMissionPicker is null)
        {
            return;
        }

        var index = _mapEditorMissionPickerIds.FindIndex(id => id == _activeMissionId);
        if (index >= 0)
        {
            _mapEditorMissionPicker.Select(index);
        }
    }

    private void RefreshMapEditorInspector()
    {
        if (_mapEditorInspectorList is null || _mapEditorOverlay is null)
        {
            return;
        }

        ClearChildren(_mapEditorInspectorList);
        AddInspectorLabel("Inspector");
        AddInspectorLabel(_mapEditorOverlay.SelectedSummary);
        if (_pendingMapEditorDelete is not null)
        {
            AddInspectorDecision(
                _pendingMapEditorDelete.Summary,
                "Cancel",
                ClearMapEditorPendingDecisions,
                "Delete and orphan references",
                ConfirmMapEditorDelete);
        }

        if (_pendingMapEditorMissionSwitchId is not null)
        {
            AddInspectorDecision(
                $"Switch to {_pendingMapEditorMissionSwitchId} with unsaved editor changes?",
                "Cancel",
                ClearMapEditorPendingDecisions,
                "Switch without saving",
                ConfirmMapEditorMissionSwitch);
        }

        if (_mapEditorOverlay.SelectedMarker is not null)
        {
            BuildMarkerInspector(_mapEditorOverlay.SelectedMarker);
        }
        else if (_mapEditorOverlay.SelectedRegion is not null)
        {
            BuildRegionInspector(_mapEditorOverlay.SelectedRegion);
        }
        else if (_mapEditorOverlay.SelectedObject is not null)
        {
            BuildObjectInspector(_mapEditorOverlay.SelectedObject);
        }
        else
        {
            AddInspectorLabel("No selection.");
        }

        BuildValidationPanel();
    }

    private void BuildMarkerInspector(MapEditorMarker marker)
    {
        AddInspectorLineEdit("id", marker.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)));
        AddVectorEdit("position", marker.Position, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)));
        var content = marker.ContentIds.Count == 0 ? "content: none" : $"content: {string.Join(", ", marker.ContentIds)}";
        AddInspectorLabel(content);
    }

    private void BuildRegionInspector(MapEditorRegion region)
    {
        AddInspectorLineEdit("id", region.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)));
        AddRegionTypeEdit(region);
        AddShapeEdit(region.Shape);
        AddVectorEdit("center", region.Center, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)));
        if (region.Shape == "circle")
        {
            AddFloatEdit("radius", region.Radius, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionRadius(value)));
        }
        else
        {
            AddVectorEdit("size", region.Size, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionSize(vector)));
        }

        AddFlagEdit("blocks_movement", region.BlocksMovement, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(value, region.BlocksBuilding, region.AllowsBuilding)));
        AddFlagEdit("blocks_building", region.BlocksBuilding, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(region.BlocksMovement, value, region.AllowsBuilding)));
        AddFlagEdit("allows_building", region.AllowsBuilding, value => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionFlags(region.BlocksMovement, region.BlocksBuilding, value)));
        AddInspectorLineEdit("tags", string.Join(", ", region.Tags), text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedTags(ParseTags(text))));
    }

    private void BuildObjectInspector(MapEditorObject mapObject)
    {
        AddInspectorLineEdit("id", mapObject.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)));
        AddInspectorLabel($"object_type: {mapObject.ObjectType}");
        AddInspectorLabel($"shape: {mapObject.Shape}");
        AddVectorEdit("center", mapObject.Center, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)));
        AddInspectorLabel(mapObject.Shape == "circle"
            ? $"radius: {FormatEditorFloat(mapObject.Radius)}"
            : $"size: {FormatEditorFloat(mapObject.Size.X)} x {FormatEditorFloat(mapObject.Size.Y)}");
        AddInspectorLabel($"max_health: {FormatEditorFloat(mapObject.MaxHealth)}");
        AddInspectorLineEdit("tags", string.Join(", ", mapObject.Tags), text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedTags(ParseTags(text))));
    }

    private void AddRegionTypeEdit(MapEditorRegion region)
    {
        if (_mapEditorInspectorList is null)
        {
            return;
        }

        AddInspectorLabel("region_type");
        var option = new OptionButton
        {
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
        _mapEditorInspectorList.AddChild(option);
        AddInspectorLineEdit("custom type", region.RegionType, text => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedRegionType(text)));
    }

    private void AddShapeEdit(string shape)
    {
        if (_mapEditorInspectorList is null)
        {
            return;
        }

        AddInspectorLabel("shape");
        var option = new OptionButton
        {
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
        _mapEditorInspectorList.AddChild(option);
    }

    private void BuildValidationPanel()
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        var issues = _mapEditorOverlay.ValidateContent();
        var errors = issues.Count(issue => issue.Severity == MapEditorValidationSeverity.Error);
        var warnings = issues.Count(issue => issue.Severity == MapEditorValidationSeverity.Warning);
        AddInspectorLabel($"Validation: {errors} error(s), {warnings} warning(s)");
        foreach (var issue in issues.Take(8))
        {
            AddInspectorLabel($"{issue.Severity}: {issue.Message}");
        }
    }

    private void AddInspectorDecision(string message, string cancelText, Action cancel, string confirmText, Action confirm)
    {
        AddInspectorLabel(message);
        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var cancelButton = new Button { Text = cancelText, MouseFilter = Control.MouseFilterEnum.Stop };
        cancelButton.Pressed += cancel;
        var confirmButton = new Button { Text = confirmText, MouseFilter = Control.MouseFilterEnum.Stop };
        confirmButton.Pressed += confirm;
        row.AddChild(cancelButton);
        row.AddChild(confirmButton);
        _mapEditorInspectorList?.AddChild(row);
    }

    private void AddInspectorLabel(string text)
    {
        _mapEditorInspectorList?.AddChild(new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private void AddInspectorLineEdit(string label, string value, Action<string> commit)
    {
        AddInspectorLabel(label);
        var input = new LineEdit
        {
            Text = value,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        input.TextSubmitted += text =>
        {
            if (!_refreshingMapEditorUi)
            {
                commit(text);
            }
        };
        input.FocusExited += () =>
        {
            if (!_refreshingMapEditorUi)
            {
                commit(input.Text);
            }
        };
        _mapEditorInspectorList?.AddChild(input);
        if (_mapEditorFocusIdOnRefresh && label == "id")
        {
            _mapEditorFocusIdOnRefresh = false;
            input.CallDeferred(Control.MethodName.GrabFocus);
            input.CallDeferred(LineEdit.MethodName.SelectAll);
        }
    }

    private void AddVectorEdit(string label, SimVector2 value, Action<SimVector2> commit)
    {
        AddInspectorLabel(label);
        var row = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var x = new LineEdit
        {
            Text = FormatEditorFloat(value.X),
            PlaceholderText = "x",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var y = new LineEdit
        {
            Text = FormatEditorFloat(value.Y),
            PlaceholderText = "y",
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        void Commit()
        {
            if (_refreshingMapEditorUi)
            {
                return;
            }

            if (TryParseEditorFloat(x.Text, out var parsedX) && TryParseEditorFloat(y.Text, out var parsedY))
            {
                commit(new SimVector2(parsedX, parsedY));
            }
            else
            {
                _lastActionMessage = $"{label} requires numeric x/y values.";
                RefreshMapEditorPanel();
            }
        }

        x.TextSubmitted += _ => Commit();
        y.TextSubmitted += _ => Commit();
        x.FocusExited += Commit;
        y.FocusExited += Commit;
        row.AddChild(x);
        row.AddChild(y);
        _mapEditorInspectorList?.AddChild(row);
    }

    private void AddFloatEdit(string label, float value, Action<float> commit)
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
        });
    }

    private void AddFlagEdit(string label, bool value, Action<bool> commit)
    {
        var checkBox = new CheckBox
        {
            Text = label,
            ButtonPressed = value,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        checkBox.Toggled += value =>
        {
            if (!_refreshingMapEditorUi)
            {
                commit(value);
            }
        };
        _mapEditorInspectorList?.AddChild(checkBox);
    }

    private void SetMapEditorToolMode(MapEditorToolMode mode)
    {
        if (_mapEditorOverlay is null)
        {
            return;
        }

        _mapEditorOverlay.SetToolMode(mode);
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

    private void ShowMapEditorContextMenu()
    {
        if (_mapEditorContextMenu is null || _mapEditorOverlay is null)
        {
            return;
        }

        _mapEditorContextMenu.Clear();
        _mapEditorContextMenu.AddItem("Delete...", ContextDeleteId);
        _mapEditorContextMenu.AddItem("Duplicate", ContextDuplicateId);
        if (_mapEditorOverlay.SelectionKind == MapEditorSelectionKind.Region)
        {
            _mapEditorContextMenu.AddItem("Change region type", ContextChangeRegionTypeId);
        }

        _mapEditorContextMenu.AddItem("Rename id", ContextRenameId);
        if (_mapEditorOverlay.SelectionKind == MapEditorSelectionKind.Marker)
        {
            _mapEditorContextMenu.AddSeparator();
            _mapEditorContextMenu.AddItem("Start mission view here", ContextStartHereId);
        }

        _mapEditorContextMenu.Popup(new Rect2I((Vector2I)GetViewport().GetMousePosition(), new Vector2I(220, 1)));
        RefreshMapEditorPanel();
    }

    private void OnMapEditorContextAction(long id)
    {
        switch (id)
        {
            case ContextDeleteId:
                BeginMapEditorDelete();
                break;
            case ContextDuplicateId:
                DuplicateMapEditorSelection();
                break;
            case ContextChangeRegionTypeId:
                ApplyMapEditorResult(_mapEditorOverlay?.CycleSelectedRegionType());
                break;
            case ContextRenameId:
                _mapEditorFocusIdOnRefresh = true;
                _lastActionMessage = "Edit the selected id in the inspector and press Enter.";
                RefreshMapEditorPanel();
                break;
            case ContextStartHereId:
                StartMapEditorViewAtSelectedMarker();
                break;
        }
    }

    private void OnMapEditorMissionPicked(long index)
    {
        if (_refreshingMapEditorUi || index < 0 || index >= _mapEditorMissionPickerIds.Count)
        {
            return;
        }

        var missionId = _mapEditorMissionPickerIds[(int)index];
        if (missionId == _activeMissionId)
        {
            return;
        }

        if (MapEditorHasUnsavedChanges())
        {
            _pendingMapEditorMissionSwitchId = missionId;
            _lastActionMessage = $"Mission switch pending. Unsaved map editor changes exist for {_activeMissionId}.";
            RefreshMapEditorPanel();
            return;
        }

        SwitchMapEditorMission(missionId);
    }

    private void ConfirmMapEditorMissionSwitch()
    {
        var missionId = _pendingMapEditorMissionSwitchId;
        _pendingMapEditorMissionSwitchId = null;
        if (!string.IsNullOrWhiteSpace(missionId))
        {
            SwitchMapEditorMission(missionId);
        }
    }

    private void SwitchMapEditorMission(string missionId)
    {
        LoadMission(missionId);
        _mapEditorEnabled = true;
        _mapEditorOverlay?.SetEditorEnabled(true);
        if (_mapEditorPanel is not null)
        {
            _mapEditorPanel.Visible = true;
        }

        SetMapEditorToolPanelsVisible(true);
        _lastActionMessage = $"Map editor switched to {missionId}.";
        RefreshMapEditorPanel();
    }

    private bool MapEditorHasUnsavedChanges()
    {
        if (_mapEditorOverlay is null)
        {
            return false;
        }

        try
        {
            return _mapEditorOverlay.CreateSavePreview(_gameRoot).HasChanges;
        }
        catch (Exception exception)
        {
            LogMapEditorException("mission_switch_unsaved_check", exception);
            return true;
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

    private static IReadOnlyList<string> ParseTags(string text)
    {
        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool TryParseEditorFloat(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string FormatEditorFloat(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
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
