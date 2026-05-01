namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    public ProductionValidation ValidateUnitProduction(string unitId, int? producerBuildingEntityId = null)
    {
        return ValidateUnitProductionForFaction(ContentIds.Factions.PlayerExpedition, unitId, producerBuildingEntityId, Materials);
    }

    public ProductionResult TryQueueUnit(string unitId, int? producerBuildingEntityId = null)
    {
        return TryQueueUnitForFaction(ContentIds.Factions.PlayerExpedition, unitId, producerBuildingEntityId, 1.0f);
    }

    internal ProductionResult TryQueueUnitForFaction(string factionId, string unitId, int? producerBuildingEntityId, float trainTimeMultiplier)
    {
        var validation = ValidateUnitProductionForFaction(factionId, unitId, producerBuildingEntityId, GetMaterialsForFaction(factionId));
        if (!validation.CanQueue || validation.Producer is null)
        {
            return new ProductionResult(false, validation.Reason, null, validation.MessageKey, validation.MessageArgs);
        }

        var unit = _catalog.GetUnit(unitId);
        SpendMaterialsForFaction(factionId, unit.Cost);
        var order = new ProductionOrderState(
            unit.Id,
            factionId,
            validation.Producer.EntityId,
            MathF.Max(0.1f, unit.TrainTimeSeconds * trainTimeMultiplier));
        _productionOrders.Add(order);
        return new ProductionResult(
            true,
            $"Queued {unit.DisplayName}.",
            order,
            "sim.production.queued",
            SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
    }

    private ProductionValidation ValidateUnitProductionForFaction(string factionId, string unitId, int? producerBuildingEntityId, float availableMaterials)
    {
        var unit = _catalog.GetUnit(unitId);

        if (unit.AllowedByBuildingId is null || unit.SpawnBuildingId is null)
        {
            return new ProductionValidation(
                false,
                $"{unit.DisplayName} cannot be trained in this mission.",
                null,
                null,
                "sim.production.not_trainable",
                SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
        }

        if (availableMaterials < unit.Cost)
        {
            return new ProductionValidation(false, $"Need {unit.Cost:0} materials.", null, null, "sim.need_materials", SimulationMessage.Args(("amount", unit.Cost)));
        }

        if (_productionOrders.Any(order => order.FactionId == factionId))
        {
            return new ProductionValidation(false, "Training queue is busy.", null, null, "sim.production.queue_busy");
        }

        var spawn = _buildings.FirstOrDefault(building =>
            building.FactionId == factionId &&
            building.Definition.Id == unit.SpawnBuildingId &&
            !building.IsDestroyed);
        if (spawn is null)
        {
            var spawnDefinition = _catalog.GetBuilding(unit.SpawnBuildingId);
            return new ProductionValidation(
                false,
                $"Requires live {spawnDefinition.DisplayName}.",
                null,
                null,
                "sim.production.requires_live_building",
                SimulationMessage.Args(("buildingId", spawnDefinition.Id), ("building", spawnDefinition.DisplayName)));
        }

        var producer = producerBuildingEntityId is null
            ? _buildings.FirstOrDefault(building =>
                building.FactionId == factionId &&
                building.Definition.Id == unit.AllowedByBuildingId &&
                building.IsPowered &&
                !building.IsDestroyed)
            : _buildings.FirstOrDefault(building =>
                building.EntityId == producerBuildingEntityId.Value &&
                building.FactionId == factionId &&
                building.Definition.Id == unit.AllowedByBuildingId &&
                !building.IsDestroyed);

        if (producer is null)
        {
            var producerDefinition = _catalog.GetBuilding(unit.AllowedByBuildingId);
            return new ProductionValidation(
                false,
                $"Requires live {producerDefinition.DisplayName}.",
                null,
                null,
                "sim.production.requires_live_building",
                SimulationMessage.Args(("buildingId", producerDefinition.Id), ("building", producerDefinition.DisplayName)));
        }

        if (!producer.IsPowered)
        {
            return new ProductionValidation(
                false,
                $"{producer.Definition.DisplayName} is unpowered.",
                null,
                producer,
                "sim.production.producer_unpowered",
                SimulationMessage.Args(("buildingId", producer.Definition.Id), ("building", producer.Definition.DisplayName)));
        }

        if (unit.RequiredAddonBuildingId is not null &&
            !HasPoweredBuilding(factionId, unit.RequiredAddonBuildingId))
        {
            var addon = _catalog.GetBuilding(unit.RequiredAddonBuildingId);
            return new ProductionValidation(
                false,
                $"Requires powered {addon.DisplayName}.",
                null,
                producer,
                "sim.production.requires_powered_addon",
                SimulationMessage.Args(("buildingId", addon.Id), ("building", addon.DisplayName)));
        }

        return new ProductionValidation(
            true,
            $"Can train {unit.DisplayName}.",
            null,
            producer,
            "sim.production.can_train",
            SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
    }

    private void TickProduction(float deltaSeconds)
    {
        for (var index = _productionOrders.Count - 1; index >= 0; index--)
        {
            var order = _productionOrders[index];
            order.RemainingSeconds -= deltaSeconds;
            if (order.RemainingSeconds > 0.0f)
            {
                continue;
            }

            CompleteProductionOrder(order);
            _productionOrders.RemoveAt(index);
        }
    }

    private void CompleteProductionOrder(ProductionOrderState order)
    {
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == order.FactionId &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);

        if (hub is null)
        {
            return;
        }

        var spawnOffset = order.FactionId == ContentIds.Factions.PrivateMilitary
            ? new SimVector2(-70, 0)
            : new SimVector2(70, 0);
        var unit = AddUnit(order.UnitId, order.FactionId, hub.Position + spawnOffset);
        _events.Add(new SimulationEvent(
            order.FactionId,
            "sim.event.training_complete",
            SimulationMessage.Args(("unitId", unit.Definition.Id), ("unit", unit.Definition.DisplayName))));
    }
}
