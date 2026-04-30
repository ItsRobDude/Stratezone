namespace Stratezone.Simulation.Content;

public sealed record UnitDefinition(
    string Id,
    string DisplayName,
    string Role,
    int Cost,
    float TrainTimeSeconds,
    IReadOnlyDictionary<string, float> DamageResistances,
    float MovementSpeed,
    int Health,
    float AttackDamage,
    float AttackRange,
    float AttackCooldown,
    string DamageType,
    bool CanAttack,
    bool CanConstruct,
    bool CanRepair,
    bool CanRunOverInfantry
);
