using Godot;
using Stratezone.Localization;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class Main : Node2D
{
    private const float DefaultUiScale = 1.6f;
    private const float MinUiScale = 1.0f;
    private const float MaxUiScale = 2.6f;
    private const int HudBaseFontSize = 18;

    private static readonly string[] BuildHotkeyOrder =
    [
        ContentIds.Buildings.PowerPlant,
        ContentIds.Buildings.Pylon,
        ContentIds.Buildings.Barracks,
        ContentIds.Buildings.ExtractorRefinery,
        ContentIds.Buildings.DefenseTower
    ];

    private static readonly string[] TrainHotkeyOrder =
    [
        ContentIds.Units.Worker,
        ContentIds.Units.Cadet,
        ContentIds.Units.Rifleman,
        ContentIds.Units.Guardian,
        ContentIds.Units.Rover
    ];

    private readonly Dictionary<int, GreyboxBuilding> _buildingViews = [];
    private readonly Dictionary<int, GreyboxSimUnit> _simUnitViews = [];
    private readonly List<ResourceWellView> _resourceWellViews = [];
    private readonly HashSet<string> _availableUnitIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _availableBuildingIds = new(StringComparer.Ordinal);

    private ContentCatalog? _catalog;
    private LocalizationCatalog? _localization;
    private RtsSimulation? _simulation;
    private Camera2D? _camera;
    private Control? _uiLayoutRoot;
    private Panel? _statusPanel;
    private Label? _statusLabel;
    private CommandPanelView? _commandPanel;
    private Panel? _missionResultPanel;
    private Label? _missionResultLabel;
    private PlacementGhost? _placementGhost;
    private EnergyWallView? _energyWallView;
    private FogOfWarView? _fogOfWarView;
    private SelectionBoxView? _selectionBoxView;
    private Node2D? _worldRoot;
    private readonly HashSet<int> _selectedUnitEntityIds = [];
    private int? _selectedBuildingEntityId;
    private string? _placementBuildingId;
    private bool _leftMouseSelecting;
    private Vector2 _selectionStartWorld;
    private float _uiScale = DefaultUiScale;
    private Vector2 _lastViewportSize;
    private string _lastActionMessage = string.Empty;

    public override void _Ready()
    {
        var gameRoot = ProjectSettings.GlobalizePath("res://");
        _catalog = ContentCatalog.LoadFromGameData(gameRoot);
        _localization = LocalizationCatalog.LoadFromGameData(gameRoot);
        _lastActionMessage = L("ui.action.initial_hint");
        _worldRoot = GetNode<Node2D>("WorldRoot");

        SetupSimulation();
        SetupCamera();
        SetupHud();
        SetupMissionResultOverlay();
        SyncWorldViews();
        SetupEnergyWallView();
        SetupFogOfWarView();
        SetupSelectionBoxView();
        SetupPlacementGhost();
        UpdateHud();

        GD.Print($"{GameInfo.Title} scaffold ready. Mission target: {ContentIds.Missions.FirstLanding}");
    }

    public override void _Process(double delta)
    {
        HandleCameraPan(delta);
        ApplyUiScaleIfViewportChanged();

        if (_simulation is not null)
        {
            _simulation.Tick((float)delta);
            SyncWorldViews();
            PollPlayerKnowledgeAlerts();
        }

        UpdatePlacementGhost();
        UpdateHud();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            var upgradeHotkeysFirst = IsSelectedBuilding(ContentIds.Buildings.DefenseTower);

            if (HandleUiScaleHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (HandleDebugHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (upgradeHotkeysFirst && HandleUpgradeHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (HandleProductionHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (!upgradeHotkeysFirst && HandleUpgradeHotkey(keyEvent.Keycode))
            {
                return;
            }

            HandleBuildHotkey(keyEvent.Keycode);
            return;
        }

        if (inputEvent is InputEventMouseMotion)
        {
            if (_leftMouseSelecting)
            {
                _selectionBoxView?.UpdateEnd(GetGlobalMousePosition());
            }

            return;
        }

        if (inputEvent is not InputEventMouseButton mouseButton)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (mouseButton.Pressed)
            {
                if (_placementBuildingId is not null)
                {
                    TryPlaceSelectedBuilding(GetGlobalMousePosition());
                    return;
                }

                BeginSelection(GetGlobalMousePosition());
                return;
            }

            CompleteSelection(GetGlobalMousePosition());
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right && mouseButton.Pressed)
        {
            if (_placementBuildingId is not null)
            {
                CancelPlacementMode();
                return;
            }

            CommandSelectedUnits(GetGlobalMousePosition());
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelUp && mouseButton.Pressed)
        {
            AdjustZoom(-0.1f);
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown && mouseButton.Pressed)
        {
            AdjustZoom(0.1f);
        }
    }

    private void SetupSimulation()
    {
        if (_catalog is null)
        {
            return;
        }

        var mission = _catalog.GetMission(ContentIds.Missions.FirstLanding);
        _availableUnitIds.Clear();
        _availableBuildingIds.Clear();
        foreach (var unitId in mission.AvailableUnitIds)
        {
            _availableUnitIds.Add(unitId);
        }

        foreach (var buildingId in mission.AvailableBuildingIds)
        {
            _availableBuildingIds.Add(buildingId);
        }

        var startingMaterials = mission.PlayerStartingResources.TryGetValue(ContentIds.Resources.Materials, out var materials)
            ? materials
            : 0;
        var enemyStartingMaterials = mission.EnemyStartingResources.TryGetValue(ContentIds.Resources.Materials, out var enemyMaterials)
            ? enemyMaterials
            : 0;

        var markers = mission.Markers.ToDictionary(marker => marker.Id, marker => marker.Position, StringComparer.Ordinal);
        var wellPlacements = mission.ResourceWellPlacements.Count > 0
            ? mission.ResourceWellPlacements
                .Select(placement => (placement.WellId, ResolveMissionPosition(markers, placement.MarkerId, placement.Offset)))
                .ToArray()
            : mission.ResourceWellIds
                .Select((wellId, index) => (wellId, index == 0 ? new SimVector2(-350, 170) : new SimVector2(220, 30)))
                .ToArray();

        _simulation = new RtsSimulation(
            _catalog,
            startingMaterials,
            wellPlacements,
            enemyStartingMaterials,
            EnemyAiMarkers.FromMission(mission),
            mission.EnemyAiProfile,
            mission.AvailableUnitIds);

        foreach (var entity in mission.StartingEntities)
        {
            var position = ResolveMissionPosition(markers, entity.MarkerId, entity.Offset);
            if (entity.ContentId.StartsWith("building_", StringComparison.Ordinal))
            {
                _simulation.AddStartingBuilding(entity.ContentId, position, entity.FactionId);
            }
            else if (entity.ContentId.StartsWith("unit_", StringComparison.Ordinal))
            {
                _simulation.AddUnit(entity.ContentId, entity.FactionId, position);
            }
        }
    }

    private static SimVector2 ResolveMissionPosition(IReadOnlyDictionary<string, SimVector2> markers, string markerId, SimVector2 offset)
    {
        return markers.TryGetValue(markerId, out var marker)
            ? marker + offset
            : offset;
    }

    private void SetupCamera()
    {
        _camera = new Camera2D
        {
            Name = "GreyboxCamera",
            Position = new Vector2(140, 20),
            Zoom = new Vector2(1.0f, 1.0f),
            Enabled = true
        };
        AddChild(_camera);
        _camera.MakeCurrent();
    }

    private void SetupHud()
    {
        var uiRoot = GetNode<CanvasLayer>("UiRoot");
        _uiLayoutRoot = new Control
        {
            Name = "HudRoot",
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        _uiLayoutRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        uiRoot.AddChild(_uiLayoutRoot);

        _statusPanel = new Panel
        {
            Name = "StatusPanel",
            Position = new Vector2(16, 16)
        };
        _statusPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.9f);
        _uiLayoutRoot.AddChild(_statusPanel);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Text = string.Empty,
            Position = new Vector2(14, 10),
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _statusPanel.AddChild(_statusLabel);

        _commandPanel = new CommandPanelView
        {
            Name = "CommandPanel",
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.92f)
        };
        _uiLayoutRoot.AddChild(_commandPanel);
        ApplyUiScale();
    }

    private void SetupPlacementGhost()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _placementGhost = new PlacementGhost
        {
            Name = "PlacementGhost",
            Visible = false,
            ZIndex = 3
        };
        _worldRoot.AddChild(_placementGhost);
    }

    private void SetupEnergyWallView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _energyWallView = new EnergyWallView
        {
            Name = "EnergyWallView",
            ZIndex = -1
        };
        _worldRoot.AddChild(_energyWallView);
    }

    private void SetupFogOfWarView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _fogOfWarView = new FogOfWarView
        {
            Name = "FogOfWarView",
            ZIndex = 20
        };
        _worldRoot.AddChild(_fogOfWarView);
    }

    private void SetupSelectionBoxView()
    {
        if (_worldRoot is null)
        {
            return;
        }

        _selectionBoxView = new SelectionBoxView
        {
            Name = "SelectionBoxView",
            ZIndex = 30
        };
        _worldRoot.AddChild(_selectionBoxView);
    }

    private void HandleBuildHotkey(Key keycode)
    {
        var index = keycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            _ => -1
        };

        if (index < 0)
        {
            return;
        }

        EnterPlacementMode(BuildHotkeyOrder[index]);
    }

    private bool HandleUiScaleHotkey(Key keycode)
    {
        if (keycode == Key.F9)
        {
            SetUiScale(_uiScale - 0.2f);
            return true;
        }

        if (keycode == Key.F10)
        {
            SetUiScale(_uiScale + 0.2f);
            return true;
        }

        if (keycode == Key.F8)
        {
            SetUiScale(DefaultUiScale);
            return true;
        }

        return false;
    }

    private bool IsSelectedBuilding(string buildingId)
    {
        if (_simulation is null || _selectedBuildingEntityId is null)
        {
            return false;
        }

        return _simulation.Buildings.Any(building =>
            building.EntityId == _selectedBuildingEntityId.Value &&
            !building.IsDestroyed &&
            building.Definition.Id == buildingId);
    }

    private bool HandleProductionHotkey(Key keycode)
    {
        var index = keycode switch
        {
            Key.Q => 0,
            Key.W => 1,
            Key.E => 2,
            Key.R => 3,
            Key.T => 4,
            _ => -1
        };

        if (index < 0)
        {
            return false;
        }

        if (_simulation is null || _selectedBuildingEntityId is null)
        {
            _lastActionMessage = L("ui.action.select_barracks_before_training");
            return true;
        }

        var unitId = TrainHotkeyOrder[index];
        if (!IsUnitCommandAvailable(unitId))
        {
            _lastActionMessage = L("ui.action.command_unavailable_mission");
            return true;
        }

        var result = _simulation.TryQueueUnit(unitId, _selectedBuildingEntityId.Value);
        _lastActionMessage = LocalizedProduction(result);
        return true;
    }

    private bool HandleUpgradeHotkey(Key keycode)
    {
        var upgradeId = keycode switch
        {
            Key.G => ContentIds.Buildings.GunTower,
            Key.T => ContentIds.Buildings.RocketTower,
            _ => null
        };

        if (upgradeId is null)
        {
            return false;
        }

        if (!IsBuildingCommandAvailable(upgradeId))
        {
            _lastActionMessage = L("ui.action.command_unavailable_mission");
            return true;
        }

        if (_simulation is null || _selectedBuildingEntityId is null)
        {
            _lastActionMessage = L("ui.action.select_tower_before_upgrading");
            return true;
        }

        var result = _simulation.TryUpgradeBuilding(_selectedBuildingEntityId.Value, upgradeId);
        _lastActionMessage = LocalizedUpgrade(result);
        return true;
    }

    private void SetUiScale(float scale)
    {
        _uiScale = Mathf.Clamp(scale, MinUiScale, MaxUiScale);
        ApplyUiScale();
        _lastActionMessage = L("ui.action.ui_scale_set", SimulationMessage.Args(("scale", $"{_uiScale:0.0}")));
    }

    private void ApplyUiScale()
    {
        if (_statusPanel is null || _statusLabel is null)
        {
            return;
        }

        _statusPanel.Size = new Vector2(660, 144) * _uiScale;
        _statusLabel.Size = new Vector2(636, 124) * _uiScale;
        _statusLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(HudBaseFontSize * _uiScale));
        if (_commandPanel is not null)
        {
            _lastViewportSize = GetSafeHudSize();
            _commandPanel.ApplyUiScale(_uiScale, HudBaseFontSize, _lastViewportSize);
        }

        ApplyMissionResultScale();
    }

    private void ApplyUiScaleIfViewportChanged()
    {
        var viewportSize = GetSafeHudSize();
        if (viewportSize == _lastViewportSize)
        {
            return;
        }

        ApplyUiScale();
    }

    private void EnterPlacementMode(string buildingId)
    {
        if (_catalog is null)
        {
            return;
        }

        if (!IsBuildingCommandAvailable(buildingId))
        {
            _lastActionMessage = L("ui.action.command_unavailable_mission");
            return;
        }

        if (!SelectedUnits().Any(unit => unit.Definition.CanConstruct))
        {
            _lastActionMessage = L("ui.action.select_worker_before_building");
            return;
        }

        var definition = _catalog.GetBuilding(buildingId);
        _placementBuildingId = buildingId;
        _lastActionMessage = L(
            "ui.action.placing_building",
            SimulationMessage.Args(("building", BuildingName(definition))));
    }

    private void CancelPlacementMode()
    {
        _placementBuildingId = null;
        _placementGhost?.Clear();
        _lastActionMessage = L("ui.action.placement_cancelled");
    }

    private void TryPlaceSelectedBuilding(Vector2 worldPosition)
    {
        if (_simulation is null || _placementBuildingId is null)
        {
            return;
        }

        if (!SelectedUnits().Any(unit => unit.Definition.CanConstruct))
        {
            _lastActionMessage = L("ui.action.select_worker_before_building");
            return;
        }

        var result = _simulation.TryPlaceBuilding(_placementBuildingId, ToSim(worldPosition));
        _lastActionMessage = LocalizedPlacement(result);

        if (result.Success)
        {
            _placementBuildingId = null;
            _placementGhost?.Clear();
            SyncWorldViews();
        }
    }

    private void SyncWorldViews()
    {
        if (_simulation is null || _worldRoot is null)
        {
            return;
        }

        foreach (var building in _simulation.Buildings)
        {
            if (!_buildingViews.TryGetValue(building.EntityId, out var view))
            {
                view = new GreyboxBuilding
                {
                    Name = $"Building_{building.EntityId}_{building.Definition.Id}",
                    ZIndex = -1
                };
                _worldRoot.AddChild(view);
                view.Initialize(building, _localization);
                _buildingViews.Add(building.EntityId, view);
            }
            else
            {
                view.UpdateFromState(building);
            }

            view.Visible = !building.IsDestroyed &&
                (building.FactionId != ContentIds.Factions.PrivateMilitary ||
                    _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position));
        }

        while (_resourceWellViews.Count < _simulation.ResourceWells.Count)
        {
            var well = _simulation.ResourceWells[_resourceWellViews.Count];
            var view = new ResourceWellView
            {
                Name = well.Definition.Id,
                ZIndex = -2
            };
            _worldRoot.AddChild(view);
            view.Initialize(well);
            _resourceWellViews.Add(view);
        }

        for (var index = 0; index < _resourceWellViews.Count; index++)
        {
            _resourceWellViews[index].UpdateFromState(_simulation.ResourceWells[index]);
        }

        _selectedUnitEntityIds.RemoveWhere(unitId => !_simulation.Units.Any(unit => unit.EntityId == unitId && !unit.IsDestroyed));
        foreach (var unit in _simulation.Units)
        {
            if (!_simUnitViews.TryGetValue(unit.EntityId, out var view))
            {
                view = new GreyboxSimUnit
                {
                    Name = $"SimUnit_{unit.EntityId}_{unit.Definition.Id}",
                    ZIndex = 2
                };
                _worldRoot.AddChild(view);
                view.Initialize(unit, _localization);
                _simUnitViews.Add(unit.EntityId, view);
            }
            else
            {
                view.UpdateFromState(unit);
            }

            view.Visible = !unit.IsDestroyed &&
                (unit.FactionId != ContentIds.Factions.PrivateMilitary ||
                    _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, unit.Position));
            view.SetSelected(_selectedUnitEntityIds.Contains(unit.EntityId));
        }

        _energyWallView?.UpdateSegments(_simulation.EnergyWalls);
        _fogOfWarView?.UpdateFromState(_simulation.PlayerFog);
    }

    private void UpdatePlacementGhost()
    {
        if (_catalog is null || _simulation is null || _placementGhost is null)
        {
            return;
        }

        if (_placementBuildingId is null)
        {
            _placementGhost.Clear();
            return;
        }

        var position = GetGlobalMousePosition();
        var definition = _catalog.GetBuilding(_placementBuildingId);
        var validation = _simulation.ValidatePlacement(_placementBuildingId, ToSim(position));
        _placementGhost.SetPreview(definition, position, validation.IsLegal);
    }

    private void UpdateHud()
    {
        if (_statusLabel is null || _simulation is null)
        {
            return;
        }

        var placementLine = L("ui.hud.build_line");
        if (_placementBuildingId is not null && _catalog is not null)
        {
            var definition = _catalog.GetBuilding(_placementBuildingId);
            var validation = _simulation.ValidatePlacement(_placementBuildingId, ToSim(GetGlobalMousePosition()));
            placementLine = L(
                "ui.hud.placing_line",
                SimulationMessage.Args(
                    ("building", BuildingName(definition)),
                    ("cost", definition.Cost),
                    ("reason", LocalizedPlacement(validation))));
        }

        var powered = _simulation.Buildings.Count(building => building.IsPowered);
        _statusLabel.Text =
            L(
                "ui.hud.status_line",
                SimulationMessage.Args(
                    ("materials", $"{_simulation.Materials:0}"),
                    ("buildings", _simulation.Buildings.Count),
                    ("powered", powered),
                    ("walls", _simulation.EnergyWalls.Count))) + "\n" +
            L("ui.hud.mission_line", SimulationMessage.Args(("status", _simulation.MissionState.Status), ("objective", LocalizedMissionText(_simulation.MissionState)))) + "\n" +
            GetCommanderHudLine() + "\n" +
            L("ui.hud.alert_line", SimulationMessage.Args(("alerts", GetAlertSummary()))) + "\n" +
            L("ui.hud.scale_line", SimulationMessage.Args(("scale", $"{_uiScale:0.0}"))) + "\n" +
            $"{placementLine} | {_lastActionMessage}";
        UpdateCommandPanel();
        UpdateMissionResultOverlay();
    }

    private void UpdateCommandPanel()
    {
        if (_commandPanel is null || _simulation is null || _catalog is null)
        {
            return;
        }

        var selectedUnits = SelectedUnits().ToArray();
        var selectedBuilding = _selectedBuildingEntityId is null
            ? null
            : _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
        var hasBuilder = selectedUnits.Any(unit => unit.Definition.CanConstruct);
        var selectedBarracks = selectedBuilding is not null && selectedBuilding.Definition.Id == ContentIds.Buildings.Barracks;
        var selectedDefenseTower = selectedBuilding is not null && selectedBuilding.Definition.Id == ContentIds.Buildings.DefenseTower;
        var actions = new List<CommandPanelAction>();

        actions.AddRange(BuildHotkeyOrder
            .Where(IsBuildingCommandAvailable)
            .Select((buildingId, index) =>
            {
                var definition = _catalog.GetBuilding(buildingId);
                var enabled = hasBuilder;
                var hint = enabled
                    ? L("ui.command.place_building", SimulationMessage.Args(("building", BuildingName(definition))))
                    : L("ui.command.requires_worker");
                return new CommandPanelAction(
                    $"{index + 1} {BuildingShortName(definition)}",
                    BuildingDetail(definition, hint),
                    enabled,
                    () => EnterPlacementMode(buildingId),
                    BuildingIcon(definition.Id),
                    L("ui.action_bar.cost", SimulationMessage.Args(("cost", definition.Cost))));
            }));

        actions.AddRange(TrainHotkeyOrder
            .Where(IsUnitCommandAvailable)
            .Select(unitId =>
            {
                var unit = _catalog.GetUnit(unitId);
                var validation = selectedBarracks
                    ? _simulation.ValidateUnitProduction(unitId, selectedBuilding!.EntityId)
                    : null;
                var enabled = validation?.CanQueue == true;
                var hint = selectedBarracks
                    ? LocalizedProduction(validation!)
                    : L("ui.command.select_barracks_for_training");
                return new CommandPanelAction(
                    $"{GetTrainHotkeyLabel(unitId)} {UnitShortName(unit)}",
                    UnitDetail(unit, hint),
                    enabled,
                    () =>
                    {
                        if (_selectedBuildingEntityId is null)
                        {
                            _lastActionMessage = L("ui.action.select_barracks_before_training");
                            return;
                        }

                        var result = _simulation.TryQueueUnit(unitId, _selectedBuildingEntityId.Value);
                        _lastActionMessage = LocalizedProduction(result);
                    },
                    UnitIcon(unit.Id),
                    TrainingCostLabel(unit, selectedBuilding));
            }));

        if (selectedDefenseTower)
        {
            actions.AddRange(new[] { ContentIds.Buildings.GunTower, ContentIds.Buildings.RocketTower }
                .Where(IsBuildingCommandAvailable)
                .Select(upgradeId =>
                {
                    var upgrade = _catalog.GetBuilding(upgradeId);
                    var key = upgradeId == ContentIds.Buildings.GunTower ? "G" : "T";
                    var validation = _simulation.ValidateBuildingUpgrade(selectedBuilding!.EntityId, upgradeId);
                    return new CommandPanelAction(
                        $"{key} {BuildingShortName(upgrade)}",
                        BuildingDetail(upgrade, LocalizedUpgrade(validation)),
                        validation.Success,
                        () =>
                        {
                            var result = _simulation.TryUpgradeBuilding(selectedBuilding.EntityId, upgradeId);
                            _lastActionMessage = LocalizedUpgrade(result);
                        },
                        BuildingIcon(upgrade.Id),
                        L("ui.action_bar.cost", SimulationMessage.Args(("cost", upgrade.Cost))));
                }));
        }

        if (selectedUnits.Length > 0)
        {
            _commandPanel.UpdateActions(
                L("ui.action_bar.title_unit_selection", SimulationMessage.Args(("count", selectedUnits.Length))),
                actions.ToArray(),
                hasBuilder ? L("ui.command.worker_selection_hint") : L("ui.command.combat_selection_hint"));
            return;
        }

        if (selectedBuilding is not null)
        {
            var hint = selectedBarracks
                ? L("ui.command.barracks_hint")
                : selectedDefenseTower
                    ? L("ui.command.defense_tower_hint")
                    : L("ui.command.no_direct_commands");
            _commandPanel.UpdateActions(BuildingName(selectedBuilding.Definition), actions.ToArray(), hint);
            return;
        }

        _commandPanel.UpdateActions(L("ui.action_bar.title_no_selection"), actions.ToArray(), L("ui.action_bar.no_selection_hint"));
    }

    private string GetSelectionHudLine()
    {
        if (_simulation is null || _catalog is null)
        {
            return string.Empty;
        }

        var selectedUnits = SelectedUnits().ToArray();
        if (selectedUnits.Length > 0)
        {
            var builders = selectedUnits.Count(unit => unit.Definition.CanConstruct);
            var combat = selectedUnits.Count(unit => unit.Definition.CanAttack);
            return builders > 0
                ? L("ui.selection.units_with_builders", SimulationMessage.Args(("count", selectedUnits.Length), ("builders", builders), ("combat", combat)))
                : L("ui.selection.units_combat", SimulationMessage.Args(("count", selectedUnits.Length), ("combat", combat)));
        }

        if (_selectedBuildingEntityId is null)
        {
            return L(
                "ui.selection.no_selection_training_hint",
                SimulationMessage.Args(("commands", GetTrainingCommandSummary())));
        }

        var building = _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
        if (building is null)
        {
            return string.Empty;
        }

        if (building.Definition.Id == ContentIds.Buildings.Barracks)
        {
            var worker = LocalizedProduction(_simulation.ValidateUnitProduction(ContentIds.Units.Worker, building.EntityId));
            return L(
                "ui.selection.barracks",
                SimulationMessage.Args(("commands", GetTrainingCommandSummary()), ("workerReason", worker)));
        }

        if (building.Definition.Id == ContentIds.Buildings.DefenseTower)
        {
            return L("ui.selection.defense_tower");
        }

        return L("ui.selection.building", SimulationMessage.Args(("building", BuildingName(building.Definition))));
    }

    private string UnitDetail(Stratezone.Simulation.Content.UnitDefinition unit, string commandHint)
    {
        var combatLine = unit.CanAttack
            ? L(
                "ui.detail.unit_combat",
                SimulationMessage.Args(
                    ("damage", $"{unit.AttackDamage:0}"),
                    ("range", $"{unit.AttackRange:0.0}"),
                    ("cooldown", $"{unit.AttackCooldown:0.0}")))
            : L("ui.detail.unit_noncombat");
        return L(
            "ui.detail.unit",
            SimulationMessage.Args(
                ("unit", UnitName(unit)),
                ("cost", unit.Cost),
                ("health", unit.Health),
                ("speed", $"{unit.MovementSpeed:0.0}"),
                ("train", $"{unit.TrainTimeSeconds:0.0}"),
                ("combat", combatLine),
                ("hint", commandHint)));
    }

    private string TrainingCostLabel(Stratezone.Simulation.Content.UnitDefinition unit, BuildingState? selectedBuilding)
    {
        var cost = L("ui.action_bar.cost", SimulationMessage.Args(("cost", unit.Cost)));
        if (selectedBuilding is null || selectedBuilding.Definition.Id != ContentIds.Buildings.Barracks)
        {
            return cost;
        }

        var queued = _simulation?.ProductionOrders.Count(order =>
            order.ProducerBuildingEntityId == selectedBuilding.EntityId &&
            order.UnitId == unit.Id) ?? 0;
        return queued <= 0
            ? cost
            : $"{cost} | {L("ui.action_bar.queued", SimulationMessage.Args(("count", queued)))}";
    }

    private string BuildingDetail(Stratezone.Simulation.Content.BuildingDefinition building, string commandHint)
    {
        var roleLine = building.ProvidesPower
            ? L("ui.detail.building_power", SimulationMessage.Args(("radius", $"{building.PowerRadius:0.0}")))
            : building.ProvidesResourceExtraction
                ? L("ui.detail.building_extractor")
                : building.WallAnchor
                    ? L("ui.detail.building_wall", SimulationMessage.Args(("range", $"{building.WallLinkRange:0.0}")))
                    : building.AttackDamage > 0.0f
                        ? L(
                            "ui.detail.building_attack",
                            SimulationMessage.Args(
                                ("damage", $"{building.AttackDamage:0}"),
                                ("range", $"{building.AttackRange:0.0}"),
                                ("cooldown", $"{building.AttackCooldown:0.0}")))
                        : L("ui.detail.building_support");
        return L(
            "ui.detail.building",
            SimulationMessage.Args(
                ("building", BuildingName(building)),
                ("cost", building.Cost),
                ("health", building.Health),
                ("role", roleLine),
                ("hint", commandHint)));
    }

    private static string UnitIcon(string unitId)
    {
        return unitId switch
        {
            ContentIds.Units.Worker => "W",
            ContentIds.Units.Cadet => "C",
            ContentIds.Units.Rifleman => "R",
            ContentIds.Units.Guardian => "G",
            ContentIds.Units.Rover => "RV",
            _ => "U"
        };
    }

    private static string BuildingIcon(string buildingId)
    {
        return buildingId switch
        {
            ContentIds.Buildings.PowerPlant => "PWR",
            ContentIds.Buildings.Pylon => "PYL",
            ContentIds.Buildings.Barracks => "BRK",
            ContentIds.Buildings.ExtractorRefinery => "EXT",
            ContentIds.Buildings.DefenseTower => "WALL",
            ContentIds.Buildings.GunTower => "GUN",
            ContentIds.Buildings.RocketTower => "RKT",
            _ => "BLD"
        };
    }

    private void HandleCameraPan(double delta)
    {
        if (_camera is null)
        {
            return;
        }

        var direction = Vector2.Zero;

        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            direction.X -= 1;
        }
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            direction.X += 1;
        }
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            direction.Y -= 1;
        }
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            direction.Y += 1;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        var speed = 520.0f / _camera.Zoom.X;
        _camera.Position += direction.Normalized() * speed * (float)delta;
    }

    private void AdjustZoom(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        var zoomValue = Mathf.Clamp(_camera.Zoom.X + delta, 0.55f, 1.8f);
        _camera.Zoom = new Vector2(zoomValue, zoomValue);
    }

    private static SimVector2 ToSim(Vector2 vector)
    {
        return new SimVector2(vector.X, vector.Y);
    }

    private Vector2 GetSafeHudSize()
    {
        return GetViewport().GetVisibleRect().Size;
    }

}
