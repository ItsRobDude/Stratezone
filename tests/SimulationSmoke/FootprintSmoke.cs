using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using static SmokeTestSupport;

// Track A1: buildings block, place, power, and take fire by their real outline.
internal static class FootprintSmoke
{
    private const float Tolerance = 0.01f;

    public static void Run(SmokeTestContext context)
    {
        ValidateConvexShapeGeometry();
        ValidateLShapedRangeUsesNearestWall();
        ValidateContentFootprints(context);
        ValidatePlacementPacksToBuffer(context);
        ValidatePowerReachesOutline(context);
        ValidateAttackRangeMeasuresToWall(context);
        ValidateFootprintsBlockMovement(context);
        ValidateOutlineCornersAreContained(context);
        ValidateRepairReachesWalls(context);
        ValidateRepairGivesUpWhenWalledOff(context);
        ValidateRetreatStandsDownAtPathEnd(context);
        ValidateEnemyDoesNotWallOffItsHub(context);
    }

    private static void ValidateConvexShapeGeometry()
    {
        var square = ConvexShape.Rect(new SimVector2(0, 0), new SimVector2(100, 100));
        Assert(square.Contains(new SimVector2(0, 0)), "convex polygon contains its center");
        Assert(!square.Contains(new SimVector2(51, 0)), "convex polygon excludes points past its wall");
        Assert(MathF.Abs(square.DistanceTo(new SimVector2(80, 0)) - 30.0f) < Tolerance, "polygon distance is measured to the nearest wall");
        Assert(MathF.Abs(square.DistanceTo(new SimVector2(80, 80)) - MathF.Sqrt(1800.0f)) < Tolerance, "polygon distance is measured to the nearest corner");
        Assert(square.ClosestPoint(new SimVector2(80, 10)).DistanceTo(new SimVector2(50, 10)) < Tolerance, "closest point lands on the facing wall");

        var reversed = ConvexShape.Polygon([new SimVector2(-50, 50), new SimVector2(50, 50), new SimVector2(50, -50), new SimVector2(-50, -50)]);
        Assert(reversed.Contains(new SimVector2(10, -10)), "point containment works for either polygon winding");

        var circle = ConvexShape.Circle(new SimVector2(200, 0), 20);
        Assert(MathF.Abs(square.DistanceTo(circle) - 130.0f) < Tolerance, "polygon-to-circle distance subtracts the radius");
        Assert(square.DistanceTo(ConvexShape.Rect(new SimVector2(60, 0), new SimVector2(40, 40))) == 0.0f, "overlapping polygons have zero distance");
        Assert(square.DistanceTo(ConvexShape.Rect(new SimVector2(0, 0), new SimVector2(10, 10))) == 0.0f, "nested polygons have zero distance");
        Assert(MathF.Abs(square.DistanceTo(ConvexShape.Rect(new SimVector2(110, 0), new SimVector2(20, 20))) - 50.0f) < Tolerance, "disjoint polygons measure the gap between walls");
        Assert(square.DistanceToSegment(new SimVector2(-200, 0), new SimVector2(200, 0)) == 0.0f, "a segment crossing a polygon touches it");
        Assert(MathF.Abs(square.DistanceToSegment(new SimVector2(-200, 60), new SimVector2(200, 60)) - 10.0f) < Tolerance, "a segment passing a polygon keeps its gap");
    }

    private static void ValidateLShapedRangeUsesNearestWall()
    {
        // Tall bar from x -2..0 plus a foot from x 0..2 along the south edge.
        var lShape = new FootprintDefinition(
        [
            FootprintPartDefinition.Rect(new SimVector2(-1, 0), new SimVector2(2, 4)),
            FootprintPartDefinition.Rect(new SimVector2(1, 1.5f), new SimVector2(2, 1))
        ]);
        var footprint = BuildingFootprint.Place(lShape, new SimVector2(0, 0));
        var notch = new SimVector2(36, -36);

        Assert(!footprint.Contains(notch), "the inside corner of an L is open ground");
        Assert(MathF.Abs(footprint.DistanceTo(notch) - 36.0f) < Tolerance, "range to an L-shaped building is measured to its nearest wall");
        Assert(notch.DistanceTo(footprint.Origin) < footprint.BoundingRadius, "a bounding circle would wrongly swallow the L's notch");
    }

