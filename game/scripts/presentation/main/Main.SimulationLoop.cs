using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private const int MaxSimulationStepsPerFrame = 4;
    private readonly FixedStepClock _frameClock = new();
    private readonly Dictionary<long, Action<CommandOutcome>> _commandCallbacks = [];
    private long? _pendingPlacementCommand;

    private void ProcessSimulation(double delta)
    {
        if (_simulation is null) return;
        if (_mapEditorEnabled || _simulation.MissionState.Status != MissionStatus.Active)
        {
            _frameClock.Reset();
        }
        else
        {
            var discardedBefore = _frameClock.DiscardedSeconds;
            _frameClock.Advance(delta, () =>
            {
                var stepped = _simulation.Step();
                HandleCommandReceipts();
                SyncWorldViews();
                PollPlayerKnowledgeAlerts();
                return stepped;
            }, MaxSimulationStepsPerFrame);
            if (_frameClock.DiscardedSeconds > discardedBefore)
                GD.Print($"Simulation catch-up capped: discarded {_frameClock.DiscardedSeconds - discardedBefore:0.000}s of wall time.");
        }
        HandleCommandReceipts();
        var alpha = _mapEditorEnabled || _simulation.MissionState.Status != MissionStatus.Active ? 1f : _frameClock.Alpha;
        foreach (var view in _simUnitViews.Values) view.RenderInterpolated(alpha);
        RefreshWorldPresentation();
    }

    private void ResetSimulationLoop()
    {
        _frameClock.Reset();
        _commandCallbacks.Clear();
        _pendingPlacementCommand = null;
    }

    private long QueueGameplayCommand(SimulationCommand command, Action<CommandOutcome>? completed = null)
    {
        if (_simulation is null || _mapEditorEnabled) return 0;
        var id = _simulation.EnqueueCommand(command);
        _commandCallbacks[id] = completed ?? ShowCommandOutcome;
        return id;
    }

    private void HandleCommandReceipts()
    {
        if (_simulation is null) return;
        foreach (var receipt in _simulation.DrainCommandReceipts())
            if (_commandCallbacks.Remove(receipt.CommandId, out var completed)) completed(receipt.Outcome);
    }

    private void ShowCommandOutcome(CommandOutcome outcome)
    {
        if (outcome.MessageKey.Length > 0)
            _lastActionMessage = L(outcome.MessageKey, ResolveMessageArgs(outcome.MessageArgs));
        _hudRefreshElapsedSeconds = HudRefreshIntervalSeconds;
    }

    private void QueueUnitOrders(IReadOnlyList<SimulationCommand> commands, string successKey,
        IReadOnlyDictionary<string, string> args, string failureKey)
    {
        if (commands.Count == 0) { _lastActionMessage = L(failureKey); return; }
        var remaining = commands.Count;
        var successes = 0;
        CommandOutcome? failure = null;
        foreach (var command in commands)
            QueueGameplayCommand(command, outcome =>
            {
                if (outcome.Success) successes++;
                else if (outcome.MessageKey.Length > 0) failure = outcome;
                if (--remaining != 0) return;
                var resolved = new Dictionary<string, string>(args) { ["count"] = successes.ToString() };
                if (successes > 0) _lastActionMessage = L(successKey, resolved);
                else if (failure is not null) ShowCommandOutcome(failure);
                else _lastActionMessage = L(failureKey);
                _hudRefreshElapsedSeconds = HudRefreshIntervalSeconds;
            });
    }

    private void RefreshWorldPresentation()
    {
        if (_simulation is null) return;
        var bounds = GetExpandedCameraWorldBounds(OffscreenCullPaddingWorld);
        var zoom = CurrentCameraZoom();
        foreach (var view in _simUnitViews.Values)
        {
            var unit = view.State;
            view.Visible = !unit.IsDestroyed &&
                (unit.FactionId != ContentIds.Factions.PrivateMilitary || _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unit.Position)) &&
                IsCircleInsideWorldBounds(ToSim(view.Position), view.SelectionRadius, bounds);
            view.SetCameraZoom(zoom);
        }
        foreach (var building in _simulation.Buildings)
            if (_buildingViews.TryGetValue(building.EntityId, out var view))
            {
                view.Visible = !building.IsDestroyed &&
                    (building.FactionId != ContentIds.Factions.PrivateMilitary || _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position)) &&
                    IsCircleInsideWorldBounds(building.Position, building.Footprint.BoundingRadius, bounds);
                view.SetCameraZoom(zoom);
            }
        for (var index = 0; index < _resourceWellViews.Count; index++)
        {
            var view = _resourceWellViews[index];
            view.Visible = IsCircleInsideWorldBounds(_simulation.ResourceWells[index].Position, 48f, bounds);
            view.SetCameraZoom(zoom);
        }
        _fogOfWarView?.UpdateFromState(_simulation.PlayerFog, bounds);
        UpdateMissionCallouts(bounds, zoom);
    }
}
