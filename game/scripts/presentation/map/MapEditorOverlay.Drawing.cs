using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Simulation.Tools;

public partial class MapEditorOverlay
{
    public override void _Draw()
    {
        if (!EditorEnabled)
        {
            return;
        }

        DrawPathingDebugLayer();
        DrawRegions();
        DrawMapObjects();
        DrawLivePowerAndWalls();
        DrawMarkerWallLinks();
        DrawMarkers();
        DrawSelectionHandles();
        DrawCreationPreview();
        DrawPointerDimensionHint();
    }

    private void CompleteShapeCreation(Vector2 endWorld)
    {
        var start = _createStartWorld;
        if (_toolMode == MapEditorToolMode.AddCircleRegion)
        {
            var radius = start.DistanceTo(endWorld);
            if (radius < 10.0f)
            {
                _session.LogWarning("add_circle_region", "Circle region drag was too small; no region was created.");
                return;
            }

            _session.AddRegion("circle", ToSim(start), new SimVector2(radius * 2.0f, radius * 2.0f), radius);
            return;
        }

        var size = new Vector2(MathF.Abs(endWorld.X - start.X), MathF.Abs(endWorld.Y - start.Y));
        if (size.X < 10.0f || size.Y < 10.0f)
        {
            _session.LogWarning("add_rect_region", "Rect region drag was too small; no region was created.");
            return;
        }

        var center = (start + endWorld) * 0.5f;
        _session.AddRegion("rect", ToSim(center), ToSim(size), 0.0f);
    }

    private Vector2 Snap(Vector2 worldPosition)
    {
        if (!_snapEnabled)
        {
            return worldPosition;
        }

        return new Vector2(Mathf.Round(worldPosition.X / 10.0f) * 10.0f, Mathf.Round(worldPosition.Y / 10.0f) * 10.0f);
    }

    private void DrawCreationPreview()
    {
        if (!_isCreatingShape)
        {
            return;
        }

        var start = _createStartWorld;
        var end = _createCurrentWorld;
        var fill = new Color(0.36f, 0.68f, 1.0f, 0.13f);
        var outline = new Color(0.74f, 0.92f, 1.0f, 0.82f);
        if (_toolMode == MapEditorToolMode.AddCircleRegion)
        {
            var radius = start.DistanceTo(end);
            DrawCircle(start, radius, fill);
            DrawArc(start, radius, 0, Mathf.Tau, 72, outline, 2.0f);
            return;
        }

        var topLeft = new Vector2(MathF.Min(start.X, end.X), MathF.Min(start.Y, end.Y));
        var size = new Vector2(MathF.Abs(end.X - start.X), MathF.Abs(end.Y - start.Y));
        var rect = new Rect2(topLeft, size);
        DrawRect(rect, fill);
        DrawRect(rect, outline, false, 2.0f);
    }

    private void DrawSelectionHandles()
    {
        if (_session.SelectedRegion is not null)
        {
            DrawShapeHandles(_session.SelectedRegion.Shape, ToGodot(_session.SelectedRegion.Center), ToGodot(_session.SelectedRegion.Size), _session.SelectedRegion.Radius);
            return;
        }

        if (_session.SelectedObject is not null)
        {
            DrawShapeHandles(_session.SelectedObject.Shape, ToGodot(_session.SelectedObject.Center), ToGodot(_session.SelectedObject.Size), _session.SelectedObject.Radius);
        }
    }

    private void DrawShapeHandles(string shape, Vector2 center, Vector2 size, float radius)
    {
        var color = new Color(1.0f, 0.94f, 0.35f, 0.96f);
        if (shape == "circle")
        {
            DrawHandle(center + new Vector2(radius, 0), color);
            return;
        }

        if (shape != "rect")
        {
            return;
        }

        var halfWidth = size.X * 0.5f;
        var halfHeight = size.Y * 0.5f;
        DrawHandle(center + new Vector2(-halfWidth, 0), color);
        DrawHandle(center + new Vector2(halfWidth, 0), color);
        DrawHandle(center + new Vector2(0, -halfHeight), color);
        DrawHandle(center + new Vector2(0, halfHeight), color);
    }

