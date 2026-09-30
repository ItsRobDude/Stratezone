using Godot;
using Stratezone.Simulation;

public partial class FogOfWarView : Node2D
{
    private FogOfWarState? _fog;
    private Rect2? _visibleWorldBounds;
    private readonly List<FogCell> _unexploredCellCache = [];
    private int _lastExploredRevision = -1;
    private Rect2? _lastVisibleWorldBounds;

    public void UpdateFromState(FogOfWarState fog, Rect2 visibleWorldBounds)
    {
        var fogChanged = fog.ExploredRevision != _lastExploredRevision;
        var boundsChanged = _lastVisibleWorldBounds is null ||
            BoundsChangedMeaningfully(_lastVisibleWorldBounds.Value, visibleWorldBounds);

        _fog = fog;
        if (fogChanged)
        {
            _lastExploredRevision = fog.ExploredRevision;
            _unexploredCellCache.Clear();
            foreach (var cell in fog.GetUnexploredCells())
            {
                _unexploredCellCache.Add(cell);
            }
        }

        if (fogChanged || boundsChanged)
        {
            _visibleWorldBounds = visibleWorldBounds;
            _lastVisibleWorldBounds = visibleWorldBounds;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (_fog is null || _fog.EditorReveal)
        {
            return;
        }

        foreach (var cell in _unexploredCellCache)
        {
            var half = cell.Size * 0.5f;
            var rect = new Rect2(
                new Vector2(cell.Center.X - half, cell.Center.Y - half),
                new Vector2(cell.Size, cell.Size));
            if (_visibleWorldBounds is not null && !_visibleWorldBounds.Value.Intersects(rect))
            {
                continue;
            }

            DrawRect(rect, Colors.Black);
        }
    }

    private static bool BoundsChangedMeaningfully(Rect2 previous, Rect2 next)
    {
        return previous.Position.DistanceTo(next.Position) > 32.0f ||
            Math.Abs(previous.Size.X - next.Size.X) > 16.0f ||
            Math.Abs(previous.Size.Y - next.Size.Y) > 16.0f;
    }
}
