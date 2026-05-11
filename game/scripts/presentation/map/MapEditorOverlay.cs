using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Simulation.Tools;

public partial class MapEditorOverlay : Node2D
{
    private const float MarkerHitRadius = 26.0f;
    private const float MarkerDrawRadius = 8.0f;
    private const float LabelFontSize = 13.0f;

    private readonly MapEditorSession _session = new();
    private RtsSimulation? _simulation;
    private float _pylonLinkWorldRange;
    private float _powerPlantWorldRange;
    private float _defenseWallWorldRange;
    private bool _isDragging;
    private Vector2 _dragOffset;

    public bool EditorEnabled { get; private set; }
    public bool IsDragging => _isDragging;
    public string SelectedSummary => _session.SelectedSummary;

    public void Load(
        MissionDefinition? mission,
        MapDefinition? map,
        ContentCatalog? catalog,
        RtsSimulation? simulation)
    {
        _simulation = simulation;
        _session.Load(mission, map);
        _isDragging = false;

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
        _session.SelectAt(ToSim(worldPosition), MarkerHitRadius);
        var selectedCenter = _session.SelectedCenter;
        if (selectedCenter is not null)
        {
            _isDragging = true;
            _dragOffset = worldPosition - ToGodot(selectedCenter.Value);
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

        _session.MoveSelectedTo(ToSim(worldPosition - _dragOffset));
        QueueRedraw();
    }

    public void EndDrag()
    {
        _isDragging = false;
    }

    public bool NudgeSelected(Vector2 delta)
    {
        if (_session.NudgeSelected(ToSim(delta)))
        {
            QueueRedraw();
            return true;
        }

        return false;
    }

    public string ExportSelectedSnippet()
    {
        return _session.ExportSelectedSnippet();
    }

    public string ExportAllSnippets()
    {
        return _session.ExportAllSnippets();
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

    private void DrawRegions()
    {
        foreach (var region in _session.Regions)
        {
            var selected = _session.SelectionKind == MapEditorSelectionKind.Region &&
                string.Equals(_session.SelectedId, region.Id, StringComparison.Ordinal);
            var fill = RegionFill(region);
            var outline = selected
                ? new Color(1.0f, 0.94f, 0.35f, 0.95f)
                : RegionOutline(region);

            if (region.Shape == "rect")
            {
                var size = ToGodot(region.Size);
                var rect = new Rect2(ToGodot(region.Center) - (size * 0.5f), size);
                DrawRect(rect, fill);
                DrawRect(rect, outline, false, selected ? 4.0f : 2.0f);
            }
            else if (region.Shape == "circle")
            {
                DrawCircle(ToGodot(region.Center), region.Radius, fill);
                DrawArc(ToGodot(region.Center), region.Radius, 0, Mathf.Tau, 72, outline, selected ? 4.0f : 2.0f);
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

        var defenseMarkers = _session.Markers
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

                DrawLine(ToGodot(left.Position), ToGodot(right.Position), new Color(0.66f, 0.42f, 1.0f, 0.20f), 12.0f);
                DrawLine(ToGodot(left.Position), ToGodot(right.Position), new Color(0.88f, 0.76f, 1.0f, 0.72f), 2.0f);
            }
        }
    }

    private void DrawMarkers()
    {
        foreach (var marker in _session.Markers)
        {
            var selected = _session.SelectionKind == MapEditorSelectionKind.Marker &&
                string.Equals(_session.SelectedId, marker.Id, StringComparison.Ordinal);
            var color = MarkerColor(marker);
            var position = ToGodot(marker.Position);
            DrawMarkerRanges(marker);
            DrawCircle(position, selected ? MarkerDrawRadius + 4.0f : MarkerDrawRadius, color);
            DrawArc(position, selected ? MarkerDrawRadius + 7.0f : MarkerDrawRadius + 3.0f, 0, Mathf.Tau, 32, new Color(1.0f, 1.0f, 1.0f, selected ? 0.95f : 0.55f), selected ? 3.0f : 1.4f);
            DrawString(
                ThemeDB.FallbackFont,
                position + new Vector2(12, -10),
                marker.Id,
                HorizontalAlignment.Left,
                -1,
                (int)LabelFontSize,
                new Color(0.94f, 0.98f, 1.0f, selected ? 1.0f : 0.78f));
        }
    }

    private void DrawMarkerRanges(MapEditorMarker marker)
    {
        var position = ToGodot(marker.Position);
        if (_pylonLinkWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.Pylon, "pylon"))
        {
            DrawRange(position, _pylonLinkWorldRange, new Color(0.26f, 0.76f, 1.0f, 0.18f));
        }

        if (_powerPlantWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.PowerPlant, "power"))
        {
            DrawRange(position, _powerPlantWorldRange, new Color(0.20f, 0.95f, 0.76f, 0.14f));
        }

        if (_defenseWallWorldRange > 0.0f && MarkerLooksLike(marker, ContentIds.Buildings.DefenseTower, "defense", "tower", "wall"))
        {
            DrawRange(position, _defenseWallWorldRange, new Color(0.72f, 0.42f, 1.0f, 0.14f));
        }
    }

    private void DrawRange(Vector2 center, float radius, Color color)
    {
        DrawArc(center, radius, 0, Mathf.Tau, 96, color, 2.0f);
    }

    private static bool MarkerLooksLike(MapEditorMarker marker, string contentId, params string[] nameFragments)
    {
        if (marker.ContentIds.Any(id => string.Equals(id, contentId, StringComparison.Ordinal)))
        {
            return true;
        }

        return nameFragments.Any(fragment => marker.Id.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static Color MarkerColor(MapEditorMarker marker)
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

    private static Color RegionFill(MapEditorRegion region)
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

    private static Color RegionOutline(MapEditorRegion region)
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

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private static SimVector2 ToSim(Vector2 vector)
    {
        return new SimVector2(vector.X, vector.Y);
    }
}
