using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

public sealed class RtsSimulation
{
    public const float ContentUnitScale = 24.0f;
    private const float ResourceWellCoreRadius = 28.0f;

    private readonly ContentCatalog _catalog;
    private readonly List<BuildingState> _buildings = [];
    private readonly List<ResourceWellState> _resourceWells = [];
    private readonly List<EnergyWallSegment> _energyWalls = [];
    private int _nextEntityId = 1;

    public RtsSimulation(ContentCatalog catalog, int startingMaterials, IEnumerable<(string WellId, SimVector2 Position)> resourceWellPlacements)
    {
        _catalog = catalog;
        Materials = startingMaterials;

        foreach (var placement in resourceWellPlacements)
        {
            _resourceWells.Add(new ResourceWellState(_catalog.GetResourceWell(placement.WellId), placement.Position));
        }
    }

    public float Materials { get; private set; }
    public IReadOnlyList<BuildingState> Buildings => _buildings;
    public IReadOnlyList<ResourceWellState> ResourceWells => _resourceWells;
    public IReadOnlyList<EnergyWallSegment> EnergyWalls => _energyWalls;

    public static float ToWorldRadius(float contentRadius)
    {
        return contentRadius * ContentUnitScale;
    }

    public BuildingState AddStartingBuilding(string buildingId, SimVector2 position)
    {
        var definition = _catalog.GetBuilding(buildingId);
        var building = new BuildingState(_nextEntityId++, definition, position, null)
        {
            IsPowered = !definition.RequiresPower
        };

        _buildings.Add(building);
        RecomputePower();
        return building;
    }

    public PlacementValidation ValidatePlacement(string buildingId, SimVector2 position)
    {
        var definition = _catalog.GetBuilding(buildingId);

        if (Materials < definition.Cost)
        {
            return new PlacementValidation(false, $"Need {definition.Cost:0} materials.");
        }

        foreach (var building in _buildings)
        {
            var requiredDistance = building.OccupancyRadius + ToWorldRadius(definition.FootprintRadius + definition.PlacementBuffer);
            if (building.Position.DistanceTo(position) < requiredDistance)
            {
                return new PlacementValidation(false, $"Blocked by {building.Definition.DisplayName}.");
            }
        }

        var targetWell = FindCompatibleResourceWell(definition, position);
        if (definition.ProvidesResourceExtraction && targetWell is null)
        {
            return new PlacementValidation(false, "Extractor must be placed on an open resource well.");
        }

        if (definition.RequiresPower && !WouldBePowered(definition, position))
        {
            return new PlacementValidation(false, "Must be placed inside powered support.");
        }

        return new PlacementValidation(true, "Placement legal.", targetWell?.Definition.Id);
    }

    public PlacementResult TryPlaceBuilding(string buildingId, SimVector2 position)
    {
        var validation = ValidatePlacement(buildingId, position);
        if (!validation.IsLegal)
        {
            return new PlacementResult(false, validation.Reason);
        }

        var definition = _catalog.GetBuilding(buildingId);
        Materials -= definition.Cost;

        var building = new BuildingState(_nextEntityId++, definition, position, validation.ResourceWellId);
        _buildings.Add(building);

        if (validation.ResourceWellId is not null)
        {
            var well = _resourceWells.First(resourceWell => resourceWell.Definition.Id == validation.ResourceWellId);
            well.ExtractorEntityId = building.EntityId;
        }

        RecomputePower();
        return new PlacementResult(true, $"Placed {definition.DisplayName}.", building);
    }

    public void Tick(float deltaSeconds)
    {
        RecomputePower();

        foreach (var extractor in _buildings.Where(building => building.Definition.ProvidesResourceExtraction && building.IsPowered))
        {
            if (extractor.ResourceWellId is null)
            {
                continue;
            }

            var well = _resourceWells.FirstOrDefault(resourceWell => resourceWell.Definition.Id == extractor.ResourceWellId);
            if (well is null || well.IsDepleted)
            {
                continue;
            }

            var extracted = well.Definition.ExtractionRate * deltaSeconds;
            if (well.Definition.Depletes)
            {
                extracted = MathF.Min(extracted, well.Remaining);
                well.Remaining -= extracted;
            }

            Materials += extracted;
        }
    }

    public bool IsLineBlockedByEnergyWall(SimVector2 start, SimVector2 end)
    {
        return _energyWalls.Any(wall => LinesIntersect(start, end, wall.Start, wall.End));
    }

