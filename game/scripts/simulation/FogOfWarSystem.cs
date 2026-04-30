namespace Stratezone.Simulation;

internal static class FogOfWarSystem
{
    private const float BuildingSightFallback = 7.0f;

    public static void Recompute(
        FogOfWarState playerFog,
        FogOfWarState enemyFog,
        IEnumerable<BuildingState> buildings,
        IEnumerable<UnitState> units)
    {
        playerFog.BeginVisibilityUpdate();
        enemyFog.BeginVisibilityUpdate();

        foreach (var building in buildings.Where(building => !building.IsDestroyed))
        {
            GetFogForFaction(playerFog, enemyFog, building.FactionId).Reveal(building.Position, GetBuildingSightRadius(building));
        }

        foreach (var unit in units.Where(unit => !unit.IsDestroyed))
        {
            GetFogForFaction(playerFog, enemyFog, unit.FactionId).Reveal(unit.Position, RtsSimulation.ToWorldRadius(unit.Definition.SightRange));
        }
    }

    private static FogOfWarState GetFogForFaction(FogOfWarState playerFog, FogOfWarState enemyFog, string factionId)
    {
        return factionId == ContentIds.Factions.PrivateMilitary ? enemyFog : playerFog;
    }

    private static float GetBuildingSightRadius(BuildingState building)
    {
        var sightRange = building.Definition.SightRange > 0.0f
            ? building.Definition.SightRange
            : BuildingSightFallback + building.Definition.FootprintRadius;
        return RtsSimulation.ToWorldRadius(sightRange);
    }
}
