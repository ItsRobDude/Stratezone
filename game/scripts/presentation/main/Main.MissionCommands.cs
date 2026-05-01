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
                .Select(unitId => $"{GetTrainHotkeyLabel(unitId)} {UnitShortName(_catalog.GetUnit(unitId))}"));
    }

    private static string GetTrainHotkeyLabel(string unitId)
    {
        return unitId switch
        {
            ContentIds.Units.Worker => "Q",
            ContentIds.Units.Rifleman => "W",
            ContentIds.Units.Guardian => "E",
            ContentIds.Units.Rover => "R",
            _ => "?"
        };
    }
}
