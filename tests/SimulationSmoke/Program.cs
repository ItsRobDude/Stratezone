using Stratezone.Simulation;
using Stratezone.Simulation.Content;

var repoRoot = FindRepoRoot();
var gameRoot = Path.Combine(repoRoot, "game");
var catalog = ContentCatalog.LoadFromGameData(gameRoot);
var mission = catalog.GetMission(ContentIds.Missions.FirstLanding);
var startingMaterials = mission.PlayerStartingResources[ContentIds.Resources.Materials];

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
