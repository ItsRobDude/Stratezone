using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal static class PathfindingSystem
{
    private const float CellSize = 32.0f;
    private const float UnitClearance = 18.0f;
    private const int DestinationFallbackRadiusCells = 6;

    public static PathResult FindPath(
        SimVector2 start,
        SimVector2 destination,
        IReadOnlyList<BuildingState> buildings,
        IReadOnlyList<EnergyWallSegment> blockingWalls,
        IReadOnlyList<MapRegionDefinition> terrainRegions,
        IReadOnlyList<BridgeState>? bridges = null,
        PlayableBounds? bounds = null)
    {
        var grid = new PathfindingGrid(buildings, blockingWalls, terrainRegions, bridges ?? [], start, bounds ?? PlayableBounds.Default);
        var startCell = grid.ToCell(start);
        var preferredDestination = grid.ToCell(destination);
        var candidateDestinations = grid.GetDestinationCandidates(preferredDestination, startCell)
            .ToArray();

        foreach (var destinationCell in candidateDestinations)
        {
            var result = TryFindPath(grid, startCell, destinationCell);
            if (result is null)
            {
                continue;
            }

            var destinationPoint = destinationCell == preferredDestination && !grid.IsBlocked(preferredDestination, startCell)
                ? destination
                : grid.ToCenter(destinationCell);
            var waypoints = SmoothWaypoints(result.Select(grid.ToCenter).ToArray(), destinationPoint);
            return new PathResult(true, "Path found.", waypoints, destinationPoint);
        }

        return new PathResult(false, "No reachable path.", [], destination);
    }

    public static IReadOnlyList<PathfindingDebugCell> SampleBlockedCells(
        IReadOnlyList<BuildingState> buildings,
        IReadOnlyList<EnergyWallSegment> blockingWalls,
        IReadOnlyList<MapRegionDefinition> terrainRegions,
        IReadOnlyList<BridgeState> bridges,
        float cellSize = 64.0f,
        PlayableBounds? bounds = null)
    {
        var resolvedBounds = bounds ?? PlayableBounds.Default;
        var grid = new PathfindingGrid(buildings, blockingWalls, terrainRegions, bridges, new SimVector2(resolvedBounds.MinX, resolvedBounds.MinY), resolvedBounds, cellSize);
        var cells = new List<PathfindingDebugCell>();
        for (var x = 0; x <= grid.MaxCellX; x++)
        {
            for (var y = 0; y <= grid.MaxCellY; y++)
            {
                var cell = new PathCell(x, y);
                if (grid.IsBlocked(cell, new PathCell(-1, -1)))
                {
                    cells.Add(new PathfindingDebugCell(grid.ToCenter(cell), cellSize));
                }
            }
        }

        return cells;
    }

    private static IReadOnlyList<PathCell>? TryFindPath(PathfindingGrid grid, PathCell start, PathCell destination)
    {
        var frontier = new PriorityQueue<PathCell, float>();
        frontier.Enqueue(start, 0.0f);

        var cameFrom = new Dictionary<PathCell, PathCell>();
        var costSoFar = new Dictionary<PathCell, float>
        {
            [start] = 0.0f
        };

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == destination)
            {
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in grid.GetNeighbors(current, start))
            {
                var newCost = costSoFar[current] + current.DistanceTo(neighbor);
                if (costSoFar.TryGetValue(neighbor, out var oldCost) && newCost >= oldCost)
                {
                    continue;
                }

                costSoFar[neighbor] = newCost;
                frontier.Enqueue(neighbor, newCost + neighbor.DistanceTo(destination));
                cameFrom[neighbor] = current;
            }
        }

        return null;
    }

    private static IReadOnlyList<PathCell> ReconstructPath(Dictionary<PathCell, PathCell> cameFrom, PathCell current)
    {
        var cells = new List<PathCell> { current };
        while (cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            cells.Add(current);
        }

        cells.Reverse();
        if (cells.Count > 0)
        {
            cells.RemoveAt(0);
        }

        return cells;
    }

    private static IReadOnlyList<SimVector2> SmoothWaypoints(IReadOnlyList<SimVector2> gridWaypoints, SimVector2 destination)
    {
        if (gridWaypoints.Count == 0)
        {
            return [destination];
        }

        var smoothed = new List<SimVector2>();
        SimVector2? previousDirection = null;
        var previousPoint = gridWaypoints[0];

        for (var index = 1; index < gridWaypoints.Count; index++)
        {
            var direction = NormalizeDirection(gridWaypoints[index] - previousPoint);
            if (previousDirection is not null && direction != previousDirection.Value)
            {
                smoothed.Add(previousPoint);
            }

            previousDirection = direction;
            previousPoint = gridWaypoints[index];
        }

        smoothed.Add(destination);
        return smoothed;
    }

    private static SimVector2 NormalizeDirection(SimVector2 direction)
    {
        return new SimVector2(MathF.Sign(direction.X), MathF.Sign(direction.Y));
    }

    private readonly record struct PathCell(int X, int Y)
    {
        public float DistanceTo(PathCell other)
        {
            var dx = X - other.X;
            var dy = Y - other.Y;
            return MathF.Sqrt((dx * dx) + (dy * dy));
        }
    }

    private sealed class PathfindingGrid
    {
        private static readonly (int X, int Y)[] NeighborOffsets =
        [
            (-1, -1), (0, -1), (1, -1),
            (-1, 0),           (1, 0),
            (-1, 1),  (0, 1),  (1, 1)
        ];

        private readonly IReadOnlyList<BuildingState> _buildings;
        private readonly IReadOnlyList<EnergyWallSegment> _blockingWalls;
        private readonly IReadOnlyList<MapRegionDefinition> _terrainRegions;
        private readonly IReadOnlyList<BridgeState> _bridges;
        private readonly HashSet<int> _ignoredStartBlockers;
        private readonly PlayableBounds _bounds;
        private readonly float _cellSize;
        private readonly int _maxCellX;
        private readonly int _maxCellY;

        public int MaxCellX => _maxCellX;
        public int MaxCellY => _maxCellY;

        public PathfindingGrid(
            IReadOnlyList<BuildingState> buildings,
            IReadOnlyList<EnergyWallSegment> blockingWalls,
            IReadOnlyList<MapRegionDefinition> terrainRegions,
            IReadOnlyList<BridgeState> bridges,
            SimVector2 start,
            PlayableBounds bounds,
            float cellSize = CellSize)
        {
            _buildings = buildings;
            _blockingWalls = blockingWalls;
            _terrainRegions = terrainRegions;
            _bridges = bridges;
            _bounds = bounds;
            _cellSize = cellSize;
            _maxCellX = (int)MathF.Floor((bounds.MaxX - bounds.MinX) / _cellSize);
            _maxCellY = (int)MathF.Floor((bounds.MaxY - bounds.MinY) / _cellSize);
            _ignoredStartBlockers = buildings
                .Where(building => !building.IsDestroyed)
                .Where(building => building.Position.DistanceTo(start) <= building.FootprintWorldRadius + UnitClearance)
                .Select(building => building.EntityId)
                .ToHashSet();
        }

        public PathCell ToCell(SimVector2 point)
        {
            var x = (int)MathF.Floor((Math.Clamp(point.X, _bounds.MinX, _bounds.MaxX) - _bounds.MinX) / _cellSize);
            var y = (int)MathF.Floor((Math.Clamp(point.Y, _bounds.MinY, _bounds.MaxY) - _bounds.MinY) / _cellSize);
            return new PathCell(Math.Clamp(x, 0, _maxCellX), Math.Clamp(y, 0, _maxCellY));
        }

        public SimVector2 ToCenter(PathCell cell)
        {
            return new SimVector2(
                _bounds.MinX + (cell.X * _cellSize) + (_cellSize / 2.0f),
                _bounds.MinY + (cell.Y * _cellSize) + (_cellSize / 2.0f));
        }

        public IEnumerable<PathCell> GetDestinationCandidates(PathCell preferred, PathCell start)
        {
            if (!IsBlocked(preferred, start))
            {
                yield return preferred;
            }

            for (var radius = 1; radius <= DestinationFallbackRadiusCells; radius++)
            {
                var candidates = new List<PathCell>();
                for (var x = preferred.X - radius; x <= preferred.X + radius; x++)
                {
                    for (var y = preferred.Y - radius; y <= preferred.Y + radius; y++)
                    {
                        if (Math.Abs(x - preferred.X) != radius && Math.Abs(y - preferred.Y) != radius)
                        {
                            continue;
                        }

                        var cell = new PathCell(x, y);
                        if (!IsInsideBounds(cell) || IsBlocked(cell, start))
                        {
                            continue;
                        }

                        candidates.Add(cell);
                    }
                }

                foreach (var candidate in candidates.OrderBy(candidate => candidate.DistanceTo(preferred)))
                {
                    yield return candidate;
                }
            }
        }

        public IEnumerable<PathCell> GetNeighbors(PathCell current, PathCell start)
        {
            foreach (var offset in NeighborOffsets)
            {
                var neighbor = new PathCell(current.X + offset.X, current.Y + offset.Y);
                if (!IsInsideBounds(neighbor) ||
                    IsBlocked(neighbor, start) ||
                    IsMovementBlocked(current, neighbor))
                {
                    continue;
                }

                if (offset.X != 0 && offset.Y != 0)
                {
                    var horizontal = new PathCell(current.X + offset.X, current.Y);
                    var vertical = new PathCell(current.X, current.Y + offset.Y);
                    if (IsBlocked(horizontal, start) ||
                        IsBlocked(vertical, start) ||
                        IsMovementBlocked(current, horizontal) ||
                        IsMovementBlocked(current, vertical))
                    {
                        continue;
                    }
                }

                yield return neighbor;
            }
        }

        public bool IsBlocked(PathCell cell, PathCell start)
        {
            if (cell == start)
            {
                return false;
            }

            var center = ToCenter(cell);
            return IsBlockedByTerrain(center) ||
                _blockingWalls.Any(wall =>
                    SimulationGeometry.DistancePointToSegment(center, wall.ExtendedStart, wall.ExtendedEnd) <= EnergyWallSegment.BlockingClearance) ||
                _buildings.Any(building =>
                !building.IsDestroyed &&
                !_ignoredStartBlockers.Contains(building.EntityId) &&
                building.Position.DistanceTo(center) <= building.FootprintWorldRadius + UnitClearance);
        }

        private bool IsBlockedByTerrain(SimVector2 center)
        {
            var blockedRegion = _terrainRegions.FirstOrDefault(region => region.BlocksMovement && region.Contains(center, UnitClearance));
            if (blockedRegion is null)
            {
                return false;
            }

            return !_bridges.Any(bridge => bridge.IsIntact && bridge.Contains(center, UnitClearance));
        }

        private bool IsInsideBounds(PathCell cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X <= _maxCellX && cell.Y <= _maxCellY;
        }

        private bool IsMovementBlocked(PathCell from, PathCell to)
        {
            var start = ToCenter(from);
            var end = ToCenter(to);
            return _blockingWalls.Any(wall =>
                SimulationGeometry.DistanceSegmentToSegment(start, end, wall.ExtendedStart, wall.ExtendedEnd) <= EnergyWallSegment.BlockingClearance);
        }
    }
}

internal readonly record struct PathfindingDebugCell(SimVector2 Center, float Size);
