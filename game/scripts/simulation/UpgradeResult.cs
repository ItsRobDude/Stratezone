namespace Stratezone.Simulation;

public sealed record UpgradeResult(
    bool Success,
    string Message,
    BuildingState? Building = null,
    string MessageKey = "",
    IReadOnlyDictionary<string, string>? MessageArgs = null
);
