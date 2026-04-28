using Godot;
using Stratezone.Simulation;

public partial class EnergyWallView : Node2D
{
    private readonly List<(Vector2 Start, Vector2 End)> _segments = [];

    public void UpdateSegments(IReadOnlyList<EnergyWallSegment> segments)
    {
        _segments.Clear();

        foreach (var segment in segments)
        {
            _segments.Add((ToGodot(segment.Start), ToGodot(segment.End)));
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (var segment in _segments)
        {
            DrawLine(segment.Start, segment.End, new Color(0.12f, 0.75f, 1.0f, 0.24f), 14.0f);
            DrawLine(segment.Start, segment.End, new Color(0.58f, 0.95f, 1.0f, 0.86f), 4.0f);
        }
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }
}
