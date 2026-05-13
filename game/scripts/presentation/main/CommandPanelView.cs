using Godot;
using Stratezone.Simulation;

public partial class CommandPanelView : Panel
{
    private const float PanelHeight = 80.0f;
    private const float ButtonSize = 56.0f;
    private const float InnerPadding = 12.0f;

    private readonly HBoxContainer _buttons = new();
    private float _uiScale = 1.0f;
    private string _lastActionSignature = string.Empty;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Pass;

        _buttons.MouseFilter = MouseFilterEnum.Pass;
        AddChild(_buttons);
    }

    public void ApplyUiScale(float uiScale, Vector2 viewportSize)
    {
        _uiScale = uiScale;
        var margin = 16.0f * uiScale;
        var panelHeight = PanelHeight * uiScale;
        AnchorLeft = 0.0f;
        AnchorTop = 1.0f;
        AnchorRight = 1.0f;
        AnchorBottom = 1.0f;
        OffsetLeft = margin;
        OffsetRight = -margin;
        OffsetTop = -panelHeight - margin;
        OffsetBottom = -margin;

        var localWidth = Mathf.Max(640.0f * uiScale, viewportSize.X - (margin * 2.0f));
        var buttonSize = ButtonSize * uiScale;
        _buttons.Position = new Vector2(InnerPadding, 12.0f) * uiScale;
        _buttons.Size = new Vector2(Mathf.Max(0.0f, localWidth - (InnerPadding * 2.0f * uiScale)), buttonSize);

        foreach (var child in _buttons.GetChildren().OfType<CommandIconButton>())
        {
            child.ApplyUiScale(uiScale);
        }
    }

    public void UpdateActions(IReadOnlyList<CommandPanelAction> actions)
    {
        var signature = BuildActionSignature(actions);
        if (signature == _lastActionSignature)
        {
            return;
        }

        _lastActionSignature = signature;

        foreach (var child in _buttons.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var action in actions)
        {
            var button = new CommandIconButton();
            button.Configure(action);
            button.ApplyUiScale(_uiScale);
            button.Pressed += action.Execute;
            _buttons.AddChild(button);
        }
    }

    private static string BuildActionSignature(IReadOnlyList<CommandPanelAction> actions)
    {
        return string.Join(
            "\u001f",
            actions.Select(action => $"{action.Name}\u001e{action.Hotkey}\u001e{action.Tooltip}\u001e{action.Enabled}\u001e{action.IconId}\u001e{action.Cost}\u001e{action.Active}"));
    }
}

public partial class CommandIconButton : Button
{
    private static readonly Font? IconFont = GD.Load<Font>("res://assets/fonts/inter_or_similar.tres");
    private CommandPanelAction? _action;
    private float _uiScale = 1.0f;

    public void Configure(CommandPanelAction action)
    {
        _action = action;
        Text = string.Empty;
        TooltipText = string.IsNullOrWhiteSpace(action.Tooltip)
            ? action.Name
            : $"{action.Name}\n{action.Tooltip}";
        Disabled = !action.Enabled;
        ThemeTypeVariation = action.Active ? "ButtonActive" : string.Empty;
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
    }

    public void ApplyUiScale(float uiScale)
    {
        _uiScale = uiScale;
        CustomMinimumSize = new Vector2(56.0f, 56.0f) * uiScale;
        QueueRedraw();
    }

    public override void _Draw()
    {
        base._Draw();
        if (_action is null)
        {
            return;
        }

        var alpha = Disabled ? 0.45f : 1.0f;
        var iconColor = IconColor(_action.IconId, alpha);
        DrawCommandIcon(_action.IconId, new Rect2(new Vector2(14, 12) * _uiScale, new Vector2(28, 26) * _uiScale), iconColor);
        DrawHotkeyBadge(_action.Hotkey, alpha);
        DrawCost(_action.Cost, alpha);
    }

