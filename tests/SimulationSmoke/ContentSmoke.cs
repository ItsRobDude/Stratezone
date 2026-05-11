using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class ContentSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var catalog = context.Catalog;
        var localization = context.Localization;
        var mission = context.FirstLandingMission;
        var ridgeMission = context.WellsAtTheRidgeMission;
        var ridgeMap = context.WellsAtTheRidgeMap;

        Assert(localization.Translate("ui.hud.build_line").Contains("Build:", StringComparison.Ordinal), "English localization catalog loads HUD strings");
        Assert(localization.Translate("missing.test.key") == "[[missing.test.key]]", "missing localization keys are obvious");
        Assert(localization.ContentName(ContentIds.Units.Grunt) == "Grunt", "content name localization keys resolve stable content ids");
        Assert(localization.ContentShortName(ContentIds.Buildings.ExtractorRefinery) == "Extractor", "content short-name localization keys resolve compact UI labels");

        var cadetDefinition = catalog.GetUnit(ContentIds.Units.Cadet);
        var riflemanDefinition = catalog.GetUnit(ContentIds.Units.Rifleman);
        var guardianDefinition = catalog.GetUnit(ContentIds.Units.Guardian);
        var mediumTankDefinition = catalog.GetUnit(ContentIds.Units.MediumTank);
        var tankDefinition = catalog.GetUnit(ContentIds.Units.Tank);
        var armoryAnnexDefinition = catalog.GetBuilding(ContentIds.Buildings.ArmoryAnnex);
        var gunTowerDefinition = catalog.GetBuilding(ContentIds.Buildings.GunTower);
        var rocketTowerDefinition = catalog.GetBuilding(ContentIds.Buildings.RocketTower);

        Assert(cadetDefinition.Cost < riflemanDefinition.Cost, "Cadet costs less than Rifleman");
        Assert(cadetDefinition.Health < riflemanDefinition.Health, "Cadet has less health than Rifleman");
        Assert(cadetDefinition.AttackDamage < riflemanDefinition.AttackDamage, "Cadet deals less damage than Rifleman");
        Assert(cadetDefinition.TrainTimeSeconds == 3.0f, "Cadet recruits in only a few seconds");
        Assert(riflemanDefinition.TrainTimeSeconds == 4.0f, "Rifleman recruits only slightly slower than Cadet");
        Assert(guardianDefinition.TrainTimeSeconds == 9.0f, "Guardian trains slower as a specialist");
        Assert(catalog.GetUnit(ContentIds.Units.Grunt).TrainTimeSeconds > guardianDefinition.TrainTimeSeconds, "Grunt stays slow and expensive compared with infantry");
        Assert(guardianDefinition.Role == "anti_armor_infantry", "Guardian content role is the anti-armor infantry proof role");
        Assert(guardianDefinition.RequiredAddonBuildingId is null, "Guardian no longer uses the legacy Armory Annex training gate");
        Assert(guardianDefinition.RequiredBarracksUpgradeId == ContentIds.BarracksUpgrades.GuardianRetrofit, "Guardian training requires the Barracks Guardian Retrofit");
        Assert(!armoryAnnexDefinition.TrainingUnlockUnitIds.Contains(ContentIds.Units.Guardian), "Armory Annex no longer declares the Guardian unlock");
        Assert(guardianDefinition.AttackDamage < riflemanDefinition.AttackDamage, "Guardian keeps lower raw damage than Rifleman");
        Assert(DamagePerSecondAgainst(guardianDefinition, riflemanDefinition) < DamagePerSecondAgainst(riflemanDefinition, riflemanDefinition), "Guardian is not a better anti-infantry Rifleman");
        Assert(DamagePerSecondAgainst(guardianDefinition, mediumTankDefinition) > DamagePerSecondAgainst(riflemanDefinition, mediumTankDefinition) * 2.0f, "Guardian energy fire outperforms Rifleman ballistics against Medium Tanks");
        Assert(DamagePerSecondAgainst(guardianDefinition, tankDefinition) > DamagePerSecondAgainst(riflemanDefinition, tankDefinition) * 2.25f, "Guardian energy fire outperforms Rifleman ballistics against Heavy Tanks");
        Assert(DamagePerSecondAgainst(mediumTankDefinition, mediumTankDefinition) > DamagePerSecondAgainst(riflemanDefinition, mediumTankDefinition) * 1.5f, "released Medium Tanks are a better anti-armor answer than Riflemen");
        Assert(BuildingDamagePerSecondAgainst(rocketTowerDefinition, mediumTankDefinition) > BuildingDamagePerSecondAgainst(gunTowerDefinition, mediumTankDefinition) * 4.0f, "Rocket Tower explosives outperform Gun Tower ballistics against Medium Tanks");
        Assert(tankDefinition.DisplayName == "Heavy Tank", "existing Tank record is promoted to Heavy Tank");
        Assert(mediumTankDefinition.Health < tankDefinition.Health, "Medium Tank is less durable than Heavy Tank");
        Assert(mediumTankDefinition.AttackDamage < tankDefinition.AttackDamage, "Medium Tank deals less damage than Heavy Tank");
        Assert(mediumTankDefinition.AreaRadius < tankDefinition.AreaRadius, "Medium Tank has smaller splash than Heavy Tank");
        Assert(Math.Abs(riflemanDefinition.Health - (mediumTankDefinition.AttackDamage * 1.1f) - (riflemanDefinition.Health * 0.3f)) < 1.5f, "Medium Tank shot leaves a Rifleman near 30 percent health");
        Assert(tankDefinition.AttackCooldown > 3.0f, "Heavy Tank cannon fires slowly enough to read as a heavy burst weapon");

        Assert(mission.AvailableUnitIds.Contains(ContentIds.Units.Grunt) && mission.AvailableUnitIds.Contains(ContentIds.Units.Cadet) && mission.AvailableUnitIds.Contains(ContentIds.Units.Rifleman), "mission data exposes Level 1 trainable units");
        Assert(!mission.AvailableUnitIds.Contains(ContentIds.Units.Guardian) && !mission.AvailableUnitIds.Contains(ContentIds.Units.Rover) && !mission.AvailableUnitIds.Contains(ContentIds.Units.Commander), "mission data hides Level 1 scenario-only units from training");
        Assert(mission.StartingEntities.Count(entity => entity.FactionId == ContentIds.Factions.PlayerExpedition && entity.ContentId == ContentIds.Units.Guardian) == 1, "Level 1 starts the player with one Guardian");
        Assert(mission.StartingEntities.Count(entity => entity.FactionId == ContentIds.Factions.PlayerExpedition && entity.ContentId == ContentIds.Units.Commander) == 1, "Level 1 starts the player with one Commander");
        Assert(mission.StartingEntities.Count(entity => entity.FactionId == ContentIds.Factions.PlayerExpedition && entity.ContentId == ContentIds.Units.Grunt) == 1, "Level 1 starts the player with one Grunt");
        Assert(mission.StartingEntities.Count(entity => entity.FactionId == ContentIds.Factions.PlayerExpedition && entity.ContentId == ContentIds.Units.Rover) == 1, "Level 1 starts the player with one provided Rover");
        Assert(!mission.StartingEntities.Any(entity => entity.FactionId == ContentIds.Factions.PlayerExpedition && entity.ContentId == ContentIds.Units.Rifleman), "Level 1 does not start the player with extra Riflemen");
        Assert(mission.Presentation.BriefingTitleKey == "mission.mission_first_landing.briefing_title", "Level 1 has localized briefing title data");

        Assert(catalog.Missions.Count >= 2, "catalog loads more than one playable mission file");
        Assert(ridgeMission.MapId == "map_wells_at_the_ridge_greybox", "Level 2 mission data points at the ridge greybox map");
        Assert(ridgeMission.AvailableUnitIds.Contains(ContentIds.Units.Guardian), "Level 2 exposes Guardian training behind the Barracks retrofit");
        Assert(ridgeMission.AvailableBuildingIds.Contains(ContentIds.Buildings.ColonyHub), "Level 2 exposes Colony Hub placement");
        Assert(!ridgeMission.StartingEntities.Any(entity =>
            entity.FactionId == ContentIds.Factions.PlayerExpedition &&
            entity.ContentId == ContentIds.Buildings.ColonyHub), "Level 2 does not start with the player base already built");
        Assert(ridgeMission.StartingEntities.Count(entity =>
            entity.FactionId == ContentIds.Factions.PlayerExpedition &&
            entity.ContentId == ContentIds.Units.Cadet) == 2, "Level 2 starts with two Cadets in the landing party");
        Assert(ridgeMission.StartingEntities.Count(entity =>
            entity.FactionId == ContentIds.Factions.PlayerExpedition &&
            entity.ContentId == ContentIds.Units.Rifleman) == 1, "Level 2 starts with one Rifleman escort");
        Assert(ridgeMission.ObjectiveIds.Contains(ContentIds.Objectives.DestroyEnemyColonyHub), "Level 2 wins through the enemy Colony Hub objective rather than well control");
        Assert(ridgeMission.Presentation.StartObjectiveKey == "mission.mission_wells_at_the_ridge.start_objective", "Level 2 owns localized start objective data");
        Assert(ridgeMission.MissionTriggers.Count == 1, "Level 2 declares one coalesced base-building pressure trigger");
        Assert(ridgeMission.MissionTriggers[0].WatchedPlayerBuildingIds.Contains(ContentIds.Buildings.Barracks), "Level 2 pressure trigger watches Barracks construction");
        Assert(ridgeMission.MissionTriggers[0].WatchedPlayerBuildingIds.Contains(ContentIds.Buildings.ExtractorRefinery), "Level 2 pressure trigger coalesces first Extractor construction");
        Assert(ridgeMission.Markers.Count >= 15, "Level 2 exposes enough authored mission markers for map editor/tuner route work");
        Assert(ridgeMission.Markers.Any(marker => marker.Id == "enemy_wall_power_pylon"), "Level 2 has a stable wall-power Pylon marker for map editor/tuner work");
        Assert(ridgeMission.Markers.Any(marker => marker.Id == "enemy_defense") && ridgeMission.Markers.Any(marker => marker.Id == "enemy_defense_south"), "Level 2 has stable Defense Tower markers for wall-link tuning");
        Assert(ridgeMap.TerrainRegions.Any(region => region.BlocksMovement && region.BlocksBuilding), "Level 2 map has blocked terrain regions");
        Assert(!ridgeMap.RequiresBuildableRegions, "Level 2 keeps normal RTS build-anywhere rules outside blocked terrain");
        Assert(ridgeMap.TerrainRegions.Count(region => region.AllowsBuilding) >= 3, "Level 2 map marks practical base areas and resource basins without whitelisting construction");
        Assert(ridgeMap.TerrainRegions.Any(region => region.RegionType == "chokepoint_marker"), "Level 2 map marks chokepoint candidates");
        Assert(ridgeMap.TerrainRegions.All(region => region.Id.Length > 0 && region.Shape.Length > 0), "Level 2 terrain regions have stable IDs and shapes for editor/tuner snippet export");
        Assert(ridgeMap.TerrainRegions.Any(region => region.Id == "central_power_corridor"), "Level 2 map keeps an explicit power-corridor region for Pylon route tuning");
    }
}