    private void DrawHandle(Vector2 center, Color color)
    {
        var rect = new Rect2(center - new Vector2(HandleDrawSize * 0.5f, HandleDrawSize * 0.5f), new Vector2(HandleDrawSize, HandleDrawSize));
        DrawRect(rect, new Color(0.02f, 0.025f, 0.03f, 0.86f));
        DrawRect(rect, color, false, 2.0f);
    }

    private void DrawPointerDimensionHint()
    {
        var text = PointerDimensionText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var position = _lastPointerWorld + new Vector2(14, -14);
        DrawRect(new Rect2(position + new Vector2(-6, -16), new Vector2(162, 22)), new Color(0.02f, 0.025f, 0.03f, 0.74f));
        DrawString(
            ThemeDB.FallbackFont,
            position,
            text,
            HorizontalAlignment.Left,
            -1,
            12,
            new Color(0.94f, 0.98f, 1.0f, 0.94f));
    }

    private string PointerDimensionText()
    {
        if (_isCreatingShape)
        {
            if (_toolMode == MapEditorToolMode.AddCircleRegion)
            {
                return $"r {FormatFloat(_createStartWorld.DistanceTo(_createCurrentWorld))}";
            }

            return $"{FormatFloat(MathF.Abs(_createCurrentWorld.X - _createStartWorld.X))} x {FormatFloat(MathF.Abs(_createCurrentWorld.Y - _createStartWorld.Y))}";
        }

        if (_activeResizeHandle != MapEditorResizeHandle.None)
        {
            return _session.SelectedShapeSizeSummary();
        }

        return string.Empty;
    }

    private void DrawPathingDebugLayer()
    {
        if (!PathingDebugEnabled || _simulation is null)
        {
            return;
        }

        var cells = PathfindingSystem.SampleBlockedCells(
            _simulation.Buildings,
            _simulation.EnergyWalls,
            _simulation.Map?.TerrainRegions ?? [],
            _simulation.Bridges);
        foreach (var cell in cells)
        {
            var size = new Vector2(cell.Size, cell.Size);
            var rect = new Rect2(ToGodot(cell.Center) - (size * 0.5f), size);
            DrawRect(rect, new Color(0.03f, 0.03f, 0.04f, 0.22f));
        }
    }

