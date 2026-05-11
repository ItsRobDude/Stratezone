using System.Text.Json;
using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed class ContentCatalog
{
    private readonly Dictionary<string, UnitDefinition> _units;
    private readonly Dictionary<string, BuildingDefinition> _buildings;
    private readonly Dictionary<string, BarracksUpgradeDefinition> _barracksUpgrades;
    private readonly Dictionary<string, ResourceWellDefinition> _resourceWells;
    private readonly Dictionary<string, MapDefinition> _maps;
    private readonly Dictionary<string, MissionDefinition> _missions;

    private ContentCatalog(
        Dictionary<string, UnitDefinition> units,
        Dictionary<string, BuildingDefinition> buildings,
        Dictionary<string, BarracksUpgradeDefinition> barracksUpgrades,
        Dictionary<string, ResourceWellDefinition> resourceWells,
        Dictionary<string, MapDefinition> maps,
        Dictionary<string, MissionDefinition> missions)
    {
        _units = units;
        _buildings = buildings;
        _barracksUpgrades = barracksUpgrades;
        _resourceWells = resourceWells;
        _maps = maps;
        _missions = missions;
    }

    public IReadOnlyDictionary<string, UnitDefinition> Units => _units;
    public IReadOnlyDictionary<string, BuildingDefinition> Buildings => _buildings;
    public IReadOnlyDictionary<string, BarracksUpgradeDefinition> BarracksUpgrades => _barracksUpgrades;
    public IReadOnlyDictionary<string, ResourceWellDefinition> ResourceWells => _resourceWells;
    public IReadOnlyDictionary<string, MapDefinition> Maps => _maps;
    public IReadOnlyDictionary<string, MissionDefinition> Missions => _missions;

    public UnitDefinition GetUnit(string id)
    {
        return _units.TryGetValue(id, out var unit)
            ? unit
            : throw new KeyNotFoundException($"Unknown unit id '{id}'.");
    }

    public BuildingDefinition GetBuilding(string id)
    {
        return _buildings.TryGetValue(id, out var building)
            ? building
            : throw new KeyNotFoundException($"Unknown building id '{id}'.");
    }

    public BarracksUpgradeDefinition GetBarracksUpgrade(string id)
    {
        return _barracksUpgrades.TryGetValue(id, out var upgrade)
            ? upgrade
            : throw new KeyNotFoundException($"Unknown Barracks upgrade id '{id}'.");
    }

    public ResourceWellDefinition GetResourceWell(string id)
    {
        return _resourceWells.TryGetValue(id, out var well)
            ? well
            : throw new KeyNotFoundException($"Unknown resource well id '{id}'.");
    }

    public MapDefinition GetMap(string id)
    {
        return _maps.TryGetValue(id, out var map)
            ? map
            : throw new KeyNotFoundException($"Unknown map id '{id}'.");
    }

    public MissionDefinition GetMission(string id)
    {
        return _missions.TryGetValue(id, out var mission)
            ? mission
            : throw new KeyNotFoundException($"Unknown mission id '{id}'.");
    }

    public static ContentCatalog LoadFromGameData(string gameRoot)
    {
        var unitsPath = Path.Combine(gameRoot, "data", "units", "units.json");
        var buildingsPath = Path.Combine(gameRoot, "data", "buildings", "buildings.json");
        var barracksUpgradesPath = Path.Combine(gameRoot, "data", "upgrades", "barracks_upgrades.json");
        var resourceWellsPath = Path.Combine(gameRoot, "data", "resources", "resource_wells.json");
        var mapsPath = Path.Combine(gameRoot, "data", "maps", "maps.json");
        var missionsPath = Path.Combine(gameRoot, "data", "missions");

        var units = LoadUnits(unitsPath);
        var buildings = LoadBuildings(buildingsPath);
        var barracksUpgrades = LoadBarracksUpgrades(barracksUpgradesPath);
        var resourceWells = LoadResourceWells(resourceWellsPath);
        var maps = LoadMaps(mapsPath);
        var missions = LoadMissions(missionsPath);
        return new ContentCatalog(units, buildings, barracksUpgrades, resourceWells, maps, missions);
    }

    private static Dictionary<string, UnitDefinition> LoadUnits(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var units = new Dictionary<string, UnitDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
            var unit = new UnitDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                record.GetProperty("role").GetString() ?? string.Empty,
                record.GetProperty("cost").GetInt32(),
                record.GetProperty("train_time_seconds").GetSingle(),
                LoadResistances(record),
                record.GetProperty("movement_speed").GetSingle(),
                record.GetProperty("sight_range").GetSingle(),
                GetOptionalTrainRequirement(record, "allowed_by_building_id"),
                GetOptionalTrainRequirement(record, "required_addon_building_id"),
                GetOptionalTrainRequirement(record, "required_barracks_upgrade_id"),
                GetOptionalTrainRequirement(record, "spawn_building_id"),
                record.GetProperty("health").GetInt32(),
                record.GetProperty("attack_damage").GetSingle(),
                record.GetProperty("attack_range").GetSingle(),
                record.GetProperty("attack_cooldown").GetSingle(),
                record.GetProperty("damage_type").GetString() ?? "none",
                record.GetProperty("area_radius").GetSingle(),
                record.GetProperty("friendly_fire").GetBoolean(),
                LoadStringArray(record, "target_filters"),
                record.GetProperty("can_attack").GetBoolean(),
                record.GetProperty("can_construct").GetBoolean(),
                record.GetProperty("can_repair").GetBoolean(),
                record.GetProperty("can_run_over_infantry").GetBoolean(),
                GetOptionalFloat(record, "run_over_damage"),
                GetOptionalString(record, "run_over_damage_type") ?? "crush",
                LoadStringArray(record, "tags")
            );

            units.Add(unit.Id, unit);
        }

        return units;
    }

    private static Dictionary<string, BuildingDefinition> LoadBuildings(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var buildings = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
            var building = new BuildingDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                record.GetProperty("role").GetString() ?? string.Empty,
                record.GetProperty("cost").GetInt32(),
                record.GetProperty("health").GetInt32(),
                LoadResistances(record),
                record.GetProperty("footprint_radius").GetSingle(),
                record.GetProperty("placement_buffer").GetSingle(),
                record.GetProperty("requires_power").GetBoolean(),
                record.GetProperty("provides_power").GetBoolean(),
                record.GetProperty("power_radius").GetSingle(),
                record.GetProperty("pylon_link_range").GetSingle(),
                record.GetProperty("provides_resource_extraction").GetBoolean(),
                GetOptionalString(record, "extractor_resource_id"),
                GetOptionalString(record, "requires_adjacent_building_id"),
                LoadStringArray(record, "training_unlock_unit_ids"),
                record.GetProperty("wall_anchor").GetBoolean(),
                GetOptionalFloat(record, "wall_link_range"),
                record.GetProperty("attack_damage").GetSingle(),
                record.GetProperty("attack_range").GetSingle(),
                record.GetProperty("attack_cooldown").GetSingle(),
                record.GetProperty("damage_type").GetString() ?? "none",
                record.GetProperty("area_radius").GetSingle(),
                record.GetProperty("friendly_fire").GetBoolean(),
                LoadStringArray(record, "target_filters"),
                GetOptionalString(record, "upgrade_from_building_id"),
                GetOptionalBool(record, "upgrade_preserves_wall_anchor"),
                GetOptionalFloat(record, "sight_range"),
                LoadStringArray(record, "tags")
            );

            buildings.Add(building.Id, building);
        }

        return buildings;
    }

    private static Dictionary<string, BarracksUpgradeDefinition> LoadBarracksUpgrades(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var upgrades = new Dictionary<string, BarracksUpgradeDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
            var upgrade = new BarracksUpgradeDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                record.GetProperty("cost").GetInt32(),
                record.GetProperty("duration_seconds").GetSingle(),
                record.GetProperty("required_grunt_count").GetInt32(),
                record.GetProperty("required_grunt_range").GetSingle(),
                record.GetProperty("requires_powered_barracks").GetBoolean(),
                record.GetProperty("requires_colony_hub").GetBoolean(),
                LoadStringArray(record, "unlock_unit_ids"),
                LoadStringArray(record, "tags"));

            upgrades.Add(upgrade.Id, upgrade);
        }

        return upgrades;
    }

    private static Dictionary<string, ResourceWellDefinition> LoadResourceWells(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var wells = new Dictionary<string, ResourceWellDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
            var well = new ResourceWellDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("resource_id").GetString() ?? string.Empty,
                record.GetProperty("capacity").GetSingle(),
                record.GetProperty("extraction_rate").GetSingle(),
                record.GetProperty("depletes").GetBoolean()
            );

            wells.Add(well.Id, well);
        }

        return wells;
    }

    private static Dictionary<string, MissionDefinition> LoadMissions(string missionsPath)
    {
        var missions = new Dictionary<string, MissionDefinition>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(missionsPath, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            LoadMissionFile(path, missions);
        }

        return missions;
    }

    private static Dictionary<string, MapDefinition> LoadMaps(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var maps = new Dictionary<string, MapDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
            var map = new MapDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                record.GetProperty("biome").GetString() ?? string.Empty,
                record.GetProperty("target_size").GetString() ?? string.Empty,
                record.TryGetProperty("requires_buildable_regions", out var requiresBuildableRegions) && requiresBuildableRegions.GetBoolean(),
                LoadStringArray(record, "required_features"),
                LoadMapRegions(record),
                LoadStringArray(record, "tags"));

            maps.Add(map.Id, map);
        }

        return maps;
    }

    private static IReadOnlyList<MapRegionDefinition> LoadMapRegions(JsonElement record)
    {
        if (!record.TryGetProperty("terrain_regions", out var regions) || regions.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return regions.EnumerateArray()
            .Select(region => new MapRegionDefinition(
                region.GetProperty("id").GetString() ?? string.Empty,
                region.GetProperty("region_type").GetString() ?? string.Empty,
                region.GetProperty("shape").GetString() ?? string.Empty,
                LoadVector(region.GetProperty("center")),
                region.TryGetProperty("size", out var size) ? LoadVector(size) : new SimVector2(0, 0),
                GetOptionalFloat(region, "radius"),
                region.TryGetProperty("blocks_movement", out var blocksMovement) && blocksMovement.GetBoolean(),
                region.TryGetProperty("blocks_building", out var blocksBuilding) && blocksBuilding.GetBoolean(),
                region.TryGetProperty("allows_building", out var allowsBuilding) && allowsBuilding.GetBoolean(),
                LoadStringArray(region, "tags")))
            .Where(region => region.Id.Length > 0)
            .ToArray();
    }

    private static void LoadMissionFile(string path, Dictionary<string, MissionDefinition> missions)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");

        foreach (var record in records.EnumerateArray())
        {
            var id = record.GetProperty("id").GetString() ?? string.Empty;
            if (!IsPlayableMissionRecord(id, record))
            {
                continue;
            }

            var startingResources = new Dictionary<string, int>(StringComparer.Ordinal);
            var enemyStartingResources = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var resource in record.GetProperty("starting_resources").EnumerateArray())
            {
                var factionId = resource.GetProperty("faction_id").GetString() ?? string.Empty;
                var resourceId = resource.GetProperty("resource_id").GetString() ?? string.Empty;
                var amount = resource.GetProperty("amount").GetInt32();

                if (factionId == ContentIds.Factions.PlayerExpedition)
                {
                    startingResources[resourceId] = amount;
                }
                else if (factionId == ContentIds.Factions.PrivateMilitary)
                {
                    enemyStartingResources[resourceId] = amount;
                }
            }

            var wellIds = record.GetProperty("resource_wells")
                .EnumerateArray()
                .Select(well => well.GetString() ?? string.Empty)
                .Where(id => id.Length > 0)
                .ToArray();
            var markers = LoadMissionMarkers(record);
            var startingEntities = LoadMissionStartingEntities(record);
            var wellPlacements = LoadMissionResourceWellPlacements(record);
            var availableUnitIds = LoadStringArray(record, "available_unit_ids");
            var availableBuildingIds = LoadStringArray(record, "available_building_ids");
            var objectiveIds = LoadStringArray(record, "objectives");
            var failureConditionIds = LoadStringArray(record, "failure_conditions");
            var presentation = LoadMissionPresentation(record);
            var missionTriggers = LoadMissionTriggers(record);
            var enemyAiProfile = LoadEnemyAiProfile(record);

            var mission = new MissionDefinition(
                id,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                record.GetProperty("map_id").GetString() ?? string.Empty,
                startingResources,
                enemyStartingResources,
                wellIds,
                markers,
                startingEntities,
                wellPlacements,
                availableUnitIds,
                availableBuildingIds,
                objectiveIds,
                failureConditionIds,
                presentation,
                missionTriggers,
                enemyAiProfile
            );

            missions.Add(mission.Id, mission);
        }
    }

    private static bool IsPlayableMissionRecord(string id, JsonElement record)
    {
        return id.StartsWith("mission_", StringComparison.Ordinal) &&
            record.TryGetProperty("starting_resources", out _) &&
            record.TryGetProperty("resource_wells", out _) &&
            record.TryGetProperty("available_unit_ids", out _) &&
            record.TryGetProperty("available_building_ids", out _);
    }

    private static IReadOnlyList<MissionMarkerDefinition> LoadMissionMarkers(JsonElement record)
    {
        if (!record.TryGetProperty("mission_markers", out var markers) || markers.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return markers.EnumerateArray()
            .Select(marker => new MissionMarkerDefinition(
                marker.GetProperty("id").GetString() ?? string.Empty,
                LoadVector(marker.GetProperty("position"))))
            .Where(marker => marker.Id.Length > 0)
            .ToArray();
    }

    private static IReadOnlyList<MissionStartingEntityDefinition> LoadMissionStartingEntities(JsonElement record)
    {
        if (!record.TryGetProperty("starting_entities", out var entities) || entities.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return entities.EnumerateArray()
            .Select(entity => new MissionStartingEntityDefinition(
                entity.GetProperty("content_id").GetString() ?? string.Empty,
                entity.GetProperty("faction_id").GetString() ?? string.Empty,
                entity.GetProperty("marker").GetString() ?? string.Empty,
                entity.TryGetProperty("offset", out var offset) ? LoadVector(offset) : new SimVector2(0, 0)))
            .Where(entity => entity.ContentId.Length > 0 && entity.FactionId.Length > 0 && entity.MarkerId.Length > 0)
            .ToArray();
    }

    private static IReadOnlyList<MissionResourceWellPlacementDefinition> LoadMissionResourceWellPlacements(JsonElement record)
    {
        if (!record.TryGetProperty("resource_well_placements", out var placements) || placements.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return placements.EnumerateArray()
            .Select(placement => new MissionResourceWellPlacementDefinition(
                placement.GetProperty("well_id").GetString() ?? string.Empty,
                placement.GetProperty("marker").GetString() ?? string.Empty,
                placement.TryGetProperty("offset", out var offset) ? LoadVector(offset) : new SimVector2(0, 0)))
            .Where(placement => placement.WellId.Length > 0 && placement.MarkerId.Length > 0)
            .ToArray();
    }

    private static MissionPresentationDefinition LoadMissionPresentation(JsonElement record)
    {
        if (!record.TryGetProperty("presentation", out var presentation) || presentation.ValueKind != JsonValueKind.Object)
        {
            return new MissionPresentationDefinition(
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        return new MissionPresentationDefinition(
            GetOptionalString(presentation, "briefing_title_key") ?? string.Empty,
            GetOptionalString(presentation, "briefing_body_key") ?? string.Empty,
            GetOptionalString(presentation, "start_objective_key") ?? string.Empty,
            GetOptionalString(presentation, "success_key") ?? string.Empty,
            GetOptionalString(presentation, "failure_key") ?? string.Empty);
    }

    private static IReadOnlyList<MissionTriggerDefinition> LoadMissionTriggers(JsonElement record)
    {
        if (!record.TryGetProperty("mission_triggers", out var triggers) || triggers.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return triggers.EnumerateArray()
            .Select(trigger => new MissionTriggerDefinition(
                trigger.GetProperty("id").GetString() ?? string.Empty,
                LoadStringArray(trigger, "watched_player_building_ids"),
                GetOptionalFloat(trigger, "min_elapsed_seconds"),
                GetOptionalFloat(trigger, "coalesce_window_seconds"),
                GetOptionalFloat(trigger, "cooldown_seconds"),
                trigger.TryGetProperty("max_fire_count", out var maxFireCount) ? maxFireCount.GetInt32() : 1,
                trigger.TryGetProperty("enemy_attack_group_size", out var enemyAttackGroupSize) ? enemyAttackGroupSize.GetInt32() : 1))
            .Where(trigger => trigger.Id.Length > 0 && trigger.WatchedPlayerBuildingIds.Count > 0)
            .ToArray();
    }

    private static EnemyAiProfileDefinition LoadEnemyAiProfile(JsonElement record)
    {
        if (!record.TryGetProperty("enemy_ai_profile", out var profile) || profile.ValueKind != JsonValueKind.Object)
        {
            return EnemyAiProfileDefinition.Default;
        }

        return new EnemyAiProfileDefinition(
            profile.GetProperty("id").GetString() ?? EnemyAiProfileDefinition.Default.Id,
            GetOptionalFloat(profile, "first_rebuild_delay_seconds"),
            GetOptionalFloat(profile, "first_central_well_rebuild_delay_seconds"),
            GetOptionalFloat(profile, "first_attack_delay_seconds"),
            GetOptionalFloat(profile, "first_patrol_delay_seconds"),
            GetOptionalFloat(profile, "patrol_interval_seconds"),
            GetOptionalFloat(profile, "rebuild_cooldown_seconds"),
            GetOptionalFloat(profile, "production_cooldown_seconds"),
            profile.TryGetProperty("attack_group_size", out var attackGroupSize) ? attackGroupSize.GetInt32() : 1,
            profile.TryGetProperty("patrol_group_size", out var patrolGroupSize) ? patrolGroupSize.GetInt32() : 1,
            profile.TryGetProperty("max_patrol_dispatches", out var maxPatrolDispatches) ? maxPatrolDispatches.GetInt32() : 0,
            GetOptionalFloat(profile, "central_well_interest"),
            GetOptionalFloat(profile, "central_well_rebuild_cooldown_seconds"),
            GetOptionalInt(profile, "max_central_well_rebuilds", int.MaxValue),
            profile.TryGetProperty("pressure_slowdown_multiplier", out var slowdown) ? slowdown.GetSingle() : 1.0f,
            profile.TryGetProperty("train_time_multiplier", out var trainTime) ? trainTime.GetSingle() : RtsSimulation.EnemyTrainTimeMultiplier,
            GetOptionalString(profile, "hub_marker") ?? EnemyAiProfileDefinition.Default.HubMarkerId,
            GetOptionalString(profile, "power_plant_marker") ?? EnemyAiProfileDefinition.Default.PowerPlantMarkerId,
            GetOptionalString(profile, "barracks_marker") ?? EnemyAiProfileDefinition.Default.BarracksMarkerId,
            GetOptionalString(profile, "extractor_marker") ?? EnemyAiProfileDefinition.Default.ExtractorMarkerId,
            GetOptionalString(profile, "defense_tower_marker") ?? EnemyAiProfileDefinition.Default.DefenseTowerMarkerId,
            GetOptionalString(profile, "rally_marker") ?? EnemyAiProfileDefinition.Default.RallyMarkerId,
            LoadStringArray(profile, "patrol_markers"));
    }

    private static SimVector2 LoadVector(JsonElement record)
    {
        return new SimVector2(
            record.GetProperty("x").GetSingle(),
            record.GetProperty("y").GetSingle());
    }

    private static string? GetOptionalString(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }

    private static string? GetOptionalTrainRequirement(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty("train_requirements", out var requirements) ||
            requirements.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return GetOptionalString(requirements, propertyName);
    }

    private static float GetOptionalFloat(JsonElement record, string propertyName)
    {
        return record.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetSingle()
            : 0.0f;
    }

    private static int GetOptionalInt(JsonElement record, string propertyName, int fallback)
    {
        return record.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : fallback;
    }

    private static bool GetOptionalBool(JsonElement record, string propertyName)
    {
        return record.TryGetProperty(propertyName, out var value) &&
            value.ValueKind == JsonValueKind.True;
    }

    private static IReadOnlyList<string> LoadStringArray(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToArray();
    }

    private static IReadOnlyDictionary<string, float> LoadResistances(JsonElement record)
    {
        var resistances = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var resistance in record.GetProperty("damage_resistances").EnumerateObject())
        {
            resistances[resistance.Name] = resistance.Value.GetSingle();
        }

        return resistances;
    }
}
