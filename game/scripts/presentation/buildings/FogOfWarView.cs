using Godot;
using Stratezone.Simulation;

public partial class FogOfWarView : Node2D
{
    private FogOfWarState? _fog;
    private Rect2? _visibleWorldBounds;

    public void UpdateFromState(FogOfWarState fog, Rect2 visibleWorldBounds)
    {
        _fog = fog;
        _visibleWorldBounds = visibleWorldBounds;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_fog is null)
        {
            return;
        }

        foreach (var cell in _fog.GetUnexploredCells())
        {
            var half = cell.Size * 0.5f;
            var rect = new Rect2(
                new Vector2(cell.Center.X - half, cell.Center.Y - half),
                new Vector2(cell.Size, cell.Size));
            if (_visibleWorldBounds is not null && !_visibleWorldBounds.Value.Intersects(rect))
            {
                continue;
            }

            DrawRect(rect, new Color(0.0f, 0.0f, 0.0f, 0.92f));
        }
    }
}
