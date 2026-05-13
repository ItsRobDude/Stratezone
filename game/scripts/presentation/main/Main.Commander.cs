using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private bool HandleDebugHotkey(Key keycode)
    {
        if (keycode == Key.F4)
        {
            LoadMission(_activeMissionId);
            var missionName = _localization?.ContentName(_activeMissionId, _activeMission?.DisplayName) ?? _activeMissionId;
            _lastActionMessage = L("ui.action.mission_restarted", SimulationMessage.Args(("mission", missionName)));
            return true;
        }

        if (keycode == Key.F6)
        {
            var nextMissionId = _activeMissionId == ContentIds.Missions.FirstLanding
                ? ContentIds.Missions.WellsAtTheRidge
                : ContentIds.Missions.FirstLanding;
            LoadMission(nextMissionId);
            var missionName = _localization?.ContentName(nextMissionId, _catalog?.GetMission(nextMissionId).DisplayName) ?? nextMissionId;
            _lastActionMessage = L("ui.action.debug_mission_loaded", SimulationMessage.Args(("mission", missionName)));
            return true;
        }

        if (keycode == Key.F12)
        {
            _lastActionMessage = L("ui.action.quit_requested");
            GetTree().Quit();
            return true;
        }

        if (keycode != Key.F7)
        {
            return false;
        }

        if (_simulation is null)
        {
            return true;
        }

        _lastActionMessage = _simulation.DebugKillPlayerCommander()
            ? L("ui.action.debug_commander_killed")
            : L("ui.action.debug_commander_missing");
        return true;
    }

    private string GetCommanderHudLine()
    {
        if (_simulation is null)
        {
            return string.Empty;
        }

        var commander = _simulation.Units.FirstOrDefault(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Commander);
        if (commander is null)
        {
            return L("ui.hud.commander_missing_line");
        }

        var statusKey = commander.IsDestroyed
            ? "ui.hud.commander_destroyed_status"
            : "ui.hud.commander_alive_status";
        return L(
            "ui.hud.commander_line",
            SimulationMessage.Args(
                ("health", $"{commander.Health:0}"),
                ("maxHealth", $"{commander.Definition.Health:0}"),
                ("status", L(statusKey))));
    }

    private void UpdateCommanderIndicator()
    {
        if (_simulation is null || _hudCommander is null)
        {
            return;
        }

        var commander = _simulation.Units.FirstOrDefault(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Commander);
        if (commander is null)
        {
            _hudCommander.UpdateCommander(0, 1, missing: true);
            return;
        }

        _hudCommander.UpdateCommander(
            Mathf.RoundToInt(commander.Health),
            Mathf.RoundToInt(commander.Definition.Health),
            commander.IsDestroyed);
    }
}
