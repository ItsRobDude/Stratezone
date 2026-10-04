namespace Stratezone.Simulation;

// Immutable values captured at input time. Setup helpers and AI apply actions directly;
// player input enters through EnqueueCommand and executes at the start of Step.
public abstract record SimulationCommand;
public sealed record MoveUnitCommand(int UnitId, SimVector2 Destination) : SimulationCommand;
public sealed record AttackUnitCommand(int UnitId, int TargetId, SimVector2 Offset = default) : SimulationCommand;
public sealed record AttackBuildingCommand(int UnitId, int TargetId, SimVector2 Offset = default) : SimulationCommand;
public sealed record AttackBridgeCommand(int UnitId, string BridgeId, SimVector2 Offset = default) : SimulationCommand;
public sealed record RepairBuildingCommand(int UnitId, int TargetId) : SimulationCommand;
public sealed record RepairBridgeCommand(int UnitId, string BridgeId) : SimulationCommand;
public sealed record PlaceBuildingCommand(string BuildingId, SimVector2 Position, int BuilderId) : SimulationCommand;
public sealed record TrainUnitCommand(string UnitId, int BarracksId) : SimulationCommand;
public sealed record UpgradeBuildingCommand(int BuildingId, string UpgradeId) : SimulationCommand;
public sealed record GuardianRetrofitCommand(int BarracksId) : SimulationCommand;
public sealed record KillCommanderCommand : SimulationCommand;

public sealed record CommandOutcome(bool Success, string MessageKey = "", IReadOnlyDictionary<string, string>? MessageArgs = null);
public sealed record CommandReceipt(long CommandId, CommandOutcome Outcome);
