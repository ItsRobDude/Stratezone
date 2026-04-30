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
    int RemainingEnemyTargets = 0,
    string PrimaryTextKey = "",
    IReadOnlyDictionary<string, string>? PrimaryTextArgs = null,
    string? FailureReasonKey = null,
    IReadOnlyDictionary<string, string>? FailureReasonArgs = null
);
