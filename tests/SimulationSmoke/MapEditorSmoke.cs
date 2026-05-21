using Stratezone.Simulation;
using Stratezone.Simulation.Content;
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
        Assert(session.Objects.Count == context.WellsAtTheRidgeMap.MapObjects.Count, "map editor session clones map objects");

        Assert(session.SelectAt(new SimVector2(610, -80)), "map editor can select an authored marker by position");
        Assert(session.SelectionKind == MapEditorSelectionKind.Marker, "marker selection wins over region selection");
        Assert(session.SelectedId == "enemy_defense", "map editor selects the nearest Defense Tower marker");
        Assert(session.SelectedMarker?.ContentIds.Contains(ContentIds.Buildings.DefenseTower) == true, "marker content links include starting-entity content ids");

        session.ClearSelection();
        Assert(session.SelectNext(), "map editor can cycle to the first editable item");
        Assert(session.SelectedId == "player_landing_zone", "map editor forward cycling starts at the first marker");
        Assert(session.SelectPrevious(), "map editor can cycle backward");
        Assert(session.SelectedId == "bridge_central_isle_east", "map editor backward cycling wraps to the final map object");
        Assert(session.SelectNext(), "map editor can cycle from final object back to first marker");
        Assert(session.SelectedId == "player_landing_zone", "map editor forward cycling wraps back to first marker");

        Assert(session.SelectMarker("enemy_defense"), "map editor can select marker by stable id");
        Assert(session.NudgeSelected(new SimVector2(10, -5)), "map editor can nudge the selected marker");
        var markerExport = session.ExportSelectedSnippet();
        Assert(markerExport.Contains("\"id\": \"enemy_defense\"", StringComparison.Ordinal), "marker export includes stable id");
        Assert(markerExport.Contains("\"x\": 620", StringComparison.Ordinal), "marker export includes nudged x position");
        Assert(markerExport.Contains("\"y\": -85", StringComparison.Ordinal), "marker export includes nudged y position");

        Assert(session.SelectAt(new SimVector2(-860, 420)), "map editor can select a terrain region by position");
        Assert(session.SelectionKind == MapEditorSelectionKind.Region, "terrain selection uses region kind");
        Assert(session.SelectedId == "player_start_well_basin", "map editor picks the smallest matching terrain region");
        session.MoveSelectedTo(new SimVector2(-880, 420));
        var regionExport = session.ExportSelectedSnippet();
        Assert(regionExport.Contains("\"id\": \"player_start_well_basin\"", StringComparison.Ordinal), "region export includes stable id");
        Assert(regionExport.Contains("\"radius\": 200", StringComparison.Ordinal), "region export preserves circle radius");
        Assert(regionExport.Contains("\"x\": -880", StringComparison.Ordinal), "region export includes moved x center");
        Assert(regionExport.Contains("\"y\": 420", StringComparison.Ordinal), "region export includes moved y center");

        Assert(session.SelectRegion("central_island_buildable"), "map editor can select a region by stable id");
        var rectExport = session.ExportSelectedSnippet();
        Assert(rectExport.Contains("\"radius\":", StringComparison.Ordinal), "circle region export includes radius");
        Assert(rectExport.Contains("\"tags\": [\"central_island\", \"buildable\", \"contested\"]", StringComparison.Ordinal), "region export preserves ordered tags");

        Assert(session.SelectAt(new SimVector2(-360, 0)), "map editor can select a bridge object by position");
        Assert(session.SelectionKind == MapEditorSelectionKind.Object, "bridge selection uses object kind");
        Assert(session.SelectedId == "bridge_central_isle_west", "map editor selects bridge objects over underlying water regions");
        var objectExport = session.ExportSelectedSnippet();
        Assert(objectExport.Contains("\"object_type\": \"bridge\"", StringComparison.Ordinal), "object export includes bridge type");
        Assert(objectExport.Contains("\"max_health\": 600", StringComparison.Ordinal), "object export includes bridge health");

        session.ClearSelection();
        Assert(session.ExportSelectedSnippet() == "No map editor selection.", "map editor reports missing selection plainly");
        Assert(session.Diagnostics.Any(entry => entry.Level == MapEditorLogLevel.Warning && entry.Operation == "export_selected"), "map editor logs missing-selection export warnings");

        var fullExport = session.ExportAllSnippets();
        Assert(fullExport.Contains("Mission: mission_wells_at_the_ridge", StringComparison.Ordinal), "full export includes mission context");
        Assert(fullExport.Contains("Map: map_wells_at_the_ridge_greybox", StringComparison.Ordinal), "full export includes map context");
        Assert(fullExport.Contains("\"mission_markers\": [", StringComparison.Ordinal), "full export includes marker block");
        Assert(fullExport.Contains("\"terrain_regions\": [", StringComparison.Ordinal), "full export includes terrain block");
        Assert(fullExport.Contains("\"map_objects\": [", StringComparison.Ordinal), "full export includes map object block");
        ValidateEditorCreationDeletionResizeAndValidation(context);
        ValidateSessionMaterializationAndSave(context);

        var brokenSession = new MapEditorSession();
        brokenSession.Load(null, null);
        Assert(brokenSession.Diagnostics.Count(entry => entry.Level == MapEditorLogLevel.Error && entry.Operation == "load") == 2, "map editor logs missing mission and map load errors");

        var diagnostic = brokenSession.Diagnostics.First(entry => entry.Level == MapEditorLogLevel.Error);
        Assert(diagnostic.ToConsoleLine().Contains("[MapEditor:Error]", StringComparison.Ordinal), "map editor diagnostic console line includes severity");
        Assert(diagnostic.ToConsoleLine().Contains("op=load", StringComparison.Ordinal), "map editor diagnostic console line includes operation");
        Assert(diagnostic.ToConsoleLine().Contains("objects=", StringComparison.Ordinal), "map editor diagnostic console line includes object count");
    }

    private static void ValidateSessionMaterializationAndSave(SmokeTestContext context)
    {
        var session = new MapEditorSession();
        session.Load(context.WellsAtTheRidgeMission, context.WellsAtTheRidgeMap);
        Assert(session.SelectMarker("enemy_defense"), "save smoke can select marker");
        Assert(session.MoveSelectedTo(new SimVector2(640, -95)), "save smoke can move marker");
        Assert(session.SelectRegion("central_island_buildable"), "save smoke can select region");
        Assert(session.MoveSelectedTo(new SimVector2(40, 15)), "save smoke can move region");
        Assert(session.SelectAt(new SimVector2(-360, 0)), "save smoke can select bridge object");
        Assert(session.MoveSelectedTo(new SimVector2(-340, 10)), "save smoke can move map object");

        var editedMission = session.ApplyToMission(context.WellsAtTheRidgeMission);
        var editedMap = session.ApplyToMap(context.WellsAtTheRidgeMap);
        Assert(editedMission.Markers.Single(marker => marker.Id == "enemy_defense").Position == new SimVector2(640, -95), "map editor materializes edited mission markers");
        Assert(editedMap.TerrainRegions.Single(region => region.Id == "central_island_buildable").Center == new SimVector2(40, 15), "map editor materializes edited terrain regions");
        Assert(editedMap.MapObjects.Single(item => item.Id == "bridge_central_isle_west").Center == new SimVector2(-340, 10), "map editor materializes edited map objects");

        var tempRoot = Path.Combine(Path.GetTempPath(), "StratezoneMapEditorSmoke", Guid.NewGuid().ToString("N"));
        try
        {
            CopyDirectory(Path.Combine(context.GameRoot, "data"), Path.Combine(tempRoot, "data"));
            var preview = MapEditorPersistence.CreatePreview(tempRoot, session);
            Assert(preview.HasChanges, "map editor save preview detects changed source files");
            Assert(preview.MissionChanged, "map editor save preview includes mission marker changes");
            Assert(preview.MapChanged, "map editor save preview includes map region/object changes");
            Assert(
                preview.DiffText.Contains("\"enemy_defense\"", StringComparison.Ordinal) &&
                preview.DiffText.Contains("\"x\": 640", StringComparison.Ordinal) &&
                preview.DiffText.Contains("\"y\": -95", StringComparison.Ordinal),
                "map editor save preview includes marker diff");
            Assert(preview.DiffText.Contains("\"central_island_buildable\"", StringComparison.Ordinal), "map editor save preview includes terrain diff");
            Assert(preview.DiffText.Contains("\"bridge_central_isle_west\"", StringComparison.Ordinal), "map editor save preview includes map-object diff");

            var result = MapEditorPersistence.WritePreview(preview);
            Assert(result.Success && result.WroteMission && result.WroteMap, "map editor save writes changed mission and map files");
            Assert(File.Exists(preview.MissionSourcePath + ".bak"), "map editor save keeps one mission backup");
            Assert(File.Exists(preview.MapSourcePath + ".bak"), "map editor save keeps one map backup");
            Assert(!File.Exists(preview.MissionSourcePath + ".tmp") && !File.Exists(preview.MapSourcePath + ".tmp"), "map editor save removes temp files after atomic replace");

            var savedCatalog = ContentCatalog.LoadFromGameData(tempRoot);
            var savedMission = savedCatalog.GetMission(ContentIds.Missions.WellsAtTheRidge);
            var savedMap = savedCatalog.GetMap(context.WellsAtTheRidgeMap.Id);
            Assert(savedMission.Markers.Single(marker => marker.Id == "enemy_defense").Position == new SimVector2(640, -95), "map editor save persists marker edits to mission JSON");
            Assert(savedMap.TerrainRegions.Single(region => region.Id == "central_island_buildable").Center == new SimVector2(40, 15), "map editor save persists region edits to map JSON");
            Assert(savedMap.MapObjects.Single(item => item.Id == "bridge_central_isle_west").Center == new SimVector2(-340, 10), "map editor save persists object edits to map JSON");

            var secondPreview = MapEditorPersistence.CreatePreview(tempRoot, session);
            Assert(!secondPreview.HasChanges, "map editor save preview reports no changes after writing the same edits");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private static void ValidateEditorCreationDeletionResizeAndValidation(SmokeTestContext context)
    {
        var session = new MapEditorSession();
        session.Load(context.WellsAtTheRidgeMission, context.WellsAtTheRidgeMap);
        var initialIssues = session.ValidateContent();
        Assert(!initialIssues.Any(issue => issue.Severity == MapEditorValidationSeverity.Error), "loaded editor session has no blocking validation errors");
        Assert(!initialIssues.Any(issue => issue.Code == "missing_required_feature"), "Level 2 required feature validation uses exact ids and tags without false warnings");

        var firstLandingSession = new MapEditorSession();
        firstLandingSession.Load(context.FirstLandingMission, context.Catalog.GetMap(context.FirstLandingMission.MapId));
        Assert(!firstLandingSession.ValidateContent().Any(issue => issue.Code == "missing_required_feature"), "First Landing required feature validation uses marker tags instead of marker-name substrings");
        Assert(firstLandingSession.SelectMarker("central_well"), "required-feature smoke can select the tagged contested well marker");
        Assert(firstLandingSession.RenameSelected("contested_isle_well").Changed, "required-feature smoke can rename a tagged marker");
        Assert(!firstLandingSession.ValidateContent().Any(issue => issue.Code == "missing_required_feature" && issue.Message.Contains("contested_well", StringComparison.Ordinal)), "renaming a tagged contested well marker does not break required-feature validation");

        var marker = session.AddMarkerAt(new SimVector2(-120, 240));
        Assert(marker.Id == "marker_new", "map editor creates a unique marker id");
        Assert(session.SelectedId == marker.Id && session.SelectionKind == MapEditorSelectionKind.Marker, "created marker becomes selected");
        Assert(session.ValidateContent().Any(issue => issue.Code == "unreferenced_marker" && issue.Message.Contains(marker.Id, StringComparison.Ordinal)), "new marker is flagged as unreferenced warning");
        Assert(session.Undo(), "map editor undo removes the created marker");
        Assert(!session.Markers.Any(item => item.Id == marker.Id), "undo restores marker list");
        Assert(session.Redo(), "map editor redo restores the created marker");
        Assert(session.Markers.Any(item => item.Id == marker.Id), "redo restores marker creation");

        var rect = session.AddRegion("rect", new SimVector2(100, 100), new SimVector2(80, 120), 0);
        Assert(rect.Shape == "rect", "map editor creates rect regions");
        Assert(rect.BlocksMovement && rect.BlocksBuilding && !rect.AllowsBuilding, "new regions default to blocked terrain logic");
        Assert(session.HitTestResizeHandle(new SimVector2(140, 100), 16) == MapEditorResizeHandle.RectRight, "rect region exposes edge resize handle");
        Assert(session.ResizeSelected(MapEditorResizeHandle.RectRight, new SimVector2(180, 100)), "rect region can be resized by edge handle");
        Assert(session.SelectedRegion?.Size.X == 120, "rect resize updates width while holding opposite edge");

        var circle = session.AddRegion("circle", new SimVector2(200, 200), new SimVector2(100, 100), 50);
        Assert(circle.Shape == "circle" && circle.Radius == 50, "map editor creates circle regions");
        Assert(session.HitTestResizeHandle(new SimVector2(250, 200), 12) == MapEditorResizeHandle.CircleRadius, "circle region exposes perimeter resize handle");
        Assert(session.ResizeSelected(MapEditorResizeHandle.CircleRadius, new SimVector2(280, 200)), "circle region can be resized by perimeter handle");
        Assert(session.SelectedRegion?.Radius == 80, "circle resize updates radius");

        Assert(session.SelectMarker("enemy_base"), "delete smoke can select a referenced marker");
        var deletePreview = session.CreateDeletePreview();
        Assert(deletePreview is not null, "delete preview is created for selected marker");
        Assert(deletePreview!.References.Any(reference => reference.StartsWith("starting_entities", StringComparison.Ordinal)), "delete preview lists starting entity references");
        Assert(deletePreview.References.Any(reference => reference.StartsWith("enemy_ai_profile.hub_marker", StringComparison.Ordinal)), "delete preview lists AI marker references");
        var deleteResult = session.DeleteSelected();
        Assert(deleteResult.Changed, "dependency-aware delete can orphan selected marker after confirmation");
        Assert(session.ValidateContent().Any(issue => issue.Severity == MapEditorValidationSeverity.Error && issue.Message.Contains("enemy_base", StringComparison.Ordinal)), "orphaned delete creates blocking validation error");
        Assert(session.Undo(), "undo restores deleted referenced marker");
        Assert(!session.ValidateContent().Any(issue => issue.Severity == MapEditorValidationSeverity.Error), "undo clears orphaned marker validation error");

        Assert(session.SelectMarker("enemy_base"), "rename smoke can select referenced marker");
        var renameResult = session.RenameSelected("enemy_base_renamed");
        Assert(renameResult.Changed, "map editor can rename selected marker");
        Assert(renameResult.Warnings.Any(warning => warning.Contains("Existing references", StringComparison.Ordinal)), "rename warning surfaces stale references");
        Assert(session.ValidateContent().Any(issue => issue.Severity == MapEditorValidationSeverity.Error && issue.Message.Contains("enemy_base", StringComparison.Ordinal)), "renaming a referenced marker creates validation error until JSON references are reviewed");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, destination, StringComparison.Ordinal));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination, StringComparison.Ordinal), overwrite: true);
        }
    }
}
