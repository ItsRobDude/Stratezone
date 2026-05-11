using Stratezone.Simulation.Content;

namespace Stratezone.Simulation;

internal sealed class EnemyAiSystem
{
    private readonly EnemyAiMarkers _markers;
    private readonly EnemyAiProfileDefinition _profile;
    private float _elapsedSeconds;
    private float _nextRebuildSeconds;
    private float _nextProductionSeconds;

    public EnemyAiSystem(EnemyAiMarkers markers, EnemyAiProfileDefinition? profile = null)
    {
        _markers = markers;
        _profile = profile ?? EnemyAiProfileDefinition.Default;
        _nextProductionSeconds = _profile.FirstAttackDelaySeconds;
    }

    public EnemyAiProfileDefinition Profile => _profile;
    public SimVector2 HubPosition => _markers.HubPosition;
    public SimVector2 RallyPosition => _markers.RallyPosition;

    public void Tick(RtsSimulation simulation, float deltaSeconds)
    {
        _elapsedSeconds += deltaSeconds;
        if (!simulation.HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub))
        {
            return;
        }

        if (_elapsedSeconds >= _nextRebuildSeconds)
        {
            EnsureBuilding(simulation, ContentIds.Buildings.PowerPlant, _markers.PowerPlantPosition);
            EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.BasePylonPosition);
            EnsureBuildingAt(simulation, ContentIds.Buildings.Pylon, _markers.ForwardPylonPosition);
            EnsureBuilding(simulation, ContentIds.Buildings.Barracks, _markers.BarracksPosition);
            if (_profile.CentralWellInterest > 0.0f)
            {
                EnsureBuildingAt(simulation, ContentIds.Buildings.ExtractorRefinery, _markers.ExtractorPosition);
            }

            EnsureBuilding(simulation, ContentIds.Buildings.DefenseTower, _markers.DefenseTowerPosition);
            _nextRebuildSeconds = _elapsedSeconds + _profile.RebuildCooldownSeconds;
        }

        simulation.TryStartEnemyGuardianRetrofitIfReady();
        TryStartProduction(simulation);
    }

    private static void EnsureBuilding(RtsSimulation simulation, string buildingId, SimVector2 position)
    {
        if (simulation.HasLiveBuilding(ContentIds.Factions.PrivateMilitary, buildingId))
        {
            return;
        }

        simulation.TryPlaceBuildingForFaction(ContentIds.Factions.PrivateMilitary, buildingId, position);
    }

    private static void EnsureBuildingAt(RtsSimulation simulation, string buildingId, SimVector2 position)
    {
        if (simulation.Buildings.Any(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            building.Definition.Id == buildingId &&
            !building.IsDestroyed &&
            building.Position.DistanceTo(position) <= RtsSimulation.ToWorldRadius(1.0f)))
        {
            return;
        }

        simulation.TryPlaceBuildingForFaction(ContentIds.Factions.PrivateMilitary, buildingId, position);
    }

    private void TryStartProduction(RtsSimulation simulation)
    {
        if (_elapsedSeconds < _nextProductionSeconds ||
            simulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary) ||
            !simulation.EnemyProductionOnline)
        {
            return;
        }

        var selectedUnitId = simulation.ShouldEnemyTrainGuardianRetrofitGrunt()
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
            _nextProductionSeconds = _elapsedSeconds + _profile.ProductionCooldownSeconds;
        }
    }
}
