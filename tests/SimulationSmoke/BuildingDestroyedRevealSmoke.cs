using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class BuildingDestroyedRevealSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateBarracksReveal(context);
        ValidatePowerPlantReveal(context);
        ValidateHubOccupantReveal(context);
        ValidateNoRevealBuilding(context);
        ValidateMaterialTrade(context);
    }

    private static void ValidateBarracksReveal(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var barracks = simulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(0, 0));
        simulation.DrainEvents();

        barracks.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);

        var cadets = simulation.Units
            .Where(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Cadet)
            .ToArray();
        Assert(cadets.Length == 3, "destroyed player Barracks releases three player Cadets");
        Assert(cadets.All(unit => !unit.IsColonyHubOccupant), "Barracks Cadets are not Hub occupants");
        Assert(!simulation.DrainEvents().Any(), "destroyed-building reveal stays silent and emits no simulation event");

        TickFor(simulation, 1.0f);
        Assert(simulation.Units.Count(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && unit.Definition.Id == ContentIds.Units.Cadet) == 3, "destroyed-building reveal fires only once");
    }

    private static void ValidatePowerPlantReveal(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var powerPlant = simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0), ContentIds.Factions.PrivateMilitary);

        powerPlant.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);

        var cadets = simulation.Units
            .Where(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && unit.Definition.Id == ContentIds.Units.Cadet)
            .ToArray();
        Assert(cadets.Length == 1, "destroyed enemy Power Plant releases one enemy Cadet");
        Assert(!cadets[0].IsColonyHubOccupant, "Power Plant Cadet is not a Hub occupant");
    }

    private static void ValidateHubOccupantReveal(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var enemyHub = simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(120, 0), ContentIds.Factions.PrivateMilitary);

        enemyHub.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);

        var occupant = simulation.Units.Single(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.Id == ContentIds.Units.MediumTank &&
            !unit.IsDestroyed);
        Assert(occupant.IsColonyHubOccupant, "destroyed Hub releases a Medium Tank marked as an occupant");
        Assert(simulation.MissionState.Status == MissionStatus.Active, "hostile Hub occupant blocks mission completion");
        occupant.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);
        Assert(simulation.MissionState.Status == MissionStatus.Won, "mission can complete after the hostile Hub occupant dies");
    }

    private static void ValidateNoRevealBuilding(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var tower = simulation.AddStartingBuilding(ContentIds.Buildings.DefenseTower, new SimVector2(0, 0));

        tower.ApplyDamage(9999, "explosive");
        TickFor(simulation, 0.1f);

        Assert(!simulation.Units.Any(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition), "building without destroyed_reveal spawns no units");
    }

    private static void ValidateMaterialTrade(SmokeTestContext context)
    {
        var barracks = context.Catalog.GetBuilding(ContentIds.Buildings.Barracks);
        var powerPlant = context.Catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
        var cadet = context.Catalog.GetUnit(ContentIds.Units.Cadet);

        Assert(barracks.DestroyedReveal?.UnitId == ContentIds.Units.Cadet && barracks.DestroyedReveal.Count * cadet.Cost < barracks.Cost, "Barracks Cadet reveal trade still favors the attacker");
        Assert(powerPlant.DestroyedReveal?.UnitId == ContentIds.Units.Cadet && powerPlant.DestroyedReveal.Count * cadet.Cost < powerPlant.Cost, "Power Plant Cadet reveal trade still favors the attacker");
    }
}
