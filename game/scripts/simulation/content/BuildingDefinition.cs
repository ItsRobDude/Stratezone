namespace Stratezone.Simulation.Content;

public sealed record BuildingDefinition(
    string Id,
    string DisplayName,
    string Role,
    int Cost,
    int Health,
    IReadOnlyDictionary<string, float> DamageResistances,
    float FootprintRadius,
    float PlacementBuffer,
    bool RequiresPower,
    bool ProvidesPower,
    float PowerRadius,
    float PylonLinkRange,
    bool ProvidesResourceExtraction,
    string? ExtractorResourceId,
    bool WallAnchor,
    float WallLinkRange
);
