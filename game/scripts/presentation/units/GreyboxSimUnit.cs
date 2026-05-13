using Godot;
using Stratezone.Localization;
using Stratezone.Simulation;

public partial class GreyboxSimUnit : Node2D
{
    private const float DirectionalSpriteScale = 0.06f;
    private const int DirectionalAtlasColumns = 4;
    private const string UnitAnimationSettingsPath = "res://assets/units/unit_animation_settings.json";
    private const float UnitLabelZoomThreshold = 0.85f;
    private const float PathDebugZoomThreshold = 0.75f;
    private const float PlaceholderDetailZoomThreshold = 0.72f;
    private const float AttackFlashZoomThreshold = 0.68f;
    private static readonly int[] DirectionalAngles = [0, 45, 90, 135, 180, 225, 270, 315];
    private static readonly Dictionary<string, IReadOnlyDictionary<int, Texture2D>> DirectionalTextureCache = [];
    private static readonly Dictionary<string, IReadOnlyDictionary<int, IReadOnlyList<Texture2D>>> DirectionalAnimationTextureCache = [];
    private static readonly Dictionary<string, UnitRunAnimationConfig?> RunAnimationConfigCache = [];
    private static readonly Dictionary<string, UnitRunAnimationConfig?> AttackAnimationConfigCache = [];
    private static UnitAnimationSettingsFile? UnitAnimationSettings;

    private UnitState? _state;
    private Label? _label;
    private LocalizationCatalog? _localization;
    private Sprite2D? _directionalSprite;
    private string? _directionalAssetSlug;
    private UnitRunAnimationConfig? _runAnimationConfig;
    private UnitRunAnimationConfig? _attackAnimationConfig;
    private bool _selected;
    private bool _useRiflemanPlaceholder;
    private bool _useCadetPlaceholder;
    private bool _useCommanderPlaceholder;
    private int _facingAngle = 180;
    private float _cameraZoom = 1.0f;
    private Vector2? _lastPosition;
    private bool _isMoving;
    private bool _isAttacking;
    private bool _hovered;
    private bool _showAlwaysOnLabel;
    private string? _attackTargetKey;
    private float _runMovementGraceSeconds;
    private float _attackEngagementGraceSeconds;
    private float _runAnimationSeconds;
    private float _attackAnimationSeconds;
    private int _runAnimationFrameIndex;
    private int _attackAnimationFrameIndex;

    public UnitState State => _state ?? throw new InvalidOperationException("GreyboxSimUnit has not been initialized.");
    public float SelectionRadius { get; private set; } = 22.0f;
    public bool ShowAlwaysOnLabel
    {
        get => _showAlwaysOnLabel;
        set
        {
            if (_showAlwaysOnLabel == value)
            {
                return;
            }

            _showAlwaysOnLabel = value;
            ApplyZoomDetailVisibility();
        }
    }

    public void Initialize(UnitState state, LocalizationCatalog? localization = null)
    {
        _localization = localization;
        _useRiflemanPlaceholder = state.Definition.Id == ContentIds.Units.Rifleman;
        _useCadetPlaceholder = state.Definition.Id == ContentIds.Units.Cadet;
        _useCommanderPlaceholder = state.Definition.Id == ContentIds.Units.Commander;
        SelectionRadius = _useCommanderPlaceholder ? 28.0f : 22.0f;
        InitializeDirectionalSprite(state.Definition.Id);
        _label = new Label
        {
            Position = new Vector2(-48, -38),
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(96, 24)
        };
        AddChild(_label);
        ApplyZoomDetailVisibility();
        UpdateFromState(state);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        ApplyZoomDetailVisibility();
        QueueRedraw();
    }

    public void SetCameraZoom(float cameraZoom)
    {
        var nextZoom = Mathf.Max(0.01f, cameraZoom);
        if (Mathf.IsEqualApprox(_cameraZoom, nextZoom))
        {
            return;
        }

        _cameraZoom = nextZoom;
        ApplyZoomDetailVisibility();
        QueueRedraw();
    }

    public void UpdateFromState(UnitState state)
    {
        var nextPosition = new Vector2(state.Position.X, state.Position.Y);
        UpdateRunAnimationState(nextPosition);
        UpdateAttackAnimationState(state);
        UpdateFacing(state, nextPosition);

        _state = state;
        Position = nextPosition;
        _lastPosition = nextPosition;

        if (_label is not null)
        {
            var status = state.IsBlockedByEnergyWall || state.IsPathBlocked
                ? _localization?.Translate("ui.unit.status.blocked_prefix", fallback: "BLOCKED ") ?? "BLOCKED "
                : string.Empty;
            var name = _localization?.ContentShortName(state.Definition.Id, state.Definition.DisplayName) ??
                state.Definition.DisplayName;
            _label.Text = $"{status}{name} {HealthPercent(state):0}%";
            _label.Modulate = state.IsDestroyed
                ? new Color(0.55f, 0.55f, 0.55f)
                : state.FactionId == ContentIds.Factions.PrivateMilitary
                    ? new Color(1.0f, 0.78f, 0.72f)
                    : new Color(0.72f, 0.9f, 1.0f);
        }

        Visible = !state.IsDestroyed;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_directionalSprite is null || _directionalAssetSlug is null)
        {
            return;
        }

        var deltaSeconds = (float)delta;
        if (_runMovementGraceSeconds > 0.0f)
        {
            _runMovementGraceSeconds = MathF.Max(0.0f, _runMovementGraceSeconds - deltaSeconds);
        }

        if (_attackEngagementGraceSeconds > 0.0f)
        {
            _attackEngagementGraceSeconds = MathF.Max(0.0f, _attackEngagementGraceSeconds - deltaSeconds);
        }

        var hovered = GlobalPosition.DistanceTo(GetGlobalMousePosition()) <= SelectionRadius;
        if (hovered != _hovered)
        {
            _hovered = hovered;
            ApplyZoomDetailVisibility();
        }

        if (_isMoving && _runMovementGraceSeconds <= 0.0f && !IsMoving(Position))
        {
            _isMoving = false;
            UpdateDirectionalTexture();
        }

        if (_isAttacking &&
            _attackEngagementGraceSeconds <= 0.0f &&
            _state?.AttackFlashSeconds <= 0.0f &&
            _state?.AttackCooldownRemaining <= 0.0f)
        {
            _isAttacking = false;
            _attackTargetKey = null;
            UpdateDirectionalTexture();
        }

        if (_isAttacking && _attackAnimationConfig is not null)
        {
            _attackAnimationSeconds += deltaSeconds;
            var nextFrameIndex = GetAnimationFrameIndex(_attackAnimationConfig, _attackAnimationSeconds);
            if (nextFrameIndex != _attackAnimationFrameIndex)
            {
                _attackAnimationFrameIndex = nextFrameIndex;
                UpdateDirectionalTexture();
            }

            return;
        }

        if (_isMoving && _runAnimationConfig is not null)
        {
            _runAnimationSeconds += deltaSeconds;
            var nextFrameIndex = GetAnimationFrameIndex(_runAnimationConfig, _runAnimationSeconds);
            if (nextFrameIndex != _runAnimationFrameIndex)
            {
                _runAnimationFrameIndex = nextFrameIndex;
                UpdateDirectionalTexture();
            }
        }
    }
}
