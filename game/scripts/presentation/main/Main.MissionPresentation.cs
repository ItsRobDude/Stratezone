using Stratezone.Simulation;

public partial class Main
{
    private string GetMissionHudLine()
    {
        if (_simulation is null)
        {
            return string.Empty;
        }

        var objective = _simulation.MissionState.Status == MissionStatus.Active &&
            !string.IsNullOrWhiteSpace(_activeMission?.Presentation.StartObjectiveKey)
                ? L(_activeMission.Presentation.StartObjectiveKey)
                : LocalizedMissionText(_simulation.MissionState);
        return L(
            "ui.hud.mission_line",
            SimulationMessage.Args(("status", _simulation.MissionState.Status), ("objective", objective)));
    }

    private string GetMissionBriefingHudLine()
    {
        if (_activeMission is null ||
            string.IsNullOrWhiteSpace(_activeMission.Presentation.BriefingTitleKey) ||
            string.IsNullOrWhiteSpace(_activeMission.Presentation.BriefingBodyKey))
        {
            return string.Empty;
        }

        return L(
            "ui.hud.briefing_line",
            SimulationMessage.Args(
                ("title", L(_activeMission.Presentation.BriefingTitleKey)),
                ("briefing", L(_activeMission.Presentation.BriefingBodyKey))));
    }

    private string LocalizedMissionResultText(MissionState state)
    {
        if (_activeMission is not null &&
            state.Status == MissionStatus.Won &&
            !string.IsNullOrWhiteSpace(_activeMission.Presentation.SuccessKey))
        {
            return L(_activeMission.Presentation.SuccessKey);
        }

        if (_activeMission is not null &&
            state.Status == MissionStatus.Lost &&
            !string.IsNullOrWhiteSpace(_activeMission.Presentation.FailureKey))
        {
            return L(_activeMission.Presentation.FailureKey);
        }

        return LocalizedMissionText(state);
    }
}
