using Godot;
using Stratezone.Simulation;

public partial class GreyboxBuilding : Node2D
{
    private BuildingState? _state;
    private Label? _label;

    public void Initialize(BuildingState state)
    {
        _label = new Label
        {
            Position = new Vector2(-42, -12),
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(84, 24)
        };
        AddChild(_label);
        UpdateFromState(state);
    }

    public void UpdateFromState(BuildingState state)
    {
        _state = state;
        Position = ToGodot(state.Position);

        if (_label is not null)
        {
            var powerPrefix = ShouldShowPoweredBolt(state) ? "⚡ " : string.Empty;
            var factionPrefix = state.FactionId == ContentIds.Factions.PrivateMilitary ? "E " : string.Empty;
            _label.Text = $"{factionPrefix}{powerPrefix}{ShortName(state.Definition.DisplayName)}{HealthSuffix(state)}";
            _label.Modulate = state.IsDestroyed
                ? new Color(0.62f, 0.62f, 0.62f)
                : state.IsPowered ? Colors.White : new Color(1.0f, 0.45f, 0.35f);
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_state is null)
        {
            return;
        }

        var radius = _state.FootprintWorldRadius;
        var fill = _state.IsPowered
            ? new Color(0.18f, 0.42f, 0.56f, 0.92f)
            : new Color(0.38f, 0.24f, 0.22f, 0.82f);

        if (_state.IsDestroyed)
        {
            fill = new Color(0.18f, 0.18f, 0.18f, 0.75f);
        }
        else if (_state.FactionId == ContentIds.Factions.PrivateMilitary)
        {
            fill = _state.IsPowered
                ? new Color(0.58f, 0.22f, 0.2f, 0.9f)
                : new Color(0.36f, 0.2f, 0.18f, 0.82f);
        }
        else if (!_state.Definition.RequiresPower)
        {
            fill = new Color(0.28f, 0.42f, 0.32f, 0.92f);
        }

        DrawRect(new Rect2(new Vector2(-radius, -radius), new Vector2(radius * 2.0f, radius * 2.0f)), fill);
        DrawRect(new Rect2(new Vector2(-radius, -radius), new Vector2(radius * 2.0f, radius * 2.0f)), new Color(0.05f, 0.08f, 0.1f), false, 3.0f);

        if (!_state.IsDestroyed && _state.Definition.ProvidesPower && _state.IsPowered && _state.Definition.PowerRadius > 0)
        {
            DrawArc(Vector2.Zero, RtsSimulation.ToWorldRadius(_state.Definition.PowerRadius), 0, Mathf.Tau, 96, new Color(0.35f, 0.8f, 1.0f, 0.35f), 2.0f);
        }

        if (_state.Definition.WallAnchor)
        {
            DrawCircle(Vector2.Zero, 6.0f, new Color(0.8f, 0.95f, 1.0f));
        }
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private static string ShortName(string displayName)
    {
        return displayName
            .Replace("Extractor/Refinery", "Extractor", StringComparison.Ordinal)
            .Replace("Power Plant", "Power", StringComparison.Ordinal)
            .Replace("Defense Tower", "Wall", StringComparison.Ordinal);
    }

    private static bool ShouldShowPoweredBolt(BuildingState state)
    {
        return state.IsPowered && (state.Definition.RequiresPower || state.Definition.ProvidesPower);
    }

    private static string HealthSuffix(BuildingState state)
    {
        if (state.IsDestroyed)
        {
            return " X";
        }

        if (state.Health >= state.Definition.Health)
        {
            return string.Empty;
        }

        return $" {Mathf.CeilToInt((state.Health / state.Definition.Health) * 100.0f)}%";
    }
}
