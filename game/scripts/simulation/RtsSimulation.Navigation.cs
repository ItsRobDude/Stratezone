namespace Stratezone.Simulation;

public sealed partial class RtsSimulation
{
    private NavigationGrid? _navigationGrid;
    private float? _smallestUnitRadius;

    internal int DebugNavigationRebuildCount => _navigationGrid?.DebugRebuildCount ?? 0;

    // Reachability questions ("can anyone walk there?") use the smallest unit.
    private float SmallestUnitRadius => _smallestUnitRadius ??= ToWorldRadius(_catalog.Units.Values.Min(unit => unit.CollisionRadius));

    public bool IsReachableForFaction(string factionId, SimVector2 start, SimVector2 destination)
    {
        return PathfindingSystem.FindPath(GetNavigationGrid(), start, destination, SmallestUnitRadius, factionId).Success;
    }

    internal static float UnitRadius(UnitState unit)
    {
        return ToWorldRadius(unit.Definition.CollisionRadius);
    }

    // The cached clearance field. It rebuilds only when buildings, walls, or bridges changed.
    internal NavigationGrid GetNavigationGrid()
    {
        _navigationGrid ??= new NavigationGrid(_playableBounds);
        _navigationGrid.Refresh(_buildings, GetNavigationWalls(), _map?.TerrainRegions ?? [], _bridges);
        return _navigationGrid;
    }

    internal IReadOnlyList<PathfindingDebugCell> SampleBlockedPathingCells(float sampleSize)
    {
        return PathfindingSystem.SampleBlockedCells(GetNavigationGrid(), SmallestUnitRadius, ContentIds.Factions.PlayerExpedition, sampleSize);
    }

    private PathResult FindUnitPath(UnitState unit, SimVector2 target)
    {
        return PathfindingSystem.FindPath(GetNavigationGrid(), unit.Position, target, UnitRadius(unit), unit.FactionId);
    }

    private List<NavigationWall> GetNavigationWalls()
    {
        var walls = new List<NavigationWall>(_energyWalls.Count);
        foreach (var wall in _energyWalls)
        {
            var owner = FindLiveBuilding(wall.StartAnchorEntityId);
            if (owner is not null)
            {
                walls.Add(new NavigationWall(wall, owner.FactionId));
            }
        }

        return walls;
    }
}
