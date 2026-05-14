using System.Globalization;
using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private const float DefaultUiScale = 1.1f;
    private const float MinUiScale = 0.8f;
    private const float MaxUiScale = 3.5f;
    private const float TvUiScale = 3.0f;
    private const float ScaleIndicatorVisibleSeconds = 2.0f;
    private const string UiScaleEnvironmentVariable = "STRATEZONE_UI_SCALE";

    private float _uiScale = DefaultUiScale;
    private float _autoUiScale = DefaultUiScale;
    private bool _uiScaleTvPresetActive;
    private Vector2 _lastViewportSize;
    private Label? _uiScaleIndicator;
    private float _uiScaleIndicatorSecondsRemaining;

    private void InitializeUiScaleDefault()
    {
        _autoUiScale = ResolveDefaultUiScale();
        _uiScale = _autoUiScale;
    }

    private float ResolveDefaultUiScale()
    {
        var overrideValue = System.Environment.GetEnvironmentVariable(UiScaleEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(overrideValue) &&
            float.TryParse(overrideValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return Mathf.Clamp(parsed, MinUiScale, MaxUiScale);
        }

        var screenSize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
        if (screenSize.X >= 3840)
        {
            return 2.0f;
        }

        if (screenSize.X >= 2560)
        {
            return 1.5f;
        }

        if (screenSize.X >= 1920)
        {
            return 1.1f;
        }

        return 1.0f;
    }

    private void SetupUiScaleIndicator()
    {
        if (_uiLayoutRoot is null)
        {
            return;
        }

        _uiScaleIndicator = new Label
        {
            Name = "UiScaleIndicator",
            ThemeTypeVariation = "LabelSecondary",
            HorizontalAlignment = HorizontalAlignment.Right,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Pass,
            Visible = false
        };
        _uiLayoutRoot.AddChild(_uiScaleIndicator);
        RefreshUiScaleIndicator();
    }

    private bool HandleUiScaleHotkey(Key keycode)
    {
        if (keycode == Key.F9)
        {
            _uiScaleTvPresetActive = false;
            SetUiScale(_uiScale - 0.2f);
            return true;
        }

        if (keycode == Key.F10)
        {
            _uiScaleTvPresetActive = false;
            SetUiScale(_uiScale + 0.2f);
            return true;
        }

        if (keycode == Key.F8)
        {
            _uiScaleTvPresetActive = false;
            SetUiScale(_autoUiScale);
            return true;
        }

        if (keycode == Key.F11)
        {
            _uiScaleTvPresetActive = !_uiScaleTvPresetActive;
            SetUiScale(_uiScaleTvPresetActive ? TvUiScale : _autoUiScale);
            return true;
        }

        return false;
    }

    private void SetUiScale(float scale)
    {
        _uiScale = Mathf.Clamp(scale, MinUiScale, MaxUiScale);
        ApplyUiScale();
        RefreshUiScaleIndicator();
        ShowUiScaleIndicator();
        _lastActionMessage = L("ui.action.ui_scale_set", SimulationMessage.Args(("scale", $"{_uiScale:0.0}")));
    }

    private void ApplyUiScale()
    {
        if (_uiLayoutRoot is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        _lastViewportSize = viewportSize;
        _hudResourceBar?.ApplyUiScale(_uiScale);
        _hudCommander?.ApplyUiScale(_uiScale);
        _hudAlertsTicker?.ApplyUiScale(_uiScale);
        if (_hudObjective is not null)
        {
            _hudObjective.AnchorLeft = 0.5f;
            _hudObjective.AnchorTop = 0.0f;
            _hudObjective.AnchorRight = 0.5f;
            _hudObjective.AnchorBottom = 0.0f;
            _hudObjective.OffsetLeft = -360.0f * _uiScale;
            _hudObjective.OffsetRight = 360.0f * _uiScale;
            _hudObjective.OffsetTop = 24.0f * _uiScale;
            _hudObjective.OffsetBottom = 54.0f * _uiScale;
        }

        if (_commandPanel is not null)
        {
            _commandPanel.ApplyUiScale(_uiScale, viewportSize);
        }

        _briefingOverlay?.ApplyUiScale(_uiScale, viewportSize);
        ApplyMissionResultScale();
        ApplyMapEditorPanelScale();
        ApplyUiScaleIndicatorLayout(viewportSize);
    }

    private void ApplyUiScaleIfViewportChanged()
    {
        var viewportSize = GetSafeHudSize();
        if (viewportSize == _lastViewportSize)
        {
            return;
        }

        ApplyUiScale();
    }

    private void ApplyUiScaleIndicatorLayout(Vector2 viewportSize)
    {
        if (_uiScaleIndicator is null)
        {
            return;
        }

        var width = 300.0f * _uiScale;
        var margin = 16.0f * _uiScale;
        _uiScaleIndicator.AnchorLeft = 1.0f;
        _uiScaleIndicator.AnchorTop = 0.0f;
        _uiScaleIndicator.AnchorRight = 1.0f;
        _uiScaleIndicator.AnchorBottom = 0.0f;
        _uiScaleIndicator.OffsetLeft = -Mathf.Min(width, viewportSize.X - (margin * 2.0f)) - margin;
        _uiScaleIndicator.OffsetRight = -margin;
        _uiScaleIndicator.OffsetTop = margin;
        _uiScaleIndicator.OffsetBottom = margin + (24.0f * _uiScale);
    }

    private void RefreshUiScaleIndicator()
    {
        if (_uiScaleIndicator is null)
        {
            return;
        }

        _uiScaleIndicator.Text = L("ui.hud.scale_indicator", SimulationMessage.Args(("percent", $"{Mathf.RoundToInt(_uiScale * 100.0f)}")));
        _uiScaleIndicator.TooltipText = L("ui.tooltip.ui_scale");
    }

    private void ShowUiScaleIndicator()
    {
        if (_uiScaleIndicator is null)
        {
            return;
        }

        _uiScaleIndicatorSecondsRemaining = ScaleIndicatorVisibleSeconds;
        _uiScaleIndicator.Visible = true;
    }

    private void UpdateUiScaleIndicator(float deltaSeconds)
    {
        if (_uiScaleIndicator is null || _uiScaleIndicatorSecondsRemaining <= 0.0f)
        {
            return;
        }

        _uiScaleIndicatorSecondsRemaining = Mathf.Max(0.0f, _uiScaleIndicatorSecondsRemaining - deltaSeconds);
        if (_uiScaleIndicatorSecondsRemaining <= 0.0f)
        {
            _uiScaleIndicator.Visible = false;
        }
    }

    private Vector2 GetSafeHudSize()
    {
        return GetViewport().GetVisibleRect().Size;
    }
}
