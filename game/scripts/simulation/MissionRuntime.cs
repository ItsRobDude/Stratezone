using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed record MissionRuntime(
    MissionDefinition Mission,
    MapDefinition Map,
    RtsSimulation Simulation,
    IReadOnlyDictionary<string, SimVector2> Markers,
    IReadOnlyList<(string WellId, SimVector2 Position)> ResourceWellPlacements);

public static class MissionRuntimeFactory
{
    public static MissionRuntime Create(ContentCatalog catalog, string missionId)
    {
        var mission = catalog.GetMission(missionId);
        var map = catalog.GetMap(mission.MapId);
        return Create(catalog, mission, map);
    }

    public static MissionRuntime Create(ContentCatalog catalog, MissionDefinition mission, MapDefinition map)
    {
        var markers = mission.Markers.ToDictionary(marker => marker.Id, marker => marker.Position, StringComparer.Ordinal);
        var wellPlacements = ResolveResourceWellPlacements(mission, markers);
        var startingMaterials = mission.PlayerStartingResources.TryGetValue(ContentIds.Resources.Materials, out var materials)
            ? materials
            : 0;
        var enemyStartingMaterials = mission.EnemyStartingResources.TryGetValue(ContentIds.Resources.Materials, out var enemyMaterials)
            ? enemyMaterials
            : 0;

        var simulation = new RtsSimulation(
            catalog,
            startingMaterials,
            wellPlacements,
            enemyStartingMaterials,
            EnemyAiMarkers.FromMission(mission),
            mission.EnemyAiProfile,
            mission.AvailableUnitIds,
            mission.ObjectiveIds,
            map,
            mission.MissionTriggers,
            mission.MapObjectOverrides);

        foreach (var entity in mission.StartingEntities)
        {
            var position = ResolveMissionPosition(markers, entity.MarkerId, entity.Offset);
            if (entity.ContentId.StartsWith("building_", StringComparison.Ordinal))
            {
                simulation.AddStartingBuilding(entity.ContentId, position, entity.FactionId);
            }
            else if (entity.ContentId.StartsWith("unit_", StringComparison.Ordinal))
            {
                simulation.AddUnit(entity.ContentId, entity.FactionId, position);
            }
        }

        return new MissionRuntime(mission, map, simulation, markers, wellPlacements);
    }

    private static IReadOnlyList<(string WellId, SimVector2 Position)> ResolveResourceWellPlacements(
        MissionDefinition mission,
        IReadOnlyDictionary<string, SimVector2> markers)
    {
        if (mission.ResourceWellPlacements.Count > 0)
        {
            return mission.ResourceWellPlacements
                .Select(placement => (placement.WellId, ResolveMissionPosition(markers, placement.MarkerId, placement.Offset)))
                .ToArray();
        }

        return mission.ResourceWellIds
            .Select((wellId, index) => (wellId, index == 0 ? new SimVector2(-350, 170) : new SimVector2(220, 30)))
            .ToArray();
    }

    private static SimVector2 ResolveMissionPosition(
        IReadOnlyDictionary<string, SimVector2> markers,
        string markerId,
        SimVector2 offset)
    {
        return markers.TryGetValue(markerId, out var marker)
            ? marker + offset
            : offset;
    }
}
