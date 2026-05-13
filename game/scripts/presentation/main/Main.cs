using Godot;
using Stratezone.Localization;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;
using Stratezone.Simulation.Tools;

public partial class Main : Node2D
{
    private const float DefaultUiScale = 1.1f;
    private const float MinUiScale = 0.8f;
    private const float MaxUiScale = 2.4f;
    private const float OffscreenCullPaddingWorld = 180.0f;
    private const float HudRefreshIntervalSeconds = 0.12f;
    private const string DefaultMissionId = ContentIds.Missions.FirstLanding;
    private const string InitialMissionEnvironmentVariable = "STRATEZONE_MISSION_ID";
    private const string DebugHotkeysEnvironmentVariable = "STRATEZONE_DEBUG_HOTKEYS";

    private static readonly string[] BuildHotkeyOrder =
    [
        ContentIds.Buildings.ColonyHub,
        ContentIds.Buildings.PowerPlant,
        ContentIds.Buildings.Pylon,
        ContentIds.Buildings.Barracks,
        ContentIds.Buildings.ExtractorRefinery,
        ContentIds.Buildings.DefenseTower
    ];

    private static readonly string[] TrainHotkeyOrder =
    [
        ContentIds.Units.Grunt,
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
    private string _gameRoot = string.Empty;
    private RtsSimulation? _simulation;
    private Camera2D? _camera;
    private Control? _uiLayoutRoot;
    private Control? _gameHudRoot;
    private HudResourceBar? _hudResourceBar;
    private HudCommanderIndicator? _hudCommander;
    private Label? _hudObjective;
    private HudAlertsTicker? _hudAlertsTicker;
    private MissionBriefingOverlay? _briefingOverlay;
    private CommandPanelView? _commandPanel;
    private Panel? _missionResultPanel;
    private Label? _missionResultLabel;
    private PlacementGhost? _placementGhost;
    private EnergyWallView? _energyWallView;
    private FogOfWarView? _fogOfWarView;
    private MapRegionView? _mapRegionView;
    private MissionCalloutView? _missionCalloutView;
    private SelectionBoxView? _selectionBoxView;
    private Node2D? _worldRoot;
    private readonly HashSet<int> _selectedUnitEntityIds = [];
    private int? _selectedBuildingEntityId;
    private string? _placementBuildingId;
    private string _activeMissionId = DefaultMissionId;
    private MissionDefinition? _activeMission;
    private bool _leftMouseSelecting;
    private Vector2 _selectionStartWorld;
    private float _uiScale = DefaultUiScale;
    private Vector2 _lastViewportSize;
    private string _lastActionMessage = string.Empty;
    private float _hudRefreshElapsedSeconds = HudRefreshIntervalSeconds;
    private bool _briefingOverlayVisible;
    private bool _debugHotkeyHintsEnabled;

    public override void _Ready()
    {
        var gameRoot = ProjectSettings.GlobalizePath("res://");
        _gameRoot = gameRoot;
        _catalog = ContentCatalog.LoadFromGameData(gameRoot);
        _localization = LocalizationCatalog.LoadFromGameData(gameRoot);
        _debugHotkeyHintsEnabled = string.Equals(
            System.Environment.GetEnvironmentVariable(DebugHotkeysEnvironmentVariable),
            "1",
            StringComparison.Ordinal);
        _lastActionMessage = L("ui.action.initial_hint");
        _worldRoot = GetNode<Node2D>("WorldRoot");

        SetupSimulation(ResolveInitialMissionId());
        SetupCamera();
        SetupHud();
        SetupMissionResultOverlay();
        SetupMapRegionView();
        SetupMissionCalloutView();
        SetupMapEditorOverlay();
        SyncWorldViews();
        SetupEnergyWallView();
        SetupFogOfWarView();
        SetupSelectionBoxView();
        SetupPlacementGhost();
        UpdateHud();

        GD.Print($"{GameInfo.Title} scaffold ready. Mission target: {_activeMissionId}");
    }

    public override void _Process(double delta)
    {
        var deltaSeconds = (float)delta;
        HandleCameraPan(delta);
        ApplyUiScaleIfViewportChanged();

        if (_simulation is not null && !_mapEditorEnabled)
        {
            _simulation.Tick(deltaSeconds);
            SyncWorldViews();
            PollPlayerKnowledgeAlerts();
        }

        UpdatePlacementGhost();
        if (_mapEditorEnabled)
        {
            _mapEditorOverlay?.QueueRedraw();
        }

        _hudRefreshElapsedSeconds += deltaSeconds;
        if (_placementBuildingId is not null || _hudRefreshElapsedSeconds >= HudRefreshIntervalSeconds)
        {
            UpdateHud();
            _hudRefreshElapsedSeconds = 0.0f;
        }
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (HandleMapEditorKey(keyEvent))
            {
                return;
            }

            if (_mapEditorEnabled)
            {
                return;
            }

            if (HandleBriefingHotkey(keyEvent.Keycode))
            {
                return;
            }

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

        if (HandleMapEditorMouse(inputEvent))
        {
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

    private void SetupSimulation(string missionId)
    {
        if (_catalog is null)
        {
            return;
        }

        var mission = _catalog.GetMission(missionId);
        var map = _catalog.GetMap(mission.MapId);
        SetupSimulation(mission, map);
    }

    private void SetupSimulation(MissionDefinition mission, MapDefinition map)
    {
        if (_catalog is null)
        {
            return;
        }

        _activeMissionId = mission.Id;
        var runtime = MissionRuntimeFactory.Create(_catalog, mission, map);
        _activeMission = mission;
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

        _simulation = runtime.Simulation;
        _mapRegionView?.UpdateFromMap(runtime.Map);
        UpdateMapEditorOverlayData();
    }

    private string ResolveInitialMissionId()
    {
        var missionId = System.Environment.GetEnvironmentVariable(InitialMissionEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(missionId) ||
            _catalog?.Missions.ContainsKey(missionId) != true)
        {
            return DefaultMissionId;
        }

        return missionId!;
    }

    private void SetupHud()
    {
        _uiLayoutRoot = GetNode<Control>("UiRoot/HudRoot");
        _uiLayoutRoot.Name = "HudRoot";
        _uiLayoutRoot.MouseFilter = Control.MouseFilterEnum.Pass;
        _uiLayoutRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _gameHudRoot = new Control
        {
            Name = "GameHudRoot",
            MouseFilter = Control.MouseFilterEnum.Pass
        };
        _gameHudRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _uiLayoutRoot.AddChild(_gameHudRoot);

        _hudResourceBar = new HudResourceBar
        {
            Name = "HudResourceBar"
        };
        _gameHudRoot.AddChild(_hudResourceBar);

        _hudCommander = new HudCommanderIndicator
        {
            Name = "HudCommander"
        };
        _gameHudRoot.AddChild(_hudCommander);

        _hudObjective = new Label
        {
            Name = "HudObjective",
            ThemeTypeVariation = "LabelTitle",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _gameHudRoot.AddChild(_hudObjective);

        _hudAlertsTicker = new HudAlertsTicker
        {
            Name = "HudAlertsTicker",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _gameHudRoot.AddChild(_hudAlertsTicker);

        _commandPanel = new CommandPanelView
        {
            Name = "CommandPanel"
        };
        _gameHudRoot.AddChild(_commandPanel);

        _briefingOverlay = new MissionBriefingOverlay
        {
            Name = "MissionBriefingOverlay"
        };
        _gameHudRoot.AddChild(_briefingOverlay);
        ApplyUiScale();
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
            Key.Key6 => 5,
            _ => -1
        };

        if (index < 0)
        {
            return;
        }

        var availableBuildingIds = BuildHotkeyOrder
            .Where(IsBuildingCommandAvailable)
            .ToArray();
        if (index >= availableBuildingIds.Length)
        {
            return;
        }

        EnterPlacementMode(availableBuildingIds[index]);
    }

    private void LoadMission(string missionId)
    {
        if (_catalog is null)
        {
            return;
        }

        ClearWorldViews();
        _selectedUnitEntityIds.Clear();
        _selectedBuildingEntityId = null;
        _placementBuildingId = null;
        _placementGhost?.Clear();
        ClearPendingMapEditorSave();
        ClearMapEditorPendingDecisions();
        SetupSimulation(missionId);
        ResetCameraToMissionStart();
        _mapRegionView?.UpdateFromMap(_simulation?.Map);
        UpdateMapEditorOverlayData();
        SyncWorldViews();
        UpdateHud();
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
            Key.W => 0,
            Key.C => 1,
            Key.R => 2,
            Key.G => 3,
            Key.V => 4,
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
        if (keycode == Key.U)
        {
            if (!IsUnitCommandAvailable(ContentIds.Units.Guardian))
            {
                return false;
            }

            if (_simulation is null || _selectedBuildingEntityId is null)
            {
                _lastActionMessage = L("ui.action.select_barracks_before_retrofit");
                return true;
            }

            var retrofitResult = _simulation.TryStartGuardianRetrofit(_selectedBuildingEntityId.Value);
            _lastActionMessage = LocalizedUpgrade(retrofitResult);
            return true;
        }

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
        if (_uiLayoutRoot is null)
        {
            return;
        }

        var viewportSize = GetSafeHudSize();
        _lastViewportSize = viewportSize;
        _hudResourceBar?.ApplyUiScale(_uiScale);
        _hudCommander?.ApplyUiScale(_uiScale);
        _hudAlertsTicker?.ApplyUiScale(_uiScale);
        if (_hudObjective is not null)
        {
            _hudObjective.AnchorLeft = 0.5f;
            _hudObjective.AnchorTop = 0.0f;
            _hudObjective.AnchorRight = 0.5f;
            _hudObjective.AnchorBottom = 0.0f;
            _hudObjective.OffsetLeft = -360.0f * _uiScale;
            _hudObjective.OffsetRight = 360.0f * _uiScale;
            _hudObjective.OffsetTop = 24.0f * _uiScale;
            _hudObjective.OffsetBottom = 54.0f * _uiScale;
        }

        if (_commandPanel is not null)
        {
            _commandPanel.ApplyUiScale(_uiScale, viewportSize);
        }

        _briefingOverlay?.ApplyUiScale(_uiScale, viewportSize);
        ApplyMissionResultScale();
        ApplyMapEditorPanelScale();
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
            _lastActionMessage = L("ui.action.select_grunt_before_building");
            return;
        }

        var blockedReason = GetBuildingCommandBlockedReason(buildingId);
        if (blockedReason is not null)
        {
            _lastActionMessage = blockedReason;
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
            _lastActionMessage = L("ui.action.select_grunt_before_building");
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
        if (_simulation is null)
        {
            return;
        }

        var livePlayerBuildings = _simulation.Buildings
            .Where(building => building.FactionId == ContentIds.Factions.PlayerExpedition && !building.IsDestroyed)
            .ToArray();
        var livePlayerUnits = _simulation.Units.Count(unit =>
            unit.FactionId == ContentIds.Factions.PlayerExpedition &&
            !unit.IsDestroyed);
        _hudResourceBar?.UpdateValues(
            Mathf.RoundToInt(_simulation.Materials),
            livePlayerBuildings.Count(building => building.IsPowered),
            livePlayerBuildings.Length,
            livePlayerUnits);
        UpdateCommanderIndicator();
        if (_hudObjective is not null)
        {
            _hudObjective.Text = GetActiveObjectiveText();
        }

        _hudAlertsTicker?.UpdateAlerts(GetVisibleAlertSnapshots());
        UpdateBriefingOverlay();
        UpdateCommandPanel();
        UpdateMissionResultOverlay();
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
