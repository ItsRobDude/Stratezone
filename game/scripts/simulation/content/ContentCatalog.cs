using System.Text.Json;
using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed class ContentCatalog
{
    private readonly Dictionary<string, UnitDefinition> _units;
    private readonly Dictionary<string, BuildingDefinition> _buildings;
    private readonly Dictionary<string, ResourceWellDefinition> _resourceWells;
    private readonly Dictionary<string, MissionDefinition> _missions;

    private ContentCatalog(
        Dictionary<string, UnitDefinition> units,
        Dictionary<string, BuildingDefinition> buildings,
        Dictionary<string, ResourceWellDefinition> resourceWells,
        Dictionary<string, MissionDefinition> missions)
    {
        _units = units;
        _buildings = buildings;
        _resourceWells = resourceWells;
        _missions = missions;
    }

    public IReadOnlyDictionary<string, UnitDefinition> Units => _units;
    public IReadOnlyDictionary<string, BuildingDefinition> Buildings => _buildings;
    public IReadOnlyDictionary<string, ResourceWellDefinition> ResourceWells => _resourceWells;
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

    public ResourceWellDefinition GetResourceWell(string id)
    {
        return _resourceWells.TryGetValue(id, out var well)
            ? well
            : throw new KeyNotFoundException($"Unknown resource well id '{id}'.");
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
        var resourceWellsPath = Path.Combine(gameRoot, "data", "resources", "resource_wells.json");
        var firstLandingPath = Path.Combine(gameRoot, "data", "missions", "first_landing.json");

        var units = LoadUnits(unitsPath);
        var buildings = LoadBuildings(buildingsPath);
        var resourceWells = LoadResourceWells(resourceWellsPath);
        var missions = LoadMissions(firstLandingPath);
        return new ContentCatalog(units, buildings, resourceWells, missions);
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

    private static Dictionary<string, MissionDefinition> LoadMissions(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var records = document.RootElement.GetProperty("records");
        var missions = new Dictionary<string, MissionDefinition>(StringComparer.Ordinal);

        foreach (var record in records.EnumerateArray())
        {
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
            var enemyAiProfile = LoadEnemyAiProfile(record);

            var mission = new MissionDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                startingResources,
                enemyStartingResources,
                wellIds,
                markers,
                startingEntities,
                wellPlacements,
                enemyAiProfile
            );

            missions.Add(mission.Id, mission);
        }

        return missions;
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

    private static EnemyAiProfileDefinition LoadEnemyAiProfile(JsonElement record)
    {
        if (!record.TryGetProperty("enemy_ai_profile", out var profile) || profile.ValueKind != JsonValueKind.Object)
        {
            return EnemyAiProfileDefinition.Default;
        }

        return new EnemyAiProfileDefinition(
            profile.GetProperty("id").GetString() ?? EnemyAiProfileDefinition.Default.Id,
            GetOptionalFloat(profile, "first_attack_delay_seconds"),
            GetOptionalFloat(profile, "rebuild_cooldown_seconds"),
            GetOptionalFloat(profile, "production_cooldown_seconds"),
            profile.TryGetProperty("attack_group_size", out var attackGroupSize) ? attackGroupSize.GetInt32() : 1,
            GetOptionalFloat(profile, "central_well_interest"),
            profile.TryGetProperty("pressure_slowdown_multiplier", out var slowdown) ? slowdown.GetSingle() : 1.0f,
            profile.TryGetProperty("train_time_multiplier", out var trainTime) ? trainTime.GetSingle() : RtsSimulation.EnemyTrainTimeMultiplier,
            GetOptionalString(profile, "hub_marker") ?? EnemyAiProfileDefinition.Default.HubMarkerId,
            GetOptionalString(profile, "power_plant_marker") ?? EnemyAiProfileDefinition.Default.PowerPlantMarkerId,
            GetOptionalString(profile, "barracks_marker") ?? EnemyAiProfileDefinition.Default.BarracksMarkerId,
            GetOptionalString(profile, "extractor_marker") ?? EnemyAiProfileDefinition.Default.ExtractorMarkerId,
            GetOptionalString(profile, "defense_tower_marker") ?? EnemyAiProfileDefinition.Default.DefenseTowerMarkerId);
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
