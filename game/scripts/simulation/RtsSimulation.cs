using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class RtsSimulation
{
    public const float ContentUnitScale = 24.0f;
    private const float ResourceWellCoreRadius = 28.0f;
    private const float CombatMovementScale = 95.0f;
    private const float EnemyIncomeMultiplier = 0.75f;
    private const float EnemyTrainTimeMultiplier = 1.25f;

    public static SimVector2 EnemyHubPosition => new(620, 120);
    public static SimVector2 EnemyPowerPlantPosition => new(380, 120);
    public static SimVector2 EnemyBarracksPosition => new(500, -80);
    public static SimVector2 EnemyExtractorPosition => new(220, 30);
    public static SimVector2 EnemyDefenseTowerPosition => new(340, -70);

    private readonly ContentCatalog _catalog;
    private readonly List<BuildingState> _buildings = [];
    private readonly List<UnitState> _units = [];
    private readonly List<ProductionOrderState> _productionOrders = [];
    private readonly List<ResourceWellState> _resourceWells = [];
    private readonly List<EnergyWallSegment> _energyWalls = [];
    private int _nextEntityId = 1;

    public RtsSimulation(ContentCatalog catalog, int startingMaterials, IEnumerable<(string WellId, SimVector2 Position)> resourceWellPlacements, int enemyStartingMaterials = 0)
    {
        _catalog = catalog;
        Materials = startingMaterials;
        EnemyMaterials = enemyStartingMaterials;

        foreach (var placement in resourceWellPlacements)
        {
            _resourceWells.Add(new ResourceWellState(_catalog.GetResourceWell(placement.WellId), placement.Position));
        }
    }

    public float Materials { get; private set; }
    public float EnemyMaterials { get; private set; }
    public IReadOnlyList<BuildingState> Buildings => _buildings;
    public IReadOnlyList<UnitState> Units => _units;
    public IReadOnlyList<ProductionOrderState> ProductionOrders => _productionOrders;
    public IReadOnlyList<ResourceWellState> ResourceWells => _resourceWells;
    public IReadOnlyList<EnergyWallSegment> EnergyWalls => _energyWalls;
    public bool ColonyHubDestroyed => _buildings.Any(building => building.FactionId == ContentIds.Factions.PlayerExpedition && building.Definition.Id == ContentIds.Buildings.ColonyHub && building.IsDestroyed);
    public bool EnemyProductionOnline => HasPoweredBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.Barracks) &&
        HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub);
    public string ObjectiveText
    {
        get
        {
            if (ColonyHubDestroyed)
            {
                return "Objective failed: Colony Hub destroyed.";
            }

            if (Units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && !unit.IsDestroyed))
            {
                return "Objective: defend the Colony Hub from produced enemy units.";
            }

            return EnemyProductionOnline
                ? "Objective: enemy Barracks is producing from limited resources."
                : "Objective: scout and disrupt the enemy base.";
        }
    }

    public static float ToWorldRadius(float contentRadius)
    {
        return contentRadius * ContentUnitScale;
    }

    public BuildingState AddStartingBuilding(string buildingId, SimVector2 position, string factionId = ContentIds.Factions.PlayerExpedition)
    {
        var definition = _catalog.GetBuilding(buildingId);
        var building = new BuildingState(_nextEntityId++, definition, factionId, position, null)
        {
            IsPowered = !definition.RequiresPower
        };

        _buildings.Add(building);
        RecomputePower();
        return building;
    }

    public PlacementValidation ValidatePlacement(string buildingId, SimVector2 position)
    {
        return ValidatePlacementForFaction(ContentIds.Factions.PlayerExpedition, buildingId, position, Materials);
    }

    private PlacementValidation ValidatePlacementForFaction(string factionId, string buildingId, SimVector2 position, float availableMaterials)
    {
        var definition = _catalog.GetBuilding(buildingId);

        if (availableMaterials < definition.Cost)
        {
            return new PlacementValidation(false, $"Need {definition.Cost:0} materials.");
        }

        foreach (var building in _buildings)
        {
            if (building.IsDestroyed)
            {
                continue;
            }

            var requiredDistance = building.OccupancyRadius + ToWorldRadius(definition.FootprintRadius + definition.PlacementBuffer);
            if (building.Position.DistanceTo(position) < requiredDistance)
            {
                return new PlacementValidation(false, $"Blocked by {building.Definition.DisplayName}.");
            }
        }

        var targetWell = FindCompatibleResourceWell(definition, position);
        if (definition.ProvidesResourceExtraction && targetWell is null)
        {
            return new PlacementValidation(false, "Extractor must be placed on an open resource well.");
        }

        if (definition.RequiresPower && !WouldBePowered(factionId, definition, position))
        {
            return new PlacementValidation(false, "Must be placed inside powered support.");
        }

        return new PlacementValidation(true, "Placement legal.", targetWell?.Definition.Id);
    }

    public PlacementResult TryPlaceBuilding(string buildingId, SimVector2 position)
    {
        return TryPlaceBuildingForFaction(ContentIds.Factions.PlayerExpedition, buildingId, position);
    }

    private PlacementResult TryPlaceBuildingForFaction(string factionId, string buildingId, SimVector2 position)
    {
        var availableMaterials = GetMaterialsForFaction(factionId);
        var validation = ValidatePlacementForFaction(factionId, buildingId, position, availableMaterials);
        if (!validation.IsLegal)
        {
            return new PlacementResult(false, validation.Reason);
        }

        var definition = _catalog.GetBuilding(buildingId);
        SpendMaterialsForFaction(factionId, definition.Cost);

        var building = new BuildingState(_nextEntityId++, definition, factionId, position, validation.ResourceWellId);
        _buildings.Add(building);

        if (validation.ResourceWellId is not null)
        {
            var well = _resourceWells.First(resourceWell => resourceWell.Definition.Id == validation.ResourceWellId);
            well.ExtractorEntityId = building.EntityId;
        }

        RecomputePower();
        return new PlacementResult(true, $"Placed {definition.DisplayName}.", building);
    }

    public void Tick(float deltaSeconds)
    {
        RecomputePower();
        TickEnemyConstructionPlanner();
        TickEnemyProduction(deltaSeconds);
        TickEnemyPressure(deltaSeconds);

        foreach (var extractor in _buildings.Where(building => building.Definition.ProvidesResourceExtraction && building.IsPowered && !building.IsDestroyed))
        {
            if (extractor.ResourceWellId is null)
            {
                continue;
            }

            var well = _resourceWells.FirstOrDefault(resourceWell => resourceWell.Definition.Id == extractor.ResourceWellId);
            if (well is null || well.IsDepleted)
            {
                continue;
            }

            var extracted = well.Definition.ExtractionRate * deltaSeconds;
            if (well.Definition.Depletes)
            {
                extracted = MathF.Min(extracted, well.Remaining);
                well.Remaining -= extracted;
            }

            if (extractor.FactionId == ContentIds.Factions.PrivateMilitary)
            {
                EnemyMaterials += extracted * EnemyIncomeMultiplier;
            }
            else
            {
                Materials += extracted;
            }
        }
    }

    public UnitState AddUnit(string unitId, string factionId, SimVector2 position)
    {
        var unit = new UnitState(_nextEntityId++, _catalog.GetUnit(unitId), factionId, position);
        _units.Add(unit);
        return unit;
    }

    public bool IsLineBlockedByEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.Any(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private void TickEnemyConstructionPlanner()
    {
        if (!HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub))
        {
            return;
        }

        TryEnsureEnemyBuilding(ContentIds.Buildings.PowerPlant, EnemyPowerPlantPosition);
        TryEnsureEnemyBuilding(ContentIds.Buildings.Barracks, EnemyBarracksPosition);
        TryEnsureEnemyBuilding(ContentIds.Buildings.ExtractorRefinery, EnemyExtractorPosition);
        TryEnsureEnemyBuilding(ContentIds.Buildings.DefenseTower, EnemyDefenseTowerPosition);
    }

    private void TryEnsureEnemyBuilding(string buildingId, SimVector2 position)
    {
        if (HasLiveBuilding(ContentIds.Factions.PrivateMilitary, buildingId))
        {
            return;
        }

        TryPlaceBuildingForFaction(ContentIds.Factions.PrivateMilitary, buildingId, position);
    }

    private void TickEnemyProduction(float deltaSeconds)
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

        TryStartEnemyProduction();
    }

    private void TryStartEnemyProduction()
    {
        if (_productionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary))
        {
            return;
        }

        if (!EnemyProductionOnline)
        {
            return;
        }

        var rifleman = _catalog.GetUnit(ContentIds.Units.Rifleman);
        if (EnemyMaterials < rifleman.Cost)
        {
            return;
        }

        EnemyMaterials -= rifleman.Cost;
        _productionOrders.Add(new ProductionOrderState(
            rifleman.Id,
            ContentIds.Factions.PrivateMilitary,
            rifleman.TrainTimeSeconds * EnemyTrainTimeMultiplier));
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

        AddUnit(order.UnitId, order.FactionId, hub.Position + new SimVector2(-70, 0));
    }

    private void TickEnemyPressure(float deltaSeconds)
    {
        foreach (var unit in _units.Where(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && !unit.IsDestroyed))
        {
            unit.AttackCooldownRemaining = MathF.Max(0.0f, unit.AttackCooldownRemaining - deltaSeconds);

            var target = GetEnemyTargetBuilding(unit);
            unit.TargetBuildingEntityId = target?.EntityId;
            if (target is null)
            {
                continue;
            }

            var distanceToTarget = unit.Position.DistanceTo(target.Position);
            var attackRange = ToWorldRadius(unit.Definition.AttackRange) + target.FootprintWorldRadius;
            if (unit.Definition.CanAttack && distanceToTarget <= attackRange)
            {
                TryUnitAttackBuilding(unit, target);
                continue;
            }

            var direction = target.Position - unit.Position;
            var distance = direction.Length();
            if (distance <= 0.001f)
            {
                continue;
            }

            var stepDistance = unit.Definition.MovementSpeed * CombatMovementScale * deltaSeconds;
            unit.Position += direction.Normalized() * MathF.Min(stepDistance, distance);
        }
    }

    private BuildingState? GetEnemyTargetBuilding(UnitState unit)
    {
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        if (hub is null)
        {
            unit.IsBlockedByEnergyWall = false;
            return null;
        }

        var blockingWall = FindBlockingEnergyWall(unit.Position, hub.Position);
        unit.IsBlockedByEnergyWall = blockingWall is not null;
        if (blockingWall is null)
        {
            return hub;
        }

        var startAnchor = FindLiveBuilding(blockingWall.StartAnchorEntityId);
        var endAnchor = FindLiveBuilding(blockingWall.EndAnchorEntityId);
        return new[] { startAnchor, endAnchor }
            .Where(anchor => anchor is not null)
            .OrderBy(anchor => anchor!.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private void TryUnitAttackBuilding(UnitState unit, BuildingState target)
    {
        if (unit.AttackCooldownRemaining > 0.0f || target.IsDestroyed || unit.Definition.AttackDamage <= 0.0f)
        {
            return;
        }

        target.ApplyDamage(unit.Definition.AttackDamage, unit.Definition.DamageType);
        unit.AttackCooldownRemaining = MathF.Max(0.05f, unit.Definition.AttackCooldown);
        RecomputePower();
    }

    private EnergyWallSegment? FindBlockingEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.FirstOrDefault(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private BuildingState? FindLiveBuilding(int entityId)
    {
        return _buildings.FirstOrDefault(building => building.EntityId == entityId && !building.IsDestroyed);
    }

    private bool HasPoweredBuilding(string factionId, string buildingId)
    {
        return _buildings.Any(building =>
            building.FactionId == factionId &&
            building.Definition.Id == buildingId &&
            building.IsPowered &&
            !building.IsDestroyed);
    }

    private bool HasLiveBuilding(string factionId, string buildingId)
    {
        return _buildings.Any(building =>
            building.FactionId == factionId &&
            building.Definition.Id == buildingId &&
            !building.IsDestroyed);
    }

    private ResourceWellState? FindCompatibleResourceWell(BuildingDefinition definition, SimVector2 position)
    {
        if (!definition.ProvidesResourceExtraction || definition.ExtractorResourceId is null)
        {
            return null;
        }

        return _resourceWells
            .Where(well => well.Definition.ResourceId == definition.ExtractorResourceId)
            .Where(well => well.ExtractorEntityId is null)
            .Where(well => !well.IsDepleted)
            .Where(well => well.Position.DistanceTo(position) <= ResourceWellCoreRadius + ToWorldRadius(definition.FootprintRadius))
            .OrderBy(well => well.Position.DistanceTo(position))
            .FirstOrDefault();
    }

    private float GetMaterialsForFaction(string factionId)
    {
        return factionId == ContentIds.Factions.PrivateMilitary ? EnemyMaterials : Materials;
    }

    private void SpendMaterialsForFaction(string factionId, float amount)
    {
        if (factionId == ContentIds.Factions.PrivateMilitary)
        {
            EnemyMaterials -= amount;
            return;
        }

        Materials -= amount;
    }

    private bool WouldBePowered(string factionId, BuildingDefinition definition, SimVector2 position)
    {
        if (!definition.RequiresPower)
        {
            return true;
        }

        var targetRadius = ToWorldRadius(definition.FootprintRadius);
        return definition.Id == ContentIds.Buildings.Pylon
            ? IsPylonSupported(factionId, position, targetRadius)
            : IsPositionPowered(factionId, position, targetRadius);
    }

    private bool IsPositionPowered(string factionId, SimVector2 position, float targetRadius)
    {
        return _buildings.Any(building =>
            building.FactionId == factionId &&
            building.Definition.ProvidesPower &&
            building.IsPowered &&
            building.Definition.PowerRadius > 0 &&
            building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PowerRadius) + targetRadius);
    }

    private bool IsPylonSupported(string factionId, SimVector2 position, float targetRadius)
    {
        foreach (var building in _buildings.Where(building =>
            building.FactionId == factionId &&
            building.Definition.ProvidesPower &&
            building.IsPowered))
        {
            if (IsInsideLocalPowerField(building, position, targetRadius))
            {
                return true;
            }

            if (building.Definition.Id == ContentIds.Buildings.Pylon &&
                building.Definition.PylonLinkRange > 0 &&
                building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PylonLinkRange))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideLocalPowerField(BuildingState building, SimVector2 position, float targetRadius)
    {
        return building.Definition.PowerRadius > 0 &&
            building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PowerRadius) + targetRadius;
    }

    private void RecomputePower()
    {
        foreach (var building in _buildings)
        {
            building.IsPowered = !building.IsDestroyed && !building.Definition.RequiresPower;
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var building in _buildings.Where(building => building.Definition.RequiresPower && !building.IsPowered && !building.IsDestroyed))
            {
                var targetRadius = ToWorldRadius(building.Definition.FootprintRadius);
                var powered = building.Definition.Id == ContentIds.Buildings.Pylon
                    ? IsPylonSupported(building.FactionId, building.Position, targetRadius)
                    : IsPositionPowered(building.FactionId, building.Position, targetRadius);

                if (!powered)
                {
                    continue;
                }

                building.IsPowered = true;
                changed = true;
            }
        }

        RecomputeEnergyWalls();
    }

    private void RecomputeEnergyWalls()
    {
        _energyWalls.Clear();

        var anchors = _buildings
            .Where(building => building.Definition.WallAnchor && building.IsPowered && !building.IsDestroyed && building.Definition.WallLinkRange > 0)
            .OrderBy(building => building.EntityId)
            .ToArray();

        for (var leftIndex = 0; leftIndex < anchors.Length; leftIndex++)
        {
            var left = anchors[leftIndex];
            var linkRange = ToWorldRadius(left.Definition.WallLinkRange);

            for (var rightIndex = leftIndex + 1; rightIndex < anchors.Length; rightIndex++)
            {
                var right = anchors[rightIndex];
                if (left.FactionId != right.FactionId)
                {
                    continue;
                }

                var maxLinkRange = MathF.Min(linkRange, ToWorldRadius(right.Definition.WallLinkRange));
                if (left.Position.DistanceTo(right.Position) > maxLinkRange)
                {
                    continue;
                }

                _energyWalls.Add(new EnergyWallSegment(left.EntityId, right.EntityId, left.Position, right.Position));
            }
        }
    }

    private static bool LinesIntersect(SimVector2 a, SimVector2 b, SimVector2 c, SimVector2 d)
    {
        var denominator = ((d.Y - c.Y) * (b.X - a.X)) - ((d.X - c.X) * (b.Y - a.Y));
        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var ua = (((d.X - c.X) * (a.Y - c.Y)) - ((d.Y - c.Y) * (a.X - c.X))) / denominator;
        var ub = (((b.X - a.X) * (a.Y - c.Y)) - ((b.Y - a.Y) * (a.X - c.X))) / denominator;
        return ua is >= 0.0f and <= 1.0f && ub is >= 0.0f and <= 1.0f;
    }
}
