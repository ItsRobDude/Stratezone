using System.Globalization;
using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Simulation.Tools;

public partial class MapEditorOverlay : Node2D
{
    private const float MarkerHitRadius = 26.0f;
    private const float HoverMarkerHitRadius = 18.0f;
    private const float MarkerDrawRadius = 8.0f;
    private const float LabelFontSize = 13.0f;
    private const float ResizeHandleTolerance = 14.0f;
    private const float HandleDrawSize = 10.0f;

    private readonly MapEditorSession _session = new();
    private RtsSimulation? _simulation;
    private float _pylonLinkWorldRange;
    private float _powerPlantWorldRange;
    private float _defenseWallWorldRange;
    private bool _isDragging;
    private bool _isCreatingShape;
    private bool _snapEnabled = true;
    private MapEditorToolMode _toolMode = MapEditorToolMode.Select;
    private MapEditorResizeHandle _activeResizeHandle = MapEditorResizeHandle.None;
    private Vector2 _dragOffset;
    private Vector2 _createStartWorld;
    private Vector2 _createCurrentWorld;
    private Vector2 _lastPointerWorld;
    private MapEditorSelectionKind _hoverKind = MapEditorSelectionKind.None;
    private string? _hoverId;

    public bool EditorEnabled { get; private set; }
    public bool IsDragging => _isDragging || _isCreatingShape || _activeResizeHandle != MapEditorResizeHandle.None;
    public string SelectedSummary => _session.SelectedSummary;
    public string SelectedInspector => _session.SelectedInspector;
    public string? SelectedId => _session.SelectedId;
    public MapEditorSelectionKind SelectionKind => _session.SelectionKind;
    public int Revision => _session.Revision;
    public bool PathingDebugEnabled { get; private set; }
    public MapEditorToolMode ToolMode => _toolMode;
    public bool SnapEnabled => _snapEnabled;
    public bool CanUndo => _session.CanUndo;
    public bool CanRedo => _session.CanRedo;
    public MapEditorMarker? SelectedMarker => _session.SelectedMarker;
    public MapEditorRegion? SelectedRegion => _session.SelectedRegion;
    public MapEditorObject? SelectedObject => _session.SelectedObject;

    public MapEditorOverlay()
    {
        _session.DiagnosticEmitted += LogSessionDiagnostic;
    }

    public void Load(
        MissionDefinition? mission,
        MapDefinition? map,
        ContentCatalog? catalog,
        RtsSimulation? simulation)
    {
        _simulation = simulation;
        _isDragging = false;
        try
        {
            _session.Load(mission, map);
        }
        catch (Exception exception)
        {
            _session.LogException("load", exception, "Map editor session load failed.");
        }

        _pylonLinkWorldRange = 0.0f;
        _powerPlantWorldRange = 0.0f;
        _defenseWallWorldRange = 0.0f;
        if (catalog is not null)
        {
            try
            {
                var pylon = catalog.GetBuilding(ContentIds.Buildings.Pylon);
                var powerPlant = catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
                var defenseTower = catalog.GetBuilding(ContentIds.Buildings.DefenseTower);
                _pylonLinkWorldRange = RtsSimulation.ToWorldRadius(pylon.PylonLinkRange);
                _powerPlantWorldRange = RtsSimulation.ToWorldRadius(powerPlant.PowerRadius);
                _defenseWallWorldRange = RtsSimulation.ToWorldRadius(defenseTower.WallLinkRange);
                _session.LogInfo("load_ranges", $"Loaded range overlays: pylon={_pylonLinkWorldRange:0.###}, power={_powerPlantWorldRange:0.###}, wall={_defenseWallWorldRange:0.###}.");
            }
            catch (Exception exception)
            {
                _session.LogException("load_ranges", exception, "Map editor range overlay load failed.");
            }
        }
        else
        {
            _session.LogWarning("load_ranges", "No content catalog was provided; range overlays are disabled.");
        }

        QueueRedraw();
    }

    public void LogWarning(string operation, string message)
    {
        _session.LogWarning(operation, message);
    }

    public void LogException(string operation, Exception exception, string? message = null)
    {
        _session.LogException(operation, exception, message);
    }

    public void SetEditorEnabled(bool enabled)
    {
        EditorEnabled = enabled;
        Visible = enabled;
        SetProcess(enabled);
        if (!enabled)
        {
            _isDragging = false;
            _isCreatingShape = false;
            _activeResizeHandle = MapEditorResizeHandle.None;
            ClearHover();
            _session.CancelEditAction();
        }

        QueueRedraw();
    }

    public void SetToolMode(MapEditorToolMode toolMode)
    {
        _toolMode = toolMode;
        _isDragging = false;
        _isCreatingShape = false;
        _activeResizeHandle = MapEditorResizeHandle.None;
        ClearHover();
        _session.CancelEditAction();
        QueueRedraw();
    }

