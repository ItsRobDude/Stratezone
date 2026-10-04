using Stratezone.Presentation;
using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class FixedStepSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateClock();
        ValidateFrameSchedules(context);
        ValidateCommands(context);
        ValidatePlacementCancellation(context);
        ValidateMovementBudget(context);
        ValidateCornerCrush(context);
        ValidateInterpolation();
    }

    private static void ValidateClock()
    {
        var clock = new FixedStepClock();
        var steps = 0;
        bool Step() { steps++; return true; }
        clock.Advance(0.049, Step);
        Assert(steps == 0, "substep time does not execute partial gameplay");
        clock.Advance(0.001, Step);
        Assert(steps == 1 && clock.Alpha < 0.00001f, "fractional time carries to the next whole step");
        clock.Advance(0.525, Step, maxSteps: 4);
        Assert(steps == 5 && Math.Abs(clock.DiscardedSeconds - 0.3) < 1e-9 && Math.Abs(clock.Alpha - 0.5) < 1e-5,
            "hitch cap executes four steps, discards excess whole wall-time steps and preserves the fraction");
        clock.Reset();
        Assert(clock.Alpha == 0 && clock.DiscardedSeconds == 0, "reset drops all timing history");
        clock.Advance(1000, Step);
        Assert(steps == 20005 && clock.Alpha == 0, "offline clock does not apply the frame catch-up cap");
        clock.Advance(0.02, Step);
        clock.Advance(0.1, () => false);
        Assert(clock.Alpha == 0, "terminal simulation drops leftover time");
        foreach (var invalid in new[] { -1d, double.NaN, double.PositiveInfinity })
        {
            var rejected = false;
            try { clock.Advance(invalid, Step); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected, "invalid elapsed time is rejected");
        }
    }

    private static RtsSimulation Scenario(SmokeTestContext context)
    {
        var sim = new RtsSimulation(context.Catalog, 3000, [("well_first_landing_start", new SimVector2(-350, 170))]);
        sim.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(-300, -140));
        sim.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new(-170, 40));
        sim.AddStartingBuilding(ContentIds.Buildings.Pylon, new(-220, 160));
        sim.AddStartingBuilding(ContentIds.Buildings.ExtractorRefinery, new(-350, 170));
        var barracks = sim.AddStartingBuilding(ContentIds.Buildings.Barracks, new(-350, 80));
        var rifle = sim.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new(0, 0));
        var target = sim.AddStartingBuilding(ContentIds.Buildings.Barracks, new(70, 0), ContentIds.Factions.PrivateMilitary);
        sim.EnqueueCommand(new AttackBuildingCommand(rifle.EntityId, target.EntityId));
        var rover = sim.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new(-400, -250));
        sim.EnqueueCommand(new MoveUnitCommand(rover.EntityId, new(400, -250)));
        sim.EnqueueCommand(new TrainUnitCommand(ContentIds.Units.Cadet, barracks.EntityId));
        return sim;
    }

    private static string Fingerprint(RtsSimulation sim) => string.Join("|",
        sim.StepIndex, sim.ElapsedSeconds, sim.Materials, sim.EnemyMaterials, sim.MissionState,
        string.Join(";", sim.Units.Select(u => $"{u.EntityId}:{u.Position}:{u.Health:R}:{u.AttackCooldownRemaining:R}:{u.MoveTarget}:{u.TargetBuildingEntityId}")),
        string.Join(";", sim.Buildings.Select(b => $"{b.EntityId}:{b.Health:R}:{b.IsPowered}")),
        string.Join(";", sim.ProductionOrders.Select(o => $"{o.UnitId}:{o.RemainingSeconds:R}")),
        string.Join(";", sim.ResourceWells.Select(w => w.Remaining.ToString("R"))),
        string.Join(";", sim.Events.Select(e => e.MessageKey)), sim.PlayerFog.ExploredRevision);

    private static void ValidateFrameSchedules(SmokeTestContext context)
    {
        var exact = Scenario(context);
        for (var i = 0; i < 20; i++) exact.Step();
        var advanced = Scenario(context);
        advanced.Advance(1.0);
        Assert(Fingerprint(exact) == Fingerprint(advanced), "twenty Step calls equal Advance(1.0), including combat/economy/production/fog");

        string? expected = null;
        foreach (var fps in new[] { 30, 40, 60, 120, 144, 240, 0 })
        {
            var sim = Scenario(context);
            var clock = new FixedStepClock();
            var time = 0d;
            var frame = 0;
            var irregular = new[] { 0.011, 0.078, 0.004, 0.12, 0.033 };
            while (time < 3 - 1e-10)
            {
                var delta = Math.Min(3 - time, fps == 0 ? irregular[frame % irregular.Length] : 1d / fps);
                clock.Advance(delta, () =>
                {
                    if (sim.StepIndex == 10)
                    {
                        var rover = sim.Units.Single(u => u.Definition.Id == ContentIds.Units.Rover);
                        sim.EnqueueCommand(new MoveUnitCommand(rover.EntityId, new(300, -200)));
                    }
                    return sim.Step();
                }, maxSteps: 4);
                frame++;
                time += delta;
            }
            Assert(sim.StepIndex == 60 && sim.ElapsedSeconds == 3 && clock.DiscardedSeconds == 0,
                $"{fps} FPS schedule runs the exact number of fixed steps");
            var actual = Fingerprint(sim);
            expected ??= actual;
            Assert(actual == expected, $"{fps} FPS schedule produces identical gameplay for commands on the same steps");
        }
    }

    private static void ValidateCommands(SmokeTestContext context)
    {
        var cost = context.Catalog.GetUnit(ContentIds.Units.Cadet).Cost;
        var sim = new RtsSimulation(context.Catalog, cost * 2, []);
        sim.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(-300, 0));
        sim.AddStartingBuilding(ContentIds.Buildings.PowerPlant, new(-100, 0));
        var barracks = sim.AddStartingBuilding(ContentIds.Buildings.Barracks, new(0, 0));
        var cadet = sim.AddUnit(ContentIds.Units.Cadet, ContentIds.Factions.PlayerExpedition, new(400, 0));
        sim.EnqueueCommand(new MoveUnitCommand(cadet.EntityId, new(500, 0)));
        sim.EnqueueCommand(new MoveUnitCommand(cadet.EntityId, new(600, 0)));
        for (var i = 0; i < 3; i++) sim.EnqueueCommand(new TrainUnitCommand(ContentIds.Units.Cadet, barracks.EntityId));
        Assert(cadet.MoveTarget is null && sim.Materials == cost * 2 && sim.ProductionOrders.Count == 0,
            "enqueuing commands does not mutate gameplay before the step");
        sim.Step();
        var receipts = sim.DrainCommandReceipts();
        Assert(cadet.MoveTarget == new SimVector2(600, 0), "commands execute in FIFO order");
        Assert(sim.Materials == 0 && sim.ProductionOrders.Count == 2 && receipts.Count(r => r.Outcome.Success) == 4,
            "spending is revalidated between queued commands and cannot overspend");
        Assert(sim.DrainCommandReceipts().Count == 0, "command receipts are delivered once");

        sim.EnqueueCommand(new MoveUnitCommand(cadet.EntityId, new(700, 0)));
        cadet.ApplyDamage(9999, "debug");
        sim.Step();
        Assert(!sim.DrainCommandReceipts().Single().Outcome.Success, "dead command subject is rejected at execution");
        var commander = sim.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new(400, 0));
        sim.EnqueueCommand(new KillCommanderCommand());
        sim.EnqueueCommand(new MoveUnitCommand(commander.EntityId, new(600, 0)));
        sim.Step();
        var ended = sim.DrainCommandReceipts();
        Assert(ended[0].Outcome.Success && !ended[1].Outcome.Success, "mission termination cancels subsequent commands");
        var ticks = sim.StepIndex;
        sim.Advance(1000);
        Assert(sim.StepIndex == ticks, "terminal mission cannot accrue gameplay time");

        var reset = Scenario(context);
        reset.CancelPendingCommands();
        Assert(reset.DrainCommandReceipts().All(r => !r.Outcome.Success), "editor transition cancels pending commands");
        reset.Step();
        Assert(reset.Units.All(u => u.MoveTarget is null), "cancelled commands do not leak into resumed simulation");
    }

    private static void ValidatePlacementCancellation(SmokeTestContext context)
    {
        var sim = new RtsSimulation(context.Catalog, 1000, []);
        sim.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new(-300, -140));
        var grunt = sim.AddUnit(ContentIds.Units.Grunt, ContentIds.Factions.PlayerExpedition, new(-400, 0));
        var buildingId = ContentIds.Buildings.PowerPlant;
        var oldSite = new SimVector2(-170, 40);
        var newSite = new SimVector2(-170, 80);
        Assert(sim.ValidatePlacement(buildingId, oldSite).IsLegal && sim.ValidatePlacement(buildingId, newSite).IsLegal,
            "cancellation fixture has two legal placement sites");
        var firstMove = sim.EnqueueCommand(new MoveUnitCommand(grunt.EntityId, new(-450, 0)));
        var placement = sim.EnqueueCommand(new PlaceBuildingCommand(buildingId, oldSite, grunt.EntityId));
        var lastMove = sim.EnqueueCommand(new MoveUnitCommand(grunt.EntityId, new(-500, 0)));
        Assert(sim.CancelPendingCommand(placement), "pending placement can be cancelled by ID");
        Assert(!sim.CancelPendingCommand(placement) && !sim.CancelPendingCommand(long.MaxValue),
            "repeated or unknown cancellation does not affect other commands");
        sim.Step();
        Assert(sim.Buildings.Count == 1 && sim.Materials == 1000, "cancelled placement neither builds nor spends");
        Assert(grunt.MoveTarget == new SimVector2(-500, 0), "cancelling a middle command preserves other orders and FIFO");
        var receipts = sim.DrainCommandReceipts();
        Assert(receipts.Count == 3 && receipts.Single(r => r.CommandId == placement).Outcome.Success == false &&
            receipts.Where(r => r.Outcome.Success).Select(r => r.CommandId).SequenceEqual(new[] { firstMove, lastMove }),
            "cancellation emits one failed receipt and surrounding commands execute once in order");

        var cancelled = sim.EnqueueCommand(new PlaceBuildingCommand(buildingId, oldSite, grunt.EntityId));
        Assert(sim.CancelPendingCommand(cancelled), "another placement can be cancelled before its step");
        var replacement = sim.EnqueueCommand(new PlaceBuildingCommand(buildingId, newSite, grunt.EntityId));
        sim.Step();
        Assert(sim.Buildings.Count == 2 && sim.Buildings.Single(b => b.Definition.Id == buildingId).Position == newSite &&
            sim.Materials == 1000 - context.Catalog.GetBuilding(buildingId).Cost,
            "replacement places only at the new site and charges exactly once");
        Assert(!sim.CancelPendingCommand(replacement), "executed placement cannot be cancelled retroactively");
        receipts = sim.DrainCommandReceipts();
        Assert(receipts.Count == 2 && !receipts.Single(r => r.CommandId == cancelled).Outcome.Success &&
            receipts.Single(r => r.CommandId == replacement).Outcome.Success,
            "late cancellation preserves the executed receipt without adding another");
    }

    private static void ValidateMovementBudget(SmokeTestContext context)
    {
        var sim = new RtsSimulation(context.Catalog, 0, []);
        var rover = sim.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new(0, 0));
        var budget = rover.Definition.MovementSpeed * 95f * (float)RtsSimulation.TickSeconds;
        // Turn before the budget runs out; the remainder must continue on the next segment.
        rover.SetPath(new(1, 100), [new(1, 0), new(1, 100)]);
        sim.Step();
        Assert(Math.Abs(rover.Position.X - 1) < 1e-5 && Math.Abs(rover.Position.Y - (budget - 1)) < 1e-5,
            "a turn consumes only traveled distance, with no lost step or free waypoint snap");
    }

    private static void ValidateCornerCrush(SmokeTestContext context)
    {
        var sim = new RtsSimulation(context.Catalog, 0, []);
        var rover = sim.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new(-4, 0));
        var cornerTarget = sim.AddUnit(ContentIds.Units.Guardian, ContentIds.Factions.PrivateMilitary, new(19.9f, 0));
        var outgoingTarget = sim.AddUnit(ContentIds.Units.Guardian, ContentIds.Factions.PrivateMilitary, new(0, 23));
        rover.SetPath(new(0, 100), [new(0, 0), new(0, 100)]);
        sim.Step();
        Assert(cornerTarget.Health == 5, $"corner contact deals one crush, leaving Guardian at 5 HP (got {cornerTarget.Health})");
        Assert(outgoingTarget.Health == 5, "outgoing-only contact still receives crush damage");

        // Contact immunity lasts one step, not the whole movement order.
        cornerTarget.Position = rover.Position + new SimVector2(0, 1);
        sim.Step();
        Assert(cornerTarget.IsDestroyed, "the same vehicle can crush the same target again on the next step");

        var pair = new RtsSimulation(context.Catalog, 0, []);
        var first = pair.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new(-4, 0));
        var second = pair.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new(-4, 0));
        var sharedTarget = pair.AddUnit(ContentIds.Units.Guardian, ContentIds.Factions.PrivateMilitary, new(19.9f, 0));
        first.SetPath(new(100, 0), [new(100, 0)]);
        second.SetPath(new(100, 0), [new(100, 0)]);
        pair.Step();
        Assert(sharedTarget.IsDestroyed, "two separate vehicles can each deal crush damage within one step");
    }

    private static void ValidateInterpolation()
    {
        var poses = new UnitPoseInterpolation();
        poses.Push(new(new(100, 200), 350));
        Assert(poses.Sample(0).Position == new SimVector2(100, 200), "new entity never interpolates from the origin");
        poses.Push(new(new(110, 200), 10));
        var middle = poses.Sample(0.5f);
        Assert(middle.Position == new SimVector2(105, 200) && Math.Abs(middle.FacingDegrees - 360) < 1e-5,
            "position interpolates and facing takes the shortest turn across north");
        poses.Push(new(new(120, 200), 20));
        Assert(poses.Sample(0).Position == new SimVector2(110, 200), "catch-up keeps the last two steps, not the last two frames");
        poses.Reset(new(new(-200, 0), 180));
        Assert(poses.Sample(0.5f).Position == new SimVector2(-200, 0), "reset snaps both histories to the new location");
    }
}
