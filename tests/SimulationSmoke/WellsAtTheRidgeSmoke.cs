using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class WellsAtTheRidgeSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var catalog = context.Catalog;
        var ridgeRuntime = MissionRuntimeFactory.Create(catalog, ContentIds.Missions.WellsAtTheRidge);
        var ridgeSimulation = ridgeRuntime.Simulation;

        Assert(!ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub), "Level 2 runtime starts without a player Colony Hub");
        var beforeHubPowerPlant = ridgeSimulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, ridgeRuntime.Markers["player_landing_zone"] + new SimVector2(-240, 170));
        Assert(!beforeHubPowerPlant.IsLegal, "Level 2 requires Colony Hub deployment before other player structures");
        Assert(beforeHubPowerPlant.MessageKey == "sim.placement.requires_colony_hub", "pre-Hub structure placement returns a stable message key");
        var hubPlacement = ridgeSimulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, ridgeRuntime.Markers["player_landing_zone"]);
        Assert(hubPlacement.Success, $"Level 2 lets the player deploy the Colony Hub from mission data ({hubPlacement.MessageKey}: {hubPlacement.Message})");
        Assert(!ridgeSimulation.ValidatePlacement(ContentIds.Buildings.ColonyHub, ridgeRuntime.Markers["player_landing_zone"] + new SimVector2(150, 0)).IsLegal, "Level 2 rejects a second player Colony Hub");
        var afterHubPowerPlant = ridgeSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, ridgeRuntime.Markers["player_landing_zone"] + new SimVector2(-240, 170));
        Assert(afterHubPowerPlant.Success, $"Level 2 allows normal building after the Colony Hub is deployed ({afterHubPowerPlant.MessageKey}: {afterHubPowerPlant.Message})");
        var northRidgeBlocker = ridgeRuntime.Map.TerrainRegions.Single(region => region.Id == "ridge_north_blocker");
        var southRidgeBlocker = ridgeRuntime.Map.TerrainRegions.Single(region => region.Id == "ridge_south_blocker");
        var blockedTerrainPlacement = ridgeSimulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, northRidgeBlocker.Center);
        Assert(!blockedTerrainPlacement.IsLegal, "Level 2 blocked terrain rejects building placement");
        Assert(blockedTerrainPlacement.MessageKey == "sim.placement.blocked_by_terrain", "blocked terrain placement returns a stable message key");
        var outsideMarkedRegionPlacement = ridgeSimulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(-1060, 190));
        Assert(outsideMarkedRegionPlacement.IsLegal, $"Level 2 allows powered Pylon placement outside marked base regions ({outsideMarkedRegionPlacement.MessageKey}: {outsideMarkedRegionPlacement.Reason})");
        var ridgePathProbe = ridgeSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-640, -290));
        ridgeSimulation.CommandUnitMove(ridgePathProbe.EntityId, new SimVector2(860, -290));
        Assert(ridgePathProbe.PathWaypoints.Count > 1, "Level 2 pathfinding routes around ridge blockers instead of taking the direct line");
        Assert(!ridgePathProbe.PathWaypoints.Any(waypoint => northRidgeBlocker.Contains(waypoint, 0.0f)), "Level 2 path waypoints stay out of blocked terrain");
        ridgePathProbe.ApplyDamage(9999, "debug");
        Assert(ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_enemy" &&
            building.IsPowered), "Level 2 starts the enemy entrenched on its own powered well");
        Assert(ridgeSimulation.EnergyWalls.Any(wall =>
        {
            var start = ridgeSimulation.Buildings.Single(building => building.EntityId == wall.StartAnchorEntityId);
            var end = ridgeSimulation.Buildings.Single(building => building.EntityId == wall.EndAnchorEntityId);
            return start.FactionId == ContentIds.Factions.PrivateMilitary &&
                end.FactionId == ContentIds.Factions.PrivateMilitary;
        }), "Level 2 starts with a real powered enemy defense wall segment");
        Assert(ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(ridgeRuntime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            building.IsPowered), "Level 2 enemy forward Pylon starts powered enough to contest later");
        var enemyWall = ridgeSimulation.EnergyWalls.First(wall =>
        {
            var start = ridgeSimulation.Buildings.Single(building => building.EntityId == wall.StartAnchorEntityId);
            var end = ridgeSimulation.Buildings.Single(building => building.EntityId == wall.EndAnchorEntityId);
            return start.FactionId == ContentIds.Factions.PrivateMilitary &&
                end.FactionId == ContentIds.Factions.PrivateMilitary;
        });
        Assert(ridgeSimulation.IsLineBlockedByEnergyWall(new SimVector2(640, -185), new SimVector2(820, -185)), "Level 2 enemy wall buffer blocks the north walk-around lane");
        Assert(ridgeSimulation.IsLineBlockedByEnergyWall(new SimVector2(640, 60), new SimVector2(820, 60)), "Level 2 enemy wall buffer blocks the south walk-around lane");
        Assert(northRidgeBlocker.Contains(enemyWall.ExtendedStart, EnergyWallSegment.BlockingClearance), "Level 2 enemy wall north buffer overlaps the ridge blocker instead of leaving a walk-around gap");
        Assert(southRidgeBlocker.Contains(enemyWall.ExtendedEnd, EnergyWallSegment.BlockingClearance), "Level 2 enemy wall south buffer overlaps the ridge blocker instead of leaving a walk-around gap");
        var enemyPylons = ridgeSimulation.Buildings.Where(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            !building.IsDestroyed).ToArray();
        foreach (var enemyPylon in enemyPylons)
        {
            Assert(
                SimulationGeometry.DistancePointToSegment(enemyPylon.Position, enemyWall.Start, enemyWall.End) > enemyPylon.FootprintWorldRadius + 8.0f,
                "Level 2 enemy Pylons are not authored inside the defense wall lane");
        }
        TickFor(ridgeSimulation, 0.1f);
        Assert(!ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested"), "Level 2 enemy does not instantly own the midfield well");
        TickFor(ridgeSimulation, ridgeRuntime.Mission.EnemyAiProfile.FirstRebuildDelaySeconds + 20.0f);
        Assert(!ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed), "Level 2 enemy core rebuild timing does not also claim the midfield well");
        TickFor(ridgeSimulation, ridgeRuntime.Mission.EnemyAiProfile.FirstCentralWellRebuildDelaySeconds - ridgeRuntime.Mission.EnemyAiProfile.FirstRebuildDelaySeconds - 19.8f);
        var forwardPylonAfterDelay = ridgeSimulation.Buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(ridgeRuntime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            !building.IsDestroyed);
        var basePylonAfterDelay = ridgeSimulation.Buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(ridgeRuntime.Markers["enemy_base_pylon"]) < 0.01f &&
            !building.IsDestroyed);
        Assert(ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested"), $"Level 2 enemy AI races for the contested ridge well; base pylon powered={basePylonAfterDelay?.IsPowered}; forward pylon powered={forwardPylonAfterDelay?.IsPowered}; enemy materials={ridgeSimulation.EnemyMaterials:0.0}");
        var firstContestedExtractor = ridgeSimulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed);
        firstContestedExtractor.ApplyDamage(9999, "explosive");
        TickFor(ridgeSimulation, 60.0f);
        Assert(!ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed), "Level 2 enemy does not immediately rebuild the destroyed midfield Extractor");
        TickFor(ridgeSimulation, ridgeRuntime.Mission.EnemyAiProfile.CentralWellRebuildCooldownSeconds - 59.7f);
        Assert(ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed), "Level 2 enemy may retake the midfield well after the authored cooldown");
        var secondContestedExtractor = ridgeSimulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed);
        secondContestedExtractor.ApplyDamage(9999, "explosive");
        TickFor(ridgeSimulation, ridgeRuntime.Mission.EnemyAiProfile.CentralWellRebuildCooldownSeconds + 1.0f);
        Assert(!ridgeSimulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed), "Level 2 enemy stops treating the midfield well as an infinite rebuild loop");
        Assert(ridgeSimulation.MissionState.PrimaryTextKey == "mission.objective.destroy_enemy_colony_hub", "Level 2 objective text targets the enemy Colony Hub");
        ValidateNaturalPlayerPylonRoute(context);
        ValidateBaseBreachRebuildPriority(context);
        ValidateEnemyBaseDefenseResponse(context);
        ValidateEnemyPatrolsExploreFlanks(context);
        ValidateWallDoesNotBlockFire(context);
        ValidatePlayerGuardianRoute(context);

        var ridgeEnemyHub = ridgeSimulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        ridgeEnemyHub.ApplyDamage(9999, "explosive");
        TickFor(ridgeSimulation, 0.1f);
        var releasedHubTank = ridgeSimulation.Units.Single(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.Id == ContentIds.Units.MediumTank &&
            unit.IsColonyHubOccupant &&
            !unit.IsDestroyed);
        Assert(ridgeSimulation.MissionState.Status == MissionStatus.Active, "destroying the Level 2 enemy Colony Hub releases a tank that must be killed before victory");
        releasedHubTank.ApplyDamage(9999, "explosive");
        TickFor(ridgeSimulation, 0.1f);
        Assert(ridgeSimulation.MissionState.Status == MissionStatus.Won, "Level 2 wins after the enemy Colony Hub and its released tank occupant are destroyed");
    }

    private static void ValidateBaseBreachRebuildPriority(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var basePylon = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_base_pylon"]) < 0.01f &&
            !building.IsDestroyed);
        var defenseTower = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.DefenseTower &&
            building.Position.DistanceTo(runtime.Markers["enemy_defense"]) < 0.01f &&
            !building.IsDestroyed);
        var wallPowerPylon = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_wall_power_pylon"]) < 0.01f &&
            !building.IsDestroyed);
        basePylon.ApplyDamage(9999, "explosive");
        wallPowerPylon.ApplyDamage(9999, "explosive");
        defenseTower.ApplyDamage(9999, "explosive");
        simulation.AddUnit(ContentIds.Units.Tank, ContentIds.Factions.PlayerExpedition, runtime.Markers["enemy_base"] + new SimVector2(-90, 0));

        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstRebuildDelaySeconds + 0.2f);

        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_base_pylon"]) < 0.01f &&
            !building.IsDestroyed), "Level 2 enemy skips base Pylon rebuilds during a base breach");
        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.DefenseTower &&
            building.Position.DistanceTo(runtime.Markers["enemy_defense"]) < 0.01f &&
            !building.IsDestroyed), "Level 2 enemy prioritizes rebuilding a defensive tower during a base breach");
        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_wall_power_pylon"]) < RtsSimulation.ToWorldRadius(6.0f) &&
            !building.IsDestroyed), "Level 2 enemy rebuilds defensive wall power instead of expansion power during a base breach");
    }

    private static void ValidateEnemyBaseDefenseResponse(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var raider = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, runtime.Markers["enemy_base"] + new SimVector2(-260, 0));

        TickFor(simulation, 0.2f);

        Assert(simulation.Units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            unit.TargetUnitEntityId == raider.EntityId), "Level 2 idle enemy defenders pursue a base intruder before the scheduled attack timer");
        Assert(simulation.ProductionOrders.Any(order =>
            order.FactionId == ContentIds.Factions.PrivateMilitary &&
            order.UnitId != ContentIds.Units.Grunt), "Level 2 enemy queues combat production immediately during a base breach");
    }

    private static void ValidateEnemyPatrolsExploreFlanks(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var patrolPositions = runtime.Mission.EnemyAiProfile.PatrolMarkerIds
            .Select(markerId => runtime.Markers[markerId])
            .ToArray();

        Assert(runtime.Mission.EnemyAiProfile.MaxPatrolDispatches == 3, "Level 2 enemy profile dispatches a few early patrols instead of a single lane scout");
        Assert(patrolPositions.Length == 3, "Level 2 enemy profile has three authored patrol areas for top/bottom exploration pressure");
        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstPatrolDelaySeconds + 0.2f);
        Assert(simulation.EnemyOfficer.PatrolDispatches == 1, "Level 2 sends its first patrol to an authored exploration marker before the main attack timer");
        Assert(EnemyPatrolsAtAuthoredDestinations(simulation, patrolPositions).Count >= 1, "Level 2 first patrol uses an authored exploration destination");
        TickFor(simulation, runtime.Mission.EnemyAiProfile.PatrolIntervalSeconds + 0.2f);
        Assert(simulation.EnemyOfficer.PatrolDispatches == 2, "Level 2 sends a second early patrol instead of collapsing all pressure into the bridge lane");
        TickFor(simulation, runtime.Mission.EnemyAiProfile.PatrolIntervalSeconds + 0.2f);
        Assert(simulation.EnemyOfficer.PatrolDispatches == 3, "Level 2 sends a third early patrol before normal attack pressure starts");
        Assert(simulation.EnemyOfficer.PatrolAreasVisited == 3, "Level 2 patrols fan out across all three authored exploration areas");
    }

    private static void ValidateWallDoesNotBlockFire(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var wallPowerPylon = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_wall_power_pylon"]) < 0.01f &&
            !building.IsDestroyed);
        var rifleman = simulation.AddUnit(
            ContentIds.Units.Rifleman,
            ContentIds.Factions.PlayerExpedition,
            runtime.Markers["enemy_wall_power_pylon"] + new SimVector2(-190, 0));

        simulation.CommandUnitAttackBuilding(rifleman.EntityId, wallPowerPylon.EntityId);
        TickFor(simulation, 4.0f);

        Assert(wallPowerPylon.Health < wallPowerPylon.Definition.Health, "Level 2 energy walls block movement but not rifle fire at a power Pylon behind the wall");
        Assert(rifleman.Position.X < runtime.Markers["enemy_defense"].X, "Level 2 rifleman shoots the wall-power Pylon from the near side instead of pathing around the wall");
        Assert(!rifleman.IsBlockedByEnergyWall, "Level 2 rifleman attacks a through-wall Pylon from a reachable firing point instead of getting stuck on the wall");
    }

    private static List<int> EnemyPatrolsAtAuthoredDestinations(RtsSimulation simulation, IReadOnlyList<SimVector2> patrolPositions)
    {
        return simulation.Units
            .Where(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                unit.IsEnemyScout &&
                unit.MoveTarget is not null)
            .Select(unit => Array.FindIndex(patrolPositions.ToArray(), position => position.DistanceTo(unit.MoveTarget!.Value) < 0.01f))
            .Where(index => index >= 0)
            .ToList();
    }

    private static void ValidateNaturalPlayerPylonRoute(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var landingZone = runtime.Markers["player_landing_zone"];

        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, landingZone).Success, "Level 2 natural route deploys the Colony Hub at the landing marker");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, landingZone + new SimVector2(-240, 170)).Success, "Level 2 natural route places a forward Power Plant from the landing marker");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-1060, 190)).Success, "Level 2 natural route can start a Pylon chain east from base");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-360, 25)).Success, "Level 2 natural route can cross the longer bridge approach");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-60, 25)).Success, "Level 2 natural route can turn the Pylon chain toward the midfield well");
        var contestedWellPlacement = simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, runtime.Markers["contested_ridge_well"]);
        Assert(contestedWellPlacement.Success, $"Level 2 natural route can power the midfield Extractor without exact designer-only coordinates ({contestedWellPlacement.MessageKey}: {contestedWellPlacement.Message})");
        var enemyForwardPylon = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            !building.IsDestroyed);
        enemyForwardPylon.ApplyDamage(9999, "explosive");
        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstRebuildDelaySeconds + runtime.Mission.EnemyAiProfile.RebuildCooldownSeconds + 0.2f);
        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            !building.IsDestroyed), "Level 2 enemy does not rebuild the forward Pylon after the player owns the midfield well");
    }

    private static void ValidatePlayerGuardianRoute(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var hubPosition = runtime.Markers["player_landing_zone"] + new SimVector2(-120, -80);
        var playerWell = runtime.Markers["player_start_well"];

        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, hubPosition).Success, "Level 2 route starts with player-deployed Colony Hub");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, hubPosition + new SimVector2(230, -20)).Success, "Level 2 route places Power Plant inside landing clearing");
        var barracks = simulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, hubPosition + new SimVector2(200, -170));
        Assert(barracks.Success, $"Level 2 route places Barracks ({barracks.MessageKey}: {barracks.Message})");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, playerWell).Success, "Level 2 route captures the safe start well");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-560, 75)).Success, "Level 2 route can extend a Pylon into the western power corridor");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-60, 25)).Success, "Level 2 route can continue Pylon power through the central corridor without blocking the well");
        var contestedWellPlacement = simulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, runtime.Markers["contested_ridge_well"]);
        Assert(contestedWellPlacement.IsLegal, $"Level 2 route can legally power an Extractor at the midfield well ({contestedWellPlacement.MessageKey}: {contestedWellPlacement.Reason})");
        Assert(simulation.TryQueueUnit(ContentIds.Units.Grunt, barracks.Building!.EntityId).Success, "Level 2 route trains the second Grunt needed for retrofit staffing");

        TickFor(simulation, context.Catalog.GetUnit(ContentIds.Units.Grunt).TrainTimeSeconds + 0.2f);
        Assert(simulation.Units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Grunt &&
            !unit.IsDestroyed) >= 2, "Level 2 route has two live Grunts after training");

        var retrofit = simulation.TryStartGuardianRetrofit(barracks.Building.EntityId);
        Assert(retrofit.Success, $"Level 2 route starts Guardian Retrofit ({retrofit.MessageKey}: {retrofit.Message})");
        TickFor(simulation, context.Catalog.GetBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit).DurationSeconds + 0.2f);
        Assert(barracks.Building.HasBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit), "Level 2 route completes Guardian Retrofit");
        Assert(simulation.TryQueueUnit(ContentIds.Units.Guardian, barracks.Building.EntityId).Success, "Level 2 route can queue Guardian after retrofit");
        TickFor(simulation, context.Catalog.GetUnit(ContentIds.Units.Guardian).TrainTimeSeconds + 0.2f);
        Assert(simulation.Units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Guardian &&
            !unit.IsDestroyed), "Level 2 route produces a Guardian as the specialist unlock");
    }
}
