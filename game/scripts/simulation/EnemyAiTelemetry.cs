namespace Stratezone.Simulation;

public sealed class EnemyAiTelemetry
{
    public int AttackGroupsLost { get; internal set; }
    public int PowerStrikesTaken { get; internal set; }
    public int WallBlocksEncountered { get; internal set; }
    public int RetreatsOrdered { get; internal set; }
    public int PatrolDispatches { get; internal set; }
    public int PatrolAreasVisited { get; internal set; }
}
