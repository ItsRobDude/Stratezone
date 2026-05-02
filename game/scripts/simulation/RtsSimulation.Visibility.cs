namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    public bool IsVisibleToFaction(string factionId, SimVector2 position)
    {
        return GetFogForFaction(factionId).IsExplored(position);
    }

    public bool IsCurrentlyObservedByFaction(string factionId, SimVector2 position)
    {
        return GetFogForFaction(factionId).IsVisible(position);
    }

    public bool IsExploredByFaction(string factionId, SimVector2 position)
    {
        return GetFogForFaction(factionId).IsExplored(position);
    }

    private void RecomputeFog()
    {
        FogOfWarSystem.Recompute(_playerFog, _enemyFog, _buildings, _units);
    }

    private FogOfWarState GetFogForFaction(string factionId)
    {
        return factionId == ContentIds.Factions.PrivateMilitary ? _enemyFog : _playerFog;
    }

    private void RevealTanksForDestroyedHubs()
    {
        foreach (var hub in _buildings.Where(building =>
            building.Definition.Id == ContentIds.Buildings.ColonyHub &&
            building.IsDestroyed &&
            !_hubTankReveals.Contains(building.EntityId)).ToArray())
        {
            _hubTankReveals.Add(hub.EntityId);
            AddUnit(ContentIds.Units.MediumTank, hub.FactionId, hub.Position + new SimVector2(85, 45));
        }
    }

    private void RevealCadetsForDestroyedBuildings()
    {
        foreach (var building in _buildings.Where(building =>
            building.IsDestroyed &&
            !_buildingCadetReveals.Contains(building.EntityId)).ToArray())
        {
            var cadetCount = GetDestroyedBuildingCadetCount(building.Definition.Id);
            if (cadetCount <= 0)
            {
                continue;
            }

            _buildingCadetReveals.Add(building.EntityId);
            foreach (var offset in GetDestroyedBuildingCadetOffsets(cadetCount))
            {
                AddUnit(ContentIds.Units.Cadet, building.FactionId, building.Position + offset);
            }
        }
    }

    private static int GetDestroyedBuildingCadetCount(string buildingId)
    {
        return buildingId switch
        {
            ContentIds.Buildings.Barracks => 3,
            ContentIds.Buildings.PowerPlant => 1,
            _ => 0
        };
    }

    private static IEnumerable<SimVector2> GetDestroyedBuildingCadetOffsets(int count)
    {
        var spacing = 34.0f;
        for (var index = 0; index < count; index++)
        {
            var column = index - ((count - 1) * 0.5f);
            yield return new SimVector2(column * spacing, 44.0f);
        }
    }

    private void UpdateMissionState()
    {
        if (MissionState.Status != MissionStatus.Active)
        {
            return;
        }

        MissionState = _missionObjectives.Evaluate(_units, _buildings);
    }
}
