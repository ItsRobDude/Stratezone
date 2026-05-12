using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class MissionTriggerSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var runtime = MissionRuntimeFactory.Create(context.Catalog, ContentIds.Missions.WellsAtTheRidge);
        var simulation = runtime.Simulation;
        var hubPosition = runtime.Markers["player_landing_zone"];

        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.ColonyHub, hubPosition).Success, "trigger route deploys Level 2 Colony Hub");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.PowerPlant, hubPosition + new SimVector2(-260, -120)).Success, "trigger route places powered support");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Barracks, hubPosition + new SimVector2(-180, -260)).Success, "trigger route places watched Barracks");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-1040, 270)).Success, "trigger route starts a legal well power chain");
        Assert(simulation.TryPlaceBuilding(ContentIds.Buildings.Pylon, new SimVector2(-610, 245)).Success, "trigger route powers the start well approach");
        var extractor = simulation.TryPlaceBuilding(ContentIds.Buildings.ExtractorRefinery, runtime.Markers["player_start_well"]);
        Assert(extractor.Success, $"trigger route places watched first Extractor ({extractor.MessageKey}: {extractor.Message})");

        TickFor(simulation, 74.0f);
        Assert(!simulation.Units.Any(IsCommittedEnemyCombatUnit), "coalesced Mission 2 trigger does not fire before its grace window");

        TickFor(simulation, 2.0f);
        Assert(simulation.Units.Count(IsCommittedEnemyCombatUnit) == 1, "Barracks and Extractor trigger coalesce into one small pressure commitment");
    }

    private static bool IsCommittedEnemyCombatUnit(UnitState unit)
    {
        return unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.Definition.CanAttack &&
            !unit.IsDestroyed &&
            unit.IsEnemyAttackCommitted;
    }
}
