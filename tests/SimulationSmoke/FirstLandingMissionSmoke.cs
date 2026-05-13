using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class FirstLandingMissionSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var catalog = context.Catalog;
        var mission = context.FirstLandingMission;
        var missionRuntime = MissionRuntimeFactory.Create(catalog, ContentIds.Missions.FirstLanding);
        var missionMarkers = missionRuntime.Markers;

        Assert(mission.ResourceWellPlacements.Count == 2, "mission data owns resource well placements");
        Assert(mission.StartingEntities.Any(entity => entity.ContentId == ContentIds.Units.Grunt), "mission data owns starting player units");
        Assert(mission.Markers.Any(marker => marker.Id == "enemy_pylon_weak_point"), "mission data exposes an enemy pylon weak-point marker");
        Assert(mission.StartingEntities.Any(entity => entity.ContentId == ContentIds.Buildings.Pylon && entity.MarkerId == "enemy_pylon_weak_point"), "mission starts with a real enemy Pylon weak point");

        var missionStartingBuildings = mission.StartingEntities
            .Where(entity => entity.ContentId.StartsWith("building_", StringComparison.Ordinal))
            .Select(entity => (
                Entity: entity,
                Position: missionMarkers[entity.MarkerId] + entity.Offset,
                Definition: catalog.GetBuilding(entity.ContentId)))
            .ToArray();

        foreach (var left in missionStartingBuildings)
        {
            foreach (var right in missionStartingBuildings)
            {
                if (left.Entity == right.Entity ||
                    string.CompareOrdinal(left.Entity.MarkerId, right.Entity.MarkerId) >= 0)
                {
                    continue;
                }

                var requiredDistance = RtsSimulation.ToWorldRadius(left.Definition.FootprintRadius + left.Definition.PlacementBuffer) +
                    RtsSimulation.ToWorldRadius(right.Definition.FootprintRadius + right.Definition.PlacementBuffer);
                var actualDistance = left.Position.DistanceTo(right.Position);
                Assert(
                    actualDistance >= requiredDistance,
                    $"mission starting buildings are spaced legally ({left.Entity.ContentId} at {left.Entity.MarkerId}, {right.Entity.ContentId} at {right.Entity.MarkerId})");
            }
        }

        var routeSimulation = MissionRuntimeFactory.Create(catalog, ContentIds.Missions.FirstLanding).Simulation;
        var enemyForwardPylon = routeSimulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.Pylon &&
            building.Position.DistanceTo(missionMarkers["enemy_pylon_weak_point"]) < 0.01f);
        Assert(enemyForwardPylon.IsPowered, "enemy pylon weak point starts powered by the enemy base chain");
        Assert(routeSimulation.EnergyWalls.Any(wall =>
        {
            var start = routeSimulation.Buildings.Single(building => building.EntityId == wall.StartAnchorEntityId);
            var end = routeSimulation.Buildings.Single(building => building.EntityId == wall.EndAnchorEntityId);
            return start.FactionId == ContentIds.Factions.PrivateMilitary &&
                end.FactionId == ContentIds.Factions.PrivateMilitary;
        }), "mission starts with a powered enemy tower-wall route");

        TickFor(routeSimulation, 0.1f);
        var enemyCentralExtractor = routeSimulation.Buildings.Single(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ExtractorRefinery &&
            building.ResourceWellId == "well_first_landing_central");
        Assert(enemyCentralExtractor.IsPowered, "enemy pylon powers the central well Extractor");
        Assert(routeSimulation.IsLineBlockedByEnergyWall(new SimVector2(-300, -140), enemyCentralExtractor.Position), "mission enemy wall blocks the direct player-base route to the central Extractor");
        enemyForwardPylon.ApplyDamage(9999, "explosive");
        TickFor(routeSimulation, 0.1f);
        Assert(routeSimulation.EnemyAiTelemetry.PowerStrikesTaken == 1, "destroying the enemy Pylon counts as internal power-strike telemetry");
        Assert(!enemyCentralExtractor.IsPowered, "destroying the enemy Pylon shuts off the central enemy Extractor");
        Assert(!routeSimulation.EnergyWalls.Any(wall =>
        {
            var start = routeSimulation.Buildings.Single(building => building.EntityId == wall.StartAnchorEntityId);
            var end = routeSimulation.Buildings.Single(building => building.EntityId == wall.EndAnchorEntityId);
            return start.FactionId == ContentIds.Factions.PrivateMilitary &&
                end.FactionId == ContentIds.Factions.PrivateMilitary;
        }), "destroying the enemy Pylon drops the enemy tower-wall route");
        enemyCentralExtractor.ApplyDamage(9999, "explosive");
        Assert(routeSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-170, 40)).Success, "retake route places player power");
        Assert(routeSimulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-220, 160)).Success, "retake route places first player Pylon");
        Assert(routeSimulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-10, 120)).Success, "retake route chains player Pylon toward the central well");
        Assert(routeSimulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(140, 210)).Success, "retake route avoids the enemy wall anchors while extending support");
        Assert(routeSimulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(300, 170)).Success, "retake route reaches the central well with powered support");
        var centralRetakeValidation = routeSimulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, missionMarkers["central_well"]);
        Assert(centralRetakeValidation.IsLegal, $"destroyed enemy Extractor releases the central well for player retake ({centralRetakeValidation.MessageKey}: {centralRetakeValidation.Reason})");

        var pacedMissionSimulation = MissionRuntimeFactory.Create(catalog, ContentIds.Missions.FirstLanding).Simulation;

        TickFor(pacedMissionSimulation, mission.EnemyAiProfile.FirstAttackDelaySeconds - 5.0f);
        Assert(!pacedMissionSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.TargetBuildingEntityId is not null), "mission AI profile delays first enemy pressure");
        Assert(pacedMissionSimulation.EnemyOfficer.ScoutDispatched, "mission AI can dispatch a scout before the first attack");
        Assert(pacedMissionSimulation.Units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.IsEnemyRoaming &&
            !unit.IsEnemyAttackCommitted), "scouting does not commit the whole enemy base to an attack");
        TickFor(pacedMissionSimulation, 20.0f);
        Assert(pacedMissionSimulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary) ||
            pacedMissionSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Rifleman) > 0,
            "mission AI profile starts paced enemy production after delay");
        var committedAttackers = pacedMissionSimulation.Units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.CanAttack &&
            !unit.IsDestroyed &&
            unit.IsEnemyAttackCommitted);
        Assert(committedAttackers > 0, "mission AI commits a small attack group after the first delay");
        Assert(committedAttackers <= mission.EnemyAiProfile.AttackGroupSize, "mission AI does not commit the entire enemy base as one attack wave");
        Assert(pacedMissionSimulation.Units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.CanAttack &&
            !unit.IsDestroyed &&
            !unit.IsEnemyAttackCommitted), "mission AI leaves defenders at the enemy base");
    }
}
