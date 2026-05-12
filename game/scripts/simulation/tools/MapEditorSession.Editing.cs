using Stratezone.Simulation;

namespace Stratezone.Simulation.Tools;

public sealed partial class MapEditorSession
{
    private const float MinShapeDimension = 20.0f;
    private const float DuplicateOffset = 40.0f;
    private const int UndoLimit = 50;

    private static readonly string[] KnownRegionTypesValue =
    [
        "blocked_terrain",
        "buildable_clearing",
        "resource_basin",
        "chokepoint_marker"
    ];

    private readonly List<MapEditorSnapshot> _undoStack = [];
    private readonly List<MapEditorSnapshot> _redoStack = [];
    private MapEditorSnapshot? _activeEditSnapshot;
    private int _activeEditStartRevision;

    public static IReadOnlyList<string> KnownRegionTypes => KnownRegionTypesValue;
    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void BeginEditAction()
    {
        if (_activeEditSnapshot is not null)
        {
            return;
        }

        _activeEditSnapshot = CreateSnapshot();
        _activeEditStartRevision = Revision;
    }

    public void CommitEditAction()
    {
        if (_activeEditSnapshot is null)
        {
            return;
        }

        if (Revision != _activeEditStartRevision)
        {
            PushUndoSnapshot(_activeEditSnapshot);
            _redoStack.Clear();
        }

        _activeEditSnapshot = null;
    }

    public void CancelEditAction()
    {
        _activeEditSnapshot = null;
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            LogInfo("undo", "Nothing to undo.");
            return false;
        }

