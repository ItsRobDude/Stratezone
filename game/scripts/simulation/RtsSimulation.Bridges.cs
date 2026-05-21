using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private const float BridgeInteractionRange = 1.5f;
    // Bridges do not have build costs yet; full bridge repair is locked by smoke tests
    // at 60% of max health until map-object data grows a per-bridge repair override.
    private const float BridgeRepairMaterialCostPerHealth = FullRepairCostFraction;

    private void InitializeBridges(
        MapDefinition? map,
        IEnumerable<MissionMapObjectOverrideDefinition>? mapObjectOverrides)
    {
        if (map is null)
        {
            return;
        }

        var overrides = (mapObjectOverrides ?? [])
            .ToDictionary(item => item.ObjectId, item => item.StartingHealthPercent, StringComparer.Ordinal);
        foreach (var mapObject in map.MapObjects.Where(item => item.IsBridge))
        {
            var startingHealthPercent = overrides.TryGetValue(mapObject.Id, out var value)
                ? value
                : 1.0f;
            _bridges.Add(new BridgeState(mapObject, startingHealthPercent));
        }
    }

    public BridgeState? FindBridge(string bridgeId)
    {
        return _bridges.FirstOrDefault(bridge => string.Equals(bridge.Id, bridgeId, StringComparison.Ordinal));
    }

    public bool IsBridgeIntact(string bridgeId)
    {
        return string.IsNullOrWhiteSpace(bridgeId) || FindBridge(bridgeId)?.IsIntact == true;
    }

    public bool IsReachableForFaction(string factionId, SimVector2 start, SimVector2 destination)
    {
        return PathfindingSystem.FindPath(
            start,
            destination,
            _buildings,
            GetBlockingEnergyWallsForFaction(factionId),
            _map?.TerrainRegions ?? [],
            _bridges,
            _playableBounds).Success;
    }

    public bool DebugDamageBridge(string bridgeId, float amount)
    {
        var bridge = FindBridge(bridgeId);
        if (bridge is null)
        {
            return false;
        }

        DamageBridge(bridge, amount);
        return true;
    }

    private void DamageBridge(BridgeState bridge, float amount)
    {
        var wasIntact = bridge.IsIntact;
        var damaged = bridge.ApplyDamage(amount);
        if (damaged <= 0.0f || wasIntact == bridge.IsIntact)
        {
            return;
        }

        _events.Add(new SimulationEvent(
            ContentIds.Factions.PlayerExpedition,
            "sim.event.bridge_collapsed",
            SimulationMessage.Args(("bridgeId", bridge.Id), ("bridge", BridgeDisplayName(bridge)))));
    }

    private static string BridgeDisplayName(BridgeState bridge)
    {
        return string.Join(
            " ",
            bridge.Id.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part != "bridge")
                .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private bool TickUnitRepairBridge(UnitState unit, float deltaSeconds)
    {
        if (unit.RepairTargetBridgeId is null)
        {
            return false;
        }

        var bridge = FindBridge(unit.RepairTargetBridgeId);
        if (!unit.Definition.CanRepair || bridge is null)
        {
            unit.RepairTargetBridgeId = null;
            return false;
        }

        if (!bridge.IsDamaged)
        {
            unit.RepairTargetBridgeId = null;
            return true;
        }

        var repairRange = ToWorldRadius(BridgeInteractionRange);
        if (bridge.DistanceTo(unit.Position) > repairRange)
        {
            MoveUnitToward(unit, bridge.Center, deltaSeconds);
            return true;
        }

        unit.ClearPath();
        var materialsAvailable = GetMaterialsForFaction(unit.FactionId);
        if (materialsAvailable <= 0.0f)
        {
            return true;
        }

        var materialPerHealth = GetBridgeRepairMaterialCostPerHealth(bridge);
        var targetHealth = GruntRepairRatePerSecond * deltaSeconds;
        var affordableHealth = materialPerHealth <= 0.0f
            ? targetHealth
            : materialsAvailable / materialPerHealth;
        var repaired = bridge.Repair(MathF.Min(targetHealth, affordableHealth));
        if (repaired <= 0.0f)
        {
            return true;
        }

        SpendMaterialsForFaction(unit.FactionId, repaired * materialPerHealth);
        if (!bridge.IsDamaged)
        {
            unit.RepairTargetBridgeId = null;
            _events.Add(new SimulationEvent(
                unit.FactionId,
                "sim.event.bridge_repair_complete",
                SimulationMessage.Args(("bridgeId", bridge.Id), ("bridge", BridgeDisplayName(bridge)))));
        }

        return true;
    }

    private static float GetBridgeRepairMaterialCostPerHealth(BridgeState bridge)
    {
        return bridge.MaxHealth <= 0.0f
            ? 0.0f
            : BridgeRepairMaterialCostPerHealth;
    }
}
