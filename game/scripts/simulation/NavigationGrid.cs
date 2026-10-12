using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

// An energy wall segment and the faction that owns it; it blocks every other faction.
internal readonly record struct NavigationWall(EnergyWallSegment Segment, string OwnerFactionId);

// Cached clearance field over the playable bounds (Track A2). Each cell stores the distance
// from its center to the nearest building or blocking terrain, so a cell is passable for a
// unit when that distance is at least the unit's collision radius. Layers rebuild only when
// their inputs change: buildings, energy walls, or terrain and bridge state.
internal sealed class NavigationGrid
{
    public const float CellSize = 8.0f;

    // Clearance is tracked up to this distance; it must exceed the largest unit radius.
    public const float MaxClearance = 64.0f;

    private readonly PlayableBounds _bounds;
    private readonly float[] _terrainClearance;
    private readonly float[] _buildingClearance;
    private readonly float[] _clearance;
    private readonly Dictionary<string, bool[]> _wallMasksByOwner;
    private readonly Dictionary<(float Radius, string FactionId), int[]> _components = [];
    private readonly bool _frozen;

    private readonly List<BuildingFootprint> _footprintKey = [];
    private readonly List<(int Start, int End, string Owner)> _wallKey = [];
    private readonly List<bool> _bridgeKey = [];
    private IReadOnlyList<MapRegionDefinition>? _regionKey;
    private bool _terrainBuilt;

    private float[] _searchCost = [];
    private int[] _searchParent = [];
    private int[] _searchStamp = [];
    private int _searchGeneration;

    public NavigationGrid(PlayableBounds bounds)
    {
        _bounds = bounds;
        Columns = Math.Max(1, (int)MathF.Ceiling(bounds.Width / CellSize));
        Rows = Math.Max(1, (int)MathF.Ceiling(bounds.Height / CellSize));
        _terrainClearance = new float[CellCount];
        _buildingClearance = new float[CellCount];
        _clearance = new float[CellCount];
        _wallMasksByOwner = new Dictionary<string, bool[]>(StringComparer.Ordinal);
        Array.Fill(_terrainClearance, MaxClearance);
        Array.Fill(_buildingClearance, MaxClearance);
        Array.Fill(_clearance, MaxClearance);
    }

    private NavigationGrid(NavigationGrid source)
    {
        _bounds = source._bounds;
        Columns = source.Columns;
        Rows = source.Rows;
        _terrainClearance = source._terrainClearance;
        _buildingClearance = (float[])source._buildingClearance.Clone();
        _clearance = (float[])source._clearance.Clone();
        _wallMasksByOwner = source._wallMasksByOwner;
        _frozen = true;
    }

    public int Columns { get; }
    public int Rows { get; }
    public int CellCount => Columns * Rows;
    internal int DebugRebuildCount { get; private set; }

    public void Refresh(
        IReadOnlyList<BuildingState> buildings,
        IReadOnlyList<NavigationWall> walls,
        IReadOnlyList<MapRegionDefinition> regions,
        IReadOnlyList<BridgeState> bridges)
    {
        if (_frozen)
        {
            throw new InvalidOperationException("A what-if navigation grid cannot be refreshed.");
        }

        var terrainChanged = RefreshTerrainKey(regions, bridges);
        var buildingsChanged = RefreshBuildingKey(buildings);
        var wallsChanged = RefreshWallKey(walls);
        if (!terrainChanged && !buildingsChanged && !wallsChanged)
        {
            return;
        }

        DebugRebuildCount++;
        if (terrainChanged)
        {
            RebuildTerrain(regions, bridges);
        }

        if (buildingsChanged)
        {
            Array.Fill(_buildingClearance, MaxClearance);
            foreach (var footprint in _footprintKey)
            {
                StampFootprint(footprint);
            }
        }

        if (wallsChanged)
        {
            RebuildWalls(walls);
        }

        CombineLayers();
        _components.Clear();
    }

    // A copy with one extra footprint, for asking "would this building wall something off?"
    public NavigationGrid WithExtraFootprint(BuildingFootprint footprint)
    {
        var copy = new NavigationGrid(this);
        copy.StampFootprint(footprint);
        copy.CombineLayers();
        return copy;
    }

    public bool IsPassable(int cell, float radius, string factionId)
    {
        return _clearance[cell] >= radius && !IsWallBlocked(cell, factionId);
    }

    public float ClearanceAt(int cell)
    {
        return _clearance[cell];
    }

