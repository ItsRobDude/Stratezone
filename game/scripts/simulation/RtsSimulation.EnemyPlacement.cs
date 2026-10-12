namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    // Buildings are walls, so the AI must not wall its own Hub off from its rally point by
    // accident. Players may trap themselves; this guard is AI-only.
    internal bool TryPlaceEnemyBuilding(string buildingId, SimVector2 position)
    {
        var factionId = ContentIds.Factions.PrivateMilitary;
        if (!ValidatePlacementForFaction(factionId, buildingId, position, EnemyMaterials).IsLegal ||
            WouldWallOffEnemyHub(buildingId, position))
        {
            return false;
        }

        return TryPlaceBuildingForFaction(factionId, buildingId, position).Success;
    }

    internal bool WouldWallOffEnemyHub(string buildingId, SimVector2 position)
    {
        var hub = _buildings.FirstOrDefault(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            !building.IsDestroyed);
        if (hub is null || buildingId == ContentIds.Buildings.ColonyHub)
        {
            return false;
        }

        // Units still spawn inside the Hub until A3, so the route starts at its center.
        var grid = GetNavigationGrid();
        if (!IsEnemyHubRouteOpen(grid, hub))
        {
            // Something else already cut the route (terrain, a broken bridge); do not freeze rebuilding.
            return false;
        }

        var candidate = BuildingFootprint.Place(_catalog.GetBuilding(buildingId).Footprint, position);
        return !IsEnemyHubRouteOpen(grid.WithExtraFootprint(candidate), hub);
    }

    private bool IsEnemyHubRouteOpen(NavigationGrid grid, BuildingState hub)
    {
        return PathfindingSystem.CanReach(grid, hub.Position, _enemyAi.RallyPosition, SmallestUnitRadius, ContentIds.Factions.PrivateMilitary);
    }
}
