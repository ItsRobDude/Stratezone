using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class BuildingState
{
    public BuildingState(int entityId, BuildingDefinition definition, string factionId, SimVector2 position, string? resourceWellId)
    {
        EntityId = entityId;
        Definition = definition;
        FactionId = factionId;
        Position = position;
        ResourceWellId = resourceWellId;
        Health = definition.Health;
    }

    public int EntityId { get; }
    public BuildingDefinition Definition { get; private set; }
    public string FactionId { get; }
    public SimVector2 Position { get; }
    public string? ResourceWellId { get; }
    public float Health { get; private set; }
    public bool IsPowered { get; internal set; }
    public float AttackCooldownRemaining { get; internal set; }
    public bool IsDestroyed => Health <= 0.0f;

    public float FootprintWorldRadius => RtsSimulation.ToWorldRadius(Definition.FootprintRadius);
    public float OccupancyRadius => RtsSimulation.ToWorldRadius(Definition.FootprintRadius + Definition.PlacementBuffer);

    internal void ApplyDamage(float rawDamage, string damageType)
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

    internal void UpgradeTo(BuildingDefinition definition)
    {
        var healthPercent = Definition.Health <= 0
            ? 1.0f
            : Health / Definition.Health;
        Definition = definition;
        Health = MathF.Max(1.0f, definition.Health * healthPercent);
        AttackCooldownRemaining = 0.0f;
    }
}
