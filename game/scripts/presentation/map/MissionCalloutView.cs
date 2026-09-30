using Godot;

public readonly record struct MissionCalloutSnapshot(Vector2 Position, string Text);

public partial class MissionCalloutView : Node2D
{
    private static readonly Font? CalloutFont = GD.Load<Font>("res://assets/fonts/inter_or_similar.tres");
    private readonly List<MissionCalloutSnapshot> _callouts = [];
    private Rect2 _visibleWorldBounds;
    private float _cameraZoom = 1.0f;

    public void UpdateCallouts(IReadOnlyList<MissionCalloutSnapshot> callouts, Rect2 visibleWorldBounds, float cameraZoom)
    {
        _callouts.Clear();
        _callouts.AddRange(callouts);
        _visibleWorldBounds = visibleWorldBounds;
        _cameraZoom = cameraZoom;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (CalloutFont is null || _callouts.Count == 0 || _cameraZoom < 0.65f)
        {
            return;
        }

        var fontSize = Mathf.RoundToInt(UiPalette.FontSizeLabel / Mathf.Max(0.6f, _cameraZoom));
        foreach (var callout in _callouts)
        {
            if (!_visibleWorldBounds.HasPoint(callout.Position))
            {
                continue;
            }

            var textSize = CalloutFont.GetStringSize(callout.Text, HorizontalAlignment.Left, -1, fontSize);
            var origin = callout.Position + new Vector2(12.0f, -18.0f);
            var rect = new Rect2(origin + new Vector2(-6.0f, -fontSize - 4.0f), textSize + new Vector2(12.0f, 8.0f));
            DrawRect(rect, UiPalette.PanelBg with { A = 0.78f });
            DrawRect(rect, UiPalette.PanelOutline, false, 1.0f / Mathf.Max(0.6f, _cameraZoom));
            DrawString(CalloutFont, origin, callout.Text, HorizontalAlignment.Left, -1, fontSize, UiPalette.TextPrimary);
        }
    }
}
