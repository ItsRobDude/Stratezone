namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private readonly Queue<(long Id, SimulationCommand Command)> _pendingCommands = new();
    private readonly List<CommandReceipt> _commandReceipts = [];
    private long _nextCommandId;

    public long EnqueueCommand(SimulationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = ++_nextCommandId;
        if (MissionState.Status == MissionStatus.Active) _pendingCommands.Enqueue((id, command));
        else _commandReceipts.Add(new(id, new(false)));
        return id;
    }

    public IReadOnlyList<CommandReceipt> DrainCommandReceipts()
    {
        var result = _commandReceipts.ToArray();
        _commandReceipts.Clear();
        return result;
    }

    public void CancelPendingCommands()
    {
        while (_pendingCommands.TryDequeue(out var item)) _commandReceipts.Add(new(item.Id, new(false)));
    }

    public bool CancelPendingCommand(long commandId)
    {
        var cancelled = false;
        var count = _pendingCommands.Count;
        // Visit the original queue once, preserving every other command's order.
        for (var i = 0; i < count; i++)
        {
            var item = _pendingCommands.Dequeue();
            if (item.Id == commandId)
            {
                _commandReceipts.Add(new(item.Id, new(false)));
                cancelled = true;
            }
            else _pendingCommands.Enqueue(item);
        }
        return cancelled;
    }

    private void ApplyPendingCommands()
    {
        while (_pendingCommands.TryDequeue(out var item))
        {
            _commandReceipts.Add(new(item.Id, MissionState.Status == MissionStatus.Active
                ? ApplyPlayerCommand(item.Command) : new(false)));
        }
    }

    private bool IsPlayerUnit(int id) => FindLiveUnit(id)?.FactionId == ContentIds.Factions.PlayerExpedition;

    private CommandOutcome ApplyPlayerCommand(SimulationCommand command)
    {
        switch (command)
        {
            case MoveUnitCommand c when IsPlayerUnit(c.UnitId):
                CommandUnitMove(c.UnitId, c.Destination);
                return new(true);
            case AttackUnitCommand c when IsPlayerUnit(c.UnitId) && FindLiveUnit(c.UnitId)!.Definition.CanAttack:
                var unitTarget = FindLiveUnit(c.TargetId);
                if (unitTarget is null || unitTarget.FactionId == ContentIds.Factions.PlayerExpedition ||
                    !IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unitTarget.Position)) return new(false);
                CommandUnitAttackUnit(c.UnitId, c.TargetId, c.Offset);
                return new(true);
            case AttackBuildingCommand c when IsPlayerUnit(c.UnitId) && FindLiveUnit(c.UnitId)!.Definition.CanAttack:
                var buildingTarget = FindLiveBuilding(c.TargetId);
                if (buildingTarget is null || buildingTarget.FactionId == ContentIds.Factions.PlayerExpedition ||
                    !IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, buildingTarget.Position)) return new(false);
                CommandUnitAttackBuilding(c.UnitId, c.TargetId, c.Offset);
                return new(true);
            case AttackBridgeCommand c when IsPlayerUnit(c.UnitId) && FindLiveUnit(c.UnitId)!.Definition.CanAttack:
                if (FindBridge(c.BridgeId)?.IsIntact != true) return new(false);
                CommandUnitAttackBridge(c.UnitId, c.BridgeId, c.Offset);
                return new(true);
            case RepairBuildingCommand c when IsPlayerUnit(c.UnitId):
                var repair = CommandUnitRepairBuilding(c.UnitId, c.TargetId);
                return new(repair.Success, repair.MessageKey, repair.MessageArgs);
            case RepairBridgeCommand c when IsPlayerUnit(c.UnitId):
                var bridgeRepair = CommandUnitRepairBridge(c.UnitId, c.BridgeId);
                return new(bridgeRepair.Success, bridgeRepair.MessageKey, bridgeRepair.MessageArgs);
            case PlaceBuildingCommand c:
                if (!IsPlayerUnit(c.BuilderId) || FindLiveUnit(c.BuilderId)!.Definition.CanConstruct != true)
                    return new(false, "ui.action.select_grunt_before_building");
                var placement = TryPlaceBuilding(c.BuildingId, c.Position);
                return new(placement.Success, placement.MessageKey, placement.MessageArgs);
            case TrainUnitCommand c:
                var production = TryQueueUnit(c.UnitId, c.BarracksId);
                return new(production.Success, production.MessageKey, production.MessageArgs);
            case UpgradeBuildingCommand c:
                var upgrade = TryUpgradeBuilding(c.BuildingId, c.UpgradeId);
                return new(upgrade.Success, upgrade.MessageKey, upgrade.MessageArgs);
            case GuardianRetrofitCommand c:
                var retrofit = TryStartGuardianRetrofit(c.BarracksId);
                return new(retrofit.Success, retrofit.MessageKey, retrofit.MessageArgs);
            case KillCommanderCommand:
                var killed = DebugKillPlayerCommander();
                return new(killed, killed ? "ui.action.debug_commander_killed" : "ui.action.debug_commander_missing");
            default:
                return new(false);
        }
    }
}
