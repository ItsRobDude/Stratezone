using Godot;
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
        ContentIds.Units.Rifleman,
        ContentIds.Units.Guardian,
        ContentIds.Units.Rover
    ];

    private readonly Dictionary<int, GreyboxBuilding> _buildingViews = [];
    private readonly Dictionary<int, GreyboxSimUnit> _simUnitViews = [];
    private readonly List<ResourceWellView> _resourceWellViews = [];

    private ContentCatalog? _catalog;
    private RtsSimulation? _simulation;
    private Camera2D? _camera;
    private Panel? _statusPanel;
    private Label? _statusLabel;
    private CommandPanelView? _commandPanel;
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
    private string _lastActionMessage = "Left click Worker or a building. Worker builds with 1-5.";

    public override void _Ready()
    {
        _catalog = ContentCatalog.LoadFromGameData(ProjectSettings.GlobalizePath("res://"));
        _worldRoot = GetNode<Node2D>("WorldRoot");

        SetupSimulation();
        SetupCamera();
        SetupHud();
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

        if (_simulation is not null)
        {
            _simulation.Tick((float)delta);
            SyncWorldViews();
        }

        UpdatePlacementGhost();
        UpdateHud();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (HandleUiScaleHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (HandleProductionHotkey(keyEvent.Keycode))
            {
                return;
            }

            if (HandleUpgradeHotkey(keyEvent.Keycode))
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
        var startingMaterials = mission.PlayerStartingResources.TryGetValue(ContentIds.Resources.Materials, out var materials)
            ? materials
            : 0;
        var enemyStartingMaterials = mission.EnemyStartingResources.TryGetValue(ContentIds.Resources.Materials, out var enemyMaterials)
            ? enemyMaterials
            : 0;

        var wellPlacements = new List<(string, SimVector2)>
        {
            ("well_first_landing_start", new SimVector2(-350, 170)),
            ("well_first_landing_central", new SimVector2(220, 30))
        };

        _simulation = new RtsSimulation(_catalog, startingMaterials, wellPlacements, enemyStartingMaterials);
        _simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, new SimVector2(-300, -140));
        _simulation.AddStartingBuilding(ContentIds.Buildings.ColonyHub, RtsSimulation.EnemyHubPosition, ContentIds.Factions.PrivateMilitary);
        _simulation.AddStartingBuilding(ContentIds.Buildings.PowerPlant, RtsSimulation.EnemyPowerPlantPosition, ContentIds.Factions.PrivateMilitary);
        _simulation.AddStartingBuilding(ContentIds.Buildings.Barracks, RtsSimulation.EnemyBarracksPosition, ContentIds.Factions.PrivateMilitary);
        _simulation.AddUnit(ContentIds.Units.Worker, ContentIds.Factions.PlayerExpedition, new SimVector2(-180, -60));
        _simulation.AddUnit(ContentIds.Units.Rifleman, ContentIds.Factions.PlayerExpedition, new SimVector2(-110, -25));
        _simulation.AddUnit(ContentIds.Units.Guardian, ContentIds.Factions.PlayerExpedition, new SimVector2(-40, 15));
        _simulation.AddUnit(ContentIds.Units.Rover, ContentIds.Factions.PlayerExpedition, new SimVector2(-10, 90));
        _simulation.AddUnit(ContentIds.Units.Commander, ContentIds.Factions.PlayerExpedition, new SimVector2(-210, 40));
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
        _statusPanel = new Panel
        {
            Name = "StatusPanel",
            Position = new Vector2(16, 16)
        };
        _statusPanel.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.9f);
        uiRoot.AddChild(_statusPanel);

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
            Position = new Vector2(16, 262),
            Modulate = new Color(1.0f, 1.0f, 1.0f, 0.92f)
        };
        uiRoot.AddChild(_commandPanel);
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

    private bool HandleProductionHotkey(Key keycode)
    {
        var index = keycode switch
        {
            Key.Q => 0,
            Key.W => 1,
            Key.E => 2,
            Key.R => 3,
            _ => -1
        };

        if (index < 0)
        {
            return false;
        }

        if (_simulation is null || _selectedBuildingEntityId is null)
        {
            _lastActionMessage = "Select a Barracks before training units.";
            return true;
        }

        var unitId = TrainHotkeyOrder[index];
        var result = _simulation.TryQueueUnit(unitId, _selectedBuildingEntityId.Value);
        _lastActionMessage = result.Message;
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

        if (_simulation is null || _selectedBuildingEntityId is null)
        {
            _lastActionMessage = "Select a Defense Tower before upgrading.";
            return true;
        }

        var result = _simulation.TryUpgradeBuilding(_selectedBuildingEntityId.Value, upgradeId);
        _lastActionMessage = result.Message;
        return true;
    }

    private void SetUiScale(float scale)
    {
        _uiScale = Mathf.Clamp(scale, MinUiScale, MaxUiScale);
        ApplyUiScale();
        _lastActionMessage = $"UI scale set to {_uiScale:0.0}x.";
    }

    private void ApplyUiScale()
    {
        if (_statusPanel is null || _statusLabel is null)
        {
            return;
        }

        _statusPanel.Size = new Vector2(660, 150) * _uiScale;
        _statusLabel.Size = new Vector2(636, 130) * _uiScale;
        _statusLabel.AddThemeFontSizeOverride("font_size", Mathf.RoundToInt(HudBaseFontSize * _uiScale));
        if (_commandPanel is not null)
        {
            _commandPanel.Position = new Vector2(16, 262) * _uiScale;
            _commandPanel.ApplyUiScale(_uiScale, HudBaseFontSize);
        }
    }

    private void EnterPlacementMode(string buildingId)
    {
        if (_catalog is null)
        {
            return;
        }

        if (!SelectedUnits().Any(unit => unit.Definition.CanConstruct))
        {
            _lastActionMessage = "Select a Worker before building.";
            return;
        }

        var definition = _catalog.GetBuilding(buildingId);
        _placementBuildingId = buildingId;
        _lastActionMessage = $"Placing {definition.DisplayName}. Left click to place, right click to cancel.";
    }

    private void CancelPlacementMode()
    {
        _placementBuildingId = null;
        _placementGhost?.Clear();
        _lastActionMessage = "Placement cancelled.";
    }

    private void TryPlaceSelectedBuilding(Vector2 worldPosition)
    {
        if (_simulation is null || _placementBuildingId is null)
        {
            return;
        }

        if (!SelectedUnits().Any(unit => unit.Definition.CanConstruct))
        {
            _lastActionMessage = "Select a Worker before building.";
            return;
        }

        var result = _simulation.TryPlaceBuilding(_placementBuildingId, ToSim(worldPosition));
        _lastActionMessage = result.Message;

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
                view.Initialize(building);
                _buildingViews.Add(building.EntityId, view);
            }
            else
            {
                view.UpdateFromState(building);
            }

            view.Visible = building.FactionId != ContentIds.Factions.PrivateMilitary ||
                _simulation.IsVisibleToFaction(ContentIds.Factions.PlayerExpedition, building.Position);
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
                view.Initialize(unit);
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

        var placementLine = "Build: 1 Power | 2 Pylon | 3 Barracks | 4 Extractor | 5 Wall";
        if (_placementBuildingId is not null && _catalog is not null)
        {
            var definition = _catalog.GetBuilding(_placementBuildingId);
            var validation = _simulation.ValidatePlacement(_placementBuildingId, ToSim(GetGlobalMousePosition()));
            placementLine = $"Placing {definition.DisplayName} ({definition.Cost} materials): {validation.Reason}";
        }

        var powered = _simulation.Buildings.Count(building => building.IsPowered);
        var selectionLine = GetSelectionHudLine();
        _statusLabel.Text =
            $"Materials: {_simulation.Materials:0} | Enemy: {_simulation.EnemyMaterials:0} | Buildings: {_simulation.Buildings.Count} | Powered: {powered}/{_simulation.Buildings.Count} | Walls: {_simulation.EnergyWalls.Count}\n" +
            $"{_simulation.MissionState.Status}: {_simulation.MissionState.PrimaryText}\n" +
            $"{placementLine}\n" +
            $"{selectionLine}\n" +
            $"UI: F9 smaller | F10 larger | F8 reset ({_uiScale:0.0}x)\n" +
            _lastActionMessage;
        UpdateCommandPanel();
    }

    private void UpdateCommandPanel()
    {
        if (_commandPanel is null || _simulation is null || _catalog is null)
        {
            return;
        }

        var selectedUnits = SelectedUnits().ToArray();
        if (selectedUnits.Length > 0)
        {
            var hasBuilder = selectedUnits.Any(unit => unit.Definition.CanConstruct);
            var actions = BuildHotkeyOrder
                .Select((buildingId, index) =>
                {
                    var definition = _catalog.GetBuilding(buildingId);
                    var label = $"{index + 1} {ShortCommandName(definition.DisplayName)}";
                    var hint = hasBuilder ? $"Place {definition.DisplayName}" : "Requires selected Worker.";
                    return new CommandPanelAction(label, hint, hasBuilder, () => EnterPlacementMode(buildingId));
                })
                .ToArray();
            _commandPanel.UpdateActions(
                $"{selectedUnits.Length} Unit Selection",
                actions,
                hasBuilder ? "Worker builds with 1-5. Right click moves selected units." : "Right click moves selected units or attacks visible enemies.");
            return;
        }

        if (_selectedBuildingEntityId is not null)
        {
            var building = _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
            if (building is not null && building.Definition.Id == ContentIds.Buildings.Barracks)
            {
                var actions = TrainHotkeyOrder
                    .Select((unitId, index) =>
                    {
                        var unit = _catalog.GetUnit(unitId);
                        var key = index switch { 0 => "Q", 1 => "W", 2 => "E", _ => "R" };
                        var validation = _simulation.ValidateUnitProduction(unitId, building.EntityId);
                        return new CommandPanelAction(
                            $"{key} {ShortCommandName(unit.DisplayName)}",
                            validation.Reason,
                            validation.CanQueue,
                            () =>
                            {
                                var result = _simulation.TryQueueUnit(unitId, building.EntityId);
                                _lastActionMessage = result.Message;
                            });
                    })
                    .ToArray();
                _commandPanel.UpdateActions("Barracks", actions, "Train units from the Colony Hub. Disabled buttons show the blocked reason on hover.");
                return;
            }

            if (building is not null && building.Definition.Id == ContentIds.Buildings.DefenseTower)
            {
                var upgrades = new[] { ContentIds.Buildings.GunTower, ContentIds.Buildings.RocketTower };
                var actions = upgrades
                    .Select(upgradeId =>
                    {
                        var upgrade = _catalog.GetBuilding(upgradeId);
                        var key = upgradeId == ContentIds.Buildings.GunTower ? "G" : "T";
                        var validation = _simulation.ValidateBuildingUpgrade(building.EntityId, upgradeId);
                        return new CommandPanelAction(
                            $"{key} {ShortCommandName(upgrade.DisplayName)}",
                            validation.Message,
                            validation.Success,
                            () =>
                            {
                                var result = _simulation.TryUpgradeBuilding(building.EntityId, upgradeId);
                                _lastActionMessage = result.Message;
                            });
                    })
                    .ToArray();
                _commandPanel.UpdateActions("Defense Tower", actions, "Upgrade in place without dropping the wall link while powered.");
                return;
            }

            if (building is not null)
            {
                _commandPanel.UpdateActions(building.Definition.DisplayName, [], "No direct commands for this building yet.");
                return;
            }
        }

        _commandPanel.UpdateActions("No Selection", [], "Select units, a Barracks, or a Defense Tower.");
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
                ? $"{selectedUnits.Length} selected | {builders} builder | {combat} combat | build with 1-5."
                : $"{selectedUnits.Length} selected | {combat} combat | right click ground/enemy.";
        }

        if (_selectedBuildingEntityId is null)
        {
            return "Select Barracks to train: Q Worker | W Rifleman | E Guardian | R Rover.";
        }

        var building = _simulation.Buildings.FirstOrDefault(item => item.EntityId == _selectedBuildingEntityId.Value);
        if (building is null)
        {
            return string.Empty;
        }

        if (building.Definition.Id == ContentIds.Buildings.Barracks)
        {
            var worker = _simulation.ValidateUnitProduction(ContentIds.Units.Worker, building.EntityId).Reason;
            return $"Barracks: Q Worker | W Rifleman | E Guardian | R Rover ({worker})";
        }

        if (building.Definition.Id == ContentIds.Buildings.DefenseTower)
        {
            return "Defense Tower: G Gun Tower | T Rocket Tower.";
        }

        return $"Selected: {building.Definition.DisplayName}.";
    }

    private static string ShortCommandName(string displayName)
    {
        return displayName
            .Replace("Extractor/Refinery", "Extractor", StringComparison.Ordinal)
            .Replace("Power Plant", "Power", StringComparison.Ordinal)
            .Replace("Defense Tower", "Wall", StringComparison.Ordinal);
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
}
