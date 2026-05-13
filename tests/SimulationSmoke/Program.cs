using Stratezone.Simulation;
using static SmokeTestSupport;

var context = SmokeTestSupport.LoadContext();
var catalog = context.Catalog;
var mission = context.FirstLandingMission;
var ridgeMission = context.WellsAtTheRidgeMission;
var startingMaterials = mission.PlayerStartingResources[ContentIds.Resources.Materials];

ContentSmoke.Run(context);
LocalizationSmoke.Run();

var simulation = new RtsSimulation(
    catalog,
    startingMaterials,
    [
        ("well_first_landing_start", new SimVector2(-350, 170)),
        ("well_first_landing_central", new SimVector2(220, 30))
    ]);

simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));

Assert(!simulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, new SimVector2(-300, -140)).IsLegal, "overlapping building placement is rejected");
Assert(!simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(120, 160)).IsLegal, "powered buildings cannot be placed outside powered support");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(120, 160)).MessageKey == "sim.placement.requires_powered_support", "placement validation returns a stable message key");

var powerPlant = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-170, 40));
Assert(powerPlant.Success, powerPlant.Message);
Assert(Math.Abs(simulation.Materials - (startingMaterials - catalog.GetBuilding(ContentIds.Buildings.PowerPlant).Cost)) < 0.01f, "placing buildings spends materials");

var lowBudgetSimulation = new RtsSimulation(catalog, 100, []);
lowBudgetSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
Assert(!lowBudgetSimulation.ValidatePlacement(ContentIds.Buildings.PowerPlant, new SimVector2(-170, 40)).IsLegal, "spending cannot go below zero");
Assert(!simulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(520, 320)).IsLegal, "pylon cannot be placed globally from a power plant");
Assert(catalog.GetBuilding(ContentIds.Buildings.Pylon).FootprintRadius < catalog.GetBuilding(ContentIds.Buildings.DefenseTower).FootprintRadius, "pylon footprint is smaller than a tower footprint");
Assert(Math.Abs(catalog.GetBuilding(ContentIds.Buildings.Pylon).PylonLinkRange - 30.0f) < 0.01f, "Pylon links cover long enough distances to avoid wasteful short-hop power grids");

var pylon = simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-220, 160));
Assert(pylon.Success, pylon.Message);
Assert(pylon.Building?.IsPowered == true, "pylon is powered by the plant");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(-430, 170)).IsLegal, "powered pylons can daisy chain to new pylons");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(-350, 80)).IsLegal, "pylon extends powered placement");

var extractor = simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, new SimVector2(-350, 170));
Assert(extractor.Success, extractor.Message);
Assert(extractor.Building?.IsPowered == true, "extractor is powered by pylon support");

var overlapSimulation = new RtsSimulation(
    catalog,
    startingMaterials,
    [("well_first_landing_start", new SimVector2(0, 0))]);
overlapSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
overlapSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-160, 0));
Assert(overlapSimulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, new SimVector2(70, 0)).IsLegal, "extractor placement accepts footprint overlap with resource well");
var barracksOnWell = overlapSimulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(70, 0));
Assert(!barracksOnWell.IsLegal, "non-extractor buildings cannot be placed on open resource wells");
Assert(barracksOnWell.MessageKey == "sim.placement.resource_well_reserved", "resource well placement block returns a stable message key");

var reclaimedWellSimulation = new RtsSimulation(
    catalog,
    2000,
    [("well_first_landing_central", new SimVector2(0, 0))]);
reclaimedWellSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
reclaimedWellSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-160, 0));
var claimedExtractor = reclaimedWellSimulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, new SimVector2(0, 0));
Assert(claimedExtractor.Success, claimedExtractor.Message);
Assert(!reclaimedWellSimulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, new SimVector2(0, 0)).IsLegal, "live Extractor keeps the well claimed");
claimedExtractor.Building!.ApplyDamage(9999, "explosive");
Assert(reclaimedWellSimulation.ValidatePlacement(ContentIds.Buildings.ExtractorRefinery, new SimVector2(0, 0)).IsLegal, "destroyed Extractor releases the well for capture");

var materialsBeforeIncome = simulation.Materials;
simulation.Tick(1.0f);
Assert(simulation.Materials > materialsBeforeIncome, "powered extractor generates materials");

var startWell = simulation.ResourceWells.First(well => well.Definition.Id == "well_first_landing_start");
simulation.Tick(1000.0f);
Assert(startWell.IsDepleted, "well can deplete");
var materialsAfterDepletion = simulation.Materials;
simulation.Tick(10.0f);
Assert(Math.Abs(simulation.Materials - materialsAfterDepletion) < 0.01f, "depleted well stops generating income");

var wallSimulation = new RtsSimulation(catalog, startingMaterials, []);
wallSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
var wallPower = wallSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
Assert(wallPower.Success, wallPower.Message);
var firstTower = wallSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, -80));
Assert(firstTower.Success, firstTower.Message);
Assert(wallSimulation.EnergyWalls.Count == 0, "one wall anchor does not create a wall segment");
var secondTower = wallSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, 80));
Assert(secondTower.Success, secondTower.Message);
Assert(wallSimulation.EnergyWalls.Count == 1, "two nearby powered wall anchors create a wall segment");
Assert(wallSimulation.IsLineBlockedByEnergyWall(new SimVector2(0, 0), new SimVector2(260, 0)), "energy wall blocks a crossing line");
var wallBlockedPlacement = wallSimulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(150, 0));
Assert(!wallBlockedPlacement.IsLegal, "building placement cannot cut through an active energy wall gap");
var blockedEnemy = wallSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(520, 70));
TickFor(wallSimulation, 5.0f);
Assert(blockedEnemy.IsBlockedByEnergyWall, "enemy pressure recognizes a blocking energy wall");
Assert(blockedEnemy.TargetBuildingEntityId == firstTower.Building?.EntityId || blockedEnemy.TargetBuildingEntityId == secondTower.Building?.EntityId, "blocked enemy targets a wall anchor");
Assert(wallSimulation.EnemyAiTelemetry.WallBlocksEncountered == 1, "enemy telemetry records a wall block without spamming repeat counts");
TickFor(wallSimulation, 70.0f);
Assert(wallSimulation.EnergyWalls.Count == 0, "destroying a wall anchor drops the energy wall segment");

