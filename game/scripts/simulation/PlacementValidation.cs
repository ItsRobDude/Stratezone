namespace Stratezone.Simulation;

public sealed record PlacementValidation(
    bool IsLegal,
    string Reason,
    string? ResourceWellId = null
);
