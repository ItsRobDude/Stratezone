namespace Stratezone.Simulation;

public sealed record SimulationEvent(
    string FactionId,
    string MessageKey,
    IReadOnlyDictionary<string, string>? MessageArgs = null
);