    public bool IsPassableAt(SimVector2 point, float radius, string factionId)
    {
        return IsPassable(CellIndexAt(point), radius, factionId);
    }

    public int CellIndexAt(SimVector2 point)
    {
        var x = Math.Clamp((int)MathF.Floor((point.X - _bounds.MinX) / CellSize), 0, Columns - 1);
        var y = Math.Clamp((int)MathF.Floor((point.Y - _bounds.MinY) / CellSize), 0, Rows - 1);
        return (y * Columns) + x;
    }

    public SimVector2 CellCenter(int cell)
    {
        var x = cell % Columns;
        var y = cell / Columns;
        return new SimVector2(
            _bounds.MinX + (x * CellSize) + (CellSize * 0.5f),
            _bounds.MinY + (y * CellSize) + (CellSize * 0.5f));
    }

    public SimVector2 ClampToBounds(SimVector2 point)
    {
        return new SimVector2(
            Math.Clamp(point.X, _bounds.MinX, _bounds.MaxX),
            Math.Clamp(point.Y, _bounds.MinY, _bounds.MaxY));
    }

    // Connected-region label for a cell (-1 when impassable). Two cells with the same label
    // are mutually reachable for that radius and faction, so reachability is one lookup.
    public int ComponentOf(int cell, float radius, string factionId)
    {
        if (!_components.TryGetValue((radius, factionId), out var labels))
        {
            labels = LabelComponents(radius, factionId);
            _components[(radius, factionId)] = labels;
        }

        return labels[cell];
    }

    // The accepted cell nearest to `point`, searching square rings out to `maxRings`; -1 if none.
    public int FindNearestCell(SimVector2 point, int maxRings, Func<int, bool> accept)
    {
        var center = CellIndexAt(point);
        var centerX = center % Columns;
        var centerY = center / Columns;
        var best = -1;
        var bestDistance = float.MaxValue;
        for (var ring = 0; ring <= maxRings; ring++)
        {
            // Every cell in this ring is at least (ring - 1) cells from the point.
            if (best >= 0 && (ring - 1) * CellSize > bestDistance)
            {
                break;
            }

            for (var y = centerY - ring; y <= centerY + ring; y++)
            {
                if (y < 0 || y >= Rows)
                {
                    continue;
                }

                var onEdgeRow = y == centerY - ring || y == centerY + ring;
                for (var x = centerX - ring; x <= centerX + ring; x += onEdgeRow ? 1 : ring * 2)
                {
                    if (x >= 0 && x < Columns)
                    {
                        var cell = (y * Columns) + x;
                        var distance = CellCenter(cell).DistanceTo(point);
                        if (distance < bestDistance && accept(cell))
                        {
                            best = cell;
                            bestDistance = distance;
                        }
                    }

                    if (ring == 0)
                    {
                        break;
                    }
                }
            }
        }

        return best;
    }

    // Reusable A* buffers; generation stamps avoid clearing them between searches.
    internal (float[] Cost, int[] Parent, int[] Stamp, int Generation) BeginSearch()
    {
        if (_searchStamp.Length != CellCount)
        {
            _searchCost = new float[CellCount];
            _searchParent = new int[CellCount];
            _searchStamp = new int[CellCount];
            _searchGeneration = 0;
        }

        _searchGeneration++;
        return (_searchCost, _searchParent, _searchStamp, _searchGeneration);
    }

