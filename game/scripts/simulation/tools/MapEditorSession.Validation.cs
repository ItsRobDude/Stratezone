using Stratezone.Simulation.Content;

namespace Stratezone.Simulation.Tools;

public sealed partial class MapEditorSession
{
    public IReadOnlyList<MapEditorValidationIssue> ValidateContent()
    {
        var issues = new List<MapEditorValidationIssue>();
        ValidateDuplicateIds(issues);
        ValidateMarkerReferences(issues);
        ValidateMapObjectReferences(issues);
        ValidateUnreferencedMarkers(issues);
        ValidateRequiredMapFeatures(issues);
        return issues;
    }

    private void ValidateDuplicateIds(List<MapEditorValidationIssue> issues)
    {
        foreach (var group in AllIds().GroupBy(id => id, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            issues.Add(new MapEditorValidationIssue(
                MapEditorValidationSeverity.Error,
                "duplicate_id",
                $"Duplicate editor id '{group.Key}' appears {group.Count()} times."));
        }
    }

    private void ValidateMarkerReferences(List<MapEditorValidationIssue> issues)
    {
        if (_sourceMission is null)
        {
            return;
        }

        var markerIds = _markers.Select(marker => marker.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var entity in _sourceMission.StartingEntities)
        {
            if (!markerIds.Contains(entity.MarkerId))
            {
                issues.Add(new MapEditorValidationIssue(
                    MapEditorValidationSeverity.Error,
                    "missing_marker_ref",
                    $"starting_entities references missing marker '{entity.MarkerId}'."));
            }
        }

        foreach (var placement in _sourceMission.ResourceWellPlacements)
        {
            if (!markerIds.Contains(placement.MarkerId))
            {
                issues.Add(new MapEditorValidationIssue(
                    MapEditorValidationSeverity.Error,
                    "missing_marker_ref",
                    $"resource_well_placements references missing marker '{placement.MarkerId}'."));
            }
        }

        foreach (var callout in _sourceMission.Presentation.MapCallouts)
        {
            if (!markerIds.Contains(callout.MarkerId))
            {
                issues.Add(new MapEditorValidationIssue(
                    MapEditorValidationSeverity.Error,
                    "missing_marker_ref",
                    $"presentation.map_callouts references missing marker '{callout.MarkerId}'."));
            }
        }

        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.hub_marker", _sourceMission.EnemyAiProfile.HubMarkerId);
        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.power_plant_marker", _sourceMission.EnemyAiProfile.PowerPlantMarkerId);
        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.barracks_marker", _sourceMission.EnemyAiProfile.BarracksMarkerId);
        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.extractor_marker", _sourceMission.EnemyAiProfile.ExtractorMarkerId);
        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.defense_tower_marker", _sourceMission.EnemyAiProfile.DefenseTowerMarkerId);
        ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.rally_marker", _sourceMission.EnemyAiProfile.RallyMarkerId);
        foreach (var patrolMarkerId in _sourceMission.EnemyAiProfile.PatrolMarkerIds)
        {
            ValidateAiMarkerReference(issues, markerIds, "enemy_ai_profile.patrol_markers", patrolMarkerId);
        }
    }

    private void ValidateMapObjectReferences(List<MapEditorValidationIssue> issues)
    {
        if (_sourceMission is null)
        {
            return;
        }

        var objectIds = _objects.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var mapObjectOverride in _sourceMission.MapObjectOverrides)
        {
            if (!objectIds.Contains(mapObjectOverride.ObjectId))
            {
                issues.Add(new MapEditorValidationIssue(
                    MapEditorValidationSeverity.Error,
                    "missing_map_object_ref",
                    $"mission_object_overrides references missing map object '{mapObjectOverride.ObjectId}'."));
            }
        }

        var bridgeId = _sourceMission.EnemyAiProfile.CentralIslandAttackViaBridgeId;
        if (!string.IsNullOrWhiteSpace(bridgeId) && !objectIds.Contains(bridgeId))
        {
            issues.Add(new MapEditorValidationIssue(
                MapEditorValidationSeverity.Error,
                "missing_map_object_ref",
                $"enemy_ai_profile.central_island_attack_via_bridge_id references missing map object '{bridgeId}'."));
        }
    }

