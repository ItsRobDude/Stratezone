namespace Stratezone.Simulation.Content;

public sealed record UnitDefinition(
    string Id,
    string DisplayName,
    string Role,
    float MovementSpeed,
    int Health,
    bool CanAttack,
    bool CanConstruct,
    bool CanRepair,
    bool CanRunOverInfantry
);

