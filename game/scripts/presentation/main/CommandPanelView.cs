using Godot;

public partial class CommandPanelView : Panel
{
    private readonly Label _title = new();
    private readonly HBoxContainer _buttons = new();
    private readonly Label _hint = new();
    private float _uiScale = 1.0f;

    public override void _Ready()
    {
        _title.Position = new Vector2(12, 8);
        _title.Size = new Vector2(620, 28);
        AddChild(_title);

        _buttons.Position = new Vector2(12, 40);
        _buttons.Size = new Vector2(620, 40);
        AddChild(_buttons);

        _hint.Position = new Vector2(12, 82);
        _hint.Size = new Vector2(620, 28);
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_hint);
    }

    public void ApplyUiScale(float uiScale, int baseFontSize)
    {
        _uiScale = uiScale;
        Size = new Vector2(660, 118) * uiScale;
        _title.Size = new Vector2(620, 24) * uiScale;
        _buttons.Size = new Vector2(620, 34) * uiScale;
        _hint.Size = new Vector2(620, 24) * uiScale;
        _title.Position = new Vector2(12, 8) * uiScale;
        _buttons.Position = new Vector2(12, 38) * uiScale;
        _hint.Position = new Vector2(12, 78) * uiScale;
        _title.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(baseFontSize * uiScale));
        _hint.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((baseFontSize - 2) * uiScale));

        foreach (var child in _buttons.GetChildren().OfType<Button>())
        {
            child.CustomMinimumSize = new Vector2(104, 30) * uiScale;
            child.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((baseFontSize - 2) * uiScale));
        }
    }

    public void UpdateActions(string title, IReadOnlyList<CommandPanelAction> actions, string hint)
    {
        _title.Text = title;
        _hint.Text = hint;

        foreach (var child in _buttons.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var action in actions)
        {
            var button = new Button
            {
                Text = action.Label,
                Disabled = !action.Enabled,
                TooltipText = action.Hint,
                CustomMinimumSize = new Vector2(104, 30) * _uiScale
            };
            button.Pressed += action.Execute;
            _buttons.AddChild(button);
        }
    }
}
