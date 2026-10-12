using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using static SmokeTestSupport;

internal static class NavigationRegressionSmoke
{
    private const string Player = ContentIds.Factions.PlayerExpedition;
    private const string Enemy = ContentIds.Factions.PrivateMilitary;

    public static void Run(SmokeTestContext context)
    {
        ValidateBlockedStartDoesNotCrossHostileWall(context);
        ValidateWallAndBuildingCornerSweeps(context);
        ValidateThinTerrainAndBridgeRasterization(context);
        ValidateStrictReachabilityAndPlacementGuard(context);
        ValidateBridgeRepairChoosesReachableBank(context);
        ValidateUnreachableBridgeRepairStops(context);
        ValidateReachabilityAndCandidateAllocations(context);
    }

    private static void ValidateBlockedStartDoesNotCrossHostileWall(SmokeTestContext context)
    {
        var simulation = WallSimulation(context, new(112, -80), new(112, 80), new(280, 0));
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(0, 0));
        var grunt = simulation.AddUnit(ContentIds.Units.Grunt, Player, new(70, 0));
        var goal = new SimVector2(200, 0);
        simulation.CommandUnitMove(grunt.EntityId, goal);
        Assert(!grunt.IsPathBlocked, "a blocked Hub spawn still has a route out");
        AssertWallSegmentsClear(simulation, grunt.Position, grunt.PathWaypoints);
        simulation.Advance(8);
        Assert(grunt.Position.DistanceTo(goal) < 0.01f, "the Hub spawn walks around the hostile wall and reaches its order");

