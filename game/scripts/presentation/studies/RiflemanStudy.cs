using Godot;
using System;
using System.Linq;

namespace Stratezone.Presentation.Studies;

/// <summary>Isolated asset review. No mission or simulation rules are changed.</summary>
public partial class RiflemanStudy : Node3D
{
    private readonly string[] _clips = { "rifle_ready_idle", "rifle_aim_idle", "rifle_run" };
    private Node3D _soldier = null!;
    private Camera3D _camera = null!;
    private AnimationPlayer _player = null!;
    private Label _status = null!;
    private Control _reference = null!;
    private bool _close;
    private bool _paused;
    private bool _enemy;
    private int _clipIndex;
    private float _yaw = -25f;
    private float _size = 14.5f;

    public override void _Ready()
    {
        GetWindow().Title = "Stratezone — Rifleman / shared infantry rig study";
        _camera = GetNode<Camera3D>("Camera");
        _soldier = GD.Load<PackedScene>("res://assets/models/studies/rifleman/rifleman_study.glb").Instantiate<Node3D>();
        _soldier.Name = "Rifleman";
        AddChild(_soldier);
        _soldier.Position = new Vector3(1.65f, 0, 5.6f);
        _player = new AnimationPlayer { Name = "SharedAnimations" };
        _soldier.AddChild(_player);
        _player.AddAnimationLibrary("", GD.Load<AnimationLibrary>("res://assets/models/studies/rifleman/infantry_anims.glb"));
        _player.Play(_clips[0]);
        BuildReviewOverlay();
        UpdateCamera();
        RefreshStatus();
        GD.Print("Rifleman study: one mesh, shared AnimationLibrary; clips: ", string.Join(", ", _player.GetAnimationList()));
    }

    private void BuildReviewOverlay()
    {
        // All labels here are developer-only study controls, not mission UI.
        var layer = new CanvasLayer();
        AddChild(layer);
        var panel = new PanelContainer { Position = new Vector2(16, 16), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(panel);
        _status = new Label();
        _status.AddThemeFontSizeOverride("font_size", 17);
        panel.AddChild(_status);
        _reference = new Control { Position = new Vector2(25, 142), Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_reference);
        var label = new Label { Text = "Rifleman sprite reference", Position = Vector2.Zero };
        _reference.AddChild(label);
        var reference = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            Texture = GD.Load<Texture2D>("res://assets/units/rifleman/directional/rifleman_180.png"),
            Position = new Vector2(0, 32), Size = new Vector2(285, 440),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _reference.AddChild(reference);
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            _yaw -= motion.Relative.X * .3f;
            UpdateCamera();
        }
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp) _size = Mathf.Max(2.8f, _size * .9f);
            if (mouse.ButtonIndex == MouseButton.WheelDown) _size = Mathf.Min(30, _size / .9f);
            UpdateCamera();
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        switch (key.PhysicalKeycode)
        {
            case Key.Key1: SetClose(true); break;
            case Key.Key2: SetClose(false); break;
            case Key.Home: _yaw = 0; UpdateCamera(); break;
            case Key.R:
                SelectClip(_clips[(_clipIndex + 1) % _clips.Length]);
                break;
            case Key.Space:
                _paused = !_paused;
                _player.SpeedScale = _paused ? 0 : 1;
                break;
            case Key.T:
                _enemy = !_enemy;
                foreach (var mesh in _soldier.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>())
                    mesh.SetInstanceShaderParameter("team_color", _enemy ? new Color("e0473f") : new Color("46c2cc"));
                break;
        }
        RefreshStatus();
    }

    public void SetClose(bool close)
    {
        _close = close;
        _size = close ? 3.7f : 14.5f;
        _yaw = close ? -30 : -25;
        _reference.Visible = close;
        GetNode<Node3D>("Hub").Visible = !close;
        UpdateCamera();
    }

    public void SelectClip(string name)
    {
        var index = Array.IndexOf(_clips, name);
        if (index < 0) throw new ArgumentException("Unknown study clip", nameof(name));
        _clipIndex = index;
        _player.Play(name);
        RefreshStatus();
    }

    private void UpdateCamera()
    {
        var yaw = Mathf.DegToRad(_yaw);
        var target = _close ? _soldier.Position + new Vector3(0, 1.08f, 0) : new Vector3(0, 2.6f, 1.2f);
        if (_close) target -= new Vector3(Mathf.Cos(yaw), 0, -Mathf.Sin(yaw)) * .47f;
        _camera.Position = target + new Vector3(12 * Mathf.Sin(yaw), 12, 12 * Mathf.Cos(yaw));
        _camera.LookAt(target);
        _camera.Size = _size;
    }

    private void RefreshStatus()
    {
        _status.Text = $"RIFLEMAN STUDY   •   {_clips[_clipIndex]}   {(_paused ? "PAUSED" : "")}\n" +
            "1: model + sprite   2: beside Hub   R: next clip   Space: pause   T: team color\n" +
            "Middle drag: rotate   Wheel: zoom   Home: north   |   45° orthographic • meters";
    }
}
