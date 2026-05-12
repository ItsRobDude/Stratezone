using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class WellsAtTheRidgeSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;

        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub), "Level 2 runtime starts without a player Colony Hub");
        var beforeHubPowerPlant = simulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, runtime.Markers["player_landing_zone"] + new SimVector2(-260, -120));
        Assert(!beforeHubPowerPlant.IsLegal, "Level 2 requires Colony Hub deployment before other player structures");
        Assert(beforeHubPowerPlant.MessageKey == "sim.placement.requires_colony_hub", "pre-Hub structure placement returns a stable message key");

        var hubPlacement = simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, runtime.Markers["player_landing_zone"]);
        Assert(hubPlacement.Success, $"Level 2 lets the player deploy the Colony Hub from mission data ({hubPlacement.MessageKey}: {hubPlacement.Message})");
        Assert(!simulation.ValidatePlacement(ContentIds.Buildings.ColonyHub, runtime.Markers["player_landing_zone"] + new SimVector2(150, 0)).IsLegal, "Level 2 rejects a second player Colony Hub");
        var afterHubPowerPlant = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, runtime.Markers["player_landing_zone"] + new SimVector2(-260, -120));
        Assert(afterHubPowerPlant.Success, $"Level 2 allows normal building after the Colony Hub is deployed ({afterHubPowerPlant.MessageKey}: {afterHubPowerPlant.Message})");

        var westWater = runtime.Map.TerrainRegions.Single(region => region.Id == "west_channel_water");
        var blockedTerrainPlacement = simulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, westWater.Center);
        Assert(!blockedTerrainPlacement.IsLegal, "Level 2 water channel rejects building placement");
        Assert(blockedTerrainPlacement.MessageKey == "sim.placement.blocked_by_terrain", "blocked terrain placement returns a stable message key");
        var outsideMarkedRegionPlacement = simulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(-1040, 270));
        Assert(outsideMarkedRegionPlacement.IsLegal, $"Level 2 allows powered Pylon placement outside marked base regions ({outsideMarkedRegionPlacement.MessageKey}: {outsideMarkedRegionPlacement.Reason})");

        Assert(simulation.Bridges.Count == 2, "Level 2 runtime creates bridge state from map objects");
        Assert(simulation.Bridges.All(bridge => bridge.IsIntact), "Level 2 bridges start intact");
        ValidateBridgePassabilityAndRepair(context);

        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_enemy" &&
            building.IsPowered), "Level 2 starts the enemy entrenched on its own powered well");
        Assert(simulation.EnergyWalls.Any(wall =>
        {
            var start = simulation.Buildings.Single(building => building.EntityId == wall.StartAnchorEntityId);
            var end = simulation.Buildings.Single(building => building.EntityId == wall.EndAnchorEntityId);
            return start.FactionId == ContentIds.Factions.PrivateMilitary &&
                end.FactionId == ContentIds.Factions.PrivateMilitary;
        }), "Level 2 starts with a real powered enemy defense wall segment");
        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            building.IsPowered), "Level 2 enemy forward Pylon starts powered enough to contest the island well");

        TickFor(simulation, 0.1f);
        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested"), "Level 2 enemy does not instantly own the island well");
        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstCentralWellRebuildDelaySeconds + 0.2f);
        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested"), "Level 2 enemy AI can race for the island well when the bridge route is intact");

        ValidateEnemyBridgeAwareDeferral(context);
        ValidateBaseBreachRebuildPriority(context);
        ValidateEnemyBaseDefenseResponse(context);
        ValidateEnemyPatrolsExploreFlanks(context);
        ValidateWallDoesNotBlockFire(context);
        ValidateNaturalPlayerPylonRoute(context);
        ValidatePlayerGuardianRoute(context);

        Assert(simulation.MissionState.PrimaryTextKey == "mission.objective.destroy_enemy_colony_hub", "Level 2 objective text targets the enemy Colony Hub");
        var enemyHub = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        enemyHub.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);
        var releasedHubTank = simulation.Units.Single(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.Id == ContentIds.Units.MediumTank &&
            unit.IsColonyHubOccupant &&
            !unit.IsDestroyed);
        Assert(simulation.MissionState.Status == MissionStatus.Active, "destroying the Level 2 enemy Colony Hub releases a tank that must be killed before victory");
        releasedHubTank.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);
        Assert(simulation.MissionState.Status == MissionStatus.Won, "Level 2 wins after the enemy Colony Hub and its released tank occupant are destroyed");
    }

    private static void ValidateBridgePassabilityAndRepair(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var bridge = simulation.FindBridge("bridge_central_isle_west")!;

        var pathProbe = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-650, 0));
        simulation.CommandUnitMove(pathProbe.EntityId, runtime.Markers["central_island_well"]);
        Assert(pathProbe.PathWaypoints.Count > 0 && !pathProbe.IsPathBlocked, "Level 2 intact west bridge allows pathing to the island well");
        pathProbe.ApplyDamage(9999, "debug");

        simulation.DebugDamageBridge(bridge.Id, bridge.MaxHealth + 1.0f);
        Assert(!bridge.IsIntact, "Level 2 bridge damage can collapse an intact bridge");
        var blockedProbe = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-650, 0));
        simulation.CommandUnitMove(blockedProbe.EntityId, runtime.Markers["central_island_well"]);
        Assert(blockedProbe.IsPathBlocked, "Level 2 collapsed bridge blocks the same island route");
        blockedProbe.ApplyDamage(9999, "debug");

        var grunt = simulation.Units.First(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Grunt &&
            !unit.IsDestroyed);
        var repair = simulation.CommandUnitRepairBridge(grunt.EntityId, bridge.Id);
        Assert(repair.Success, $"Level 2 Grunt can start bridge repair ({repair.MessageKey}: {repair.Message})");
        TickFor(simulation, 36.0f);
        Assert(bridge.IsIntact && !bridge.IsDamaged, "Level 2 Grunt repair restores the collapsed bridge");

        var restoredProbe = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-650, 0));
        simulation.CommandUnitMove(restoredProbe.EntityId, runtime.Markers["central_island_well"]);
        Assert(!restoredProbe.IsPathBlocked, "Level 2 restored bridge reopens island pathing without mission restart");
    }

    private static void ValidateEnemyBridgeAwareDeferral(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, runtime.Markers["player_landing_zone"]).Success, "bridge deferral route deploys player Hub");
        var enemyBridge = simulation.FindBridge("bridge_central_isle_east")!;
        simulation.DebugDamageBridge(enemyBridge.Id, enemyBridge.MaxHealth + 1.0f);

        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstCentralWellRebuildDelaySeconds + runtime.Mission.EnemyAiProfile.CentralWellRebuildCooldownSeconds + 0.2f);
        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_wells_ridge_contested" &&
            !building.IsDestroyed), "Level 2 enemy defers island-well rebuilds while its required bridge is broken");

        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstAttackDelaySeconds + 0.2f);
        Assert(!simulation.Units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            unit.IsEnemyAttackCommitted), "Level 2 enemy defers committed attacks when the bridge route is unreachable");
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
        var forwardPylon = simulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_pylon_weak_point"]) < 0.01f &&
            !building.IsDestroyed);
        basePylon.ApplyDamage(9999, "explosive");
        wallPowerPylon.ApplyDamage(9999, "explosive");
        forwardPylon.ApplyDamage(9999, "explosive");
        defenseTower.ApplyDamage(9999, "explosive");
        simulation.AddUnit(ContentIds.Units.Tank, ContentIds.Factions.PlayerExpedition, runtime.Markers["enemy_base"] + new SimVector2(-90, 0));

        TickFor(simulation, runtime.Mission.EnemyAiProfile.FirstRebuildDelaySeconds + 0.2f);

        Assert(simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_base_pylon"]) < 0.01f &&
            !building.IsDestroyed), "Level 2 enemy rebuilds the defensive power spine during a base breach when the wall route depends on it");
        Assert(!simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(runtime.Markers["enemy_pylon_weak_point"]) < RtsSimulation.ToWorldRadius(6.0f) &&
            !building.IsDestroyed), "Level 2 enemy skips forward expansion Pylon rebuilds during a base breach");
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
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, landingZone + new SimVector2(-260, -120)).Success, "Level 2 natural route places a Power Plant from the landing marker");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-1040, 270)).Success, "Level 2 natural route can start a Pylon chain from base");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-610, 245)).Success, "Level 2 natural route can power the player well approach");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-510, 120)).Success, "Level 2 natural route can reach the west bridge approach");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-170, 20)).Success, "Level 2 natural route can extend power onto the island");
        var contestedWellPlacement = simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, runtime.Markers["central_island_well"]);
        Assert(contestedWellPlacement.Success, $"Level 2 natural route can power the island Extractor without exact designer-only coordinates ({contestedWellPlacement.MessageKey}: {contestedWellPlacement.Message})");
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
            !building.IsDestroyed), "Level 2 enemy does not rebuild the forward Pylon after the player owns the island well");
    }

    private static void ValidatePlayerGuardianRoute(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var hubPosition = runtime.Markers["player_landing_zone"];
        var playerWell = runtime.Markers["player_start_well"];

        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, hubPosition).Success, "Level 2 route starts with player-deployed Colony Hub");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, hubPosition + new SimVector2(-260, -120)).Success, "Level 2 route places Power Plant inside landing clearing");
        var barracks = simulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, hubPosition + new SimVector2(-180, -260));
        Assert(barracks.Success, $"Level 2 route places Barracks ({barracks.MessageKey}: {barracks.Message})");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-1040, 270)).Success, "Level 2 route can start a Pylon chain toward the player well");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-610, 245)).Success, "Level 2 route can power the player well approach");
        var startWellPlacement = simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, playerWell);
        Assert(startWellPlacement.Success, $"Level 2 route captures the safe start well ({startWellPlacement.MessageKey}: {startWellPlacement.Message})");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-510, 120)).Success, "Level 2 route can extend a Pylon toward the west bridge");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-170, 20)).Success, "Level 2 route can continue Pylon power onto the island without blocking the well");
        var contestedWellPlacement = simulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, runtime.Markers["central_island_well"]);
        Assert(contestedWellPlacement.IsLegal, $"Level 2 route can legally power an Extractor at the island well ({contestedWellPlacement.MessageKey}: {contestedWellPlacement.Reason})");
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
