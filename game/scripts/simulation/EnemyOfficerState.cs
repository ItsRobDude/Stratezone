namespace Stratezone.Simulation;

public sealed class EnemyOfficerState
{
    public string Id { get; } = "enemy_officer_private_military_lieutenant";
    public int AttackGroupsLost { get; internal set; }
    public int PowerStrikesTaken { get; internal set; }
    public int WallBlocksEncountered { get; internal set; }
    public int RetreatsOrdered { get; internal set; }
    public bool CommanderSighted { get; internal set; }
    public bool ScoutDispatched { get; internal set; }
    public float NextAttackAllowedSeconds { get; internal set; }
}
