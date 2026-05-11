namespace Stratezone.Simulation;

internal sealed class MissionObjectiveSystem
{
    private readonly HashSet<string> _objectiveIds;

    public MissionObjectiveSystem(IEnumerable<string>? objectiveIds = null)
    {
        _objectiveIds = objectiveIds is null
            ? []
            : new HashSet<string>(objectiveIds, StringComparer.Ordinal);
    }

    public MissionState Evaluate(IReadOnlyList<UnitState> units, IReadOnlyList<BuildingState> buildings)
    {
        var protectCommander = HasObjective(ContentIds.Objectives.ProtectCommander) || _objectiveIds.Count == 0;
        if (protectCommander && units.Any(unit =>
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

        var hasEnemyPresence =
            units.Any(unit => unit.FactionId == ContentIds.Factions.PrivateMilitary) ||
            buildings.Any(building => building.FactionId == ContentIds.Factions.PrivateMilitary);

        if (HasObjective(ContentIds.Objectives.DestroyEnemyColonyHub) &&
            hasEnemyPresence &&
            !HasLiveEnemyColonyHub(buildings) &&
            !HasLiveEnemyHubOccupants(units))
        {
            return new MissionState(
                MissionStatus.Won,
                "Mission won: enemy Colony Hub destroyed.",
                null,
                0,
                "mission.won.enemy_colony_hub_destroyed");
        }

        var remainingEnemyTargets =
            units.Count(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                !unit.IsDestroyed) +
            buildings.Count(building => building.FactionId == ContentIds.Factions.PrivateMilitary && !building.IsDestroyed);

        var destroyAllEnemies = HasObjective(ContentIds.Objectives.DestroyAllEnemies) || _objectiveIds.Count == 0;
        if (destroyAllEnemies && hasEnemyPresence && remainingEnemyTargets == 0)
        {
            return new MissionState(
                MissionStatus.Won,
                "Mission won: enemy force eliminated.",
                null,
                0,
                "mission.won.enemy_force_eliminated");
        }

        if (HasObjective(ContentIds.Objectives.DestroyEnemyColonyHub))
        {
            var remainingHubs = HasLiveEnemyColonyHub(buildings) ? 1 : 0;
            var remainingHubOccupants = units.Count(unit =>
                unit.FactionId == ContentIds.Factions.PrivateMilitary &&
                unit.IsColonyHubOccupant &&
                !unit.IsDestroyed);
            return new MissionState(
                MissionStatus.Active,
                "Objective: destroy enemy Colony Hub.",
                null,
                remainingHubs + remainingHubOccupants,
                "mission.objective.destroy_enemy_colony_hub");
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

    private bool HasObjective(string objectiveId)
    {
        return _objectiveIds.Contains(objectiveId);
    }

    private static bool HasLiveEnemyColonyHub(IReadOnlyList<BuildingState> buildings)
    {
        return buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
    }

    private static bool HasLiveEnemyHubOccupants(IReadOnlyList<UnitState> units)
    {
        return units.Any(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            unit.IsColonyHubOccupant &&
            !unit.IsDestroyed);
    }
}
