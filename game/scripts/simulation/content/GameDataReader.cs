namespace Stratezone.Simulation.Content;

public interface IGameDataReader
{
    bool FileExists(string relativePath);
    string ReadAllText(string relativePath);
    IReadOnlyList<string> EnumerateFiles(string relativeDirectory, string searchPattern);
}

public sealed class FileSystemGameDataReader : IGameDataReader
{
    private readonly string _root;

    public FileSystemGameDataReader(string gameRoot)
    {
        _root = Path.GetFullPath(gameRoot);
    }

    public bool FileExists(string relativePath)
    {
        return File.Exists(ToAbsolutePath(relativePath));
    }

    public string ReadAllText(string relativePath)
    {
        return File.ReadAllText(ToAbsolutePath(relativePath));
    }

    public IReadOnlyList<string> EnumerateFiles(string relativeDirectory, string searchPattern)
    {
        var directory = ToAbsolutePath(relativeDirectory);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, searchPattern)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/'))
            .ToArray();
    }

    private string ToAbsolutePath(string relativePath)
    {
        return Path.Combine(_root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
