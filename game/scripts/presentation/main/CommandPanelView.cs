using Godot;

public partial class CommandPanelView : Panel
{
    private readonly Label _title = new();
    private readonly HBoxContainer _buttons = new();
    private readonly Label _hint = new();
    private float _uiScale = 1.0f;
    private string _defaultHint = string.Empty;
    private string _lastActionSignature = string.Empty;

    public override void _Ready()
    {
        _title.Position = new Vector2(12, 8);
        _title.Size = new Vector2(920, 28);
        AddChild(_title);

        _buttons.Position = new Vector2(12, 40);
        _buttons.Size = new Vector2(920, 58);
        AddChild(_buttons);

        _hint.Position = new Vector2(12, 104);
        _hint.Size = new Vector2(920, 32);
        _hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        AddChild(_hint);
    }

    public void ApplyUiScale(float uiScale, int baseFontSize, Vector2 viewportSize)
    {
        _uiScale = uiScale;
        var margin = 16.0f * uiScale;
        var panelHeight = 142.0f * uiScale;
        var panelWidth = Mathf.Clamp(viewportSize.X - (margin * 2.0f), 640.0f * uiScale, 900.0f * uiScale);
        Size = new Vector2(panelWidth, panelHeight);
        Position = new Vector2(margin, Mathf.Max(margin, viewportSize.Y - panelHeight - margin));
        _title.Size = new Vector2(panelWidth - (24.0f * uiScale), 24.0f * uiScale);
        _buttons.Size = new Vector2(panelWidth - (24.0f * uiScale), 58.0f * uiScale);
        _hint.Size = new Vector2(panelWidth - (24.0f * uiScale), 32.0f * uiScale);
        _title.Position = new Vector2(12, 8) * uiScale;
        _buttons.Position = new Vector2(12, 38) * uiScale;
        _hint.Position = new Vector2(12, 104) * uiScale;
        _title.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(baseFontSize * uiScale));
        _hint.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((baseFontSize - 2) * uiScale));

        var buttonWidth = Mathf.Clamp((panelWidth - (34.0f * uiScale)) / 8.0f, 70.0f * uiScale, 92.0f * uiScale);
        var buttonHeight = 54.0f * uiScale;
        foreach (var child in _buttons.GetChildren().OfType<Button>())
        {
            child.CustomMinimumSize = new Vector2(buttonWidth, buttonHeight);
            child.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt((baseFontSize - 4) * uiScale));
        }
    }

    public void UpdateActions(string title, IReadOnlyList<CommandPanelAction> actions, string hint)
    {
        _title.Text = title;
        _defaultHint = hint;
        var signature = BuildActionSignature(title, actions, hint);
        if (signature == _lastActionSignature)
        {
            if (!HasMouseOverButton())
            {
                _hint.Text = hint;
            }

            return;
        }

        _lastActionSignature = signature;
        _hint.Text = hint;

        foreach (var child in _buttons.GetChildren())
        {
            child.QueueFree();
        }

        var buttonWidth = Mathf.Clamp((Size.X - (34.0f * _uiScale)) / 8.0f, 70.0f * _uiScale, 92.0f * _uiScale);
        foreach (var action in actions)
        {
            var button = new Button
            {
                Text = FormatButtonText(action),
                Disabled = false,
                TooltipText = action.Hint,
                CustomMinimumSize = new Vector2(buttonWidth, 54.0f * _uiScale)
            };
            button.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(14 * _uiScale));
            button.MouseEntered += () => _hint.Text = action.Hint;
            button.MouseExited += () => _hint.Text = _defaultHint;
            button.Pressed += action.Execute;
            _buttons.AddChild(button);
        }
    }

    private static string FormatButtonText(CommandPanelAction action)
    {
        var icon = string.IsNullOrWhiteSpace(action.Icon) ? string.Empty : $"{action.Icon}\n";
        var cost = string.IsNullOrWhiteSpace(action.Cost) ? string.Empty : $"\n{action.Cost}";
        return $"{icon}{action.Label}{cost}";
    }

    private bool HasMouseOverButton()
    {
        return _buttons.GetChildren().OfType<Button>().Any(button => button.GetRect().HasPoint(button.GetLocalMousePosition()));
    }

    private static string BuildActionSignature(string title, IReadOnlyList<CommandPanelAction> actions, string hint)
    {
        return string.Join(
            "\u001f",
            actions.Select(action => $"{action.Label}\u001e{action.Hint}\u001e{action.Enabled}\u001e{action.Icon}\u001e{action.Cost}")
                .Prepend(title)
                .Append(hint));
    }
}
