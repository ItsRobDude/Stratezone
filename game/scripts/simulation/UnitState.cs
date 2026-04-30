using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class UnitState
{
    public UnitState(int entityId, UnitDefinition definition, string factionId, SimVector2 position)
    {
        EntityId = entityId;
        Definition = definition;
        FactionId = factionId;
        Position = position;
        Health = definition.Health;
    }

    public int EntityId { get; }
    public UnitDefinition Definition { get; }
    public string FactionId { get; }
    public SimVector2 Position { get; internal set; }
    public float Health { get; private set; }
    public float AttackCooldownRemaining { get; internal set; }
    public int? TargetBuildingEntityId { get; internal set; }
    public bool IsBlockedByEnergyWall { get; internal set; }
    public bool IsDestroyed => Health <= 0.0f;

    public void ApplyDamage(float rawDamage, string damageType)
    {
        if (IsDestroyed || rawDamage <= 0.0f)
        {
            return;
        }

        var resistance = Definition.DamageResistances.TryGetValue(damageType, out var value)
            ? value
            : 0.0f;
        var finalDamage = rawDamage * MathF.Max(0.0f, 1.0f - resistance);
        Health = MathF.Max(0.0f, Health - finalDamage);
    }
}