var openPressureSimulation = new RtsSimulation(catalog, startingMaterials, []);
var openHub = openPressureSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
openPressureSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(520, 70));
TickFor(openPressureSimulation, 20.0f);
Assert(openHub.Health < openHub.Definition.Health, "enemy pressure damages the Colony Hub when no wall blocks the route");

var pathSimulation = new RtsSimulation(catalog, startingMaterials, []);
pathSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
var pathUnit = pathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-260, 0));
pathSimulation.CommandUnitMove(pathUnit.EntityId, new SimVector2(260, 0));
Assert(pathUnit.PathWaypoints.Any(point => Math.Abs(point.Y) > 20.0f), "unit movement path routes around building blockers");
TickFor(pathSimulation, 7.0f);
Assert(pathUnit.Position.DistanceTo(new SimVector2(260, 0)) < 12.0f, "unit reaches routed move destination");

var fallbackPathSimulation = new RtsSimulation(catalog, startingMaterials, []);
fallbackPathSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
var fallbackUnit = fallbackPathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-260, 0));
fallbackPathSimulation.CommandUnitMove(fallbackUnit.EntityId, new SimVector2(0, 0));
Assert(!fallbackUnit.IsPathBlocked, "pathfinding finds a fallback when the exact destination is inside a building footprint");
Assert(fallbackUnit.MoveTarget is not null && fallbackUnit.MoveTarget.Value.DistanceTo(new SimVector2(0, 0)) > 40.0f, "blocked destination fallback redirects to a nearby reachable point");
TickFor(fallbackPathSimulation, 7.0f);
Assert(fallbackUnit.Position.DistanceTo(fallbackUnit.MoveTarget ?? fallbackUnit.Position) < 12.0f, "unit reaches fallback destination near blocked target");

var wallPathSimulation = new RtsSimulation(catalog, startingMaterials, []);
wallPathSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(wallPathSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Success, "wall path test places power");
Assert(wallPathSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, -80)).Success, "wall path test places first tower");
Assert(wallPathSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, 80)).Success, "wall path test places second tower");
var hostileWallRunner = wallPathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(260, 0));
wallPathSimulation.CommandUnitMove(hostileWallRunner.EntityId, new SimVector2(0, 0));
Assert(!hostileWallRunner.IsPathBlocked, "hostile unit can route around a finite energy wall segment");
Assert(hostileWallRunner.PathWaypoints.Any(point => Math.Abs(point.Y) > 90.0f), "hostile unit path detours around enemy energy wall segment");
TickFor(wallPathSimulation, 1.5f);
Assert(!(hostileWallRunner.Position.X < 130.0f && Math.Abs(hostileWallRunner.Position.Y) < 90.0f), "hostile unit does not walk through the active energy wall gap");
var friendlyWallRunner = wallPathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(260, 0));
wallPathSimulation.CommandUnitMove(friendlyWallRunner.EntityId, new SimVector2(0, 0));
Assert(!friendlyWallRunner.IsPathBlocked, "friendly energy wall does not block allied movement");
Assert(!friendlyWallRunner.PathWaypoints.Any(point => Math.Abs(point.Y) > 90.0f), "friendly unit path does not detour around allied energy wall segment");

var pursuitPathSimulation = new RtsSimulation(catalog, startingMaterials, []);
pursuitPathSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
var pursuingRifleman = pursuitPathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-260, 0));
var pursuedRifleman = pursuitPathSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(260, 0));
pursuitPathSimulation.CommandUnitAttackUnit(pursuingRifleman.EntityId, pursuedRifleman.EntityId);
TickFor(pursuitPathSimulation, 7.0f);
Assert(pursuedRifleman.Health < pursuedRifleman.Definition.Health, "attack pursuit uses routed movement around blockers");

var enemyBaseSimulation = new RtsSimulation(catalog, startingMaterials, [], 800);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, EnemyAiMarkers.FirstLanding.PowerPlantPosition, ContentIds.Factions.PrivateMilitary);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, EnemyAiMarkers.FirstLanding.BarracksPosition, ContentIds.Factions.PrivateMilitary);
TickFor(enemyBaseSimulation, 0.1f);
Assert(enemyBaseSimulation.ProductionOrders.Count == 1, "enemy base queues production from powered Barracks");
Assert(enemyBaseSimulation.EnemyMaterials <= 700, "enemy production and construction spend resources when queued");
TickFor(enemyBaseSimulation, 13.0f);
Assert(enemyBaseSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Rifleman), "enemy production can choose Riflemen from its base when resources allow");

var lowResourceEnemySimulation = new RtsSimulation(catalog, startingMaterials, [], 75);
lowResourceEnemySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
lowResourceEnemySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
lowResourceEnemySimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, EnemyAiMarkers.FirstLanding.PowerPlantPosition, ContentIds.Factions.PrivateMilitary);
lowResourceEnemySimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, EnemyAiMarkers.FirstLanding.BarracksPosition, ContentIds.Factions.PrivateMilitary);
TickFor(lowResourceEnemySimulation, 0.1f);
Assert(lowResourceEnemySimulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary && order.UnitId == ContentIds.Units.Cadet), "enemy production can choose Cadets when resources are too low for Riflemen");

var enemyGuardianRetrofitSimulation = new RtsSimulation(
    catalog,
    startingMaterials,
    [],
    2000,
    null,
    null,
    [ContentIds.Units.Grunt, ContentIds.Units.Cadet, ContentIds.Units.Rifleman, ContentIds.Units.Guardian]);
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, EnemyAiMarkers.FirstLanding.PowerPlantPosition, ContentIds.Factions.PrivateMilitary);
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.Pylon, EnemyAiMarkers.FirstLanding.BasePylonPosition, ContentIds.Factions.PrivateMilitary);
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.Pylon, EnemyAiMarkers.FirstLanding.ForwardPylonPosition, ContentIds.Factions.PrivateMilitary);
var enemyRetrofitBarracks = enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, EnemyAiMarkers.FirstLanding.BarracksPosition, ContentIds.Factions.PrivateMilitary);
enemyGuardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.DefenseTower, EnemyAiMarkers.FirstLanding.DefenseTowerPosition, ContentIds.Factions.PrivateMilitary);
TickFor(enemyGuardianRetrofitSimulation, 0.1f);
Assert(enemyGuardianRetrofitSimulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary && order.UnitId == ContentIds.Units.Grunt), "enemy AI trains Grunts first when Guardian Retrofit is mission-enabled but understaffed");
var enemyGruntTrainSeconds = catalog.GetUnit(ContentIds.Units.Grunt).TrainTimeSeconds * enemyGuardianRetrofitSimulation.EnemyAiProfile.TrainTimeMultiplier;
TickFor(enemyGuardianRetrofitSimulation, (enemyGruntTrainSeconds * 2.0f) + 0.6f);
Assert(enemyGuardianRetrofitSimulation.Units.Count(unit =>
    unit.FactionId == ContentIds.Factions.PrivateMilitary &&
    unit.Definition.Id == ContentIds.Units.Grunt &&
    !unit.IsDestroyed) >= 2, "enemy AI produces the two Grunts required for Guardian Retrofit");
