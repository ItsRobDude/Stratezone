namespace Stratezone.Simulation;

public readonly record struct SimVector2(float X, float Y)
{
    public float Length()
    {
        return MathF.Sqrt((X * X) + (Y * Y));
    }

    public float DistanceTo(SimVector2 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }

    public SimVector2 Normalized()
    {
        var length = Length();
        return length <= 0.0001f
            ? new SimVector2(0.0f, 0.0f)
            : new SimVector2(X / length, Y / length);
    }

    public static SimVector2 operator +(SimVector2 left, SimVector2 right)
    {
        return new SimVector2(left.X + right.X, left.Y + right.Y);
    }

    public static SimVector2 operator -(SimVector2 left, SimVector2 right)
    {
        return new SimVector2(left.X - right.X, left.Y - right.Y);
    }

    public static SimVector2 operator *(SimVector2 vector, float scalar)
    {
        return new SimVector2(vector.X * scalar, vector.Y * scalar);
    }
}
