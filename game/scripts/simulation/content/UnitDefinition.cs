namespace Stratezone.Simulation.Content;

public sealed record UnitDefinition(
    string Id,
    string DisplayName,
    string Role,
    int Cost,
    float TrainTimeSeconds,
    IReadOnlyDictionary<string, float> DamageResistances,
    float MovementSpeed,
    float SightRange,
    string? AllowedByBuildingId,
    string? RequiredAddonBuildingId,
    string? SpawnBuildingId,
    int Health,
    float AttackDamage,
    float AttackRange,
    float AttackCooldown,
    string DamageType,
    float AreaRadius,
    bool FriendlyFire,
    IReadOnlyList<string> TargetFilters,
    bool CanAttack,
    bool CanConstruct,
    bool CanRepair,
    bool CanRunOverInfantry,
    IReadOnlyList<string> Tags
);
