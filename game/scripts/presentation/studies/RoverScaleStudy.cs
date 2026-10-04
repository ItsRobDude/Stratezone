using Godot;
using System;
using System.Linq;

namespace Stratezone.Presentation.Studies;

/// <summary>Opt-in meter-scale lineup; no mission, collision, or simulation changes.</summary>
public partial class RoverScaleStudy : Node3D
{
    private Camera3D _camera = null!;
    private Node3D _rover = null!;
    private Node3D _soldier = null!;
    private Node3D _radar = null!;
    private MultiMeshInstance3D _wheels = null!;
    private readonly Vector3[] _wheelCenters = new Vector3[4];
    private readonly string[] _wheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
    private Control _reference = null!;
    private Label _status = null!;
    private int _view = 2;
    private float _yaw = -25;
    private float _size = 17;
    private double _time;
    private bool _motion;
    private bool _enemy;

    public override void _Ready()
    {
        GetWindow().Title = "Stratezone — Rover / infantry / Hub scale study";
        _camera = GetNode<Camera3D>("Camera");
        _rover = GD.Load<PackedScene>("res://assets/models/studies/rover/rover_study.glb").Instantiate<Node3D>();
        _rover.Name = "Rover"; AddChild(_rover);
        _rover.Position = new Vector3(4.3f, 0, 7.4f);
        _radar = (Node3D)_rover.FindChild("Radar", true, false);
        var material = GD.Load<ShaderMaterial>("res://assets/models/studies/rover/rover_study.tres");
        foreach (var mesh in _rover.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
            mesh.MaterialOverride = material;
        // One shared wheel mesh, four rigid transforms, one wheel draw surface.
        var wheelMesh = ((MeshInstance3D)_rover.FindChild("Wheel_FR", true, false)).Mesh;
        var multi = new MultiMesh { TransformFormat = MultiMesh.TransformFormatEnum.Transform3D, Mesh = wheelMesh, InstanceCount = 4 };
        _wheels = new MultiMeshInstance3D { Name = "Wheels", Multimesh = multi, MaterialOverride = material };
        _rover.AddChild(_wheels);
        for (var i = 0; i < 4; i++)
        {
            var wheel = (MeshInstance3D)_rover.FindChild(_wheelNames[i], true, false);
            _wheelCenters[i] = _rover.ToLocal(wheel.GlobalPosition);
            wheel.Visible = false;
        }
        UpdateParts();
        _soldier = GD.Load<PackedScene>("res://assets/models/studies/rifleman/rifleman_study.glb").Instantiate<Node3D>();
        _soldier.Name = "Rifleman"; AddChild(_soldier);
        var player = new AnimationPlayer { Name = "SharedAnimations" }; _soldier.AddChild(player);
        player.AddAnimationLibrary("", GD.Load<AnimationLibrary>("res://assets/models/studies/rifleman/infantry_anims.glb"));
        player.Play("rifle_ready_idle");
        var layer = new CanvasLayer(); AddChild(layer);
        _status = new Label { Position = new Vector2(16, 16) }; _status.AddThemeFontSizeOverride("font_size", 17); layer.AddChild(_status);
        _reference = new Control { Position = new Vector2(20, 165), MouseFilter = Control.MouseFilterEnum.Ignore }; layer.AddChild(_reference);
        _reference.AddChild(new Label { Text = "Original Rover sprite", Position = Vector2.Zero });
        _reference.AddChild(new TextureRect {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = GD.Load<Texture2D>("res://assets/units/rover/directional/rover_225.png"),
            Position = new Vector2(0, 32), Size = new Vector2(325, 360),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore });
        SetView(2);
    }

    public override void _Process(double delta)
    {
        if (!_motion) return;
        _time += delta; UpdateParts();
    }

    private void UpdateParts()
    {
        for (var i = 0; i < 4; i++)
        {
            var left = _wheelCenters[i].X < 0;
            var steer = i < 2 ? Mathf.Sin((float)_time) * .22f : 0;
            var basis = new Basis(Vector3.Up, (left ? Mathf.Pi : 0) + steer)
                * new Basis(Vector3.Right, (float)_time * 2.2f * (left ? -1 : 1));
            _wheels.Multimesh.SetInstanceTransform(i, new Transform3D(basis, _wheelCenters[i]));
        }
        _radar.Rotation = new Vector3(0, (float)(_time * .28 % Math.Tau), 0);
    }

    public void SetView(int view)
    {
        _view = view;
        GetNode<Node3D>("Hub").Visible = view != 1;
        _rover.Visible = view != 3;
        _reference.Visible = view == 1;
        _soldier.Position = view == 3 ? new Vector3(.62f, .44f, 4.0f) : new Vector3(2.05f, 0, 7.9f);
        _size = view == 1 ? 7.8f : view == 3 ? 7.4f : 18.5f;
        _yaw = view == 1 ? -35 : view == 3 ? -8 : -25;
        UpdateCamera(); RefreshStatus();
    }

    public void SetYaw(float yaw) { _yaw = yaw; UpdateCamera(); }

    private void UpdateCamera()
    {
        var target = _view == 1 ? new Vector3(3.7f, 1.15f, 7.4f) :
            _view == 3 ? new Vector3(0, 2.0f, 3.1f) : new Vector3(1.2f, 3.5f, 3.1f);
        var yaw = Mathf.DegToRad(_yaw);
        if (_view == 1) target -= new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw)) * 1.5f;
        _camera.Position = target + new Vector3(14*Mathf.Sin(yaw),14,14*Mathf.Cos(yaw));
        _camera.LookAt(target); _camera.Size = _size;
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion move && Input.IsMouseButtonPressed(MouseButton.Middle)) SetYaw(_yaw-move.Relative.X*.3f);
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp) _size = Mathf.Max(4,_size*.9f);
            if (mouse.ButtonIndex == MouseButton.WheelDown) _size = Mathf.Min(32,_size/.9f);
            UpdateCamera();
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        switch (key.PhysicalKeycode)
        {
            case Key.Key1: SetView(1); break;
            case Key.Key2: SetView(2); break;
            case Key.Key3: SetView(3); break;
            case Key.Home: SetYaw(0); break;
            case Key.R: _motion = !_motion; break;
            case Key.T:
                _enemy = !_enemy;
                foreach (var geometry in _rover.FindChildren("*", "GeometryInstance3D", true, false).OfType<GeometryInstance3D>())
                    geometry.SetInstanceShaderParameter("team_color", new Color(_enemy ? "e0473f" : "46c2cc"));
                break;
        }
        RefreshStatus();
    }

    private void RefreshStatus() => _status.Text =
        "SCALE STUDY  |  Same meters and 45° camera  |  Provisional assets\n" +
        "1: Rover + sprite   2: all three   3: entrance   R: wheels/radar scan   T: team\n" +
        "Middle drag: rotate   Wheel: zoom   Home: north\n" +
        "Rifleman 2.23 m  •  Hub roof 6.44 m; door 2.70 m clear  •  Rover 4.83 × 2.60 m";
}
