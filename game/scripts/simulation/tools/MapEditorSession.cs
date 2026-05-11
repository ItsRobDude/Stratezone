using System.Globalization;
using System.Text;
using Stratezone.Simulation.Content;

namespace Stratezone.Simulation.Tools;

public sealed class MapEditorSession
{
    public const float DefaultMarkerHitRadius = 26.0f;

    private readonly List<MapEditorMarker> _markers = [];
    private readonly List<MapEditorRegion> _regions = [];
    private MapEditorSelectionKind _selectionKind = MapEditorSelectionKind.None;
    private int _selectionIndex = -1;

    public string MissionId { get; private set; } = string.Empty;
    public string MapId { get; private set; } = string.Empty;
    public IReadOnlyList<MapEditorMarker> Markers => _markers;
    public IReadOnlyList<MapEditorRegion> Regions => _regions;
    public MapEditorSelectionKind SelectionKind => _selectionKind;

    public string? SelectedId => SelectedMarker?.Id ?? SelectedRegion?.Id;

    public SimVector2? SelectedCenter
    {
        get
        {
            if (SelectedMarker is not null)
            {
                return SelectedMarker.Position;
            }

            if (SelectedRegion is not null)
            {
                return SelectedRegion.Center;
            }

            return null;
        }
    }

    public MapEditorMarker? SelectedMarker => _selectionKind == MapEditorSelectionKind.Marker &&
        _selectionIndex >= 0 &&
        _selectionIndex < _markers.Count
            ? _markers[_selectionIndex]
            : null;

    public MapEditorRegion? SelectedRegion => _selectionKind == MapEditorSelectionKind.Region &&
        _selectionIndex >= 0 &&
        _selectionIndex < _regions.Count
            ? _regions[_selectionIndex]
            : null;

    public string SelectedSummary
    {
        get
        {
            if (SelectedMarker is not null)
            {
                return $"{SelectedMarker.Id} marker at {FormatVector(SelectedMarker.Position)}";
            }

            if (SelectedRegion is not null)
            {
                return $"{SelectedRegion.Id} {SelectedRegion.RegionType} {SelectedRegion.Shape} at {FormatVector(SelectedRegion.Center)}";
            }

            return "No marker or region selected.";
        }
    }

    public void Load(MissionDefinition? mission, MapDefinition? map)
    {
        MissionId = mission?.Id ?? string.Empty;
        MapId = map?.Id ?? string.Empty;
        _markers.Clear();
        _regions.Clear();
        ClearSelection();

        if (mission is not null)
        {
            foreach (var marker in mission.Markers)
            {
                _markers.Add(new MapEditorMarker(marker.Id, marker.Position));
            }

            foreach (var entity in mission.StartingEntities)
            {
                AddMarkerContent(entity.MarkerId, entity.ContentId);
            }

            foreach (var well in mission.ResourceWellPlacements)
            {
                AddMarkerContent(well.MarkerId, well.WellId);
            }
        }

        if (map is not null)
        {
            foreach (var region in map.TerrainRegions)
            {
                _regions.Add(new MapEditorRegion(
                    region.Id,
                    region.RegionType,
                    region.Shape,
                    region.Center,
                    region.Size,
                    region.Radius,
                    region.BlocksMovement,
                    region.BlocksBuilding,
                    region.AllowsBuilding,
                    region.Tags.ToArray()));
            }
        }
    }

    public bool SelectAt(SimVector2 position, float markerHitRadius = DefaultMarkerHitRadius)
    {
        ClearSelection();
        var markerIndex = FindMarkerAt(position, markerHitRadius);
        if (markerIndex >= 0)
        {
            _selectionKind = MapEditorSelectionKind.Marker;
            _selectionIndex = markerIndex;
            return true;
        }

        var regionIndex = FindRegionAt(position);
        if (regionIndex >= 0)
        {
            _selectionKind = MapEditorSelectionKind.Region;
            _selectionIndex = regionIndex;
            return true;
        }

        return false;
    }

