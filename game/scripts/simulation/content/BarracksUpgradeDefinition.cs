namespace Stratezone.Simulation.Content;

public sealed record BarracksUpgradeDefinition(
    string Id,
    string DisplayName,
    int Cost,
    float DurationSeconds,
    int RequiredGruntCount,
    float RequiredGruntRange,
    bool RequiresPoweredBarracks,
    bool RequiresColonyHub,
    IReadOnlyList<string> UnlockUnitIds,
    IReadOnlyList<string> Tags
);
