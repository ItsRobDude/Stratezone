using Godot;
using Stratezone.Simulation.Content;

public partial class GreyboxUnit : Node2D
{
    private const float ArrivalDistance = 4.0f;

    private UnitDefinition? _definition;
    private Vector2 _targetPosition;
    private bool _hasMoveTarget;
    private bool _selected;
    private Color _bodyColor = new(0.28f, 0.62f, 0.95f);

    public UnitDefinition Definition => _definition ?? throw new InvalidOperationException("GreyboxUnit has not been initialized.");

    public float SelectionRadius { get; private set; } = 18.0f;

    public void Initialize(UnitDefinition definition, Vector2 startPosition, Color bodyColor)
    {
        _definition = definition;
        Position = startPosition;
        _targetPosition = startPosition;
        _bodyColor = bodyColor;
        SelectionRadius = definition.Id == "unit_rover" ? 22.0f : 18.0f;
        QueueRedraw();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        QueueRedraw();
    }

    public void SetMoveTarget(Vector2 target)
    {
        _targetPosition = target;
        _hasMoveTarget = true;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!_hasMoveTarget || _definition is null)
        {
            return;
        }

        var toTarget = _targetPosition - Position;
        if (toTarget.Length() <= ArrivalDistance)
        {
            Position = _targetPosition;
            _hasMoveTarget = false;
            QueueRedraw();
            return;
        }

        var pixelsPerSecond = _definition.MovementSpeed * 95.0f;
        Position += toTarget.Normalized() * pixelsPerSecond * (float)delta;
    }

    public override void _Draw()
    {
        if (_definition is null)
        {
            return;
        }

        var radius = SelectionRadius;
        DrawCircle(Vector2.Zero, radius, _bodyColor);

        var outline = _definition.CanAttack ? new Color(0.05f, 0.12f, 0.16f) : new Color(0.09f, 0.35f, 0.18f);
        DrawArc(Vector2.Zero, radius + 2.0f, 0, Mathf.Tau, 48, outline, 3.0f);

        if (_definition.CanConstruct || _definition.CanRepair)
        {
            DrawRect(new Rect2(new Vector2(-6, -6), new Vector2(12, 12)), new Color(0.98f, 0.78f, 0.24f));
        }

        if (_definition.CanRunOverInfantry)
        {
            DrawLine(new Vector2(-10, 7), new Vector2(10, 7), new Color(0.12f, 0.12f, 0.12f), 4.0f);
        }

        if (_selected)
        {
            DrawArc(Vector2.Zero, radius + 7.0f, 0, Mathf.Tau, 64, new Color(1.0f, 0.95f, 0.25f), 4.0f);
        }

        if (_hasMoveTarget)
        {
            DrawLine(Vector2.Zero, ToLocal(_targetPosition), new Color(0.75f, 0.95f, 1.0f), 1.5f);
        }
    }
}