        // The start exemption is local to blockers already intersecting the unit.
        var grid = simulation.GetNavigationGrid();
        Assert(!grid.CanEscapeTo(new(70, 0), new(148, -4), RtsSimulation.UnitRadius(grunt), Player),
            "the nearest free cell beyond a hostile wall is not a legal escape segment");
    }

    private static void ValidateWallAndBuildingCornerSweeps(SmokeTestContext context)
    {
        var simulation = WallSimulation(context, new(-40, 0), new(40, 0), new(0, -160));
        var unit = simulation.AddUnit(ContentIds.Units.Rifleman, Player, new(113, 192));
        simulation.CommandUnitMove(unit.EntityId, new(-207, -200));
        Assert(!unit.IsPathBlocked, "a wall end-cap has a route around it");
        AssertWallSegmentsClear(simulation, unit.Position, unit.PathWaypoints);
        Assert(!simulation.GetNavigationGrid().HasLineOfSight(new(-140, 28), new(-207, -200), RtsSimulation.UnitRadius(unit), Player),
            "a short wall-band intersection between old four-pixel samples is blocked");

        foreach (var (buildingId, position, start, goal) in new[]
        {
            (ContentIds.Buildings.PowerPlant, new SimVector2(1, 1), new SimVector2(-79, 97), new SimVector2(69, -4)),
            (ContentIds.Buildings.ArtilleryBattery, new SimVector2(0, 0), new SimVector2(-90, 90), new SimVector2(90, -90)),
            (ContentIds.Buildings.ExtractorRefinery, new SimVector2(0.7f, 1.3f), new SimVector2(268, 272), new SimVector2(43, -76))
        })
        {
            var scene = new RtsSimulation(context.Catalog, 4000, []);
            var building = scene.AddStartingBuilding(buildingId, position);
            var walker = scene.AddUnit(ContentIds.Units.Rifleman, Player, start);
            scene.CommandUnitMove(walker.EntityId, goal);
            Assert(!walker.IsPathBlocked, $"a route exists around {buildingId}");
            var previous = start;
            foreach (var point in walker.PathWaypoints)
            {
                Assert(building.Footprint.DistanceToSegment(previous, point) >= RtsSimulation.UnitRadius(walker) - 0.001f,
                    $"every {buildingId} approach segment clears the full unit radius");
                Assert(scene.GetNavigationGrid().HasLineOfSight(previous, point, RtsSimulation.UnitRadius(walker), Player),
                    "straightening preserves every crossed cell and diagonal corner check");
                previous = point;
            }
        }
    }

    private static void ValidateThinTerrainAndBridgeRasterization(SmokeTestContext context)
    {
        var bounds = new PlayableBounds(-200, 200, -200, 200);
        var region = Region("thin_barrier", new(0, 0), new(6, 400));
        var grid = new NavigationGrid(bounds);
        grid.Refresh([], [], [region], []);
        var radius = RtsSimulation.ToWorldRadius(context.Catalog.GetUnit(ContentIds.Units.Rifleman).CollisionRadius);
        var path = PathfindingSystem.FindPath(grid, new(-100, 0), new(100, 0), radius, Player);
        Assert(path.Success && path.Destination.X < 0, "a sub-cell terrain barrier blocks crossing and preserves movement fallback");
        Assert(!PathfindingSystem.CanReach(grid, new(-100, 0), new(100, 0), radius, Player),
            "terrain fallback is not reported as destination reachability");

        var smallCircle = region with { Shape = "circle", Radius = 3, Size = new(0, 0) };
        grid.Refresh([], [], [smallCircle], []);
        Assert(!grid.IsPassableAt(new(0, 0), radius, Player), "a sub-cell circle between grid centers is rasterized");

        var tinyBridge = new BridgeState(new("bridge_probe", "bridge", "circle", new(0, 0), new(0, 0), 1, 100, true, true, []));
        grid.Refresh([], [], [smallCircle], [tinyBridge]);
        Assert(!grid.IsPassableAt(new(0, 0), radius, Player), "a tiny bridge cannot erase an entire blocking terrain cell");
    }

    private static void ValidateStrictReachabilityAndPlacementGuard(SmokeTestContext context)
    {
        var markers = EnemyAiMarkers.FirstLanding with { HubPosition = new(0, 0), RallyPosition = new(-350, 0) };
        var map = context.WellsAtTheRidgeMap with
        {
            PlayableBounds = new(-400, 400, -400, 400),
            TerrainRegions =
            [
                Region("north", new(0, -110), new(500, 60)),
                Region("south", new(0, 110), new(500, 60)),
                Region("east", new(110, 0), new(60, 280))
            ],
            MapObjects = []
        };
        var simulation = new RtsSimulation(context.Catalog, 4000, [], enemyStartingMaterials: 4000, enemyAiMarkers: markers, map: map);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, markers.HubPosition, Enemy);
        simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new(-200, 210), Enemy);
        Assert(simulation.IsReachableForFaction(Enemy, markers.HubPosition, markers.RallyPosition), "the authored corridor initially reaches the rally");
        Assert(simulation.WouldWallOffEnemyHub(ContentIds.Buildings.Barracks, new(-200, 0)), "a close rally fallback does not defeat the Hub guard");
        Assert(!simulation.TryPlaceEnemyBuilding(ContentIds.Buildings.Barracks, new(-200, 0)), "the legal but sealing candidate is refused");
        Assert(simulation.TryPlaceBuildingForFaction(Enemy, ContentIds.Buildings.Barracks, new(-200, 0)).Success,
            "the fixture's placement is otherwise legal");
        Assert(!simulation.IsReachableForFaction(Enemy, markers.HubPosition, markers.RallyPosition), "an obstacle change invalidates cached true reachability");
        var grid = simulation.GetNavigationGrid();
        var path = PathfindingSystem.FindPath(grid, markers.HubPosition, markers.RallyPosition, 14.4f, Enemy);
        Assert(path.Success && path.Destination.DistanceTo(markers.RallyPosition) > 100,
            "movement retains a useful fallback even when the actual rally is unreachable");
        simulation.Buildings.Last().ApplyDamage(10000, "explosive");
        Assert(simulation.IsReachableForFaction(Enemy, markers.HubPosition, markers.RallyPosition), "destroying the barrier invalidates cached false reachability");
    }

    private static void ValidateBridgeRepairChoosesReachableBank(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, [], map: context.WellsAtTheRidgeMap);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(-1100, 0));
        foreach (var y in new[] { -84.0f, 0.0f, 84.0f })
            Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new(-535, y)).Success, "the bank blockers use legal six-pixel packing");
        var bridge = simulation.Bridges.First();
        simulation.DebugDamageBridge(bridge.Id, bridge.MaxHealth);
        var grunt = simulation.AddUnit(ContentIds.Units.Grunt, Player, new(-800, 0));
        Assert(simulation.CommandUnitRepairBridge(grunt.EntityId, bridge.Id).Success, "the Grunt accepts the collapsed bridge repair");
        var before = simulation.DebugPathRequestCount;
        simulation.Advance(30);
        Assert(bridge.CurrentHealth > 0, "bridge repair reaches the other bank when the nearest bank is sealed");
        Assert(simulation.DebugPathRequestCount - before == 1, "bridge repair keeps one path to its reachable work point");
        Assert(bridge.DistanceTo(grunt.Position) <= 36, "the work point stays inside the documented repair range");
    }

    private static void ValidateUnreachableBridgeRepairStops(SmokeTestContext context)
    {
        var definition = new MapObjectDefinition("bridge_sealed", "bridge", "rect", new(0, 0), new(100, 20), 0, 100, true, true, []);
        var map = context.WellsAtTheRidgeMap with { TerrainRegions = [], MapObjects = [definition], PlayableBounds = PlayableBounds.Default };
        var simulation = new RtsSimulation(context.Catalog, 4000, [], map: map);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(-600, -400));
        for (var along = -150; along <= 150; along += 50)
            foreach (var point in new[] { new SimVector2(along, -150), new SimVector2(along, 150), new SimVector2(-150, along), new SimVector2(150, along) })
                simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, point);
        simulation.DebugDamageBridge(definition.Id, definition.MaxHealth);
        var grunt = simulation.AddUnit(ContentIds.Units.Grunt, Player, new(-400, 0));
        simulation.CommandUnitRepairBridge(grunt.EntityId, definition.Id);
        simulation.Advance(5);
        Assert(grunt.RepairTargetBridgeId is null && grunt.MoveTarget is null, "a bridge with no reachable work point drops its repair order");
        Assert(simulation.DebugPathRequestCount == 1, "an unreachable bridge does not retry every tick");
    }

    private static void ValidateReachabilityAndCandidateAllocations(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var landing = runtime.Markers["player_landing_zone"];
        simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, landing);
        var markers = EnemyAiMarkers.FromMission(context.WellsAtTheRidgeMission);
        var grid = simulation.GetNavigationGrid();
        Assert(simulation.IsReachableForFaction(Enemy, markers.HubPosition, landing), "a blocked player Hub is reachable at its outline");
        var searches = grid.DebugPathSearchCount;
        for (var index = 0; index < 100; index++)
            Assert(simulation.IsReachableForFaction(Enemy, markers.HubPosition, landing), "cached reachability stays truthful");
        Assert(grid.DebugPathSearchCount == searches, "reachability questions perform no A* searches");

        var candidate = markers.HubPosition + new SimVector2(-240, -200);
        simulation.WouldWallOffEnemyHub(ContentIds.Buildings.PowerPlant, candidate);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 20; index++) simulation.WouldWallOffEnemyHub(ContentIds.Buildings.PowerPlant, candidate);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert(allocated < 256000, $"twenty warmed AI candidates avoid whole-map copies and search-buffer allocations ({allocated} bytes)");
        Assert(grid.DebugPathSearchCount == searches, "the AI candidate guard does not construct A* paths");
    }

    private static RtsSimulation WallSimulation(SmokeTestContext context, SimVector2 first, SimVector2 second, SimVector2 plant)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, [], enemyStartingMaterials: 4000);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(500, 200), Enemy);
        Assert(simulation.TryPlaceBuildingForFaction(Enemy, ContentIds.Buildings.PowerPlant, plant).Success, "the hostile wall's power source places");
        Assert(simulation.TryPlaceBuildingForFaction(Enemy, ContentIds.Buildings.DefenseTower, first).Success, "the first wall anchor places");
        Assert(simulation.TryPlaceBuildingForFaction(Enemy, ContentIds.Buildings.DefenseTower, second).Success, "the second wall anchor places");
        Assert(simulation.EnergyWalls.Count == 1, "the repro uses a real powered energy wall");
        return simulation;
    }

    private static void AssertWallSegmentsClear(RtsSimulation simulation, SimVector2 start, IReadOnlyList<SimVector2> points)
    {
        var previous = start;
        foreach (var point in points)
        {
            foreach (var wall in simulation.EnergyWalls)
                Assert(SimulationGeometry.DistanceSegmentToSegment(previous, point, wall.ExtendedStart, wall.ExtendedEnd) > EnergyWallSegment.BlockingClearance,
                    "every movement segment clears the hostile wall's entire hard band");
            previous = point;
        }
    }

    private static MapRegionDefinition Region(string id, SimVector2 center, SimVector2 size)
    {
        return new(id, "blocked_terrain", "rect", center, size, 0, true, true, false, []);
    }
}
