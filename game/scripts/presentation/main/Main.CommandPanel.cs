using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class Main
{
    private void UpdateCommandPanel()
    {
        if (_commandPanel is null || _simulation is null || _catalog is null)
        {
            return;
        }

        var selectedUnits = SelectedUnits().ToArray();
        var selectedBuilding = _selectedBuildingEntityId is null
            ? null
            : _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
        var hasBuilder = selectedUnits.Any(unit => unit.Definition.CanConstruct);
        var selectedBarracks = selectedBuilding is not null && selectedBuilding.Definition.Id == ContentIds.Buildings.Barracks;
        var selectedDefenseTower = selectedBuilding is not null && selectedBuilding.Definition.Id == ContentIds.Buildings.DefenseTower;
        var actions = new List<CommandPanelAction>();

        actions.AddRange(BuildHotkeyOrder
            .Where(IsBuildingCommandAvailable)
            .Select((buildingId, index) =>
            {
                var definition = _catalog.GetBuilding(buildingId);
                var blockedReason = GetBuildingCommandBlockedReason(buildingId);
                var enabled = hasBuilder && blockedReason is null;
                var hint = !hasBuilder
                    ? L("ui.command.requires_grunt")
                    : blockedReason ?? L("ui.command.place_building", SimulationMessage.Args(("building", BuildingName(definition))));
                return new CommandPanelAction(
                    BuildingName(definition),
                    $"{index + 1}",
                    BuildingDetail(definition, hint),
                    enabled,
                    () => EnterPlacementMode(buildingId),
                    definition.Id,
                    L("ui.action_bar.cost", SimulationMessage.Args(("cost", definition.Cost))),
                    _placementBuildingId == buildingId);
            }));

        actions.AddRange(TrainHotkeyOrder
            .Where(IsUnitCommandAvailable)
            .Select(unitId =>
            {
                var unit = _catalog.GetUnit(unitId);
                var validation = selectedBarracks
                    ? _simulation.ValidateUnitProduction(unitId, selectedBuilding!.EntityId)
                    : null;
                var enabled = validation?.CanQueue == true;
                var hint = selectedBarracks
                    ? LocalizedProduction(validation!)
                    : L("ui.command.select_barracks_for_training");
                return new CommandPanelAction(
                    UnitName(unit),
                    GetTrainHotkeyLabel(unitId),
                    UnitDetail(unit, hint),
                    enabled,
                    () =>
                    {
                        if (_selectedBuildingEntityId is null)
                        {
                            _lastActionMessage = L("ui.action.select_barracks_before_training");
                            return;
                        }

                        var result = _simulation.TryQueueUnit(unitId, _selectedBuildingEntityId.Value);
                        _lastActionMessage = LocalizedProduction(result);
                    },
                    unit.Id,
                    TrainingCostLabel(unit, selectedBuilding));
            }));

        if (selectedBarracks && IsUnitCommandAvailable(ContentIds.Units.Guardian))
        {
            var upgrade = _catalog.GetBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit);
            if (!selectedBuilding!.HasBarracksUpgrade(upgrade.Id))
            {
                var validation = _simulation.ValidateGuardianRetrofit(selectedBuilding.EntityId);
                actions.Add(new CommandPanelAction(
                    BarracksUpgradeName(upgrade),
                    "U",
                    BarracksUpgradeDetail(upgrade, validation),
                    validation.Success,
                    () =>
                    {
                        var result = _simulation.TryStartGuardianRetrofit(selectedBuilding.EntityId);
                        _lastActionMessage = LocalizedUpgrade(result);
                    },
                    ContentIds.Units.Guardian,
                    BarracksUpgradeCostLabel(upgrade, selectedBuilding)));
            }
        }

        if (selectedDefenseTower)
        {
            actions.AddRange(new[] { ContentIds.Buildings.GunTower, ContentIds.Buildings.RocketTower }
                .Where(IsBuildingCommandAvailable)
                .Select(upgradeId =>
                {
                    var upgrade = _catalog.GetBuilding(upgradeId);
                    var key = upgradeId == ContentIds.Buildings.GunTower ? "G" : "T";
                    var validation = _simulation.ValidateBuildingUpgrade(selectedBuilding!.EntityId, upgradeId);
                    return new CommandPanelAction(
                        BuildingName(upgrade),
                        key,
                        BuildingDetail(upgrade, LocalizedUpgrade(validation)),
                        validation.Success,
                        () =>
                        {
                            var result = _simulation.TryUpgradeBuilding(selectedBuilding.EntityId, upgradeId);
                            _lastActionMessage = LocalizedUpgrade(result);
                        },
                        upgrade.Id,
                        L("ui.action_bar.cost", SimulationMessage.Args(("cost", upgrade.Cost))));
                }));
        }

        if (selectedUnits.Length > 0)
        {
            _commandPanel.UpdateActions(actions.ToArray());
            return;
        }

        if (selectedBuilding is not null)
        {
            _commandPanel.UpdateActions(actions.ToArray());
            return;
        }

        _commandPanel.UpdateActions(actions.ToArray());
    }

    private string GetSelectionHudLine()
    {
        if (_simulation is null || _catalog is null)
        {
            return string.Empty;
        }

        var selectedUnits = SelectedUnits().ToArray();
        if (selectedUnits.Length > 0)
        {
            var builders = selectedUnits.Count(unit => unit.Definition.CanConstruct);
            var combat = selectedUnits.Count(unit => unit.Definition.CanAttack);
            return builders > 0
                ? L("ui.selection.units_with_builders", SimulationMessage.Args(("count", selectedUnits.Length), ("builders", builders), ("combat", combat)))
                : L("ui.selection.units_combat", SimulationMessage.Args(("count", selectedUnits.Length), ("combat", combat)));
        }

        if (_selectedBuildingEntityId is null)
        {
            return L(
                "ui.selection.no_selection_training_hint",
                SimulationMessage.Args(("commands", GetTrainingCommandSummary())));
        }

        var building = _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
        if (building is null)
        {
            return string.Empty;
        }

        if (building.Definition.Id == ContentIds.Buildings.Barracks)
        {
            var grunt = LocalizedProduction(_simulation.ValidateUnitProduction(ContentIds.Units.Grunt, building.EntityId));
            return L(
                "ui.selection.barracks",
                SimulationMessage.Args(("commands", GetTrainingCommandSummary()), ("gruntReason", grunt)));
        }

        if (building.Definition.Id == ContentIds.Buildings.DefenseTower)
        {
            return L("ui.selection.defense_tower");
        }

        return L("ui.selection.building", SimulationMessage.Args(("building", BuildingName(building.Definition))));
    }

    private string UnitDetail(UnitDefinition unit, string commandHint)
    {
        var combatLine = unit.CanAttack
            ? L(
                "ui.detail.unit_combat",
                SimulationMessage.Args(
                    ("damage", $"{unit.AttackDamage:0}"),
                    ("range", $"{unit.AttackRange:0.0}"),
                    ("cooldown", $"{unit.AttackCooldown:0.0}")))
            : L("ui.detail.unit_noncombat");
        return L(
            "ui.detail.unit",
            SimulationMessage.Args(
                ("unit", UnitName(unit)),
                ("cost", unit.Cost),
                ("health", unit.Health),
                ("speed", $"{unit.MovementSpeed:0.0}"),
                ("train", $"{unit.TrainTimeSeconds:0.0}"),
                ("combat", combatLine),
                ("hint", commandHint)));
    }

    private string TrainingCostLabel(UnitDefinition unit, BuildingState? selectedBuilding)
    {
        var cost = L("ui.action_bar.cost", SimulationMessage.Args(("cost", unit.Cost)));
        if (selectedBuilding is null || selectedBuilding.Definition.Id != ContentIds.Buildings.Barracks)
        {
            return cost;
        }

        var queued = _simulation?.ProductionOrders.Count(order =>
            order.ProducerBuildingEntityId == selectedBuilding.EntityId &&
            order.UnitId == unit.Id) ?? 0;
        return queued <= 0
            ? cost
            : $"{cost} | {L("ui.action_bar.queued", SimulationMessage.Args(("count", queued)))}";
    }

    private string BarracksUpgradeCostLabel(BarracksUpgradeDefinition upgrade, BuildingState barracks)
    {
        if (barracks.IsBarracksUpgradeInProgress && barracks.ActiveBarracksUpgradeId == upgrade.Id)
        {
            return L(
                "ui.action_bar.progress_seconds",
                SimulationMessage.Args(("seconds", $"{Mathf.CeilToInt(barracks.ActiveBarracksUpgradeRemainingSeconds)}")));
        }

        return L("ui.action_bar.cost", SimulationMessage.Args(("cost", upgrade.Cost)));
    }

    private string BarracksUpgradeDetail(BarracksUpgradeDefinition upgrade, UpgradeResult validation)
    {
        return L(
            "ui.detail.barracks_upgrade",
            SimulationMessage.Args(
                ("upgradeId", upgrade.Id),
                ("upgrade", BarracksUpgradeName(upgrade)),
                ("cost", upgrade.Cost),
                ("time", $"{upgrade.DurationSeconds:0}"),
                ("requiredGrunts", upgrade.RequiredGruntCount),
                ("hint", LocalizedUpgrade(validation))));
    }

    private string BuildingDetail(BuildingDefinition building, string commandHint)
    {
        var roleLine = building.ProvidesPower
            ? L("ui.detail.building_power", SimulationMessage.Args(("radius", $"{building.PowerRadius:0.0}")))
            : building.ProvidesResourceExtraction
                ? L("ui.detail.building_extractor")
                : building.WallAnchor
                    ? L("ui.detail.building_wall", SimulationMessage.Args(("range", $"{building.WallLinkRange:0.0}")))
                    : building.AttackDamage > 0.0f
                        ? L(
                            "ui.detail.building_attack",
                            SimulationMessage.Args(
                                ("damage", $"{building.AttackDamage:0}"),
                                ("range", $"{building.AttackRange:0.0}"),
                                ("cooldown", $"{building.AttackCooldown:0.0}")))
                        : L("ui.detail.building_support");
        return L(
            "ui.detail.building",
            SimulationMessage.Args(
                ("building", BuildingName(building)),
                ("cost", building.Cost),
                ("health", building.Health),
                ("role", roleLine),
                ("hint", commandHint)));
    }

    private string BarracksUpgradeName(BarracksUpgradeDefinition definition)
    {
        return _localization?.ContentName(definition.Id, definition.DisplayName) ?? definition.DisplayName;
    }

}
