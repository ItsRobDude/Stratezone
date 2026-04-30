namespace Stratezone.Simulation;

public sealed class FogOfWarState
{
    private readonly HashSet<(int X, int Y)> _visible = [];
    private readonly HashSet<(int X, int Y)> _explored = [];

    public FogOfWarState(float minX, float maxX, float minY, float maxY, float cellSize)
    {
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
        CellSize = cellSize;
    }

    public float MinX { get; }
    public float MaxX { get; }
    public float MinY { get; }
    public float MaxY { get; }
    public float CellSize { get; }

    public void BeginVisibilityUpdate()
    {
        _visible.Clear();
    }

    public void Reveal(SimVector2 position, float radius)
    {
        var minCellX = ToCellX(position.X - radius);
        var maxCellX = ToCellX(position.X + radius);
        var minCellY = ToCellY(position.Y - radius);
        var maxCellY = ToCellY(position.Y + radius);

        for (var cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                var center = ToCenter(cellX, cellY);
                if (center.DistanceTo(position) > radius + (CellSize * 0.75f))
                {
                    continue;
                }

                var key = (cellX, cellY);
                _visible.Add(key);
                _explored.Add(key);
            }
        }
    }

    public bool IsVisible(SimVector2 position)
    {
        return _visible.Contains(ToCell(position));
    }

    public bool IsExplored(SimVector2 position)
    {
        return _explored.Contains(ToCell(position));
    }

    public IEnumerable<FogCell> GetUnexploredCells()
    {
        var minCellX = ToCellX(MinX);
        var maxCellX = ToCellX(MaxX);
        var minCellY = ToCellY(MinY);
        var maxCellY = ToCellY(MaxY);

        for (var cellX = minCellX; cellX <= maxCellX; cellX++)
        {
            for (var cellY = minCellY; cellY <= maxCellY; cellY++)
            {
                if (_explored.Contains((cellX, cellY)))
                {
                    continue;
                }

                yield return new FogCell(cellX, cellY, ToCenter(cellX, cellY), CellSize);
            }
        }
    }

    private (int X, int Y) ToCell(SimVector2 position)
    {
        return (ToCellX(position.X), ToCellY(position.Y));
    }

    private int ToCellX(float value)
    {
        return (int)MathF.Floor((value - MinX) / CellSize);
    }

    private int ToCellY(float value)
    {
        return (int)MathF.Floor((value - MinY) / CellSize);
    }

    private SimVector2 ToCenter(int cellX, int cellY)
    {
        return new SimVector2(
            MinX + (cellX * CellSize) + (CellSize * 0.5f),
            MinY + (cellY * CellSize) + (CellSize * 0.5f));
    }
}
