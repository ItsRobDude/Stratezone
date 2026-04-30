namespace Stratezone.Simulation;

public sealed record PlacementResult(
    bool Success,
    string Message,
    BuildingState? Building = null,
    string MessageKey = "",
    IReadOnlyDictionary<string, string>? MessageArgs = null
);
