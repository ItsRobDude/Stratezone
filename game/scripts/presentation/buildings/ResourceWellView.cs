using Godot;
using Stratezone.Simulation;

public partial class ResourceWellView : Node2D
{
    private ResourceWellState? _state;
    private Label? _label;

    public void Initialize(ResourceWellState state)
    {
        _label = new Label
        {
            Position = new Vector2(-58, -12),
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(116, 24)
        };
        AddChild(_label);
        UpdateFromState(state);
    }

    public void UpdateFromState(ResourceWellState state)
    {
        _state = state;
        Position = new Vector2(state.Position.X, state.Position.Y);

        if (_label is not null)
        {
            _label.Text = $"{state.Remaining:0}";
            _label.Modulate = state.IsDepleted ? new Color(0.75f, 0.32f, 0.28f) : new Color(0.95f, 0.9f, 0.5f);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_state is null)
        {
            return;
        }

        var fill = _state.IsDepleted
            ? new Color(0.24f, 0.2f, 0.16f, 0.8f)
            : new Color(0.65f, 0.55f, 0.18f, 0.8f);

        DrawCircle(Vector2.Zero, 28.0f, fill);
        DrawArc(Vector2.Zero, 30.0f, 0, Mathf.Tau, 64, new Color(0.12f, 0.1f, 0.05f), 2.5f);
    }
}
