using Godot;
using Stratezone.Simulation.Content;

public sealed class GodotGameDataReader : IGameDataReader
{
    private readonly string _root;

    public GodotGameDataReader(string root = "res://")
    {
        _root = root.EndsWith("://", StringComparison.Ordinal)
            ? root
            : root.TrimEnd('/', '\\');
    }

    public bool FileExists(string relativePath)
    {
        return Godot.FileAccess.FileExists(ToResourcePath(relativePath));
    }

    public string ReadAllText(string relativePath)
    {
        var resourcePath = ToResourcePath(relativePath);
        using var file = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            throw new FileNotFoundException($"Missing game data file '{resourcePath}'.");
        }

        return file.GetAsText();
    }

    public IReadOnlyList<string> EnumerateFiles(string relativeDirectory, string searchPattern)
    {
        var directoryPath = ToResourcePath(relativeDirectory);
        using var directory = DirAccess.Open(directoryPath);
        if (directory is null)
        {
            return [];
        }

        var suffix = searchPattern.StartsWith("*", StringComparison.Ordinal)
            ? searchPattern[1..]
            : searchPattern;
        var results = new List<string>();
        foreach (var fileName in directory.GetFiles())
        {
            if (suffix.Length > 0 && !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            results.Add($"{relativeDirectory.TrimEnd('/', '\\')}/{fileName}");
        }

        results.Sort(StringComparer.Ordinal);
        return results;
    }

    private string ToResourcePath(string relativePath)
    {
        var normalizedRelativePath = relativePath.TrimStart('/', '\\').Replace('\\', '/');
        return _root.EndsWith("://", StringComparison.Ordinal)
            ? $"{_root}{normalizedRelativePath}"
            : $"{_root}/{normalizedRelativePath}";
    }
}
