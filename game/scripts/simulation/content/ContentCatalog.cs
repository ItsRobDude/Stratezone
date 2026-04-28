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
                record.GetProperty("movement_speed").GetSingle(),
                record.GetProperty("health").GetInt32(),
                record.GetProperty("can_attack").GetBoolean(),
                record.GetProperty("can_construct").GetBoolean(),
                record.GetProperty("can_repair").GetBoolean(),
                record.GetProperty("can_run_over_infantry").GetBoolean()
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
                record.GetProperty("footprint_radius").GetSingle(),
                record.GetProperty("placement_buffer").GetSingle(),
                record.GetProperty("requires_power").GetBoolean(),
                record.GetProperty("provides_power").GetBoolean(),
                record.GetProperty("power_radius").GetSingle(),
                record.GetProperty("pylon_link_range").GetSingle(),
                record.GetProperty("provides_resource_extraction").GetBoolean(),
                GetOptionalString(record, "extractor_resource_id"),
                record.GetProperty("wall_anchor").GetBoolean()
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
            foreach (var resource in record.GetProperty("starting_resources").EnumerateArray())
            {
                if (resource.GetProperty("faction_id").GetString() != ContentIds.Factions.PlayerExpedition)
                {
                    continue;
                }

                startingResources[resource.GetProperty("resource_id").GetString() ?? string.Empty] =
                    resource.GetProperty("amount").GetInt32();
            }

            var wellIds = record.GetProperty("resource_wells")
                .EnumerateArray()
                .Select(well => well.GetString() ?? string.Empty)
                .Where(id => id.Length > 0)
                .ToArray();

            var mission = new MissionDefinition(
                record.GetProperty("id").GetString() ?? string.Empty,
                record.GetProperty("display_name").GetString() ?? string.Empty,
                startingResources,
                wellIds
            );

            missions.Add(mission.Id, mission);
        }

        return missions;
    }

    private static string? GetOptionalString(JsonElement record, string propertyName)
    {
        if (!record.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.GetString();
    }
}
