using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Localization;

var repoRoot = FindRepoRoot();
var gameRoot = Path.Combine(repoRoot, "game");
var catalog = ContentCatalog.LoadFromGameData(gameRoot);
var localization = LocalizationCatalog.LoadFromGameData(gameRoot);
var mission = catalog.GetMission(ContentIds.Missions.FirstLanding);
var startingMaterials = mission.PlayerStartingResources[ContentIds.Resources.Materials];

Assert(localization.Translate("ui.hud.build_line").Contains("Build:", StringComparison.Ordinal), "English localization catalog loads HUD strings");
Assert(localization.Translate("missing.test.key") == "[[missing.test.key]]", "missing localization keys are obvious");
Assert(localization.ContentName(ContentIds.Units.Worker) == "Worker", "content name localization keys resolve stable content ids");

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

var pylon = simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-220, 160));
Assert(pylon.Success, pylon.Message);
Assert(pylon.Building?.IsPowered == true, "pylon is powered by the plant");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Pylon, new SimVector2(-430, 170)).IsLegal, "powered pylons can daisy chain to new pylons");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(-350, 100)).IsLegal, "pylon extends powered placement");

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
var blockedEnemy = wallSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(520, 70));
TickFor(wallSimulation, 5.0f);
Assert(blockedEnemy.IsBlockedByEnergyWall, "enemy pressure recognizes a blocking energy wall");
Assert(blockedEnemy.TargetBuildingEntityId == firstTower.Building?.EntityId || blockedEnemy.TargetBuildingEntityId == secondTower.Building?.EntityId, "blocked enemy targets a wall anchor");
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

var enemyBaseSimulation = new RtsSimulation(catalog, startingMaterials, [], 450);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, RtsSimulation.EnemyHubPosition, ContentIds.Factions.PrivateMilitary);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, RtsSimulation.EnemyPowerPlantPosition, ContentIds.Factions.PrivateMilitary);
enemyBaseSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, RtsSimulation.EnemyBarracksPosition, ContentIds.Factions.PrivateMilitary);
TickFor(enemyBaseSimulation, 0.1f);
Assert(enemyBaseSimulation.ProductionOrders.Count == 1, "enemy base queues production from powered Barracks");
Assert(enemyBaseSimulation.EnemyMaterials <= 350, "enemy production and construction spend resources when queued");
TickFor(enemyBaseSimulation, 13.0f);
Assert(enemyBaseSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Rifleman), "enemy production spawns trained units from its base");

var playerProductionSimulation = new RtsSimulation(catalog, 2000, []);
playerProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
Assert(playerProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-220, 0)).Success, "player production test places power");
var playerBarracks = playerProductionSimulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, new SimVector2(-20, 0));
Assert(playerBarracks.Success, playerBarracks.Message);
var materialsBeforeTraining = playerProductionSimulation.Materials;
var riflemanQueue = playerProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, playerBarracks.Building!.EntityId);
Assert(riflemanQueue.Success, riflemanQueue.Message);
Assert(riflemanQueue.MessageKey == "sim.production.queued", "production result returns a stable message key");
Assert(playerProductionSimulation.Materials < materialsBeforeTraining, "player training spends materials immediately");
TickFor(playerProductionSimulation, 11.0f);
Assert(playerProductionSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Rifleman), "player production spawns trained units from the Colony Hub");
Assert(!playerProductionSimulation.TryQueueUnit(ContentIds.Units.Guardian, playerBarracks.Building.EntityId).Success, "Guardian training requires powered Armory Annex");
playerProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ArmoryAnnex, new SimVector2(-40, 80));
Assert(playerProductionSimulation.TryQueueUnit(ContentIds.Units.Guardian, playerBarracks.Building.EntityId).Success, "powered Armory Annex unlocks Guardian training");

var unpoweredProductionSimulation = new RtsSimulation(catalog, 1000, []);
unpoweredProductionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-500, 0));
var unpoweredBarracks = unpoweredProductionSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
Assert(!unpoweredProductionSimulation.TryQueueUnit(ContentIds.Units.Rifleman, unpoweredBarracks.EntityId).Success, "unpowered Barracks cannot train units");

