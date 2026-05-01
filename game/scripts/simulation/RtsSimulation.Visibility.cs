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
            AddUnit(ContentIds.Units.Tank, hub.FactionId, hub.Position + new SimVector2(85, 45));
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
