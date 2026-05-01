using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private bool HandleDebugHotkey(Key keycode)
    {
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
}
