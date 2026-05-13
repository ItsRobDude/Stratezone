using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class PowerDirtySmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateIdleTicksDoNotRecomputePower(context);
        ValidateDirectDestroyedBuildingDirtiesPower(context);
        ValidateUnitSplashDestructionUpdatesPower(context);
    }

    private static void ValidateIdleTicksDoNotRecomputePower(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-60, 0));
        simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(130, 0));

        var recomputesBeforeIdleTick = simulation.DebugPowerRecomputeCount;
        simulation.Tick(0.5f);
        Assert(simulation.DebugPowerRecomputeCount == recomputesBeforeIdleTick, "idle simulation tick does not recompute power");
    }

    private static void ValidateDirectDestroyedBuildingDirtiesPower(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var powerPlant = simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(-60, 0)).Building!;
        var pylon = simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(130, 0)).Building!;
        Assert(pylon.IsPowered, "pylon starts powered before direct destruction test");

        var recomputesBeforeDestroy = simulation.DebugPowerRecomputeCount;
        powerPlant.ApplyDamage(9999, "debug");
        simulation.Tick(0.1f);

        Assert(simulation.DebugPowerRecomputeCount > recomputesBeforeDestroy, "direct building destruction marks power dirty by next tick");
        Assert(!pylon.IsPowered, "directly destroyed power provider updates dependent power state");
    }

    private static void ValidateUnitSplashDestructionUpdatesPower(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 4000, []);
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        var powerPlant = simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new SimVector2(0, 0));
        var pylon = simulation.AddStartingBuilding(ContentIds.Buildings.Pylon, new SimVector2(150, 0));
        Assert(pylon.IsPowered, "pylon starts powered before splash destruction test");

        powerPlant.ApplyDamage(powerPlant.Definition.Health - 1.0f, "debug");
        var tank = simulation.AddUnit(ContentIds.Units.Tank, ContentIds.Factions.PlayerExpedition, new SimVector2(-100, 0));
        var target = simulation.AddUnit(ContentIds.Units.Cadet, ContentIds.Factions.PrivateMilitary, new SimVector2(0, 0));
        simulation.CommandUnitAttackUnit(tank.EntityId, target.EntityId);

        TickFor(simulation, 0.2f);

        Assert(powerPlant.IsDestroyed, "unit-vs-unit explosive splash can destroy a nearby power building");
        Assert(!pylon.IsPowered, "unit-vs-unit splash destruction recomputes power immediately");
    }
}
