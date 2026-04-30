using Godot;
using Stratezone.Simulation;

public partial class GreyboxSimUnit : Node2D
{
    private UnitState? _state;
    private Label? _label;

    public void Initialize(UnitState state)
    {
        _label = new Label
        {
            Position = new Vector2(-48, -38),
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(96, 24)
        };
        AddChild(_label);
        UpdateFromState(state);
    }

    public void UpdateFromState(UnitState state)
    {
        _state = state;
        Position = new Vector2(state.Position.X, state.Position.Y);

        if (_label is not null)
        {
            var status = state.IsBlockedByEnergyWall ? "BLOCKED " : string.Empty;
            _label.Text = $"{status}{state.Definition.DisplayName} {HealthPercent(state):0}%";
            _label.Modulate = state.IsDestroyed ? new Color(0.55f, 0.55f, 0.55f) : new Color(1.0f, 0.78f, 0.72f);
        }

        Visible = !state.IsDestroyed;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_state is null || _state.IsDestroyed)
        {
            return;
        }

        var fill = new Color(0.82f, 0.18f, 0.16f);
        var outline = _state.IsBlockedByEnergyWall
            ? new Color(0.58f, 0.95f, 1.0f)
            : new Color(0.18f, 0.04f, 0.04f);

        DrawPolygon(
            [
                new Vector2(0, -18),
                new Vector2(16, 0),
                new Vector2(0, 18),
                new Vector2(-16, 0)
            ],
            [fill]);
        DrawPolyline(
            [
                new Vector2(0, -18),
                new Vector2(16, 0),
                new Vector2(0, 18),
                new Vector2(-16, 0),
                new Vector2(0, -18)
            ],
            outline,
            3.0f);

        if (_state.IsBlockedByEnergyWall)
        {
            DrawArc(Vector2.Zero, 24.0f, 0, Mathf.Tau, 48, outline, 2.0f);
        }
    }

    private static float HealthPercent(UnitState state)
    {
        return state.Definition.Health <= 0
            ? 0.0f
            : (state.Health / state.Definition.Health) * 100.0f;
    }
}
