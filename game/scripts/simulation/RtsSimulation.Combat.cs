namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private void TickEnemyPressure(float deltaSeconds)
    {
        CommitEnemyAttackGroup();

        foreach (var unit in _units.Where(unit => !unit.IsDestroyed))
        {
            unit.AttackCooldownRemaining = MathF.Max(0.0f, unit.AttackCooldownRemaining - deltaSeconds);

            if (unit.FactionId == ContentIds.Factions.PrivateMilitary)
            {
                if (unit.IsEnemyAttackCommitted)
                {
                    TickEnemyUnit(unit, deltaSeconds * _enemyAi.Profile.PressureSlowdownMultiplier);
                }
                else
                {
                    TickEnemyDefenderUnit(unit);
                }
            }
            else
            {
                TickPlayerUnit(unit, deltaSeconds);
            }
        }
    }

    private void CommitEnemyAttackGroup()
    {
        if (_elapsedSeconds < _enemyAi.Profile.FirstAttackDelaySeconds)
        {
            return;
        }

        var groupSize = Math.Max(1, _enemyAi.Profile.AttackGroupSize);
        var committedCount = _units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            unit.Definition.CanAttack &&
            unit.IsEnemyAttackCommitted);
        if (committedCount >= groupSize)
        {
            return;
        }

        var idleCombatUnits = _units
            .Where(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                unit.Definition.CanAttack &&
                !unit.IsEnemyAttackCommitted)
            .OrderBy(unit => unit.Position.DistanceTo(_enemyAi.HubPosition))
            .ToArray();
        if (idleCombatUnits.Length + committedCount < groupSize)
        {
            return;
        }

        foreach (var unit in idleCombatUnits.Take(groupSize - committedCount))
        {
            unit.IsEnemyAttackCommitted = true;
        }
    }

    private void TickBuildingAttacks(float deltaSeconds)
    {
        foreach (var building in _buildings.Where(building => !building.IsDestroyed))
        {
            building.AttackCooldownRemaining = MathF.Max(0.0f, building.AttackCooldownRemaining - deltaSeconds);
            if (!building.IsPowered ||
                building.Definition.AttackDamage <= 0.0f ||
                building.Definition.AttackRange <= 0.0f ||
                building.AttackCooldownRemaining > 0.0f)
            {
                continue;
            }

            var targetUnit = FindNearestHostileUnitForBuilding(building);
            if (targetUnit is not null)
            {
                CombatResolver.ResolveBuildingAttack(building, targetUnit, _units, _buildings);
                RecomputePower();
                continue;
            }

            if (!building.Definition.TargetFilters.Contains("building", StringComparer.Ordinal))
            {
                continue;
            }

            var targetBuilding = FindNearestHostileBuildingForBuilding(building);
            if (targetBuilding is null)
            {
                continue;
            }

            CombatResolver.ResolveBuildingAttack(building, targetBuilding, _units, _buildings);
            RecomputePower();
        }
    }

    private void TickPlayerUnit(UnitState unit, float deltaSeconds)
    {
        var targetUnit = unit.TargetUnitEntityId is null ? null : FindLiveUnit(unit.TargetUnitEntityId.Value);
        if (targetUnit is not null && targetUnit.FactionId != unit.FactionId)
        {
            TickUnitAttackTarget(unit, targetUnit, deltaSeconds);
            return;
        }

        var targetBuilding = unit.TargetBuildingEntityId is null ? null : FindLiveBuilding(unit.TargetBuildingEntityId.Value);
        if (targetBuilding is not null && targetBuilding.FactionId != unit.FactionId)
        {
            TickUnitAttackTarget(unit, targetBuilding, deltaSeconds);
            return;
        }

        var nearbyEnemy = FindNearestEnemyUnitInRange(unit);
        if (nearbyEnemy is not null)
        {
            TryUnitAttackUnit(unit, nearbyEnemy);
            return;
        }

        if (unit.MoveTarget is not null)
        {
            MoveUnitToward(unit, unit.MoveTarget.Value, deltaSeconds);
        }
    }

    private void TickEnemyUnit(UnitState unit, float deltaSeconds)
    {
        var targetUnit = FindNearestEnemyUnitInRange(unit);
        if (targetUnit is not null)
        {
            unit.TargetUnitEntityId = targetUnit.EntityId;
            unit.TargetBuildingEntityId = null;
            TryUnitAttackUnit(unit, targetUnit);
            return;
        }

        var target = GetEnemyTargetBuilding(unit);
        unit.TargetUnitEntityId = null;
        unit.TargetBuildingEntityId = target?.EntityId;
        if (target is null)
        {
            return;
        }

        TickUnitAttackTarget(unit, target, deltaSeconds);
    }

    private void TickEnemyDefenderUnit(UnitState unit)
    {
        var targetUnit = FindNearestEnemyUnitInRange(unit);
        if (targetUnit is null)
        {
            return;
        }

        unit.TargetUnitEntityId = targetUnit.EntityId;
        unit.TargetBuildingEntityId = null;
        TryUnitAttackUnit(unit, targetUnit);
    }

    private void TickUnitAttackTarget(UnitState attacker, UnitState target, float deltaSeconds)
    {
        var distanceToTarget = attacker.Position.DistanceTo(target.Position);
        if (attacker.Definition.CanAttack && distanceToTarget <= ToWorldRadius(attacker.Definition.AttackRange))
        {
            TryUnitAttackUnit(attacker, target);
            return;
        }

        MoveUnitToward(attacker, target.Position, deltaSeconds);
    }

    private void TickUnitAttackTarget(UnitState attacker, BuildingState target, float deltaSeconds)
    {
        var distanceToTarget = attacker.Position.DistanceTo(target.Position);
        var attackRange = ToWorldRadius(attacker.Definition.AttackRange) + target.FootprintWorldRadius;
        if (attacker.Definition.CanAttack && distanceToTarget <= attackRange)
        {
            TryUnitAttackBuilding(attacker, target);
            return;
        }

        MoveUnitToward(attacker, target.Position, deltaSeconds);
    }

    private BuildingState? GetEnemyTargetBuilding(UnitState unit)
    {
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        if (hub is null)
        {
            unit.IsBlockedByEnergyWall = false;
            return null;
        }

        var blockingWall = FindBlockingEnergyWallForFaction(unit.FactionId, unit.Position, hub.Position);
        unit.IsBlockedByEnergyWall = blockingWall is not null;
        if (blockingWall is null)
        {
            return hub;
        }

        var startAnchor = FindLiveBuilding(blockingWall.StartAnchorEntityId);
        var endAnchor = FindLiveBuilding(blockingWall.EndAnchorEntityId);
        return new[] { startAnchor, endAnchor }
            .Where(anchor => anchor is not null)
            .OrderBy(anchor => anchor!.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private void TryUnitAttackBuilding(UnitState unit, BuildingState target)
    {
        if (unit.AttackCooldownRemaining > 0.0f || target.IsDestroyed || unit.Definition.AttackDamage <= 0.0f)
        {
            return;
        }

        CombatResolver.ResolveUnitAttack(unit, target, _units, _buildings);
        RecomputePower();
    }

    private void TryUnitAttackUnit(UnitState attacker, UnitState target)
    {
        if (attacker.AttackCooldownRemaining > 0.0f ||
            target.IsDestroyed ||
            attacker.Definition.AttackDamage <= 0.0f)
        {
            return;
        }

        CombatResolver.ResolveUnitAttack(attacker, target, _units, _buildings);
        RecomputePower();
    }

    private UnitState? FindNearestEnemyUnitInRange(UnitState unit)
    {
        if (!unit.Definition.CanAttack || unit.Definition.AttackRange <= 0.0f)
        {
            return null;
        }

        var range = ToWorldRadius(unit.Definition.AttackRange);
        return _units
            .Where(candidate => candidate.FactionId != unit.FactionId && !candidate.IsDestroyed)
            .Where(candidate => unit.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(unit.Position) <= range)
            .OrderBy(candidate => candidate.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private UnitState? FindNearestHostileUnitForBuilding(BuildingState building)
    {
        var range = ToWorldRadius(building.Definition.AttackRange) + building.FootprintWorldRadius;
        return _units
            .Where(candidate => candidate.FactionId != building.FactionId && !candidate.IsDestroyed)
            .Where(candidate => building.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(building.Position) <= range)
            .OrderBy(candidate => candidate.Position.DistanceTo(building.Position))
            .FirstOrDefault();
    }

    private BuildingState? FindNearestHostileBuildingForBuilding(BuildingState building)
    {
        var range = ToWorldRadius(building.Definition.AttackRange) + building.FootprintWorldRadius;
        return _buildings
            .Where(candidate => candidate.FactionId != building.FactionId && !candidate.IsDestroyed)
            .Where(candidate => building.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(building.Position) <= range + candidate.FootprintWorldRadius)
            .OrderBy(candidate => candidate.Position.DistanceTo(building.Position))
            .FirstOrDefault();
    }
}
