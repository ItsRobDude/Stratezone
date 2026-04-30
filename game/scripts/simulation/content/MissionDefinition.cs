namespace Stratezone.Simulation.Content;

public sealed record MissionDefinition(
    string Id,
    string DisplayName,
    IReadOnlyDictionary<string, int> PlayerStartingResources,
    IReadOnlyDictionary<string, int> EnemyStartingResources,
    IReadOnlyList<string> ResourceWellIds
);