Assert(enemyRetrofitBarracks.IsBarracksUpgradeInProgress, "enemy AI starts Guardian Retrofit after staffing requirements are met");
TickFor(enemyGuardianRetrofitSimulation, catalog.GetBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit).DurationSeconds + 0.2f);
Assert(enemyRetrofitBarracks.HasBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit), "enemy AI completes Guardian Retrofit instead of assuming the unlock");
TickFor(enemyGuardianRetrofitSimulation, 0.2f);
Assert(enemyGuardianRetrofitSimulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary && order.UnitId == ContentIds.Units.Guardian), "enemy production can choose Guardian after the AI-completed Barracks retrofit");

var playerProductionSimulation = new RtsSimulation(catalog, 2000, []);
playerProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(playerProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-220, 0)).Success, "player production test places power");
var playerBarracks = playerProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, new SimVector2(-20, 0));
Assert(playerBarracks.Success, playerBarracks.Message);
playerProductionSimulation.DrainEvents();
var materialsBeforeTraining = playerProductionSimulation.Materials;
var cadetQueue = playerProductionSimulation.TryQueueUnit(ContentIds.Units.Cadet, playerBarracks.Building!.EntityId);
Assert(cadetQueue.Success, cadetQueue.Message);
Assert(cadetQueue.MessageKey == "sim.production.queued", "production result returns a stable message key");
Assert(playerProductionSimulation.Materials < materialsBeforeTraining, "player training spends materials immediately");
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building.EntityId).Success, "player can queue a Rifleman behind an active Barracks order");
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building.EntityId).Success, "player can queue multiple Riflemen behind an active Barracks order");
Assert(playerProductionSimulation.ProductionOrders.Count(order => order.ProducerBuildingEntityId == playerBarracks.Building.EntityId) == 3, "Barracks keeps a small serial training queue");
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building.EntityId).Success, "Barracks queue accepts a fourth order");
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building.EntityId).Success, "Barracks queue accepts a fifth order");
Assert(!playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building.EntityId).Success, "Barracks queue blocks a sixth order");
TickFor(playerProductionSimulation, 3.2f);
Assert(playerProductionSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Cadet), "player production spawns trained Cadets from the Colony Hub");
Assert(!playerProductionSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Rifleman), "queued Riflemen do not train in parallel from one Barracks");
TickFor(playerProductionSimulation, 4.2f);
Assert(playerProductionSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Rifleman) == 1, "queued Riflemen train one after another from one Barracks");
Assert(playerProductionSimulation.DrainEvents().Any(item => item.MessageKey == "sim.event.training_complete"), "player production emits a training-complete event");
Assert(!playerProductionSimulation.TryQueueUnit(ContentIds.Units.Guardian, playerBarracks.Building.EntityId).Success, "Guardian training requires completed Barracks retrofit");
playerBarracks.Building.CompleteBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit);
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Guardian, playerBarracks.Building.EntityId).Success, "completed Barracks retrofit unlocks Guardian training");

var levelOneProductionSimulation = new RtsSimulation(catalog, 3000, [], 0, null, null, mission.AvailableUnitIds);
levelOneProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(levelOneProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-220, 0)).Success, "Level 1 production test places power");
var levelOneBarracks = levelOneProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, new SimVector2(-20, 0));
Assert(levelOneBarracks.Success, levelOneBarracks.Message);
levelOneBarracks.Building!.CompleteBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit);
Assert(!levelOneProductionSimulation.TryQueueUnit(ContentIds.Units.Guardian, levelOneBarracks.Building.EntityId).Success, "Level 1 mission rules block training extra Guardians even if a Barracks has the retrofit");
Assert(!levelOneProductionSimulation.TryQueueUnit(ContentIds.Units.Rover, levelOneBarracks.Building.EntityId).Success, "Level 1 mission rules block training extra Rovers");
Assert(!levelOneProductionSimulation.TryQueueUnit(ContentIds.Units.Commander, levelOneBarracks.Building.EntityId).Success, "Level 1 mission rules block training extra Commanders");

