using System.Globalization;
using System.Text;
using Stratezone.Simulation.Content;

namespace Stratezone.Simulation.Tools;

public sealed class MapEditorSession
{
    public const float DefaultMarkerHitRadius = 26.0f;
    private const int MaxDiagnostics = 200;

    private readonly List<MapEditorMarker> _markers = [];
    private readonly List<MapEditorRegion> _regions = [];
    private readonly List<MapEditorObject> _objects = [];
    private readonly List<MapEditorLogEntry> _diagnostics = [];
    private MapEditorSelectionKind _selectionKind = MapEditorSelectionKind.None;
    private int _selectionIndex = -1;

    public event Action<MapEditorLogEntry>? DiagnosticEmitted;

    public string MissionId { get; private set; } = string.Empty;
    public string MapId { get; private set; } = string.Empty;
    public IReadOnlyList<MapEditorMarker> Markers => _markers;
    public IReadOnlyList<MapEditorRegion> Regions => _regions;
    public IReadOnlyList<MapEditorObject> Objects => _objects;
    public IReadOnlyList<MapEditorLogEntry> Diagnostics => _diagnostics;
    public MapEditorSelectionKind SelectionKind => _selectionKind;
    public int Revision { get; private set; }

    public string? SelectedId => SelectedMarker?.Id ?? SelectedObject?.Id ?? SelectedRegion?.Id;

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