    private bool IsWallBlocked(int cell, string factionId)
    {
        foreach (var (owner, mask) in _wallMasksByOwner)
        {
            if (mask[cell] && !string.Equals(owner, factionId, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool RefreshTerrainKey(IReadOnlyList<MapRegionDefinition> regions, IReadOnlyList<BridgeState> bridges)
    {
        var changed = !_terrainBuilt || !ReferenceEquals(_regionKey, regions) || _bridgeKey.Count != bridges.Count;
        for (var index = 0; !changed && index < bridges.Count; index++)
        {
            changed = _bridgeKey[index] != bridges[index].IsIntact;
        }

        if (changed)
        {
            _regionKey = regions;
            _bridgeKey.Clear();
            _bridgeKey.AddRange(bridges.Select(bridge => bridge.IsIntact));
            _terrainBuilt = true;
        }

        return changed;
    }

    private bool RefreshBuildingKey(IReadOnlyList<BuildingState> buildings)
    {
        var index = 0;
        var changed = false;
        foreach (var building in buildings)
        {
            if (building.IsDestroyed)
            {
                continue;
            }

            if (index >= _footprintKey.Count || !ReferenceEquals(_footprintKey[index], building.Footprint))
            {
                changed = true;
                break;
            }

            index++;
        }

        changed |= !changed && index != _footprintKey.Count;
        if (changed)
        {
            _footprintKey.Clear();
            _footprintKey.AddRange(buildings.Where(building => !building.IsDestroyed).Select(building => building.Footprint));
        }

        return changed;
    }

    private bool RefreshWallKey(IReadOnlyList<NavigationWall> walls)
    {
        var changed = walls.Count != _wallKey.Count;
        for (var index = 0; !changed && index < walls.Count; index++)
        {
            var wall = walls[index];
            changed = _wallKey[index] != (wall.Segment.StartAnchorEntityId, wall.Segment.EndAnchorEntityId, wall.OwnerFactionId);
        }

        if (changed)
        {
            _wallKey.Clear();
            _wallKey.AddRange(walls.Select(wall => (wall.Segment.StartAnchorEntityId, wall.Segment.EndAnchorEntityId, wall.OwnerFactionId)));
        }

        return changed;
    }

    private void RebuildTerrain(IReadOnlyList<MapRegionDefinition> regions, IReadOnlyList<BridgeState> bridges)
    {
        // Rasterize blocking terrain not spanned by an intact bridge, then take exact
        // distances to it. Raster edges are within half a cell of the true edge.
        var blocked = new bool[CellCount];
        var anyBlocked = false;
        foreach (var region in regions.Where(region => region.BlocksMovement))
        {
            var halfExtent = region.Shape == "circle"
                ? new SimVector2(region.Radius, region.Radius)
                : region.Size * 0.5f;
            ForEachCellIn(region.Center - halfExtent, region.Center + halfExtent, cell =>
            {
                var center = CellCenter(cell);
                if (region.Contains(center) && !bridges.Any(bridge => bridge.IsIntact && bridge.Contains(center)))
                {
                    blocked[cell] = true;
                    anyBlocked = true;
                }
            });
        }

        if (!anyBlocked)
        {
            Array.Fill(_terrainClearance, MaxClearance);
            return;
        }

        var squared = SquaredDistanceTransform(blocked);
        for (var cell = 0; cell < CellCount; cell++)
        {
            var distance = (float)(Math.Sqrt(squared[cell]) * CellSize) - (CellSize * 0.5f);
            _terrainClearance[cell] = blocked[cell] ? 0.0f : Math.Clamp(distance, 0.0f, MaxClearance);
        }
    }

    private void StampFootprint(BuildingFootprint footprint)
    {
        var reach = new SimVector2(footprint.BoundingRadius + MaxClearance, footprint.BoundingRadius + MaxClearance);
        ForEachCellIn(footprint.Origin - reach, footprint.Origin + reach, cell =>
        {
            var distance = footprint.DistanceTo(CellCenter(cell));
            if (distance < _buildingClearance[cell])
            {
                _buildingClearance[cell] = distance;
            }
        });
    }

    private void RebuildWalls(IReadOnlyList<NavigationWall> walls)
    {
        _wallMasksByOwner.Clear();
        var reach = new SimVector2(EnergyWallSegment.BlockingClearance, EnergyWallSegment.BlockingClearance);
        foreach (var wall in walls)
        {
            if (!_wallMasksByOwner.TryGetValue(wall.OwnerFactionId, out var mask))
            {
                mask = new bool[CellCount];
                _wallMasksByOwner[wall.OwnerFactionId] = mask;
            }

            var start = wall.Segment.ExtendedStart;
            var end = wall.Segment.ExtendedEnd;
            var min = new SimVector2(MathF.Min(start.X, end.X), MathF.Min(start.Y, end.Y)) - reach;
            var max = new SimVector2(MathF.Max(start.X, end.X), MathF.Max(start.Y, end.Y)) + reach;
            ForEachCellIn(min, max, cell =>
            {
                if (SimulationGeometry.DistancePointToSegment(CellCenter(cell), start, end) <= EnergyWallSegment.BlockingClearance)
                {
                    mask[cell] = true;
                }
            });
        }
    }

    private void CombineLayers()
    {
        for (var cell = 0; cell < CellCount; cell++)
        {
            _clearance[cell] = MathF.Min(_terrainClearance[cell], _buildingClearance[cell]);
        }
    }

    private void ForEachCellIn(SimVector2 min, SimVector2 max, Action<int> visit)
    {
        var minCell = CellIndexAt(min);
        var maxCell = CellIndexAt(max);
        for (var y = minCell / Columns; y <= maxCell / Columns; y++)
        {
            for (var x = minCell % Columns; x <= maxCell % Columns; x++)
            {
                visit((y * Columns) + x);
            }
        }
    }

    // Flood fill with the same moves A* uses: diagonals need both orthogonal neighbors open.
    private int[] LabelComponents(float radius, string factionId)
    {
        var labels = new int[CellCount];
        Array.Fill(labels, -1);
        var queue = new int[CellCount];
        var nextLabel = 0;
        Span<int> neighbors = stackalloc int[8];
        for (var seed = 0; seed < CellCount; seed++)
        {
            if (labels[seed] >= 0 || !IsPassable(seed, radius, factionId))
            {
                continue;
            }

            var head = 0;
            var tail = 0;
            labels[seed] = nextLabel;
            queue[tail++] = seed;
            while (head < tail)
            {
                var cell = queue[head++];
                var count = GetNeighbors(cell, radius, factionId, neighbors);
                for (var index = 0; index < count; index++)
                {
                    var neighbor = neighbors[index];
                    if (labels[neighbor] < 0)
                    {
                        labels[neighbor] = nextLabel;
                        queue[tail++] = neighbor;
                    }
                }
            }

            nextLabel++;
        }

        return labels;
    }

    // Writes the passable neighbors of `cell` into `output` and returns how many there are.
    internal int GetNeighbors(int cell, float radius, string factionId, Span<int> output)
    {
        var x = cell % Columns;
        var y = cell / Columns;
        var count = 0;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                if ((dx == 0 && dy == 0) || !IsOpen(x + dx, y + dy, radius, factionId))
                {
                    continue;
                }

                if (dx != 0 && dy != 0 &&
                    (!IsOpen(x + dx, y, radius, factionId) || !IsOpen(x, y + dy, radius, factionId)))
                {
                    continue;
                }

                output[count++] = ((y + dy) * Columns) + x + dx;
            }
        }

        return count;
    }

    private bool IsOpen(int x, int y, float radius, string factionId)
    {
        return x >= 0 && y >= 0 && x < Columns && y < Rows && IsPassable((y * Columns) + x, radius, factionId);
    }

    // Exact squared Euclidean distance (in cells) to the nearest blocked cell, using
    // Felzenszwalb and Huttenlocher's separable transform.
    private double[] SquaredDistanceTransform(bool[] blocked)
    {
        // Larger than any real squared distance on this grid, yet small enough to keep
        // the parabola intersections precise.
        var unreached = (double)(Columns + Rows) * (Columns + Rows);
        var result = new double[CellCount];
        for (var cell = 0; cell < CellCount; cell++)
        {
            result[cell] = blocked[cell] ? 0.0 : unreached;
        }

        var length = Math.Max(Columns, Rows);
        var line = new double[length];
        var output = new double[length];
        var hulls = new int[length];
        var bounds = new double[length + 1];

        for (var x = 0; x < Columns; x++)
        {
            for (var y = 0; y < Rows; y++)
            {
                line[y] = result[(y * Columns) + x];
            }

            Transform1D(line, Rows, output, hulls, bounds);
            for (var y = 0; y < Rows; y++)
            {
                result[(y * Columns) + x] = output[y];
            }
        }

        for (var y = 0; y < Rows; y++)
        {
            Array.Copy(result, y * Columns, line, 0, Columns);
            Transform1D(line, Columns, output, hulls, bounds);
            Array.Copy(output, 0, result, y * Columns, Columns);
        }

        return result;
    }

    private static void Transform1D(double[] f, int n, double[] d, int[] v, double[] z)
    {
        var k = 0;
        v[0] = 0;
        z[0] = double.NegativeInfinity;
        z[1] = double.PositiveInfinity;
        for (var q = 1; q < n; q++)
        {
            var s = Intersection(f, q, v[k]);
            while (s <= z[k])
            {
                k--;
                s = Intersection(f, q, v[k]);
            }

            k++;
            v[k] = q;
            z[k] = s;
            z[k + 1] = double.PositiveInfinity;
        }

        k = 0;
        for (var q = 0; q < n; q++)
        {
            while (z[k + 1] < q)
            {
                k++;
            }

            var offset = q - v[k];
            d[q] = (offset * offset) + f[v[k]];
        }
    }

    private static double Intersection(double[] f, int q, int p)
    {
        return ((f[q] + ((double)q * q)) - (f[p] + ((double)p * p))) / ((2.0 * q) - (2.0 * p));
    }
}
