using System.Globalization;
using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private void BuildMarkerInspector(MapEditorMarker marker)
    {
        AddInspectorLineEdit("ID", marker.Id, text => ApplyMapEditorResult(_mapEditorOverlay?.RenameSelected(text)), "Marker id.");
        AddVectorEdit("Position", marker.Position, vector => ApplyMapEditorResult(_mapEditorOverlay?.SetSelectedCenter(vector)), "Marker world position.");
        AddReadOnlyText("Tags", FormatTagsForInspector(marker.Tags), "Marker tags are shown from mission data.");
        var content = marker.ContentIds.Count == 0 ? "none" : string.Join(", ", marker.ContentIds);
        AddReadOnlyText("Content", content, "Content that references this marker.");
    }

    private void AddInspectorDecision(string message, string cancelText, Action cancel, string confirmText, Action confirm)
    {
        if (_mapEditorInspectorList is null)
        {
            return;
        }

        var panel = new Panel
        {
            CustomMinimumSize = new Vector2(1.0f, 88.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var messageLabel = new Label
        {
            Text = message,
            ThemeTypeVariation = "LabelSecondary",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            Position = new Vector2(8.0f, 6.0f) * _uiScale,
            Size = new Vector2(240.0f, 42.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        panel.AddChild(messageLabel);

        var row = new HBoxContainer
        {
            Position = new Vector2(8.0f, 56.0f) * _uiScale,
            Size = new Vector2(240.0f, 26.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var cancelButton = new Button
        {
            Text = cancelText,
            TooltipText = cancelText,
            CustomMinimumSize = new Vector2(72.0f, 24.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        cancelButton.Pressed += cancel;
        var confirmButton = new Button
        {
            Text = confirmText,
            TooltipText = confirmText,
            CustomMinimumSize = new Vector2(88.0f, 24.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        confirmButton.Pressed += confirm;
        row.AddChild(cancelButton);
        row.AddChild(confirmButton);
        panel.AddChild(row);
        _mapEditorInspectorList.AddChild(panel);
    }

    private void AddInspectorLineEdit(string label, string value, Action<string> commit, string tooltip)
    {
        var input = new LineEdit
        {
            Text = value,
            TooltipText = tooltip,
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
        AddInspectorRow(label, input);
        if (_mapEditorFocusIdOnRefresh && label == "ID")
        {
            _mapEditorFocusIdOnRefresh = false;
            input.CallDeferred(Control.MethodName.GrabFocus);
            input.CallDeferred(LineEdit.MethodName.SelectAll);
        }
    }

    private void AddVectorEdit(string label, SimVector2 value, Action<SimVector2> commit, string tooltip)
    {
        var row = new HBoxContainer
        {
            TooltipText = tooltip,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var x = NewNumberLineEdit("X", value.X);
        var y = NewNumberLineEdit("Y", value.Y);

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
        AddInspectorRow(label, row);
    }

    private LineEdit NewNumberLineEdit(string placeholder, float value)
    {
        return new LineEdit
        {
            Text = FormatEditorFloat(value),
            PlaceholderText = placeholder,
            CustomMinimumSize = new Vector2(74.0f, 24.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
    }

    private void AddReadOnlyText(string label, string value, string tooltip)
    {
        var input = new LineEdit
        {
            Text = value,
            Editable = false,
            TooltipText = tooltip,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        AddInspectorRow(label, input);
    }

    private void AddInspectorText(string text, bool secondary)
    {
        _mapEditorInspectorList?.AddChild(new Label
        {
            Text = text,
            ThemeTypeVariation = secondary ? "LabelSecondary" : "LabelBody",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private void AddInspectorSpacer()
    {
        _mapEditorInspectorList?.AddChild(new Control
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private void AddInspectorRow(string label, Control input)
    {
        if (_mapEditorInspectorList is null)
        {
            return;
        }

        var row = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(1.0f, 28.0f) * _uiScale,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var fieldLabel = new Label
        {
            Text = label,
            ThemeTypeVariation = "LabelSecondary",
            CustomMinimumSize = new Vector2(InspectorLabelWidth, 22.0f) * _uiScale,
            VerticalAlignment = VerticalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        input.CustomMinimumSize = input.CustomMinimumSize == Vector2.Zero
            ? new Vector2(154.0f, 24.0f) * _uiScale
            : input.CustomMinimumSize;
        input.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(fieldLabel);
        row.AddChild(input);
        _mapEditorInspectorList.AddChild(row);
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
        ApplyMapEditorModePresentation(enabled: true);
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

    private static string FormatTagsForInspector(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? "none" : string.Join(", ", tags);
    }
}

public partial class MapEditorValidationRow : HBoxContainer
{
    private readonly string _severity;
    private readonly string _text;
    private readonly float _uiScale;

    public MapEditorValidationRow(string severity, string text, float uiScale)
    {
        _severity = severity;
        _text = text;
        _uiScale = uiScale;
        CustomMinimumSize = new Vector2(1.0f, 34.0f) * uiScale;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Ready()
    {
        var icon = new MapEditorValidationIcon(_severity)
        {
            CustomMinimumSize = new Vector2(22.0f, 22.0f) * _uiScale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        var label = new Label
        {
            Text = _text,
            ThemeTypeVariation = "LabelSecondary",
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(210.0f, 30.0f) * _uiScale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(icon);
        AddChild(label);
    }
}

public partial class MapEditorValidationIcon : Control
{
    private readonly string _severity;

    public MapEditorValidationIcon(string severity)
    {
        _severity = severity;
    }

    public override void _Draw()
    {
        var center = Size * 0.5f;
        var color = _severity switch
        {
            "error" => UiPalette.Error,
            "success" => UiPalette.Success,
            _ => UiPalette.Warning
        };
        DrawCircle(center, Mathf.Min(Size.X, Size.Y) * 0.36f, color with { A = 0.20f });
        DrawArc(center, Mathf.Min(Size.X, Size.Y) * 0.36f, 0.0f, Mathf.Tau, 24, color, 2.0f);
        if (_severity == "success")
        {
            DrawLine(center + new Vector2(-5, 0), center + new Vector2(-1, 5), color, 2.0f);
            DrawLine(center + new Vector2(-1, 5), center + new Vector2(6, -5), color, 2.0f);
        }
        else if (_severity == "error")
        {
            DrawLine(center + new Vector2(-5, -5), center + new Vector2(5, 5), color, 2.0f);
            DrawLine(center + new Vector2(5, -5), center + new Vector2(-5, 5), color, 2.0f);
        }
        else
        {
            DrawLine(center + new Vector2(0, -6), center + new Vector2(0, 2), color, 2.0f);
            DrawCircle(center + new Vector2(0, 6), 1.6f, color);
        }
    }
}
