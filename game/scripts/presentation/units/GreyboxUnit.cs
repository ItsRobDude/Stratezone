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
        var outline = _definition.CanAttack ? new Color(0.05f, 0.12f, 0.16f) : new Color(0.09f, 0.35f, 0.18f);
        DrawUnitSilhouette(outline);

        if (_selected)
        {
            DrawArc(Vector2.Zero, radius + 7.0f, 0, Mathf.Tau, 64, new Color(1.0f, 0.95f, 0.25f), 4.0f);
        }

        if (_hasMoveTarget)
        {
            DrawLine(Vector2.Zero, ToLocal(_targetPosition), new Color(0.75f, 0.95f, 1.0f), 1.5f);
        }
    }

    private void DrawUnitSilhouette(Color outline)
    {
        if (_definition is null)
        {
            return;
        }

        switch (_definition.Id)
        {
            case "unit_worker":
                DrawWorker(outline);
                break;
            case "unit_cadet":
                DrawCadet(outline);
                break;
            case "unit_rifleman":
                DrawRifleman(outline);
                break;
            case "unit_guardian":
                DrawGuardian(outline);
                break;
            case "unit_rover":
                DrawRover(outline);
                break;
            case "unit_commander":
                DrawCommander(outline);
                break;
            default:
                DrawCircle(Vector2.Zero, SelectionRadius, _bodyColor);
                DrawArc(Vector2.Zero, SelectionRadius + 2.0f, 0, Mathf.Tau, 48, outline, 3.0f);
                break;
        }
    }

    private void DrawWorker(Color outline)
    {
        DrawBodyCapsule(new Vector2(0, 1), 9.0f, 14.0f, _bodyColor, outline);
        DrawRect(new Rect2(new Vector2(-6, 1), new Vector2(12, 11)), new Color(0.98f, 0.78f, 0.24f));
        DrawRect(new Rect2(new Vector2(-6, 1), new Vector2(12, 11)), outline, false, 2.0f);
        DrawLine(new Vector2(7, -4), new Vector2(15, -10), new Color(0.95f, 0.9f, 0.55f), 3.0f);
        DrawLine(new Vector2(12, -12), new Vector2(17, -7), new Color(0.95f, 0.9f, 0.55f), 2.0f);
    }

    private void DrawRifleman(Color outline)
    {
        DrawBodyCapsule(new Vector2(-2, 1), 8.0f, 15.0f, _bodyColor, outline);
        DrawLine(new Vector2(5, -4), new Vector2(18, -12), outline, 4.0f);
        DrawLine(new Vector2(8, -1), new Vector2(20, -8), new Color(0.75f, 0.78f, 0.72f), 2.0f);
        DrawCircle(new Vector2(-2, -10), 4.5f, new Color(0.78f, 0.86f, 0.88f));
    }

    private void DrawCadet(Color outline)
    {
        DrawBodyCapsule(new Vector2(0, 1), 7.0f, 14.0f, _bodyColor, outline);
        DrawLine(new Vector2(7, 2), new Vector2(13, 12), outline, 3.0f);
        DrawLine(new Vector2(13, 12), new Vector2(16, 12), new Color(0.75f, 0.78f, 0.72f), 2.0f);
        DrawCircle(new Vector2(0, -9), 4.0f, new Color(0.78f, 0.86f, 0.88f));
    }

    private void DrawGuardian(Color outline)
    {
        DrawBodyCapsule(Vector2.Zero, 11.0f, 16.0f, _bodyColor, outline);
        DrawRect(new Rect2(new Vector2(-13, -5), new Vector2(8, 14)), new Color(0.22f, 0.75f, 1.0f));
        DrawRect(new Rect2(new Vector2(-13, -5), new Vector2(8, 14)), outline, false, 2.0f);
        DrawLine(new Vector2(7, -3), new Vector2(17, -8), new Color(0.62f, 0.9f, 1.0f), 3.0f);
        DrawLine(new Vector2(-8, 9), new Vector2(8, 9), new Color(0.75f, 0.9f, 1.0f), 2.0f);
    }

    private void DrawRover(Color outline)
    {
        var body = new Rect2(new Vector2(-18, -11), new Vector2(36, 22));
        DrawRect(body, _bodyColor);
        DrawRect(body, outline, false, 3.0f);
        DrawRect(new Rect2(new Vector2(-10, -6), new Vector2(15, 12)), new Color(0.16f, 0.2f, 0.18f));
        DrawLine(new Vector2(-15, -15), new Vector2(15, -15), outline, 4.0f);
        DrawLine(new Vector2(-15, 15), new Vector2(15, 15), outline, 4.0f);
        DrawLine(new Vector2(10, -2), new Vector2(20, -2), new Color(0.98f, 0.82f, 0.32f), 3.0f);
    }

    private void DrawCommander(Color outline)
    {
        DrawBodyCapsule(Vector2.Zero, 8.0f, 15.0f, _bodyColor, outline);
        DrawLine(new Vector2(-9, -1), new Vector2(9, -1), new Color(1.0f, 0.9f, 0.35f), 3.0f);
        DrawStar(new Vector2(0, -10), 5.0f, new Color(1.0f, 0.9f, 0.35f));
        DrawLine(new Vector2(5, -4), new Vector2(13, -8), outline, 3.0f);
    }

    private void DrawBodyCapsule(Vector2 center, float halfWidth, float halfHeight, Color fill, Color outline)
    {
        var body = new Rect2(center - new Vector2(halfWidth, halfHeight), new Vector2(halfWidth * 2.0f, halfHeight * 2.0f));
        DrawRect(body, fill);
        DrawRect(body, outline, false, 2.5f);
        DrawCircle(center + new Vector2(0, -halfHeight), halfWidth * 0.7f, fill);
        DrawArc(center + new Vector2(0, -halfHeight), halfWidth * 0.7f, 0, Mathf.Tau, 24, outline, 2.0f);
    }

    private void DrawStar(Vector2 center, float size, Color color)
    {
        DrawLine(center + new Vector2(0, -size), center + new Vector2(0, size), color, 2.0f);
        DrawLine(center + new Vector2(-size, 0), center + new Vector2(size, 0), color, 2.0f);
        DrawLine(center + new Vector2(-size * 0.7f, -size * 0.7f), center + new Vector2(size * 0.7f, size * 0.7f), color, 2.0f);
        DrawLine(center + new Vector2(-size * 0.7f, size * 0.7f), center + new Vector2(size * 0.7f, -size * 0.7f), color, 2.0f);
    }
}
