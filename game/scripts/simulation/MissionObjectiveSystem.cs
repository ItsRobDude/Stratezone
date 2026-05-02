namespace Stratezone.Simulation;

internal sealed class MissionObjectiveSystem
{
    public MissionState Evaluate(IReadOnlyList<UnitState> units, IReadOnlyList<BuildingState> buildings)
    {
        if (units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            unit.Definition.Id == ContentIds.Units.Commander &&
            unit.IsDestroyed))
        {
            return new MissionState(
                MissionStatus.Lost,
                "Mission lost: Commander killed.",
                "Commander killed.",
                0,
                "mission.lost.commander_killed",
                null,
                "mission.failure.commander_killed");
        }

        var remainingEnemyTargets =
            units.Count(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed &&
                IsRequiredEnemyTarget(unit)) +
            buildings.Count(building => building.FactionId == ContentIds.Factions.PrivateMilitary && !building.IsDestroyed);

        var hasEnemyPresence =
            units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary) ||
            buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary);

        if (hasEnemyPresence && remainingEnemyTargets == 0)
        {
            return new MissionState(
                MissionStatus.Won,
                "Mission won: enemy force eliminated.",
                null,
                0,
                "mission.won.enemy_force_eliminated");
        }

        var text = remainingEnemyTargets > 0
            ? $"Objective: eliminate enemy force. Targets remaining: {remainingEnemyTargets}."
            : "Objective: establish the outpost.";
        var key = remainingEnemyTargets > 0
            ? "mission.objective.eliminate_enemy_force"
            : "mission.objective.establish_outpost";
        var args = remainingEnemyTargets > 0
            ? SimulationMessage.Args(("remaining", remainingEnemyTargets))
            : null;
        return new MissionState(MissionStatus.Active, text, null, remainingEnemyTargets, key, args);
    }

    private static bool IsRequiredEnemyTarget(UnitState unit)
    {
        return !unit.Definition.Tags.Contains("level_1_reveal_only", StringComparer.Ordinal);
    }
}
