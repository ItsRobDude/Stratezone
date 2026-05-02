using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private const int MaxQueuedOrdersPerProducer = 5;

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

        if (_trainableUnitIds is not null && !_trainableUnitIds.Contains(unit.Id))
        {
            return new ProductionValidation(
                false,
                $"{unit.DisplayName} is not trainable in this mission.",
                null,
                null,
                "sim.production.not_trainable",
                SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
        }

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
            ? _buildings
                .Where(building =>
                    building.FactionId == factionId &&
                    building.Definition.Id == unit.AllowedByBuildingId &&
                    building.IsPowered &&
                    !building.IsDestroyed)
                .OrderBy(CountQueuedOrdersForProducer)
                .FirstOrDefault()
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

        if (CountQueuedOrdersForProducer(producer) >= MaxQueuedOrdersPerProducer)
        {
            return new ProductionValidation(
                false,
                "Training queue is full.",
                null,
                producer,
                "sim.production.queue_full",
                SimulationMessage.Args(("count", MaxQueuedOrdersPerProducer)));
        }

        return new ProductionValidation(
            true,
            $"Can train {unit.DisplayName}.",
            null,
            producer,
            "sim.production.can_train",
            SimulationMessage.Args(("unitId", unit.Id), ("unit", unit.DisplayName)));
    }

    private int CountQueuedOrdersForProducer(BuildingState producer)
    {
        return _productionOrders.Count(order => order.ProducerBuildingEntityId == producer.EntityId);
    }

    internal UnitDefinition? SelectEnemyProductionUnit()
    {
        var candidates = CandidateEnemyProductionUnits().ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        return candidates
            .OrderByDescending(ScoreEnemyProductionCandidate)
            .ThenByDescending(unit => unit.Cost)
            .First();
    }

    private IEnumerable<UnitDefinition> CandidateEnemyProductionUnits()
    {
        var unitIds = _trainableUnitIds ?? _catalog.Units.Keys;
        foreach (var unitId in unitIds)
        {
            var unit = _catalog.GetUnit(unitId);
            if (!unit.CanAttack)
            {
                continue;
            }

            if (ValidateUnitProductionForFaction(ContentIds.Factions.PrivateMilitary, unit.Id, null, EnemyMaterials).CanQueue)
            {
                yield return unit;
            }
        }
    }

    private int CountEnemyUnitsAndOrders(string unitId)
    {
        return _units.Count(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                unit.Definition.Id == unitId &&
                !unit.IsDestroyed) +
            _productionOrders.Count(order =>
                order.FactionId == ContentIds.Factions.PrivateMilitary &&
                order.UnitId == unitId);
    }

    private float ScoreEnemyProductionCandidate(UnitDefinition unit)
    {
        return EstimateCombatValue(unit) - (CountEnemyUnitsAndOrders(unit.Id) * 80.0f);
    }

    private static float EstimateCombatValue(UnitDefinition unit)
    {
        return unit.Health + (unit.AttackDamage * MathF.Max(1.0f, unit.AttackRange) / MathF.Max(0.1f, unit.AttackCooldown));
    }

    private void TickProduction(float deltaSeconds)
    {
        var activeOrders = _productionOrders
            .GroupBy(order => order.ProducerBuildingEntityId)
            .Select(group => group.First())
            .ToArray();

        foreach (var order in activeOrders)
        {
            order.RemainingSeconds -= deltaSeconds;
            if (order.RemainingSeconds > 0.0f)
            {
                continue;
            }

            CompleteProductionOrder(order);
            _productionOrders.Remove(order);
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

        var spawnIndex = _units.Count(unit =>
            unit.FactionId == order.FactionId &&
            !unit.IsDestroyed);
        var spawnOffset = GetProductionSpawnOffset(order.FactionId, spawnIndex);
        var unit = AddUnit(order.UnitId, order.FactionId, hub.Position + spawnOffset);
        _events.Add(new SimulationEvent(
            order.FactionId,
            "sim.event.training_complete",
            SimulationMessage.Args(("unitId", unit.Definition.Id), ("unit", unit.Definition.DisplayName))));
    }

    private static SimVector2 GetProductionSpawnOffset(string factionId, int spawnIndex)
    {
        var side = factionId == ContentIds.Factions.PrivateMilitary ? -1.0f : 1.0f;
        var lane = spawnIndex % 3;
        var row = spawnIndex / 3;
        var x = 70.0f + (row * 34.0f);
        var y = (lane - 1) * 42.0f;
        return new SimVector2(x * side, y);
    }
}
