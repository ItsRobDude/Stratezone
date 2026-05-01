namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private void UpdateEnemyOfficerState()
    {
        TrackEnemyAttackGroupLosses();
        TrackEnemyPowerStrikes();
        TrackCommanderSight();
    }

    private void TrackEnemyAttackGroupLosses()
    {
        if (_knownCommittedEnemyIds.Count == 0)
        {
            return;
        }

        var liveCommitted = _units.Any(unit =>
            _knownCommittedEnemyIds.Contains(unit.EntityId) &&
            !unit.IsDestroyed &&
            unit.IsEnemyAttackCommitted);
        if (liveCommitted)
        {
            return;
        }

        var destroyedCommitted = _units.Any(unit =>
            _knownCommittedEnemyIds.Contains(unit.EntityId) &&
            unit.IsDestroyed);
        if (destroyedCommitted)
        {
            _enemyOfficer.AttackGroupsLost++;
            _enemyOfficer.NextAttackAllowedSeconds = MathF.Max(
                _enemyOfficer.NextAttackAllowedSeconds,
                _elapsedSeconds + EnemyRegroupDelaySeconds);
        }

        _knownCommittedEnemyIds.RemoveWhere(id =>
        {
            var unit = _units.FirstOrDefault(candidate => candidate.EntityId == id);
            return unit is null || unit.IsDestroyed || !unit.IsEnemyAttackCommitted;
        });
    }

    private void TrackEnemyPowerStrikes()
    {
        foreach (var building in _buildings.Where(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.IsDestroyed &&
            (building.Definition.Id == ContentIds.Buildings.PowerPlant ||
                building.Definition.Id == ContentIds.Buildings.Pylon)))
        {
            if (_knownDestroyedEnemyPowerIds.Add(building.EntityId))
            {
                _enemyOfficer.PowerStrikesTaken++;
            }
        }
    }

    private void TrackCommanderSight()
    {
        _enemyOfficer.CommanderSighted = _units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Commander &&
            !unit.IsDestroyed &&
            IsCurrentlyObservedByFaction(ContentIds.Factions.PrivateMilitary, unit.Position));
    }
}
