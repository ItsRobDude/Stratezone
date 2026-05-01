namespace Stratezone.Simulation;

public sealed record EnemyAiMarkers(
    SimVector2 HubPosition,
    SimVector2 PowerPlantPosition,
    SimVector2 BarracksPosition,
    SimVector2 ExtractorPosition,
    SimVector2 DefenseTowerPosition,
    SimVector2 RallyPosition
)
{
    public static EnemyAiMarkers FirstLanding { get; } = new(
        new SimVector2(620, 120),
        new SimVector2(380, 120),
        new SimVector2(500, -80),
        new SimVector2(220, 30),
        new SimVector2(340, -70),
        new SimVector2(300, 40));

    public static EnemyAiMarkers FromMission(Content.MissionDefinition mission)
    {
        var markers = mission.Markers.ToDictionary(marker => marker.Id, marker => marker.Position, StringComparer.Ordinal);
        var profile = mission.EnemyAiProfile;
        return new EnemyAiMarkers(
            GetMarker(markers, profile.HubMarkerId, FirstLanding.HubPosition),
            GetMarker(markers, profile.PowerPlantMarkerId, FirstLanding.PowerPlantPosition),
            GetMarker(markers, profile.BarracksMarkerId, FirstLanding.BarracksPosition),
            GetMarker(markers, profile.ExtractorMarkerId, FirstLanding.ExtractorPosition),
            GetMarker(markers, profile.DefenseTowerMarkerId, FirstLanding.DefenseTowerPosition),
            GetMarker(markers, profile.RallyMarkerId, FirstLanding.RallyPosition));
    }

    private static SimVector2 GetMarker(IReadOnlyDictionary<string, SimVector2> markers, string id, SimVector2 fallback)
    {
        return markers.TryGetValue(id, out var position) ? position : fallback;
    }
}