var guardianRetrofitSimulation = new RtsSimulation(catalog, 2000, [], 0, null, null, ridgeMission.AvailableUnitIds);
var retrofitHub = guardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
var retrofitPower = guardianRetrofitSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-220, 0));
Assert(retrofitPower.Success, retrofitPower.Message);
var retrofitBarracksResult = guardianRetrofitSimulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, new SimVector2(-20, 0));
Assert(retrofitBarracksResult.Success, retrofitBarracksResult.Message);
var retrofitBarracks = retrofitBarracksResult.Building!;
Assert(guardianRetrofitSimulation.ValidateGuardianRetrofit(retrofitBarracks.EntityId).MessageKey == "sim.barracks_upgrade.requires_grunts", "Guardian Retrofit requires two live Grunts at the base");
guardianRetrofitSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, retrofitBarracks.Position + new SimVector2(40, 0));
Assert(!guardianRetrofitSimulation.ValidateGuardianRetrofit(retrofitBarracks.EntityId).Success, "one Grunt is not enough to start Guardian Retrofit");
guardianRetrofitSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, retrofitHub.Position + new SimVector2(40, 0));
var retrofitMaterialsBefore = guardianRetrofitSimulation.Materials;
var retrofitStart = guardianRetrofitSimulation.TryStartGuardianRetrofit(retrofitBarracks.EntityId);
Assert(retrofitStart.Success, retrofitStart.Message);
Assert(retrofitStart.MessageKey == "sim.barracks_upgrade.started", "Guardian Retrofit start returns a stable message key");
Assert(Math.Abs(guardianRetrofitSimulation.Materials - (retrofitMaterialsBefore - catalog.GetBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit).Cost)) < 0.01f, "Guardian Retrofit spends materials immediately");
Assert(!guardianRetrofitSimulation.TryQueueUnit(ContentIds.Units.Cadet, retrofitBarracks.EntityId).Success, "Barracks cannot train while Guardian Retrofit is in progress");
retrofitPower.Building!.ApplyDamage(9999, "explosive");
var pausedSeconds = retrofitBarracks.ActiveBarracksUpgradeRemainingSeconds;
TickFor(guardianRetrofitSimulation, 5.0f);
Assert(Math.Abs(retrofitBarracks.ActiveBarracksUpgradeRemainingSeconds - pausedSeconds) < 0.01f, "Guardian Retrofit pauses while the Barracks is unpowered");
guardianRetrofitSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-120, 0));
TickFor(guardianRetrofitSimulation, pausedSeconds + 0.2f);
Assert(retrofitBarracks.HasBarracksUpgrade(ContentIds.BarracksUpgrades.GuardianRetrofit), "Guardian Retrofit completes after powered build time");
Assert(guardianRetrofitSimulation.DrainEvents().Any(item => item.MessageKey == "sim.event.barracks_upgrade_complete"), "Guardian Retrofit completion emits a player-known event");
Assert(guardianRetrofitSimulation.TryQueueUnit(ContentIds.Units.Guardian, retrofitBarracks.EntityId).Success, "Guardian training queues after the Barracks retrofit completes");

var unpoweredProductionSimulation = new RtsSimulation(catalog, 1000, []);
unpoweredProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
var unpoweredBarracks = unpoweredProductionSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
Assert(!unpoweredProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, unpoweredBarracks.EntityId).Success, "unpowered Barracks cannot train units");

var repairSimulation = new RtsSimulation(catalog, 1000, []);
var repairGrunt = repairSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new SimVector2(50, 0));
var repairPowerPlant = repairSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
repairPowerPlant.ApplyDamage(120, "debug");
var damagedHealth = repairPowerPlant.Health;
var repairMaterialsBefore = repairSimulation.Materials;
var repairStart = repairSimulation.CommandUnitRepairBuilding(repairGrunt.EntityId, repairPowerPlant.EntityId);
Assert(repairStart.Success, repairStart.Message);
TickFor(repairSimulation, 1.0f);
Assert(repairPowerPlant.Health > damagedHealth, "Grunt repairs damaged friendly buildings over time");
Assert(repairPowerPlant.Health < repairPowerPlant.Definition.Health, "Grunt repair takes time instead of finishing instantly");
Assert(repairSimulation.Materials < repairMaterialsBefore, "Grunt repair spends materials while restoring health");
Assert(repairGrunt.RepairTargetBuildingEntityId == repairPowerPlant.EntityId, "Grunt has one active repair target");
var repairTower = repairSimulation.AddStartingBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(180, 0));
repairTower.ApplyDamage(80, "debug");
Assert(repairSimulation.CommandUnitRepairBuilding(repairGrunt.EntityId, repairTower.EntityId).Success, "Grunt can switch repair targets");
Assert(repairGrunt.RepairTargetBuildingEntityId == repairTower.EntityId, "Grunt repair command replaces the previous target");
var repairRifleman = repairSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(50, 40));
Assert(!repairSimulation.CommandUnitRepairBuilding(repairRifleman.EntityId, repairTower.EntityId).Success, "non-Grunt units cannot repair buildings");
var enemyRepairTarget = repairSimulation.AddStartingBuilding(ContentIds.Buildings.Pylon, new SimVector2(360, 0), ContentIds.Factions.PrivateMilitary);
enemyRepairTarget.ApplyDamage(40, "debug");
Assert(!repairSimulation.CommandUnitRepairBuilding(repairGrunt.EntityId, enemyRepairTarget.EntityId).Success, "Grunt cannot repair enemy buildings");

var lightRepairSimulation = new RtsSimulation(catalog, 1000, []);
var lightRepairGrunt = lightRepairSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new SimVector2(50, 0));
var lightRepairBuilding = lightRepairSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
lightRepairBuilding.ApplyDamage(60, "debug");
var lightRepairMaterialsBefore = lightRepairSimulation.Materials;
Assert(lightRepairSimulation.CommandUnitRepairBuilding(lightRepairGrunt.EntityId, lightRepairBuilding.EntityId).Success, "light repair command starts");
TickFor(lightRepairSimulation, 3.0f);
var lightRepairCost = lightRepairMaterialsBefore - lightRepairSimulation.Materials;

var heavyRepairSimulation = new RtsSimulation(catalog, 1000, []);
var heavyRepairGrunt = heavyRepairSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new SimVector2(50, 0));
var heavyRepairBuilding = heavyRepairSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
heavyRepairBuilding.ApplyDamage(180, "debug");
var heavyRepairMaterialsBefore = heavyRepairSimulation.Materials;
Assert(heavyRepairSimulation.CommandUnitRepairBuilding(heavyRepairGrunt.EntityId, heavyRepairBuilding.EntityId).Success, "heavy repair command starts");
TickFor(heavyRepairSimulation, 8.0f);
var heavyRepairCost = heavyRepairMaterialsBefore - heavyRepairSimulation.Materials;
Assert(heavyRepairCost > lightRepairCost * 2.0f, "repair cost scales with missing health");

var lowMaterialRepairSimulation = new RtsSimulation(catalog, 1, []);
var lowMaterialRepairGrunt = lowMaterialRepairSimulation.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new SimVector2(50, 0));
var lowMaterialRepairBuilding = lowMaterialRepairSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
lowMaterialRepairBuilding.ApplyDamage(180, "debug");
Assert(lowMaterialRepairSimulation.CommandUnitRepairBuilding(lowMaterialRepairGrunt.EntityId, lowMaterialRepairBuilding.EntityId).Success, "repair can start with limited materials");
TickFor(lowMaterialRepairSimulation, 10.0f);
Assert(lowMaterialRepairSimulation.Materials >= 0.0f, "repair spending cannot drive materials negative");