    private void ValidateUnreferencedMarkers(List<MapEditorValidationIssue> issues)
    {
        if (_sourceMission is null)
        {
            return;
        }

        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entity in _sourceMission.StartingEntities)
        {
            referenced.Add(entity.MarkerId);
        }

        foreach (var placement in _sourceMission.ResourceWellPlacements)
        {
            referenced.Add(placement.MarkerId);
        }

        foreach (var callout in _sourceMission.Presentation.MapCallouts)
        {
            referenced.Add(callout.MarkerId);
        }

        var ai = _sourceMission.EnemyAiProfile;
        referenced.Add(ai.HubMarkerId);
        referenced.Add(ai.PowerPlantMarkerId);
        referenced.Add(ai.BarracksMarkerId);
        referenced.Add(ai.ExtractorMarkerId);
        referenced.Add(ai.DefenseTowerMarkerId);
        referenced.Add(ai.RallyMarkerId);
        foreach (var patrolMarkerId in ai.PatrolMarkerIds)
        {
            referenced.Add(patrolMarkerId);
        }

        foreach (var marker in _markers.Where(marker => !referenced.Contains(marker.Id)))
        {
            issues.Add(new MapEditorValidationIssue(
                MapEditorValidationSeverity.Warning,
                "unreferenced_marker",
                $"Marker '{marker.Id}' is not referenced by entities, wells, callouts, or AI markers."));
        }
    }

    private void ValidateRequiredMapFeatures(List<MapEditorValidationIssue> issues)
    {
        if (_sourceMap is null)
        {
            return;
        }

        foreach (var feature in _sourceMap.RequiredFeatures)
        {
            if (!HasRequiredFeature(feature))
            {
                issues.Add(new MapEditorValidationIssue(
                    MapEditorValidationSeverity.Warning,
                    "missing_required_feature",
                    $"Required map feature '{feature}' is not visible in current markers, regions, objects, or tags."));
            }
        }
    }

    private bool HasRequiredFeature(string feature)
    {
        if (_markers.Any(marker => string.Equals(marker.Id, feature, StringComparison.Ordinal)))
        {
            return true;
        }

        if (_markers.Any(marker => marker.Tags.Contains(feature, StringComparer.Ordinal)))
        {
            return true;
        }

        if (_regions.Any(region =>
            string.Equals(region.Id, feature, StringComparison.Ordinal) ||
            string.Equals(region.RegionType, feature, StringComparison.Ordinal) ||
            region.Tags.Contains(feature, StringComparer.Ordinal)))
        {
            return true;
        }

        if (_objects.Any(item =>
            string.Equals(item.Id, feature, StringComparison.Ordinal) ||
            string.Equals(item.ObjectType, feature, StringComparison.Ordinal) ||
            item.Tags.Contains(feature, StringComparer.Ordinal)))
        {
            return true;
        }

        return feature switch
        {
            "destructible_bridges" => _objects.Any(item => item.ObjectType == "bridge" && item.MaxHealth > 0.0f),
            "buildable_clearings" => _regions.Any(region => region.AllowsBuilding),
            "enemy_colony_hub" => MissionHasEntityAtLiveMarker(ContentIds.Buildings.ColonyHub, ContentIds.Factions.PrivateMilitary),
            _ => false
        };
    }

    private bool MissionHasEntityAtLiveMarker(string contentId, string factionId)
    {
        if (_sourceMission is null)
        {
            return false;
        }

        var markerIds = _markers.Select(marker => marker.Id).ToHashSet(StringComparer.Ordinal);
        return _sourceMission.StartingEntities.Any(entity =>
            entity.ContentId == contentId &&
            entity.FactionId == factionId &&
            markerIds.Contains(entity.MarkerId));
    }

    private static void ValidateAiMarkerReference(
        List<MapEditorValidationIssue> issues,
        HashSet<string> markerIds,
        string field,
        string markerId)
    {
        if (string.IsNullOrWhiteSpace(markerId) || markerIds.Contains(markerId))
        {
            return;
        }

        issues.Add(new MapEditorValidationIssue(
            MapEditorValidationSeverity.Error,
            "missing_marker_ref",
            $"{field} references missing marker '{markerId}'."));
    }
}
