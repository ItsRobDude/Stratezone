using System.Text.Json;

namespace Stratezone.Localization;

public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, string> _strings;

    private LocalizationCatalog(Dictionary<string, string> strings)
    {
        _strings = strings;
    }

    public static LocalizationCatalog LoadFromGameData(string gameRoot, string locale = "en")
    {
        var path = Path.Combine(gameRoot, "data", "i18n", $"{locale}.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement.TryGetProperty("strings", out var stringsElement)
            ? stringsElement
            : document.RootElement;
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var item in root.EnumerateObject())
        {
            if (item.Value.ValueKind == JsonValueKind.String)
            {
                strings[item.Name] = item.Value.GetString() ?? string.Empty;
            }
        }

        return new LocalizationCatalog(strings);
    }

    public string Translate(string key, IReadOnlyDictionary<string, string>? args = null, string? fallback = null)
    {
        if (!_strings.TryGetValue(key, out var template))
        {
            return fallback is null ? $"[[{key}]]" : fallback;
        }

        if (args is null)
        {
            return template;
        }

        foreach (var arg in args)
        {
            template = template.Replace($"{{{arg.Key}}}", arg.Value, StringComparison.Ordinal);
        }

        return template;
    }

    public string ContentName(string contentId, string? fallback = null)
    {
        return Translate(ContentNameKey(contentId), null, fallback);
    }

    public static string ContentNameKey(string contentId)
    {
        var prefix = contentId.Split('_', 2)[0];
        return $"{prefix}.{contentId}.name";
    }
}
