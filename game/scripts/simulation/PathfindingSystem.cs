namespace Stratezone.Simulation;

internal static class PathfindingSystem
{
    private const float MinX = -760.0f;
    private const float MaxX = 940.0f;
    private const float MinY = -420.0f;
    private const float MaxY = 420.0f;
    private const float CellSize = 32.0f;
    private const float UnitClearance = 18.0f;
    private const int DestinationFallbackRadiusCells = 6;

    public static PathResult FindPath(
        SimVector2 start,
        SimVector2 destination,
        IReadOnlyList<BuildingState> buildings,
        IReadOnlyList<EnergyWallSegment> blockingWalls)
    {
        var grid = new PathfindingGrid(buildings, blockingWalls, start);
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
        private readonly HashSet<int> _ignoredStartBlockers;
        private readonly int _maxCellX = (int)MathF.Floor((MaxX - MinX) / CellSize);
        private readonly int _maxCellY = (int)MathF.Floor((MaxY - MinY) / CellSize);

        public PathfindingGrid(IReadOnlyList<BuildingState> buildings, IReadOnlyList<EnergyWallSegment> blockingWalls, SimVector2 start)
        {
            _buildings = buildings;
            _blockingWalls = blockingWalls;
            _ignoredStartBlockers = buildings
                .Where(building => !building.IsDestroyed)
                .Where(building => building.Position.DistanceTo(start) <= building.FootprintWorldRadius + UnitClearance)
                .Select(building => building.EntityId)
                .ToHashSet();
        }

        public PathCell ToCell(SimVector2 point)
        {
            var x = (int)MathF.Floor((Math.Clamp(point.X, MinX, MaxX) - MinX) / CellSize);
            var y = (int)MathF.Floor((Math.Clamp(point.Y, MinY, MaxY) - MinY) / CellSize);
            return new PathCell(Math.Clamp(x, 0, _maxCellX), Math.Clamp(y, 0, _maxCellY));
        }

        public SimVector2 ToCenter(PathCell cell)
        {
            return new SimVector2(
                MinX + (cell.X * CellSize) + (CellSize / 2.0f),
                MinY + (cell.Y * CellSize) + (CellSize / 2.0f));
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
            return _buildings.Any(building =>
                !building.IsDestroyed &&
                !_ignoredStartBlockers.Contains(building.EntityId) &&
                building.Position.DistanceTo(center) <= building.FootprintWorldRadius + UnitClearance);
        }

        private bool IsInsideBounds(PathCell cell)
        {
            return cell.X >= 0 && cell.Y >= 0 && cell.X <= _maxCellX && cell.Y <= _maxCellY;
        }

        private bool IsMovementBlocked(PathCell from, PathCell to)
        {
            var start = ToCenter(from);
            var end = ToCenter(to);
            return _blockingWalls.Any(wall => LinesIntersect(start, end, wall.Start, wall.End));
        }
    }

    private static bool LinesIntersect(SimVector2 a, SimVector2 b, SimVector2 c, SimVector2 d)
    {
        var denominator = ((d.Y - c.Y) * (b.X - a.X)) - ((d.X - c.X) * (b.Y - a.Y));
        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var ua = (((d.X - c.X) * (a.Y - c.Y)) - ((d.Y - c.Y) * (a.X - c.X))) / denominator;
        var ub = (((b.X - a.X) * (a.Y - c.Y)) - ((b.Y - a.Y) * (a.X - c.X))) / denominator;
        return ua is >= 0.0f and <= 1.0f && ub is >= 0.0f and <= 1.0f;
    }
}
