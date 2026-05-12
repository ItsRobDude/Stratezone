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

    public string Id { get; set; }
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

    public string Id { get; set; }
    public string RegionType { get; set; }
    public string Shape { get; set; }
    public SimVector2 Center { get; set; }
    public SimVector2 Size { get; set; }
    public float Radius { get; set; }
    public bool BlocksMovement { get; set; }
    public bool BlocksBuilding { get; set; }
    public bool AllowsBuilding { get; set; }
    public IReadOnlyList<string> Tags { get; set; }

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

    public string Id { get; set; }
    public string ObjectType { get; set; }
    public string Shape { get; set; }
    public SimVector2 Center { get; set; }
    public SimVector2 Size { get; set; }
    public float Radius { get; set; }
    public float MaxHealth { get; set; }
    public bool StartsIntact { get; set; }
    public bool BlocksMovementWhenBroken { get; set; }
    public IReadOnlyList<string> Tags { get; set; }

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

public enum MapEditorToolMode
{
    Select,
    AddMarker,
    AddRectRegion,
    AddCircleRegion,
    Delete
}

public enum MapEditorResizeHandle
{
    None,
    RectLeft,
    RectRight,
    RectTop,
    RectBottom,
    CircleRadius
}

public enum MapEditorValidationSeverity
{
    Warning,
    Error
}

public sealed record MapEditorValidationIssue(
    MapEditorValidationSeverity Severity,
    string Code,
    string Message);

public sealed record MapEditorDeletePreview(
    MapEditorSelectionKind SelectionKind,
    string SelectionId,
    IReadOnlyList<string> References)
{
    public string Summary
    {
        get
        {
            if (References.Count == 0)
            {
                return $"Delete {SelectionId}? No known references.";
            }

            return $"Delete {SelectionId}? Referenced by: {string.Join(", ", References)}";
        }
    }
}

public sealed record MapEditorEditResult(
    bool Success,
    bool Changed,
    string Message,
    IReadOnlyList<string> Warnings)
{
    public static MapEditorEditResult Unchanged(string message)
    {
        return new MapEditorEditResult(true, false, message, []);
    }

    public static MapEditorEditResult ChangedResult(string message, IReadOnlyList<string>? warnings = null)
    {
        return new MapEditorEditResult(true, true, message, warnings ?? []);
    }

    public static MapEditorEditResult Failed(string message)
    {
        return new MapEditorEditResult(false, false, message, []);
    }
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
