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
    private const float LineOfSightStep = NavigationGrid.CellSize * 0.5f;

    // Farthest a unit sent toward a wall may stop from it: its radius plus one cell diagonal.
    internal static float MaxApproachGap(float radius)
    {
        return radius + (NavigationGrid.CellSize * DiagonalCost);
    }

    public static PathResult FindPath(NavigationGrid grid, SimVector2 start, SimVector2 destination, float radius, string factionId)
    {
        var startCell = grid.CellIndexAt(start);
        var origin = start;
        var escaping = !grid.IsPassable(startCell, radius, factionId);
        if (escaping)
        {
            startCell = grid.FindNearestCell(start, StartEscapeRings, cell => grid.IsPassable(cell, radius, factionId));
            if (startCell < 0)
            {
                return new PathResult(false, "No reachable path.", [], destination);
            }

            origin = grid.CellCenter(startCell);
        }

        var component = grid.ComponentOf(startCell, radius, factionId);
        var destinationCell = grid.CellIndexAt(destination);
        var destinationPoint = grid.ClampToBounds(destination);
        if (grid.ComponentOf(destinationCell, radius, factionId) != component)
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

        var cells = SearchCells(grid, startCell, destinationCell, radius, factionId);
        var route = Corners(grid, cells, destinationPoint);
        var waypoints = StraightenWithLineOfSight(grid, origin, route, radius, factionId);
        if (escaping)
        {
            waypoints.Insert(0, origin);
        }

        return new PathResult(true, "Path found.", waypoints, destinationPoint);
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

        corners.Add(destinationPoint);
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
        var delta = to - from;
        var steps = Math.Max(1, (int)MathF.Ceiling(delta.Length() / LineOfSightStep));
        for (var step = 0; step <= steps; step++)
        {
            if (!grid.IsPassableAt(from + (delta * (step / (float)steps)), radius, factionId))
            {
                return false;
            }
        }

        return true;
    }
}

internal readonly record struct PathfindingDebugCell(SimVector2 Center, float Size);
