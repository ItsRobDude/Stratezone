using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private const float WorkerRepairRatePerSecond = 24.0f;
    private const float FullRepairCostFraction = 0.6f;
    private const float RepairInteractionRange = 1.5f;

    public RepairResult CommandUnitRepairBuilding(int unitEntityId, int targetBuildingEntityId)
    {
        var validation = ValidateUnitRepairBuilding(unitEntityId, targetBuildingEntityId);
        if (!validation.Success || validation.Worker is null || validation.Building is null)
        {
            return validation;
        }

        validation.Worker.TargetUnitEntityId = null;
        validation.Worker.TargetBuildingEntityId = null;
        validation.Worker.RepairTargetBuildingEntityId = validation.Building.EntityId;
        validation.Worker.TargetFormationOffset = default;
        validation.Worker.ClearPath();
        return validation;
    }

    public RepairResult ValidateUnitRepairBuilding(int unitEntityId, int targetBuildingEntityId)
    {
        var unit = FindLiveUnit(unitEntityId);
        var building = FindLiveBuilding(targetBuildingEntityId);
        if (unit is null || !unit.Definition.CanRepair)
        {
            return new RepairResult(false, "Select a Worker.", null, building, "sim.repair.requires_worker");
        }

        if (building is null || building.FactionId != unit.FactionId)
        {
            return new RepairResult(false, "Select a damaged friendly structure.", unit, null, "sim.repair.requires_friendly_building");
        }

        if (!building.IsDamaged)
        {
            return new RepairResult(
                false,
                $"{building.Definition.DisplayName} does not need repair.",
                unit,
                building,
                "sim.repair.not_damaged",
                SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
        }

        if (GetMaterialsForFaction(unit.FactionId) <= 0.0f)
        {
            return new RepairResult(false, "Need materials for repair.", unit, building, "sim.repair.need_materials");
        }

        return new RepairResult(
            true,
            $"Repairing {building.Definition.DisplayName}.",
            unit,
            building,
            "sim.repair.started",
            SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
    }

    private bool TickUnitRepair(UnitState unit, float deltaSeconds)
    {
        if (unit.RepairTargetBuildingEntityId is null)
        {
            return false;
        }

        var target = FindLiveBuilding(unit.RepairTargetBuildingEntityId.Value);
        if (!unit.Definition.CanRepair ||
            target is null ||
            target.FactionId != unit.FactionId)
        {
            unit.RepairTargetBuildingEntityId = null;
            return false;
        }

        if (!target.IsDamaged)
        {
            unit.RepairTargetBuildingEntityId = null;
            return true;
        }

        var repairRange = target.FootprintWorldRadius + ToWorldRadius(RepairInteractionRange);
        if (unit.Position.DistanceTo(target.Position) > repairRange)
        {
            MoveUnitToward(unit, target.Position, deltaSeconds);
            return true;
        }

        unit.ClearPath();
        var materialsAvailable = GetMaterialsForFaction(unit.FactionId);
        if (materialsAvailable <= 0.0f)
        {
            return true;
        }

        var materialPerHealth = GetRepairMaterialCostPerHealth(target.Definition);
        var targetHealth = WorkerRepairRatePerSecond * deltaSeconds;
        var affordableHealth = materialPerHealth <= 0.0f
            ? targetHealth
            : materialsAvailable / materialPerHealth;
        var repaired = target.Repair(MathF.Min(targetHealth, affordableHealth));
        if (repaired <= 0.0f)
        {
            return true;
        }

        SpendMaterialsForFaction(unit.FactionId, repaired * materialPerHealth);
        if (!target.IsDamaged)
        {
            unit.RepairTargetBuildingEntityId = null;
            _events.Add(new SimulationEvent(
                unit.FactionId,
                "sim.event.repair_complete",
                SimulationMessage.Args(("buildingId", target.Definition.Id), ("building", target.Definition.DisplayName))));
        }

        return true;
    }

    private static float GetRepairMaterialCostPerHealth(BuildingDefinition definition)
    {
        return definition.Health <= 0
            ? 0.0f
            : definition.Cost * FullRepairCostFraction / definition.Health;
    }
}