var enemyConstructionSimulation = new RtsSimulation(
    catalog,
    startingMaterials,
    [("well_first_landing_central", RtsSimulation.EnemyExtractorPosition)],
    1000);
enemyConstructionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
enemyConstructionSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, RtsSimulation.EnemyHubPosition, ContentIds.Factions.PrivateMilitary);
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

var enemyBuilding = playerCombatSimulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(120, 0), ContentIds.Factions.PrivateMilitary);
playerCombatSimulation.CommandUnitAttackBuilding(playerRifleman.EntityId, enemyBuilding.EntityId);
TickFor(playerCombatSimulation, 3.0f);
Assert(enemyBuilding.Health < enemyBuilding.Definition.Health, "player combat unit can damage an enemy building");

var missionLossSimulation = new RtsSimulation(catalog, startingMaterials, []);
missionLossSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var commander = missionLossSimulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
commander.ApplyDamage(999, "ballistic");
missionLossSimulation.Tick(0.1f);
Assert(missionLossSimulation.MissionState.Status == MissionStatus.Lost, "commander death triggers mission loss");

var missionWinSimulation = new RtsSimulation(catalog, startingMaterials, []);
missionWinSimulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
var finalEnemy = missionWinSimulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(90, 0));
finalEnemy.ApplyDamage(999, "ballistic");
missionWinSimulation.Tick(0.1f);
Assert(missionWinSimulation.MissionState.Status == MissionStatus.Won, "destroying all enemy targets triggers mission win");

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
Assert(!fogSimulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, hiddenEnemy.Position), "enemy outside current vision is hidden again");

var missionMarkers = mission.Markers.ToDictionary(marker => marker.Id, marker => marker.Position, StringComparer.Ordinal);
Assert(mission.ResourceWellPlacements.Count == 2, "mission data owns resource well placements");
Assert(mission.StartingEntities.Any(entity => entity.ContentId == ContentIds.Units.Worker), "mission data owns starting player units");
var missionWellPlacements = mission.ResourceWellPlacements
    .Select(placement => (placement.WellId, missionMarkers[placement.MarkerId] + placement.Offset))
    .ToArray();
var pacedMissionSimulation = new RtsSimulation(
    catalog,
    startingMaterials,
    missionWellPlacements,
    mission.EnemyStartingResources[ContentIds.Resources.Materials],
    EnemyAiMarkers.FromMission(mission),
    mission.EnemyAiProfile);
foreach (var entity in mission.StartingEntities)
{
    var position = missionMarkers[entity.MarkerId] + entity.Offset;
    if (entity.ContentId.StartsWith("building_", StringComparison.Ordinal))
    {
        pacedMissionSimulation.AddStartingBuilding(entity.ContentId, position, entity.FactionId);
    }
    else if (entity.ContentId.StartsWith("unit_", StringComparison.Ordinal))
    {
        pacedMissionSimulation.AddUnit(entity.ContentId, entity.FactionId, position);
    }
}

TickFor(pacedMissionSimulation, mission.EnemyAiProfile.FirstAttackDelaySeconds - 5.0f);
Assert(!pacedMissionSimulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.TargetBuildingEntityId is not null), "mission AI profile delays first enemy pressure");
TickFor(pacedMissionSimulation, 20.0f);
Assert(pacedMissionSimulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary) ||
    pacedMissionSimulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Rifleman) > 0,
    "mission AI profile starts paced enemy production after delay");

Console.WriteLine("Simulation smoke checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Assertion failed: {message}");
    }
}

static void TickFor(RtsSimulation simulation, float seconds)
{
    const float step = 0.1f;
    var elapsed = 0.0f;
    while (elapsed < seconds)
    {
        simulation.Tick(MathF.Min(step, seconds - elapsed));
        elapsed += step;
    }
}

static string FindRepoRoot()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (directory is not null)
    {
        if (Directory.Exists(Path.Combine(directory.FullName, "game")) &&
            Directory.Exists(Path.Combine(directory.FullName, "docs")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not find Stratezone repo root.");
}
