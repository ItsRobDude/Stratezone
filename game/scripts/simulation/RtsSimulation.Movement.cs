namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
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

        var waypoint = unit.CurrentWaypoint;
        if (waypoint is null)
        {
            unit.ClearPath();
            return;
        }

        var direction = waypoint.Value - unit.Position;
        var distance = direction.Length();
        if (distance <= 4.0f)
        {
            unit.Position = waypoint.Value;
            unit.AdvanceWaypoint();
            if (unit.CurrentWaypoint is null)
            {
                unit.ClearPath();
            }

            return;
        }

        var stepDistance = unit.Definition.MovementSpeed * CombatMovementScale * deltaSeconds;
        var start = unit.Position;
        unit.Position += direction.Normalized() * MathF.Min(stepDistance, distance);
        CombatResolver.TryCrushInfantry(unit, start, unit.Position, _units);
    }

    private void SetUnitPathTo(UnitState unit, SimVector2 target)
    {
        var path = PathfindingSystem.FindPath(
            unit.Position,
            target,
            _buildings,
            GetBlockingEnergyWallsForFaction(unit.FactionId),
            _map?.TerrainRegions ?? [],
            _bridges);

        if (path.Success)
        {
            unit.SetPath(path.Destination, path.Waypoints);
            unit.IsBlockedByEnergyWall = false;
            return;
        }

        unit.SetPathBlocked(target, path.Message);
        unit.IsBlockedByEnergyWall = FindBlockingEnergyWallForFaction(unit.FactionId, unit.Position, target) is not null;
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

        return unit.PathWaypoints.Count == 0 || unit.MoveTarget.Value.DistanceTo(target) > 32.0f;
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

    private bool IsBuildingFootprintBlockedByEnergyWall(SimVector2 position, float footprintRadius)
    {
        return _energyWalls.Any(wall =>
            SimulationGeometry.DistancePointToSegment(position, wall.ExtendedStart, wall.ExtendedEnd) <= footprintRadius + EnergyWallSegment.PlacementBuffer);
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
