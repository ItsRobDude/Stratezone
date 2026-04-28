using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class BuildingState
{
    public BuildingState(int entityId, BuildingDefinition definition, SimVector2 position, string? resourceWellId)
    {
        EntityId = entityId;
        Definition = definition;
        Position = position;
        ResourceWellId = resourceWellId;
    }

    public int EntityId { get; }
    public BuildingDefinition Definition { get; }
    public SimVector2 Position { get; }
    public string? ResourceWellId { get; }
    public bool IsPowered { get; internal set; }

    public float OccupancyRadius => RtsSimulation.ToWorldRadius(Definition.FootprintRadius + Definition.PlacementBuffer);
}
