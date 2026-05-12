using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class BridgeState
{
    public BridgeState(MapObjectDefinition definition, float startingHealthPercent = 1.0f)
    {
        Definition = definition;
        CurrentHealth = Math.Clamp(startingHealthPercent, 0.0f, 1.0f) * definition.MaxHealth;
        if (!definition.StartsIntact)
        {
            CurrentHealth = 0.0f;
        }
    }

    public MapObjectDefinition Definition { get; }
    public string Id => Definition.Id;
    public SimVector2 Center => Definition.Center;
    public float CurrentHealth { get; private set; }
    public float MaxHealth => Definition.MaxHealth;
    public bool IsIntact => CurrentHealth > 0.0f;
    public bool IsDamaged => CurrentHealth < MaxHealth;
    public float HealthRatio => MaxHealth <= 0.0f ? 0.0f : CurrentHealth / MaxHealth;

    public bool Contains(SimVector2 position, float padding = 0.0f)
    {
        return Definition.Contains(position, padding);
    }

    public float DistanceTo(SimVector2 position)
    {
        return Definition.DistanceTo(position);
    }

    public float ApplyDamage(float amount)
    {
        if (amount <= 0.0f || CurrentHealth <= 0.0f)
        {
            return 0.0f;
        }

        var previous = CurrentHealth;
        CurrentHealth = MathF.Max(0.0f, CurrentHealth - amount);
        return previous - CurrentHealth;
    }

    public float Repair(float amount)
    {
        if (amount <= 0.0f || CurrentHealth >= MaxHealth)
        {
            return 0.0f;
        }

        var previous = CurrentHealth;
        CurrentHealth = MathF.Min(MaxHealth, CurrentHealth + amount);
        return CurrentHealth - previous;
    }
}
