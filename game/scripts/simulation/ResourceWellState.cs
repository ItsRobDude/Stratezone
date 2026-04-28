using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class ResourceWellState
{
    public ResourceWellState(ResourceWellDefinition definition, SimVector2 position)
    {
        Definition = definition;
        Position = position;
        Remaining = definition.Capacity;
    }

    public ResourceWellDefinition Definition { get; }
    public SimVector2 Position { get; }
    public float Remaining { get; internal set; }
    public int? ExtractorEntityId { get; internal set; }
    public bool IsDepleted => Definition.Depletes && Remaining <= 0.001f;
}
