using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private void SetupPlacementGhost()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _placementGhost = new PlacementGhost
        {
            Name = "PlacementGhost",
            Visible = false,
            ZIndex = 3
        };
        _worldRoot.AddChild(_placementGhost);
    }

    private void SetupEnergyWallView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _energyWallView = new EnergyWallView
        {
            Name = "EnergyWallView",
            ZIndex = -1
        };
        _worldRoot.AddChild(_energyWallView);
    }

    private void SetupMapRegionView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _mapRegionView = new MapRegionView
        {
            Name = "MapRegionView",
            ZIndex = -10
        };
        _worldRoot.AddChild(_mapRegionView);
        _mapRegionView.UpdateFromMap(_simulation?.Map);
    }

    private void SetupMissionCalloutView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _missionCalloutView = new MissionCalloutView
        {
            Name = "MissionCalloutView",
            ZIndex = 22
        };
        _worldRoot.AddChild(_missionCalloutView);
    }

    private void SetupFogOfWarView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _fogOfWarView = new FogOfWarView
        {
            Name = "FogOfWarView",
            ZIndex = 20
        };
        _worldRoot.AddChild(_fogOfWarView);
    }

    private void SetupSelectionBoxView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _selectionBoxView = new SelectionBoxView
        {
            Name = "SelectionBoxView",
            ZIndex = 30
        };
        _worldRoot.AddChild(_selectionBoxView);
    }

    private void ClearWorldViews()
    {
        foreach (var view in _buildingViews.Values)
        {
            view.QueueFree();
        }

        foreach (var view in _simUnitViews.Values)
        {
            view.QueueFree();
        }

        foreach (var view in _resourceWellViews)
        {
            view.QueueFree();
        }

        _buildingViews.Clear();
        _simUnitViews.Clear();
        _resourceWellViews.Clear();
    }

    private void SyncWorldViews()
    {
        if (_simulation is null || _worldRoot is null)
        {
            return;
        }

        var visibleWorldBounds = GetExpandedCameraWorldBounds(OffscreenCullPaddingWorld);
        var cameraZoom = CurrentCameraZoom();
        var liveBuildingIds = new HashSet<int>();
        foreach (var building in _simulation.Buildings)
        {
            if (!building.IsDestroyed)
            {
                liveBuildingIds.Add(building.EntityId);
            }
        }

        var liveUnitIds = new HashSet<int>();
        foreach (var unit in _simulation.Units)
        {
            if (!unit.IsDestroyed)
            {
                liveUnitIds.Add(unit.EntityId);
            }
        }

        foreach (var building in _simulation.Buildings)
        {
            if (!_buildingViews.TryGetValue(building.EntityId, out var view))
            {
                if (building.IsDestroyed)
                {
                    continue;
                }

                view = new GreyboxBuilding
                {
                    Name = $"Building_{building.EntityId}_{building.Definition.Id}",
                    ZIndex = -1
                };
                _worldRoot.AddChild(view);
                view.Initialize(building, _localization);
                _buildingViews.Add(building.EntityId, view);
            }
            else
            {
                view.UpdateFromState(building);
            }

            var knownToPlayer = building.FactionId != ContentIds.Factions.PrivateMilitary ||
                _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position);
            view.SetCameraZoom(cameraZoom);
            view.LabelsSuppressed = _mapEditorEnabled;
            view.ShowAlwaysOnLabel = !_mapEditorEnabled && _debugLabelsEnabled;
            view.Visible = !building.IsDestroyed &&
                knownToPlayer &&
                IsCircleInsideWorldBounds(building.Position, building.FootprintWorldRadius, visibleWorldBounds);
        }

        while (_resourceWellViews.Count < _simulation.ResourceWells.Count)
        {
            var well = _simulation.ResourceWells[_resourceWellViews.Count];
            var view = new ResourceWellView
            {
                Name = well.Definition.Id,
                ZIndex = -2
            };
            _worldRoot.AddChild(view);
            view.Initialize(well);
            _resourceWellViews.Add(view);
        }

        for (var index = 0; index < _resourceWellViews.Count; index++)
        {
            var well = _simulation.ResourceWells[index];
            var view = _resourceWellViews[index];
            view.UpdateFromState(well);
            view.SetCameraZoom(cameraZoom);
            view.Visible = IsCircleInsideWorldBounds(well.Position, 48.0f, visibleWorldBounds);
        }

        _selectedUnitEntityIds.RemoveWhere(unitId => !liveUnitIds.Contains(unitId));
        foreach (var unit in _simulation.Units)
        {
            if (!_simUnitViews.TryGetValue(unit.EntityId, out var view))
            {
                if (unit.IsDestroyed)
                {
                    continue;
                }

                view = new GreyboxSimUnit
                {
                    Name = $"SimUnit_{unit.EntityId}_{unit.Definition.Id}",
                    ZIndex = 2
                };
                _worldRoot.AddChild(view);
                view.Initialize(unit, _localization);
                _simUnitViews.Add(unit.EntityId, view);
            }
            else
            {
                view.UpdateFromState(unit);
            }

            var knownToPlayer = unit.FactionId != ContentIds.Factions.PrivateMilitary ||
                _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unit.Position);
            view.Visible = !unit.IsDestroyed &&
                knownToPlayer &&
                IsCircleInsideWorldBounds(unit.Position, view.SelectionRadius, visibleWorldBounds);
            view.SetSelected(_selectedUnitEntityIds.Contains(unit.EntityId));
            view.SetCameraZoom(cameraZoom);
            view.LabelsSuppressed = _mapEditorEnabled;
            view.ShowAlwaysOnLabel = !_mapEditorEnabled && _debugLabelsEnabled;
        }

        PruneDeadWorldViews(liveBuildingIds, liveUnitIds);
        _energyWallView?.UpdateSegments(_simulation.EnergyWalls);
        _mapRegionView?.UpdateBridgeStates(_simulation.Bridges);
        _fogOfWarView?.UpdateFromState(_simulation.PlayerFog, visibleWorldBounds);
        UpdateMissionCallouts(visibleWorldBounds, cameraZoom);
    }

    private void PruneDeadWorldViews(HashSet<int> liveBuildingIds, HashSet<int> liveUnitIds)
    {
        foreach (var buildingId in _buildingViews.Keys.Where(id => !liveBuildingIds.Contains(id)).ToArray())
        {
            _buildingViews[buildingId].QueueFree();
            _buildingViews.Remove(buildingId);
            if (_selectedBuildingEntityId == buildingId)
            {
                _selectedBuildingEntityId = null;
            }
        }

        foreach (var unitId in _simUnitViews.Keys.Where(id => !liveUnitIds.Contains(id)).ToArray())
        {
            _simUnitViews[unitId].QueueFree();
            _simUnitViews.Remove(unitId);
            _selectedUnitEntityIds.Remove(unitId);
        }
    }

    private void UpdateMissionCallouts(Rect2 visibleWorldBounds, float cameraZoom)
    {
        if (_missionCalloutView is null || _simulation is null || _activeMission is null)
        {
            return;
        }

        _missionCalloutView.Visible = !_mapEditorEnabled;
        if (_mapEditorEnabled)
        {
            _missionCalloutView.UpdateCallouts([], visibleWorldBounds, cameraZoom);
            return;
        }

        _calloutBuffer.Clear();
        foreach (var callout in _activeMission.Presentation.MapCallouts)
        {
            if (!_markersById.TryGetValue(callout.MarkerId, out var marker) ||
                !_simulation.IsExploredByFaction(ContentIds.Factions.PlayerExpedition, marker.Position))
            {
                continue;
            }

            var text = L(callout.TextKey);
            if (!string.IsNullOrWhiteSpace(text))
            {
                _calloutBuffer.Add(new MissionCalloutSnapshot(new Vector2(marker.Position.X, marker.Position.Y), text));
            }
        }

        _missionCalloutView.UpdateCallouts(_calloutBuffer, visibleWorldBounds, cameraZoom);
    }
}
