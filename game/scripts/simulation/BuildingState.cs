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
    public SimVector2? LastIncomingAttackOrigin { get; private set; }
    public float HitFlashSeconds { get; private set; }
    private readonly HashSet<string> _completedBarracksUpgradeIds = [];
    public bool IsDestroyed => Health <= 0.0f;
    public bool IsDamaged => !IsDestroyed && Health < Definition.Health;
    public string? ActiveBarracksUpgradeId { get; private set; }
    public float ActiveBarracksUpgradeRemainingSeconds { get; private set; }
    public bool IsBarracksUpgradeInProgress => ActiveBarracksUpgradeId is not null;

    public float FootprintWorldRadius => RtsSimulation.ToWorldRadius(Definition.FootprintRadius);
    public float OccupancyRadius => RtsSimulation.ToWorldRadius(Definition.FootprintRadius + Definition.PlacementBuffer);

    internal bool ApplyDamage(float rawDamage, string damageType)
    {
        if (IsDestroyed || rawDamage <= 0.0f)
        {
            return false;
        }

        var wasDestroyed = IsDestroyed;
        var resistance = Definition.DamageResistances.TryGetValue(damageType, out var value)
            ? value
            : 0.0f;
        var finalDamage = rawDamage * MathF.Max(0.0f, 1.0f - resistance);
        Health = MathF.Max(0.0f, Health - finalDamage);
        return !wasDestroyed && IsDestroyed;
    }

    internal float Repair(float healthAmount)
    {
        if (IsDestroyed || healthAmount <= 0.0f)
        {
            return 0.0f;
        }

        var previousHealth = Health;
        Health = MathF.Min(Definition.Health, Health + healthAmount);
        return Health - previousHealth;
    }

    internal void RegisterIncomingAttack(SimVector2 origin)
    {
        LastIncomingAttackOrigin = origin;
        HitFlashSeconds = 0.28f;
    }

    internal void TickPresentation(float deltaSeconds)
    {
        HitFlashSeconds = MathF.Max(0.0f, HitFlashSeconds - deltaSeconds);
    }

    public bool HasBarracksUpgrade(string upgradeId)
    {
        return _completedBarracksUpgradeIds.Contains(upgradeId);
    }

    internal void StartBarracksUpgrade(string upgradeId, float durationSeconds)
    {
        ActiveBarracksUpgradeId = upgradeId;
        ActiveBarracksUpgradeRemainingSeconds = MathF.Max(0.1f, durationSeconds);
    }

    internal bool TickBarracksUpgrade(float deltaSeconds)
    {
        if (ActiveBarracksUpgradeId is null)
        {
            return false;
        }

        ActiveBarracksUpgradeRemainingSeconds = MathF.Max(0.0f, ActiveBarracksUpgradeRemainingSeconds - deltaSeconds);
        if (ActiveBarracksUpgradeRemainingSeconds > 0.0f)
        {
            return false;
        }

        _completedBarracksUpgradeIds.Add(ActiveBarracksUpgradeId);
        ActiveBarracksUpgradeId = null;
        return true;
    }

    internal void CompleteBarracksUpgrade(string upgradeId)
    {
        _completedBarracksUpgradeIds.Add(upgradeId);
        if (ActiveBarracksUpgradeId == upgradeId)
        {
            ActiveBarracksUpgradeId = null;
            ActiveBarracksUpgradeRemainingSeconds = 0.0f;
        }
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
