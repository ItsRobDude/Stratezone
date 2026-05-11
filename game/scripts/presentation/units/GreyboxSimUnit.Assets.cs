using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stratezone.Simulation;

public partial class GreyboxSimUnit
{
    private void InitializeDirectionalSprite(string unitId)
    {
        var assetSlug = unitId switch
        {
            ContentIds.Units.Grunt => "grunt",
            ContentIds.Units.Cadet => "cadet",
            ContentIds.Units.Rifleman => "rifleman",
            ContentIds.Units.Guardian => "guardian",
            ContentIds.Units.Commander => "commander",
            ContentIds.Units.Rover => "rover",
            ContentIds.Units.MediumTank => "medium_tank",
            ContentIds.Units.Tank => "tank",
            _ => null
        };

        if (assetSlug is null)
        {
            return;
        }

        _directionalAssetSlug = assetSlug;
        _runAnimationConfig = LoadAnimationConfig(assetSlug, UnitAnimationKind.Run);
        _attackAnimationConfig = LoadAnimationConfig(assetSlug, UnitAnimationKind.Attack);
        var textures = LoadDirectionalTextures(assetSlug);
        if (!textures.TryGetValue(_facingAngle, out var initialTexture))
        {
            GD.PushWarning($"No directional unit sprites found for {unitId}; using greybox placeholder.");
            _directionalAssetSlug = null;
            _runAnimationConfig = null;
            _attackAnimationConfig = null;
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

        var textures = LoadDirectionalAtlasTextures(assetSlug);
        if (textures.Count == DirectionalAngles.Length)
        {
            DirectionalTextureCache[assetSlug] = textures;
            return textures;
        }

        textures = LoadLooseDirectionalTextures(assetSlug);
        DirectionalTextureCache[assetSlug] = textures;
        return textures;
    }

    private static IReadOnlyDictionary<int, Texture2D> LoadDirectionalAtlasTextures(string assetSlug)
    {
        var atlasPath = $"res://assets/units/{assetSlug}/{assetSlug}_directional_atlas.png";
        var atlas = LoadTexture(atlasPath);
        if (atlas is null)
        {
            return new Dictionary<int, Texture2D>();
        }

        var atlasSize = atlas.GetSize();
        var atlasRows = Mathf.CeilToInt(DirectionalAngles.Length / (float)DirectionalAtlasColumns);
        var frameWidth = atlasSize.X / DirectionalAtlasColumns;
        var frameHeight = atlasSize.Y / atlasRows;
        if (frameWidth <= 0.0f || frameHeight <= 0.0f)
        {
            GD.PushWarning($"Directional atlas for {assetSlug} has invalid size {atlasSize}.");
            return new Dictionary<int, Texture2D>();
        }

        var textures = new Dictionary<int, Texture2D>();
        for (var index = 0; index < DirectionalAngles.Length; index++)
        {
            var angle = DirectionalAngles[index];
            var column = index % DirectionalAtlasColumns;
            var row = index / DirectionalAtlasColumns;
            textures[angle] = new AtlasTexture
            {
                Atlas = atlas,
                Region = new Rect2(column * frameWidth, row * frameHeight, frameWidth, frameHeight)
            };
        }

        return textures;
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<Texture2D>> LoadDirectionalAnimationTextures(
        string assetSlug,
        UnitAnimationKind animationKind,
        UnitRunAnimationConfig config)
    {
        var cacheKey = $"{assetSlug}:{animationKind}";
        if (DirectionalAnimationTextureCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var atlas = LoadTexture(config.AtlasPath);
        if (atlas is null)
        {
            var missing = new Dictionary<int, IReadOnlyList<Texture2D>>();
            DirectionalAnimationTextureCache[cacheKey] = missing;
            return missing;
        }

        var atlasSize = atlas.GetSize();
        var frameWidth = atlasSize.X / config.FrameCount;
        var frameHeight = atlasSize.Y / DirectionalAngles.Length;
        if (frameWidth <= 0.0f || frameHeight <= 0.0f)
        {
            GD.PushWarning($"{animationKind} animation atlas for {assetSlug} has invalid size {atlasSize}.");
            var invalid = new Dictionary<int, IReadOnlyList<Texture2D>>();
            DirectionalAnimationTextureCache[cacheKey] = invalid;
            return invalid;
        }

        var textures = new Dictionary<int, IReadOnlyList<Texture2D>>();
        for (var row = 0; row < DirectionalAngles.Length; row++)
        {
            var angle = DirectionalAngles[row];
            var frames = new List<Texture2D>(config.FrameCount);
            for (var column = 0; column < config.FrameCount; column++)
            {
                frames.Add(new AtlasTexture
                {
                    Atlas = atlas,
                    Region = new Rect2(column * frameWidth, row * frameHeight, frameWidth, frameHeight)
                });
            }

            textures[angle] = frames;
        }

        DirectionalAnimationTextureCache[cacheKey] = textures;
        return textures;
    }

    private static IReadOnlyDictionary<int, Texture2D> LoadLooseDirectionalTextures(string assetSlug)
    {
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

    private static UnitRunAnimationConfig? LoadAnimationConfig(string assetSlug, UnitAnimationKind animationKind)
    {
        var cache = animationKind == UnitAnimationKind.Attack
            ? AttackAnimationConfigCache
            : RunAnimationConfigCache;
        if (cache.TryGetValue(assetSlug, out var cached))
        {
            return cached;
        }

        var settings = LoadUnitAnimationSettings();
        if (settings.UnitAnimations is null ||
            !settings.UnitAnimations.TryGetValue(assetSlug, out var unitSettings))
        {
            cache[assetSlug] = null;
            return null;
        }

        var config = animationKind == UnitAnimationKind.Attack
            ? unitSettings.Attack
            : unitSettings.Run;
        if (config is null)
        {
            cache[assetSlug] = null;
            return null;
        }

        if (!config.IsValid())
        {
            GD.PushWarning($"{animationKind} animation settings for {assetSlug} are invalid.");
            cache[assetSlug] = null;
            return null;
        }

        if (!ResourceOrFileExists(config.AtlasPath))
        {
            cache[assetSlug] = null;
            return null;
        }

        cache[assetSlug] = config;
        return config;
    }

    private static UnitAnimationSettingsFile LoadUnitAnimationSettings()
    {
        if (UnitAnimationSettings is not null)
        {
            return UnitAnimationSettings;
        }

        if (!Godot.FileAccess.FileExists(UnitAnimationSettingsPath))
        {
            UnitAnimationSettings = new UnitAnimationSettingsFile();
            return UnitAnimationSettings;
        }

        using var file = Godot.FileAccess.Open(UnitAnimationSettingsPath, Godot.FileAccess.ModeFlags.Read);
        if (file is null)
        {
            UnitAnimationSettings = new UnitAnimationSettingsFile();
            return UnitAnimationSettings;
        }

        try
        {
            UnitAnimationSettings = JsonSerializer.Deserialize<UnitAnimationSettingsFile>(
                file.GetAsText(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new UnitAnimationSettingsFile();
        }
        catch (JsonException exception)
        {
            GD.PushWarning($"Could not parse unit animation settings: {exception.Message}");
            UnitAnimationSettings = new UnitAnimationSettingsFile();
        }

        return UnitAnimationSettings;
    }

    private static bool ResourceOrFileExists(string resourcePath)
    {
        return ResourceLoader.Exists(resourcePath) ||
            Godot.FileAccess.FileExists(resourcePath) ||
            File.Exists(ProjectSettings.GlobalizePath(resourcePath));
    }

    private static Vector2 GetSpriteOriginOffset(Texture2D texture, float spriteScale = DirectionalSpriteScale)
    {
        var size = texture.GetSize() * spriteScale;
        return new Vector2(-size.X * 0.5f, -size.Y * 0.5f);
    }

    private sealed class UnitAnimationSettingsFile
    {
        [JsonPropertyName("unit_animations")]
        public Dictionary<string, UnitAnimationEntry>? UnitAnimations { get; set; }
    }

    private sealed class UnitAnimationEntry
    {
        [JsonPropertyName("run")]
        public UnitRunAnimationConfig? Run { get; set; }

        [JsonPropertyName("attack")]
        public UnitRunAnimationConfig? Attack { get; set; }
    }

    private sealed class UnitRunAnimationConfig
    {
        [JsonPropertyName("atlas_path")]
        public string AtlasPath { get; set; } = string.Empty;

        [JsonPropertyName("frame_count")]
        public int FrameCount { get; set; }

        [JsonPropertyName("frames_per_second")]
        public float FramesPerSecond { get; set; }

        [JsonPropertyName("sprite_scale")]
        public float SpriteScale { get; set; }

        [JsonPropertyName("sustain_loop_start_frame")]
        public int SustainLoopStartFrame { get; set; }

        [JsonPropertyName("movement_grace_seconds")]
        public float MovementGraceSeconds { get; set; }

        [JsonPropertyName("engagement_grace_seconds")]
        public float EngagementGraceSeconds { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(AtlasPath) &&
                FrameCount > 0 &&
                FramesPerSecond > 0.0f &&
                SpriteScale > 0.0f &&
                SustainLoopStartFrame >= 0 &&
                SustainLoopStartFrame <= FrameCount &&
                MovementGraceSeconds >= 0.0f &&
                EngagementGraceSeconds >= 0.0f;
        }
    }

    private enum UnitAnimationKind
    {
        Run,
        Attack
    }
}
