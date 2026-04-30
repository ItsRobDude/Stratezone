namespace Stratezone.Simulation;

internal sealed record PathResult(
    bool Success,
    string Message,
    IReadOnlyList<SimVector2> Waypoints,
    SimVector2 Destination);
