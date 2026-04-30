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

    private readonly Dictionary<int, GreyboxBuilding> _buildingViews = [];
    private readonly Dictionary<int, GreyboxSimUnit> _simUnitViews = [];
    private readonly List<ResourceWellView> _resourceWellViews = [];
    private readonly List<GreyboxUnit> _units = [];

    private ContentCatalog? _catalog;
    private RtsSimulation? _simulation;
    private Camera2D? _camera;
    private Panel? _statusPanel;
    private Label? _statusLabel;
    private PlacementGhost? _placementGhost;
    private EnergyWallView? _energyWallView;
    private Node2D? _worldRoot;
    private GreyboxUnit? _selectedUnit;
    private string? _placementBuildingId;
    private float _uiScale = DefaultUiScale;
    private string _lastActionMessage = "Left click a unit. Select Worker, then press 1-5 to build.";

    public override void _Ready()
    {
        _catalog = ContentCatalog.LoadFromGameData(ProjectSettings.GlobalizePath("res://"));
        _worldRoot = GetNode<Node2D>("WorldRoot");

        SetupSimulation();
        SetupCamera();
        SetupHud();
        SyncWorldViews();
        SpawnGreyboxUnits();
        SetupEnergyWallView();
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

            HandleBuildHotkey(keyEvent.Keycode);
            return;
        }

        if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (_placementBuildingId is not null)
            {
                TryPlaceSelectedBuilding(GetGlobalMousePosition());
                return;
            }

            SelectUnitAt(GetGlobalMousePosition());
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right)
        {
            if (_placementBuildingId is not null)
            {
                CancelPlacementMode();
                return;
            }

            MoveSelectedUnit(GetGlobalMousePosition());
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            AdjustZoom(-0.1f);
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
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
    }

    private void SetupCamera()
    {
        _camera = new Camera2D
        {
            Name = "GreyboxCamera",
            Position = new Vector2(0, 0),
            Zoom = new Vector2(1.0f, 1.0f),
            Enabled = true
        };
        AddChild(_camera);
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

    private void SpawnGreyboxUnits()
    {
        if (_catalog is null || _worldRoot is null)
        {
            return;
        }

        AddUnit(_worldRoot, ContentIds.Units.Worker, new Vector2(-180, -60), new Color(0.24f, 0.7f, 0.35f));
        AddUnit(_worldRoot, ContentIds.Units.Rifleman, new Vector2(-110, -25), new Color(0.28f, 0.62f, 0.95f));
        AddUnit(_worldRoot, ContentIds.Units.Guardian, new Vector2(-40, 15), new Color(0.45f, 0.55f, 1.0f));
        AddUnit(_worldRoot, ContentIds.Units.Rover, new Vector2(-10, 90), new Color(0.88f, 0.74f, 0.28f));
        AddUnit(_worldRoot, ContentIds.Units.Commander, new Vector2(-210, 40), new Color(0.95f, 0.4f, 0.3f));
    }

    private void AddUnit(Node parent, string unitId, Vector2 position, Color color)
    {
        if (_catalog is null)
        {
            return;
        }

        var unit = new GreyboxUnit
        {
            Name = unitId,
            ZIndex = 1
        };
        unit.Initialize(_catalog.GetUnit(unitId), position, color);
        parent.AddChild(unit);
        _units.Add(unit);
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
    }

    private void EnterPlacementMode(string buildingId)
    {
        if (_catalog is null)
        {
            return;
        }

        if (_selectedUnit is null || !_selectedUnit.Definition.CanConstruct)
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

        if (_selectedUnit is null || !_selectedUnit.Definition.CanConstruct)
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

    private void SelectUnitAt(Vector2 worldPosition)
    {
        GreyboxUnit? nearest = null;
        var nearestDistance = float.MaxValue;

        foreach (var unit in _units)
        {
            var distance = unit.GlobalPosition.DistanceTo(worldPosition);
            if (distance <= unit.SelectionRadius && distance < nearestDistance)
            {
                nearest = unit;
                nearestDistance = distance;
            }
        }

        if (_selectedUnit is not null)
        {
            _selectedUnit.SetSelected(false);
        }

        _selectedUnit = nearest;

        if (_selectedUnit is null)
        {
            _lastActionMessage = "No unit selected.";
            return;
        }

        _selectedUnit.SetSelected(true);
        var definition = _selectedUnit.Definition;
        _lastActionMessage = $"Selected {definition.DisplayName} ({definition.Id}) | HP {definition.Health} | Speed {definition.MovementSpeed:0.00}";
    }

    private void MoveSelectedUnit(Vector2 worldPosition)
    {
        if (_selectedUnit is null)
        {
            _lastActionMessage = "No unit selected. Left click a unit first.";
            return;
        }

        _selectedUnit.SetMoveTarget(worldPosition);
        var definition = _selectedUnit.Definition;
        _lastActionMessage = $"Moving {definition.DisplayName} to {worldPosition.X:0}, {worldPosition.Y:0}";
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
        }

        _energyWallView?.UpdateSegments(_simulation.EnergyWalls);
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
        _statusLabel.Text =
            $"Materials: {_simulation.Materials:0} | Enemy: {_simulation.EnemyMaterials:0} | Buildings: {_simulation.Buildings.Count} | Powered: {powered}/{_simulation.Buildings.Count} | Walls: {_simulation.EnergyWalls.Count}\n" +
            $"{_simulation.ObjectiveText}\n" +
            $"{placementLine}\n" +
            $"UI: F9 smaller | F10 larger | F8 reset ({_uiScale:0.0}x)\n" +
            _lastActionMessage;
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
