namespace Stratezone.Simulation;

public sealed record ProductionValidation(
    bool CanQueue,
    string Reason,
    UnitState? PreviewUnit = null,
    BuildingState? Producer = null,
    string MessageKey = "",
    IReadOnlyDictionary<string, string>? MessageArgs = null
);
