using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal sealed class EnemyAiSystem
{
    private const float EmergencyProductionCooldownSeconds = 8.0f;

    private static readonly SimVector2[] PlacementFallbackOffsets =
    [
        new(0, 0),
        new(64, 0),
        new(-64, 0),
        new(0, 64),
        new(0, -64),
        new(64, 64),
        new(64, -64),
        new(-64, 64),
        new(-64, -64),
        new(112, 0),
        new(-112, 0),
        new(0, 112),
        new(0, -112)
    ];

    private readonly EnemyAiMarkers _markers;
    private readonly EnemyAiProfileDefinition _profile;
    private float _elapsedSeconds;
    private float _nextCentralWellRebuildSeconds;
    private float _nextRebuildSeconds;
    private float _nextProductionSeconds;
    private int _centralWellRebuilds;

    public EnemyAiSystem(EnemyAiMarkers markers, EnemyAiProfileDefinition? profile = null)
    {
        _markers = markers;
        _profile = profile ?? EnemyAiProfileDefinition.Default;
        _nextCentralWellRebuildSeconds = _profile.FirstCentralWellRebuildDelaySeconds > 0.0f
            ? _profile.FirstCentralWellRebuildDelaySeconds
            : _profile.FirstRebuildDelaySeconds;
        _nextRebuildSeconds = _profile.FirstRebuildDelaySeconds;
        _nextProductionSeconds = _profile.FirstAttackDelaySeconds;
    }

    public EnemyAiProfileDefinition Profile => _profile;
    public SimVector2 HubPosition => _markers.HubPosition;
    public SimVector2 RallyPosition => _markers.RallyPosition;
    public IReadOnlyList<SimVector2> PatrolPositions => _markers.PatrolPositions;

    public void Tick(RtsSimulation simulation, float deltaSeconds)
    {
        _elapsedSeconds += deltaSeconds;
        if (!simulation.HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub))
        {
            return;
        }

        var baseUnderThreat = simulation.IsEnemyBaseUnderThreat(_markers.HubPosition);
        if (baseUnderThreat)
        {
            _nextProductionSeconds = MathF.Min(_nextProductionSeconds, _elapsedSeconds);
        }

        if (_elapsedSeconds >= _nextRebuildSeconds)
        {
            var shouldMaintainCentralWellRoute = ShouldMaintainCentralWellRoute(simulation);
            EnsureBuilding(simulation, ContentIds.Buildings.PowerPlant, _markers.PowerPlantPosition);
            EnsureBuilding(simulation, ContentIds.Buildings.Barracks, _markers.BarracksPosition);
            EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.WallPowerPylonPosition);
            if (baseUnderThreat)
            {
                if (!HasLiveBuildingNear(simulation, ContentIds.Buildings.Pylon, _markers.WallPowerPylonPosition, allowNearbyFallback: true))
                {
                    EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.BasePylonPosition);
                    EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.WallPowerPylonPosition);
                }

                EnsureBuildingAt(simulation, ContentIds.Buildings.DefenseTower, _markers.DefenseTowerPosition);
            }
            else
            {
                EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.BasePylonPosition);
                if (shouldMaintainCentralWellRoute)
                {
                    EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.ForwardPylonPosition);
                }

                TryEnsureCentralWellExtractor(simulation);
                EnsureBuildingAt(simulation, ContentIds.Buildings.DefenseTower, _markers.DefenseTowerPosition);
            }

            _nextRebuildSeconds = _elapsedSeconds + _profile.RebuildCooldownSeconds;
        }
        else
        {
            TryEnsureCentralWellExtractor(simulation);
        }

        simulation.TryStartEnemyGuardianRetrofitIfReady();
        TryStartProduction(simulation, baseUnderThreat);
    }

    private void TryEnsureCentralWellExtractor(RtsSimulation simulation)
    {
        if (simulation.IsEnemyBaseUnderThreat(_markers.HubPosition) ||
            !ShouldMaintainCentralWellRoute(simulation))
        {
            return;
        }

        var cooldown = _profile.CentralWellRebuildCooldownSeconds > 0.0f
            ? _profile.CentralWellRebuildCooldownSeconds
            : _profile.RebuildCooldownSeconds;
        if (HasLiveBuildingAt(simulation, ContentIds.Buildings.ExtractorRefinery, _markers.ExtractorPosition))
        {
            _nextCentralWellRebuildSeconds = _elapsedSeconds + cooldown;
            return;
        }

        if (_elapsedSeconds < _nextCentralWellRebuildSeconds)
        {
            return;
        }

        if (EnsureBuildingAt(simulation, ContentIds.Buildings.ExtractorRefinery, _markers.ExtractorPosition, allowNearbyFallback: false))
        {
            _centralWellRebuilds++;
        }

        _nextCentralWellRebuildSeconds = _elapsedSeconds + cooldown;
    }

    private bool ShouldMaintainCentralWellRoute(RtsSimulation simulation)
    {
        if (_profile.CentralWellInterest <= 0.0f ||
            !simulation.IsEnemyCentralWellRouteStrategic(_markers.ExtractorPosition) ||
            !simulation.IsBridgeIntact(_profile.CentralIslandAttackViaBridgeId))
        {
            return false;
        }

        return _centralWellRebuilds < _profile.MaxCentralWellRebuilds ||
            HasLiveBuildingAt(simulation, ContentIds.Buildings.ExtractorRefinery, _markers.ExtractorPosition);
    }

    private static bool EnsureBuilding(RtsSimulation simulation, string buildingId, SimVector2 position)
    {
        if (simulation.HasLiveBuilding(ContentIds.Factions.PrivateMilitary, buildingId))
        {
            return false;
        }

        return TryPlaceBuildingAtOrNear(simulation, buildingId, position, allowNearbyFallback: true);
    }

    private static bool EnsureBuildingAt(RtsSimulation simulation, string buildingId, SimVector2 position, bool allowNearbyFallback = true)
    {
        if (HasLiveBuildingNear(simulation, buildingId, position, allowNearbyFallback))
        {
            return false;
        }

        return TryPlaceBuildingAtOrNear(simulation, buildingId, position, allowNearbyFallback);
    }

    private static bool HasLiveBuildingAt(RtsSimulation simulation, string buildingId, SimVector2 position)
    {
        return HasLiveBuildingNear(simulation, buildingId, position, allowNearbyFallback: false);
    }

    private static bool HasLiveBuildingNear(RtsSimulation simulation, string buildingId, SimVector2 position, bool allowNearbyFallback)
    {
        var radius = allowNearbyFallback
            ? RtsSimulation.ToWorldRadius(6.0f)
            : RtsSimulation.ToWorldRadius(1.0f);
        return simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == buildingId &&
            !building.IsDestroyed &&
            building.Position.DistanceTo(position) <= radius);
    }

    private static bool TryPlaceBuildingAtOrNear(RtsSimulation simulation, string buildingId, SimVector2 position, bool allowNearbyFallback)
    {
        foreach (var offset in PlacementFallbackOffsets)
        {
            if (!allowNearbyFallback && offset != new SimVector2(0, 0))
            {
                continue;
            }

            if (simulation.TryPlaceBuildingForFaction(ContentIds.Factions.PrivateMilitary, buildingId, position + offset).Success)
            {
                return true;
            }
        }

        return false;
    }

    private void TryStartProduction(RtsSimulation simulation, bool baseUnderThreat)
    {
        if ((!baseUnderThreat && _elapsedSeconds < _nextProductionSeconds) ||
            simulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary) ||
            !simulation.EnemyProductionOnline)
        {
            return;
        }

        var selectedUnitId = baseUnderThreat
            ? simulation.SelectEnemyProductionUnit()?.Id
            : simulation.ShouldEnemyTrainGuardianRetrofitGrunt()
                ? ContentIds.Units.Grunt
                : simulation.ShouldEnemyHoldProductionForGuardianRetrofit()
                    ? null
                    : simulation.SelectEnemyProductionUnit()?.Id;
        if (selectedUnitId is null)
        {
            return;
        }

        var result = simulation.TryQueueUnitForFaction(
            ContentIds.Factions.PrivateMilitary,
            selectedUnitId,
            null,
            _profile.TrainTimeMultiplier);

        if (result.Success)
        {
            var cooldown = baseUnderThreat
                ? MathF.Min(_profile.ProductionCooldownSeconds, EmergencyProductionCooldownSeconds)
                : _profile.ProductionCooldownSeconds;
            _nextProductionSeconds = _elapsedSeconds + cooldown;
        }
    }
}
