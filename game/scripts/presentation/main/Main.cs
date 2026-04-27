using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class Main : Node2D
{
    private readonly List<GreyboxUnit> _units = [];
    private ContentCatalog? _catalog;
    private Camera2D? _camera;
    private Label? _statusLabel;
    private GreyboxUnit? _selectedUnit;

    public override void _Ready()
    {
        _catalog = ContentCatalog.LoadFromGameData(ProjectSettings.GlobalizePath("res://"));
        SetupCamera();
        SetupHud();
        SpawnGreyboxUnits();
        UpdateStatus("No unit selected. Left click a unit, right click to move.");
        GD.Print($"{GameInfo.Title} scaffold ready. Mission target: {ContentIds.Missions.FirstLanding}");
    }

    public override void _Process(double delta)
    {
        HandleCameraPan(delta);
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.Left)
        {
            SelectUnitAt(GetGlobalMousePosition());
        }
        else if (mouseButton.ButtonIndex == MouseButton.Right)
        {
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
        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Text = string.Empty,
            Position = new Vector2(16, 16)
        };
        uiRoot.AddChild(_statusLabel);
    }

    private void SpawnGreyboxUnits()
    {
        if (_catalog is null)
        {
            return;
        }

        var worldRoot = GetNode<Node2D>("WorldRoot");
        AddUnit(worldRoot, ContentIds.Units.Worker, new Vector2(-180, -60), new Color(0.24f, 0.7f, 0.35f));
        AddUnit(worldRoot, ContentIds.Units.Rifleman, new Vector2(-110, -25), new Color(0.28f, 0.62f, 0.95f));
        AddUnit(worldRoot, ContentIds.Units.Guardian, new Vector2(-40, 15), new Color(0.45f, 0.55f, 1.0f));
        AddUnit(worldRoot, ContentIds.Units.Rover, new Vector2(-10, 90), new Color(0.88f, 0.74f, 0.28f));
        AddUnit(worldRoot, ContentIds.Units.Commander, new Vector2(-210, 40), new Color(0.95f, 0.4f, 0.3f));
    }

    private void AddUnit(Node parent, string unitId, Vector2 position, Color color)
    {
        if (_catalog is null)
        {
            return;
        }

        var unit = new GreyboxUnit
        {
            Name = unitId
        };
        unit.Initialize(_catalog.GetUnit(unitId), position, color);
        parent.AddChild(unit);
        _units.Add(unit);
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
            UpdateStatus("No unit selected. Left click a unit, right click to move.");
            return;
        }

        _selectedUnit.SetSelected(true);
        var definition = _selectedUnit.Definition;
        UpdateStatus($"Selected {definition.DisplayName} ({definition.Id}) | HP {definition.Health} | Speed {definition.MovementSpeed:0.00}");
    }

    private void MoveSelectedUnit(Vector2 worldPosition)
    {
        if (_selectedUnit is null)
        {
            UpdateStatus("No unit selected. Left click a unit first.");
            return;
        }

        _selectedUnit.SetMoveTarget(worldPosition);
        var definition = _selectedUnit.Definition;
        UpdateStatus($"Moving {definition.DisplayName} to {worldPosition.X:0}, {worldPosition.Y:0}");
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

    private void UpdateStatus(string text)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = text;
        }
    }
}