    private void DrawHotkeyBadge(string hotkey, float alpha)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return;
        }

        var rect = new Rect2(new Vector2(4, 4) * _uiScale, new Vector2(16, 16) * _uiScale);
        DrawRect(rect, UiPalette.PanelHeader with { A = 0.95f * alpha });
        DrawRect(rect, UiPalette.PanelOutline with { A = alpha }, false, Mathf.Max(1.0f, _uiScale));
        DrawText(hotkey, rect.Position + new Vector2(4.0f, 12.0f) * _uiScale, UiPalette.TextPrimary with { A = alpha }, Mathf.RoundToInt(UiPalette.FontSizeLabel * _uiScale));
    }

    private void DrawCost(string cost, float alpha)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return;
        }

        var fontSize = Mathf.RoundToInt(UiPalette.FontSizeLabel * _uiScale);
        var textSize = IconFont?.GetStringSize(cost, HorizontalAlignment.Left, -1, fontSize) ?? Vector2.Zero;
        var position = new Vector2(Size.X - textSize.X - (5.0f * _uiScale), Size.Y - (6.0f * _uiScale));
        DrawText(cost, position, UiPalette.TextSecondary with { A = alpha }, fontSize);
    }

    private static Color IconColor(string iconId, float alpha)
    {
        var color = iconId switch
        {
            ContentIds.Buildings.PowerPlant or ContentIds.Buildings.Pylon => UiPalette.AccentPower,
            ContentIds.Buildings.ExtractorRefinery => UiPalette.AccentResource,
            ContentIds.Buildings.DefenseTower or ContentIds.Buildings.GunTower or ContentIds.Buildings.RocketTower => UiPalette.AccentWall,
            ContentIds.Units.Commander => UiPalette.AccentCommand,
            _ => UiPalette.TextPrimary
        };
        color.A *= alpha;
        return color;
    }

    private void DrawCommandIcon(string iconId, Rect2 rect, Color color)
    {
        if (iconId.StartsWith("building_", StringComparison.Ordinal))
        {
            DrawRect(new Rect2(rect.Position + new Vector2(3, 7) * _uiScale, new Vector2(22, 16) * _uiScale), color with { A = color.A * 0.25f });
            DrawRect(new Rect2(rect.Position + new Vector2(3, 7) * _uiScale, new Vector2(22, 16) * _uiScale), color, false, Mathf.Max(2.0f, 2.0f * _uiScale));
            if (iconId == ContentIds.Buildings.Pylon)
            {
                DrawLine(rect.Position + new Vector2(14, 2) * _uiScale, rect.Position + new Vector2(14, 25) * _uiScale, color, Mathf.Max(2.0f, 2.0f * _uiScale));
                DrawLine(rect.Position + new Vector2(7, 12) * _uiScale, rect.Position + new Vector2(21, 12) * _uiScale, color, Mathf.Max(2.0f, 2.0f * _uiScale));
            }
            else if (iconId.Contains("tower", StringComparison.Ordinal))
            {
                DrawLine(rect.Position + new Vector2(7, 23) * _uiScale, rect.Position + new Vector2(21, 23) * _uiScale, color, Mathf.Max(2.0f, 2.0f * _uiScale));
                DrawCircle(rect.Position + new Vector2(14, 8) * _uiScale, 5.0f * _uiScale, color);
            }

            return;
        }

        var center = rect.Position + new Vector2(14, 12) * _uiScale;
        DrawCircle(center + new Vector2(0, -4) * _uiScale, 5.0f * _uiScale, color);
        DrawLine(center + new Vector2(0, 2) * _uiScale, center + new Vector2(0, 15) * _uiScale, color, Mathf.Max(3.0f, 3.0f * _uiScale));
        DrawLine(center + new Vector2(-8, 8) * _uiScale, center + new Vector2(8, 8) * _uiScale, color, Mathf.Max(2.0f, 2.0f * _uiScale));
        if (iconId == ContentIds.Units.Rover)
        {
            DrawRect(new Rect2(rect.Position + new Vector2(4, 10) * _uiScale, new Vector2(22, 12) * _uiScale), color with { A = color.A * 0.25f });
            DrawRect(new Rect2(rect.Position + new Vector2(4, 10) * _uiScale, new Vector2(22, 12) * _uiScale), color, false, Mathf.Max(2.0f, 2.0f * _uiScale));
        }
        else if (iconId == ContentIds.Units.Guardian)
        {
            DrawArc(center + new Vector2(0, 6) * _uiScale, 12.0f * _uiScale, -0.1f, Mathf.Pi + 0.1f, 20, color, Mathf.Max(2.0f, 2.0f * _uiScale));
        }
        else if (iconId == ContentIds.Units.Rifleman)
        {
            DrawLine(center + new Vector2(6, 2) * _uiScale, center + new Vector2(15, -5) * _uiScale, color, Mathf.Max(2.0f, 2.0f * _uiScale));
        }
    }

    private void DrawText(string text, Vector2 position, Color color, int fontSize)
    {
        if (IconFont is null)
        {
            return;
        }

        DrawString(IconFont, position, text, HorizontalAlignment.Left, -1, fontSize, color);
    }
}
