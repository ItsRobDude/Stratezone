namespace Stratezone.Simulation;

public readonly record struct BuildingRenderSnapshot(
    string DefinitionId,
    string FactionId,
    bool IsDestroyed,
    bool IsPowered,
    float HealthRatio,
    float FootprintWorldRadius,
    bool ProvidesPower,
    float PowerRadius,
    bool WallAnchor,
    bool Selected,
    float CameraZoom,
    string LabelText)
{
    public static BuildingRenderSnapshot From(BuildingState state, bool selected, float cameraZoom, string labelText)
    {
        return new BuildingRenderSnapshot(
            state.Definition.Id,
            state.FactionId,
            state.IsDestroyed,
            state.IsPowered,
            state.Definition.Health <= 0 ? 0.0f : state.Health / state.Definition.Health,
            state.FootprintWorldRadius,
            state.Definition.ProvidesPower,
            state.Definition.PowerRadius,
            state.Definition.WallAnchor,
            selected,
            cameraZoom,
            labelText);
    }
}

public readonly record struct UnitRenderSnapshot(
    string DefinitionId,
    string FactionId,
    bool IsDestroyed,
    float HealthRatio,
    bool IsBlockedByEnergyWall,
    bool IsPathBlocked,
    bool HasMoveTarget,
    int FacingAngle,
    bool Selected,
    bool IsMoving,
    bool IsAttacking,
    int RunAnimationFrameIndex,
    int AttackAnimationFrameIndex,
    float CameraZoom,
    string LabelText)
{
    public static UnitRenderSnapshot From(
        UnitState state,
        int facingAngle,
        bool selected,
        bool isMoving,
        bool isAttacking,
        int runAnimationFrameIndex,
        int attackAnimationFrameIndex,
        float cameraZoom,
        string labelText)
    {
        return new UnitRenderSnapshot(
            state.Definition.Id,
            state.FactionId,
            state.IsDestroyed,
            state.HealthRatio,
            state.IsBlockedByEnergyWall,
            state.IsPathBlocked,
            state.MoveTarget is not null,
            facingAngle,
            selected,
            isMoving,
            isAttacking,
            runAnimationFrameIndex,
            attackAnimationFrameIndex,
            cameraZoom,
            labelText);
    }
}
