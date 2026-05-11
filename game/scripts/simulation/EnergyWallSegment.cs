namespace Stratezone.Simulation;

public sealed record EnergyWallSegment(
    int StartAnchorEntityId,
    int EndAnchorEntityId,
    SimVector2 Start,
    SimVector2 End
)
{
    public const float EndCapExtension = 80.0f;
    public const float BlockingClearance = 28.0f;
    public const float PlacementBuffer = 8.0f;

    public SimVector2 ExtendedStart => Start - (Direction * EndCapExtension);
    public SimVector2 ExtendedEnd => End + (Direction * EndCapExtension);

    private SimVector2 Direction => (End - Start).Normalized();
}