            if (SelectedObject is not null)
            {
                return SelectedObject.Center;
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

    public MapEditorObject? SelectedObject => _selectionKind == MapEditorSelectionKind.Object &&
        _selectionIndex >= 0 &&
        _selectionIndex < _objects.Count
            ? _objects[_selectionIndex]
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

            if (SelectedObject is not null)
            {
                return $"{SelectedObject.Id} {SelectedObject.ObjectType} {SelectedObject.Shape} HP {SelectedObject.MaxHealth:0} at {FormatVector(SelectedObject.Center)}";
            }

            return "No marker, region, or object selected.";
        }
    }

    public string SelectedInspector
    {
        get
        {
            if (SelectedMarker is not null)
            {
                var content = SelectedMarker.ContentIds.Count == 0
                    ? "none"
                    : string.Join(", ", SelectedMarker.ContentIds);
                return $"Marker id={SelectedMarker.Id}\nposition={FormatVector(SelectedMarker.Position)}\ncontent={content}";
            }

            if (SelectedRegion is not null)
            {
                return "Region " +
                    $"id={SelectedRegion.Id}\n" +
                    $"type={SelectedRegion.RegionType} shape={SelectedRegion.Shape}\n" +
                    $"center={FormatVector(SelectedRegion.Center)} size={FormatShapeSize(SelectedRegion.Shape, SelectedRegion.Size, SelectedRegion.Radius)}\n" +
                    $"blocks_movement={FormatBool(SelectedRegion.BlocksMovement)} blocks_building={FormatBool(SelectedRegion.BlocksBuilding)} allows_building={FormatBool(SelectedRegion.AllowsBuilding)}\n" +
                    $"tags={FormatTags(SelectedRegion.Tags)}";
            }

            if (SelectedObject is not null)
            {
                return "Object " +
                    $"id={SelectedObject.Id}\n" +
                    $"type={SelectedObject.ObjectType} shape={SelectedObject.Shape}\n" +
                    $"center={FormatVector(SelectedObject.Center)} size={FormatShapeSize(SelectedObject.Shape, SelectedObject.Size, SelectedObject.Radius)}\n" +
                    $"max_health={FormatNumber(SelectedObject.MaxHealth)} starts_intact={FormatBool(SelectedObject.StartsIntact)} blocks_when_broken={FormatBool(SelectedObject.BlocksMovementWhenBroken)}\n" +
                    $"tags={FormatTags(SelectedObject.Tags)}";
            }

            return "No selection.";
        }
    }

    public void Load(MissionDefinition? mission, MapDefinition? map)
    {
        MissionId = mission?.Id ?? string.Empty;
        MapId = map?.Id ?? string.Empty;
        _markers.Clear();
        _regions.Clear();
        _objects.Clear();
        ClearSelection();
        Revision = 0;

        if (mission is null)
        {
            LogError("load", "No mission definition was provided; map editor has no mission markers.");
        }

        if (map is null)
        {
            LogError("load", "No map definition was provided; map editor has no terrain regions.");
        }

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

        LogDuplicateIds(_markers.Select(marker => marker.Id), "mission marker");

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

            foreach (var mapObject in map.MapObjects)
            {
                _objects.Add(new MapEditorObject(
                    mapObject.Id,
                    mapObject.ObjectType,
                    mapObject.Shape,
                    mapObject.Center,
                    mapObject.Size,
                    mapObject.Radius,
                    mapObject.MaxHealth,
                    mapObject.StartsIntact,
                    mapObject.BlocksMovementWhenBroken,
                    mapObject.Tags.ToArray()));
            }
        }

        LogDuplicateIds(_regions.Select(region => region.Id), "terrain region");
        LogDuplicateIds(_objects.Select(item => item.Id), "map object");
        if (mission is not null && _markers.Count == 0)
        {
            LogWarning("load", $"Mission '{MissionId}' has no mission markers.");
        }

        if (map is not null && _regions.Count == 0)
        {
            LogInfo("load", $"Map '{MapId}' has no terrain regions; editor will show markers and live simulation overlays only.");
        }

        LogInfo("load", $"Loaded map editor session: mission='{MissionId}', map='{MapId}', markers={_markers.Count}, regions={_regions.Count}, objects={_objects.Count}.");
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

        var objectIndex = FindObjectAt(position);
        if (objectIndex >= 0)
        {
            _selectionKind = MapEditorSelectionKind.Object;
            _selectionIndex = objectIndex;
            return true;
        }

        var regionIndex = FindRegionAt(position);
        if (regionIndex >= 0)
        {
            _selectionKind = MapEditorSelectionKind.Region;
            _selectionIndex = regionIndex;
            return true;
        }

        LogInfo("select_at", $"No marker, region, or object at {FormatVector(position)}.");
        return false;
    }

    public bool SelectMarker(string id)
    {
        var index = _markers.FindIndex(marker => string.Equals(marker.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            LogWarning("select_marker", $"Marker '{id}' does not exist in mission '{MissionId}'.");
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
            LogWarning("select_region", $"Region '{id}' does not exist in map '{MapId}'.");
            return false;
        }

        _selectionKind = MapEditorSelectionKind.Region;
        _selectionIndex = index;
        return true;
    }

    public bool SelectNext()
    {
        return SelectRelative(1, "select_next");
    }

    public bool SelectPrevious()
    {
        return SelectRelative(-1, "select_previous");
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
            if (SelectedMarker.Position == center)
            {
                return true;
            }

            SelectedMarker.Position = center;
            Revision++;
            return true;
        }

        if (SelectedRegion is not null)
        {
            if (SelectedRegion.Center == center)
            {
                return true;
            }

            SelectedRegion.Center = center;
            Revision++;
            return true;
        }

        if (SelectedObject is not null)
        {
            if (SelectedObject.Center == center)
            {
                return true;
            }

            SelectedObject.Center = center;
            Revision++;
            return true;
        }

        LogWarning("move_selected", $"Cannot move to {FormatVector(center)} because no marker, region, or object is selected.");
        return false;
    }

    public MissionDefinition ApplyToMission(MissionDefinition mission)
    {
        return mission with
        {
            Markers = _markers
                .Select(marker => new MissionMarkerDefinition(marker.Id, marker.Position))
                .ToArray()
        };
    }

    public MapDefinition ApplyToMap(MapDefinition map)
    {
        return map with
        {
            TerrainRegions = _regions
                .Select(region => new MapRegionDefinition(
                    region.Id,
                    region.RegionType,
                    region.Shape,
                    region.Center,
                    region.Size,
                    region.Radius,
                    region.BlocksMovement,
                    region.BlocksBuilding,
                    region.AllowsBuilding,
                    region.Tags.ToArray()))
                .ToArray(),
            MapObjects = _objects
                .Select(mapObject => new MapObjectDefinition(
                    mapObject.Id,
                    mapObject.ObjectType,
                    mapObject.Shape,
                    mapObject.Center,
                    mapObject.Size,
                    mapObject.Radius,
                    mapObject.MaxHealth,
                    mapObject.StartsIntact,
                    mapObject.BlocksMovementWhenBroken,
                    mapObject.Tags.ToArray()))
                .ToArray()
        };
    }

    public bool NudgeSelected(SimVector2 delta)
    {
        var center = SelectedCenter;
        if (center is null)
        {
            LogWarning("nudge_selected", $"Cannot nudge by {FormatVector(delta)} because no marker, region, or object is selected.");
            return false;
        }

        return MoveSelectedTo(center.Value + delta);
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

        if (SelectedObject is not null)
        {
            return FormatMapObject(SelectedObject);
        }

        LogWarning("export_selected", "Cannot export selection because no marker, region, or object is selected.");
        return "No map editor selection.";
    }

    public string ExportAllSnippets()
    {
        if (_markers.Count == 0 && _regions.Count == 0 && _objects.Count == 0)
        {
            LogWarning("export_all", "Full export requested with no mission markers, terrain regions, or map objects loaded.");
        }

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

        builder.AppendLine("],");
        builder.AppendLine("\"map_objects\": [");
        for (var index = 0; index < _objects.Count; index++)
        {
            builder.Append(Indent(FormatMapObject(_objects[index]), 2));
            builder.AppendLine(index == _objects.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("]");
        return builder.ToString();
    }

    public void LogInfo(string operation, string message)
    {
        EmitDiagnostic(MapEditorLogLevel.Info, operation, message);
    }

    public void LogWarning(string operation, string message)
    {
        EmitDiagnostic(MapEditorLogLevel.Warning, operation, message);
    }

    public void LogError(string operation, string message)
    {
        EmitDiagnostic(MapEditorLogLevel.Error, operation, message);
    }

    public void LogException(string operation, Exception exception, string? message = null)
    {
        EmitDiagnostic(
            MapEditorLogLevel.Error,
            operation,
            message ?? "Unhandled map editor exception.",
            exception.GetType().Name,
            exception.Message);
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

    public static string FormatMapObject(MapEditorObject mapObject)
    {
        var builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine($"  \"id\": \"{JsonEscape(mapObject.Id)}\",");
        builder.AppendLine($"  \"object_type\": \"{JsonEscape(mapObject.ObjectType)}\",");
        builder.AppendLine($"  \"shape\": \"{JsonEscape(mapObject.Shape)}\",");
        builder.AppendLine($"  \"center\": {{ \"x\": {FormatNumber(mapObject.Center.X)}, \"y\": {FormatNumber(mapObject.Center.Y)} }},");
        if (mapObject.Shape == "rect")
        {
            builder.AppendLine($"  \"size\": {{ \"x\": {FormatNumber(mapObject.Size.X)}, \"y\": {FormatNumber(mapObject.Size.Y)} }},");
        }
        else if (mapObject.Shape == "circle")
        {
            builder.AppendLine($"  \"radius\": {FormatNumber(mapObject.Radius)},");
        }

        builder.AppendLine($"  \"max_health\": {FormatNumber(mapObject.MaxHealth)},");
        builder.AppendLine($"  \"starts_intact\": {FormatBool(mapObject.StartsIntact)},");
        builder.AppendLine($"  \"blocks_movement_when_broken\": {FormatBool(mapObject.BlocksMovementWhenBroken)},");
        builder.AppendLine($"  \"tags\": [{string.Join(", ", mapObject.Tags.Select(tag => $"\"{JsonEscape(tag)}\""))}]");
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

    private int FindObjectAt(SimVector2 position)
    {
        var bestIndex = -1;
        var bestArea = float.MaxValue;
        for (var index = 0; index < _objects.Count; index++)
        {
            var mapObject = _objects[index];
            if (!mapObject.Contains(position))
            {
                continue;
            }

            var area = mapObject.Area;
            if (area < bestArea)
            {
                bestIndex = index;
                bestArea = area;
            }
        }

        return bestIndex;
    }

    private bool SelectRelative(int delta, string operation)
    {
        var total = _markers.Count + _regions.Count + _objects.Count;
        if (total == 0)
        {
            LogWarning(operation, "Cannot cycle selection because no markers, regions, or objects are loaded.");
            return false;
        }

        var currentIndex = SelectionKind switch
        {
            MapEditorSelectionKind.Marker => _selectionIndex,
            MapEditorSelectionKind.Region => _markers.Count + _selectionIndex,
            MapEditorSelectionKind.Object => _markers.Count + _regions.Count + _selectionIndex,
            _ => delta > 0 ? -1 : 0
        };
        var nextIndex = ((currentIndex + delta) % total + total) % total;
        if (nextIndex < _markers.Count)
        {
            _selectionKind = MapEditorSelectionKind.Marker;
            _selectionIndex = nextIndex;
        }
        else if (nextIndex < _markers.Count + _regions.Count)
        {
            _selectionKind = MapEditorSelectionKind.Region;
            _selectionIndex = nextIndex - _markers.Count;
        }
        else
        {
            _selectionKind = MapEditorSelectionKind.Object;
            _selectionIndex = nextIndex - _markers.Count - _regions.Count;
        }

        LogInfo(operation, $"Selected {SelectedSummary}.");
        return true;
    }

    private void AddMarkerContent(string markerId, string contentId)
    {
        var marker = _markers.FirstOrDefault(marker => string.Equals(marker.Id, markerId, StringComparison.Ordinal));
        if (marker is null)
        {
            LogError("load_marker_reference", $"Content '{contentId}' references missing mission marker '{markerId}'.");
            return;
        }

        marker.AddContentId(contentId);
    }

    private void LogDuplicateIds(IEnumerable<string> ids, string label)
    {
        foreach (var group in ids.GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            LogError("load_duplicate_id", $"Duplicate {label} id '{group.Key}' appears {group.Count()} times.");
        }
    }

    private void EmitDiagnostic(
        MapEditorLogLevel level,
        string operation,
        string message,
        string? exceptionType = null,
        string? exceptionMessage = null)
    {
        var entry = new MapEditorLogEntry(
            level,
            operation,
            message,
            MissionId,
            MapId,
            SelectionKind,
            SelectedId,
            _markers.Count,
            _regions.Count,
            _objects.Count,
            exceptionType,
            exceptionMessage);
        _diagnostics.Add(entry);
        if (_diagnostics.Count > MaxDiagnostics)
        {
            _diagnostics.RemoveAt(0);
        }

        DiagnosticEmitted?.Invoke(entry);
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

    private static string FormatShapeSize(string shape, SimVector2 size, float radius)
    {
        return shape == "circle"
            ? $"radius {FormatNumber(radius)}"
            : $"{FormatNumber(size.X)}x{FormatNumber(size.Y)}";
    }

    private static string FormatTags(IReadOnlyList<string> tags)
    {
        return tags.Count == 0 ? "none" : string.Join(", ", tags);
    }

    private static string JsonEscape(string value)
    {
        return value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
    }
}
