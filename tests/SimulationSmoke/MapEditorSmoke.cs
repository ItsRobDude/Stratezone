using Stratezone.Simulation;
using Stratezone.Simulation.Tools;
using static SmokeTestSupport;

internal static class MapEditorSmoke
{
    public static void Run(SmokeTestContext context)
    {
        var session = new MapEditorSession();
        session.Load(context.WellsAtTheRidgeMission, context.WellsAtTheRidgeMap);

        Assert(session.MissionId == ContentIds.Missions.WellsAtTheRidge, "map editor session loads the active mission id");
        Assert(session.MapId == context.WellsAtTheRidgeMap.Id, "map editor session loads the active map id");
        Assert(session.Markers.Count == context.WellsAtTheRidgeMission.Markers.Count, "map editor session clones mission markers");
        Assert(session.Regions.Count == context.WellsAtTheRidgeMap.TerrainRegions.Count, "map editor session clones map regions");

        Assert(session.SelectAt(new SimVector2(430, -115)), "map editor can select an authored marker by position");
        Assert(session.SelectionKind == MapEditorSelectionKind.Marker, "marker selection wins over region selection");
        Assert(session.SelectedId == "enemy_defense", "map editor selects the nearest Defense Tower marker");
        Assert(session.SelectedMarker?.ContentIds.Contains(ContentIds.Buildings.DefenseTower) == true, "marker content links include starting-entity content ids");

        Assert(session.NudgeSelected(new SimVector2(10, -5)), "map editor can nudge the selected marker");
        var markerExport = session.ExportSelectedSnippet();
        Assert(markerExport.Contains("\"id\": \"enemy_defense\"", StringComparison.Ordinal), "marker export includes stable id");
        Assert(markerExport.Contains("\"x\": 440", StringComparison.Ordinal), "marker export includes nudged x position");
        Assert(markerExport.Contains("\"y\": -120", StringComparison.Ordinal), "marker export includes nudged y position");

        Assert(session.SelectAt(new SimVector2(-460, 360)), "map editor can select a terrain region by position");
        Assert(session.SelectionKind == MapEditorSelectionKind.Region, "terrain selection uses region kind");
        Assert(session.SelectedId == "player_start_well_basin", "map editor picks the smallest matching terrain region");
        session.MoveSelectedTo(new SimVector2(-470, 365));
        var regionExport = session.ExportSelectedSnippet();
        Assert(regionExport.Contains("\"id\": \"player_start_well_basin\"", StringComparison.Ordinal), "region export includes stable id");
        Assert(regionExport.Contains("\"radius\": 175", StringComparison.Ordinal), "region export preserves circle radius");
        Assert(regionExport.Contains("\"x\": -470", StringComparison.Ordinal), "region export includes moved x center");
        Assert(regionExport.Contains("\"y\": 365", StringComparison.Ordinal), "region export includes moved y center");

        Assert(session.SelectRegion("central_power_corridor"), "map editor can select a region by stable id");
        var rectExport = session.ExportSelectedSnippet();
        Assert(rectExport.Contains("\"size\":", StringComparison.Ordinal), "rect region export includes size");
        Assert(rectExport.Contains("\"tags\": [\"power_corridor\", \"buildable\", \"contested_route\"]", StringComparison.Ordinal), "region export preserves ordered tags");

        session.ClearSelection();
        Assert(session.ExportSelectedSnippet() == "No map editor selection.", "map editor reports missing selection plainly");

        var fullExport = session.ExportAllSnippets();
        Assert(fullExport.Contains("Mission: mission_wells_at_the_ridge", StringComparison.Ordinal), "full export includes mission context");
        Assert(fullExport.Contains("Map: map_wells_at_the_ridge_greybox", StringComparison.Ordinal), "full export includes map context");
        Assert(fullExport.Contains("\"mission_markers\": [", StringComparison.Ordinal), "full export includes marker block");
        Assert(fullExport.Contains("\"terrain_regions\": [", StringComparison.Ordinal), "full export includes terrain block");
    }
}
