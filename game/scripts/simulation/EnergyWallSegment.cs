namespace Stratezone.Simulation;

public sealed record EnergyWallSegment(
    int StartAnchorEntityId,
    int EndAnchorEntityId,
    SimVector2 Start,
    SimVector2 End
);
