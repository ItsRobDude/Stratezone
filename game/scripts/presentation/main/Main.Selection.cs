using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private void BeginSelection(Vector2 worldPosition)
    {
        _leftMouseSelecting = true;
        _selectionStartWorld = worldPosition;
        _selectionBoxView?.Begin(worldPosition);
    }

    private void CompleteSelection(Vector2 worldPosition)
    {
        if (!_leftMouseSelecting)
        {
            return;
        }

        _leftMouseSelecting = false;
        _selectionBoxView?.Clear();

        if (_selectionStartWorld.DistanceTo(worldPosition) < 12.0f)
        {
            SelectSingleAt(worldPosition);
            return;
        }

        SelectUnitsInBox(new Rect2(_selectionStartWorld, worldPosition - _selectionStartWorld).Abs());
    }

    private void SelectSingleAt(Vector2 worldPosition)
    {
        ClearSelectedUnits();
        ClearSelectedBuilding();

        var nearestSimUnit = FindSelectableSimUnitAt(worldPosition);
        if (nearestSimUnit is not null)
        {
            SelectUnitView(nearestSimUnit);
            var simDefinition = nearestSimUnit.State.Definition;
            _lastActionMessage = $"Selected {simDefinition.DisplayName} ({simDefinition.Id}) | HP {nearestSimUnit.State.Health:0}/{simDefinition.Health}";
            return;
        }

        var building = FindSelectablePlayerBuildingAt(worldPosition);
        if (building is not null)
        {
            _selectedBuildingEntityId = building.EntityId;
            if (_buildingViews.TryGetValue(building.EntityId, out var view))
            {
                view.SetSelected(true);
            }

            _lastActionMessage = $"Selected {building.Definition.DisplayName} | HP {building.Health:0}/{building.Definition.Health}";
            return;
        }

        _lastActionMessage = "No unit or building selected.";
    }

    private void SelectUnitsInBox(Rect2 box)
    {
        ClearSelectedUnits();
        ClearSelectedBuilding();

        foreach (var unit in _simUnitViews.Values)
        {
            if (unit.State.FactionId != ContentIds.Factions.PlayerExpedition ||
                unit.State.IsDestroyed ||
                !unit.Visible ||
                !box.HasPoint(unit.GlobalPosition))
            {
                continue;
            }

            SelectUnitView(unit);
        }

        _lastActionMessage = _selectedUnitEntityIds.Count > 0
            ? $"Selected {_selectedUnitEntityIds.Count} units."
            : "No units in selection box.";
    }

    private void SelectUnitView(GreyboxSimUnit unit)
    {
        _selectedUnitEntityIds.Add(unit.State.EntityId);
        unit.SetSelected(true);
    }

    private void ClearSelectedUnits()
    {
        foreach (var unitId in _selectedUnitEntityIds)
        {
            if (_simUnitViews.TryGetValue(unitId, out var view))
            {
                view.SetSelected(false);
            }
        }

        _selectedUnitEntityIds.Clear();
    }

    private void ClearSelectedBuilding()
    {
        if (_selectedBuildingEntityId is not null &&
            _buildingViews.TryGetValue(_selectedBuildingEntityId.Value, out var selectedView))
        {
            selectedView.SetSelected(false);
        }

        _selectedBuildingEntityId = null;
    }

    private void CommandSelectedUnits(Vector2 worldPosition)
    {
        var selectedUnits = SelectedUnits().ToArray();
        if (selectedUnits.Length == 0)
        {
            _lastActionMessage = "No unit selected. Left click a unit first.";
            return;
        }

        if (_simulation is null)
        {
            return;
        }

        var enemyUnit = FindEnemyUnitAt(worldPosition);
        if (enemyUnit is not null)
        {
            var attackers = 0;
            foreach (var unit in selectedUnits.Where(unit => unit.Definition.CanAttack))
            {
                _simulation.CommandUnitAttackUnit(unit.EntityId, enemyUnit.EntityId);
                attackers++;
            }

            _lastActionMessage = attackers > 0
                ? $"{attackers} units attacking {enemyUnit.Definition.DisplayName}."
                : "Selected units cannot attack.";
            return;
        }

        var enemyBuilding = FindEnemyBuildingAt(worldPosition);
        if (enemyBuilding is not null)
        {
            var attackers = 0;
            foreach (var unit in selectedUnits.Where(unit => unit.Definition.CanAttack))
            {
                _simulation.CommandUnitAttackBuilding(unit.EntityId, enemyBuilding.EntityId);
                attackers++;
            }

            _lastActionMessage = attackers > 0
                ? $"{attackers} units attacking {enemyBuilding.Definition.DisplayName}."
                : "Selected units cannot attack.";
            return;
        }

        foreach (var unit in selectedUnits)
        {
            _simulation.CommandUnitMove(unit.EntityId, ToSim(worldPosition));
        }

        _lastActionMessage = $"Moving {selectedUnits.Length} units to {worldPosition.X:0}, {worldPosition.Y:0}";
    }

    private IEnumerable<UnitState> SelectedUnits()
    {
        if (_simulation is null)
        {
            return [];
        }

        return _simulation.Units
            .Where(unit => _selectedUnitEntityIds.Contains(unit.EntityId))
            .Where(unit => !unit.IsDestroyed);
    }

    private GreyboxSimUnit? FindSelectableSimUnitAt(Vector2 worldPosition)
    {
        GreyboxSimUnit? nearest = null;
        var nearestDistance = float.MaxValue;

        foreach (var unit in _simUnitViews.Values)
        {
            if (unit.State.FactionId != ContentIds.Factions.PlayerExpedition ||
                unit.State.IsDestroyed ||
                !unit.Visible)
            {
                continue;
            }

            var distance = unit.GlobalPosition.DistanceTo(worldPosition);
            if (distance <= unit.SelectionRadius && distance < nearestDistance)
            {
                nearest = unit;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private BuildingState? FindSelectablePlayerBuildingAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Buildings
            .Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed)
            .Where(building => new Vector2(building.Position.X, building.Position.Y).DistanceTo(worldPosition) <= building.FootprintWorldRadius)
            .OrderBy(building => new Vector2(building.Position.X, building.Position.Y).DistanceTo(worldPosition))
            .FirstOrDefault();
    }

    private UnitState? FindEnemyUnitAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Units
            .Where(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && !unit.IsDestroyed)
            .Where(unit => _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unit.Position))
            .Where(unit => new Vector2(unit.Position.X, unit.Position.Y).DistanceTo(worldPosition) <= 22.0f)
            .OrderBy(unit => new Vector2(unit.Position.X, unit.Position.Y).DistanceTo(worldPosition))
            .FirstOrDefault();
    }

    private BuildingState? FindEnemyBuildingAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Buildings
            .Where(building => building.FactionId == ContentIds.Factions.PrivateMilitary && !building.IsDestroyed)
            .Where(building => _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position))
            .Where(building => new Vector2(building.Position.X, building.Position.Y).DistanceTo(worldPosition) <= building.FootprintWorldRadius)
            .OrderBy(building => new Vector2(building.Position.X, building.Position.Y).DistanceTo(worldPosition))
            .FirstOrDefault();
    }
}
