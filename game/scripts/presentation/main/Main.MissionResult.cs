using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private void SetupMissionResultOverlay()
    {
        var uiRoot = GetNode<CanvasLayer>("UiRoot");
        _missionResultPanel = new Panel
        {
            Name = "MissionResultPanel",
            Visible = false,
            Position = new Vector2(360, 250),
            Size = new Vector2(560, 160)
        };
        uiRoot.AddChild(_missionResultPanel);

        _missionResultLabel = new Label
        {
            Name = "MissionResultLabel",
            Position = new Vector2(24, 24),
            Size = new Vector2(512, 112),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _missionResultPanel.AddChild(_missionResultLabel);
        ApplyMissionResultScale();
    }

    private void UpdateMissionResultOverlay()
    {
        if (_simulation is null || _missionResultPanel is null || _missionResultLabel is null)
        {
            return;
        }

        if (_simulation.MissionState.Status == MissionStatus.Active)
        {
            _missionResultPanel.Visible = false;
            return;
        }

        _missionResultPanel.Visible = true;
        var titleKey = _simulation.MissionState.Status == MissionStatus.Won
            ? "ui.mission_result.won_title"
            : "ui.mission_result.lost_title";
        _missionResultLabel.Text = $"{L(titleKey)}\n{LocalizedMissionText(_simulation.MissionState)}";
    }

    private void ApplyMissionResultScale()
    {
        if (_missionResultPanel is null || _missionResultLabel is null)
        {
            return;
        }

        _missionResultPanel.Position = new Vector2(360, 250) * _uiScale;
        _missionResultPanel.Size = new Vector2(560, 160) * _uiScale;
        _missionResultLabel.Position = new Vector2(24, 24) * _uiScale;
        _missionResultLabel.Size = new Vector2(512, 112) * _uiScale;
        _missionResultLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(24 * _uiScale));
    }
}
