using Godot;
using Stratezone.Simulation;

public partial class FogOfWarView : Node2D
{
    private FogOfWarState? _fog;

    public void UpdateFromState(FogOfWarState fog)
    {
        _fog = fog;
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
            DrawRect(
                new Rect2(
                    new Vector2(cell.Center.X - half, cell.Center.Y - half),
                    new Vector2(cell.Size, cell.Size)),
                new Color(0.0f, 0.0f, 0.0f, 0.92f));
        }
    }
}
