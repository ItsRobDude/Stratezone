namespace Stratezone.Simulation;

/// <summary>Converts elapsed time to whole simulation steps; never changes their length.</summary>
public sealed class FixedStepClock
{
    public double RemainderSeconds { get; private set; }
    public double DiscardedSeconds { get; private set; }
    public float Alpha => (float)Math.Clamp(RemainderSeconds / RtsSimulation.TickSeconds, 0.0, 1.0);

    public int Advance(double seconds, Func<bool> step, int maxSteps = int.MaxValue)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (maxSteps < 1) throw new ArgumentOutOfRangeException(nameof(maxSteps));
        RemainderSeconds += seconds;
        var due = Math.Floor((RemainderSeconds + 1e-10) / RtsSimulation.TickSeconds);
        var count = (int)Math.Min(due, maxSteps);
        var executed = 0;
        for (; executed < count; executed++)
        {
            if (!step())
            {
                RemainderSeconds = 0;
                return executed;
            }
        }

        // Only the real-time host supplies a cap. Offline Advance consumes all due steps.
        DiscardedSeconds += Math.Max(0, due - count) * RtsSimulation.TickSeconds;
        RemainderSeconds = Math.Max(0, RemainderSeconds - due * RtsSimulation.TickSeconds);
        return executed;
    }

    public void Reset()
    {
        RemainderSeconds = 0;
        DiscardedSeconds = 0;
    }
}
