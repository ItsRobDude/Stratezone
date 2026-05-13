using Godot;

public partial class MapEditorIconButton : Button
{
    private string _iconId = "select";
    private float _uiScale = 1.0f;
    private bool _active;

    public bool IsActive
    {
        get => _active;
        set
        {
            if (_active == value)
            {
                return;
            }

            _active = value;
            ThemeTypeVariation = value ? "ButtonActive" : string.Empty;
            QueueRedraw();
        }
    }

    public void Configure(string iconId, string tooltip, bool active, Action action)
    {
        _iconId = iconId;
        Text = string.Empty;
        TooltipText = tooltip;
        ToggleMode = false;
        MouseFilter = MouseFilterEnum.Stop;
        Pressed += action;
        IsActive = active;
    }

    public void ApplyUiScale(float uiScale, float size = 48.0f)
    {
        _uiScale = uiScale;
        CustomMinimumSize = new Vector2(size, size) * uiScale;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        var color = _active ? UiPalette.AccentCommand : UiPalette.TextPrimary;
        var center = Size * 0.5f;
        var scale = _uiScale;
        switch (_iconId)
        {
            case "select":
                DrawPolyline([
                    center + new Vector2(-8, -13) * scale,
                    center + new Vector2(11, 1) * scale,
                    center + new Vector2(2, 4) * scale,
                    center + new Vector2(7, 14) * scale
                ], color, Mathf.Max(2.0f, 2.0f * scale));
                break;
            case "marker":
                DrawCircle(center + new Vector2(0, -4) * scale, 7.0f * scale, color);
                DrawLine(center + new Vector2(0, 4) * scale, center + new Vector2(0, 15) * scale, color, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "rect":
                DrawRect(new Rect2(center - new Vector2(13, 10) * scale, new Vector2(26, 20) * scale), color, false, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "circle":
                DrawArc(center, 13.0f * scale, 0, Mathf.Tau, 40, color, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "delete":
                DrawLine(center + new Vector2(-10, -10) * scale, center + new Vector2(10, 10) * scale, UiPalette.Error, Mathf.Max(3.0f, 3.0f * scale));
                DrawLine(center + new Vector2(10, -10) * scale, center + new Vector2(-10, 10) * scale, UiPalette.Error, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "snap":
                DrawArc(center + new Vector2(-5, 0) * scale, 9.0f * scale, Mathf.Pi * 0.5f, Mathf.Pi * 1.5f, 20, color, Mathf.Max(3.0f, 3.0f * scale));
                DrawArc(center + new Vector2(5, 0) * scale, 9.0f * scale, -Mathf.Pi * 0.5f, Mathf.Pi * 0.5f, 20, color, Mathf.Max(3.0f, 3.0f * scale));
                DrawLine(center + new Vector2(-5, -9) * scale, center + new Vector2(5, -9) * scale, color, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "undo":
                DrawArc(center, 12.0f * scale, Mathf.Pi * 0.2f, Mathf.Pi * 1.55f, 28, color, Mathf.Max(3.0f, 3.0f * scale));
                DrawLine(center + new Vector2(-13, -1) * scale, center + new Vector2(-5, -8) * scale, color, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "redo":
                DrawArc(center, 12.0f * scale, -Mathf.Pi * 0.55f, Mathf.Pi * 0.8f, 28, color, Mathf.Max(3.0f, 3.0f * scale));
                DrawLine(center + new Vector2(13, -1) * scale, center + new Vector2(5, -8) * scale, color, Mathf.Max(3.0f, 3.0f * scale));
                break;
            case "duplicate":
                DrawRect(new Rect2(center - new Vector2(8, 5) * scale, new Vector2(16, 14) * scale), color, false, Mathf.Max(2.0f, 2.0f * scale));
                DrawRect(new Rect2(center - new Vector2(3, 11) * scale, new Vector2(16, 14) * scale), color with { A = 0.65f }, false, Mathf.Max(2.0f, 2.0f * scale));
                break;
            default:
                DrawCircle(center, 7.0f * scale, color);
                break;
        }
    }
}
