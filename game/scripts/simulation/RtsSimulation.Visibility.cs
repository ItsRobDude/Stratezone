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

    private void RevealDestroyedBuildingUnits()
    {
        // Intentional: destroyed-building reveals do not raise player-facing events.
        // They are quiet on-map consequences documented in system contracts.
        foreach (var building in _buildings.Where(building =>
            building.IsDestroyed &&
            building.Definition.DestroyedReveal is not null &&
            !_destroyedBuildingReveals.Contains(building.EntityId)).ToArray())
        {
            var reveal = building.Definition.DestroyedReveal!;
            if (reveal.Count <= 0 || string.IsNullOrWhiteSpace(reveal.UnitId))
            {
                continue;
            }

            _destroyedBuildingReveals.Add(building.EntityId);
            foreach (var offset in GetDestroyedBuildingRevealOffsets(reveal.Count, reveal.Occupant))
            {
                AddUnit(reveal.UnitId, building.FactionId, building.Position + offset, reveal.Occupant);
            }
        }
    }

    private static IEnumerable<SimVector2> GetDestroyedBuildingRevealOffsets(int count, bool occupant)
    {
        if (occupant && count == 1)
        {
            yield return new SimVector2(85, 45);
            yield break;
        }

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
