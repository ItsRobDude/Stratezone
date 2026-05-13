using System.Text.Json;
using Stratezone.Simulation.Content;

namespace Stratezone.Localization;

public sealed record LocalizationLoadResult(LocalizationCatalog Catalog, IReadOnlyList<string> Warnings);

public sealed class LocalizationCatalog
{
    private readonly Dictionary<string, string> _strings;

    private LocalizationCatalog(Dictionary<string, string> strings)
    {
        _strings = strings;
    }

    public static LocalizationLoadResult LoadFromGameData(string gameRoot, string locale = "en")
    {
        return LoadFromGameData(new FileSystemGameDataReader(gameRoot), locale);
    }

    public static LocalizationLoadResult LoadFromGameData(IGameDataReader reader, string locale = "en")
    {
        var warnings = new List<string>();
        return LoadInternal(reader, locale, warnings, new HashSet<string>(StringComparer.Ordinal));
    }

    private static LocalizationLoadResult LoadInternal(
        IGameDataReader reader,
        string locale,
        List<string> warnings,
        HashSet<string> attemptedLocales)
    {
        if (!attemptedLocales.Add(locale))
        {
            throw new InvalidOperationException($"Locale fallback loop while loading '{locale}'.");
        }

        var path = $"data/i18n/{locale}.json";
        if (!reader.FileExists(path))
        {
            if (locale == "en")
            {
                throw new FileNotFoundException($"Required English locale missing at {path}.");
            }

            warnings.Add($"Locale '{locale}' missing at {path}; falling back to 'en'.");
            return LoadInternal(reader, "en", warnings, attemptedLocales);
        }

        using var document = JsonDocument.Parse(reader.ReadAllText(path));
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

        return new LocalizationLoadResult(new LocalizationCatalog(strings), warnings);
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

    public string ContentShortName(string contentId, string? fallback = null)
    {
        return Translate(ContentShortNameKey(contentId), null, fallback ?? ContentName(contentId));
    }

    public static string ContentNameKey(string contentId)
    {
        var prefix = contentId.Split('_', 2)[0];
        return $"{prefix}.{contentId}.name";
    }

    public static string ContentShortNameKey(string contentId)
    {
        var prefix = contentId.Split('_', 2)[0];
        return $"{prefix}.{contentId}.short_name";
    }
}
