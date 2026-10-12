using System.Diagnostics;
using Stratezone.Simulation;
using static SmokeTestSupport;

// Track A2: a cached clearance field where a gap is passable when the unit's radius fits.
internal static class NavigationSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateGapsAdmitUnitsBySize(context);
        ValidateSealedRingKeepsEveryoneOut(context);
        ValidateOpenGroundPathsAreStraight(context);
        ValidateGridRebuildsOnlyWhenObstaclesChange(context);
        ValidateTerrainClearanceMatchesWaterEdge(context);
        ValidateCrowdRepathCost(context);
        NavigationRegressionSmoke.Run(context);
    }

    private static void ValidateGapsAdmitUnitsBySize(SmokeTestContext context)
    {
        // A cell-aligned gap needs about twice the radius plus one 8 px cell.
        AssertGap(context, gap: 40.0f, ContentIds.Units.Rifleman, passes: true);
        AssertGap(context, gap: 40.0f, ContentIds.Units.Rover, passes: false);
        AssertGap(context, gap: 64.0f, ContentIds.Units.Rover, passes: true);
        AssertGap(context, gap: 64.0f, ContentIds.Units.MediumTank, passes: false);
        AssertGap(context, gap: 84.0f, ContentIds.Units.MediumTank, passes: true);
    }

    private static void AssertGap(SmokeTestContext context, float gap, string unitId, bool passes)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        // Overlapping starting Power Plants form a long east-west wall with one gap at x = 0.
        var plantHalfWidth = 42.0f;
        for (var x = (gap * 0.5f) + plantHalfWidth; x <= 640.0f; x += 60.0f)
        {
            simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(x, 0));
            simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-x, 0));
        }

        var unit = simulation.AddUnit(unitId, ContentIds.Factions.PlayerExpedition, new SimVector2(0, -200));
        simulation.CommandUnitMove(unit.EntityId, new SimVector2(0, 200));
        var detoured = unit.PathWaypoints.Any(point => MathF.Abs(point.X) > 600.0f);
        Assert(!unit.IsPathBlocked, $"{unitId} finds some route past a wall with a {gap} px gap");
        Assert(detoured != passes, passes
            ? $"{unitId} fits through a {gap} px gap between buildings"
            : $"{unitId} is too wide for a {gap} px gap and walks around the wall");
    }

    private static void ValidateSealedRingKeepsEveryoneOut(SmokeTestContext context)
    {
        foreach (var unitId in new[] { ContentIds.Units.Rifleman, ContentIds.Units.Rover, ContentIds.Units.MediumTank })
        {
            var simulation = new RtsSimulation(context.Catalog, 4000, []);
            for (var along = -150.0f; along <= 150.0f; along += 50.0f)
            {
                foreach (var position in new[] { new SimVector2(along, -150), new SimVector2(along, 150), new SimVector2(-150, along), new SimVector2(150, along) })
                {
                    simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, position);
                }
            }

            var unit = simulation.AddUnit(unitId, ContentIds.Factions.PlayerExpedition, new SimVector2(-420, 0));
            simulation.CommandUnitMove(unit.EntityId, new SimVector2(0, 0));
            for (var step = 0; step < 200 && unit.MoveTarget is not null; step++)
            {
                simulation.Advance(RtsSimulation.TickSeconds);
                Assert(MathF.Abs(unit.Position.X) > 100.0f || MathF.Abs(unit.Position.Y) > 100.0f, $"{unitId} never gets inside a sealed ring of buildings");
            }
        }
    }

    private static void ValidateOpenGroundPathsAreStraight(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        var unit = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-500, -300));
        simulation.CommandUnitMove(unit.EntityId, new SimVector2(503, 287));
        Assert(unit.PathWaypoints.Count <= 2, $"an open-ground move is one or two straight segments, not a staircase ({unit.PathWaypoints.Count} waypoints)");
        Assert(unit.MoveTarget is { } target && target.DistanceTo(new SimVector2(503, 287)) < 0.01f, "an open-ground move ends exactly where it was ordered");
    }

    private static void ValidateGridRebuildsOnlyWhenObstaclesChange(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-600, 0));
        var units = Enumerable.Range(0, 10)
            .Select(index => simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-300, -200 + (index * 40))))
            .ToArray();

        foreach (var unit in units)
        {
            simulation.CommandUnitMove(unit.EntityId, new SimVector2(400, 0));
        }

        var rebuildsAfterFirstOrders = simulation.DebugNavigationRebuildCount;
        foreach (var unit in units)
        {
            simulation.CommandUnitMove(unit.EntityId, new SimVector2(300, 100));
        }

        Assert(simulation.DebugNavigationRebuildCount == rebuildsAfterFirstOrders, "path requests reuse the cached navigation grid");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Success, "a new building places");
        simulation.CommandUnitMove(units[0].EntityId, new SimVector2(400, 0));
        Assert(simulation.DebugNavigationRebuildCount == rebuildsAfterFirstOrders + 1, "a new building refreshes the navigation grid once");
    }

    private static void ValidateTerrainClearanceMatchesWaterEdge(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var grid = runtime.Simulation.GetNavigationGrid();
        // The west channel's water spans x -480..-240; this probe stands 60 px west of it.
        var clearance = grid.ClearanceAt(grid.CellIndexAt(new SimVector2(-540, 300)));
        Assert(MathF.Abs(clearance - 60.0f) <= 6.0f, $"terrain clearance tracks the water edge within a cell ({clearance} px, expected about 60)");
        Assert(!grid.IsPassableAt(new SimVector2(-360, 300), 1.0f, ContentIds.Factions.PlayerExpedition), "open water is impassable");
        Assert(grid.IsPassableAt(new SimVector2(-360, 0), RtsSimulation.ToWorldRadius(context.Catalog.GetUnit(ContentIds.Units.MediumTank).CollisionRadius), ContentIds.Factions.PlayerExpedition), "an intact bridge deck carries a Medium Tank");
    }

    private static void ValidateCrowdRepathCost(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var units = Enumerable.Range(0, 100)
            .Select(index => simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-1300 + ((index % 10) * 30), -150 + ((index / 10) * 30))))
            .ToArray();
        simulation.GetNavigationGrid();

        var timer = Stopwatch.StartNew();
        foreach (var unit in units)
        {
            simulation.CommandUnitMove(unit.EntityId, new SimVector2(1400, -250));
        }

        timer.Stop();
        Assert(units.All(unit => !unit.IsPathBlocked), "a crowd ordered across Level 2 finds routes");
        Console.WriteLine($"Navigation: 100 cross-map Level 2 paths in {timer.Elapsed.TotalMilliseconds:0.0} ms.");
        // A coarse guard against pathological regressions (such as per-candidate fallback
        // searches), loose enough for Debug builds on slower machines. Real budgets need
        // Steam Deck profiling (A7).
        Assert(timer.Elapsed.TotalMilliseconds < 5000.0, "100 cross-map path requests avoid pathological search blowups");
    }
}
