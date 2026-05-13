using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class CombatRobustnessSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateDirectTargetDamage(context);
        ValidateUnitSplashBuildingDestructionUpdatesPower(context);
    }

    private static void ValidateDirectTargetDamage(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var shooter = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));
        var directTarget = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(90, 0));
        var overlappingNeighbor = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(90, 0));

        simulation.CommandUnitAttackUnit(shooter.EntityId, directTarget.EntityId);
        TickFor(simulation, 0.2f);

        Assert(directTarget.Health < directTarget.Definition.Health, "non-AOE direct fire damages the requested target");
        Assert(Math.Abs(overlappingNeighbor.Health - overlappingNeighbor.Definition.Health) < 0.01f, "non-AOE direct fire does not damage another unit at the same position");
    }

    private static void ValidateUnitSplashBuildingDestructionUpdatesPower(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
        var pylon = simulation.AddStartingBuilding(ContentIds.Buildings.Pylon, new SimVector2(160, 0));
        var barracks = simulation.AddStartingBuilding(ContentIds.Buildings.Barracks, new SimVector2(300, 0));
        Assert(barracks.IsPowered, "test Barracks starts powered through the Pylon");

        var tankTarget = simulation.AddUnit(ContentIds.Units.MediumTank, ContentIds.Factions.PlayerExpedition, pylon.Position + new SimVector2(8, 0));
        var sheller = simulation.AddUnit(ContentIds.Units.Tank, ContentIds.Factions.PrivateMilitary, pylon.Position + new SimVector2(0, -120));
        simulation.CommandUnitAttackUnit(sheller.EntityId, tankTarget.EntityId);

        TickFor(simulation, 8.5f);

        Assert(pylon.IsDestroyed, "unit-vs-unit explosive splash can destroy nearby buildings");
        Assert(!barracks.IsPowered, "building destruction from unit-vs-unit splash recomputes power state");
    }
}
