using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class EnemyAiBehaviorSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateCommanderSightedTargetingFallsBack(context);
    }

    private static void ValidateCommanderSightedTargetingFallsBack(SmokeTestContext context)
    {
        var simulation = new RtsSimulation(context.Catalog, 1000, [], 0);
        var playerHub = simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, 0));
        simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, EnemyAiMarkers.FirstLanding.HubPosition, ContentIds.Factions.PrivateMilitary);
        var commander = simulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(120, 0));
        var observer = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, new SimVector2(150, 0));
        var attacker = simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PrivateMilitary, EnemyAiMarkers.FirstLanding.HubPosition + new SimVector2(-180, 0));

        TickFor(simulation, 0.2f);
        TickFor(simulation, 0.2f);

        Assert(simulation.EnemyOfficer.CommanderSighted, "enemy officer behavior state records a currently visible Commander");
        Assert(attacker.TargetUnitEntityId == commander.EntityId, "committed enemy attacker prioritizes a visible Commander beyond normal local pursuit");

        observer.ApplyDamage(9999, "debug");
        TickFor(simulation, 0.3f);

        Assert(!simulation.EnemyOfficer.CommanderSighted, "Commander sight behavior state falls off when no enemy currently observes the Commander");
        Assert(attacker.TargetBuildingEntityId == playerHub.EntityId, "enemy attacker falls back to normal building targets after Commander sight is lost");
    }
}
