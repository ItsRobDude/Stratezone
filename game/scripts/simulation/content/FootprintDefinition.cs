namespace Stratezone.Simulation.Content;

public enum FootprintPartKind
{
    Polygon,
    Circle
}

// One convex piece of a building's ground outline, in content units relative to the
// building origin (+Y = south). Rect parts are loaded as four-point polygons.
public sealed record FootprintPartDefinition(
    FootprintPartKind Kind,
    IReadOnlyList<SimVector2> Points,
    SimVector2 Center,
    float Radius)
{
    public static FootprintPartDefinition Polygon(IReadOnlyList<SimVector2> points)
    {
        return new FootprintPartDefinition(FootprintPartKind.Polygon, points, new SimVector2(0.0f, 0.0f), 0.0f);
    }

    public static FootprintPartDefinition Circle(SimVector2 center, float radius)
    {
        return new FootprintPartDefinition(FootprintPartKind.Circle, [], center, radius);
    }

    public static FootprintPartDefinition Rect(SimVector2 center, SimVector2 size)
    {
        var halfWidth = size.X * 0.5f;
        var halfHeight = size.Y * 0.5f;
        return Polygon(
        [
            new SimVector2(center.X - halfWidth, center.Y - halfHeight),
            new SimVector2(center.X + halfWidth, center.Y - halfHeight),
            new SimVector2(center.X + halfWidth, center.Y + halfHeight),
            new SimVector2(center.X - halfWidth, center.Y + halfHeight)
        ]);
    }
}

// A building's real ground outline: one or more convex parts with fixed orientation.
public sealed record FootprintDefinition(IReadOnlyList<FootprintPartDefinition> Parts)
{
    // Farthest outline point from the building origin, in content units.
    public float BoundingRadius { get; } = Parts.Count == 0
        ? 0.0f
        : Parts.Max(part => part.Kind == FootprintPartKind.Circle
            ? part.Center.Length() + part.Radius
            : part.Points.Max(point => point.Length()));

    // Southmost outline extent (+Y), in content units. 2D sprites stand on this edge.
    public float SouthExtent { get; } = Parts.Count == 0
        ? 0.0f
        : Parts.Max(part => part.Kind == FootprintPartKind.Circle
            ? part.Center.Y + part.Radius
            : part.Points.Max(point => point.Y));
}
