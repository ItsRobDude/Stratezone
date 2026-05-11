namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private const float EnemyScoutDispatchDelaySeconds = 18.0f;
    private const float EnemyUnitPursuitRange = 9.0f;
    private const float EnemyBaseDefensePursuitRange = 18.0f;
    private const float EnemyRetreatHealthRatio = 0.32f;
    private const float EnemyRetreatRecoverRatio = 0.58f;

    private void TickEnemyPressure(float deltaSeconds)
    {
        DispatchEnemyPatrol();
        DispatchEnemyScout();
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
                    TickEnemyDefenderUnit(unit, deltaSeconds);
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
        if (_elapsedSeconds < _enemyAi.Profile.FirstAttackDelaySeconds ||
            _elapsedSeconds < _enemyOfficer.NextAttackAllowedSeconds)
        {
            return;
        }

        CommitIdleEnemyAttackers(Math.Max(1, _enemyAi.Profile.AttackGroupSize), requireFullGroup: true);
    }

    internal int CommitMissionTriggerEnemyPressure(int groupSize)
    {
        if (_elapsedSeconds < _enemyOfficer.NextAttackAllowedSeconds)
        {
            return 0;
        }

        return CommitIdleEnemyAttackers(Math.Max(1, groupSize), requireFullGroup: false);
    }

    private int CommitIdleEnemyAttackers(int groupSize, bool requireFullGroup)
    {
        var committedCount = _units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            unit.Definition.CanAttack &&
            unit.IsEnemyAttackCommitted);
        if (committedCount >= groupSize)
        {
            return 0;
        }

        var idleCombatUnits = _units
            .Where(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                unit.Definition.CanAttack &&
                unit.HealthRatio >= EnemyRetreatRecoverRatio &&
                !unit.IsEnemyAttackCommitted)
            .OrderBy(unit => unit.Position.DistanceTo(_enemyAi.HubPosition))
            .ToArray();
        if (idleCombatUnits.Length == 0 ||
            (requireFullGroup && idleCombatUnits.Length + committedCount < groupSize))
        {
            return 0;
        }

        var committedNow = 0;
        foreach (var unit in idleCombatUnits.Take(groupSize - committedCount))
        {
            unit.IsEnemyAttackCommitted = true;
            unit.IsEnemyScout = false;
            unit.IsEnemyRetreating = false;
            _knownCommittedEnemyIds.Add(unit.EntityId);
            committedNow++;
        }

        return committedNow;
    }

    private void DispatchEnemyScout()
    {
        if (_enemyAi.PatrolPositions.Count > 0)
        {
            return;
        }

        if (_enemyOfficer.ScoutDispatched ||
            _elapsedSeconds < EnemyScoutDispatchDelaySeconds ||
            _elapsedSeconds >= _enemyAi.Profile.FirstAttackDelaySeconds)
        {
            return;
        }

        var scout = _units
            .Where(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                unit.Definition.CanAttack &&
                unit.HealthRatio >= EnemyRetreatRecoverRatio &&
                !unit.IsEnemyAttackCommitted)
            .OrderByDescending(unit => unit.Definition.MovementSpeed)
            .ThenBy(unit => unit.Position.DistanceTo(_enemyAi.HubPosition))
            .FirstOrDefault();
        if (scout is null)
        {
            return;
        }

        scout.IsEnemyScout = true;
        _enemyOfficer.ScoutDispatched = true;
        SetUnitPathTo(scout, _enemyAi.RallyPosition);
    }

    private void DispatchEnemyPatrol()
    {
        if (_enemyAi.PatrolPositions.Count == 0 ||
            _enemyAi.Profile.MaxPatrolDispatches <= 0 ||
            _enemyPatrolDispatches >= _enemyAi.Profile.MaxPatrolDispatches ||
            _elapsedSeconds < _nextEnemyPatrolDispatchSeconds ||
            _elapsedSeconds >= _enemyAi.Profile.FirstAttackDelaySeconds)
        {
            return;
        }

        var patrolGroupSize = Math.Max(1, _enemyAi.Profile.PatrolGroupSize);
        var idleCombatUnits = _units
            .Where(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                unit.Definition.CanAttack &&
                unit.HealthRatio >= EnemyRetreatRecoverRatio &&
                !unit.IsEnemyAttackCommitted &&
                !unit.IsEnemyScout)
            .OrderBy(unit => unit.Position.DistanceTo(_enemyAi.HubPosition))
            .Take(patrolGroupSize)
            .ToArray();
        if (idleCombatUnits.Length == 0)
        {
            _nextEnemyPatrolDispatchSeconds = _elapsedSeconds + Math.Max(1.0f, _enemyAi.Profile.PatrolIntervalSeconds);
            return;
        }

        var destinationIndex = SelectEnemyPatrolDestinationIndex();
        var destination = _enemyAi.PatrolPositions[destinationIndex];
        foreach (var unit in idleCombatUnits)
        {
            unit.IsEnemyScout = true;
            unit.IsEnemyRetreating = false;
            unit.TargetUnitEntityId = null;
            unit.TargetBuildingEntityId = null;
            SetUnitPathTo(unit, destination);
        }

        _enemyPatrolDestinationIndexes.Add(destinationIndex);
        _enemyPatrolDispatches++;
        _enemyOfficer.PatrolDispatches = _enemyPatrolDispatches;
        _enemyOfficer.PatrolAreasVisited = _enemyPatrolDestinationIndexes.Count;
        _nextEnemyPatrolDispatchSeconds = _elapsedSeconds + Math.Max(1.0f, _enemyAi.Profile.PatrolIntervalSeconds);
    }

    private int SelectEnemyPatrolDestinationIndex()
    {
        var positions = _enemyAi.PatrolPositions;
        if (positions.Count == 0)
        {
            return 0;
        }

        if (_enemyPatrolDestinationOffset < 0)
        {
            _enemyPatrolDestinationOffset = _enemyPatrolRandom.Next(positions.Count);
        }

        return (_enemyPatrolDispatches + _enemyPatrolDestinationOffset) % positions.Count;
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
        if (TickUnitRepair(unit, deltaSeconds))
        {
            return;
        }

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

        if (unit.MoveTarget is not null)
        {
            MoveUnitToward(unit, unit.MoveTarget.Value, deltaSeconds);
            return;
        }

        var nearbyEnemy = FindNearestEnemyUnitInRange(unit);
        if (nearbyEnemy is not null)
        {
            TryUnitAttackUnit(unit, nearbyEnemy);
            return;
        }

        unit.ClearPath();
    }

    private void TickEnemyUnit(UnitState unit, float deltaSeconds)
    {
        if (ShouldEnemyRetreat(unit))
        {
            TickEnemyRetreat(unit, deltaSeconds);
            return;
        }

        var targetUnit = FindEnemyPriorityUnit(unit, ToWorldRadius(EnemyUnitPursuitRange));
        if (targetUnit is not null)
        {
            unit.TargetUnitEntityId = targetUnit.EntityId;
            unit.TargetBuildingEntityId = null;
            TickUnitAttackTarget(unit, targetUnit, deltaSeconds);
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

    private void TickEnemyDefenderUnit(UnitState unit, float deltaSeconds)
    {
        var baseIntruder = FindEnemyBaseIntruder(unit);
        if (baseIntruder is not null)
        {
            unit.IsEnemyScout = false;
            unit.TargetUnitEntityId = baseIntruder.EntityId;
            unit.TargetBuildingEntityId = null;
            TickUnitAttackTarget(unit, baseIntruder, deltaSeconds);
            return;
        }

        if (unit.IsEnemyScout)
        {
            TickEnemyScout(unit, deltaSeconds * _enemyAi.Profile.PressureSlowdownMultiplier);
        }

        var targetUnit = FindNearestEnemyUnitInRange(unit);
        if (targetUnit is null)
        {
            return;
        }

        unit.TargetUnitEntityId = targetUnit.EntityId;
        unit.TargetBuildingEntityId = null;
        TryUnitAttackUnit(unit, targetUnit);
    }

    private UnitState? FindEnemyBaseIntruder(UnitState unit)
    {
        if (!unit.Definition.CanAttack || unit.Definition.AttackRange <= 0.0f)
        {
            return null;
        }

        var defenseRadius = ToWorldRadius(EnemyBaseDefensePursuitRange);
        return _units
            .Where(candidate =>
                candidate.FactionId == ContentIds.Factions.PlayerExpedition &&
                !candidate.IsDestroyed &&
                candidate.Definition.CanAttack &&
                candidate.Position.DistanceTo(_enemyAi.HubPosition) <= defenseRadius)
            .OrderBy(candidate => candidate.Definition.Id == ContentIds.Units.Commander ? 0 : 1)
            .ThenBy(candidate => candidate.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private bool ShouldEnemyRetreat(UnitState unit)
    {
        if (!HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub))
        {
            return false;
        }

        if (unit.IsEnemyRetreating)
        {
            return unit.HealthRatio < EnemyRetreatRecoverRatio &&
                unit.Position.DistanceTo(_enemyAi.HubPosition) > GetEnemyRetreatStandDownDistance();
        }

        return unit.HealthRatio > 0.0f &&
            unit.HealthRatio <= EnemyRetreatHealthRatio &&
            unit.Position.DistanceTo(_enemyAi.HubPosition) > GetEnemyRetreatTriggerDistance();
    }

    private void TickEnemyRetreat(UnitState unit, float deltaSeconds)
    {
        if (!unit.IsEnemyRetreating)
        {
            _enemyOfficer.RetreatsOrdered++;
        }

        unit.IsEnemyRetreating = true;
        unit.TargetUnitEntityId = null;
        unit.TargetBuildingEntityId = null;
        MoveUnitToward(unit, _enemyAi.HubPosition, deltaSeconds);
        if (unit.Position.DistanceTo(_enemyAi.HubPosition) <= GetEnemyRetreatStandDownDistance())
        {
            unit.IsEnemyRetreating = false;
            unit.IsEnemyAttackCommitted = false;
            unit.ClearPath();
        }
    }

    private float GetEnemyRetreatStandDownDistance()
    {
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        return hub is null
            ? ToWorldRadius(2.0f)
            : hub.FootprintWorldRadius + ToWorldRadius(1.0f);
    }

    private float GetEnemyRetreatTriggerDistance()
    {
        return GetEnemyRetreatStandDownDistance() + ToWorldRadius(2.0f);
    }

    private void TickEnemyScout(UnitState unit, float deltaSeconds)
    {
        if (unit.MoveTarget is null || unit.Position.DistanceTo(unit.MoveTarget.Value) < 24.0f)
        {
            SetUnitPathTo(unit, _enemyAi.RallyPosition);
        }

        if (unit.MoveTarget is not null)
        {
            MoveUnitToward(unit, unit.MoveTarget.Value, deltaSeconds);
        }
    }

    private void TickUnitAttackTarget(UnitState attacker, UnitState target, float deltaSeconds)
    {
        var distanceToTarget = attacker.Position.DistanceTo(target.Position);
        if (attacker.Definition.CanAttack && distanceToTarget <= ToWorldRadius(attacker.Definition.AttackRange))
        {
            TryUnitAttackUnit(attacker, target);
            return;
        }

        MoveUnitToward(attacker, target.Position + attacker.TargetFormationOffset, deltaSeconds);
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

        var approachPoint = GetBuildingAttackApproachPoint(attacker, target, attackRange) + attacker.TargetFormationOffset;
        MoveUnitToward(attacker, approachPoint, deltaSeconds);
    }

    private static SimVector2 GetBuildingAttackApproachPoint(UnitState attacker, BuildingState target, float attackRange)
    {
        var directionFromTarget = (attacker.Position - target.Position).Normalized();
        if (directionFromTarget.Length() <= 0.0001f)
        {
            directionFromTarget = new SimVector2(-1.0f, 0.0f);
        }

        var standOffDistance = MathF.Max(target.FootprintWorldRadius + 4.0f, attackRange - 0.5f);
        return target.Position + (directionFromTarget * standOffDistance);
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
            return FindEnemyPriorityBuilding(unit) ?? hub;
        }

        if (_knownWallBlockedEnemyIds.Add(unit.EntityId))
        {
            _enemyOfficer.WallBlocksEncountered++;
        }
        var startAnchor = FindLiveBuilding(blockingWall.StartAnchorEntityId);
        var endAnchor = FindLiveBuilding(blockingWall.EndAnchorEntityId);
        return new[] { startAnchor, endAnchor }
            .Where(anchor => anchor is not null)
            .OrderBy(anchor => anchor!.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private UnitState? FindEnemyPriorityUnit(UnitState unit, float pursuitRange)
    {
        if (!unit.Definition.CanAttack || unit.Definition.AttackRange <= 0.0f)
        {
            return null;
        }

        return _units
            .Where(candidate =>
                candidate.FactionId == ContentIds.Factions.PlayerExpedition &&
                !candidate.IsDestroyed &&
                IsCurrentlyObservedByFaction(ContentIds.Factions.PrivateMilitary, candidate.Position) &&
                candidate.Position.DistanceTo(unit.Position) <= pursuitRange)
            .OrderBy(candidate => candidate.Definition.Id == ContentIds.Units.Commander ? 0 : 1)
            .ThenBy(candidate => candidate.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private BuildingState? FindEnemyPriorityBuilding(UnitState unit)
    {
        return _buildings
            .Where(building =>
                building.FactionId == ContentIds.Factions.PlayerExpedition &&
                !building.IsDestroyed &&
                IsCurrentlyObservedByFaction(ContentIds.Factions.PrivateMilitary, building.Position))
            .OrderBy(building => GetEnemyBuildingPriority(building.Definition.Id))
            .ThenBy(building => building.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private static int GetEnemyBuildingPriority(string buildingId)
    {
        return buildingId switch
        {
            ContentIds.Buildings.ExtractorRefinery => 0,
            ContentIds.Buildings.PowerPlant => 1,
            ContentIds.Buildings.Pylon => 1,
            ContentIds.Buildings.Barracks => 2,
            ContentIds.Buildings.ColonyHub => 3,
            _ => 4
        };
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
