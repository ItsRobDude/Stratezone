namespace Stratezone.Simulation;

internal static class SimulationGeometry
{
    public static bool LinesIntersect(SimVector2 a, SimVector2 b, SimVector2 c, SimVector2 d)
    {
        var denominator = ((d.Y - c.Y) * (b.X - a.X)) - ((d.X - c.X) * (b.Y - a.Y));
        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var ua = (((d.X - c.X) * (a.Y - c.Y)) - ((d.Y - c.Y) * (a.X - c.X))) / denominator;
        var ub = (((b.X - a.X) * (a.Y - c.Y)) - ((b.Y - a.Y) * (a.X - c.X))) / denominator;
        return ua is >= 0.0f and <= 1.0f && ub is >= 0.0f and <= 1.0f;
    }

    public static float DistancePointToSegment(SimVector2 point, SimVector2 start, SimVector2 end)
    {
        var segment = end - start;
        var lengthSquared = (segment.X * segment.X) + (segment.Y * segment.Y);
        if (lengthSquared <= 0.0001f)
        {
            return point.DistanceTo(start);
        }

        var pointOffset = point - start;
        var t = Dot(pointOffset, segment) / lengthSquared;
        t = Math.Clamp(t, 0.0f, 1.0f);
        var projection = start + (segment * t);
        return point.DistanceTo(projection);
    }

    public static float DistanceSegmentToSegment(SimVector2 a, SimVector2 b, SimVector2 c, SimVector2 d)
    {
        if (LinesIntersect(a, b, c, d))
        {
            return 0.0f;
        }

        return MathF.Min(
            MathF.Min(DistancePointToSegment(a, c, d), DistancePointToSegment(b, c, d)),
            MathF.Min(DistancePointToSegment(c, a, b), DistancePointToSegment(d, a, b)));
    }

    private static float Dot(SimVector2 left, SimVector2 right)
    {
        return (left.X * right.X) + (left.Y * right.Y);
    }
}
