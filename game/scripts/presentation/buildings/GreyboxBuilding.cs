using System.Collections.Generic;
using System.IO;
using Godot;
using Stratezone.Localization;
using Stratezone.Simulation;

public partial class GreyboxBuilding : Node2D
{
    private const float DirectionalSpriteScale = 0.5f;
    private const int DirectionalSpriteAngle = 180;
    private const int DirectionalAtlasColumns = 4;

    private static readonly int[] DirectionalAngles = [0, 45, 90, 135, 180, 225, 270, 315];
    private static readonly Dictionary<string, Texture2D> DirectionalTextureCache = [];

    private BuildingState? _state;
    private Sprite2D? _directionalSprite;
    private Label? _label;
    private LocalizationCatalog? _localization;
    private bool _selected;

    public void Initialize(BuildingState state, LocalizationCatalog? localization = null)
    {
        _localization = localization;
        InitializeDirectionalSprite(state.Definition.Id);
        _label = new Label
        {
            Position = new Vector2(-54, -72),
            HorizontalAlignment = HorizontalAlignment.Center,
            Size = new Vector2(108, 24),
            ZIndex = 20
        };
        AddChild(_label);
        UpdateFromState(state);
    }

    public void UpdateFromState(BuildingState state)
    {
        _state = state;
        Position = ToGodot(state.Position);

        if (_label is not null)
        {
            var powerPrefix = ShouldShowPoweredBolt(state) ? "⚡ " : string.Empty;
            var factionPrefix = state.FactionId == ContentIds.Factions.PrivateMilitary ? "E " : string.Empty;
            _label.Text = $"{factionPrefix}{powerPrefix}{ShortName(state)}{HealthSuffix(state)}";
            _label.Modulate = state.IsDestroyed
                ? new Color(0.62f, 0.62f, 0.62f)
                : state.IsPowered ? Colors.White : new Color(1.0f, 0.45f, 0.35f);
            UpdateLabelPosition();
        }

        if (_directionalSprite is not null)
        {
            _directionalSprite.Visible = !state.IsDestroyed;
            _directionalSprite.Modulate = GetSpriteModulate(state);
        }

        QueueRedraw();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_state is null || _state.IsDestroyed)
        {
            return;
        }

