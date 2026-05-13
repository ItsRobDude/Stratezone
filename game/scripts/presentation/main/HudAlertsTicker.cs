using Godot;

public readonly record struct HudAlertSnapshot(string Text, float AgeSeconds);

public partial class HudAlertsTicker : Control
{
    private const float AlertLifetimeSeconds = 4.0f;
    private static readonly Font? AlertFont = GD.Load<Font>("res://assets/fonts/inter_or_similar.tres");
    private IReadOnlyList<HudAlertSnapshot> _alerts = [];
    private float _uiScale = 1.0f;

    public void ApplyUiScale(float uiScale)
    {
        _uiScale = uiScale;
        AnchorLeft = 1.0f;
        AnchorTop = 1.0f;
        AnchorRight = 1.0f;
        AnchorBottom = 1.0f;
        OffsetLeft = -392.0f * uiScale;
        OffsetRight = -16.0f * uiScale;
        OffsetTop = -180.0f * uiScale;
        OffsetBottom = -96.0f * uiScale;
        QueueRedraw();
    }

    public void UpdateAlerts(IReadOnlyList<HudAlertSnapshot> alerts)
    {
        _alerts = alerts.Take(2).ToArray();
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (AlertFont is null || _alerts.Count == 0)
        {
            return;
        }

        var fontSize = Mathf.RoundToInt(UiPalette.FontSizeBody * _uiScale);
        var lineHeight = 32.0f * _uiScale;
        for (var index = 0; index < _alerts.Count; index++)
        {
            var alert = _alerts[index];
            var alpha = Mathf.Clamp(1.0f - MathF.Max(0.0f, alert.AgeSeconds - 2.6f) / (AlertLifetimeSeconds - 2.6f), 0.0f, 1.0f);
            var rect = new Rect2(new Vector2(0.0f, index * lineHeight), new Vector2(Size.X, 28.0f * _uiScale));
            DrawRect(rect, UiPalette.PanelBg with { A = 0.88f * alpha });
            DrawRect(rect, UiPalette.PanelOutline with { A = alpha }, false, Mathf.Max(1.0f, _uiScale));
            DrawString(AlertFont, rect.Position + new Vector2(10.0f, 20.0f) * _uiScale, alert.Text, HorizontalAlignment.Left, rect.Size.X - (20.0f * _uiScale), fontSize, UiPalette.TextPrimary with { A = alpha });
        }
    }
}
