using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    public const float ContentUnitScale = 24.0f;
    private const float ResourceWellCoreRadius = 28.0f;
    private const float CombatMovementScale = 95.0f;
    private const float EnemyIncomeMultiplier = 0.75f;
    internal const float EnemyTrainTimeMultiplier = 1.25f;
    private const float FogCellSize = 64.0f;

    public static SimVector2 EnemyHubPosition => EnemyAiMarkers.FirstLanding.HubPosition;
    public static SimVector2 EnemyPowerPlantPosition => EnemyAiMarkers.FirstLanding.PowerPlantPosition;
    public static SimVector2 EnemyBarracksPosition => EnemyAiMarkers.FirstLanding.BarracksPosition;
    public static SimVector2 EnemyExtractorPosition => EnemyAiMarkers.FirstLanding.ExtractorPosition;
    public static SimVector2 EnemyDefenseTowerPosition => EnemyAiMarkers.FirstLanding.DefenseTowerPosition;

    private readonly ContentCatalog _catalog;
    private readonly List<BuildingState> _buildings = [];
    private readonly List<UnitState> _units = [];
    private readonly List<ProductionOrderState> _productionOrders = [];
    private readonly List<ResourceWellState> _resourceWells = [];
    private readonly List<EnergyWallSegment> _energyWalls = [];
    private readonly FogOfWarState _playerFog = new(-760, 940, -420, 420, FogCellSize);
    private readonly FogOfWarState _enemyFog = new(-760, 940, -420, 420, FogCellSize);
    private readonly HashSet<int> _hubTankReveals = [];
    private readonly EnemyAiSystem _enemyAi;
    private readonly MissionObjectiveSystem _missionObjectives = new();
    private float _elapsedSeconds;
    private int _nextEntityId = 1;

    public RtsSimulation(
        ContentCatalog catalog,
        int startingMaterials,
        IEnumerable<(string WellId, SimVector2 Position)> resourceWellPlacements,
        int enemyStartingMaterials = 0,
        EnemyAiMarkers? enemyAiMarkers = null,
        EnemyAiProfileDefinition? enemyAiProfile = null)
    {
        _catalog = catalog;
        Materials = startingMaterials;
        EnemyMaterials = enemyStartingMaterials;
        _enemyAi = new EnemyAiSystem(enemyAiMarkers ?? EnemyAiMarkers.FirstLanding, enemyAiProfile);

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
    public FogOfWarState PlayerFog => _playerFog;
    public MissionState MissionState { get; private set; } = new(MissionStatus.Active, "Objective: establish the outpost.");
    public float ElapsedSeconds => _elapsedSeconds;
    public EnemyAiProfileDefinition EnemyAiProfile => _enemyAi.Profile;
    public bool EnemyProductionOnline => HasPoweredBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.Barracks) &&
        HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub);

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
        RecomputeFog();
        UpdateMissionState();
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
            return new PlacementValidation(
                false,
                $"Need {definition.Cost:0} materials.",
                null,
                "sim.need_materials",
                SimulationMessage.Args(("amount", definition.Cost)));
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
                return new PlacementValidation(
                    false,
                    $"Blocked by {building.Definition.DisplayName}.",
                    null,
                    "sim.placement.blocked_by_building",
                    SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
            }
        }

        var targetWell = FindCompatibleResourceWell(definition, position);
        if (definition.ProvidesResourceExtraction && targetWell is null)
        {
            return new PlacementValidation(false, "Extractor must be placed on an open resource well.", null, "sim.placement.extractor_requires_well");
        }

        if (!IsAdjacentRequirementMet(factionId, definition, position))
        {
            var required = _catalog.GetBuilding(definition.RequiresAdjacentBuildingId!);
            return new PlacementValidation(
                false,
                $"{definition.DisplayName} must be built adjacent to {required.DisplayName}.",
                null,
                "sim.placement.requires_adjacent_building",
                SimulationMessage.Args(
                    ("buildingId", definition.Id),
                    ("building", definition.DisplayName),
                    ("requiredBuildingId", required.Id),
                    ("requiredBuilding", required.DisplayName)));
        }

        if (definition.RequiresPower && !WouldBePowered(factionId, definition, position))
        {
            return new PlacementValidation(false, "Must be placed inside powered support.", null, "sim.placement.requires_powered_support");
        }

        return new PlacementValidation(true, "Placement legal.", targetWell?.Definition.Id, "sim.placement.legal");
    }

    public PlacementResult TryPlaceBuilding(string buildingId, SimVector2 position)
    {
        return TryPlaceBuildingForFaction(ContentIds.Factions.PlayerExpedition, buildingId, position);
    }

    internal PlacementResult TryPlaceBuildingForFaction(string factionId, string buildingId, SimVector2 position)
    {
        var availableMaterials = GetMaterialsForFaction(factionId);
        var validation = ValidatePlacementForFaction(factionId, buildingId, position, availableMaterials);
        if (!validation.IsLegal)
        {
            return new PlacementResult(false, validation.Reason, null, validation.MessageKey, validation.MessageArgs);
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
        RecomputeFog();
        UpdateMissionState();
        return new PlacementResult(
            true,
            $"Placed {definition.DisplayName}.",
            building,
            "sim.placement.placed",
            SimulationMessage.Args(("buildingId", definition.Id), ("building", definition.DisplayName)));
    }

    public void Tick(float deltaSeconds)
    {
        _elapsedSeconds += deltaSeconds;
        RecomputePower();
        _enemyAi.Tick(this, deltaSeconds);
        TickProduction(deltaSeconds);
        TickEnemyPressure(deltaSeconds);
        TickBuildingAttacks(deltaSeconds);

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

        RevealTanksForDestroyedHubs();
        RecomputeFog();
        UpdateMissionState();
    }

    public UnitState AddUnit(string unitId, string factionId, SimVector2 position)
    {
        var unit = new UnitState(_nextEntityId++, _catalog.GetUnit(unitId), factionId, position);
        _units.Add(unit);
        RecomputeFog();
        UpdateMissionState();
        return unit;
    }

    public void CommandUnitMove(int unitEntityId, SimVector2 position)
    {
        var unit = FindLiveUnit(unitEntityId);
        if (unit is null)
        {
            return;
        }

        unit.TargetUnitEntityId = null;
        unit.TargetBuildingEntityId = null;
        SetUnitPathTo(unit, position);
    }

    public void CommandUnitAttackUnit(int unitEntityId, int targetUnitEntityId)
    {
        var unit = FindLiveUnit(unitEntityId);
        var target = FindLiveUnit(targetUnitEntityId);
        if (unit is null || target is null || unit.FactionId == target.FactionId)
        {
            return;
        }

        unit.ClearPath();
        unit.TargetUnitEntityId = target.EntityId;
        unit.TargetBuildingEntityId = null;
    }

    public void CommandUnitAttackBuilding(int unitEntityId, int targetBuildingEntityId)
    {
        var unit = FindLiveUnit(unitEntityId);
        var target = FindLiveBuilding(targetBuildingEntityId);
        if (unit is null || target is null || unit.FactionId == target.FactionId)
        {
            return;
        }

        unit.ClearPath();
        unit.TargetUnitEntityId = null;
        unit.TargetBuildingEntityId = target.EntityId;
    }

    public UpgradeResult TryUpgradeBuilding(int buildingEntityId, string upgradeBuildingId)
    {
        return TryUpgradeBuildingForFaction(ContentIds.Factions.PlayerExpedition, buildingEntityId, upgradeBuildingId);
    }

    public UpgradeResult ValidateBuildingUpgrade(int buildingEntityId, string upgradeBuildingId)
    {
        return ValidateBuildingUpgradeForFaction(ContentIds.Factions.PlayerExpedition, buildingEntityId, upgradeBuildingId);
    }

    internal UpgradeResult TryUpgradeBuildingForFaction(string factionId, int buildingEntityId, string upgradeBuildingId)
    {
        var validation = ValidateBuildingUpgradeForFaction(factionId, buildingEntityId, upgradeBuildingId);
        if (!validation.Success || validation.Building is null)
        {
            return validation;
        }

        var upgrade = _catalog.GetBuilding(upgradeBuildingId);
        SpendMaterialsForFaction(factionId, upgrade.Cost);
        validation.Building.UpgradeTo(upgrade);
        RecomputePower();
        RecomputeFog();
        UpdateMissionState();
        return new UpgradeResult(
            true,
            $"Upgraded to {upgrade.DisplayName}.",
            validation.Building,
            "sim.upgrade.upgraded",
            SimulationMessage.Args(("buildingId", upgrade.Id), ("building", upgrade.DisplayName)));
    }

    private UpgradeResult ValidateBuildingUpgradeForFaction(string factionId, int buildingEntityId, string upgradeBuildingId)
    {
        var building = FindLiveBuilding(buildingEntityId);
        if (building is null || building.FactionId != factionId)
        {
            return new UpgradeResult(false, "Select a live friendly building.", null, "sim.upgrade.select_live_friendly");
        }

        var upgrade = _catalog.GetBuilding(upgradeBuildingId);
        if (upgrade.UpgradeFromBuildingId != building.Definition.Id)
        {
            return new UpgradeResult(
                false,
                $"{building.Definition.DisplayName} cannot upgrade into {upgrade.DisplayName}.",
                null,
                "sim.upgrade.invalid_target",
                SimulationMessage.Args(
                    ("buildingId", building.Definition.Id),
                    ("building", building.Definition.DisplayName),
                    ("upgradeBuildingId", upgrade.Id),
                    ("upgradeBuilding", upgrade.DisplayName)));
        }

        if (upgrade.RequiresPower && !building.IsPowered)
        {
            return new UpgradeResult(
                false,
                $"{building.Definition.DisplayName} is unpowered.",
                null,
                "sim.upgrade.unpowered",
                SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
        }

        if (GetMaterialsForFaction(factionId) < upgrade.Cost)
        {
            return new UpgradeResult(false, $"Need {upgrade.Cost:0} materials.", null, "sim.need_materials", SimulationMessage.Args(("amount", upgrade.Cost)));
        }

        return new UpgradeResult(
            true,
            $"Can upgrade to {upgrade.DisplayName}.",
            building,
            "sim.upgrade.can_upgrade",
            SimulationMessage.Args(("buildingId", upgrade.Id), ("building", upgrade.DisplayName)));
    }

    public bool IsLineBlockedByEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.Any(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private void TickEnemyPressure(float deltaSeconds)
    {
        foreach (var unit in _units.Where(unit => !unit.IsDestroyed))
        {
            unit.AttackCooldownRemaining = MathF.Max(0.0f, unit.AttackCooldownRemaining - deltaSeconds);

            if (unit.FactionId == ContentIds.Factions.PrivateMilitary)
            {
                if (_elapsedSeconds >= _enemyAi.Profile.FirstAttackDelaySeconds &&
                    CountEnemyCombatUnits() >= _enemyAi.Profile.AttackGroupSize)
                {
                    TickEnemyUnit(unit, deltaSeconds * _enemyAi.Profile.PressureSlowdownMultiplier);
                }
            }
            else
            {
                TickPlayerUnit(unit, deltaSeconds);
            }
        }
    }

    private int CountEnemyCombatUnits()
    {
        return _units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            unit.Definition.CanAttack);
    }

    private void TickBuildingAttacks(float deltaSeconds)
    {
        foreach (var building in _buildings.Where(building => !building.IsDestroyed))
        {
            building.AttackCooldownRemaining = MathF.Max(0.0f, building.AttackCooldownRemaining - deltaSeconds);
            if (!building.IsPowered ||
                building.Definition.AttackDamage <= 0.0f ||
                building.Definition.AttackRange <= 0.0f ||
                building.AttackCooldownRemaining > 0.0f)
            {
                continue;
            }

            var targetUnit = FindNearestHostileUnitForBuilding(building);
            if (targetUnit is not null)
            {
                CombatResolver.ResolveBuildingAttack(building, targetUnit, _units, _buildings);
                RecomputePower();
                continue;
            }

            if (!building.Definition.TargetFilters.Contains("building", StringComparer.Ordinal))
            {
                continue;
            }

            var targetBuilding = FindNearestHostileBuildingForBuilding(building);
            if (targetBuilding is null)
            {
                continue;
            }

            CombatResolver.ResolveBuildingAttack(building, targetBuilding, _units, _buildings);
            RecomputePower();
        }
    }

    private void TickPlayerUnit(UnitState unit, float deltaSeconds)
    {
        var targetUnit = unit.TargetUnitEntityId is null ? null : FindLiveUnit(unit.TargetUnitEntityId.Value);
        if (targetUnit is not null && targetUnit.FactionId != unit.FactionId)
        {
            TickUnitAttackTarget(unit, targetUnit, deltaSeconds);
            return;
        }

        var targetBuilding = unit.TargetBuildingEntityId is null ? null : FindLiveBuilding(unit.TargetBuildingEntityId.Value);
        if (targetBuilding is not null && targetBuilding.FactionId != unit.FactionId)
        {
            TickUnitAttackTarget(unit, targetBuilding, deltaSeconds);
            return;
        }

        var nearbyEnemy = FindNearestEnemyUnitInRange(unit);
        if (nearbyEnemy is not null)
        {
            TryUnitAttackUnit(unit, nearbyEnemy);
            return;
        }

        if (unit.MoveTarget is not null)
        {
            MoveUnitToward(unit, unit.MoveTarget.Value, deltaSeconds);
        }
    }

    private void TickEnemyUnit(UnitState unit, float deltaSeconds)
    {
        var targetUnit = FindNearestEnemyUnitInRange(unit);
        if (targetUnit is not null)
        {
            unit.TargetUnitEntityId = targetUnit.EntityId;
            unit.TargetBuildingEntityId = null;
            TryUnitAttackUnit(unit, targetUnit);
            return;
        }

        var target = GetEnemyTargetBuilding(unit);
        unit.TargetUnitEntityId = null;
        unit.TargetBuildingEntityId = target?.EntityId;
        if (target is null)
        {
            return;
        }

        TickUnitAttackTarget(unit, target, deltaSeconds);
    }

    private void TickUnitAttackTarget(UnitState attacker, UnitState target, float deltaSeconds)
    {
        var distanceToTarget = attacker.Position.DistanceTo(target.Position);
        if (attacker.Definition.CanAttack && distanceToTarget <= ToWorldRadius(attacker.Definition.AttackRange))
        {
            TryUnitAttackUnit(attacker, target);
            return;
        }

        MoveUnitToward(attacker, target.Position, deltaSeconds);
    }

    private void TickUnitAttackTarget(UnitState attacker, BuildingState target, float deltaSeconds)
    {
        var distanceToTarget = attacker.Position.DistanceTo(target.Position);
        var attackRange = ToWorldRadius(attacker.Definition.AttackRange) + target.FootprintWorldRadius;
        if (attacker.Definition.CanAttack && distanceToTarget <= attackRange)
        {
            TryUnitAttackBuilding(attacker, target);
            return;
        }

        MoveUnitToward(attacker, target.Position, deltaSeconds);
    }

    private void MoveUnitToward(UnitState unit, SimVector2 target, float deltaSeconds)
    {
        if (NeedsNewPath(unit, target))
        {
            SetUnitPathTo(unit, target);
        }

        if (unit.IsPathBlocked)
        {
            return;
        }

        var waypoint = unit.CurrentWaypoint;
        if (waypoint is null)
        {
            unit.ClearPath();
            return;
        }

        var direction = waypoint.Value - unit.Position;
        var distance = direction.Length();
        if (distance <= 4.0f)
        {
            unit.Position = waypoint.Value;
            unit.AdvanceWaypoint();
            if (unit.CurrentWaypoint is null)
            {
                unit.ClearPath();
            }

            return;
        }

        var stepDistance = unit.Definition.MovementSpeed * CombatMovementScale * deltaSeconds;
        var start = unit.Position;
        unit.Position += direction.Normalized() * MathF.Min(stepDistance, distance);
        CombatResolver.TryCrushInfantry(unit, start, unit.Position, _units);
    }

    private void SetUnitPathTo(UnitState unit, SimVector2 target)
    {
        var path = PathfindingSystem.FindPath(
            unit.Position,
            target,
            _buildings,
            GetBlockingEnergyWallsForFaction(unit.FactionId));

        if (path.Success)
        {
            unit.SetPath(path.Destination, path.Waypoints);
            unit.IsBlockedByEnergyWall = false;
            return;
        }

        unit.SetPathBlocked(target, path.Message);
        unit.IsBlockedByEnergyWall = FindBlockingEnergyWallForFaction(unit.FactionId, unit.Position, target) is not null;
    }

    private static bool NeedsNewPath(UnitState unit, SimVector2 target)
    {
        if (unit.MoveTarget is null)
        {
            return true;
        }

        if (unit.IsPathBlocked)
        {
            return true;
        }

        return unit.PathWaypoints.Count == 0 || unit.MoveTarget.Value.DistanceTo(target) > 32.0f;
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

        var blockingWall = FindBlockingEnergyWallForFaction(unit.FactionId, unit.Position, hub.Position);
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

        CombatResolver.ResolveUnitAttack(unit, target, _units, _buildings);
        RecomputePower();
    }

    private void TryUnitAttackUnit(UnitState attacker, UnitState target)
    {
        if (attacker.AttackCooldownRemaining > 0.0f ||
            target.IsDestroyed ||
            attacker.Definition.AttackDamage <= 0.0f)
        {
            return;
        }

        CombatResolver.ResolveUnitAttack(attacker, target, _units, _buildings);
        RecomputePower();
    }

    private UnitState? FindNearestEnemyUnitInRange(UnitState unit)
    {
        if (!unit.Definition.CanAttack || unit.Definition.AttackRange <= 0.0f)
        {
            return null;
        }

        var range = ToWorldRadius(unit.Definition.AttackRange);
        return _units
            .Where(candidate => candidate.FactionId != unit.FactionId && !candidate.IsDestroyed)
            .Where(candidate => unit.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(unit.Position) <= range)
            .OrderBy(candidate => candidate.Position.DistanceTo(unit.Position))
            .FirstOrDefault();
    }

    private UnitState? FindNearestHostileUnitForBuilding(BuildingState building)
    {
        var range = ToWorldRadius(building.Definition.AttackRange) + building.FootprintWorldRadius;
        return _units
            .Where(candidate => candidate.FactionId != building.FactionId && !candidate.IsDestroyed)
            .Where(candidate => building.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(building.Position) <= range)
            .OrderBy(candidate => candidate.Position.DistanceTo(building.Position))
            .FirstOrDefault();
    }

    private BuildingState? FindNearestHostileBuildingForBuilding(BuildingState building)
    {
        var range = ToWorldRadius(building.Definition.AttackRange) + building.FootprintWorldRadius;
        return _buildings
            .Where(candidate => candidate.FactionId != building.FactionId && !candidate.IsDestroyed)
            .Where(candidate => building.FactionId != ContentIds.Factions.PlayerExpedition ||
                IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, candidate.Position))
            .Where(candidate => candidate.Position.DistanceTo(building.Position) <= range + candidate.FootprintWorldRadius)
            .OrderBy(candidate => candidate.Position.DistanceTo(building.Position))
            .FirstOrDefault();
    }

    private EnergyWallSegment? FindBlockingEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.FirstOrDefault(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private EnergyWallSegment? FindBlockingEnergyWallForFaction(string factionId, SimVector2 start, SimVector2 end)
    {
        return GetBlockingEnergyWallsForFaction(factionId)
            .FirstOrDefault(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private IReadOnlyList<EnergyWallSegment> GetBlockingEnergyWallsForFaction(string factionId)
    {
        return _energyWalls
            .Where(wall =>
            {
                var startAnchor = FindLiveBuilding(wall.StartAnchorEntityId);
                return startAnchor is not null && startAnchor.FactionId != factionId;
            })
            .ToArray();
    }

    private BuildingState? FindLiveBuilding(int entityId)
    {
        return _buildings.FirstOrDefault(building => building.EntityId == entityId && !building.IsDestroyed);
    }

    private UnitState? FindLiveUnit(int entityId)
    {
        return _units.FirstOrDefault(unit => unit.EntityId == entityId && !unit.IsDestroyed);
    }

    private bool HasPoweredBuilding(string factionId, string buildingId)
    {
        return _buildings.Any(building =>
            building.FactionId == factionId &&
            building.Definition.Id == buildingId &&
            building.IsPowered &&
            !building.IsDestroyed);
    }

    internal bool HasLiveBuilding(string factionId, string buildingId)
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

    private bool IsAdjacentRequirementMet(string factionId, BuildingDefinition definition, SimVector2 position)
    {
        if (definition.RequiresAdjacentBuildingId is null)
        {
            return true;
        }

        return _buildings.Any(building =>
            building.FactionId == factionId &&
            building.Definition.Id == definition.RequiresAdjacentBuildingId &&
            !building.IsDestroyed &&
            building.Position.DistanceTo(position) <= building.OccupancyRadius + ToWorldRadius(definition.FootprintRadius + definition.PlacementBuffer + 1.5f));
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