    public bool SelectMarker(string id)
    {
        var index = _markers.FindIndex(marker => string.Equals(marker.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        _selectionKind = MapEditorSelectionKind.Marker;
        _selectionIndex = index;
        return true;
    }

    public bool SelectRegion(string id)
    {
        var index = _regions.FindIndex(region => string.Equals(region.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        _selectionKind = MapEditorSelectionKind.Region;
        _selectionIndex = index;
        return true;
    }

    public void ClearSelection()
    {
        _selectionKind = MapEditorSelectionKind.None;
        _selectionIndex = -1;
    }

    public bool MoveSelectedTo(SimVector2 center)
    {
        if (SelectedMarker is not null)
        {
            SelectedMarker.Position = center;
            return true;
        }

        if (SelectedRegion is not null)
        {
            SelectedRegion.Center = center;
            return true;
        }

        return false;
    }

    public bool NudgeSelected(SimVector2 delta)
    {
        var center = SelectedCenter;
        return center is not null && MoveSelectedTo(center.Value + delta);
    }

    public string ExportSelectedSnippet()
    {
        if (SelectedMarker is not null)
        {
            return FormatMissionMarker(SelectedMarker);
        }

        if (SelectedRegion is not null)
        {
            return FormatTerrainRegion(SelectedRegion);
        }

        return "No map editor selection.";
    }

    public string ExportAllSnippets()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Mission: {MissionId}");
        builder.AppendLine("\"mission_markers\": [");
        for (var index = 0; index < _markers.Count; index++)
        {
            builder.Append(Indent(FormatMissionMarker(_markers[index]), 2));
            builder.AppendLine(index == _markers.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("],");
        builder.AppendLine($"Map: {MapId}");
        builder.AppendLine("\"terrain_regions\": [");
        for (var index = 0; index < _regions.Count; index++)
        {
            builder.Append(Indent(FormatTerrainRegion(_regions[index]), 2));
            builder.AppendLine(index == _regions.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("]");
        return builder.ToString();
    }

    public static string FormatMissionMarker(MapEditorMarker marker)
    {
        return $$"""
        {
          "id": "{{JsonEscape(marker.Id)}}",
          "position": { "x": {{FormatNumber(marker.Position.X)}}, "y": {{FormatNumber(marker.Position.Y)}} }
        }
        """;
    }

    public static string FormatTerrainRegion(MapEditorRegion region)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"id\": \"{JsonEscape(region.Id)}\",");
        builder.AppendLine($"  \"region_type\": \"{JsonEscape(region.RegionType)}\",");
        builder.AppendLine($"  \"shape\": \"{JsonEscape(region.Shape)}\",");
        builder.AppendLine($"  \"center\": {{ \"x\": {FormatNumber(region.Center.X)}, \"y\": {FormatNumber(region.Center.Y)} }},");
        if (region.Shape == "rect")
        {
            builder.AppendLine($"  \"size\": {{ \"x\": {FormatNumber(region.Size.X)}, \"y\": {FormatNumber(region.Size.Y)} }},");
        }
        else if (region.Shape == "circle")
        {
            builder.AppendLine($"  \"radius\": {FormatNumber(region.Radius)},");
        }

        builder.AppendLine($"  \"blocks_movement\": {FormatBool(region.BlocksMovement)},");
        builder.AppendLine($"  \"blocks_building\": {FormatBool(region.BlocksBuilding)},");
        builder.AppendLine($"  \"allows_building\": {FormatBool(region.AllowsBuilding)},");
        builder.AppendLine($"  \"tags\": [{string.Join(", ", region.Tags.Select(tag => $"\"{JsonEscape(tag)}\""))}]");
        builder.Append("}");
        return builder.ToString();
    }

    public static string FormatVector(SimVector2 vector)
    {
        return $"({FormatNumber(vector.X)}, {FormatNumber(vector.Y)})";
    }

    private int FindMarkerAt(SimVector2 position, float markerHitRadius)
    {
        var bestIndex = -1;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < _markers.Count; index++)
        {
            var distance = position.DistanceTo(_markers[index].Position);
            if (distance <= markerHitRadius && distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
            }
        }

        return bestIndex;
    }

    private int FindRegionAt(SimVector2 position)
    {
        var bestIndex = -1;
        var bestArea = float.MaxValue;
        for (var index = 0; index < _regions.Count; index++)
        {
            var region = _regions[index];
            if (!region.Contains(position))
            {
                continue;
            }

            var area = region.Area;
            if (area < bestArea)
            {
                bestIndex = index;
                bestArea = area;
            }
        }

        return bestIndex;
    }

    private void AddMarkerContent(string markerId, string contentId)
    {
        var marker = _markers.FirstOrDefault(marker => string.Equals(marker.Id, markerId, StringComparison.Ordinal));
        marker?.AddContentId(contentId);
    }

    private static string Indent(string value, int spaces)
    {
        var prefix = new string(' ', spaces);
        return string.Join(
            Environment.NewLine,
            value.Split(Environment.NewLine).Select(line => line.Length == 0 ? line : prefix + line));
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static string JsonEscape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}

public sealed class MapEditorMarker
{
    private readonly List<string> _contentIds = [];

    public MapEditorMarker(string id, SimVector2 position)
    {
        Id = id;
        Position = position;
    }

    public string Id { get; }
    public SimVector2 Position { get; set; }
    public IReadOnlyList<string> ContentIds => _contentIds;

    internal void AddContentId(string contentId)
    {
        if (!_contentIds.Contains(contentId, StringComparer.Ordinal))
        {
            _contentIds.Add(contentId);
        }
    }
}

public sealed class MapEditorRegion
{
    public MapEditorRegion(
        string id,
        string regionType,
        string shape,
        SimVector2 center,
        SimVector2 size,
        float radius,
        bool blocksMovement,
        bool blocksBuilding,
        bool allowsBuilding,
        IReadOnlyList<string> tags)
    {
        Id = id;
        RegionType = regionType;
        Shape = shape;
        Center = center;
        Size = size;
        Radius = radius;
        BlocksMovement = blocksMovement;
        BlocksBuilding = blocksBuilding;
        AllowsBuilding = allowsBuilding;
        Tags = tags;
    }

    public string Id { get; }
    public string RegionType { get; }
    public string Shape { get; }
    public SimVector2 Center { get; set; }
    public SimVector2 Size { get; }
    public float Radius { get; }
    public bool BlocksMovement { get; }
    public bool BlocksBuilding { get; }
    public bool AllowsBuilding { get; }
    public IReadOnlyList<string> Tags { get; }

    public float Area => Shape == "circle"
        ? MathF.PI * Radius * Radius
        : MathF.Abs(Size.X * Size.Y);

    public bool Contains(SimVector2 position)
    {
        if (Shape == "circle")
        {
            return Center.DistanceTo(position) <= Radius;
        }

        if (Shape != "rect")
        {
            return false;
        }

        var halfWidth = Size.X * 0.5f;
        var halfHeight = Size.Y * 0.5f;
        return MathF.Abs(position.X - Center.X) <= halfWidth &&
            MathF.Abs(position.Y - Center.Y) <= halfHeight;
    }
}

public enum MapEditorSelectionKind
{
    None,
    Marker,
    Region
}
