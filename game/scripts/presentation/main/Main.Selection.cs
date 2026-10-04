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

            var selectedBuildingMessageKey = building.IsDamaged
                ? "ui.action.selected_damaged_building"
                : "ui.action.selected_building";
            _lastActionMessage = L(
                selectedBuildingMessageKey,
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
        var selected = SelectedUnits().ToArray();
        if (selected.Length == 0) { _lastActionMessage = L("ui.action.no_unit_selected"); return; }
        if (_simulation is null) return;

        var enemyUnit = FindEnemyUnitAt(worldPosition);
        if (enemyUnit is not null)
        {
            var attackers = selected.Where(unit => unit.Definition.CanAttack).ToArray();
            QueueUnitOrders(attackers.Select((unit, index) => (SimulationCommand)new AttackUnitCommand(
                unit.EntityId, enemyUnit.EntityId, ToSim(GetGroupOffset(index, attackers.Length)))).ToArray(),
                "ui.action.units_attacking_unit", SimulationMessage.Args(("unit", UnitName(enemyUnit.Definition))),
                "ui.action.selected_units_cannot_attack");
            return;
        }
        var enemyBuilding = FindEnemyBuildingAt(worldPosition);
        if (enemyBuilding is not null)
        {
            var attackers = selected.Where(UnitCanAttackBuildings).ToArray();
            QueueUnitOrders(attackers.Select((unit, index) => (SimulationCommand)new AttackBuildingCommand(
                unit.EntityId, enemyBuilding.EntityId, ToSim(GetGroupOffset(index, attackers.Length)))).ToArray(),
                "ui.action.units_attacking_building", SimulationMessage.Args(("building", BuildingName(enemyBuilding.Definition))),
                "ui.action.selected_units_cannot_attack");
            return;
        }
        var repairTarget = FindRepairablePlayerBuildingAt(worldPosition);
        if (repairTarget is not null)
        {
            QueueUnitOrders(selected.Where(unit => unit.Definition.CanRepair)
                .Select(unit => (SimulationCommand)new RepairBuildingCommand(unit.EntityId, repairTarget.EntityId)).ToArray(),
                "ui.action.units_repairing_building", SimulationMessage.Args(("building", BuildingName(repairTarget.Definition))),
                "ui.action.selected_units_cannot_repair");
            return;
        }
        var bridge = FindBridgeAt(worldPosition);
        if (bridge is not null)
        {
            var repairers = selected.Where(unit => unit.Definition.CanRepair).ToArray();
            if (bridge.IsDamaged && repairers.Length > 0)
            {
                QueueUnitOrders(repairers.Select(unit => (SimulationCommand)new RepairBridgeCommand(unit.EntityId, bridge.Id)).ToArray(),
                    "ui.action.units_repairing_bridge", SimulationMessage.Args(("bridge", BridgeName(bridge))),
                    "ui.action.selected_units_cannot_repair");
                return;
            }
            if (bridge.IsIntact)
            {
                var attackers = selected.Where(unit => unit.Definition.CanAttack).ToArray();
                QueueUnitOrders(attackers.Select((unit, index) => (SimulationCommand)new AttackBridgeCommand(
                    unit.EntityId, bridge.Id, ToSim(GetGroupOffset(index, attackers.Length)))).ToArray(),
                    "ui.action.units_attacking_bridge", SimulationMessage.Args(("bridge", BridgeName(bridge))),
                    "ui.action.selected_units_cannot_attack");
                return;
            }
        }
        QueueUnitOrders(selected.Select((unit, index) => (SimulationCommand)new MoveUnitCommand(
            unit.EntityId, ToSim(worldPosition + GetGroupOffset(index, selected.Length)))).ToArray(),
            "ui.action.moving_units", SimulationMessage.Args(("x", $"{worldPosition.X:0}"), ("y", $"{worldPosition.Y:0}")),
            "ui.action.no_unit_selected");
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
        return FindBuildingAt(
            worldPosition,
            building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed);
    }

    private BuildingState? FindRepairablePlayerBuildingAt(Vector2 worldPosition)
    {
        return FindBuildingAt(
            worldPosition,
            building =>
                building.FactionId == ContentIds.Factions.PlayerExpedition &&
                !building.IsDestroyed &&
                building.IsDamaged);
    }

    private UnitState? FindEnemyUnitAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simUnitViews.Values
            .Where(view => view.Visible && view.State.FactionId == ContentIds.Factions.PrivateMilitary && !view.State.IsDestroyed)
            .Where(view => _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, view.State.Position))
            .Where(view => view.GlobalPosition.DistanceTo(worldPosition) <= view.SelectionRadius)
            .OrderBy(view => view.GlobalPosition.DistanceTo(worldPosition))
            .Select(view => view.State)
            .FirstOrDefault();
    }

    private BuildingState? FindEnemyBuildingAt(Vector2 worldPosition)
    {
        return FindBuildingAt(
            worldPosition,
            building =>
                building.FactionId == ContentIds.Factions.PrivateMilitary &&
                !building.IsDestroyed &&
                _simulation is not null &&
                _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position));
    }

    private BuildingState? FindBuildingAt(Vector2 worldPosition, Func<BuildingState, bool> predicate)
    {
        if (_simulation is null)
        {
            return null;
        }

        return _simulation.Buildings
            .Where(predicate)
            .Where(building => BuildingContainsPoint(building, worldPosition))
            .OrderBy(building => BuildingHitDistance(building, worldPosition))
            .FirstOrDefault();
    }

    private BridgeState? FindBridgeAt(Vector2 worldPosition)
    {
        if (_simulation is null)
        {
            return null;
        }

        var simPosition = ToSim(worldPosition);
        return _simulation.Bridges
            .Where(bridge => bridge.Contains(simPosition, 12.0f))
            .OrderBy(bridge => bridge.DistanceTo(simPosition))
            .FirstOrDefault();
    }

    private bool BuildingContainsPoint(BuildingState building, Vector2 worldPosition)
    {
        if (_buildingViews.TryGetValue(building.EntityId, out var view) && view.Visible)
        {
            return view.ContainsInteractionPoint(worldPosition);
        }

        return ToGodot(building.Position).DistanceTo(worldPosition) <= building.FootprintWorldRadius;
    }

    private float BuildingHitDistance(BuildingState building, Vector2 worldPosition)
    {
        if (_buildingViews.TryGetValue(building.EntityId, out var view) && view.Visible)
        {
            return view.DistanceToInteractionBounds(worldPosition);
        }

        return ToGodot(building.Position).DistanceTo(worldPosition);
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

}
