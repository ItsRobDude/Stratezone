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
            _lastActionMessage = L(
                "ui.action.selected_unit",
                SimulationMessage.Args(
                    ("unit", UnitName(simDefinition)),
                    ("id", simDefinition.Id),
                    ("health", $"{nearestSimUnit.State.Health:0}"),
                    ("maxHealth", simDefinition.Health)));
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

            _lastActionMessage = L(
                "ui.action.selected_building",
                SimulationMessage.Args(
                    ("building", BuildingName(building.Definition)),
                    ("health", $"{building.Health:0}"),
                    ("maxHealth", building.Definition.Health)));
            return;
        }

        _lastActionMessage = L("ui.action.no_selectable");
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
            ? L("ui.action.selected_units", SimulationMessage.Args(("count", _selectedUnitEntityIds.Count)))
            : L("ui.action.no_units_in_box");
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
            _lastActionMessage = L("ui.action.no_unit_selected");
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
            var attackingUnits = selectedUnits.Where(unit => unit.Definition.CanAttack).ToArray();
            for (var index = 0; index < attackingUnits.Length; index++)
            {
                _simulation.CommandUnitAttackUnit(
                    attackingUnits[index].EntityId,
                    enemyUnit.EntityId,
                    ToSim(GetGroupOffset(index, attackingUnits.Length)));
                attackers++;
            }

            _lastActionMessage = attackers > 0
                ? L("ui.action.units_attacking_unit", SimulationMessage.Args(("count", attackers), ("unit", UnitName(enemyUnit.Definition))))
                : L("ui.action.selected_units_cannot_attack");
            return;
        }

        var enemyBuilding = FindEnemyBuildingAt(worldPosition);
        if (enemyBuilding is not null)
        {
            var attackers = 0;
            var attackingUnits = selectedUnits.Where(UnitCanAttackBuildings).ToArray();
            for (var index = 0; index < attackingUnits.Length; index++)
            {
                _simulation.CommandUnitAttackBuilding(
                    attackingUnits[index].EntityId,
                    enemyBuilding.EntityId,
                    ToSim(GetGroupOffset(index, attackingUnits.Length)));
                attackers++;
            }

            _lastActionMessage = attackers > 0
                ? L("ui.action.units_attacking_building", SimulationMessage.Args(("count", attackers), ("building", BuildingName(enemyBuilding.Definition))))
                : L("ui.action.selected_units_cannot_attack");
            return;
        }

        var repairTarget = FindRepairablePlayerBuildingAt(worldPosition);
        if (repairTarget is not null)
        {
            var repairers = 0;
            foreach (var worker in selectedUnits.Where(unit => unit.Definition.CanRepair))
            {
                var result = _simulation.CommandUnitRepairBuilding(worker.EntityId, repairTarget.EntityId);
                if (result.Success)
                {
                    repairers++;
                }
            }

            _lastActionMessage = repairers > 0
                ? L("ui.action.units_repairing_building", SimulationMessage.Args(("count", repairers), ("building", BuildingName(repairTarget.Definition))))
                : L("ui.action.selected_units_cannot_repair");
            return;
        }

        for (var index = 0; index < selectedUnits.Length; index++)
        {
            _simulation.CommandUnitMove(selectedUnits[index].EntityId, ToSim(worldPosition + GetGroupOffset(index, selectedUnits.Length)));
        }

        _lastActionMessage = L(
            "ui.action.moving_units",
            SimulationMessage.Args(("count", selectedUnits.Length), ("x", $"{worldPosition.X:0}"), ("y", $"{worldPosition.Y:0}")));
    }

    private static Vector2 GetGroupOffset(int index, int count)
    {
        if (count <= 1)
        {
            return Vector2.Zero;
        }

        const float spacing = 52.0f;
        var columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        var rows = Mathf.CeilToInt(count / (float)columns);
        var column = index % columns;
        var row = index / columns;
        return new Vector2(
            (column - ((columns - 1) * 0.5f)) * spacing,
            (row - ((rows - 1) * 0.5f)) * spacing);
    }

    private static bool UnitCanAttackBuildings(UnitState unit)
    {
        return unit.Definition.CanAttack &&
            unit.Definition.TargetFilters.Contains("building", StringComparer.Ordinal);
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

    private BuildingState? FindRepairablePlayerBuildingAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Buildings
            .Where(building =>
                building.FactionId == ContentIds.Factions.PlayerExpedition &&
                !building.IsDestroyed &&
                building.IsDamaged)
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