    private ResourceWellState? FindCompatibleResourceWell(BuildingDefinition definition, SimVector2 position)
    {
        if (!definition.ProvidesResourceExtraction || definition.ExtractorResourceId is null)
        {
            return null;
        }

        return _resourceWells
            .Where(well => well.Definition.ResourceId == definition.ExtractorResourceId)
            .Where(well => well.ExtractorEntityId is null)
            .Where(well => !well.IsDepleted)
            .Where(well => well.Position.DistanceTo(position) <= ResourceWellCoreRadius + ToWorldRadius(definition.FootprintRadius))
            .OrderBy(well => well.Position.DistanceTo(position))
            .FirstOrDefault();
    }

    private bool WouldBePowered(BuildingDefinition definition, SimVector2 position)
    {
        if (!definition.RequiresPower)
        {
            return true;
        }

        var targetRadius = ToWorldRadius(definition.FootprintRadius);
        return definition.Id == ContentIds.Buildings.Pylon
            ? IsPylonSupported(position, targetRadius)
            : IsPositionPowered(position, targetRadius);
    }

    private bool IsPositionPowered(SimVector2 position, float targetRadius)
    {
        return _buildings.Any(building =>
            building.Definition.ProvidesPower &&
            building.IsPowered &&
            building.Definition.PowerRadius > 0 &&
            building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PowerRadius) + targetRadius);
    }

    private bool IsPylonSupported(SimVector2 position, float targetRadius)
    {
        foreach (var building in _buildings.Where(building => building.Definition.ProvidesPower && building.IsPowered))
        {
            if (IsInsideLocalPowerField(building, position, targetRadius))
            {
                return true;
            }

            if (building.Definition.Id == ContentIds.Buildings.Pylon &&
                building.Definition.PylonLinkRange > 0 &&
                building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PylonLinkRange))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsInsideLocalPowerField(BuildingState building, SimVector2 position, float targetRadius)
    {
        return building.Definition.PowerRadius > 0 &&
            building.Position.DistanceTo(position) <= ToWorldRadius(building.Definition.PowerRadius) + targetRadius;
    }

    private void RecomputePower()
    {
        foreach (var building in _buildings)
        {
            building.IsPowered = !building.Definition.RequiresPower;
        }

        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var building in _buildings.Where(building => building.Definition.RequiresPower && !building.IsPowered))
            {
                var targetRadius = ToWorldRadius(building.Definition.FootprintRadius);
                var powered = building.Definition.Id == ContentIds.Buildings.Pylon
                    ? IsPylonSupported(building.Position, targetRadius)
                    : IsPositionPowered(building.Position, targetRadius);

                if (!powered)
                {
                    continue;
                }

                building.IsPowered = true;
                changed = true;
            }
        }

        RecomputeEnergyWalls();
    }

    private void RecomputeEnergyWalls()
    {
        _energyWalls.Clear();

        var anchors = _buildings
            .Where(building => building.Definition.WallAnchor && building.IsPowered && building.Definition.WallLinkRange > 0)
            .OrderBy(building => building.EntityId)
            .ToArray();

        for (var leftIndex = 0; leftIndex < anchors.Length; leftIndex++)
        {
            var left = anchors[leftIndex];
            var linkRange = ToWorldRadius(left.Definition.WallLinkRange);

            for (var rightIndex = leftIndex + 1; rightIndex < anchors.Length; rightIndex++)
            {
                var right = anchors[rightIndex];
                var maxLinkRange = MathF.Min(linkRange, ToWorldRadius(right.Definition.WallLinkRange));
                if (left.Position.DistanceTo(right.Position) > maxLinkRange)
                {
                    continue;
                }

                _energyWalls.Add(new EnergyWallSegment(left.EntityId, right.EntityId, left.Position, right.Position));
            }
        }
    }

    private static bool LinesIntersect(SimVector2 a, SimVector2 b, SimVector2 c, SimVector2 d)
    {
        var denominator = ((d.Y - c.Y) * (b.X - a.X)) - ((d.X - c.X) * (b.Y - a.Y));
        if (MathF.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var ua = (((d.X - c.X) * (a.Y - c.Y)) - ((d.Y - c.Y) * (a.X - c.X))) / denominator;
        var ub = (((b.X - a.X) * (a.Y - c.Y)) - ((b.Y - a.Y) * (a.X - c.X))) / denominator;
        return ua is >= 0.0f and <= 1.0f && ub is >= 0.0f and <= 1.0f;
    }
}
