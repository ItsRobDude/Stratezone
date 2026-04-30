namespace Stratezone.Simulation;

public sealed class ProductionOrderState
{
    public ProductionOrderState(string unitId, string factionId, int producerBuildingEntityId, float remainingSeconds)
    {
        UnitId = unitId;
        FactionId = factionId;
        ProducerBuildingEntityId = producerBuildingEntityId;
        RemainingSeconds = remainingSeconds;
    }

    public string UnitId { get; }
    public string FactionId { get; }
    public int ProducerBuildingEntityId { get; }
    public float RemainingSeconds { get; internal set; }
}
