using Godot;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private const float InspectorLabelWidth = 78.0f;

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
        AddInspectorHeader(InspectorTitle());
        AddMissionPickerRow();
        if (!string.IsNullOrWhiteSpace(_lastMapEditorExportSummary) && _lastMapEditorExportSummary != "No export yet.")
        {
            AddInspectorText(_lastMapEditorExportSummary, secondary: true);
        }

        if (!string.IsNullOrWhiteSpace(_mapEditorSavePreviewSummary))
        {
            AddInspectorText(_mapEditorSavePreviewSummary, secondary: true);
        }

        if (_pendingMapEditorDelete is not null)
        {
            AddInspectorDecision(
                _pendingMapEditorDelete.Summary,
                "Cancel",
                () =>
                {
                    ClearMapEditorPendingDecisions();
                    RefreshMapEditorPanel();
                },
                "Delete",
                ConfirmMapEditorDelete);
        }

        if (_pendingMapEditorMissionSwitchId is not null)
        {
            AddInspectorDecision(
                $"Switch to {_pendingMapEditorMissionSwitchId} with unsaved editor changes?",
                "Cancel",
                () =>
                {
                    ClearMapEditorPendingDecisions();
                    RefreshMapEditorPanel();
                },
                "Switch",
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
            AddInspectorText("No selection.", secondary: true);
        }

        AddInspectorSpacer();
        BuildValidationPanel();
    }

    private string InspectorTitle()
    {
        if (_mapEditorOverlay is null)
        {
            return "Inspector";
        }

        return _mapEditorOverlay.SelectionKind switch
        {
            MapEditorSelectionKind.Marker => $"Marker: {_mapEditorOverlay.SelectedId}",
            MapEditorSelectionKind.Region => $"Region: {_mapEditorOverlay.SelectedId}",
            MapEditorSelectionKind.Object => $"Object: {_mapEditorOverlay.SelectedId}",
            _ => "Inspector"
        };
    }

    private void AddInspectorHeader(string title)
    {
        if (_mapEditorInspectorList is null)
        {
            return;
        }

        var panel = new Panel
        {
            ThemeTypeVariation = "PanelHeader",
            CustomMinimumSize = new Vector2(1.0f, 28.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var label = new Label
        {
            Text = title,
            ThemeTypeVariation = "LabelBody",
            Position = new Vector2(8.0f, 5.0f) * _uiScale,
            Size = new Vector2(236.0f, 18.0f) * _uiScale,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        panel.AddChild(label);
        _mapEditorInspectorList.AddChild(panel);
    }

    private void AddMissionPickerRow()
    {
        _mapEditorMissionPicker = new OptionButton
        {
            TooltipText = "Switch editor mission.",
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
        AddInspectorRow("Mission", _mapEditorMissionPicker);
        RefreshMissionPickerSelection();
    }

    private void BuildValidationPanel()
    {
        if (_mapEditorOverlay is null || _mapEditorInspectorList is null)
        {
            return;
        }

        AddInspectorText("Validation", secondary: true);
        var issues = _mapEditorOverlay.ValidateContent();
        if (issues.Count == 0)
        {
            _mapEditorInspectorList.AddChild(new MapEditorValidationRow("success", "No validation issues.", _uiScale));
            return;
        }

        foreach (var issue in issues.Take(8))
        {
            var severity = issue.Severity == MapEditorValidationSeverity.Error ? "error" : "warning";
            _mapEditorInspectorList.AddChild(new MapEditorValidationRow(severity, issue.Message, _uiScale));
        }
    }
}
