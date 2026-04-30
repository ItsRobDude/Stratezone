namespace Stratezone.Simulation;

public enum MissionStatus
{
    Active,
    Won,
    Lost
}

public sealed record MissionState(
    MissionStatus Status,
    string PrimaryText,
    string? FailureReason = null,
    int RemainingEnemyTargets = 0
);