    private void DrawRegions()
    {
        foreach (var region in _session.Regions)
        {
            var selected = _session.SelectionKind == MapEditorSelectionKind.Region &&
                string.Equals(_session.SelectedId, region.Id, StringComparison.Ordinal);
            var hovered = IsHovered(MapEditorSelectionKind.Region, region.Id);
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
                if (hovered)
                {
                    DrawRect(rect.Grow(2.0f), UiPalette.AccentCommand, false, 1.0f);
                }
            }
            else if (region.Shape == "circle")
            {
                DrawCircle(ToGodot(region.Center), region.Radius, fill);
                DrawArc(ToGodot(region.Center), region.Radius, 0, Mathf.Tau, 72, outline, selected ? 4.0f : 2.0f);
                if (hovered)
                {
                    DrawArc(ToGodot(region.Center), region.Radius + 2.0f, 0, Mathf.Tau, 72, UiPalette.AccentCommand, 1.0f);
                }
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

    private void DrawMapObjects()
    {
        foreach (var mapObject in _session.Objects)
        {
            var selected = _session.SelectionKind == MapEditorSelectionKind.Object &&
                string.Equals(_session.SelectedId, mapObject.Id, StringComparison.Ordinal);
            var hovered = IsHovered(MapEditorSelectionKind.Object, mapObject.Id);
            var runtimeBridge = _simulation?.Bridges.FirstOrDefault(bridge => string.Equals(bridge.Id, mapObject.Id, StringComparison.Ordinal));
            var intact = runtimeBridge?.IsIntact ?? mapObject.StartsIntact;
            var fill = intact
                ? new Color(0.54f, 0.45f, 0.34f, 0.42f)
                : new Color(0.35f, 0.14f, 0.10f, 0.48f);
            var outline = selected
                ? new Color(1.0f, 0.94f, 0.35f, 0.96f)
                : intact
                    ? new Color(0.92f, 0.74f, 0.46f, 0.90f)
                    : new Color(1.0f, 0.30f, 0.18f, 0.92f);

            if (mapObject.Shape == "rect")
            {
                var size = ToGodot(mapObject.Size);
                var rect = new Rect2(ToGodot(mapObject.Center) - (size * 0.5f), size);
                DrawRect(rect, fill);
                DrawRect(rect, outline, false, selected ? 4.0f : 2.0f);
                if (hovered)
                {
                    DrawRect(rect.Grow(2.0f), UiPalette.AccentCommand, false, 1.0f);
                }
            }
            else if (mapObject.Shape == "circle")
            {
                DrawCircle(ToGodot(mapObject.Center), mapObject.Radius, fill);
                DrawArc(ToGodot(mapObject.Center), mapObject.Radius, 0, Mathf.Tau, 72, outline, selected ? 4.0f : 2.0f);
                if (hovered)
                {
                    DrawArc(ToGodot(mapObject.Center), mapObject.Radius + 2.0f, 0, Mathf.Tau, 72, UiPalette.AccentCommand, 1.0f);
                }
            }

            var healthText = runtimeBridge is null
                ? mapObject.Id
                : $"{mapObject.Id} {runtimeBridge.CurrentHealth:0}/{runtimeBridge.MaxHealth:0}";
            DrawString(
                ThemeDB.FallbackFont,
                ToGodot(mapObject.Center) + new Vector2(10, 18),
                healthText,
                HorizontalAlignment.Left,
                -1,
                (int)LabelFontSize,
                new Color(1.0f, 0.92f, 0.72f, selected ? 1.0f : 0.82f));
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
            var hovered = IsHovered(MapEditorSelectionKind.Marker, marker.Id);
            var color = MarkerColor(marker);
            var position = ToGodot(marker.Position);
            DrawMarkerRanges(marker);
            DrawCircle(position, selected ? MarkerDrawRadius + 4.0f : MarkerDrawRadius, color);
            DrawArc(position, selected ? MarkerDrawRadius + 7.0f : MarkerDrawRadius + 3.0f, 0, Mathf.Tau, 32, new Color(1.0f, 1.0f, 1.0f, selected ? 0.95f : 0.55f), selected ? 3.0f : 1.4f);
            if (hovered)
            {
                DrawArc(position, selected ? MarkerDrawRadius + 10.0f : MarkerDrawRadius + 6.0f, 0, Mathf.Tau, 32, UiPalette.AccentCommand, 1.0f);
            }

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

    private void DrawLegend()
    {
        var origin = GetViewportTransform().AffineInverse() * new Vector2(18, 18);
        var lines = new[]
        {
            $"F5 editor | tool: {_toolMode} | snap: {(_snapEnabled ? "10" : "off")}",
            "Gray: blocked terrain",
            "Green: buildable hint",
            "Gold: resource basin/well",
            "Blue: chokepoint/lane",
            "Tan/red: bridge intact/broken",
            PathingDebugEnabled ? "P: pathing blocked layer ON" : "P: pathing blocked layer off"
        };

        var lineHeight = 16.0f;
        var width = 240.0f;
        var height = (lines.Length * lineHeight) + 14.0f;
        DrawRect(new Rect2(origin - new Vector2(8, 16), new Vector2(width, height)), new Color(0.02f, 0.025f, 0.03f, 0.68f));
        for (var index = 0; index < lines.Length; index++)
        {
            DrawString(
                ThemeDB.FallbackFont,
                origin + new Vector2(0, index * lineHeight),
                lines[index],
                HorizontalAlignment.Left,
                -1,
                12,
                new Color(0.90f, 0.96f, 1.0f, 0.90f));
        }
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

}
