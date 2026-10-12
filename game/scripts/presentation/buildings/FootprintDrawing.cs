using System.Linq;
using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

// Draws a building's real footprint outline. Coordinates are local pixels around `origin`.
public static class FootprintDrawing
{
    private const int CircleSegments = 48;

    public static void DrawOutline(CanvasItem canvas, FootprintDefinition footprint, Vector2 origin, Color color, float width, float grow = 0.0f)
    {
        foreach (var part in footprint.Parts)
        {
            if (part.Kind == FootprintPartKind.Circle)
            {
                canvas.DrawArc(origin + ToLocal(part.Center), RtsSimulation.ToWorldRadius(part.Radius) + grow, 0, Mathf.Tau, CircleSegments, color, width);
                continue;
            }

            var points = GetPolygon(part, origin, grow);
            canvas.DrawPolyline([.. points, points[0]], color, width);
        }
    }

    public static void DrawFill(CanvasItem canvas, FootprintDefinition footprint, Vector2 origin, Color color)
    {
        foreach (var part in footprint.Parts)
        {
            if (part.Kind == FootprintPartKind.Circle)
            {
                canvas.DrawCircle(origin + ToLocal(part.Center), RtsSimulation.ToWorldRadius(part.Radius), color);
                continue;
            }

            canvas.DrawColoredPolygon(GetPolygon(part, origin, 0.0f), color);
        }
    }

    public static Rect2 GetBounds(FootprintDefinition footprint)
    {
        var bounds = new Rect2();
        var hasBounds = false;
        foreach (var part in footprint.Parts)
        {
            var partBounds = part.Kind == FootprintPartKind.Circle
                ? new Rect2(ToLocal(part.Center) - (Vector2.One * RtsSimulation.ToWorldRadius(part.Radius)), Vector2.One * RtsSimulation.ToWorldRadius(part.Radius) * 2.0f)
                : GetPolygonBounds(GetPolygon(part, Vector2.Zero, 0.0f));
            bounds = hasBounds ? bounds.Merge(partBounds) : partBounds;
            hasBounds = true;
        }

        return bounds;
    }

    private static Vector2[] GetPolygon(FootprintPartDefinition part, Vector2 origin, float grow)
    {
        var points = part.Points.Select(point => origin + ToLocal(point)).ToArray();
        if (grow <= 0.0f)
        {
            return points;
        }

        var grown = Geometry2D.OffsetPolygon(points, grow, Geometry2D.PolyJoinType.Round);
        return grown.Count > 0 ? grown[0] : points;
    }

    private static Rect2 GetPolygonBounds(Vector2[] points)
    {
        var bounds = new Rect2(points[0], Vector2.Zero);
        foreach (var point in points)
        {
            bounds = bounds.Expand(point);
        }

        return bounds;
    }

    private static Vector2 ToLocal(SimVector2 contentPoint)
    {
        return new Vector2(contentPoint.X, contentPoint.Y) * RtsSimulation.ContentUnitScale;
    }
}
