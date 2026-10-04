using Stratezone.Simulation;

namespace Stratezone.Presentation;

public readonly record struct UnitPose(SimVector2 Position, float FacingDegrees);

/// <summary>Value copies of consecutive step poses, never references to mutable unit state.</summary>
public sealed class UnitPoseInterpolation
{
    public UnitPose Previous { get; private set; }
    public UnitPose Current { get; private set; }
    private bool _initialized;

    public void Reset(UnitPose pose)
    {
        Previous = Current = pose;
        _initialized = true;
    }

    public void Push(UnitPose pose)
    {
        if (!_initialized) { Reset(pose); return; }
        Previous = Current;
        Current = pose;
    }

    public UnitPose Sample(float alpha)
    {
        alpha = Math.Clamp(alpha, 0, 1);
        var turn = ((Current.FacingDegrees - Previous.FacingDegrees) % 360 + 540) % 360 - 180;
        return new(Previous.Position + (Current.Position - Previous.Position) * alpha,
            Previous.FacingDegrees + turn * alpha);
    }
}
