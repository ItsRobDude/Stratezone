using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class MapRegionView : Node2D
{
    private IReadOnlyList<MapRegionDefinition> _regions = [];
    private IReadOnlyList<BridgeState> _bridges = [];

    public void UpdateFromMap(MapDefinition? map)
    {
        if (map is null)
        {
            _regions = [];
        }
        else
        {
            _regions = map.TerrainRegions
                .Where(region => ShouldDrawRegion(region, map.UsesRestrictedBuildRegions))
                .ToArray();
        }

        QueueRedraw();
    }

    public void UpdateBridgeStates(IReadOnlyList<BridgeState> bridges)
    {
        _bridges = bridges;
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var region in _regions)
        {
            DrawRegion(region);
        }

        foreach (var bridge in _bridges)
        {
            DrawBridge(bridge);
        }
    }

    private void DrawBridge(BridgeState bridge)
    {
        var fill = bridge.IsIntact
            ? new Color(0.45f, 0.36f, 0.24f, 0.82f)
            : new Color(0.24f, 0.10f, 0.08f, 0.78f);
        var outline = bridge.IsIntact
            ? new Color(0.86f, 0.68f, 0.42f, 0.92f)
            : new Color(0.95f, 0.25f, 0.18f, 0.92f);

        if (bridge.Definition.Shape == "rect")
        {
            var size = ToGodot(bridge.Definition.Size);
            var rect = new Rect2(ToGodot(bridge.Definition.Center) - (size * 0.5f), size);
            DrawRect(rect, fill);
            DrawRect(rect, outline, false, 2.0f);
            return;
        }

        if (bridge.Definition.Shape == "circle")
        {
            DrawCircle(ToGodot(bridge.Definition.Center), bridge.Definition.Radius, fill);
            DrawArc(ToGodot(bridge.Definition.Center), bridge.Definition.Radius, 0, Mathf.Tau, 72, outline, 2.0f);
        }
    }

    private void DrawRegion(MapRegionDefinition region)
    {
        var fill = GetFill(region);
        var outline = GetOutline(region);
        if (region.Shape == "rect")
        {
            var size = ToGodot(region.Size);
            var rect = new Rect2(ToGodot(region.Center) - (size * 0.5f), size);
            DrawRect(rect, fill);
            DrawRect(rect, outline, false, 2.0f);
            return;
        }

        if (region.Shape == "circle")
        {
            DrawCircle(ToGodot(region.Center), region.Radius, fill);
            DrawArc(ToGodot(region.Center), region.Radius, 0, Mathf.Tau, 72, outline, 2.0f);
        }
    }

    private static bool ShouldDrawRegion(MapRegionDefinition region, bool showBuildRegionHints)
    {
        if (region.BlocksMovement || region.BlocksBuilding)
        {
            return true;
        }

        return showBuildRegionHints;
    }

    private static Color GetFill(MapRegionDefinition region)
    {
        if (region.BlocksMovement || region.BlocksBuilding)
        {
            return new Color(0.17f, 0.19f, 0.20f, 0.72f);
        }

        return region.RegionType switch
        {
            "resource_basin" => new Color(0.70f, 0.56f, 0.16f, 0.22f),
            "buildable_clearing" => new Color(0.20f, 0.45f, 0.28f, 0.18f),
            "chokepoint_marker" => new Color(0.20f, 0.70f, 0.95f, 0.10f),
            _ => new Color(0.45f, 0.45f, 0.45f, 0.12f)
        };
    }

    private static Color GetOutline(MapRegionDefinition region)
    {
        if (region.BlocksMovement || region.BlocksBuilding)
        {
            return new Color(0.42f, 0.46f, 0.48f, 0.90f);
        }

        return region.RegionType switch
        {
            "resource_basin" => new Color(0.95f, 0.76f, 0.22f, 0.82f),
            "buildable_clearing" => new Color(0.42f, 0.78f, 0.42f, 0.65f),
            "chokepoint_marker" => new Color(0.38f, 0.86f, 1.0f, 0.70f),
            _ => new Color(0.60f, 0.60f, 0.60f, 0.60f)
        };
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }
}
