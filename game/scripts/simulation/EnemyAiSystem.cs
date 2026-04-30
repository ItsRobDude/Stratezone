namespace Stratezone.Simulation;

internal sealed class EnemyAiSystem
{
    private readonly EnemyAiMarkers _markers;

    public EnemyAiSystem(EnemyAiMarkers markers)
    {
        _markers = markers;
    }

    public void Tick(RtsSimulation simulation)
    {
        if (!simulation.HasLiveBuilding(ContentIds.Factions.PrivateMilitary, ContentIds.Buildings.ColonyHub))
        {
            return;
        }

        EnsureBuilding(simulation, ContentIds.Buildings.PowerPlant, _markers.PowerPlantPosition);
        EnsureBuilding(simulation, ContentIds.Buildings.Barracks, _markers.BarracksPosition);
        EnsureBuilding(simulation, ContentIds.Buildings.ExtractorRefinery, _markers.ExtractorPosition);
        EnsureBuilding(simulation, ContentIds.Buildings.DefenseTower, _markers.DefenseTowerPosition);
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

    private static void TryStartProduction(RtsSimulation simulation)
    {
        if (simulation.ProductionOrders.Any(order => order.FactionId == ContentIds.Factions.PrivateMilitary) ||
            !simulation.EnemyProductionOnline)
        {
            return;
        }

        simulation.TryQueueUnitForFaction(
            ContentIds.Factions.PrivateMilitary,
            ContentIds.Units.Rifleman,
            null,
            RtsSimulation.EnemyTrainTimeMultiplier);
    }
}
