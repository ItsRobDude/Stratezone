using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

// A building's footprint placed in the world (pixels). Placement, pathing, and ranges to
// buildings all measure against this one outline.
public sealed class BuildingFootprint
{
    private const float BoundingSlack = 0.01f;

    private readonly ConvexShape[] _parts;

    private BuildingFootprint(SimVector2 origin, ConvexShape[] parts, float boundingRadius)
    {
        Origin = origin;
        _parts = parts;
        BoundingRadius = boundingRadius;
    }

    public SimVector2 Origin { get; }
    public IReadOnlyList<ConvexShape> Parts => _parts;

    // Farthest outline point from the origin, in world pixels.
    public float BoundingRadius { get; }

    public static BuildingFootprint Place(FootprintDefinition definition, SimVector2 origin)
    {
        var parts = definition.Parts
            .Select(part => part.Kind == FootprintPartKind.Circle
                ? ConvexShape.Circle(origin + ToWorld(part.Center), RtsSimulation.ToWorldRadius(part.Radius))
                : ConvexShape.Polygon(part.Points.Select(point => origin + ToWorld(point)).ToArray()))
            .ToArray();

        // Measure from the placed world points so the bound agrees with the exact tests.
        var boundingRadius = parts.Length == 0
            ? 0.0f
            : parts.Max(part => part.IsCircle
                ? origin.DistanceTo(part.Center) + part.Radius
                : part.Points.Max(point => origin.DistanceTo(point)));
        return new BuildingFootprint(origin, parts, boundingRadius);
    }

    public bool Contains(SimVector2 point)
    {
        return IsWithin(point, 0.0f);
    }

    // Cheap bounding-circle reject before the exact outline test. The slack keeps
    // float rounding from rejecting points that lie exactly on the outline.
    public bool IsWithin(SimVector2 point, float distance)
    {
        return Origin.DistanceTo(point) <= BoundingRadius + distance + BoundingSlack &&
            DistanceTo(point) <= distance;
    }

    public float DistanceTo(SimVector2 point)
    {
        var best = float.MaxValue;
        foreach (var part in _parts)
        {
            best = MathF.Min(best, part.DistanceTo(point));
        }

        return best;
    }

    public SimVector2 ClosestPoint(SimVector2 point)
    {
        var best = Origin;
        var bestDistance = float.MaxValue;
        foreach (var part in _parts)
        {
            var candidate = part.ClosestPoint(point);
            var distance = candidate.DistanceTo(point);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }

    public float DistanceToSegment(SimVector2 start, SimVector2 end)
    {
        var best = float.MaxValue;
        foreach (var part in _parts)
        {
            best = MathF.Min(best, part.DistanceToSegment(start, end));
        }

        return best;
    }

    public float DistanceTo(ConvexShape shape)
    {
        var best = float.MaxValue;
        foreach (var part in _parts)
        {
            best = MathF.Min(best, part.DistanceTo(shape));
        }

        return best;
    }

    public float DistanceTo(BuildingFootprint other)
    {
        var best = float.MaxValue;
        foreach (var part in other._parts)
        {
            best = MathF.Min(best, DistanceTo(part));
        }

        return best;
    }

    private static SimVector2 ToWorld(SimVector2 contentOffset)
    {
        return contentOffset * RtsSimulation.ContentUnitScale;
    }
}
