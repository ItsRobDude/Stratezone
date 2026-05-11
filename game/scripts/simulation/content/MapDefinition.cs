using Stratezone.Simulation;

namespace Stratezone.Simulation.Content;

public sealed record MapDefinition(
    string Id,
    string DisplayName,
    string Biome,
    string TargetSize,
    bool RequiresBuildableRegions,
    IReadOnlyList<string> RequiredFeatures,
    IReadOnlyList<MapRegionDefinition> TerrainRegions,
    IReadOnlyList<string> Tags)
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
