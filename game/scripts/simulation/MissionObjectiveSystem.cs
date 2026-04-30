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
            return new MissionState(MissionStatus.Lost, "Mission lost: Commander killed.", "Commander killed.");
        }

        var remainingEnemyTargets =
            units.Count(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary && !unit.IsDestroyed) +
            buildings.Count(building => building.FactionId == ContentIds.Factions.PrivateMilitary && !building.IsDestroyed);

        var hasEnemyPresence =
            units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary) ||
            buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary);

        if (hasEnemyPresence && remainingEnemyTargets == 0)
        {
            return new MissionState(MissionStatus.Won, "Mission won: enemy force eliminated.", null, 0);
        }

        var text = remainingEnemyTargets > 0
            ? $"Objective: eliminate enemy force. Targets remaining: {remainingEnemyTargets}."
            : "Objective: establish the outpost.";
        return new MissionState(MissionStatus.Active, text, null, remainingEnemyTargets);
    }
}