        var radius = _state.FootprintWorldRadius;
        var fill = GetBodyFill(_state);
        var outline = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.18f, 0.04f, 0.04f)
            : new Color(0.05f, 0.12f, 0.16f);
        DrawFootprintOutline(radius, _state);
        if (_directionalSprite is null)
        {
            DrawBuildingSilhouette(_state, fill, outline);
        }

        if (!_state.IsDestroyed && _state.Definition.ProvidesPower && _state.IsPowered && _state.Definition.PowerRadius > 0)
        {
            DrawArc(Vector2.Zero, RtsSimulation.ToWorldRadius(_state.Definition.PowerRadius), 0, Mathf.Tau, 96, new Color(0.35f, 0.8f, 1.0f, 0.35f), 2.0f);
        }

        if (_state.Definition.WallAnchor)
        {
            DrawCircle(Vector2.Zero, 6.0f, new Color(0.8f, 0.95f, 1.0f));
        }

        DrawIncomingAttackFlash();

        if (_selected)
        {
            DrawArc(Vector2.Zero, radius + 10.0f, 0, Mathf.Tau, 72, new Color(1.0f, 0.95f, 0.25f), 4.0f);
        }
    }

    private void InitializeDirectionalSprite(string buildingId)
    {
        var assetSlug = buildingId switch
        {
            ContentIds.Buildings.Barracks => "barracks",
            ContentIds.Buildings.ColonyHub => "colony_hub",
            ContentIds.Buildings.DefenseTower => "defense_tower",
            ContentIds.Buildings.GunTower => "gun_tower",
            ContentIds.Buildings.PowerPlant => "power_plant",
            ContentIds.Buildings.Pylon => "pylon",
            ContentIds.Buildings.RocketTower => "rocket_tower",
            ContentIds.Buildings.VehicleBay => "vehicle_bay",
            _ => null
        };

        if (assetSlug is null)
        {
            return;
        }

        var texture = LoadDirectionalTexture(assetSlug);
        if (texture is null)
        {
            GD.PushWarning($"No directional building sprite found for {buildingId}; using greybox placeholder.");
            return;
        }

        _directionalSprite = new Sprite2D
        {
            Texture = texture,
            Centered = true,
            Scale = Vector2.One * DirectionalSpriteScale,
            ZIndex = 1
        };
        AddChild(_directionalSprite);
    }

    private static Texture2D? LoadDirectionalTexture(string assetSlug)
    {
        if (DirectionalTextureCache.TryGetValue(assetSlug, out var cached))
        {
            return cached;
        }

        var atlas = LoadTexture($"res://assets/buildings/{assetSlug}/{assetSlug}_directional_atlas.png");
        if (atlas is not null)
        {
            var atlasSize = atlas.GetSize();
            var rows = Mathf.CeilToInt(DirectionalAngles.Length / (float)DirectionalAtlasColumns);
            var frameWidth = atlasSize.X / DirectionalAtlasColumns;
            var frameHeight = atlasSize.Y / rows;
            if (frameWidth > 0 && frameHeight > 0)
            {
                for (var index = 0; index < DirectionalAngles.Length; index++)
                {
                    if (DirectionalAngles[index] != DirectionalSpriteAngle)
                    {
                        continue;
                    }

                    var column = index % DirectionalAtlasColumns;
                    var row = index / DirectionalAtlasColumns;
                    var texture = new AtlasTexture
                    {
                        Atlas = atlas,
                        Region = new Rect2(column * frameWidth, row * frameHeight, frameWidth, frameHeight)
                    };
                    DirectionalTextureCache[assetSlug] = texture;
                    return texture;
                }
            }
        }

        var looseTexture = LoadTexture($"res://assets/buildings/{assetSlug}/directional/{assetSlug}_{DirectionalSpriteAngle:000}.png");
        if (looseTexture is not null)
        {
            DirectionalTextureCache[assetSlug] = looseTexture;
        }

        return looseTexture;
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

    private void UpdateLabelPosition()
    {
        if (_label is null)
        {
            return;
        }

        if (_directionalSprite?.Texture is not null)
        {
            var size = _directionalSprite.Texture.GetSize() * DirectionalSpriteScale;
            _label.Position = new Vector2(-54, -size.Y / 2.0f - 24.0f);
            return;
        }

        _label.Position = new Vector2(-54, -72);
    }

    private static Color GetSpriteModulate(BuildingState state)
    {
        if (state.IsDestroyed)
        {
            return new Color(0.45f, 0.45f, 0.45f, 0.8f);
        }

        if (state.FactionId == ContentIds.Factions.PrivateMilitary)
        {
            return state.IsPowered || !state.Definition.RequiresPower
                ? new Color(1.0f, 0.72f, 0.68f)
                : new Color(0.72f, 0.48f, 0.44f);
        }

        return state.IsPowered || !state.Definition.RequiresPower
            ? Colors.White
            : new Color(0.64f, 0.58f, 0.52f);
    }

    private void DrawIncomingAttackFlash()
    {
        if (_state?.LastIncomingAttackOrigin is null || _state.HitFlashSeconds <= 0.0f)
        {
            return;
        }

        var alpha = Mathf.Clamp(_state.HitFlashSeconds / 0.28f, 0.0f, 1.0f);
        var origin = ToLocal(ToGodot(_state.LastIncomingAttackOrigin.Value));
        var direction = origin.Length() > 0.001f ? origin.Normalized() : new Vector2(-1, 0);
        var edge = direction * (_state.FootprintWorldRadius + 12.0f);
        var impact = direction * MathF.Min(_state.FootprintWorldRadius, 34.0f);
        var color = _state.FactionId == ContentIds.Factions.PrivateMilitary
            ? new Color(0.55f, 0.92f, 1.0f, alpha)
            : new Color(1.0f, 0.55f, 0.28f, alpha);

        DrawLine(edge, impact, color, 5.0f);
        DrawArc(Vector2.Zero, _state.FootprintWorldRadius + 7.0f, 0, Mathf.Tau, 48, new Color(1.0f, 0.92f, 0.36f, alpha), 3.0f);
    }

    private void DrawFootprintOutline(float radius, BuildingState state)
    {
        var color = state.IsPowered || !state.Definition.RequiresPower
            ? new Color(0.55f, 0.9f, 1.0f, 0.34f)
            : new Color(1.0f, 0.38f, 0.24f, 0.42f);
        if (state.FactionId == ContentIds.Factions.PrivateMilitary)
        {
            color = new Color(1.0f, 0.42f, 0.34f, 0.34f);
        }

        var rect = new Rect2(new Vector2(-radius, -radius), new Vector2(radius * 2.0f, radius * 2.0f));
        DrawRect(rect, color, false, 2.0f);
    }

    private void DrawBuildingSilhouette(BuildingState state, Color fill, Color outline)
    {
        switch (state.Definition.Id)
        {
            case ContentIds.Buildings.ColonyHub:
                DrawColonyHub(fill, outline);
                break;
            case ContentIds.Buildings.Barracks:
                DrawBarracks(fill, outline);
                break;
            case ContentIds.Buildings.ArmoryAnnex:
                DrawArmoryAnnex(fill, outline);
                break;
            case ContentIds.Buildings.VehicleBay:
                DrawVehicleBay(fill, outline);
                break;
            case ContentIds.Buildings.PowerPlant:
                DrawPowerPlant(fill, outline);
                break;
            case ContentIds.Buildings.Pylon:
                DrawPylon(fill, outline);
                break;
            case ContentIds.Buildings.ExtractorRefinery:
                DrawExtractor(fill, outline);
                break;
            case ContentIds.Buildings.DefenseTower:
            case ContentIds.Buildings.GunTower:
            case ContentIds.Buildings.RocketTower:
                DrawTower(state.Definition.Id, fill, outline);
                break;
            default:
                DrawGenericBuilding(fill, outline);
                break;
        }
    }

    private void DrawColonyHub(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-42, -30), new Vector2(84, 60)), fill);
        DrawRect(new Rect2(new Vector2(-42, -30), new Vector2(84, 60)), outline, false, 3.0f);
        DrawRect(new Rect2(new Vector2(-22, -43), new Vector2(44, 18)), new Color(0.22f, 0.34f, 0.36f));
        DrawRect(new Rect2(new Vector2(-22, -43), new Vector2(44, 18)), outline, false, 2.0f);
        DrawLine(new Vector2(-28, 0), new Vector2(28, 0), new Color(0.76f, 0.92f, 0.95f), 2.0f);
        DrawLine(new Vector2(0, -22), new Vector2(0, 22), new Color(0.76f, 0.92f, 0.95f), 2.0f);
    }

    private void DrawBarracks(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-38, -23), new Vector2(76, 46)), fill);
        DrawRect(new Rect2(new Vector2(-38, -23), new Vector2(76, 46)), outline, false, 3.0f);
        for (var x = -24; x <= 24; x += 16)
        {
            DrawLine(new Vector2(x, -21), new Vector2(x, 21), new Color(0.78f, 0.84f, 0.78f), 1.5f);
        }

        DrawRect(new Rect2(new Vector2(-11, 6), new Vector2(22, 17)), new Color(0.12f, 0.17f, 0.18f));
    }

    private void DrawArmoryAnnex(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-28, -24), new Vector2(56, 48)), fill);
        DrawRect(new Rect2(new Vector2(-28, -24), new Vector2(56, 48)), outline, false, 3.0f);
        DrawLine(new Vector2(-18, 10), new Vector2(18, -12), new Color(0.95f, 0.82f, 0.36f), 4.0f);
        DrawLine(new Vector2(-18, -12), new Vector2(18, 10), new Color(0.95f, 0.82f, 0.36f), 4.0f);
        DrawCircle(Vector2.Zero, 7.0f, new Color(0.14f, 0.18f, 0.18f));
    }

    private void DrawVehicleBay(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-36, -26), new Vector2(72, 52)), fill);
        DrawRect(new Rect2(new Vector2(-36, -26), new Vector2(72, 52)), outline, false, 3.0f);
        DrawRect(new Rect2(new Vector2(-22, -10), new Vector2(44, 26)), new Color(0.16f, 0.2f, 0.18f));
        DrawLine(new Vector2(-28, 22), new Vector2(28, 22), new Color(0.95f, 0.82f, 0.36f), 4.0f);
        DrawLine(new Vector2(-26, -18), new Vector2(26, -18), new Color(0.74f, 0.86f, 0.9f), 2.0f);
    }

    private void DrawPowerPlant(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-32, -25), new Vector2(64, 50)), fill);
        DrawRect(new Rect2(new Vector2(-32, -25), new Vector2(64, 50)), outline, false, 3.0f);
        DrawCircle(new Vector2(-18, -4), 11.0f, new Color(0.22f, 0.34f, 0.36f));
        DrawCircle(new Vector2(18, -4), 11.0f, new Color(0.22f, 0.34f, 0.36f));
        DrawLine(new Vector2(4, -16), new Vector2(-7, 2), new Color(0.55f, 0.92f, 1.0f), 3.0f);
        DrawLine(new Vector2(-7, 2), new Vector2(8, 2), new Color(0.55f, 0.92f, 1.0f), 3.0f);
        DrawLine(new Vector2(8, 2), new Vector2(-4, 18), new Color(0.55f, 0.92f, 1.0f), 3.0f);
    }

    private void DrawPylon(Color fill, Color outline)
    {
        DrawPolygon(
            [new Vector2(0, -28), new Vector2(20, 18), new Vector2(-20, 18)],
            [fill]);
        DrawPolyline(
            [new Vector2(0, -28), new Vector2(20, 18), new Vector2(-20, 18), new Vector2(0, -28)],
            outline,
            3.0f);
        DrawLine(new Vector2(0, -18), new Vector2(0, 17), new Color(0.55f, 0.92f, 1.0f), 2.0f);
        DrawLine(new Vector2(-10, 4), new Vector2(10, 4), new Color(0.55f, 0.92f, 1.0f), 2.0f);
    }

    private void DrawExtractor(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-34, -22), new Vector2(68, 44)), fill);
        DrawRect(new Rect2(new Vector2(-34, -22), new Vector2(68, 44)), outline, false, 3.0f);
        DrawCircle(Vector2.Zero, 15.0f, new Color(0.12f, 0.18f, 0.18f));
        DrawArc(Vector2.Zero, 22.0f, 0, Mathf.Tau, 32, new Color(0.98f, 0.78f, 0.24f), 3.0f);
        DrawLine(new Vector2(-26, -16), new Vector2(-9, -4), new Color(0.98f, 0.78f, 0.24f), 3.0f);
        DrawLine(new Vector2(9, 4), new Vector2(26, 16), new Color(0.98f, 0.78f, 0.24f), 3.0f);
    }

    private void DrawTower(string buildingId, Color fill, Color outline)
    {
        DrawPolygon(
            [new Vector2(0, -34), new Vector2(28, -10), new Vector2(20, 28), new Vector2(-20, 28), new Vector2(-28, -10)],
            [fill]);
        DrawPolyline(
            [new Vector2(0, -34), new Vector2(28, -10), new Vector2(20, 28), new Vector2(-20, 28), new Vector2(-28, -10), new Vector2(0, -34)],
            outline,
            3.0f);
        DrawCircle(Vector2.Zero, 9.0f, new Color(0.16f, 0.2f, 0.2f));

        if (buildingId == ContentIds.Buildings.GunTower)
        {
            DrawLine(Vector2.Zero, new Vector2(30, -8), outline, 5.0f);
        }
        else if (buildingId == ContentIds.Buildings.RocketTower)
        {
            DrawLine(new Vector2(-4, -4), new Vector2(27, -15), new Color(0.95f, 0.82f, 0.36f), 4.0f);
            DrawLine(new Vector2(4, 4), new Vector2(31, 1), new Color(0.95f, 0.82f, 0.36f), 4.0f);
        }
        else
        {
            DrawArc(Vector2.Zero, 18.0f, 0, Mathf.Tau, 36, new Color(0.62f, 0.9f, 1.0f), 2.0f);
        }
    }

    private void DrawGenericBuilding(Color fill, Color outline)
    {
        DrawRect(new Rect2(new Vector2(-30, -24), new Vector2(60, 48)), fill);
        DrawRect(new Rect2(new Vector2(-30, -24), new Vector2(60, 48)), outline, false, 3.0f);
        DrawLine(new Vector2(-20, -10), new Vector2(20, -10), new Color(0.76f, 0.86f, 0.86f), 2.0f);
        DrawLine(new Vector2(-20, 8), new Vector2(20, 8), new Color(0.76f, 0.86f, 0.86f), 2.0f);
    }

    private static Color GetBodyFill(BuildingState state)
    {
        if (state.IsDestroyed)
        {
            return new Color(0.18f, 0.18f, 0.18f, 0.75f);
        }

        if (state.FactionId == ContentIds.Factions.PrivateMilitary)
        {
            return state.IsPowered || !state.Definition.RequiresPower
                ? new Color(0.58f, 0.22f, 0.2f, 0.92f)
                : new Color(0.36f, 0.2f, 0.18f, 0.82f);
        }

        if (!state.Definition.RequiresPower)
        {
            return new Color(0.28f, 0.42f, 0.32f, 0.92f);
        }

        return state.IsPowered
            ? new Color(0.18f, 0.42f, 0.56f, 0.92f)
            : new Color(0.38f, 0.24f, 0.22f, 0.82f);
    }

    private static Vector2 ToGodot(SimVector2 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    private string ShortName(BuildingState state)
    {
        return _localization?.ContentShortName(state.Definition.Id, state.Definition.DisplayName) ??
            state.Definition.DisplayName;
    }

    private static bool ShouldShowPoweredBolt(BuildingState state)
    {
        return state.IsPowered && (state.Definition.RequiresPower || state.Definition.ProvidesPower);
    }

    private string HealthSuffix(BuildingState state)
    {
        if (state.IsDestroyed)
        {
            return _localization?.Translate("ui.building.destroyed_suffix", fallback: " X") ?? " X";
        }

        if (state.Health >= state.Definition.Health)
        {
            return string.Empty;
        }

        return $" {Mathf.CeilToInt((state.Health / state.Definition.Health) * 100.0f)}%";
    }
}
