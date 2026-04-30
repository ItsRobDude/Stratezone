namespace Stratezone.Simulation;

public sealed record ProductionResult(
    bool Success,
    string Message,
    ProductionOrderState? Order = null,
    string MessageKey = "",
    IReadOnlyDictionary<string, string>? MessageArgs = null
);
