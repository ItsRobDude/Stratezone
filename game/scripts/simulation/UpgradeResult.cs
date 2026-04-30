namespace Stratezone.Simulation;

public sealed record UpgradeResult(
    bool Success,
    string Message,
    BuildingState? Building = null
);
