using Godot;
using Stratezone.Localization;
using Stratezone.Simulation;

public partial class GreyboxSimUnit : Node2D
{
    private const float DirectionalSpriteScale = 0.06f;
    private static readonly int[] DirectionalAngles = [0, 45, 90, 135, 180, 225, 270, 315];
    private static readonly Dictionary<string, IReadOnlyDictionary<int, Texture2D>> DirectionalTextureCache = [];

    private UnitState? _state;
    private Label? _label;
    private LocalizationCatalog? _localization;
    private Sprite2D? _directionalSprite;
    private string? _directionalAssetSlug;
    private bool _selected;
    private bool _useRiflemanPlaceholder;
    private bool _useCadetPlaceholder;
    private bool _useCommanderPlaceholder;
    private int _facingAngle = 180;
    private Vector2? _lastPosition;

    public UnitState State => _state ?? throw new InvalidOperationException("GreyboxSimUnit has not been initialized.");
    public float SelectionRadius { get; private set; } = 22.0f;

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
        UpdateFromState(state);
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        QueueRedraw();
    }

    public void UpdateFromState(UnitState state)
    {
        var nextPosition = new Vector2(state.Position.X, state.Position.Y);
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

    public override void _Draw()
    {
        if (_state is null || _state.IsDestroyed)
        {
            return;
        }

        if (_directionalSprite is not null)
        {
            // Real unit art is drawn by the child Sprite2D; keep overlays and debug cues in this node.
        }
        else if (_useCadetPlaceholder)
        {
            DrawCadetPlaceholder();
        }
        else if (_useCommanderPlaceholder)
        {
            DrawCommanderPlaceholder();
        }
        else if (_useRiflemanPlaceholder)
        {
            DrawRiflemanPlaceholder();
        }
        else
        {
            DrawUnitToken();
        }

        if (_state.IsBlockedByEnergyWall || _state.IsPathBlocked)
        {
            var outline = _state.IsBlockedByEnergyWall
                ? new Color(0.58f, 0.95f, 1.0f)
                : new Color(0.18f, 0.04f, 0.04f);
            DrawArc(Vector2.Zero, 24.0f, 0, Mathf.Tau, 48, outline, 2.0f);
        }

        DrawOutgoingAttackFlash();
        DrawIncomingAttackFlash();

        if (_selected)
        {
            DrawArc(Vector2.Zero, SelectionRadius + 7.0f, 0, Mathf.Tau, 64, new Color(1.0f, 0.95f, 0.25f), 4.0f);
        }

        if (_state.MoveTarget is not null)
        {
            DrawPathDebug();
        }
    }

    private void InitializeDirectionalSprite(string unitId)
    {
        var assetSlug = unitId switch
        {
            ContentIds.Units.Rifleman => "rifleman",
            ContentIds.Units.Guardian => "guardian",
            _ => null
        };

        if (assetSlug is null)
        {
            return;
        }

        _directionalAssetSlug = assetSlug;
        var textures = LoadDirectionalTextures(assetSlug);
        if (!textures.TryGetValue(_facingAngle, out var initialTexture))
        {
            GD.PushWarning($"No directional unit sprites found for {unitId}; using greybox placeholder.");
            _directionalAssetSlug = null;
            return;
        }

        _directionalSprite = new Sprite2D
        {
            Centered = false,
            Texture = initialTexture,
            Scale = Vector2.One * DirectionalSpriteScale,
            Position = GetSpriteOriginOffset(initialTexture)
        };
        AddChild(_directionalSprite);
    }

    private static IReadOnlyDictionary<int, Texture2D> LoadDirectionalTextures(string assetSlug)
    {
        if (DirectionalTextureCache.TryGetValue(assetSlug, out var cached))
        {
            return cached;
        }

        var textures = new Dictionary<int, Texture2D>();
        foreach (var angle in DirectionalAngles)
        {
            var path = $"res://assets/units/{assetSlug}/directional/{assetSlug}_{angle:000}.png";
            var texture = LoadTexture(path);
            if (texture is not null)
            {
                textures[angle] = texture;
            }
        }

        DirectionalTextureCache[assetSlug] = textures;
        return textures;
    }

    private static Texture2D? LoadTexture(string resourcePath)
    {
        if (ResourceLoader.Exists(resourcePath))
        {
            return GD.Load<Texture2D>(resourcePath);
        }

        var filePath = ProjectSettings.GlobalizePath(resourcePath);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var image = Image.LoadFromFile(filePath);
        return image is null || image.IsEmpty()
            ? null
            : ImageTexture.CreateFromImage(image);
    }

    private void UpdateFacing(UnitState state, Vector2 nextPosition)
    {
        var direction = GetFacingDirection(state, nextPosition);
        if (direction is null || direction.Value.LengthSquared() <= 0.001f)
        {
            return;
        }

        var angle = DirectionToCompassAngle(direction.Value);
        if (angle == _facingAngle)
        {
            return;
        }

        _facingAngle = angle;
        UpdateDirectionalTexture();
    }

    private Vector2? GetFacingDirection(UnitState state, Vector2 nextPosition)
    {
        if (state.LastAttackTargetPosition is not null && state.AttackFlashSeconds > 0.0f)
        {
            var target = new Vector2(state.LastAttackTargetPosition.Value.X, state.LastAttackTargetPosition.Value.Y);
            return target - nextPosition;
        }

        if (_lastPosition is not null)
        {
            var movementDelta = nextPosition - _lastPosition.Value;
            if (movementDelta.LengthSquared() > 0.25f)
            {
                return movementDelta;
            }
        }

        if (state.CurrentWaypointIndex < state.PathWaypoints.Count)
        {
            var waypoint = state.PathWaypoints[state.CurrentWaypointIndex];
            return new Vector2(waypoint.X, waypoint.Y) - nextPosition;
        }

        if (state.MoveTarget is not null)
        {
            return new Vector2(state.MoveTarget.Value.X, state.MoveTarget.Value.Y) - nextPosition;
        }

        return null;
    }

    private void UpdateDirectionalTexture()
    {
        if (_directionalSprite is null || _directionalAssetSlug is null)
        {
            return;
        }

        var textures = LoadDirectionalTextures(_directionalAssetSlug);
        if (!textures.TryGetValue(_facingAngle, out var texture))
        {
            return;
        }

        _directionalSprite.Texture = texture;
        _directionalSprite.Position = GetSpriteOriginOffset(texture);
    }

    private static Vector2 GetSpriteOriginOffset(Texture2D texture)
    {
        var size = texture.GetSize() * DirectionalSpriteScale;
        return new Vector2(-size.X * 0.5f, -size.Y * 0.5f);
    }

    private static int DirectionToCompassAngle(Vector2 direction)
    {
        var degrees = Mathf.RadToDeg(Mathf.Atan2(direction.X, -direction.Y));
        if (degrees < 0.0f)
        {
            degrees += 360.0f;
        }

        return Mathf.PosMod(Mathf.RoundToInt(degrees / 45.0f) * 45, 360);
    }

    private void DrawIncomingAttackFlash()
    {
        if (_state?.LastIncomingAttackOrigin is null || _state.HitFlashSeconds <= 0.0f)
        {
            return;
        }

        var alpha = Mathf.Clamp(_state.HitFlashSeconds / 0.28f, 0.0f, 1.0f);
        var origin = ToLocal(new Vector2(_state.LastIncomingAttackOrigin.Value.X, _state.LastIncomingAttackOrigin.Value.Y));
        var direction = origin.Length() > 0.001f ? origin.Normalized() : new Vector2(-1, 0);
        var tracerStart = direction * 46.0f;
        var tracerEnd = direction * 10.0f;
        var color = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.55f, 0.92f, 1.0f, alpha)
            : new Color(1.0f, 0.55f, 0.28f, alpha);

        DrawLine(tracerStart, tracerEnd, color, 4.0f);
        DrawCircle(Vector2.Zero, 18.0f + (8.0f * alpha), new Color(1.0f, 0.92f, 0.36f, 0.24f * alpha));
        DrawArc(Vector2.Zero, 21.0f, 0, Mathf.Tau, 28, new Color(1.0f, 0.92f, 0.36f, alpha), 2.0f);
    }

    private void DrawOutgoingAttackFlash()
    {
        if (_state?.LastAttackTargetPosition is null || _state.AttackFlashSeconds <= 0.0f)
        {
            return;
        }

        var alpha = Mathf.Clamp(_state.AttackFlashSeconds / 0.22f, 0.0f, 1.0f);
        var target = ToLocal(new Vector2(_state.LastAttackTargetPosition.Value.X, _state.LastAttackTargetPosition.Value.Y));
        var direction = target.Length() > 0.001f ? target.Normalized() : new Vector2(1, 0);
        var muzzle = direction * 30.0f;
        var flashEnd = direction * 52.0f;
        var side = new Vector2(-direction.Y, direction.X) * 5.0f;
        var color = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(1.0f, 0.42f, 0.32f, alpha)
            : new Color(0.50f, 0.90f, 1.0f, alpha);

        DrawLine(muzzle, flashEnd, color, 4.0f);
        DrawLine(muzzle + side, flashEnd - side, new Color(1.0f, 0.92f, 0.36f, alpha), 2.0f);
        DrawCircle(flashEnd, 5.0f * alpha, new Color(1.0f, 0.92f, 0.36f, 0.75f * alpha));
    }

    private void DrawUnitToken()
    {
        if (_state is null)
        {
            return;
        }

        var fill = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.82f, 0.18f, 0.16f)
            : new Color(0.28f, 0.62f, 0.95f);
        var outline = _state.IsBlockedByEnergyWall
            ? new Color(0.58f, 0.95f, 1.0f)
            : new Color(0.18f, 0.04f, 0.04f);

        DrawPolygon(
            [
                new Vector2(0, -18),
                new Vector2(16, 0),
                new Vector2(0, 18),
                new Vector2(-16, 0)
            ],
            [fill]);
        DrawPolyline(
            [
                new Vector2(0, -18),
                new Vector2(16, 0),
                new Vector2(0, 18),
                new Vector2(-16, 0),
                new Vector2(0, -18)
            ],
            outline,
            3.0f);
    }

    private void DrawRiflemanPlaceholder()
    {
        if (_state is null)
        {
            return;
        }

        var armor = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.82f, 0.18f, 0.16f)
            : new Color(0.10f, 0.47f, 0.78f);
        var armorLight = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(1.0f, 0.38f, 0.32f)
            : new Color(0.22f, 0.68f, 0.95f);
        var dark = new Color(0.08f, 0.10f, 0.13f);
        var cloth = new Color(0.16f, 0.18f, 0.21f);
        var plate = new Color(0.72f, 0.75f, 0.78f);
        var outline = _state.IsBlockedByEnergyWall
            ? new Color(0.58f, 0.95f, 1.0f)
            : new Color(0.04f, 0.06f, 0.08f);

        DrawEllipse(new Vector2(0, -18), new Vector2(8, 9), armor, outline, 2.0f);
        DrawPolygon(
            [
                new Vector2(-9, -9),
                new Vector2(9, -9),
                new Vector2(13, 8),
                new Vector2(7, 19),
                new Vector2(-7, 19),
                new Vector2(-13, 8)
            ],
            [plate]);
        DrawPolyline(
            [
                new Vector2(-9, -9),
                new Vector2(9, -9),
                new Vector2(13, 8),
                new Vector2(7, 19),
                new Vector2(-7, 19),
                new Vector2(-13, 8),
                new Vector2(-9, -9)
            ],
            outline,
            2.0f);
        DrawRect(new Rect2(-5, -20, 10, 5), new Color(0.28f, 0.82f, 1.0f));
        DrawLine(new Vector2(-4, -16), new Vector2(5, -16), outline, 1.5f);

        DrawLimb(new Vector2(-12, -5), new Vector2(-22, 9), 5.5f, armorLight, outline);
        DrawLimb(new Vector2(12, -5), new Vector2(22, 7), 5.5f, armorLight, outline);
        DrawLimb(new Vector2(-8, 18), new Vector2(-12, 34), 5.5f, cloth, outline);
        DrawLimb(new Vector2(8, 18), new Vector2(12, 34), 5.5f, cloth, outline);
        DrawLimb(new Vector2(-12, 34), new Vector2(-12, 45), 5.0f, armorLight, outline);
        DrawLimb(new Vector2(12, 34), new Vector2(12, 45), 5.0f, armorLight, outline);
        DrawLine(new Vector2(-18, 44), new Vector2(-7, 44), dark, 5.0f);
        DrawLine(new Vector2(7, 44), new Vector2(18, 44), dark, 5.0f);

        DrawLine(new Vector2(-19, 11), new Vector2(24, 2), dark, 5.0f);
        DrawLine(new Vector2(18, 1), new Vector2(32, 1), dark, 3.0f);
        DrawLine(new Vector2(-18, 11), new Vector2(6, 7), new Color(0.35f, 0.38f, 0.42f), 2.0f);
        DrawCircle(new Vector2(-10, 12), 4.0f, plate);
        DrawCircle(new Vector2(15, 5), 4.0f, plate);

        DrawLine(new Vector2(-25, -20), new Vector2(-25, 21), dark, 4.0f);
        DrawLine(new Vector2(-25, -20), new Vector2(-22, -12), new Color(0.40f, 0.44f, 0.48f), 3.0f);
    }

    private void DrawCadetPlaceholder()
    {
        if (_state is null)
        {
            return;
        }

        var armor = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.82f, 0.18f, 0.16f)
            : new Color(0.10f, 0.45f, 0.78f);
        var armorLight = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(1.0f, 0.38f, 0.32f)
            : new Color(0.24f, 0.66f, 0.92f);
        var dark = new Color(0.08f, 0.10f, 0.13f);
        var cloth = new Color(0.15f, 0.16f, 0.19f);
        var plate = new Color(0.74f, 0.76f, 0.78f);
        var outline = _state.IsBlockedByEnergyWall
            ? new Color(0.58f, 0.95f, 1.0f)
            : new Color(0.04f, 0.06f, 0.08f);

        DrawEllipse(new Vector2(0, -17), new Vector2(7, 8), armor, outline, 2.0f);
        DrawRect(new Rect2(-4, -20, 8, 5), new Color(0.30f, 0.78f, 1.0f));
        DrawLine(new Vector2(0, -24), new Vector2(0, -17), new Color(0.95f, 0.72f, 0.22f), 1.5f);
        DrawPolygon(
            [
                new Vector2(-9, -8),
                new Vector2(9, -8),
                new Vector2(12, 7),
                new Vector2(6, 18),
                new Vector2(-6, 18),
                new Vector2(-12, 7)
            ],
            [plate]);
        DrawPolyline(
            [
                new Vector2(-9, -8),
                new Vector2(9, -8),
                new Vector2(12, 7),
                new Vector2(6, 18),
                new Vector2(-6, 18),
                new Vector2(-12, 7),
                new Vector2(-9, -8)
            ],
            outline,
            2.0f);

        DrawLimb(new Vector2(-11, -5), new Vector2(-18, 9), 5.0f, armorLight, outline);
        DrawLimb(new Vector2(11, -5), new Vector2(19, 10), 5.0f, armorLight, outline);
        DrawLimb(new Vector2(-7, 18), new Vector2(-11, 34), 5.0f, cloth, outline);
        DrawLimb(new Vector2(7, 18), new Vector2(11, 34), 5.0f, cloth, outline);
        DrawLimb(new Vector2(-11, 34), new Vector2(-11, 43), 4.5f, armorLight, outline);
        DrawLimb(new Vector2(11, 34), new Vector2(11, 43), 4.5f, armorLight, outline);
        DrawLine(new Vector2(-16, 43), new Vector2(-6, 43), dark, 4.5f);
        DrawLine(new Vector2(6, 43), new Vector2(16, 43), dark, 4.5f);

        DrawLine(new Vector2(19, 10), new Vector2(26, 28), dark, 4.0f);
        DrawLine(new Vector2(24, 27), new Vector2(30, 26), dark, 3.0f);
        DrawCircle(new Vector2(-18, 11), 3.5f, dark);
        DrawCircle(new Vector2(19, 10), 3.5f, plate);
    }

    private void DrawCommanderPlaceholder()
    {
        if (_state is null)
        {
            return;
        }

        var coat = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.62f, 0.10f, 0.10f)
            : new Color(0.12f, 0.30f, 0.54f);
        var armor = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.95f, 0.32f, 0.24f)
            : new Color(0.24f, 0.64f, 0.95f);
        var gold = new Color(0.95f, 0.76f, 0.22f);
        var dark = new Color(0.05f, 0.07f, 0.09f);
        var outline = _state.IsBlockedByEnergyWall
            ? new Color(0.58f, 0.95f, 1.0f)
            : dark;

        DrawPolygon(
            [
                new Vector2(0, -24),
                new Vector2(18, -4),
                new Vector2(12, 24),
                new Vector2(0, 34),
                new Vector2(-12, 24),
                new Vector2(-18, -4)
            ],
            [coat]);
        DrawPolyline(
            [
                new Vector2(0, -24),
                new Vector2(18, -4),
                new Vector2(12, 24),
                new Vector2(0, 34),
                new Vector2(-12, 24),
                new Vector2(-18, -4),
                new Vector2(0, -24)
            ],
            outline,
            2.5f);
        DrawEllipse(new Vector2(0, -18), new Vector2(8, 9), armor, outline, 2.0f);
        DrawRect(new Rect2(-9, -30, 18, 8), dark);
        DrawRect(new Rect2(-7, -32, 14, 4), gold);
        DrawCircle(new Vector2(0, -10), 4.0f, gold);
        DrawLine(new Vector2(-10, -1), new Vector2(10, -1), gold, 2.0f);
        DrawLine(new Vector2(-3, 5), new Vector2(-3, 22), gold, 2.0f);
        DrawLine(new Vector2(3, 5), new Vector2(3, 22), gold, 2.0f);
        DrawLine(new Vector2(10, 3), new Vector2(28, 14), dark, 4.0f);
        DrawLine(new Vector2(25, 14), new Vector2(34, 14), dark, 2.5f);
    }

    private void DrawLimb(Vector2 start, Vector2 end, float width, Color fill, Color outline)
    {
        DrawLine(start, end, outline, width + 2.5f);
        DrawLine(start, end, fill, width);
    }

    private void DrawEllipse(Vector2 center, Vector2 radius, Color fill, Color outline, float outlineWidth)
    {
        DrawSetTransform(center, 0.0f, radius);
        DrawCircle(Vector2.Zero, 1.0f, fill);
        DrawArc(Vector2.Zero, 1.0f, 0, Mathf.Tau, 32, outline, outlineWidth / MathF.Max(radius.X, radius.Y));
        DrawSetTransform(Vector2.Zero, 0.0f, Vector2.One);
    }

    private void DrawPathDebug()
    {
        if (_state is null)
        {
            return;
        }

        var color = _state.IsPathBlocked
            ? new Color(1.0f, 0.35f, 0.28f)
            : new Color(0.75f, 0.95f, 1.0f);
        var previous = Vector2.Zero;
        var drawn = 0;

        for (var index = _state.CurrentWaypointIndex; index < _state.PathWaypoints.Count && drawn < 8; index++, drawn++)
        {
            var waypoint = _state.PathWaypoints[index];
            var localWaypoint = ToLocal(new Vector2(waypoint.X, waypoint.Y));
            DrawLine(previous, localWaypoint, color, 1.5f);
            DrawCircle(localWaypoint, 3.0f, color);
            previous = localWaypoint;
        }

        if (drawn == 0 && _state.MoveTarget is not null)
        {
            DrawLine(previous, ToLocal(new Vector2(_state.MoveTarget.Value.X, _state.MoveTarget.Value.Y)), color, 1.5f);
        }
    }

    private static float HealthPercent(UnitState state)
    {
        return state.Definition.Health <= 0
            ? 0.0f
            : (state.Health / state.Definition.Health) * 100.0f;
    }
}
