using System.Text.Json;

namespace Stratezone.Simulation.Content;

public sealed class ContentCatalog
{
    private readonly Dictionary<string, UnitDefinition> _units;

    private ContentCatalog(Dictionary<string, UnitDefinition> units)
    {
        _units = units;
    }

    public IReadOnlyDictionary<string, UnitDefinition> Units => _units;

    public UnitDefinition GetUnit(string id)
    {
        return _units.TryGetValue(id, out var unit)
            ? unit
            : throw new KeyNotFoundException($"Unknown unit id '{id}'.");
    }

    public static ContentCatalog LoadFromGameData(string gameRoot)
    {
        var unitsPath = Path.Combine(gameRoot, "data", "units", "units.json");
        var units = LoadUnits(unitsPath);
        return new ContentCatalog(units);
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
}

