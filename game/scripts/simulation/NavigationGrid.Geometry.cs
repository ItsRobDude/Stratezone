using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal sealed partial class NavigationGrid
{
    private const float GeometryTolerance = 0.0001f;

    internal bool HasLineOfSight(SimVector2 from, SimVector2 to, float radius, string factionId)
    {
        return IsGeometrySegmentPassable(from, to, radius, factionId) &&
            TraversePassableCells(from, to, radius, factionId, escaping: false);
    }

    internal bool CanEscapeTo(SimVector2 from, SimVector2 to, float radius, string factionId)
    {
        return IsGeometrySegmentPassable(from, to, radius, factionId, escaping: true) &&
            TraversePassableCells(from, to, radius, factionId, escaping: true);
    }

    private bool IsExtraFootprintClear(SimVector2 point, float radius)
    {
        return _extraFootprint is null ||
            point.DistanceTo(_extraFootprint.Origin) > _extraFootprint.BoundingRadius + radius ||
            _extraFootprint.DistanceTo(point) >= radius;
    }

    private bool IsNeighborSegmentPassable(int cell, int neighbor, float radius, string factionId)
    {
        // Most of the map is far enough from obstacles that center clearance alone
        // proves the whole edge is clear. Sweep geometry only near an obstacle.
        var reach = radius + (CellSize * 1.41421356f);
        if (_extraFootprint is null && !_nearWalls[cell] && !_nearWalls[neighbor] &&
            _buildingClearance[cell] >= reach && _buildingClearance[neighbor] >= reach)
        {
            return true;
        }

        return IsGeometrySegmentPassable(CellCenter(cell), CellCenter(neighbor), radius, factionId);
    }

    private bool IsGeometrySegmentPassable(SimVector2 from, SimVector2 to, float radius, string factionId, bool escaping = false)
    {
        foreach (var footprint in _footprintKey)
        {
            if (!IsFootprintSegmentPassable(footprint, from, to, radius, escaping)) return false;
        }

        if (!AreExtraSegmentsPassable(from, to, radius, escaping)) return false;
        foreach (var wall in _walls)
        {
            if (wall.OwnerFactionId == factionId) continue;
            var segment = wall.Segment;
            var distance = SimulationGeometry.DistanceSegmentToSegment(from, to, segment.ExtendedStart, segment.ExtendedEnd);
            if (distance > EnergyWallSegment.BlockingClearance) continue;

            // A unit already caught in a newly formed wall may leave its band, but
            // an escape cannot move deeper into it or cross it from open ground.
            var startDistance = SimulationGeometry.DistancePointToSegment(from, segment.ExtendedStart, segment.ExtendedEnd);
            if (!escaping || startDistance > EnergyWallSegment.BlockingClearance ||
                distance + GeometryTolerance < startDistance) return false;
        }

        return true;
    }

    private bool AreExtraSegmentsPassable(SimVector2 from, SimVector2 to, float radius, bool escaping)
    {
        return (_extraFootprint is null || IsFootprintSegmentPassable(_extraFootprint, from, to, radius, escaping)) &&
            (_source is null || _source.AreExtraSegmentsPassable(from, to, radius, escaping));
    }

    private static bool IsFootprintSegmentPassable(BuildingFootprint footprint, SimVector2 from, SimVector2 to, float radius, bool escaping)
    {
        if (SimulationGeometry.DistancePointToSegment(footprint.Origin, from, to) > footprint.BoundingRadius + radius) return true;
        var distance = footprint.DistanceToSegment(from, to);
        if (distance + GeometryTolerance >= radius) return true;
        return escaping && distance + GeometryTolerance >= footprint.DistanceTo(from);
    }

    // Supercover traversal visits even a short cell intersection. At an exact
    // diagonal crossing it checks both orthogonal cells, just like GetNeighbors.
    private bool TraversePassableCells(SimVector2 from, SimVector2 to, float radius, string factionId, bool escaping)
    {
        if (!_bounds.Contains(from) || !_bounds.Contains(to)) return false;
        var startCell = CellIndexAt(from);
        var endCell = CellIndexAt(to);
        var x = startCell % Columns;
        var y = startCell / Columns;
        var endX = endCell % Columns;
        var endY = endCell / Columns;
        var delta = to - from;
        var stepX = Math.Sign(delta.X);
        var stepY = Math.Sign(delta.Y);
        var gridX = (from.X - _bounds.MinX) / CellSize;
        var gridY = (from.Y - _bounds.MinY) / CellSize;
        var alongXBoundary = stepX == 0 && MathF.Abs(gridX - MathF.Round(gridX)) < GeometryTolerance;
        var alongYBoundary = stepY == 0 && MathF.Abs(gridY - MathF.Round(gridY)) < GeometryTolerance;
        var tX = stepX == 0 ? float.PositiveInfinity :
            (_bounds.MinX + ((x + (stepX > 0 ? 1 : 0)) * CellSize) - from.X) / delta.X;
        var tY = stepY == 0 ? float.PositiveInfinity :
            (_bounds.MinY + ((y + (stepY > 0 ? 1 : 0)) * CellSize) - from.Y) / delta.Y;
        var strideX = stepX == 0 ? float.PositiveInfinity : CellSize / MathF.Abs(delta.X);
        var strideY = stepY == 0 ? float.PositiveInfinity : CellSize / MathF.Abs(delta.Y);

        bool Allowed(int column, int row)
        {
            if (column < 0 || row < 0 || column >= Columns || row >= Rows) return false;
            var cell = (row * Columns) + column;
            return escaping ? IsTerrainEscapeCellAllowed(cell, startCell, from, radius) : IsPassable(cell, radius, factionId);
        }

        while (true)
        {
            if (!Allowed(x, y)) return false;
            if (alongXBoundary && x > 0 && !Allowed(x - 1, y)) return false;
            if (alongYBoundary && y > 0 && !Allowed(x, y - 1)) return false;
            if (x == endX && y == endY) return true;

            if (MathF.Abs(tX - tY) <= 0.000001f)
            {
                if (!Allowed(x + stepX, y) || !Allowed(x, y + stepY)) return false;
                x += stepX;
                y += stepY;
                tX += strideX;
                tY += strideY;
            }
            else if (tX < tY)
            {
                x += stepX;
                tX += strideX;
            }
            else
            {
                y += stepY;
                tY += strideY;
            }
        }
    }

    private bool IsTerrainEscapeCellAllowed(int cell, int startCell, SimVector2 start, float radius)
    {
        if (_terrainClearance[cell] >= radius) return true;
        if (_terrainClearance[startCell] >= radius) return false;

        // Escape from the terrain that already contains the unit, without granting
        // an exemption for another region met on the way out.
        var padding = radius + (CellSize * 1.41421356f);
        var covered = false;
        foreach (var region in _regionKey ?? [])
        {
            if (!region.BlocksMovement || !region.Contains(CellCenter(cell), padding)) continue;
            if (!region.Contains(start, padding)) return false;
            covered = true;
        }

        return covered;
    }

    private static bool RegionOverlapsCell(MapRegionDefinition region, SimVector2 center)
    {
        var half = CellSize * 0.5f;
        if (region.Shape == "rect")
        {
            return MathF.Abs(center.X - region.Center.X) < (region.Size.X * 0.5f) + half &&
                MathF.Abs(center.Y - region.Center.Y) < (region.Size.Y * 0.5f) + half;
        }

        var dx = MathF.Max(MathF.Abs(center.X - region.Center.X) - half, 0.0f);
        var dy = MathF.Max(MathF.Abs(center.Y - region.Center.Y) - half, 0.0f);
        return (dx * dx) + (dy * dy) < region.Radius * region.Radius;
    }

    private static bool BridgeCoversCell(BridgeState bridge, SimVector2 center)
    {
        var half = CellSize * 0.5f;
        return bridge.Contains(center + new SimVector2(-half, -half)) &&
            bridge.Contains(center + new SimVector2(half, -half)) &&
            bridge.Contains(center + new SimVector2(half, half)) &&
            bridge.Contains(center + new SimVector2(-half, half));
    }
}
