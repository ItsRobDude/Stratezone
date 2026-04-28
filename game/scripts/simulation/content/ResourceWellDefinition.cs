namespace Stratezone.Simulation.Content;

public sealed record ResourceWellDefinition(
    string Id,
    string ResourceId,
    float Capacity,
    float ExtractionRate,
    bool Depletes
);