    public void SetSnapEnabled(bool enabled)
    {
        _snapEnabled = enabled;
        QueueRedraw();
    }

    public void ToggleSnap()
    {
        SetSnapEnabled(!_snapEnabled);
    }

    public void SetPathingDebugEnabled(bool enabled)
    {
        PathingDebugEnabled = enabled;
        QueueRedraw();
    }

    public bool TryBeginDrag(Vector2 worldPosition)
    {
        var snapped = Snap(worldPosition);
        _lastPointerWorld = snapped;
        _session.SelectAt(ToSim(snapped), MarkerHitRadius);
        var selectedCenter = _session.SelectedCenter;
        if (selectedCenter is not null)
        {
            _session.BeginEditAction();
            _isDragging = true;
            _dragOffset = snapped - ToGodot(selectedCenter.Value);
            QueueRedraw();
            return true;
        }

        return false;
    }

    public bool TrySelectMarkerAt(Vector2 worldPosition)
    {
        var selected = _session.SelectAt(ToSim(worldPosition), MarkerHitRadius);
        QueueRedraw();
        return selected && _session.SelectionKind == MapEditorSelectionKind.Marker;
    }

    public bool SelectAt(Vector2 worldPosition)
    {
        var selected = _session.SelectAt(ToSim(worldPosition), MarkerHitRadius);
        QueueRedraw();
        return selected;
    }

    public Vector2? SelectedMarkerWorldPosition()
    {
        return _session.SelectedMarker is null
            ? null
            : ToGodot(_session.SelectedMarker.Position);
    }

    public void DragTo(Vector2 worldPosition)
    {
        if (!_isDragging)
        {
            return;
        }

        var snapped = Snap(worldPosition);
        _lastPointerWorld = snapped;
        _session.MoveSelectedTo(ToSim(snapped - _dragOffset));
        QueueRedraw();
    }

    public void EndDrag()
    {
        _isDragging = false;
        _activeResizeHandle = MapEditorResizeHandle.None;
        _session.CommitEditAction();
    }

    public bool BeginPointerAction(Vector2 worldPosition, out MapEditorDeletePreview? deletePreview)
    {
        deletePreview = null;
        var snapped = Snap(worldPosition);
        _lastPointerWorld = snapped;

        if (_toolMode == MapEditorToolMode.AddMarker)
        {
            _session.AddMarkerAt(ToSim(snapped));
            QueueRedraw();
            return true;
        }

        if (_toolMode is MapEditorToolMode.AddRectRegion or MapEditorToolMode.AddCircleRegion)
        {
            _isCreatingShape = true;
            _createStartWorld = snapped;
            _createCurrentWorld = snapped;
            QueueRedraw();
            return true;
        }

        if (_toolMode == MapEditorToolMode.Delete)
        {
            if (_session.SelectAt(ToSim(snapped), MarkerHitRadius))
            {
                deletePreview = _session.CreateDeletePreview();
            }
            else
            {
                _session.LogWarning("delete_select", $"No marker, region, or map object at {MapEditorSession.FormatVector(ToSim(snapped))}.");
            }

            QueueRedraw();
            return true;
        }

        var resizeHandle = _session.HitTestResizeHandle(ToSim(snapped), ResizeHandleTolerance);
        if (resizeHandle != MapEditorResizeHandle.None)
        {
            _activeResizeHandle = resizeHandle;
            _session.BeginEditAction();
            QueueRedraw();
            return true;
        }

        return TryBeginDrag(snapped);
    }

    public bool UpdatePointerAction(Vector2 worldPosition)
    {
        var snapped = Snap(worldPosition);
        _lastPointerWorld = snapped;
        if (_isCreatingShape)
        {
            _createCurrentWorld = snapped;
            QueueRedraw();
            return true;
        }

        if (_activeResizeHandle != MapEditorResizeHandle.None)
        {
            _session.ResizeSelected(_activeResizeHandle, ToSim(snapped));
            QueueRedraw();
            return true;
        }

        if (_isDragging)
        {
            DragTo(snapped);
            return true;
        }

        UpdateHover(snapped);
        return false;
    }

