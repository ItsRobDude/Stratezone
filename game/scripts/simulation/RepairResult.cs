namespace Stratezone.Simulation;

public sealed record RepairResult(
    bool Success,
    string Message,
    UnitState? Grunt = null,
    BuildingState? Building = null,
    string MessageKey = "",
    IReadOnlyDictionary<string, string>? MessageArgs = null,
    BridgeState? Bridge = null
);
