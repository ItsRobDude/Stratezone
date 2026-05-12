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
    private const int HudBaseFontSize = 18;
    private const float OffscreenCullPaddingWorld = 180.0f;
    private const float HudRefreshIntervalSeconds = 0.12f;
    private const string DefaultMissionId = ContentIds.Missions.FirstLanding;
    private const string InitialMissionEnvironmentVariable = "STRATEZONE_MISSION_ID";

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
    private Panel? _statusPanel;
    private Label? _statusLabel;
    private CommandPanelView? _commandPanel;
    private Panel? _missionResultPanel;
    private Label? _missionResultLabel;
    private PlacementGhost? _placementGhost;
    private EnergyWallView? _energyWallView;
    private FogOfWarView? _fogOfWarView;
    private MapRegionView? _mapRegionView;
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

    public override void _Ready()
    {
        var gameRoot = ProjectSettings.GlobalizePath("res://");
        _gameRoot = gameRoot;
        _catalog = ContentCatalog.LoadFromGameData(gameRoot);
        _localization = LocalizationCatalog.LoadFromGameData(gameRoot);
        _lastActionMessage = L("ui.action.initial_hint");
        _worldRoot = GetNode<Node2D>("WorldRoot");

        SetupSimulation(ResolveInitialMissionId());
        SetupCamera();
        SetupHud();
        SetupMissionResultOverlay();
        SetupMapRegionView();
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
        if (_statusPanel is null || _statusLabel is null)
        {
            return;
        }

        _statusPanel.Size = new Vector2(660, 174) * _uiScale;
        _statusLabel.Size = new Vector2(636, 154) * _uiScale;
        _statusLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(HudBaseFontSize * _uiScale));
        if (_commandPanel is not null)
        {
            _lastViewportSize = GetSafeHudSize();
            _commandPanel.ApplyUiScale(_uiScale, HudBaseFontSize, _lastViewportSize);
        }

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
            GetMissionHudLine() + "\n" +
            GetMissionBriefingHudLine() + "\n" +
            GetMissionReadabilityHudLine() + "\n" +
            GetCommanderHudLine() + "\n" +
            L("ui.hud.alert_line", SimulationMessage.Args(("alerts", GetAlertSummary()))) + "\n" +
            L("ui.hud.scale_line", SimulationMessage.Args(("scale", $"{_uiScale:0.0}"))) + "\n" +
            $"{placementLine} | {_lastActionMessage}";
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
