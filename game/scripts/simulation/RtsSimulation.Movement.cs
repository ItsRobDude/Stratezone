namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private readonly HashSet<(int CrusherId, int TargetId)> _crushContactsThisStep = [];

    public void CommandUnitMove(int unitEntityId, SimVector2 position)
    {
        var unit = FindLiveUnit(unitEntityId);
        if (unit is null)
        {
            return;
        }

        unit.ClearPath();
        unit.ClearCommandTargets(clearAttackPresentation: true);
        SetUnitPathTo(unit, position);
    }

    public void CommandUnitAttackUnit(int unitEntityId, int targetUnitEntityId, SimVector2 formationOffset = default)
    {
        var unit = FindLiveUnit(unitEntityId);
        var target = FindLiveUnit(targetUnitEntityId);
        if (unit is null || target is null || unit.FactionId == target.FactionId)
        {
            return;
        }

        unit.ClearPath();
        unit.ClearCommandTargets();
        unit.TargetUnitEntityId = target.EntityId;
        unit.TargetFormationOffset = formationOffset;
    }

    public void CommandUnitAttackBuilding(int unitEntityId, int targetBuildingEntityId, SimVector2 formationOffset = default)
    {
        var unit = FindLiveUnit(unitEntityId);
        var target = FindLiveBuilding(targetBuildingEntityId);
        if (unit is null || target is null || unit.FactionId == target.FactionId)
        {
            return;
        }

        unit.ClearPath();
        unit.ClearCommandTargets();
        unit.TargetBuildingEntityId = target.EntityId;
        unit.TargetFormationOffset = formationOffset;
    }

    public void CommandUnitAttackBridge(int unitEntityId, string targetBridgeId, SimVector2 formationOffset = default)
    {
        var unit = FindLiveUnit(unitEntityId);
        var target = FindBridge(targetBridgeId);
        if (unit is null || target is null || !target.IsIntact)
        {
            return;
        }

        unit.ClearPath();
        unit.ClearCommandTargets();
        unit.TargetBridgeId = target.Id;
        unit.TargetFormationOffset = formationOffset;
    }

    public bool IsLineBlockedByEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.Any(wall => DoesEnergyWallBlockLine(wall, start, end));
    }

    private void MoveUnitToward(UnitState unit, SimVector2 target, float deltaSeconds)
    {
        if (NeedsNewPath(unit, target))
        {
            SetUnitPathTo(unit, target);
        }

        if (unit.IsPathBlocked)
        {
            return;
        }

        var remainingDistance = unit.Definition.MovementSpeed * CombatMovementScale * deltaSeconds;
        // Consume a distance budget, including across turns. Reaching a waypoint must
        // not cost an extra simulation step or give a free four-unit position snap.
        while (unit.CurrentWaypoint is { } waypoint)
        {
            var direction = waypoint - unit.Position;
            var distance = direction.Length();
            if (distance <= 0.0001f)
            {
                unit.AdvanceWaypoint();
                continue;
            }
            if (remainingDistance <= 0) break;
            var traveled = MathF.Min(remainingDistance, distance);
            var start = unit.Position;
            unit.Position = traveled >= distance ? waypoint : start + direction.Normalized() * traveled;
            CombatResolver.TryCrushInfantry(unit, start, unit.Position, _units, _crushContactsThisStep);
            remainingDistance -= traveled;
            if (traveled >= distance) unit.AdvanceWaypoint();
            else break;
        }
        if (unit.CurrentWaypoint is null) unit.ClearPath();
    }

    private void SetUnitPathTo(UnitState unit, SimVector2 target)
    {
        DebugPathRequestCount++;
        var path = PathfindingSystem.FindPath(
            unit.Position,
            target,
            _buildings,
            GetBlockingEnergyWallsForFaction(unit.FactionId),
            _map?.TerrainRegions ?? [],
            _bridges,
            _playableBounds);

        if (path.Success)
        {
            unit.SetPath(target, path.Destination, path.Waypoints);
            unit.IsBlockedByEnergyWall = false;
            return;
        }

        unit.SetPathBlocked(target, path.Message);
        unit.IsBlockedByEnergyWall = FindBlockingEnergyWallForFaction(unit.FactionId, unit.Position, target) is not null;
    }

    // A point outside the building, standOffDistance from the wall nearest to `from`.
    private static SimVector2 GetBuildingApproachPoint(SimVector2 from, BuildingState target, float standOffDistance)
    {
        var nearestWall = target.Footprint.ClosestPoint(from);
        var directionFromWall = (from - nearestWall).Normalized();
        if (directionFromWall.Length() <= 0.0001f)
        {
            directionFromWall = (from - target.Position).Normalized();
        }

        if (directionFromWall.Length() <= 0.0001f)
        {
            directionFromWall = new SimVector2(-1.0f, 0.0f);
        }

        return nearestWall + (directionFromWall * standOffDistance);
    }

    private static bool NeedsNewPath(UnitState unit, SimVector2 target)
    {
        if (unit.MoveTarget is null)
        {
            return true;
        }

        if (unit.IsPathBlocked)
        {
            return true;
        }

        // Compare against what was requested, not the substituted fallback, so a unit headed
        // for a blocked point does not repath every tick.
        var requested = unit.RequestedMoveTarget ?? unit.MoveTarget.Value;
        return unit.PathWaypoints.Count == 0 || requested.DistanceTo(target) > 32.0f;
    }

    private EnergyWallSegment? FindBlockingEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.FirstOrDefault(wall => DoesEnergyWallBlockLine(wall, start, end));
    }

    private EnergyWallSegment? FindBlockingEnergyWallForFaction(string factionId, SimVector2 start, SimVector2 end)
    {
        return GetBlockingEnergyWallsForFaction(factionId)
            .FirstOrDefault(wall => DoesEnergyWallBlockLine(wall, start, end));
    }

    private bool IsBuildingFootprintBlockedByEnergyWall(BuildingFootprint footprint)
    {
        return _energyWalls.Any(wall =>
            footprint.DistanceToSegment(wall.ExtendedStart, wall.ExtendedEnd) <= EnergyWallSegment.PlacementBuffer);
    }

    private static bool DoesEnergyWallBlockLine(EnergyWallSegment wall, SimVector2 start, SimVector2 end)
    {
        return SimulationGeometry.DistanceSegmentToSegment(start, end, wall.ExtendedStart, wall.ExtendedEnd) <= EnergyWallSegment.BlockingClearance;
    }

    private IReadOnlyList<EnergyWallSegment> GetBlockingEnergyWallsForFaction(string factionId)
    {
        return _energyWalls
            .Where(wall =>
            {
                var startAnchor = FindLiveBuilding(wall.StartAnchorEntityId);
                return startAnchor is not null && startAnchor.FactionId != factionId;
            })
            .ToArray();
    }
}
