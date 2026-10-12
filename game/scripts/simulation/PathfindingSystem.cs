namespace Stratezone.Simulation;

// Grid A* over the cached clearance field (Track A2). Connected-region labels answer
// reachability before searching (A4), so an unreachable target costs one lookup plus one
// search to the nearest reachable stand-in. Line-of-sight smoothing straightens the grid
// path (A5).
internal static class PathfindingSystem
{
    // How far around a blocked destination to look for the nearest reachable stand-in.
    private const int DestinationFallbackRings = 32;

    // How far a unit standing in blocked space (a Hub spawn, a building placed on it) looks for open ground.
    private const int StartEscapeRings = 32;

    private const float DiagonalCost = 1.41421356f;
    private const float HeuristicTieBreak = 1.001f;

    // Farthest a unit sent toward a wall may stop from it: its radius plus one cell diagonal.
    internal static float MaxApproachGap(float radius)
    {
        return radius + (NavigationGrid.CellSize * DiagonalCost);
    }

    public static PathResult FindPath(NavigationGrid grid, SimVector2 start, SimVector2 destination, float radius, string factionId)
    {
        if (!TryResolveStart(grid, start, radius, factionId, out var startCell, out var origin, out var escaping))
        {
            return new PathResult(false, "No reachable path.", [], destination);
        }

        var component = grid.ComponentOf(startCell, radius, factionId);
        var destinationCell = grid.CellIndexAt(destination);
        var destinationPoint = grid.ClampToBounds(destination);
        if (grid.ComponentOf(destinationCell, radius, factionId) != component || !grid.IsPassableAt(destinationPoint, radius, factionId))
        {
            destinationCell = grid.FindNearestCell(
                destination,
                DestinationFallbackRings,
                cell => grid.ComponentOf(cell, radius, factionId) == component);
            if (destinationCell < 0)
            {
                return new PathResult(false, "No reachable path.", [], destination);
            }

            destinationPoint = grid.CellCenter(destinationCell);
        }
        else if (!grid.HasLineOfSight(grid.CellCenter(destinationCell), destinationPoint, radius, factionId))
        {
            // A free point and its cell center can still lie on opposite sides of an
            // inflated curved corner. Connect the exact endpoint through a safe cell.
            destinationCell = grid.FindNearestCell(destinationPoint, DestinationFallbackRings, cell =>
                grid.ComponentOf(cell, radius, factionId) == component &&
                grid.HasLineOfSight(grid.CellCenter(cell), destinationPoint, radius, factionId));
            if (destinationCell < 0) return new PathResult(false, "No reachable path.", [], destination);
        }

        var cells = SearchCells(grid, startCell, destinationCell, radius, factionId);
        var route = Corners(grid, cells, destinationPoint);
        var waypoints = StraightenWithLineOfSight(grid, origin, route, radius, factionId);
        if (escaping)
        {
            waypoints.Insert(0, origin);
        }

        return new PathResult(true, "Path found.", waypoints, destinationPoint);
    }

    // Reachability never treats a substituted movement destination as success.
    // A blocked building target may be reached at its outline; a free point must
    // belong to the start component itself.
    public static bool CanReach(NavigationGrid grid, SimVector2 start, SimVector2 destination, float radius, string factionId, BuildingFootprint? target = null)
    {
        if (grid.TryGetReachability(start, destination, radius, factionId, target, out var cached)) return cached;
        var reachable = false;
        if (TryResolveStart(grid, start, radius, factionId, out var startCell, out _, out _))
        {
            if (grid.IsPassableAt(destination, radius, factionId))
            {
                reachable = grid.AreConnected(startCell, grid.CellIndexAt(destination), radius, factionId);
            }
            else if (target is not null)
            {
                var gap = MaxApproachGap(radius);
                var rings = (int)MathF.Ceiling((target.BoundingRadius + gap) / NavigationGrid.CellSize) + 1;
                reachable = grid.FindNearestCell(destination, rings, cell =>
                    grid.IsPassable(cell, radius, factionId) && target.IsWithin(grid.CellCenter(cell), gap) &&
                    grid.AreConnected(startCell, cell, radius, factionId)) >= 0;
            }
        }

        grid.CacheReachability(start, destination, radius, factionId, target, reachable);
        return reachable;
    }

    public static PathResult FindPathToRange(NavigationGrid grid, SimVector2 start, ConvexShape target, float range, float radius, string factionId)
    {
        var nearest = target.ClosestPoint(start);
        if (!TryResolveStart(grid, start, radius, factionId, out var startCell, out _, out _))
            return new PathResult(false, "No reachable path.", [], nearest);
        var component = grid.ComponentOf(startCell, radius, factionId);
        var rings = (int)MathF.Ceiling(((target.Radius * 2.0f) + range) / NavigationGrid.CellSize) + 1;
        var cell = grid.FindNearestCell(nearest, rings, candidate =>
            grid.ComponentOf(candidate, radius, factionId) == component && target.DistanceTo(grid.CellCenter(candidate)) <= range);
        return cell < 0
            ? new PathResult(false, "No reachable path.", [], nearest)
            : FindPath(grid, start, grid.CellCenter(cell), radius, factionId);
    }

