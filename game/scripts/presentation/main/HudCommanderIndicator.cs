using Godot;

public partial class HudCommanderIndicator : Panel
{
    private readonly Label _name = new();
    private readonly Label _value = new();
    private int _health;
    private int _maxHealth = 1;
    private bool _missing;
    private float _uiScale = 1.0f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        _name.ThemeTypeVariation = "LabelSecondary";
        _name.Text = "CMD";
        AddChild(_name);

        _value.ThemeTypeVariation = "LabelSecondary";
        _value.HorizontalAlignment = HorizontalAlignment.Right;
        AddChild(_value);
        ApplyUiScale(_uiScale);
    }

    public void ApplyUiScale(float uiScale)
    {
        _uiScale = uiScale;
        AnchorLeft = 0.0f;
        AnchorTop = 0.0f;
        AnchorRight = 0.0f;
        AnchorBottom = 0.0f;
        OffsetLeft = 306.0f * uiScale;
        OffsetTop = 16.0f * uiScale;
        OffsetRight = OffsetLeft + (152.0f * uiScale);
        OffsetBottom = OffsetTop + (64.0f * uiScale);
        _name.Position = new Vector2(40.0f, 11.0f) * uiScale;
        _name.Size = new Vector2(46.0f, 18.0f) * uiScale;
        _value.Position = new Vector2(84.0f, 11.0f) * uiScale;
        _value.Size = new Vector2(54.0f, 18.0f) * uiScale;
        QueueRedraw();
    }

    public void UpdateCommander(int health, int maxHealth, bool missing, string tooltip)
    {
        _health = Math.Max(0, health);
        _maxHealth = Math.Max(1, maxHealth);
        _missing = missing;
        _value.Text = missing ? "--" : $"{_health:0}/{_maxHealth:0}";
        TooltipText = tooltip;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var alpha = _missing ? 0.5f : 1.0f;
        var iconCenter = new Vector2(23.0f, 24.0f) * _uiScale;
        DrawCircle(iconCenter + new Vector2(0, -7) * _uiScale, 6.0f * _uiScale, UiPalette.AccentCommand with { A = alpha });
        DrawLine(iconCenter, iconCenter + new Vector2(0, 15) * _uiScale, UiPalette.AccentCommand with { A = alpha }, Mathf.Max(4.0f, 4.0f * _uiScale));
        DrawLine(iconCenter + new Vector2(-8, 8) * _uiScale, iconCenter + new Vector2(8, 8) * _uiScale, UiPalette.AccentCommand with { A = alpha }, Mathf.Max(2.0f, 2.0f * _uiScale));

        var barRect = new Rect2(new Vector2(40.0f, 36.0f) * _uiScale, new Vector2(98.0f, 10.0f) * _uiScale);
        DrawRect(barRect, UiPalette.ButtonPressed);
        var fillWidth = _missing ? 0.0f : barRect.Size.X * Mathf.Clamp(_health / (float)_maxHealth, 0.0f, 1.0f);
        var healthColor = _health <= _maxHealth * 0.35f ? UiPalette.Warning : UiPalette.Success;
        DrawRect(new Rect2(barRect.Position, new Vector2(fillWidth, barRect.Size.Y)), healthColor with { A = alpha });
        DrawRect(barRect, UiPalette.PanelOutline, false, Mathf.Max(1.0f, _uiScale));
    }
}