var spawnSpreadSimulation = new RtsSimulation(catalog, 2000, []);
spawnSpreadSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(spawnSpreadSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-220, 0)).Success, "spawn spread test places power");
var spawnSpreadBarracks = spawnSpreadSimulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, new SimVector2(-20, 0));
Assert(spawnSpreadBarracks.Success, spawnSpreadBarracks.Message);
Assert(spawnSpreadSimulation.TryQueueUnit(ContentIds.Units.Cadet, spawnSpreadBarracks.Building!.EntityId).Success, "spawn spread queues first Cadet");
Assert(spawnSpreadSimulation.TryQueueUnit(ContentIds.Units.Cadet, spawnSpreadBarracks.Building.EntityId).Success, "spawn spread queues second Cadet");
TickFor(spawnSpreadSimulation, 17.0f);
var spawnedCadets = spawnSpreadSimulation.Units
    .Where(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Cadet)
    .ToArray();
Assert(spawnedCadets.Length == 2, "spawn spread test produced two Cadets");
Assert(spawnedCadets[0].Position.DistanceTo(spawnedCadets[1].Position) > 24.0f, "trained units spawn spread out instead of stacked");

var enemyConstructionSimulation = new RtsSimulation(
    catalog,
    startingMaterials,
    [("well_first_landing_central", EnemyAiMarkers.FirstLanding.ExtractorPosition)],
    1000);
enemyConstructionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyConstructionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
TickFor(enemyConstructionSimulation, 0.1f);
Assert(enemyConstructionSimulation.Buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary && building.Definition.Id == ContentIds.Buildings.PowerPlant), "enemy construction planner builds a Power Plant");
Assert(enemyConstructionSimulation.Buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary && building.Definition.Id == ContentIds.Buildings.Barracks), "enemy construction planner builds a Barracks");
Assert(enemyConstructionSimulation.Buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary && building.Definition.Id == ContentIds.Buildings.ExtractorRefinery), "enemy construction planner builds an Extractor on an open well");
Assert(enemyConstructionSimulation.EnemyMaterials < 1000, "enemy construction planner spends enemy resources");

var playerCombatSimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
playerCombatSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var playerRifleman = playerCombatSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var enemyRifleman = playerCombatSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(70, 0));
playerCombatSimulation.CommandUnitAttackUnit(playerRifleman.EntityId, enemyRifleman.EntityId);
TickFor(playerCombatSimulation, 4.0f);
Assert(enemyRifleman.IsDestroyed, "player combat unit can destroy an enemy unit");

var buildingCombatSimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
buildingCombatSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var buildingAttacker = buildingCombatSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var enemyBuilding = buildingCombatSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(120, 0), ContentIds.Factions.PrivateMilitary);
buildingCombatSimulation.CommandUnitAttackBuilding(buildingAttacker.EntityId, enemyBuilding.EntityId);
TickFor(buildingCombatSimulation, 3.0f);
Assert(enemyBuilding.Health < enemyBuilding.Definition.Health, "player combat unit can damage an enemy building");

var idleAutoFireSimulation = new RtsSimulation(catalog, startingMaterials, []);
idleAutoFireSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var idleRifleman = idleAutoFireSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var nearbyEnemyBuilding = idleAutoFireSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(130, 0), ContentIds.Factions.PrivateMilitary);
TickFor(idleAutoFireSimulation, 1.0f);
Assert(Math.Abs(nearbyEnemyBuilding.Health - nearbyEnemyBuilding.Definition.Health) < 0.01f, "idle player combat unit does not auto-fire at hostile buildings");
var nearbyEnemyUnit = idleAutoFireSimulation.AddUnit(ContentIds.Units.Cadet, ContentIds.Factions.PrivateMilitary, idleRifleman.Position + new SimVector2(80, 0));
TickFor(idleAutoFireSimulation, 1.0f);
Assert(nearbyEnemyUnit.Health < nearbyEnemyUnit.Definition.Health, "idle player combat unit auto-fires at a hostile unit in range");
var idleTankAutoFireSimulation = new RtsSimulation(catalog, startingMaterials, []);
idleTankAutoFireSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
idleTankAutoFireSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var nearbyEnemyTank = idleTankAutoFireSimulation.AddUnit(ContentIds.Units.MediumTank, ContentIds.Factions.PrivateMilitary, new SimVector2(95, 0));
TickFor(idleTankAutoFireSimulation, 1.0f);
Assert(nearbyEnemyTank.Health < nearbyEnemyTank.Definition.Health, "idle player combat unit auto-fires at a hostile tank in range");

var moveOverridesIdleFireSimulation = new RtsSimulation(catalog, startingMaterials, []);
moveOverridesIdleFireSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var movingRifleman = moveOverridesIdleFireSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var ignoredCadet = moveOverridesIdleFireSimulation.AddUnit(ContentIds.Units.Cadet, ContentIds.Factions.PrivateMilitary, new SimVector2(80, 0));
moveOverridesIdleFireSimulation.CommandUnitMove(movingRifleman.EntityId, new SimVector2(-260, 0));
TickFor(moveOverridesIdleFireSimulation, 0.2f);
Assert(movingRifleman.Position.X < -5.0f, "explicit move orders make infantry move instead of stopping for idle auto-fire");
Assert(Math.Abs(ignoredCadet.Health - ignoredCadet.Definition.Health) < 0.01f, "idle auto-fire waits while a direct move order is active");

var moveCancelsBuildingAttackSimulation = new RtsSimulation(catalog, startingMaterials, []);
moveCancelsBuildingAttackSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var cancelAttacker = moveCancelsBuildingAttackSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var cancelTargetHub = moveCancelsBuildingAttackSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(170, 0), ContentIds.Factions.PrivateMilitary);
moveCancelsBuildingAttackSimulation.CommandUnitAttackBuilding(cancelAttacker.EntityId, cancelTargetHub.EntityId);
TickFor(moveCancelsBuildingAttackSimulation, 1.0f);
Assert(cancelTargetHub.Health < cancelTargetHub.Definition.Health, "setup: player Rifleman starts damaging the enemy Hub");
var hubHealthAfterAttack = cancelTargetHub.Health;
moveCancelsBuildingAttackSimulation.CommandUnitMove(cancelAttacker.EntityId, new SimVector2(-260, 0));
TickFor(moveCancelsBuildingAttackSimulation, 1.0f);
Assert(cancelAttacker.TargetBuildingEntityId is null, "move command clears a stale building attack target");
Assert(cancelAttacker.Position.X < -40.0f, "move command pulls a Rifleman off an enemy building attack");
Assert(Math.Abs(cancelTargetHub.Health - hubHealthAfterAttack) < 0.01f, "Rifleman stops shooting the enemy Hub after a direct move command");

var enemyRangeParitySimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
enemyRangeParitySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyRangeParitySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
var playerStandingRifleman = enemyRangeParitySimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var riflemanRange = RtsSimulation.ToWorldRadius(catalog.GetUnit(ContentIds.Units.Rifleman).AttackRange);
var enemyOverrangeRifleman = enemyRangeParitySimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(riflemanRange + 16.0f, 0));
TickFor(enemyRangeParitySimulation, 0.1f);
Assert(Math.Abs(playerStandingRifleman.Health - playerStandingRifleman.Definition.Health) < 0.01f, "enemy Rifleman cannot damage a player Rifleman outside the same Rifleman weapon range");
Assert(enemyOverrangeRifleman.TargetUnitEntityId == playerStandingRifleman.EntityId, "enemy Rifleman pursues visible player troops before firing");

var attackVisualSimulation = new RtsSimulation(catalog, startingMaterials, []);
attackVisualSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var visualAttacker = attackVisualSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var visualTarget = attackVisualSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(60, 0));
attackVisualSimulation.CommandUnitAttackUnit(visualAttacker.EntityId, visualTarget.EntityId);
TickFor(attackVisualSimulation, 0.1f);
Assert(visualAttacker.AttackFlashSeconds > 0.0f, "unit attack records a short outgoing-fire flash");
Assert(visualTarget.HitFlashSeconds > 0.0f, "unit damage records a short incoming-fire flash");
Assert(visualTarget.LastIncomingAttackOrigin is not null && visualTarget.LastIncomingAttackOrigin.Value.DistanceTo(visualAttacker.Position) < 0.01f, "unit damage records incoming-fire direction");
TickFor(attackVisualSimulation, 0.5f);
Assert(visualTarget.HitFlashSeconds <= 0.0f, "incoming-fire flash decays without gameplay side effects");

var commanderBuildingFilterSimulation = new RtsSimulation(catalog, startingMaterials, []);
commanderBuildingFilterSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var commanderWithPistol = commanderBuildingFilterSimulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var commanderBuildingTarget = commanderBuildingFilterSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(70, 0), ContentIds.Factions.PrivateMilitary);
commanderBuildingFilterSimulation.CommandUnitAttackBuilding(commanderWithPistol.EntityId, commanderBuildingTarget.EntityId);
TickFor(commanderBuildingFilterSimulation, 3.0f);
Assert(Math.Abs(commanderBuildingTarget.Health - commanderBuildingTarget.Definition.Health) < 0.01f, "Commander target filters do not allow building damage");

var attackFormationSimulation = new RtsSimulation(catalog, startingMaterials, []);
attackFormationSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var formationLeft = attackFormationSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-280, -40));
var formationRight = attackFormationSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-280, 40));
var formationTarget = attackFormationSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(260, 0));
attackFormationSimulation.CommandUnitAttackUnit(formationLeft.EntityId, formationTarget.EntityId, new SimVector2(-52, 0));
attackFormationSimulation.CommandUnitAttackUnit(formationRight.EntityId, formationTarget.EntityId, new SimVector2(52, 0));
TickFor(attackFormationSimulation, 0.2f);
Assert(formationLeft.MoveTarget is not null && formationRight.MoveTarget is not null, "attack formation creates movement targets while closing range");
var leftMoveTarget = formationLeft.MoveTarget!.Value;
var rightMoveTarget = formationRight.MoveTarget!.Value;
Assert(leftMoveTarget.DistanceTo(rightMoveTarget) > 80.0f, "attack formation offsets keep grouped attackers from sharing one destination");

var enemyPrioritySimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
enemyPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-210, -120));
var priorityExtractor = enemyPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.ExtractorRefinery, new SimVector2(-160, -120));
enemyPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(-120, -120));
enemyPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
var priorityEnemy = enemyPrioritySimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(-80, -120));
TickFor(enemyPrioritySimulation, 0.2f);
Assert(priorityEnemy.TargetBuildingEntityId == priorityExtractor.EntityId, "enemy target priority favors visible Extractor before base cracking");

var commanderPrioritySimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
commanderPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
commanderPrioritySimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
var exposedCommander = commanderPrioritySimulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(520, 120));
var commanderHunter = commanderPrioritySimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(600, 120));
TickFor(commanderPrioritySimulation, 0.2f);
Assert(commanderPrioritySimulation.EnemyOfficer.CommanderSighted, "enemy officer internally remembers when the Commander is visible");
Assert(commanderHunter.TargetUnitEntityId == exposedCommander.EntityId, "enemy target priority can punish an exposed visible Commander");

var retreatSimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
retreatSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
retreatSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
var retreatingEnemy = retreatSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, EnemyAiMarkers.FirstLanding.HubPosition + new SimVector2(-180, 0));
TickFor(retreatSimulation, 0.2f);
retreatingEnemy.ApplyDamage(34, "ballistic");
var retreatDistanceBefore = retreatingEnemy.Position.DistanceTo(EnemyAiMarkers.FirstLanding.HubPosition);
TickFor(retreatSimulation, 0.3f);
Assert(retreatingEnemy.IsEnemyRetreating, "badly damaged committed enemies can retreat toward base");
Assert(retreatingEnemy.Position.DistanceTo(EnemyAiMarkers.FirstLanding.HubPosition) < retreatDistanceBefore, "retreating enemy moves closer to its base");
TickFor(retreatSimulation, 20.0f);
Assert(!retreatingEnemy.IsEnemyRetreating && !retreatingEnemy.IsEnemyAttackCommitted, "badly damaged enemies stand down after reaching their base");
TickFor(retreatSimulation, 20.0f);
Assert(!retreatingEnemy.IsEnemyAttackCommitted, "badly damaged enemies are not immediately recommitted into another attack wave");

var regroupSimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
regroupSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
regroupSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
var doomedAttacker = regroupSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, EnemyAiMarkers.FirstLanding.HubPosition + new SimVector2(-180, 0));
TickFor(regroupSimulation, 0.2f);
Assert(doomedAttacker.IsEnemyAttackCommitted, "enemy attacker is committed before the regroup test");
doomedAttacker.ApplyDamage(999, "ballistic");
TickFor(regroupSimulation, 0.2f);
Assert(regroupSimulation.EnemyAiTelemetry.AttackGroupsLost == 1, "enemy telemetry records a wiped committed attack group");
Assert(regroupSimulation.EnemyOfficer.NextAttackAllowedSeconds > regroupSimulation.ElapsedSeconds, "wiped attack group creates a regroup delay");

var missionLossSimulation = new RtsSimulation(catalog, startingMaterials, []);
missionLossSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var commander = missionLossSimulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
commander.ApplyDamage(999, "ballistic");
missionLossSimulation.Tick(0.1f);
Assert(missionLossSimulation.MissionState.Status == MissionStatus.Lost, "commander death triggers mission loss");
var elapsedAtLoss = missionLossSimulation.ElapsedSeconds;
missionLossSimulation.Tick(1.0f);
Assert(Math.Abs(missionLossSimulation.ElapsedSeconds - elapsedAtLoss) < 0.01f, "terminal mission loss stops simulation ticking");

var debugLossSimulation = new RtsSimulation(catalog, startingMaterials, []);
debugLossSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
debugLossSimulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
Assert(debugLossSimulation.DebugKillPlayerCommander(), "debug commander loss command kills the Commander");
Assert(debugLossSimulation.MissionState.Status == MissionStatus.Lost, "debug commander loss command updates mission state immediately");

var missionWinSimulation = new RtsSimulation(catalog, startingMaterials, []);
missionWinSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var finalEnemy = missionWinSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(90, 0));
finalEnemy.ApplyDamage(999, "ballistic");
missionWinSimulation.Tick(0.1f);
Assert(missionWinSimulation.MissionState.Status == MissionStatus.Won, "destroying all enemy targets triggers mission win");

var destroyedEnemyBarracksSimulation = new RtsSimulation(catalog, startingMaterials, []);
destroyedEnemyBarracksSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var destroyedEnemyBarracks = destroyedEnemyBarracksSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(90, 0), ContentIds.Factions.PrivateMilitary);
destroyedEnemyBarracks.ApplyDamage(9999, "explosive");
destroyedEnemyBarracksSimulation.Tick(0.1f);
Assert(destroyedEnemyBarracksSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Cadet && !unit.IsDestroyed) == 3, "destroyed enemy Barracks releases three Cadets");
Assert(destroyedEnemyBarracksSimulation.MissionState.Status == MissionStatus.Active, "destroyed enemy Barracks Cadets must be cleared before victory");
destroyedEnemyBarracksSimulation.Tick(1.0f);
Assert(destroyedEnemyBarracksSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Cadet && !unit.IsDestroyed) == 3, "destroyed Barracks Cadets spawn only once");

var destroyedPlayerPowerSimulation = new RtsSimulation(catalog, startingMaterials, []);
destroyedPlayerPowerSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var destroyedPlayerPower = destroyedPlayerPowerSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
destroyedPlayerPower.ApplyDamage(9999, "explosive");
destroyedPlayerPowerSimulation.Tick(0.1f);
Assert(destroyedPlayerPowerSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Cadet && !unit.IsDestroyed) == 1, "destroyed player Power Plant releases one player Cadet");

var hubRevealWinSimulation = new RtsSimulation(catalog, startingMaterials, []);
hubRevealWinSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var enemyHubToDestroy = hubRevealWinSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(90, 0), ContentIds.Factions.PrivateMilitary);
enemyHubToDestroy.ApplyDamage(9999, "explosive");
hubRevealWinSimulation.Tick(0.1f);
var revealedEnemyHubTank = hubRevealWinSimulation.Units.Single(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.MediumTank && !unit.IsDestroyed);
Assert(revealedEnemyHubTank.IsColonyHubOccupant, "destroyed enemy Hub releases a Medium Tank occupant");
Assert(hubRevealWinSimulation.MissionState.Status == MissionStatus.Active, "enemy Hub tank occupant must be cleared before destroy-all-enemies victory");
revealedEnemyHubTank.ApplyDamage(9999, "explosive");
hubRevealWinSimulation.Tick(0.1f);
Assert(hubRevealWinSimulation.MissionState.Status == MissionStatus.Won, "destroy-all-enemies victory waits for the released Hub tank to die");

var playerHubRevealSimulation = new RtsSimulation(catalog, startingMaterials, []);
var playerHubToDestroy = playerHubRevealSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
playerHubRevealSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(90, 0));
playerHubToDestroy.ApplyDamage(9999, "explosive");
playerHubRevealSimulation.Tick(0.1f);
Assert(playerHubRevealSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.MediumTank && !unit.IsDestroyed), "destroyed player Hub releases a Medium Tank for the player");
Assert(playerHubRevealSimulation.MissionState.Status == MissionStatus.Active, "player Hub Medium Tank release does not replace mission loss conditions by itself");

