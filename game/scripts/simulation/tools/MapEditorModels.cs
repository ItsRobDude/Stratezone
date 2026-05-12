using Stratezone.Simulation;

namespace Stratezone.Simulation.Tools;

public sealed class MapEditorMarker
{
    private readonly List<string> _contentIds = [];

    public MapEditorMarker(string id, SimVector2 position)
    {
        Id = id;
        Position = position;
    }

    public string Id { get; }
    public SimVector2 Position { get; set; }
    public IReadOnlyList<string> ContentIds => _contentIds;

    internal void AddContentId(string contentId)
    {
        if (!_contentIds.Contains(contentId, StringComparer.Ordinal))
        {
            _contentIds.Add(contentId);
        }
    }
}

public sealed class MapEditorRegion
{
    public MapEditorRegion(
        string id,
        string regionType,
        string shape,
        SimVector2 center,
        SimVector2 size,
        float radius,
        bool blocksMovement,
        bool blocksBuilding,
        bool allowsBuilding,
        IReadOnlyList<string> tags)
    {
        Id = id;
        RegionType = regionType;
        Shape = shape;
        Center = center;
        Size = size;
        Radius = radius;
        BlocksMovement = blocksMovement;
        BlocksBuilding = blocksBuilding;
        AllowsBuilding = allowsBuilding;
        Tags = tags;
    }

    public string Id { get; }
    public string RegionType { get; }
    public string Shape { get; }
    public SimVector2 Center { get; set; }
    public SimVector2 Size { get; }
    public float Radius { get; }
    public bool BlocksMovement { get; }
    public bool BlocksBuilding { get; }
    public bool AllowsBuilding { get; }
    public IReadOnlyList<string> Tags { get; }

    public float Area => Shape == "circle"
        ? MathF.PI * Radius * Radius
        : MathF.Abs(Size.X * Size.Y);

    public bool Contains(SimVector2 position)
    {
        if (Shape == "circle")
        {
            return Center.DistanceTo(position) <= Radius;
        }

        if (Shape != "rect")
        {
            return false;
        }

        var halfWidth = Size.X * 0.5f;
        var halfHeight = Size.Y * 0.5f;
        return MathF.Abs(position.X - Center.X) <= halfWidth &&
            MathF.Abs(position.Y - Center.Y) <= halfHeight;
    }
}

public sealed class MapEditorObject
{
    public MapEditorObject(
        string id,
        string objectType,
        string shape,
        SimVector2 center,
        SimVector2 size,
        float radius,
        float maxHealth,
        bool startsIntact,
        bool blocksMovementWhenBroken,
        IReadOnlyList<string> tags)
    {
        Id = id;
        ObjectType = objectType;
        Shape = shape;
        Center = center;
        Size = size;
        Radius = radius;
        MaxHealth = maxHealth;
        StartsIntact = startsIntact;
        BlocksMovementWhenBroken = blocksMovementWhenBroken;
        Tags = tags;
    }

    public string Id { get; }
    public string ObjectType { get; }
    public string Shape { get; }
    public SimVector2 Center { get; set; }
    public SimVector2 Size { get; }
    public float Radius { get; }
    public float MaxHealth { get; }
    public bool StartsIntact { get; }
    public bool BlocksMovementWhenBroken { get; }
    public IReadOnlyList<string> Tags { get; }

    public float Area => Shape == "circle"
        ? MathF.PI * Radius * Radius
        : MathF.Abs(Size.X * Size.Y);

    public bool Contains(SimVector2 position)
    {
        if (Shape == "circle")
        {
            return Center.DistanceTo(position) <= Radius;
        }

        if (Shape != "rect")
        {
            return false;
        }

        var halfWidth = Size.X * 0.5f;
        var halfHeight = Size.Y * 0.5f;
        return MathF.Abs(position.X - Center.X) <= halfWidth &&
            MathF.Abs(position.Y - Center.Y) <= halfHeight;
    }
}

public enum MapEditorSelectionKind
{
    None,
    Marker,
    Region,
    Object
}

public sealed record MapEditorLogEntry(
    MapEditorLogLevel Level,
    string Operation,
    string Message,
    string MissionId,
    string MapId,
    MapEditorSelectionKind SelectionKind,
    string? SelectionId,
    int MarkerCount,
    int RegionCount,
    int ObjectCount,
    string? ExceptionType,
    string? ExceptionMessage)
{
    public string ToConsoleLine()
    {
        var selection = SelectionId is null
            ? SelectionKind.ToString()
            : $"{SelectionKind}:{SelectionId}";
        var exception = ExceptionType is null
            ? string.Empty
            : $" exception={ExceptionType}: {ExceptionMessage}";
        return $"[MapEditor:{Level}] op={Operation} mission={MissionId} map={MapId} selection={selection} markers={MarkerCount} regions={RegionCount} objects={ObjectCount} message=\"{Message}\"{exception}";
    }
}

public enum MapEditorLogLevel
{
    Info,
    Warning,
    Error
}
