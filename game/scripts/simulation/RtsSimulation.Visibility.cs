namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    public bool IsVisibleToFaction(string factionId, SimVector2 position)
    {
        return (factionId == ContentIds.Factions.PrivateMilitary ? _enemyFog : _playerFog).IsVisible(position);
    }

    public bool IsExploredByFaction(string factionId, SimVector2 position)
    {
        return (factionId == ContentIds.Factions.PrivateMilitary ? _enemyFog : _playerFog).IsExplored(position);
    }

    private void RecomputeFog()
    {
        FogOfWarSystem.Recompute(_playerFog, _enemyFog, _buildings, _units);
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
        MissionState = _missionObjectives.Evaluate(_units, _buildings);
    }
}
