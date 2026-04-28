namespace Stratezone.Simulation;

public readonly record struct SimVector2(float X, float Y)
{
    public float DistanceTo(SimVector2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}
