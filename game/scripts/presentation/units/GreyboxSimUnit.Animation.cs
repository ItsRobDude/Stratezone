using Godot;
using Stratezone.Simulation;

public partial class GreyboxSimUnit
{
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

        var texture = GetCurrentDirectionalTexture(out var animationConfig);
        if (texture is null)
        {
            return;
        }

        var spriteScale = animationConfig is not null
            ? animationConfig.SpriteScale
            : DirectionalSpriteScale;
        _directionalSprite.Texture = texture;
        _directionalSprite.Scale = Vector2.One * spriteScale;
        _directionalSprite.Position = GetSpriteOriginOffset(texture, spriteScale);
    }

    private Texture2D? GetCurrentDirectionalTexture(out UnitRunAnimationConfig? animationConfig)
    {
        animationConfig = null;
        if (_directionalAssetSlug is null)
        {
            return null;
        }

        if (_isAttacking && _attackAnimationConfig is not null)
        {
            var attackTextures = LoadDirectionalAnimationTextures(
                _directionalAssetSlug,
                UnitAnimationKind.Attack,
                _attackAnimationConfig);
            if (attackTextures.TryGetValue(_facingAngle, out var frames) && frames.Count > 0)
            {
                animationConfig = _attackAnimationConfig;
                return frames[Mathf.PosMod(_attackAnimationFrameIndex, frames.Count)];
            }
        }

        if (_isMoving && _runAnimationConfig is not null)
        {
            var runTextures = LoadDirectionalAnimationTextures(
                _directionalAssetSlug,
                UnitAnimationKind.Run,
                _runAnimationConfig);
            if (runTextures.TryGetValue(_facingAngle, out var frames) && frames.Count > 0)
            {
                animationConfig = _runAnimationConfig;
                return frames[Mathf.PosMod(_runAnimationFrameIndex, frames.Count)];
            }
        }

        var textures = LoadDirectionalTextures(_directionalAssetSlug);
        return textures.TryGetValue(_facingAngle, out var texture) ? texture : null;
    }

    private bool IsMoving(Vector2 nextPosition)
    {
        return _lastPosition is not null &&
            (nextPosition - _lastPosition.Value).LengthSquared() > 0.01f;
    }

    private void UpdateRunAnimationState(Vector2 nextPosition)
    {
        if (_runAnimationConfig is null)
        {
            _isMoving = false;
            _runMovementGraceSeconds = 0.0f;
            return;
        }

        var movedThisTick = IsMoving(nextPosition);
        if (movedThisTick)
        {
            _runMovementGraceSeconds = MathF.Max(
                _runMovementGraceSeconds,
                _runAnimationConfig.MovementGraceSeconds);
        }

        var nextIsMoving = movedThisTick || _runMovementGraceSeconds > 0.0f;
        if (nextIsMoving && !_isMoving)
        {
            _runAnimationSeconds = 0.0f;
            _runAnimationFrameIndex = 0;
        }

        if (_isMoving != nextIsMoving)
        {
            _isMoving = nextIsMoving;
            UpdateDirectionalTexture();
            return;
        }

        _isMoving = nextIsMoving;
    }

    private void UpdateAttackAnimationState(UnitState state)
    {
        if (_attackAnimationConfig is null)
        {
            _isAttacking = false;
            _attackTargetKey = null;
            _attackEngagementGraceSeconds = 0.0f;
            return;
        }

        var targetKey = GetAttackTargetKey(state);
        var hasRecentAttack = state.LastAttackTargetPosition is not null &&
            (state.AttackFlashSeconds > 0.0f || state.AttackCooldownRemaining > 0.0f);
        if (hasRecentAttack)
        {
            _attackEngagementGraceSeconds = MathF.Max(
                _attackEngagementGraceSeconds,
                _attackAnimationConfig.EngagementGraceSeconds);
        }

        var nextIsAttacking = hasRecentAttack || (targetKey is not null && _attackEngagementGraceSeconds > 0.0f);
        var targetChanged = targetKey is not null && targetKey != _attackTargetKey;
        if (nextIsAttacking && (!_isAttacking || targetChanged))
        {
            _attackAnimationSeconds = 0.0f;
            _attackAnimationFrameIndex = 0;
        }

        var changed = _isAttacking != nextIsAttacking || targetChanged;
        _isAttacking = nextIsAttacking;
        _attackTargetKey = nextIsAttacking ? targetKey ?? _attackTargetKey : null;

        if (changed)
        {
            UpdateDirectionalTexture();
        }
    }

    private static string? GetAttackTargetKey(UnitState state)
    {
        if (state.TargetUnitEntityId is not null)
        {
            return $"unit:{state.TargetUnitEntityId.Value}";
        }

        if (state.TargetBuildingEntityId is not null)
        {
            return $"building:{state.TargetBuildingEntityId.Value}";
        }

        return null;
    }

    private static int GetAnimationFrameIndex(UnitRunAnimationConfig config, float animationSeconds)
    {
        var rawFrame = Mathf.FloorToInt(animationSeconds * config.FramesPerSecond);
        var sustainLoopStartIndex = config.SustainLoopStartFrame - 1;
        if (sustainLoopStartIndex > 0 && sustainLoopStartIndex < config.FrameCount && rawFrame >= sustainLoopStartIndex)
        {
            var loopLength = config.FrameCount - sustainLoopStartIndex;
            return sustainLoopStartIndex + Mathf.PosMod(rawFrame - sustainLoopStartIndex, loopLength);
        }

        return Mathf.PosMod(rawFrame, config.FrameCount);
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
}