        _redoStack.Add(CreateSnapshot());
        var snapshot = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        RestoreSnapshot(snapshot);
        Revision++;
        LogInfo("undo", $"Undo applied. Revision {Revision}.");
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            LogInfo("redo", "Nothing to redo.");
            return false;
        }

        _undoStack.Add(CreateSnapshot());
        var snapshot = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        RestoreSnapshot(snapshot);
        Revision++;
        LogInfo("redo", $"Redo applied. Revision {Revision}.");
        return true;
    }

    public bool SelectObject(string id)
    {
        var index = _objects.FindIndex(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (index < 0)
        {
            LogWarning("select_object", $"Map object '{id}' does not exist in map '{MapId}'.");
            return false;
        }

        _selectionKind = MapEditorSelectionKind.Object;
        _selectionIndex = index;
        return true;
    }

    public MapEditorMarker AddMarkerAt(SimVector2 position)
    {
        PrepareChange();
        var marker = new MapEditorMarker(UniqueId("marker_new", AllIds()), position);
        _markers.Add(marker);
        _selectionKind = MapEditorSelectionKind.Marker;
        _selectionIndex = _markers.Count - 1;
        Revision++;
        LogInfo("add_marker", $"Added marker '{marker.Id}' at {FormatVector(position)}.");
        return marker;
    }

    public MapEditorRegion AddRegion(string shape, SimVector2 center, SimVector2 size, float radius)
    {
        PrepareChange();
        var normalizedShape = NormalizeShape(shape);
        var region = new MapEditorRegion(
            UniqueId($"{DefaultRegionTypeForCreation()}_{normalizedShape}", AllIds()),
            DefaultRegionTypeForCreation(),
            normalizedShape,
            center,
            ClampSize(size),
            MathF.Max(MinShapeDimension * 0.5f, radius),
            blocksMovement: true,
            blocksBuilding: true,
            allowsBuilding: false,
            [DefaultRegionTypeForCreation()]);
        _regions.Add(region);
        _selectionKind = MapEditorSelectionKind.Region;
        _selectionIndex = _regions.Count - 1;
        Revision++;
        LogInfo("add_region", $"Added {normalizedShape} region '{region.Id}' at {FormatVector(center)}.");
        return region;
    }

    public MapEditorEditResult DuplicateSelected()
    {
        if (SelectedMarker is not null)
        {
            PrepareChange();
            var original = SelectedMarker;
            var clone = new MapEditorMarker(
                UniqueId($"{original.Id}_copy", AllIds()),
                original.Position + new SimVector2(DuplicateOffset, DuplicateOffset),
                original.Tags.ToArray());
            foreach (var contentId in original.ContentIds)
            {
                clone.AddContentId(contentId);
            }

            _markers.Add(clone);
            _selectionKind = MapEditorSelectionKind.Marker;
            _selectionIndex = _markers.Count - 1;
            Revision++;
            return MapEditorEditResult.ChangedResult($"Duplicated marker '{original.Id}' as '{clone.Id}'.");
        }

        if (SelectedRegion is not null)
        {
            PrepareChange();
            var original = SelectedRegion;
            var clone = new MapEditorRegion(
                UniqueId($"{original.Id}_copy", AllIds()),
                original.RegionType,
                original.Shape,
                original.Center + new SimVector2(DuplicateOffset, DuplicateOffset),
                original.Size,
                original.Radius,
                original.BlocksMovement,
                original.BlocksBuilding,
                original.AllowsBuilding,
                original.Tags.ToArray());
            _regions.Add(clone);
            _selectionKind = MapEditorSelectionKind.Region;
            _selectionIndex = _regions.Count - 1;
            Revision++;
            return MapEditorEditResult.ChangedResult($"Duplicated region '{original.Id}' as '{clone.Id}'.");
        }

        if (SelectedObject is not null)
        {
            PrepareChange();
            var original = SelectedObject;
            var clone = new MapEditorObject(
                UniqueId($"{original.Id}_copy", AllIds()),
                original.ObjectType,
                original.Shape,
                original.Center + new SimVector2(DuplicateOffset, DuplicateOffset),
                original.Size,
                original.Radius,
                original.MaxHealth,
                original.StartsIntact,
                original.BlocksMovementWhenBroken,
                original.Tags.ToArray());
            _objects.Add(clone);
            _selectionKind = MapEditorSelectionKind.Object;
            _selectionIndex = _objects.Count - 1;
            Revision++;
            return MapEditorEditResult.ChangedResult($"Duplicated map object '{original.Id}' as '{clone.Id}'.");
        }

        return MapEditorEditResult.Failed("Select a marker, region, or map object before duplicating.");
    }

    public MapEditorDeletePreview? CreateDeletePreview()
    {
        if (SelectedId is null)
        {
            return null;
        }

        return new MapEditorDeletePreview(SelectionKind, SelectedId, ReferencesFor(SelectionKind, SelectedId));
    }

    public MapEditorEditResult DeleteSelected()
    {
        var preview = CreateDeletePreview();
        if (preview is null)
        {
            return MapEditorEditResult.Failed("Select a marker, region, or map object before deleting.");
        }

        PrepareChange();
        switch (SelectionKind)
        {
            case MapEditorSelectionKind.Marker:
                _markers.RemoveAt(_selectionIndex);
                break;
            case MapEditorSelectionKind.Region:
                _regions.RemoveAt(_selectionIndex);
                break;
            case MapEditorSelectionKind.Object:
                _objects.RemoveAt(_selectionIndex);
                break;
            default:
                return MapEditorEditResult.Failed("No map editor selection to delete.");
        }

        ClearSelection();
        Revision++;
        var warnings = preview.References.Count == 0
            ? []
            : new[] { $"Deleted '{preview.SelectionId}' with orphaned references: {string.Join(", ", preview.References)}." };
        return MapEditorEditResult.ChangedResult($"Deleted {preview.SelectionId}.", warnings);
    }

    public MapEditorEditResult RenameSelected(string newId)
    {
        newId = NormalizeId(newId);
        if (string.IsNullOrWhiteSpace(newId))
        {
            return MapEditorEditResult.Failed("Id cannot be empty.");
        }

        var selectedId = SelectedId;
        if (selectedId is null)
        {
            return MapEditorEditResult.Failed("Select a marker, region, or map object before renaming.");
        }

        if (string.Equals(selectedId, newId, StringComparison.Ordinal))
        {
            return MapEditorEditResult.Unchanged("Id unchanged.");
        }

        if (AllIds().Any(id => string.Equals(id, newId, StringComparison.Ordinal)))
        {
            return MapEditorEditResult.Failed($"Id '{newId}' already exists.");
        }

        PrepareChange();
        var references = ReferencesFor(SelectionKind, selectedId);
        if (SelectedMarker is not null)
        {
            SelectedMarker.Id = newId;
        }
        else if (SelectedRegion is not null)
        {
            SelectedRegion.Id = newId;
        }
        else if (SelectedObject is not null)
        {
            SelectedObject.Id = newId;
        }

        Revision++;
        var warnings = references.Count == 0
            ? []
            : new[] { $"Renamed '{selectedId}' to '{newId}'. Existing references still point at '{selectedId}': {string.Join(", ", references)}." };
        return MapEditorEditResult.ChangedResult($"Renamed '{selectedId}' to '{newId}'.", warnings);
    }

    public MapEditorEditResult SetSelectedCenter(SimVector2 center)
    {
        return MoveSelectedTo(center)
            ? MapEditorEditResult.ChangedResult($"Moved selection to {FormatVector(center)}.")
            : MapEditorEditResult.Failed("No selection to move.");
    }

    public MapEditorEditResult SetSelectedRegionType(string regionType)
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Select a region before changing region type.");
        }

        regionType = NormalizeId(regionType);
        if (string.IsNullOrWhiteSpace(regionType))
        {
            return MapEditorEditResult.Failed("Region type cannot be empty.");
        }

        if (string.Equals(SelectedRegion.RegionType, regionType, StringComparison.Ordinal))
        {
            return MapEditorEditResult.Unchanged("Region type unchanged.");
        }

        PrepareChange();
        ApplyRegionTypePreset(SelectedRegion, regionType);
        Revision++;
        return MapEditorEditResult.ChangedResult($"Changed region type to '{regionType}'.");
    }

    public MapEditorEditResult CycleSelectedRegionType()
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Change region type only applies to terrain regions.");
        }

        var currentIndex = Array.FindIndex(KnownRegionTypesValue, item => string.Equals(item, SelectedRegion.RegionType, StringComparison.Ordinal));
        var next = KnownRegionTypesValue[(currentIndex + 1 + KnownRegionTypesValue.Length) % KnownRegionTypesValue.Length];
        return SetSelectedRegionType(next);
    }

    public MapEditorEditResult SetSelectedRegionShape(string shape)
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Select a region before changing shape.");
        }

        shape = NormalizeShape(shape);
        if (string.Equals(SelectedRegion.Shape, shape, StringComparison.Ordinal))
        {
            return MapEditorEditResult.Unchanged("Region shape unchanged.");
        }

        PrepareChange();
        if (shape == "circle")
        {
            SelectedRegion.Radius = MathF.Max(MinShapeDimension * 0.5f, MathF.Max(SelectedRegion.Size.X, SelectedRegion.Size.Y) * 0.5f);
        }
        else
        {
            var diameter = MathF.Max(MinShapeDimension, SelectedRegion.Radius * 2.0f);
            SelectedRegion.Size = new SimVector2(diameter, diameter);
        }

        SelectedRegion.Shape = shape;
        Revision++;
        return MapEditorEditResult.ChangedResult($"Changed region shape to '{shape}'.");
    }

    public MapEditorEditResult SetSelectedRegionSize(SimVector2 size)
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Select a region before changing size.");
        }

        var clamped = ClampSize(size);
        if (SelectedRegion.Size == clamped)
        {
            return MapEditorEditResult.Unchanged("Region size unchanged.");
        }

        PrepareChange();
        SelectedRegion.Shape = "rect";
        SelectedRegion.Size = clamped;
        Revision++;
        return MapEditorEditResult.ChangedResult($"Set region size to {FormatNumber(clamped.X)}x{FormatNumber(clamped.Y)}.");
    }

    public MapEditorEditResult SetSelectedRegionRadius(float radius)
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Select a region before changing radius.");
        }

        var clamped = MathF.Max(MinShapeDimension * 0.5f, radius);
        if (MathF.Abs(SelectedRegion.Radius - clamped) < 0.001f)
        {
            return MapEditorEditResult.Unchanged("Region radius unchanged.");
        }

        PrepareChange();
        SelectedRegion.Shape = "circle";
        SelectedRegion.Radius = clamped;
        Revision++;
        return MapEditorEditResult.ChangedResult($"Set region radius to {FormatNumber(clamped)}.");
    }

    public MapEditorEditResult SetSelectedRegionFlags(bool blocksMovement, bool blocksBuilding, bool allowsBuilding)
    {
        if (SelectedRegion is null)
        {
            return MapEditorEditResult.Failed("Select a region before changing flags.");
        }

        if (SelectedRegion.BlocksMovement == blocksMovement &&
            SelectedRegion.BlocksBuilding == blocksBuilding &&
            SelectedRegion.AllowsBuilding == allowsBuilding)
        {
            return MapEditorEditResult.Unchanged("Region flags unchanged.");
        }

        PrepareChange();
        SelectedRegion.BlocksMovement = blocksMovement;
        SelectedRegion.BlocksBuilding = blocksBuilding;
        SelectedRegion.AllowsBuilding = allowsBuilding;
        Revision++;
        return MapEditorEditResult.ChangedResult("Updated region flags.");
    }

    public MapEditorEditResult SetSelectedTags(IReadOnlyList<string> tags)
    {
        var normalizedTags = tags
            .Select(NormalizeId)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (SelectedRegion is not null)
        {
            if (SelectedRegion.Tags.SequenceEqual(normalizedTags, StringComparer.Ordinal))
            {
                return MapEditorEditResult.Unchanged("Tags unchanged.");
            }

            PrepareChange();
            SelectedRegion.Tags = normalizedTags;
            Revision++;
            return MapEditorEditResult.ChangedResult("Updated region tags.");
        }

        if (SelectedObject is not null)
        {
            if (SelectedObject.Tags.SequenceEqual(normalizedTags, StringComparer.Ordinal))
            {
                return MapEditorEditResult.Unchanged("Tags unchanged.");
            }

            PrepareChange();
            SelectedObject.Tags = normalizedTags;
            Revision++;
            return MapEditorEditResult.ChangedResult("Updated map object tags.");
        }

        return MapEditorEditResult.Failed("Tags only apply to regions and map objects.");
    }

    public MapEditorResizeHandle HitTestResizeHandle(SimVector2 position, float tolerance)
    {
        if (SelectedRegion is not null)
        {
            return MapEditorShapeResizeHelper.HitTest(SelectedRegion.Shape, SelectedRegion.Center, SelectedRegion.Size, SelectedRegion.Radius, position, tolerance);
        }

        if (SelectedObject is not null)
        {
            return MapEditorShapeResizeHelper.HitTest(SelectedObject.Shape, SelectedObject.Center, SelectedObject.Size, SelectedObject.Radius, position, tolerance);
        }

        return MapEditorResizeHandle.None;
    }

    public bool ResizeSelected(MapEditorResizeHandle handle, SimVector2 position)
    {
        if (handle == MapEditorResizeHandle.None)
        {
            return false;
        }

        if (SelectedRegion is not null)
        {
            var resized = MapEditorShapeResizeHelper.Resize(SelectedRegion.Shape, SelectedRegion.Center, SelectedRegion.Size, SelectedRegion.Radius, handle, position, MinShapeDimension);
            if (SelectedRegion.Center == resized.Center && SelectedRegion.Size == resized.Size && MathF.Abs(SelectedRegion.Radius - resized.Radius) < 0.001f)
            {
                return true;
            }

            PrepareChange();
            SelectedRegion.Center = resized.Center;
            SelectedRegion.Size = resized.Size;
            SelectedRegion.Radius = resized.Radius;
            Revision++;
            return true;
        }

        if (SelectedObject is not null)
        {
            var resized = MapEditorShapeResizeHelper.Resize(SelectedObject.Shape, SelectedObject.Center, SelectedObject.Size, SelectedObject.Radius, handle, position, MinShapeDimension);
            if (SelectedObject.Center == resized.Center && SelectedObject.Size == resized.Size && MathF.Abs(SelectedObject.Radius - resized.Radius) < 0.001f)
            {
                return true;
            }

            PrepareChange();
            SelectedObject.Center = resized.Center;
            SelectedObject.Size = resized.Size;
            SelectedObject.Radius = resized.Radius;
            Revision++;
            return true;
        }

        return false;
    }

    public string SelectedShapeSizeSummary()
    {
        if (SelectedRegion is not null)
        {
            return FormatShapeSize(SelectedRegion.Shape, SelectedRegion.Size, SelectedRegion.Radius);
        }

        if (SelectedObject is not null)
        {
            return FormatShapeSize(SelectedObject.Shape, SelectedObject.Size, SelectedObject.Radius);
        }

        return string.Empty;
    }

    private void PrepareChange()
    {
        if (_activeEditSnapshot is not null)
        {
            return;
        }

        PushUndoSnapshot(CreateSnapshot());
        _redoStack.Clear();
    }

    private void ResetUndoHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _activeEditSnapshot = null;
        _activeEditStartRevision = 0;
    }

    private void PushUndoSnapshot(MapEditorSnapshot snapshot)
    {
        _undoStack.Add(snapshot);
        if (_undoStack.Count > UndoLimit)
        {
            _undoStack.RemoveAt(0);
        }
    }

    private MapEditorSnapshot CreateSnapshot()
    {
        return new MapEditorSnapshot(
            _markers.Select(CloneMarker).ToArray(),
            _regions.Select(CloneRegion).ToArray(),
            _objects.Select(CloneObject).ToArray(),
            _selectionKind,
            _selectionIndex);
    }

    private void RestoreSnapshot(MapEditorSnapshot snapshot)
    {
        _markers.Clear();
        _markers.AddRange(snapshot.Markers.Select(CloneMarker));
        _regions.Clear();
        _regions.AddRange(snapshot.Regions.Select(CloneRegion));
        _objects.Clear();
        _objects.AddRange(snapshot.Objects.Select(CloneObject));
        _selectionKind = snapshot.SelectionKind;
        _selectionIndex = snapshot.SelectionIndex;
        ClampSelection();
    }

    private void ClampSelection()
    {
        var limit = _selectionKind switch
        {
            MapEditorSelectionKind.Marker => _markers.Count,
            MapEditorSelectionKind.Region => _regions.Count,
            MapEditorSelectionKind.Object => _objects.Count,
            _ => 0
        };

        if (_selectionIndex < 0 || _selectionIndex >= limit)
        {
            ClearSelection();
        }
    }

    private IReadOnlyList<string> ReferencesFor(MapEditorSelectionKind selectionKind, string id)
    {
        return selectionKind switch
        {
            MapEditorSelectionKind.Marker => MarkerReferences(id),
            MapEditorSelectionKind.Object => ObjectReferences(id),
            _ => []
        };
    }

    private IReadOnlyList<string> MarkerReferences(string id)
    {
        if (_sourceMission is null)
        {
            return [];
        }

        var references = new List<string>();
        CountReferences(references, "starting_entities", _sourceMission.StartingEntities.Count(entity => entity.MarkerId == id));
        CountReferences(references, "resource_well_placements", _sourceMission.ResourceWellPlacements.Count(placement => placement.MarkerId == id));
        CountReferences(references, "presentation.map_callouts", _sourceMission.Presentation.MapCallouts.Count(callout => callout.MarkerId == id));

        var ai = _sourceMission.EnemyAiProfile;
        CountReferences(references, "enemy_ai_profile.hub_marker", ai.HubMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.power_plant_marker", ai.PowerPlantMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.barracks_marker", ai.BarracksMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.extractor_marker", ai.ExtractorMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.defense_tower_marker", ai.DefenseTowerMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.rally_marker", ai.RallyMarkerId == id ? 1 : 0);
        CountReferences(references, "enemy_ai_profile.patrol_markers", ai.PatrolMarkerIds.Count(markerId => markerId == id));
        return references;
    }

    private IReadOnlyList<string> ObjectReferences(string id)
    {
        if (_sourceMission is null)
        {
            return [];
        }

        var references = new List<string>();
        CountReferences(references, "mission_object_overrides", _sourceMission.MapObjectOverrides.Count(item => item.ObjectId == id));
        CountReferences(references, "enemy_ai_profile.central_island_attack_via_bridge_id", _sourceMission.EnemyAiProfile.CentralIslandAttackViaBridgeId == id ? 1 : 0);
        return references;
    }

    private static void CountReferences(List<string> references, string label, int count)
    {
        if (count > 0)
        {
            references.Add($"{label} ({count})");
        }
    }

    private static MapEditorMarker CloneMarker(MapEditorMarker marker)
    {
        var clone = new MapEditorMarker(marker.Id, marker.Position, marker.Tags.ToArray());
        foreach (var contentId in marker.ContentIds)
        {
            clone.AddContentId(contentId);
        }

        return clone;
    }

    private static MapEditorRegion CloneRegion(MapEditorRegion region)
    {
        return new MapEditorRegion(
            region.Id,
            region.RegionType,
            region.Shape,
            region.Center,
            region.Size,
            region.Radius,
            region.BlocksMovement,
            region.BlocksBuilding,
            region.AllowsBuilding,
            region.Tags.ToArray());
    }

    private static MapEditorObject CloneObject(MapEditorObject mapObject)
    {
        return new MapEditorObject(
            mapObject.Id,
            mapObject.ObjectType,
            mapObject.Shape,
            mapObject.Center,
            mapObject.Size,
            mapObject.Radius,
            mapObject.MaxHealth,
            mapObject.StartsIntact,
            mapObject.BlocksMovementWhenBroken,
            mapObject.Tags.ToArray());
    }

    private IEnumerable<string> AllIds()
    {
        return _markers.Select(marker => marker.Id)
            .Concat(_regions.Select(region => region.Id))
            .Concat(_objects.Select(item => item.Id));
    }

    private static string UniqueId(string basis, IEnumerable<string> existingIds)
    {
        var normalized = NormalizeId(basis);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = "item_new";
        }

        var existing = existingIds.ToHashSet(StringComparer.Ordinal);
        if (!existing.Contains(normalized))
        {
            return normalized;
        }

        for (var suffix = 2; suffix < 10000; suffix++)
        {
            var candidate = $"{normalized}_{suffix}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"Could not create a unique id from '{basis}'.");
    }

    private static string DefaultRegionTypeForCreation()
    {
        return "blocked_terrain";
    }

    private static void ApplyRegionTypePreset(MapEditorRegion region, string regionType)
    {
        region.RegionType = regionType;
        switch (regionType)
        {
            case "blocked_terrain":
            case "water":
                region.BlocksMovement = true;
                region.BlocksBuilding = true;
                region.AllowsBuilding = false;
                break;
            case "buildable_clearing":
            case "resource_basin":
                region.BlocksMovement = false;
                region.BlocksBuilding = false;
                region.AllowsBuilding = true;
                break;
            case "chokepoint_marker":
                region.BlocksMovement = false;
                region.BlocksBuilding = false;
                region.AllowsBuilding = false;
                break;
        }
    }

    private static SimVector2 ClampSize(SimVector2 size)
    {
        return new SimVector2(MathF.Max(MinShapeDimension, MathF.Abs(size.X)), MathF.Max(MinShapeDimension, MathF.Abs(size.Y)));
    }

    private static string NormalizeShape(string shape)
    {
        return string.Equals(shape, "circle", StringComparison.OrdinalIgnoreCase) ? "circle" : "rect";
    }

    private static string NormalizeId(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        var normalized = new string(chars);
        while (normalized.Contains("__", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("__", "_", StringComparison.Ordinal);
        }

        return normalized.Trim('_');
    }

    private sealed record MapEditorSnapshot(
        IReadOnlyList<MapEditorMarker> Markers,
        IReadOnlyList<MapEditorRegion> Regions,
        IReadOnlyList<MapEditorObject> Objects,
        MapEditorSelectionKind SelectionKind,
        int SelectionIndex);

}
