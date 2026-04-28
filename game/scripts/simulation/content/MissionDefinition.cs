namespace Stratezone.Simulation.Content;

public sealed record MissionDefinition(
    string Id,
    string DisplayName,
    IReadOnlyDictionary<string, int> PlayerStartingResources,
    IReadOnlyList<string> ResourceWellIds
);