    private static bool TryResolveStart(NavigationGrid grid, SimVector2 start, float radius, string factionId, out int cell, out SimVector2 origin, out bool escaping)
    {
        cell = grid.CellIndexAt(start);
        origin = start;
        escaping = !grid.IsPassableAt(start, radius, factionId) || !grid.HasLineOfSight(start, grid.CellCenter(cell), radius, factionId);
        if (!escaping) return true;
        cell = grid.FindNearestCell(start, StartEscapeRings, candidate =>
            grid.IsPassable(candidate, radius, factionId) && grid.CanEscapeTo(start, grid.CellCenter(candidate), radius, factionId));
        if (cell < 0) return false;
        origin = grid.CellCenter(cell);
        return true;
    }

    public static IReadOnlyList<PathfindingDebugCell> SampleBlockedCells(NavigationGrid grid, float radius, string factionId, float sampleSize)
    {
        var cells = new List<PathfindingDebugCell>();
        var stride = Math.Max(1, (int)MathF.Round(sampleSize / NavigationGrid.CellSize));
        for (var y = stride / 2; y < grid.Rows; y += stride)
        {
            for (var x = stride / 2; x < grid.Columns; x += stride)
            {
                var cell = (y * grid.Columns) + x;
                if (!grid.IsPassable(cell, radius, factionId))
                {
                    cells.Add(new PathfindingDebugCell(grid.CellCenter(cell), sampleSize));
                }
            }
        }

        return cells;
    }

    // Both cells share a connected region, so this search always succeeds.
    private static List<int> SearchCells(NavigationGrid grid, int start, int goal, float radius, string factionId)
    {
        grid.RecordPathSearch();
        var (cost, parent, stamp, generation) = grid.BeginSearch();
        var goalX = goal % grid.Columns;
        var goalY = goal / grid.Columns;
        float Heuristic(int cell)
        {
            var dx = Math.Abs((cell % grid.Columns) - goalX);
            var dy = Math.Abs((cell / grid.Columns) - goalY);
            return (dx + dy + ((DiagonalCost - 2.0f) * Math.Min(dx, dy))) * HeuristicTieBreak;
        }

        var frontier = new PriorityQueue<int, float>();
        cost[start] = 0.0f;
        parent[start] = -1;
        stamp[start] = generation;
        frontier.Enqueue(start, Heuristic(start));
        Span<int> neighbors = stackalloc int[8];
        while (frontier.TryDequeue(out var current, out var priority))
        {
            if (current == goal)
            {
                break;
            }

            if (priority > cost[current] + Heuristic(current) + 0.001f)
            {
                continue; // A cheaper route to this cell was queued later.
            }

            var count = grid.GetNeighbors(current, radius, factionId, neighbors);
            for (var index = 0; index < count; index++)
            {
                var neighbor = neighbors[index];
                var diagonal = (neighbor % grid.Columns) != (current % grid.Columns) && (neighbor / grid.Columns) != (current / grid.Columns);
                var newCost = cost[current] + (diagonal ? DiagonalCost : 1.0f);
                if (stamp[neighbor] == generation && newCost >= cost[neighbor])
                {
                    continue;
                }

                stamp[neighbor] = generation;
                cost[neighbor] = newCost;
                parent[neighbor] = current;
                frontier.Enqueue(neighbor, newCost + Heuristic(neighbor));
            }
        }

        var cells = new List<int>();
        for (var cell = goal; cell >= 0; cell = parent[cell])
        {
            cells.Add(cell);
        }

        cells.Reverse();
        return cells;
    }

    // Cell centers where the grid path turns, ending at the exact destination point.
    private static List<SimVector2> Corners(NavigationGrid grid, IReadOnlyList<int> cells, SimVector2 destinationPoint)
    {
        var corners = new List<SimVector2>();
        corners.Add(grid.CellCenter(cells[0]));
        for (var index = 1; index < cells.Count - 1; index++)
        {
            var previous = cells[index - 1];
            var current = cells[index];
            var next = cells[index + 1];
            if (current - previous != next - current)
            {
                corners.Add(grid.CellCenter(current));
            }
        }

        var goalCenter = grid.CellCenter(cells[^1]);
        if (corners[^1] != goalCenter) corners.Add(goalCenter);
        if (corners[^1] != destinationPoint) corners.Add(destinationPoint);
        return corners;
    }

    // Drop every corner the unit can see past, so open-ground moves become straight lines.
    private static List<SimVector2> StraightenWithLineOfSight(NavigationGrid grid, SimVector2 origin, IReadOnlyList<SimVector2> corners, float radius, string factionId)
    {
        var waypoints = new List<SimVector2>();
        var anchor = origin;
        for (var index = 0; index < corners.Count - 1; index++)
        {
            if (!HasLineOfSight(grid, anchor, corners[index + 1], radius, factionId))
            {
                waypoints.Add(corners[index]);
                anchor = corners[index];
            }
        }

        waypoints.Add(corners[^1]);
        return waypoints;
    }

    private static bool HasLineOfSight(NavigationGrid grid, SimVector2 from, SimVector2 to, float radius, string factionId)
    {
        return grid.HasLineOfSight(from, to, radius, factionId);
    }
}

internal readonly record struct PathfindingDebugCell(SimVector2 Center, float Size);