    private static void ValidateContentFootprints(SmokeTestContext context)
    {
        foreach (var building in context.Catalog.Buildings.Values)
        {
            Assert(building.Footprint.Parts.Count > 0, $"{building.Id} has a footprint outline");
            Assert(building.PlacementBuffer is > 0.0f and <= 0.5f, $"{building.Id} keeps a small model-separation buffer");
            var placed = BuildingFootprint.Place(building.Footprint, new SimVector2(0, 0));
            Assert(placed.Contains(new SimVector2(0, 0)), $"{building.Id} footprint covers its origin");
        }

        var defenseTower = context.Catalog.GetBuilding(ContentIds.Buildings.DefenseTower);
        foreach (var upgrade in context.Catalog.Buildings.Values.Where(building => building.UpgradeFromBuildingId == defenseTower.Id))
        {
            Assert(
                MathF.Abs(upgrade.Footprint.BoundingRadius - defenseTower.Footprint.BoundingRadius) < Tolerance,
                $"{upgrade.Id} upgrades in place on the Defense Tower footprint");
        }
    }

    private static void ValidatePlacementPacksToBuffer(SmokeTestContext context)
    {
        var simulation = CreateSimulation(context);
        var plant = context.Catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
        var plantWidth = RtsSimulation.ToWorldRadius(2.0f * plant.Footprint.Parts[0].Points.Max(point => point.X));
        var buffer = RtsSimulation.ToWorldRadius(plant.PlacementBuffer);

        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Success, "first Power Plant places");
        var touching = simulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, new SimVector2(plantWidth + (buffer * 0.5f), 0));
        Assert(!touching.IsLegal && touching.MessageKey == "sim.placement.blocked_by_building", "buildings closer than the buffer are blocked");
        var packed = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(plantWidth + buffer + 1.0f, 0));
        Assert(packed.Success, $"buildings may pack wall to wall at the buffer: {packed.Message}");
    }

    private static void ValidatePowerReachesOutline(SmokeTestContext context)
    {
        var simulation = CreateSimulation(context);
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Success, "power source places");
        var powerReach = RtsSimulation.ToWorldRadius(context.Catalog.GetBuilding(ContentIds.Buildings.PowerPlant).PowerRadius);
        var barracksHalfDepth = RtsSimulation.ToWorldRadius(context.Catalog.GetBuilding(ContentIds.Buildings.Barracks).Footprint.SouthExtent);

        var reached = simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(0, powerReach + barracksHalfDepth - 10.0f));
        Assert(reached.IsLegal, $"a power field powers a building whose wall it reaches, even with the center outside: {reached.Reason}");
        var missed = simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(0, powerReach + barracksHalfDepth + 10.0f));
        Assert(missed.MessageKey == "sim.placement.requires_powered_support", "a power field that stops short of the wall does not power the building");
    }

    private static void ValidateAttackRangeMeasuresToWall(SmokeTestContext context)
    {
        var simulation = CreateSimulation(context);
        var target = simulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0), ContentIds.Factions.PrivateMilitary);
        var rifleRange = RtsSimulation.ToWorldRadius(context.Catalog.GetUnit(ContentIds.Units.Rifleman).AttackRange);
        var northWall = -RtsSimulation.ToWorldRadius(context.Catalog.GetBuilding(ContentIds.Buildings.Barracks).Footprint.SouthExtent);
        var shooterStart = new SimVector2(0, northWall - rifleRange + 4.0f);
        var shooter = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, shooterStart);

        simulation.CommandUnitAttackBuilding(shooter.EntityId, target.EntityId);
        TickFor(simulation, 1.0f);
        Assert(target.Health < target.Definition.Health, "a unit in range of a building's nearest wall can hit it");
        Assert(shooter.Position.DistanceTo(shooterStart) < 1.0f, "a unit in range of the nearest wall fires without closing on the center");
    }

    private static void ValidateFootprintsBlockMovement(SmokeTestContext context)
    {
        var simulation = CreateSimulation(context);
        var plant = context.Catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
        var plantWidth = RtsSimulation.ToWorldRadius(2.0f * plant.Footprint.Parts[0].Points.Max(point => point.X));
        var gap = RtsSimulation.ToWorldRadius(plant.PlacementBuffer) + 1.0f;
        var west = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Building!;
        var east = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(plantWidth + gap, 0)).Building!;
        var seam = (west.Position.X + east.Position.X) * 0.5f;
        var walker = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(seam, -220));

        simulation.CommandUnitMove(walker.EntityId, new SimVector2(seam, 220));
        var previous = walker.Position;
        foreach (var waypoint in walker.PathWaypoints)
        {
            Assert(
                west.Footprint.DistanceToSegment(previous, waypoint) > 0.0f && east.Footprint.DistanceToSegment(previous, waypoint) > 0.0f,
                "a path never cuts through a building footprint or the buffer seam between two buildings");
            previous = waypoint;
        }

        for (var step = 0; step < 200 && walker.MoveTarget is not null; step++)
        {
            simulation.Advance(RtsSimulation.TickSeconds);
            Assert(!west.Footprint.Contains(walker.Position) && !east.Footprint.Contains(walker.Position), "a moving unit never stands inside a footprint");
        }

        Assert(walker.Position.DistanceTo(new SimVector2(seam, 220)) < 12.0f, "a unit walled off by packed buildings walks around them");
    }

    private static void ValidateOutlineCornersAreContained(SmokeTestContext context)
    {
        foreach (var building in context.Catalog.Buildings.Values)
        {
            var footprint = BuildingFootprint.Place(building.Footprint, new SimVector2(0, 0));
            foreach (var corner in footprint.Parts.Where(part => !part.IsCircle).SelectMany(part => part.Points))
            {
                Assert(footprint.Contains(corner), $"{building.Id} contains its own outline corner {corner}");
            }
        }
    }

    private static void ValidateRepairReachesWalls(SmokeTestContext context)
    {
        // Sweep Hub positions across one 32 px path cell, since reachability depends on grid alignment.
        foreach (var hubPosition in new[] { new SimVector2(0, 0), new SimVector2(10, 0), new SimVector2(20, 6), new SimVector2(5, 27), new SimVector2(29, 15) })
        {
            foreach (var startOffset in new[] { new SimVector2(200, 0), new SimVector2(190, 170), new SimVector2(-30, -210), new SimVector2(-220, 40) })
            {
                var simulation = new RtsSimulation(context.Catalog, 4000, []);
                var hub = simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, hubPosition);
                hub.ApplyDamage(300, "explosive");
                var damagedHealth = hub.Health;
                var grunt = simulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, hubPosition + startOffset);

                Assert(simulation.CommandUnitRepairBuilding(grunt.EntityId, hub.EntityId).Success, "Grunt accepts a repair order");
                var requestsBefore = simulation.DebugPathRequestCount;
                TickFor(simulation, 15.0f);
                Assert(hub.Health > damagedHealth, $"a Grunt approaching a Hub at {hubPosition} from {startOffset} reaches a wall it can repair");
                Assert(simulation.DebugPathRequestCount - requestsBefore < 20, $"a repair approach from {startOffset} does not repath every tick");
            }
        }
    }

    private static void ValidateRepairGivesUpWhenWalledOff(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        var hub = simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(0, 0));
        hub.ApplyDamage(300, "explosive");
        // Starting buildings skip placement checks, so overlapping Power Plants make a solid ring.
        foreach (var offset in SquareRing(halfSize: 150.0f, spacing: 50.0f))
        {
            simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, offset);
        }

        var damagedHealth = hub.Health;
        var grunt = simulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new SimVector2(420, 0));
        simulation.CommandUnitRepairBuilding(grunt.EntityId, hub.EntityId);
        var requestsBefore = simulation.DebugPathRequestCount;
        TickFor(simulation, 10.0f);
        Assert(hub.Health <= damagedHealth, "a Grunt cannot repair through a ring of buildings");
        Assert(grunt.RepairTargetBuildingEntityId is null, "a Grunt that cannot reach a building drops the repair order");
        Assert(simulation.DebugPathRequestCount - requestsBefore < 20, "an unreachable repair does not repath every tick");
    }

    private static void ValidateRetreatStandsDownAtPathEnd(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, [], 450);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
        var hubPosition = EnemyAiMarkers.FirstLanding.HubPosition;
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, hubPosition, ContentIds.Factions.PrivateMilitary);
        var enemy = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, hubPosition + new SimVector2(-180, 0));
        TickFor(simulation, 0.2f);
        enemy.ApplyDamage(34, "ballistic");
        TickFor(simulation, 0.3f);
        Assert(enemy.IsEnemyRetreating, "a badly damaged committed attacker retreats");

        var requestsBefore = simulation.DebugPathRequestCount;
        TickFor(simulation, 10.0f);
        Assert(!enemy.IsEnemyRetreating && !enemy.IsEnemyAttackCommitted, "a retreating attacker stands down where its path to the Hub ends");
        Assert(simulation.DebugPathRequestCount - requestsBefore < 20, "a retreat toward the blocked Hub center does not repath every tick");
    }

    private static void ValidateEnemyDoesNotWallOffItsHub(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, [], 450);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-600, -140));
        var hubPosition = EnemyAiMarkers.FirstLanding.HubPosition;
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, hubPosition, ContentIds.Factions.PrivateMilitary);
        // Ring the enemy Hub except for a gap on the west side, toward its rally point.
        var gapCenter = hubPosition + new SimVector2(-150, 0);
        foreach (var offset in SquareRing(halfSize: 150.0f, spacing: 50.0f).Where(offset => (hubPosition + offset).DistanceTo(gapCenter) > 60.0f))
        {
            simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, hubPosition + offset, ContentIds.Factions.PrivateMilitary);
        }

        Assert(simulation.IsReachableForFaction(ContentIds.Factions.PrivateMilitary, hubPosition, EnemyAiMarkers.FirstLanding.RallyPosition), "the fixture leaves the enemy Hub one exit");
        Assert(simulation.WouldWallOffEnemyHub(ContentIds.Buildings.Barracks, gapCenter), "a Barracks in the only exit would wall off the enemy Hub");
        Assert(!simulation.WouldWallOffEnemyHub(ContentIds.Buildings.Barracks, hubPosition + new SimVector2(0, 400)), "a Barracks away from the exit leaves the Hub open");
        Assert(!simulation.TryPlaceEnemyBuilding(ContentIds.Buildings.PowerPlant, gapCenter), "the AI refuses a placement that seals its own Hub");
        Assert(simulation.IsReachableForFaction(ContentIds.Factions.PrivateMilitary, hubPosition, EnemyAiMarkers.FirstLanding.RallyPosition), "the enemy Hub keeps its exit after the refused placement");
    }

    private static IEnumerable<SimVector2> SquareRing(float halfSize, float spacing)
    {
        for (var along = -halfSize; along <= halfSize; along += spacing)
        {
            yield return new SimVector2(along, -halfSize);
            yield return new SimVector2(along, halfSize);
            yield return new SimVector2(-halfSize, along);
            yield return new SimVector2(halfSize, along);
        }
    }

    private static RtsSimulation CreateSimulation(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-700, 0));
        return simulation;
    }
}
