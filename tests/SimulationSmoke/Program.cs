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

var pylon = simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-350, 50));
Assert(pylon.Success, pylon.Message);
Assert(pylon.Building?.IsPowered == true, "pylon is powered by the plant");
Assert(simulation.ValidatePlacement(ContentIds.Buildings.Barracks, new SimVector2(-470, 50)).IsLegal, "pylon extends powered placement");

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

Console.WriteLine("Simulation smoke checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException($"Assertion failed: {message}");
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
