using Stratezone.Simulation;

public partial class Main
{
    private const int MaxHudAlerts = 3;
    private const float UnderAttackAlertCooldownSeconds = 4.0f;
    private const float PowerAlertCooldownSeconds = 3.0f;

    private readonly Queue<string> _hudAlerts = [];
    private readonly HashSet<int> _spottedEnemyUnitIds = [];
    private readonly HashSet<int> _spottedEnemyBuildingIds = [];
    private readonly Dictionary<int, float> _lastPlayerUnitHealth = [];
    private readonly Dictionary<int, float> _lastPlayerBuildingHealth = [];
    private readonly Dictionary<int, bool> _lastPlayerBuildingPower = [];
    private float _nextUnderAttackAlertSeconds;
    private float _nextPowerAlertSeconds;

    private void PollPlayerKnowledgeAlerts()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var simEvent in _simulation.DrainEvents())
        {
            if (simEvent.FactionId == ContentIds.Factions.PlayerExpedition)
            {
                PushAlert(simEvent.MessageKey, simEvent.MessageArgs);
            }
        }

        PollEnemySpottedAlerts();
        PollOwnAssetDamageAlerts();
        PollOwnPowerAlerts();
    }

    private void PollEnemySpottedAlerts()
    {
        if (_simulation is null)
        {
            return;
        }

        var newUnits = 0;
        foreach (var unit in _simulation.Units.Where(unit =>
            unit.FactionId == ContentIds.Factions.PrivateMilitary &&
            !unit.IsDestroyed &&
            _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unit.Position)))
        {
            if (_spottedEnemyUnitIds.Add(unit.EntityId))
            {
                newUnits++;
            }
        }

        if (newUnits > 0)
        {
            PushAlert("ui.alert.enemy_units_spotted", SimulationMessage.Args(("count", newUnits)));
        }

        var newBuildings = 0;
        foreach (var building in _simulation.Buildings.Where(building =>
            building.FactionId == ContentIds.Factions.PrivateMilitary &&
            !building.IsDestroyed &&
            _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position)))
        {
            if (_spottedEnemyBuildingIds.Add(building.EntityId))
            {
                newBuildings++;
            }
        }

        if (newBuildings > 0)
        {
            PushAlert("ui.alert.enemy_structures_spotted", SimulationMessage.Args(("count", newBuildings)));
        }
    }

    private void PollOwnAssetDamageAlerts()
    {
        if (_simulation is null || _simulation.ElapsedSeconds < _nextUnderAttackAlertSeconds)
        {
            SeedOwnHealthSnapshots();
            return;
        }

        foreach (var building in _simulation.Buildings.Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition))
        {
            if (_lastPlayerBuildingHealth.TryGetValue(building.EntityId, out var previousHealth) &&
                building.Health < previousHealth - 0.1f)
            {
                var key = building.Definition.Id switch
                {
                    ContentIds.Buildings.ColonyHub => "ui.alert.base_under_attack",
                    ContentIds.Buildings.ExtractorRefinery => "ui.alert.extractor_under_attack",
                    _ => "ui.alert.building_under_attack"
                };
                PushAlert(key, SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
                _nextUnderAttackAlertSeconds = _simulation.ElapsedSeconds + UnderAttackAlertCooldownSeconds;
                break;
            }
        }

        if (_simulation.ElapsedSeconds >= _nextUnderAttackAlertSeconds)
        {
            foreach (var unit in _simulation.Units.Where(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition))
            {
                if (_lastPlayerUnitHealth.TryGetValue(unit.EntityId, out var previousHealth) &&
                    unit.Health < previousHealth - 0.1f)
                {
                    PushAlert("ui.alert.unit_under_attack", SimulationMessage.Args(("unitId", unit.Definition.Id), ("unit", unit.Definition.DisplayName)));
                    _nextUnderAttackAlertSeconds = _simulation.ElapsedSeconds + UnderAttackAlertCooldownSeconds;
                    break;
                }
            }
        }

        SeedOwnHealthSnapshots();
    }

    private void PollOwnPowerAlerts()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var building in _simulation.Buildings.Where(building =>
            building.FactionId == ContentIds.Factions.PlayerExpedition &&
            building.Definition.RequiresPower &&
            !building.IsDestroyed))
        {
            if (_lastPlayerBuildingPower.TryGetValue(building.EntityId, out var wasPowered) &&
                wasPowered &&
                !building.IsPowered &&
                _simulation.ElapsedSeconds >= _nextPowerAlertSeconds)
            {
                PushAlert("ui.alert.power_offline", SimulationMessage.Args(("buildingId", building.Definition.Id), ("building", building.Definition.DisplayName)));
                _nextPowerAlertSeconds = _simulation.ElapsedSeconds + PowerAlertCooldownSeconds;
            }

            _lastPlayerBuildingPower[building.EntityId] = building.IsPowered;
        }

        var liveIds = _simulation.Buildings
            .Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed)
            .Select(building => building.EntityId)
            .ToHashSet();
        foreach (var staleId in _lastPlayerBuildingPower.Keys.Where(id => !liveIds.Contains(id)).ToArray())
        {
            _lastPlayerBuildingPower.Remove(staleId);
        }
    }

    private void SeedOwnHealthSnapshots()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var unit in _simulation.Units.Where(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && !unit.IsDestroyed))
        {
            _lastPlayerUnitHealth[unit.EntityId] = unit.Health;
        }

        foreach (var building in _simulation.Buildings.Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed))
        {
            _lastPlayerBuildingHealth[building.EntityId] = building.Health;
        }

        var liveUnitIds = _simulation.Units
            .Where(unit => unit.FactionId == ContentIds.Factions.PlayerExpedition && !unit.IsDestroyed)
            .Select(unit => unit.EntityId)
            .ToHashSet();
        foreach (var staleId in _lastPlayerUnitHealth.Keys.Where(id => !liveUnitIds.Contains(id)).ToArray())
        {
            _lastPlayerUnitHealth.Remove(staleId);
        }

        var liveBuildingIds = _simulation.Buildings
            .Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed)
            .Select(building => building.EntityId)
            .ToHashSet();
        foreach (var staleId in _lastPlayerBuildingHealth.Keys.Where(id => !liveBuildingIds.Contains(id)).ToArray())
        {
            _lastPlayerBuildingHealth.Remove(staleId);
        }
    }

    private void PushAlert(string key, IReadOnlyDictionary<string, string>? args = null)
    {
        var text = L(key, ResolveMessageArgs(args));
        if (_hudAlerts.Count > 0 && _hudAlerts.Last() == text)
        {
            return;
        }

        _hudAlerts.Enqueue(text);
        while (_hudAlerts.Count > MaxHudAlerts)
        {
            _hudAlerts.Dequeue();
        }
    }

    private string GetAlertSummary()
    {
        return _hudAlerts.Count == 0
            ? L("ui.alert.none")
            : string.Join(" / ", _hudAlerts);
    }
}
