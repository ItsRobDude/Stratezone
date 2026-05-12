using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed record MissionDefinition(
    string Id,
    string DisplayName,
    string MapId,
    string StartPattern,
    IReadOnlyDictionary<string, int> PlayerStartingResources,
    IReadOnlyDictionary<string, int> EnemyStartingResources,
    IReadOnlyList<string> ResourceWellIds,
    IReadOnlyList<MissionMarkerDefinition> Markers,
    IReadOnlyList<MissionStartingEntityDefinition> StartingEntities,
    IReadOnlyList<MissionResourceWellPlacementDefinition> ResourceWellPlacements,
    IReadOnlyList<string> AvailableUnitIds,
    IReadOnlyList<string> AvailableBuildingIds,
    IReadOnlyList<string> ObjectiveIds,
    IReadOnlyList<string> FailureConditionIds,
    MissionPresentationDefinition Presentation,
    IReadOnlyList<MissionTriggerDefinition> MissionTriggers,
    IReadOnlyList<MissionMapObjectOverrideDefinition> MapObjectOverrides,
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

public sealed record MissionPresentationDefinition(
    string BriefingTitleKey,
    string BriefingBodyKey,
    string StartObjectiveKey,
    string SuccessKey,
    string FailureKey,
    string RetryHintKey,
    string TacticalNoteKey,
    IReadOnlyList<MissionMapCalloutDefinition> MapCallouts
);

public sealed record MissionMapCalloutDefinition(
    string MarkerId,
    string TextKey
);

public sealed record MissionTriggerDefinition(
    string Id,
    IReadOnlyList<string> WatchedPlayerBuildingIds,
    float MinElapsedSeconds,
    float CoalesceWindowSeconds,
    float CooldownSeconds,
    int MaxFireCount,
    int EnemyAttackGroupSize
);

public sealed record MissionMapObjectOverrideDefinition(
    string ObjectId,
    float StartingHealthPercent
);

public sealed record EnemyAiProfileDefinition(
    string Id,
    float FirstRebuildDelaySeconds,
    float FirstCentralWellRebuildDelaySeconds,
    float FirstAttackDelaySeconds,
    float FirstPatrolDelaySeconds,
    float PatrolIntervalSeconds,
    float RebuildCooldownSeconds,
    float ProductionCooldownSeconds,
    int AttackGroupSize,
    int PatrolGroupSize,
    int MaxPatrolDispatches,
    float CentralWellInterest,
    float CentralWellRebuildCooldownSeconds,
    int MaxCentralWellRebuilds,
    float PressureSlowdownMultiplier,
    float TrainTimeMultiplier,
    string HubMarkerId,
    string PowerPlantMarkerId,
    string BarracksMarkerId,
    string ExtractorMarkerId,
    string DefenseTowerMarkerId,
    string RallyMarkerId,
    IReadOnlyList<string> PatrolMarkerIds,
    string CentralIslandAttackViaBridgeId
)
{
    public static EnemyAiProfileDefinition Default { get; } = new(
        "ai_profile_default",
        0.0f,
        0.0f,
        0.0f,
        0.0f,
        0.0f,
        0.0f,
        0.0f,
        1,
        1,
        0,
        1.0f,
        0.0f,
        int.MaxValue,
        1.0f,
        RtsSimulation.EnemyTrainTimeMultiplier,
        "enemy_base",
        "enemy_power",
        "enemy_barracks",
        "enemy_extractor",
        "enemy_defense",
        "enemy_rally",
        [],
        string.Empty);
}
