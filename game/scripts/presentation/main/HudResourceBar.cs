using Godot;

public partial class HudResourceBar : Panel
{
    private readonly HudResourceMetric _materials = new(HudResourceMetricKind.Materials);
    private readonly HudResourceMetric _power = new(HudResourceMetricKind.Power);
    private readonly HudResourceMetric _population = new(HudResourceMetricKind.Population);
    private float _uiScale = 1.0f;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddChild(_materials);
        AddChild(_power);
        AddChild(_population);
    }

    public void ApplyUiScale(float uiScale)
    {
        _uiScale = uiScale;
        AnchorLeft = 0.0f;
        AnchorTop = 0.0f;
        AnchorRight = 0.0f;
        AnchorBottom = 0.0f;
        OffsetLeft = 16.0f * uiScale;
        OffsetTop = 16.0f * uiScale;
        OffsetRight = OffsetLeft + (280.0f * uiScale);
        OffsetBottom = OffsetTop + (64.0f * uiScale);

        var slotSize = new Vector2(82.0f, 44.0f) * uiScale;
        var y = 10.0f * uiScale;
        _materials.Position = new Vector2(10.0f, y) * uiScale;
        _power.Position = new Vector2(98.0f, y) * uiScale;
        _population.Position = new Vector2(186.0f, y) * uiScale;
        foreach (var slot in new[] { _materials, _power, _population })
        {
            slot.Size = slotSize;
            slot.ApplyUiScale(uiScale);
        }
    }

    public void UpdateValues(int materials, int poweredBuildings, int totalBuildings, int playerUnits)
    {
        _materials.UpdateValue($"{materials:0}", "MAT", "Materials available for construction, training, and repair.");
        _power.UpdateValue($"{poweredBuildings:0}/{totalBuildings:0}", "PWR", "Powered player structures over live player structures.");
        _population.UpdateValue($"{playerUnits:0}/--", "POP", "Live player units. No troop cap is enforced yet.");
    }
}

public enum HudResourceMetricKind
{
    Materials,
    Power,
    Population
}

public partial class HudResourceMetric : Control
{
    private readonly HudResourceMetricKind _kind;
    private readonly Label _value = new();
    private readonly Label _label = new();
    private float _uiScale = 1.0f;

    public HudResourceMetric(HudResourceMetricKind kind)
    {
        _kind = kind;
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;
        _value.ThemeTypeVariation = "LabelHudValue";
        _value.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        AddChild(_value);

        _label.ThemeTypeVariation = "LabelSecondary";
        _label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        AddChild(_label);
        ApplyUiScale(_uiScale);
    }

    public void ApplyUiScale(float uiScale)
    {
        _uiScale = uiScale;
        _value.Position = new Vector2(28.0f, 1.0f) * uiScale;
        _value.Size = new Vector2(54.0f, 23.0f) * uiScale;
        _label.Position = new Vector2(29.0f, 26.0f) * uiScale;
        _label.Size = new Vector2(48.0f, 16.0f) * uiScale;
        QueueRedraw();
    }

    public void UpdateValue(string value, string label, string tooltip)
    {
        _value.Text = value;
        _label.Text = label;
        TooltipText = tooltip;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var rect = new Rect2(new Vector2(4.0f, 7.0f) * _uiScale, new Vector2(18.0f, 18.0f) * _uiScale);
        switch (_kind)
        {
            case HudResourceMetricKind.Materials:
                DrawRect(rect, UiPalette.AccentResource with { A = 0.18f });
                DrawRect(rect, UiPalette.AccentResource, false, Mathf.Max(2.0f, 2.0f * _uiScale));
                break;
            case HudResourceMetricKind.Power:
                var a = rect.Position + new Vector2(11, 0) * _uiScale;
                var b = rect.Position + new Vector2(3, 11) * _uiScale;
                var c = rect.Position + new Vector2(10, 11) * _uiScale;
                var d = rect.Position + new Vector2(7, 20) * _uiScale;
                DrawPolyline([a, b, c, d], UiPalette.AccentPower, Mathf.Max(3.0f, 3.0f * _uiScale));
                break;
            case HudResourceMetricKind.Population:
                var center = rect.Position + new Vector2(9, 5) * _uiScale;
                DrawCircle(center, 5.0f * _uiScale, UiPalette.TextPrimary);
                DrawLine(center + new Vector2(0, 6) * _uiScale, center + new Vector2(0, 18) * _uiScale, UiPalette.TextPrimary, Mathf.Max(3.0f, 3.0f * _uiScale));
                DrawLine(center + new Vector2(-7, 12) * _uiScale, center + new Vector2(7, 12) * _uiScale, UiPalette.TextPrimary, Mathf.Max(2.0f, 2.0f * _uiScale));
                break;
        }
    }
}