    public bool EndPointerAction(Vector2 worldPosition)
    {
        var snapped = Snap(worldPosition);
        _lastPointerWorld = snapped;
        if (_isCreatingShape)
        {
            CompleteShapeCreation(snapped);
            _isCreatingShape = false;
            QueueRedraw();
            return true;
        }

        if (_activeResizeHandle != MapEditorResizeHandle.None)
        {
            _session.ResizeSelected(_activeResizeHandle, ToSim(snapped));
            _activeResizeHandle = MapEditorResizeHandle.None;
            _session.CommitEditAction();
            QueueRedraw();
            return true;
        }

        if (_isDragging)
        {
            DragTo(snapped);
            EndDrag();
            QueueRedraw();
            return true;
        }

        return false;
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

    public bool CycleSelection(bool forward)
    {
        var selected = forward
            ? _session.SelectNext()
            : _session.SelectPrevious();
        if (selected)
        {
            QueueRedraw();
        }

        return selected;
    }

    public bool Undo()
    {
        var changed = _session.Undo();
        if (changed)
        {
            QueueRedraw();
        }

        return changed;
    }

    public bool Redo()
    {
        var changed = _session.Redo();
        if (changed)
        {
            QueueRedraw();
        }

        return changed;
    }

    public MapEditorDeletePreview? CreateDeletePreview()
    {
        return _session.CreateDeletePreview();
    }

    public MapEditorEditResult DeleteSelected()
    {
        var result = _session.DeleteSelected();
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult DuplicateSelected()
    {
        var result = _session.DuplicateSelected();
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult RenameSelected(string newId)
    {
        var result = _session.RenameSelected(newId);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedCenter(SimVector2 center)
    {
        var result = _session.SetSelectedCenter(center);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedRegionType(string regionType)
    {
        var result = _session.SetSelectedRegionType(regionType);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult CycleSelectedRegionType()
    {
        var result = _session.CycleSelectedRegionType();
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedRegionShape(string shape)
    {
        var result = _session.SetSelectedRegionShape(shape);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedRegionSize(SimVector2 size)
    {
        var result = _session.SetSelectedRegionSize(size);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedRegionRadius(float radius)
    {
        var result = _session.SetSelectedRegionRadius(radius);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedRegionFlags(bool blocksMovement, bool blocksBuilding, bool allowsBuilding)
    {
        var result = _session.SetSelectedRegionFlags(blocksMovement, blocksBuilding, allowsBuilding);
        QueueRedraw();
        return result;
    }

    public MapEditorEditResult SetSelectedTags(IReadOnlyList<string> tags)
    {
        var result = _session.SetSelectedTags(tags);
        QueueRedraw();
        return result;
    }

    public IReadOnlyList<MapEditorValidationIssue> ValidateContent()
    {
        return _session.ValidateContent();
    }

    public string ExportSelectedSnippet()
    {
        return _session.ExportSelectedSnippet();
    }

    public string ExportAllSnippets()
    {
        return _session.ExportAllSnippets();
    }

    public MissionDefinition ApplyEditsToMission(MissionDefinition mission)
    {
        return _session.ApplyToMission(mission);
    }

    public MapDefinition ApplyEditsToMap(MapDefinition map)
    {
        return _session.ApplyToMap(map);
    }

    public MapEditorSavePreview CreateSavePreview(string gameRoot)
    {
        return MapEditorPersistence.CreatePreview(gameRoot, _session);
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private static SimVector2 ToSim(Vector2 vector)
    {
        return new SimVector2(vector.X, vector.Y);
    }

    private bool IsHovered(MapEditorSelectionKind kind, string id)
    {
        return _hoverKind == kind && string.Equals(_hoverId, id, StringComparison.Ordinal);
    }

    private void ClearHover()
    {
        _hoverKind = MapEditorSelectionKind.None;
        _hoverId = null;
    }

    private void UpdateHover(Vector2 worldPosition)
    {
        var position = ToSim(worldPosition);
        var (kind, id) = HitTestHover(position);
        if (_hoverKind == kind && string.Equals(_hoverId, id, StringComparison.Ordinal))
        {
            return;
        }

        _hoverKind = kind;
        _hoverId = id;
        QueueRedraw();
    }

    private (MapEditorSelectionKind Kind, string? Id) HitTestHover(SimVector2 position)
    {
        var marker = _session.Markers
            .Select(item => (Marker: item, Distance: item.Position.DistanceTo(position)))
            .Where(item => item.Distance <= HoverMarkerHitRadius)
            .OrderBy(item => item.Distance)
            .FirstOrDefault();
        if (marker.Marker is not null)
        {
            return (MapEditorSelectionKind.Marker, marker.Marker.Id);
        }

        var mapObject = _session.Objects
            .Where(item => item.Contains(position))
            .OrderBy(item => item.Area)
            .FirstOrDefault();
        if (mapObject is not null)
        {
            return (MapEditorSelectionKind.Object, mapObject.Id);
        }

        var region = _session.Regions
            .Where(item => item.Contains(position))
            .OrderBy(item => item.Area)
            .FirstOrDefault();
        return region is null
            ? (MapEditorSelectionKind.None, null)
            : (MapEditorSelectionKind.Region, region.Id);
    }

    private static string FormatFloat(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    private static void LogSessionDiagnostic(MapEditorLogEntry entry)
    {
        var line = entry.ToConsoleLine();
        switch (entry.Level)
        {
            case MapEditorLogLevel.Error:
                GD.PushError(line);
                break;
            case MapEditorLogLevel.Warning:
                GD.Print(line);
                break;
            default:
                GD.Print(line);
                break;
        }
    }
}
