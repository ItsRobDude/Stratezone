namespace Stratezone.Simulation;

// A convex polygon or circle in world pixels. Distances are zero on or inside the shape.
public sealed class ConvexShape
{
    private const float InsideTolerance = 0.0001f;

    private readonly SimVector2[] _points;

    private ConvexShape(SimVector2[] points, SimVector2 center, float radius, bool isCircle)
    {
        _points = points;
        Center = center;
        Radius = radius;
        IsCircle = isCircle;
    }

    public bool IsCircle { get; }
    public IReadOnlyList<SimVector2> Points => _points;
    public SimVector2 Center { get; }

    // Circle radius, or the polygon's bounding radius around its vertex average.
    public float Radius { get; }

    public static ConvexShape Circle(SimVector2 center, float radius)
    {
        return new ConvexShape([], center, MathF.Max(0.0f, radius), isCircle: true);
    }

    public static ConvexShape Polygon(IReadOnlyList<SimVector2> points)
    {
        if (points.Count < 3)
        {
            throw new ArgumentException("A convex polygon needs at least three points.", nameof(points));
        }

        var copy = points.ToArray();
        var center = new SimVector2(copy.Average(point => point.X), copy.Average(point => point.Y));
        var radius = copy.Max(point => point.DistanceTo(center));
        return new ConvexShape(copy, center, radius, isCircle: false);
    }

    public static ConvexShape Rect(SimVector2 center, SimVector2 size)
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

    public bool Contains(SimVector2 point)
    {
        if (IsCircle)
        {
            return Center.DistanceTo(point) <= Radius;
        }

        var hasPositive = false;
        var hasNegative = false;
        for (var index = 0; index < _points.Length; index++)
        {
            var start = _points[index];
            var end = _points[(index + 1) % _points.Length];
            var cross = ((end.X - start.X) * (point.Y - start.Y)) - ((end.Y - start.Y) * (point.X - start.X));
            hasPositive |= cross > InsideTolerance;
            hasNegative |= cross < -InsideTolerance;
            if (hasPositive && hasNegative)
            {
                return false;
            }
        }

        return true;
    }

    public float DistanceTo(SimVector2 point)
    {
        if (IsCircle)
        {
            return MathF.Max(0.0f, Center.DistanceTo(point) - Radius);
        }

        if (Contains(point))
        {
            return 0.0f;
        }

        var best = float.MaxValue;
        for (var index = 0; index < _points.Length; index++)
        {
            best = MathF.Min(best, SimulationGeometry.DistancePointToSegment(point, _points[index], _points[(index + 1) % _points.Length]));
        }

        return best;
    }

    public SimVector2 ClosestPoint(SimVector2 point)
    {
        if (IsCircle)
        {
            var offset = point - Center;
            var length = offset.Length();
            return length <= Radius
                ? point
                : Center + (offset * (Radius / length));
        }

        if (Contains(point))
        {
            return point;
        }

        var best = _points[0];
        var bestDistance = float.MaxValue;
        for (var index = 0; index < _points.Length; index++)
        {
            var candidate = SimulationGeometry.ClosestPointOnSegment(point, _points[index], _points[(index + 1) % _points.Length]);
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
        if (IsCircle)
        {
            return MathF.Max(0.0f, SimulationGeometry.DistancePointToSegment(Center, start, end) - Radius);
        }

        if (Contains(start))
        {
            return 0.0f;
        }

        var best = float.MaxValue;
        for (var index = 0; index < _points.Length; index++)
        {
            best = MathF.Min(best, SimulationGeometry.DistanceSegmentToSegment(start, end, _points[index], _points[(index + 1) % _points.Length]));
        }

        return best;
    }

    public float DistanceTo(ConvexShape other)
    {
        if (IsCircle && other.IsCircle)
        {
            return MathF.Max(0.0f, Center.DistanceTo(other.Center) - Radius - other.Radius);
        }

        if (IsCircle)
        {
            return MathF.Max(0.0f, other.DistanceTo(Center) - Radius);
        }

        if (other.IsCircle)
        {
            return MathF.Max(0.0f, DistanceTo(other.Center) - other.Radius);
        }

        if (other.Contains(_points[0]) || Contains(other._points[0]))
        {
            return 0.0f;
        }

        // Disjoint convex polygons are closest between some edge pair; crossing edges give zero.
        var best = float.MaxValue;
        for (var index = 0; index < _points.Length; index++)
        {
            var start = _points[index];
            var end = _points[(index + 1) % _points.Length];
            for (var otherIndex = 0; otherIndex < other._points.Length; otherIndex++)
            {
                best = MathF.Min(best, SimulationGeometry.DistanceSegmentToSegment(
                    start,
                    end,
                    other._points[otherIndex],
                    other._points[(otherIndex + 1) % other._points.Length]));
            }
        }

        return best;
    }
}
