using Stratezone.Simulation;
using static SmokeTestSupport;

internal static class RenderSnapshotSmoke
{
    public static void Run(SmokeTestContext context)
    {
        ValidateBuildingSnapshots(context);
        ValidateUnitSnapshots(context);
        ValidateFogExploredRevision();
    }

    private static void ValidateBuildingSnapshots(SmokeTestContext context)
    {
        var definition = context.Catalog.GetBuilding(ContentIds.Buildings.PowerPlant);
        var building = new BuildingState(1, definition, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0), null)
        {
            IsPowered = true
        };

        var idle = BuildingRenderSnapshot.From(building, selected: false, cameraZoom: 1.0f, labelText: "Power Plant");
        var same = BuildingRenderSnapshot.From(building, selected: false, cameraZoom: 1.0f, labelText: "Power Plant");
        Assert(idle == same, "identical building render state produces identical snapshots");

        building.ApplyDamage(10, "debug");
        var damaged = BuildingRenderSnapshot.From(building, selected: false, cameraZoom: 1.0f, labelText: "Power Plant 98%");
        Assert(damaged != idle, "building health and label changes affect render snapshot");

        building.IsPowered = false;
        var unpowered = BuildingRenderSnapshot.From(building, selected: false, cameraZoom: 1.0f, labelText: "Power Plant 98%");
        Assert(unpowered != damaged, "building power changes affect render snapshot");

        var selected = BuildingRenderSnapshot.From(building, selected: true, cameraZoom: 1.0f, labelText: "Power Plant 98%");
        Assert(selected != unpowered, "building selection changes affect render snapshot");
    }

    private static void ValidateUnitSnapshots(SmokeTestContext context)
    {
        var definition = context.Catalog.GetUnit(ContentIds.Units.Rifleman);
        var unit = new UnitState(2, definition, ContentIds.Factions.PlayerExpedition, new SimVector2(0, 0));

        var idle = UnitRenderSnapshot.From(unit, 180, false, false, false, 0, 0, 1.0f, "Rifleman 100%");
        var same = UnitRenderSnapshot.From(unit, 180, false, false, false, 0, 0, 1.0f, "Rifleman 100%");
        Assert(idle == same, "identical unit render state produces identical snapshots");

        var facing = UnitRenderSnapshot.From(unit, 90, false, false, false, 0, 0, 1.0f, "Rifleman 100%");
        Assert(facing != idle, "unit facing changes affect render snapshot");

        unit.SetPath(new SimVector2(100, 0), [new SimVector2(100, 0)]);
        var moving = UnitRenderSnapshot.From(unit, 90, false, true, false, 1, 0, 1.0f, "Rifleman 100%");
        Assert(moving != facing, "unit movement and animation frame changes affect render snapshot");

        unit.IsBlockedByEnergyWall = true;
        var blocked = UnitRenderSnapshot.From(unit, 90, false, true, false, 1, 0, 1.0f, "BLOCKED Rifleman 100%");
        Assert(blocked != moving, "unit blocked state changes affect render snapshot");
    }

    private static void ValidateFogExploredRevision()
    {
        var fog = new FogOfWarState(-64, 64, -64, 64, 32);
        Assert(fog.ExploredRevision == 0, "fog revision starts at zero");

        fog.Reveal(new SimVector2(0, 0), 16);
        var afterFirstReveal = fog.ExploredRevision;
        Assert(afterFirstReveal > 0, "fog revision increments when new cells are explored");

        fog.Reveal(new SimVector2(0, 0), 16);
        Assert(fog.ExploredRevision == afterFirstReveal, "fog revision does not increment for already explored cells");
    }
}
