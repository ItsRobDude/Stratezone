namespace Stratezone.Simulation;

public sealed record EnemyAiMarkers(
    SimVector2 HubPosition,
    SimVector2 PowerPlantPosition,
    SimVector2 BasePylonPosition,
    SimVector2 WallPowerPylonPosition,
    SimVector2 ForwardPylonPosition,
    SimVector2 BarracksPosition,
    SimVector2 ExtractorPosition,
    SimVector2 DefenseTowerPosition,
    SimVector2 RallyPosition,
    IReadOnlyList<SimVector2> PatrolPositions
)
{
    public static EnemyAiMarkers FirstLanding { get; } = new(
        new SimVector2(700, 140),
        new SimVector2(500, -220),
        new SimVector2(300, -190),
        new SimVector2(300, -190),
        new SimVector2(220, 30),
        new SimVector2(650, -90),
        new SimVector2(390, 30),
        new SimVector2(110, -45),
        new SimVector2(300, 80),
        []);

    public static EnemyAiMarkers FromMission(Content.MissionDefinition mission)
    {
        var markers = mission.Markers.ToDictionary(marker => marker.Id, marker => marker.Position, StringComparer.Ordinal);
        var profile = mission.EnemyAiProfile;
        return new EnemyAiMarkers(
            GetMarker(markers, profile.HubMarkerId, FirstLanding.HubPosition),
            GetMarker(markers, profile.PowerPlantMarkerId, FirstLanding.PowerPlantPosition),
            GetMarker(markers, "enemy_base_pylon", FirstLanding.BasePylonPosition),
            GetMarker(markers, "enemy_wall_power_pylon", FirstLanding.WallPowerPylonPosition),
            GetMarker(markers, "enemy_pylon_weak_point", FirstLanding.ForwardPylonPosition),
            GetMarker(markers, profile.BarracksMarkerId, FirstLanding.BarracksPosition),
            GetMarker(markers, profile.ExtractorMarkerId, FirstLanding.ExtractorPosition),
            GetMarker(markers, profile.DefenseTowerMarkerId, FirstLanding.DefenseTowerPosition),
            GetMarker(markers, profile.RallyMarkerId, FirstLanding.RallyPosition),
            ResolvePatrolPositions(markers, profile));
    }

    private static SimVector2 GetMarker(IReadOnlyDictionary<string, SimVector2> markers, string id, SimVector2 fallback)
    {
        return markers.TryGetValue(id, out var position) ? position : fallback;
    }

    private static IReadOnlyList<SimVector2> ResolvePatrolPositions(
        IReadOnlyDictionary<string, SimVector2> markers,
        Content.EnemyAiProfileDefinition profile)
    {
        var rally = GetMarker(markers, profile.RallyMarkerId, FirstLanding.RallyPosition);
        return profile.PatrolMarkerIds
            .Select(markerId => GetMarker(markers, markerId, rally))
            .ToArray();
    }
}
