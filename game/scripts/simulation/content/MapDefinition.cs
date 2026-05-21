using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed record PlayableBounds(float MinX, float MaxX, float MinY, float MaxY)
{
    public static readonly PlayableBounds Default = new(-1120.0f, 1320.0f, -760.0f, 700.0f);

    public float Width => MaxX - MinX;
    public float Height => MaxY - MinY;

    public bool Contains(SimVector2 point, float padding = 0.0f)
    {
        return point.X >= MinX - padding &&
            point.X <= MaxX + padding &&
            point.Y >= MinY - padding &&
            point.Y <= MaxY + padding;
    }
}

public sealed record MapDefinition(
    string Id,
    string DisplayName,
    string Biome,
    string TargetSize,
    bool RequiresBuildableRegions,
    IReadOnlyList<string> RequiredFeatures,
    IReadOnlyList<MapRegionDefinition> TerrainRegions,
    IReadOnlyList<MapObjectDefinition> MapObjects,
    IReadOnlyList<string> Tags,
    PlayableBounds PlayableBounds)
{
    public bool HasBuildableClearings => TerrainRegions.Any(region => region.AllowsBuilding);
    public bool UsesRestrictedBuildRegions => RequiresBuildableRegions && HasBuildableClearings;

    public bool BlocksMovementAt(SimVector2 position, float radius)
    {
        return TerrainRegions.Any(region => region.BlocksMovement && region.Contains(position, radius));
    }

    public bool BlocksBuildingAt(SimVector2 position, float radius)
    {
        return TerrainRegions.Any(region => region.BlocksBuilding && region.Contains(position, radius));
    }

    public bool AllowsBuildingAt(SimVector2 position, float radius)
    {
        return !UsesRestrictedBuildRegions ||
            TerrainRegions.Any(region => region.AllowsBuilding && region.Contains(position, radius));
    }
}

public sealed record MapObjectDefinition(
    string Id,
    string ObjectType,
    string Shape,
    SimVector2 Center,
    SimVector2 Size,
    float Radius,
    float MaxHealth,
    bool StartsIntact,
    bool BlocksMovementWhenBroken,
    IReadOnlyList<string> Tags)
{
    public bool IsBridge => string.Equals(ObjectType, "bridge", StringComparison.Ordinal);

    public bool Contains(SimVector2 position, float padding = 0.0f)
    {
        return Shape switch
        {
            "rect" => IsInsideRect(position, padding),
            "circle" => Center.DistanceTo(position) <= Radius + padding,
            _ => false
        };
    }

    public float DistanceTo(SimVector2 position)
    {
        if (Contains(position))
        {
            return 0.0f;
        }

        return Shape switch
        {
            "rect" => DistanceToRect(position),
            "circle" => MathF.Max(0.0f, Center.DistanceTo(position) - Radius),
            _ => Center.DistanceTo(position)
        };
    }

    private bool IsInsideRect(SimVector2 position, float padding)
    {
        var halfWidth = (Size.X * 0.5f) + padding;
        var halfHeight = (Size.Y * 0.5f) + padding;
        return MathF.Abs(position.X - Center.X) <= halfWidth &&
            MathF.Abs(position.Y - Center.Y) <= halfHeight;
    }

    private float DistanceToRect(SimVector2 position)
    {
        var dx = MathF.Max(MathF.Abs(position.X - Center.X) - (Size.X * 0.5f), 0.0f);
        var dy = MathF.Max(MathF.Abs(position.Y - Center.Y) - (Size.Y * 0.5f), 0.0f);
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}

public sealed record MapRegionDefinition(
    string Id,
    string RegionType,
    string Shape,
    SimVector2 Center,
    SimVector2 Size,
    float Radius,
    bool BlocksMovement,
    bool BlocksBuilding,
    bool AllowsBuilding,
    IReadOnlyList<string> Tags)
{
    public bool Contains(SimVector2 position, float padding = 0.0f)
    {
        return Shape switch
        {
            "rect" => IsInsideRect(position, padding),
            "circle" => Center.DistanceTo(position) <= Radius + padding,
            _ => false
        };
    }

    private bool IsInsideRect(SimVector2 position, float padding)
    {
        var halfWidth = (Size.X * 0.5f) + padding;
        var halfHeight = (Size.Y * 0.5f) + padding;
        return MathF.Abs(position.X - Center.X) <= halfWidth &&
            MathF.Abs(position.Y - Center.Y) <= halfHeight;
    }
}
