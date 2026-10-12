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
        if (!IsEnemyHubRouteOpen(hub, _buildings))
        {
            // Something else already cut the route (terrain, a broken bridge); do not freeze rebuilding.
            return false;
        }

        var candidate = new BuildingState(-1, _catalog.GetBuilding(buildingId), ContentIds.Factions.PrivateMilitary, position, null);
        return !IsEnemyHubRouteOpen(hub, [.. _buildings, candidate]);
    }

    private bool IsEnemyHubRouteOpen(BuildingState hub, IReadOnlyList<BuildingState> buildings)
    {
        return PathfindingSystem.FindPath(
            hub.Position,
            _enemyAi.RallyPosition,
            buildings,
            GetBlockingEnergyWallsForFaction(ContentIds.Factions.PrivateMilitary),
            _map?.TerrainRegions ?? [],
            _bridges,
            _playableBounds).Success;
    }
}
