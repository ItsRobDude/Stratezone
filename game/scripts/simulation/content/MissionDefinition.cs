using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed record MissionDefinition(
    string Id,
    string DisplayName,
    IReadOnlyDictionary<string, int> PlayerStartingResources,
    IReadOnlyDictionary<string, int> EnemyStartingResources,
    IReadOnlyList<string> ResourceWellIds,
    IReadOnlyList<MissionMarkerDefinition> Markers,
    IReadOnlyList<MissionStartingEntityDefinition> StartingEntities,
    IReadOnlyList<MissionResourceWellPlacementDefinition> ResourceWellPlacements,
    IReadOnlyList<string> AvailableUnitIds,
    IReadOnlyList<string> AvailableBuildingIds,
    EnemyAiProfileDefinition EnemyAiProfile
);

public sealed record MissionMarkerDefinition(
    string Id,
    SimVector2 Position
);

public sealed record MissionStartingEntityDefinition(
    string ContentId,
    string FactionId,
    string MarkerId,
    SimVector2 Offset
);

public sealed record MissionResourceWellPlacementDefinition(
    string WellId,
    string MarkerId,
    SimVector2 Offset
);

public sealed record EnemyAiProfileDefinition(
    string Id,
    float FirstAttackDelaySeconds,
    float RebuildCooldownSeconds,
    float ProductionCooldownSeconds,
    int AttackGroupSize,
    float CentralWellInterest,
    float PressureSlowdownMultiplier,
    float TrainTimeMultiplier,
    string HubMarkerId,
    string PowerPlantMarkerId,
    string BarracksMarkerId,
    string ExtractorMarkerId,
    string DefenseTowerMarkerId,
    string RallyMarkerId
)
{
    public static EnemyAiProfileDefinition Default { get; } = new(
        "ai_profile_default",
        0.0f,
        0.0f,
        0.0f,
        1,
        1.0f,
        1.0f,
        RtsSimulation.EnemyTrainTimeMultiplier,
        "enemy_base",
        "enemy_power",
        "enemy_barracks",
        "enemy_extractor",
        "enemy_defense",
        "enemy_rally");
}
