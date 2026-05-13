using Stratezone.Simulation;

public partial class Main
{
    private bool IsUnitCommandAvailable(string unitId)
    {
        return _availableUnitIds.Count == 0 || _availableUnitIds.Contains(unitId);
    }

    private bool IsBuildingCommandAvailable(string buildingId)
    {
        return _availableBuildingIds.Count == 0 || _availableBuildingIds.Contains(buildingId);
    }

    private string? GetBuildingCommandBlockedReason(string buildingId)
    {
        if (buildingId == ContentIds.Buildings.ColonyHub ||
            _simulation is null ||
            !IsBuildingCommandAvailable(ContentIds.Buildings.ColonyHub) ||
            HasPlayerColonyHub())
        {
            return null;
        }

        return L("sim.placement.requires_colony_hub");
    }

    private bool HasPlayerColonyHub()
    {
        return _simulation?.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed) == true;
    }

    private string GetTrainingCommandSummary()
    {
        if (_catalog is null)
        {
            return string.Empty;
        }

        return string.Join(
            " | ",
            TrainHotkeyOrder
                .Where(IsUnitCommandAvailable)
                .Select(unitId => L(
                    "ui.command.hotkey_suffix",
                    SimulationMessage.Args(
                        ("name", UnitShortName(_catalog.GetUnit(unitId))),
                        ("hotkey", GetTrainHotkeyLabel(unitId))))));
    }

    private static string GetTrainHotkeyLabel(string unitId)
    {
        return unitId switch
        {
            ContentIds.Units.Grunt => "W",
            ContentIds.Units.Cadet => "C",
            ContentIds.Units.Rifleman => "R",
            ContentIds.Units.Guardian => "G",
            ContentIds.Units.Rover => "V",
            _ => "?"
        };
    }
}
