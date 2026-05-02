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

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var margin = 24.0f * _uiScale;
        var desiredPanelSize = new Vector2(560, 160) * _uiScale;
        var maxPanelSize = new Vector2(
            Mathf.Max(260.0f, viewportSize.X - (margin * 2.0f)),
            Mathf.Max(120.0f, viewportSize.Y - (margin * 2.0f)));
        var panelSize = new Vector2(
            Mathf.Min(desiredPanelSize.X, maxPanelSize.X),
            Mathf.Min(desiredPanelSize.Y, maxPanelSize.Y));
        var padding = 24.0f * _uiScale;

        _missionResultPanel.Position = new Vector2(
            Mathf.Max(margin, (viewportSize.X - panelSize.X) * 0.5f),
            Mathf.Max(margin, (viewportSize.Y - panelSize.Y) * 0.5f));
        _missionResultPanel.Size = panelSize;
        _missionResultLabel.Position = new Vector2(padding, padding);
        _missionResultLabel.Size = new Vector2(
            Mathf.Max(120.0f, panelSize.X - (padding * 2.0f)),
            Mathf.Max(48.0f, panelSize.Y - (padding * 2.0f)));
        _missionResultLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(24 * _uiScale));
    }
}
