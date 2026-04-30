namespace Stratezone.Simulation;

internal sealed record EnemyAiMarkers(
    SimVector2 HubPosition,
    SimVector2 PowerPlantPosition,
    SimVector2 BarracksPosition,
    SimVector2 ExtractorPosition,
    SimVector2 DefenseTowerPosition
)
{
    public static EnemyAiMarkers FirstLanding { get; } = new(
        new SimVector2(620, 120),
        new SimVector2(380, 120),
        new SimVector2(500, -80),
        new SimVector2(220, 30),
        new SimVector2(340, -70));
}
