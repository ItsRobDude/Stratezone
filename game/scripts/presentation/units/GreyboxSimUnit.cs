using Godot;
using Stratezone.Simulation;

public partial class GreyboxSimUnit : Node2D
{
    private UnitState? _state;
    private Label? _label;
    private bool _selected;

    public UnitState State => _state ?? throw new InvalidOperationException("GreyboxSimUnit has not been initialized.");
    public float SelectionRadius { get; private set; } = 22.0f;

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

    public void SetSelected(bool selected)
    {
        _selected = selected;
        QueueRedraw();
    }

    public void UpdateFromState(UnitState state)
    {
        _state = state;
        Position = new Vector2(state.Position.X, state.Position.Y);

        if (_label is not null)
        {
            var status = state.IsBlockedByEnergyWall ? "BLOCKED " : string.Empty;
            _label.Text = $"{status}{state.Definition.DisplayName} {HealthPercent(state):0}%";
            _label.Modulate = state.IsDestroyed
                ? new Color(0.55f, 0.55f, 0.55f)
                : state.FactionId == ContentIds.Factions.PrivateMilitary
                    ? new Color(1.0f, 0.78f, 0.72f)
                    : new Color(0.72f, 0.9f, 1.0f);
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

        var fill = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.82f, 0.18f, 0.16f)
            : new Color(0.28f, 0.62f, 0.95f);
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

        if (_selected)
        {
            DrawArc(Vector2.Zero, SelectionRadius + 7.0f, 0, Mathf.Tau, 64, new Color(1.0f, 0.95f, 0.25f), 4.0f);
        }

        if (_state.MoveTarget is not null)
        {
            DrawLine(Vector2.Zero, ToLocal(new Vector2(_state.MoveTarget.Value.X, _state.MoveTarget.Value.Y)), new Color(0.75f, 0.95f, 1.0f), 1.5f);
        }
    }

    private static float HealthPercent(UnitState state)
    {
        return state.Definition.Health <= 0
            ? 0.0f
            : (state.Health / state.Definition.Health) * 100.0f;
    }
}
