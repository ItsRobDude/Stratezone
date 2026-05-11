using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    public UpgradeResult ValidateGuardianRetrofit(int barracksEntityId)
    {
        return ValidateBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit, barracksEntityId);
    }

    public UpgradeResult TryStartGuardianRetrofit(int barracksEntityId)
    {
        return TryStartBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit, barracksEntityId);
    }

    public UpgradeResult ValidateBarracksUpgrade(string upgradeId, int barracksEntityId)
    {
        return ValidateBarracksUpgradeForFaction(ContentIds.Factions.PlayerExpedition, upgradeId, barracksEntityId);
    }

    public UpgradeResult TryStartBarracksUpgrade(string upgradeId, int barracksEntityId)
    {
        return TryStartBarracksUpgradeForFaction(ContentIds.Factions.PlayerExpedition, upgradeId, barracksEntityId);
    }

    internal UpgradeResult TryStartBarracksUpgradeForFaction(string factionId, string upgradeId, int barracksEntityId)
    {
        var validation = ValidateBarracksUpgradeForFaction(factionId, upgradeId, barracksEntityId);
        if (!validation.Success || validation.Building is null)
        {
            return validation;
        }

        var upgrade = _catalog.GetBarracksUpgrade(upgradeId);
        SpendMaterialsForFaction(factionId, upgrade.Cost);
        validation.Building.StartBarracksUpgrade(upgrade.Id, upgrade.DurationSeconds);
        return new UpgradeResult(
            true,
            $"Started {upgrade.DisplayName}.",
            validation.Building,
            "sim.barracks_upgrade.started",
            BarracksUpgradeMessageArgs(upgrade));
    }

    internal bool TryStartEnemyGuardianRetrofitIfReady()
    {
        return TryGetEnemyGuardianRetrofitTarget(out var barracks, out _) &&
            TryStartBarracksUpgradeForFaction(
                ContentIds.Factions.PrivateMilitary,
                ContentIds.BarracksUpgrades.GuardianRetrofit,
                barracks!.EntityId).Success;
    }

    internal bool ShouldEnemyTrainGuardianRetrofitGrunt()
    {
        if (!TryGetEnemyGuardianRetrofitTarget(out var barracks, out var upgrade) ||
            !IsExplicitlyTrainableInMission(ContentIds.Units.Grunt))
        {
            return false;
        }

        if (CountBarracksUpgradeGrunts(ContentIds.Factions.PrivateMilitary, barracks!, upgrade!) >= upgrade!.RequiredGruntCount)
        {
            return false;
        }

        return ValidateUnitProductionForFaction(
            ContentIds.Factions.PrivateMilitary,
            ContentIds.Units.Grunt,
            barracks!.EntityId,
            EnemyMaterials).CanQueue;
    }

    internal bool ShouldEnemyHoldProductionForGuardianRetrofit()
    {
        if (!TryGetEnemyGuardianRetrofitTarget(out var barracks, out var upgrade))
        {
            return false;
        }

        var gruntsReady = CountBarracksUpgradeGrunts(ContentIds.Factions.PrivateMilitary, barracks!, upgrade!);
        if (gruntsReady < upgrade!.RequiredGruntCount)
        {
            return IsExplicitlyTrainableInMission(ContentIds.Units.Grunt) &&
                EnemyMaterials < _catalog.GetUnit(ContentIds.Units.Grunt).Cost;
        }

        return ValidateBarracksUpgradeForFaction(
            ContentIds.Factions.PrivateMilitary,
            upgrade.Id,
            barracks!.EntityId).MessageKey == "sim.need_materials";
    }

    private bool TryGetEnemyGuardianRetrofitTarget(
        out BuildingState? barracks,
        out BarracksUpgradeDefinition? upgrade)
    {
        barracks = null;
        upgrade = null;
        if (!IsExplicitlyTrainableInMission(ContentIds.Units.Guardian))
        {
            return false;
        }

        upgrade = _catalog.GetBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit);
        var upgradeId = upgrade.Id;
        barracks = _buildings
            .Where(building =>
                building.FactionId == ContentIds.Factions.PrivateMilitary &&
                building.Definition.Id == ContentIds.Buildings.Barracks &&
                !building.IsDestroyed &&
                !building.IsBarracksUpgradeInProgress &&
                !building.HasBarracksUpgrade(upgradeId))
            .OrderBy(building => building.Position.DistanceTo(_enemyAi.HubPosition))
            .FirstOrDefault();

        return barracks is not null;
    }

    private UpgradeResult ValidateBarracksUpgradeForFaction(string factionId, string upgradeId, int barracksEntityId)
    {
        var building = FindLiveBuilding(barracksEntityId);
        if (building is null || building.FactionId != factionId)
        {
            return new UpgradeResult(false, "Select a live friendly Barracks.", null, "sim.upgrade.select_live_friendly");
        }

        var upgrade = _catalog.GetBarracksUpgrade(upgradeId);
        if (building.Definition.Id != ContentIds.Buildings.Barracks)
        {
            return new UpgradeResult(
                false,
                "Select a Barracks.",
                null,
                "sim.barracks_upgrade.requires_barracks",
                BarracksUpgradeMessageArgs(upgrade));
        }

        var unavailableUnitId = GetUnavailableUpgradeUnitId(upgrade);
        if (unavailableUnitId is not null)
        {
            var unit = _catalog.GetUnit(unavailableUnitId);
            return new UpgradeResult(
                false,
                $"{unit.DisplayName} cannot be trained in this mission.",
                building,
                "sim.production.not_trainable",
                SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
        }

        if (building.HasBarracksUpgrade(upgrade.Id))
        {
            return new UpgradeResult(
                false,
                $"{upgrade.DisplayName} is already complete.",
                building,
                "sim.barracks_upgrade.already_complete",
                BarracksUpgradeMessageArgs(upgrade));
        }

        if (building.IsBarracksUpgradeInProgress)
        {
            return new UpgradeResult(
                false,
                $"{upgrade.DisplayName} in progress.",
                building,
                "sim.barracks_upgrade.in_progress",
                BarracksUpgradeMessageArgs(upgrade, ("seconds", $"{MathF.Ceiling(building.ActiveBarracksUpgradeRemainingSeconds):0}")));
        }

        if (upgrade.RequiresColonyHub && !HasLiveBuilding(factionId, ContentIds.Buildings.ColonyHub))
        {
            return new UpgradeResult(
                false,
                "Requires live Colony Hub.",
                building,
                "sim.barracks_upgrade.requires_colony_hub",
                BarracksUpgradeMessageArgs(upgrade));
        }

        if (upgrade.RequiresPoweredBarracks && !building.IsPowered)
        {
            return new UpgradeResult(
                false,
                "Barracks is unpowered.",
                building,
                "sim.barracks_upgrade.requires_powered_barracks",
                BarracksUpgradeMessageArgs(upgrade));
        }

        if (CountQueuedOrdersForProducer(building) > 0)
        {
            return new UpgradeResult(
                false,
                "Clear the Barracks training queue first.",
                building,
                "sim.barracks_upgrade.queue_busy",
                BarracksUpgradeMessageArgs(upgrade));
        }

        var availableGrunts = CountBarracksUpgradeGrunts(factionId, building, upgrade);
        if (availableGrunts < upgrade.RequiredGruntCount)
        {
            return new UpgradeResult(
                false,
                $"Requires {upgrade.RequiredGruntCount} Grunts at the base.",
                building,
                "sim.barracks_upgrade.requires_grunts",
                BarracksUpgradeMessageArgs(
                    upgrade,
                    ("required", upgrade.RequiredGruntCount.ToString()),
                    ("count", availableGrunts.ToString())));
        }

        if (GetMaterialsForFaction(factionId) < upgrade.Cost)
        {
            return new UpgradeResult(
                false,
                $"Need {upgrade.Cost:0} materials.",
                building,
                "sim.need_materials",
                SimulationMessage.Args(("amount", upgrade.Cost)));
        }

        return new UpgradeResult(
            true,
            $"Can start {upgrade.DisplayName}.",
            building,
            "sim.barracks_upgrade.can_start",
            BarracksUpgradeMessageArgs(upgrade));
    }

    private string? GetUnavailableUpgradeUnitId(BarracksUpgradeDefinition upgrade)
    {
        if (_trainableUnitIds is null)
        {
            return null;
        }

        return upgrade.UnlockUnitIds.FirstOrDefault(unitId => !_trainableUnitIds.Contains(unitId));
    }

    private int CountBarracksUpgradeGrunts(string factionId, BuildingState barracks, BarracksUpgradeDefinition upgrade)
    {
        var range = ToWorldRadius(upgrade.RequiredGruntRange);
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == factionId &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);

        return _units.Count(unit =>
            unit.FactionId == factionId &&
            unit.Definition.Id == ContentIds.Units.Grunt &&
            !unit.IsDestroyed &&
            (unit.Position.DistanceTo(barracks.Position) <= range ||
                hub is not null && unit.Position.DistanceTo(hub.Position) <= range));
    }

    private void TickBarracksUpgrades(float deltaSeconds)
    {
        foreach (var building in _buildings.Where(building => building.IsBarracksUpgradeInProgress && !building.IsDestroyed))
        {
            var upgrade = _catalog.GetBarracksUpgrade(building.ActiveBarracksUpgradeId!);
            if (upgrade.RequiresPoweredBarracks && !building.IsPowered)
            {
                continue;
            }

            if (!building.TickBarracksUpgrade(deltaSeconds))
            {
                continue;
            }

            _events.Add(new SimulationEvent(
                building.FactionId,
                "sim.event.barracks_upgrade_complete",
                BarracksUpgradeMessageArgs(upgrade)));
        }
    }

    private static IReadOnlyDictionary<string, string> BarracksUpgradeMessageArgs(
        BarracksUpgradeDefinition upgrade,
        params (string Key, string Value)[] extraArgs)
    {
        var args = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["upgradeId"] = upgrade.Id,
            ["upgrade"] = upgrade.DisplayName,
            ["cost"] = upgrade.Cost.ToString(),
            ["time"] = $"{upgrade.DurationSeconds:0}",
            ["requiredGrunts"] = upgrade.RequiredGruntCount.ToString()
        };

        foreach (var (key, value) in extraArgs)
        {
            args[key] = value;
        }

        return args;
    }
}
