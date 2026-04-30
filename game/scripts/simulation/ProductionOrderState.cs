namespace Stratezone.Simulation;

public sealed class ProductionOrderState
{
    public ProductionOrderState(string unitId, string factionId, float remainingSeconds)
    {
        UnitId = unitId;
        FactionId = factionId;
        RemainingSeconds = remainingSeconds;
    }

    public string UnitId { get; }
    public string FactionId { get; }
    public float RemainingSeconds { get; internal set; }
}
