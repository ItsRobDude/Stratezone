using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class UnitState
{
    private readonly List<SimVector2> _pathWaypoints = [];

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
    public SimVector2? MoveTarget { get; internal set; }
    public IReadOnlyList<SimVector2> PathWaypoints => _pathWaypoints;
    public int CurrentWaypointIndex { get; private set; }
    public bool IsPathBlocked { get; private set; }
    public string? PathBlockedReason { get; private set; }
    public int? TargetUnitEntityId { get; internal set; }
    public int? TargetBuildingEntityId { get; internal set; }
    public int? RepairTargetBuildingEntityId { get; internal set; }
    public SimVector2 TargetFormationOffset { get; internal set; }
    public bool IsBlockedByEnergyWall { get; internal set; }
    public bool IsEnemyAttackCommitted { get; internal set; }
    public bool IsEnemyScout { get; internal set; }
    public bool IsEnemyRetreating { get; internal set; }
    public SimVector2? LastIncomingAttackOrigin { get; private set; }
    public SimVector2? LastAttackTargetPosition { get; private set; }
    public float HitFlashSeconds { get; private set; }
    public float AttackFlashSeconds { get; private set; }
    public bool IsDestroyed => Health <= 0.0f;
    internal float HealthRatio => Definition.Health <= 0 ? 0.0f : Health / Definition.Health;

    internal SimVector2? CurrentWaypoint => CurrentWaypointIndex < _pathWaypoints.Count
        ? _pathWaypoints[CurrentWaypointIndex]
        : null;

    internal void SetPath(SimVector2 destination, IReadOnlyList<SimVector2> waypoints)
    {
        MoveTarget = destination;
        _pathWaypoints.Clear();
        _pathWaypoints.AddRange(waypoints);
        CurrentWaypointIndex = 0;
        IsPathBlocked = false;
        PathBlockedReason = null;
    }

    internal void SetPathBlocked(SimVector2 destination, string reason)
    {
        MoveTarget = destination;
        _pathWaypoints.Clear();
        CurrentWaypointIndex = 0;
        IsPathBlocked = true;
        PathBlockedReason = reason;
    }

    internal void ClearPath()
    {
        MoveTarget = null;
        _pathWaypoints.Clear();
        CurrentWaypointIndex = 0;
        IsPathBlocked = false;
        PathBlockedReason = null;
    }

    internal void AdvanceWaypoint()
    {
        if (CurrentWaypointIndex < _pathWaypoints.Count)
        {
            CurrentWaypointIndex++;
        }
    }

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

    internal void RegisterIncomingAttack(SimVector2 origin)
    {
        LastIncomingAttackOrigin = origin;
        HitFlashSeconds = 0.28f;
    }

    internal void RegisterOutgoingAttack(SimVector2 targetPosition)
    {
        LastAttackTargetPosition = targetPosition;
        AttackFlashSeconds = 0.22f;
    }

    internal void TickPresentation(float deltaSeconds)
    {
        HitFlashSeconds = MathF.Max(0.0f, HitFlashSeconds - deltaSeconds);
        AttackFlashSeconds = MathF.Max(0.0f, AttackFlashSeconds - deltaSeconds);
    }
}
