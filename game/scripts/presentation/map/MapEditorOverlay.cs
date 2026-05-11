using System.Globalization;
using System.Text;
using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class MapEditorOverlay : Node2D
{
    private const float MarkerHitRadius = 26.0f;
    private const float MarkerDrawRadius = 8.0f;
    private const float LabelFontSize = 13.0f;

    private readonly List<EditableMarker> _markers = [];
    private readonly List<EditableRegion> _regions = [];
    private readonly Dictionary<string, List<string>> _markerContentIds = new(StringComparer.Ordinal);
    private RtsSimulation? _simulation;
    private float _pylonLinkWorldRange;
    private float _powerPlantWorldRange;
    private float _defenseWallWorldRange;
    private string _missionId = string.Empty;
    private string _mapId = string.Empty;
    private SelectionKind _selectionKind = SelectionKind.None;
    private int _selectionIndex = -1;
    private bool _isDragging;
    private Vector2 _dragOffset;

    public bool EditorEnabled { get; private set; }
    public bool IsDragging => _isDragging;

    public string SelectedSummary
    {
        get
        {
            var selectedMarker = SelectedMarker;
            if (selectedMarker is not null)
            {
                return $"{selectedMarker.Id} marker at {FormatVector(selectedMarker.Position)}";
            }

            var selectedRegion = SelectedRegion;
            if (selectedRegion is not null)
            {
                return $"{selectedRegion.Id} {selectedRegion.RegionType} {selectedRegion.Shape} at {FormatVector(selectedRegion.Center)}";
            }

            return "No marker or region selected.";
        }
    }

    private EditableMarker? SelectedMarker => _selectionKind == SelectionKind.Marker &&
        _selectionIndex >= 0 &&
        _selectionIndex < _markers.Count
            ? _markers[_selectionIndex]
            : null;

    private EditableRegion? SelectedRegion => _selectionKind == SelectionKind.Region &&
        _selectionIndex >= 0 &&
        _selectionIndex < _regions.Count
            ? _regions[_selectionIndex]
            : null;

    public void Load(
        MissionDefinition? mission,
        MapDefinition? map,
        ContentCatalog? catalog,
        RtsSimulation? simulation)
    {
        _missionId = mission?.Id ?? string.Empty;
        _mapId = map?.Id ?? string.Empty;
        _simulation = simulation;
        _markers.Clear();
        _regions.Clear();
        _markerContentIds.Clear();
        ClearSelection();

        if (mission is not null)
        {
            foreach (var marker in mission.Markers)
            {
                _markers.Add(new EditableMarker(marker.Id, ToGodot(marker.Position)));
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
                _regions.Add(new EditableRegion(
                    region.Id,
                    region.RegionType,
                    region.Shape,
                    ToGodot(region.Center),
                    ToGodot(region.Size),
                    region.Radius,
                    region.BlocksMovement,
                    region.BlocksBuilding,
                    region.AllowsBuilding,
                    region.Tags.ToArray()));
            }
        }

        if (catalog is not null)
        {
            var pylon = catalog.GetBuilding(ContentIds.Buildings.Pylon);
            var powerPlant = catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
            var defenseTower = catalog.GetBuilding(ContentIds.Buildings.DefenseTower);
            _pylonLinkWorldRange = RtsSimulation.ToWorldRadius(pylon.PylonLinkRange);
            _powerPlantWorldRange = RtsSimulation.ToWorldRadius(powerPlant.PowerRadius);
            _defenseWallWorldRange = RtsSimulation.ToWorldRadius(defenseTower.WallLinkRange);
        }

        QueueRedraw();
    }

    public void SetEditorEnabled(bool enabled)
    {
        EditorEnabled = enabled;
        Visible = enabled;
        SetProcess(enabled);
        if (!enabled)
        {
            _isDragging = false;
        }

        QueueRedraw();
    }

    public bool TryBeginDrag(Vector2 worldPosition)
    {
        SelectAt(worldPosition);

        var selectedMarker = SelectedMarker;
        if (selectedMarker is not null)
        {
            _isDragging = true;
            _dragOffset = worldPosition - selectedMarker.Position;
            QueueRedraw();
            return true;
        }

        var selectedRegion = SelectedRegion;
        if (selectedRegion is not null)
        {
            _isDragging = true;
            _dragOffset = worldPosition - selectedRegion.Center;
            QueueRedraw();
            return true;
        }

        return false;
    }

    public void DragTo(Vector2 worldPosition)
    {
        if (!_isDragging)
        {
            return;
        }

        MoveSelected(worldPosition - _dragOffset);
    }

    public void EndDrag()
    {
        _isDragging = false;
    }

    public bool NudgeSelected(Vector2 delta)
    {
        var selectedMarker = SelectedMarker;
        if (selectedMarker is not null)
        {
            selectedMarker.Position += delta;
            QueueRedraw();
            return true;
        }

        var selectedRegion = SelectedRegion;
        if (selectedRegion is not null)
        {
            selectedRegion.Center += delta;
            QueueRedraw();
            return true;
        }

        return false;
    }

    public string ExportSelectedSnippet()
    {
        var selectedMarker = SelectedMarker;
        if (selectedMarker is not null)
        {
            return FormatMissionMarker(selectedMarker);
        }

        var selectedRegion = SelectedRegion;
        if (selectedRegion is not null)
        {
            return FormatTerrainRegion(selectedRegion);
        }

        return "No map editor selection.";
    }

    public string ExportAllSnippets()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Mission: {_missionId}");
        builder.AppendLine("\"mission_markers\": [");
        for (var index = 0; index < _markers.Count; index++)
        {
            builder.Append(Indent(FormatMissionMarker(_markers[index]), 2));
            builder.AppendLine(index == _markers.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("],");
        builder.AppendLine($"Map: {_mapId}");
        builder.AppendLine("\"terrain_regions\": [");
        for (var index = 0; index < _regions.Count; index++)
        {
            builder.Append(Indent(FormatTerrainRegion(_regions[index]), 2));
            builder.AppendLine(index == _regions.Count - 1 ? string.Empty : ",");
        }

        builder.AppendLine("]");
        return builder.ToString();
    }

    public override void _Draw()
    {
        if (!EditorEnabled)
        {
            return;
        }

        DrawRegions();
        DrawLivePowerAndWalls();
        DrawMarkerWallLinks();
        DrawMarkers();
    }

    private void SelectAt(Vector2 worldPosition)
    {
        ClearSelection();
        var markerIndex = FindMarkerAt(worldPosition);
        if (markerIndex >= 0)
        {
            _selectionKind = SelectionKind.Marker;
            _selectionIndex = markerIndex;
            QueueRedraw();
            return;
        }

        var regionIndex = FindRegionAt(worldPosition);
        if (regionIndex >= 0)
        {
            _selectionKind = SelectionKind.Region;
            _selectionIndex = regionIndex;
            QueueRedraw();
            return;
        }

        QueueRedraw();
    }

    private void ClearSelection()
    {
        _selectionKind = SelectionKind.None;
        _selectionIndex = -1;
        _isDragging = false;
    }

    private void MoveSelected(Vector2 center)
    {
        var selectedMarker = SelectedMarker;
        if (selectedMarker is not null)
        {
            selectedMarker.Position = center;
            QueueRedraw();
            return;
        }

        var selectedRegion = SelectedRegion;
        if (selectedRegion is not null)
        {
            selectedRegion.Center = center;
            QueueRedraw();
        }
    }

    private int FindMarkerAt(Vector2 worldPosition)
    {
        var bestIndex = -1;
        var bestDistance = float.MaxValue;
        for (var index = 0; index < _markers.Count; index++)
        {
            var distance = worldPosition.DistanceTo(_markers[index].Position);
            if (distance <= MarkerHitRadius && distance < bestDistance)
            {
                bestIndex = index;
                bestDistance = distance;
            }
        }

        return bestIndex;
    }

    private int FindRegionAt(Vector2 worldPosition)
    {
        var bestIndex = -1;
        var bestArea = float.MaxValue;
        for (var index = 0; index < _regions.Count; index++)
        {
            var region = _regions[index];
            if (!Contains(region, worldPosition))
            {
                continue;
            }

            var area = region.Shape == "circle"
                ? Mathf.Pi * region.Radius * region.Radius
                : Mathf.Abs(region.Size.X * region.Size.Y);
            if (area < bestArea)
            {
                bestIndex = index;
                bestArea = area;
            }
        }

        return bestIndex;
    }

    private void DrawRegions()
    {
        for (var index = 0; index < _regions.Count; index++)
        {
            var region = _regions[index];
            var selected = _selectionKind == SelectionKind.Region && _selectionIndex == index;
            var fill = RegionFill(region);
            var outline = selected
                ? new Color(1.0f, 0.94f, 0.35f, 0.95f)
                : RegionOutline(region);

            if (region.Shape == "rect")
            {
                var rect = new Rect2(region.Center - (region.Size * 0.5f), region.Size);
                DrawRect(rect, fill);
                DrawRect(rect, outline, false, selected ? 4.0f : 2.0f);
            }
            else if (region.Shape == "circle")
            {
                DrawCircle(region.Center, region.Radius, fill);
                DrawArc(region.Center, region.Radius, 0, Mathf.Tau, 72, outline, selected ? 4.0f : 2.0f);
            }
        }
    }

    private void DrawLivePowerAndWalls()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var well in _simulation.ResourceWells)
        {
            var position = ToGodot(well.Position);
            DrawCircle(position, 28.0f, new Color(0.92f, 0.72f, 0.20f, 0.18f));
            DrawArc(position, 28.0f, 0, Mathf.Tau, 40, new Color(1.0f, 0.82f, 0.25f, 0.88f), 2.0f);
        }

        foreach (var building in _simulation.Buildings.Where(building => !building.IsDestroyed))
        {
            var position = ToGodot(building.Position);
            DrawArc(
                position,
                building.FootprintWorldRadius,
                0,
                Mathf.Tau,
                40,
                building.FactionId == ContentIds.Factions.PlayerExpedition
                    ? new Color(0.40f, 0.78f, 1.0f, 0.60f)
                    : new Color(1.0f, 0.34f, 0.28f, 0.62f),
                1.4f);

            if (building.Definition.Id == ContentIds.Buildings.Pylon && building.Definition.PylonLinkRange > 0.0f)
            {
                DrawRange(position, RtsSimulation.ToWorldRadius(building.Definition.PylonLinkRange), new Color(0.22f, 0.74f, 1.0f, 0.16f));
            }
            else if (building.Definition.ProvidesPower && building.Definition.PowerRadius > 0.0f)
            {
                DrawRange(position, RtsSimulation.ToWorldRadius(building.Definition.PowerRadius), new Color(0.20f, 0.95f, 0.76f, 0.12f));
            }

            if (building.Definition.WallAnchor && building.Definition.WallLinkRange > 0.0f)
            {
                DrawRange(position, RtsSimulation.ToWorldRadius(building.Definition.WallLinkRange), new Color(0.73f, 0.44f, 1.0f, 0.12f));
            }
        }

        foreach (var wall in _simulation.EnergyWalls)
        {
            DrawLine(ToGodot(wall.ExtendedStart), ToGodot(wall.ExtendedEnd), new Color(0.26f, 0.86f, 1.0f, 0.20f), EnergyWallSegment.BlockingClearance * 2.0f);
            DrawLine(ToGodot(wall.Start), ToGodot(wall.End), new Color(0.56f, 0.96f, 1.0f, 0.90f), 4.0f);
        }
    }

    private void DrawMarkerWallLinks()
    {
        if (_defenseWallWorldRange <= 0.0f)
        {
            return;
        }

        var defenseMarkers = _markers
            .Where(marker => MarkerLooksLike(marker, ContentIds.Buildings.DefenseTower, "defense", "tower", "wall"))
            .ToArray();
        for (var leftIndex = 0; leftIndex < defenseMarkers.Length; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < defenseMarkers.Length; rightIndex++)
            {
                var left = defenseMarkers[leftIndex];
                var right = defenseMarkers[rightIndex];
                if (left.Position.DistanceTo(right.Position) > _defenseWallWorldRange)
                {
                    continue;
                }

                DrawLine(left.Position, right.Position, new Color(0.66f, 0.42f, 1.0f, 0.20f), 12.0f);
                DrawLine(left.Position, right.Position, new Color(0.88f, 0.76f, 1.0f, 0.72f), 2.0f);
            }
        }
    }

    private void DrawMarkers()
    {
        for (var index = 0; index < _markers.Count; index++)
        {
            var marker = _markers[index];
            var selected = _selectionKind == SelectionKind.Marker && _selectionIndex == index;
            var color = MarkerColor(marker);
            DrawMarkerRanges(marker);
            DrawCircle(marker.Position, selected ? MarkerDrawRadius + 4.0f : MarkerDrawRadius, color);
            DrawArc(marker.Position, selected ? MarkerDrawRadius + 7.0f : MarkerDrawRadius + 3.0f, 0, Mathf.Tau, 32, new Color(1.0f, 1.0f, 1.0f, selected ? 0.95f : 0.55f), selected ? 3.0f : 1.4f);
            DrawString(
                ThemeDB.FallbackFont,
                marker.Position + new Vector2(12, -10),
                marker.Id,
                HorizontalAlignment.Left,
                -1,
                (int)LabelFontSize,
                new Color(0.94f, 0.98f, 1.0f, selected ? 1.0f : 0.78f));
        }
    }

    private void DrawMarkerRanges(EditableMarker marker)
    {
        if (_pylonLinkWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.Pylon, "pylon"))
        {
            DrawRange(marker.Position, _pylonLinkWorldRange, new Color(0.26f, 0.76f, 1.0f, 0.18f));
        }

        if (_powerPlantWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.PowerPlant, "power"))
        {
            DrawRange(marker.Position, _powerPlantWorldRange, new Color(0.20f, 0.95f, 0.76f, 0.14f));
        }

        if (_defenseWallWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.DefenseTower, "defense", "tower", "wall"))
        {
            DrawRange(marker.Position, _defenseWallWorldRange, new Color(0.72f, 0.42f, 1.0f, 0.14f));
        }
    }

    private void DrawRange(Vector2 center, float radius, Color color)
    {
        DrawArc(center, radius, 0, Mathf.Tau, 96, color, 2.0f);
    }

    private bool MarkerLooksLike(EditableMarker marker, string contentId, params string[] nameFragments)
    {
        if (_markerContentIds.TryGetValue(marker.Id, out var contentIds) &&
            contentIds.Any(id => string.Equals(id, contentId, StringComparison.Ordinal)))
        {
            return true;
        }

        return nameFragments.Any(fragment => marker.Id.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private void AddMarkerContent(string markerId, string contentId)
    {
        if (!_markerContentIds.TryGetValue(markerId, out var contentIds))
        {
            contentIds = [];
            _markerContentIds.Add(markerId, contentIds);
        }

        contentIds.Add(contentId);
    }

    private static bool Contains(EditableRegion region, Vector2 position)
    {
        if (region.Shape == "circle")
        {
            return region.Center.DistanceTo(position) <= region.Radius;
        }

        if (region.Shape != "rect")
        {
            return false;
        }

        var half = region.Size * 0.5f;
        return Mathf.Abs(position.X - region.Center.X) <= half.X &&
            Mathf.Abs(position.Y - region.Center.Y) <= half.Y;
    }

    private static string FormatMissionMarker(EditableMarker marker)
    {
        return $$"""
        {
          "id": "{{JsonEscape(marker.Id)}}",
          "position": { "x": {{FormatNumber(marker.Position.X)}}, "y": {{FormatNumber(marker.Position.Y)}} }
        }
        """;
    }

    private static string FormatTerrainRegion(EditableRegion region)
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

    private static string Indent(string value, int spaces)
    {
        var prefix = new string(' ', spaces);
        return string.Join(
            System.Environment.NewLine,
            value.Split(System.Environment.NewLine).Select(line => line.Length == 0 ? line : prefix + line));
    }

    private static Color MarkerColor(EditableMarker marker)
    {
        if (marker.Id.Contains("enemy", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(1.0f, 0.28f, 0.24f, 0.90f);
        }

        if (marker.Id.Contains("well", StringComparison.OrdinalIgnoreCase) ||
            marker.Id.Contains("extractor", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(1.0f, 0.76f, 0.20f, 0.90f);
        }

        if (marker.Id.Contains("pylon", StringComparison.OrdinalIgnoreCase) ||
            marker.Id.Contains("power", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(0.20f, 0.78f, 1.0f, 0.90f);
        }

        if (marker.Id.Contains("defense", StringComparison.OrdinalIgnoreCase) ||
            marker.Id.Contains("tower", StringComparison.OrdinalIgnoreCase) ||
            marker.Id.Contains("wall", StringComparison.OrdinalIgnoreCase))
        {
            return new Color(0.76f, 0.48f, 1.0f, 0.90f);
        }

        return new Color(0.45f, 0.96f, 0.66f, 0.90f);
    }

    private static Color RegionFill(EditableRegion region)
    {
        if (region.BlocksMovement || region.BlocksBuilding)
        {
            return new Color(0.19f, 0.21f, 0.23f, 0.42f);
        }

        return region.RegionType switch
        {
            "resource_basin" => new Color(0.82f, 0.64f, 0.20f, 0.16f),
            "buildable_clearing" => new Color(0.20f, 0.62f, 0.34f, 0.13f),
            "chokepoint_marker" => new Color(0.22f, 0.72f, 1.0f, 0.11f),
            _ => new Color(0.50f, 0.54f, 0.58f, 0.10f)
        };
    }

    private static Color RegionOutline(EditableRegion region)
    {
        if (region.BlocksMovement || region.BlocksBuilding)
        {
            return new Color(0.72f, 0.76f, 0.80f, 0.70f);
        }

        return region.RegionType switch
        {
            "resource_basin" => new Color(1.0f, 0.76f, 0.22f, 0.78f),
            "buildable_clearing" => new Color(0.46f, 0.96f, 0.54f, 0.62f),
            "chokepoint_marker" => new Color(0.38f, 0.86f, 1.0f, 0.68f),
            _ => new Color(0.68f, 0.72f, 0.76f, 0.58f)
        };
    }

    private static string FormatVector(Vector2 vector)
    {
        return $"({FormatNumber(vector.X)}, {FormatNumber(vector.Y)})";
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

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private enum SelectionKind
    {
        None,
        Marker,
        Region
    }

    private sealed class EditableMarker(string id, Vector2 position)
    {
        public string Id { get; } = id;
        public Vector2 Position { get; set; } = position;
    }

    private sealed class EditableRegion(
        string id,
        string regionType,
        string shape,
        Vector2 center,
        Vector2 size,
        float radius,
        bool blocksMovement,
        bool blocksBuilding,
        bool allowsBuilding,
        IReadOnlyList<string> tags)
    {
        public string Id { get; } = id;
        public string RegionType { get; } = regionType;
        public string Shape { get; } = shape;
        public Vector2 Center { get; set; } = center;
        public Vector2 Size { get; } = size;
        public float Radius { get; } = radius;
        public bool BlocksMovement { get; } = blocksMovement;
        public bool BlocksBuilding { get; } = blocksBuilding;
        public bool AllowsBuilding { get; } = allowsBuilding;
        public IReadOnlyList<string> Tags { get; } = tags;
    }
}