var towerUpgradeSimulation = new RtsSimulation(catalog, 3000, []);
towerUpgradeSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(towerUpgradeSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0)).Success, "tower upgrade test places power");
var wallTowerA = towerUpgradeSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, -80));
var wallTowerB = towerUpgradeSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(130, 80));
Assert(wallTowerA.Success && wallTowerB.Success, "tower upgrade test places wall anchors");
Assert(towerUpgradeSimulation.EnergyWalls.Count == 1, "wall link exists before tower upgrade");
Assert(towerUpgradeSimulation.TryUpgradeBuilding(wallTowerA.Building!.EntityId, ContentIds.Buildings.GunTower).Success, "Defense Tower upgrades into Gun Tower");
Assert(towerUpgradeSimulation.ValidateBuildingUpgrade(wallTowerB.Building!.EntityId, ContentIds.Buildings.RocketTower).MessageKey == "sim.upgrade.can_upgrade", "upgrade validation returns a stable message key");
Assert(towerUpgradeSimulation.EnergyWalls.Count == 1, "tower upgrade preserves wall link");
var towerTarget = towerUpgradeSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(260, -80));
TickFor(towerUpgradeSimulation, 1.0f);
Assert(towerTarget.Health < towerTarget.Definition.Health, "powered Gun Tower fires at enemies");
Assert(towerUpgradeSimulation.TryUpgradeBuilding(wallTowerB.Building!.EntityId, ContentIds.Buildings.RocketTower).Success, "Defense Tower upgrades into Rocket Tower");
var towerBuildingTarget = towerUpgradeSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(280, 80), ContentIds.Factions.PrivateMilitary);
TickFor(towerUpgradeSimulation, 3.0f);
Assert(towerBuildingTarget.Health < towerBuildingTarget.Definition.Health, "powered Rocket Tower damages enemy buildings");

var splashSimulation = new RtsSimulation(catalog, 4000, []);
splashSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
Assert(splashSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-40, 0)).Success, "splash test places power");
var splashTower = splashSimulation.TryPlaceBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(110, 0));
Assert(splashTower.Success, splashTower.Message);
Assert(splashSimulation.TryUpgradeBuilding(splashTower.Building!.EntityId, ContentIds.Buildings.RocketTower).Success, "splash test upgrades Rocket Tower");
var splashTarget = splashSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(230, 0));
var splashNeighbor = splashSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(250, 20));
var friendlyInBlast = splashSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(240, -20));
TickFor(splashSimulation, 3.0f);
Assert(splashTarget.Health < splashTarget.Definition.Health, "Rocket Tower damages direct target");
Assert(splashNeighbor.Health < splashNeighbor.Definition.Health, "Rocket Tower splash damages nearby enemy");
Assert(friendlyInBlast.Health < friendlyInBlast.Definition.Health, "friendly-fire explosive splash can hurt allied units");

var ballisticFriendlyFireSimulation = new RtsSimulation(catalog, startingMaterials, []);
ballisticFriendlyFireSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
var shooter = ballisticFriendlyFireSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var ballisticEnemy = ballisticFriendlyFireSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(70, 0), ContentIds.Factions.PrivateMilitary);
var ballisticAlly = ballisticFriendlyFireSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(70, 0));
ballisticFriendlyFireSimulation.CommandUnitAttackBuilding(shooter.EntityId, ballisticEnemy.EntityId);
TickFor(ballisticFriendlyFireSimulation, 1.0f);
Assert(ballisticEnemy.Health < ballisticEnemy.Definition.Health, "ballistic attack damages enemy target");
Assert(Math.Abs(ballisticAlly.Health - ballisticAlly.Definition.Health) < 0.01f, "non-friendly-fire ballistic attack does not hurt allies");

var crushSimulation = new RtsSimulation(catalog, startingMaterials, []);
crushSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
var rover = crushSimulation.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var infantryToCrush = crushSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(40, 0));
var friendlyInfantryNearCrush = crushSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(230, 0));
Assert(!rover.Definition.CanAttack, "Rover cannot shoot");
crushSimulation.CommandUnitMove(rover.EntityId, new SimVector2(260, 0));
TickFor(crushSimulation, 2.0f);
Assert(infantryToCrush.IsDestroyed, "Rover crush kills enemy Rifleman while moving through it");
Assert(Math.Abs(friendlyInfantryNearCrush.Health - friendlyInfantryNearCrush.Definition.Health) < 0.01f, "Rover does not crush friendly infantry in this pass");

var mediumTankCrushSimulation = new RtsSimulation(catalog, startingMaterials, []);
mediumTankCrushSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
var mediumCrusher = mediumTankCrushSimulation.AddUnit(ContentIds.Units.MediumTank, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var mediumCrushTarget = mediumTankCrushSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(230, 0));
mediumTankCrushSimulation.CommandUnitMove(mediumCrusher.EntityId, new SimVector2(320, 0));
TickFor(mediumTankCrushSimulation, 4.0f);
Assert(mediumCrushTarget.IsDestroyed, "Medium Tank crush kills enemy Rifleman while moving through it");

var heavyTankCrushSimulation = new RtsSimulation(catalog, startingMaterials, []);
heavyTankCrushSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
var heavyCrusher = heavyTankCrushSimulation.AddUnit(ContentIds.Units.Tank, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
var heavyCrushTarget = heavyTankCrushSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(230, 0));
heavyTankCrushSimulation.CommandUnitMove(heavyCrusher.EntityId, new SimVector2(320, 0));
TickFor(heavyTankCrushSimulation, 4.0f);
Assert(heavyCrushTarget.IsDestroyed, "Heavy Tank crush kills enemy Rifleman while moving through it");

var fogSimulation = new RtsSimulation(catalog, startingMaterials, []);
fogSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
var scout = fogSimulation.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new SimVector2(-450, 0));
var hiddenEnemy = fogSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(620, 260), ContentIds.Factions.PrivateMilitary);
Assert(!fogSimulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "distant enemy starts hidden by fog");
fogSimulation.CommandUnitMove(scout.EntityId, new SimVector2(600, 240));
TickFor(fogSimulation, 12.0f);
Assert(fogSimulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "Rover scouting reveals enemy");
fogSimulation.CommandUnitMove(scout.EntityId, new SimVector2(-450, 0));
TickFor(fogSimulation, 12.0f);
Assert(fogSimulation.IsExploredByFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "scouted terrain remains explored");
Assert(!fogSimulation.IsCurrentlyObservedByFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "scout no longer has current sight after leaving");
Assert(fogSimulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "enemy in explored terrain remains visible in real time");
var enemyInBlackFog = fogSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(900, -360));
Assert(!fogSimulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, enemyInBlackFog.Position), "enemy in unexplored black fog remains hidden");

FirstLandingMissionSmoke.Run(context);
WellsAtTheRidgeSmoke.Run(context);
MissionTriggerSmoke.Run(context);
MapEditorSmoke.Run(context);
CombatRobustnessSmoke.Run(context);
BuildingDestroyedRevealSmoke.Run(context);
EnemyAiBehaviorSmoke.Run(context);

Console.WriteLine("Simulation smoke checks passed.");
