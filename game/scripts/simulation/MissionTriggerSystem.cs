using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal sealed class MissionTriggerSystem
{
    private readonly MissionTriggerState[] _states;

    public MissionTriggerSystem(IEnumerable<MissionTriggerDefinition>? definitions)
    {
        _states = definitions?
            .Select(definition => new MissionTriggerState(definition))
            .ToArray() ?? [];
    }

    public void RecordPlayerBuildingPlaced(string buildingId, float elapsedSeconds)
    {
        foreach (var state in _states)
        {
            state.RecordPlayerBuildingPlaced(buildingId, elapsedSeconds);
        }
    }

    public void Tick(RtsSimulation simulation, float elapsedSeconds)
    {
        foreach (var state in _states)
        {
            if (!state.ShouldFire(elapsedSeconds))
            {
                continue;
            }

            var committed = simulation.CommitMissionTriggerEnemyPressure(state.Definition.EnemyAttackGroupSize);
            if (committed > 0)
            {
                state.MarkFired(elapsedSeconds);
            }
        }
    }
}

internal sealed class MissionTriggerState
{
    private float? _pendingFireSeconds;
    private float _lastFireSeconds = float.NegativeInfinity;
    private int _fireCount;

    public MissionTriggerState(MissionTriggerDefinition definition)
    {
        Definition = definition;
    }

    public MissionTriggerDefinition Definition { get; }

    public void RecordPlayerBuildingPlaced(string buildingId, float elapsedSeconds)
    {
        if (_fireCount >= Definition.MaxFireCount ||
            !Definition.WatchedPlayerBuildingIds.Contains(buildingId, StringComparer.Ordinal))
        {
            return;
        }

        if (elapsedSeconds < _lastFireSeconds + Definition.CooldownSeconds)
        {
            return;
        }

        var requestedFireSeconds = MathF.Max(
            Definition.MinElapsedSeconds,
            elapsedSeconds + MathF.Max(0.0f, Definition.CoalesceWindowSeconds));
        _pendingFireSeconds = _pendingFireSeconds is null
            ? requestedFireSeconds
            : MathF.Min(_pendingFireSeconds.Value, requestedFireSeconds);
    }

    public bool ShouldFire(float elapsedSeconds)
    {
        return _pendingFireSeconds is not null &&
            elapsedSeconds >= _pendingFireSeconds.Value &&
            _fireCount < Definition.MaxFireCount &&
            elapsedSeconds >= _lastFireSeconds + Definition.CooldownSeconds;
    }

    public void MarkFired(float elapsedSeconds)
    {
        _pendingFireSeconds = null;
        _lastFireSeconds = elapsedSeconds;
        _fireCount++;
    }
}
