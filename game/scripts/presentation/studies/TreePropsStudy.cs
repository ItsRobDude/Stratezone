using Godot;

namespace Stratezone.Presentation.Studies;

/// <summary>Opt-in world-prop review at unit scale; no map or simulation writes.</summary>
public partial class TreePropsStudy : Node3D
{
    private static readonly string[] Ids = { "pine_a", "pine_b", "cherry_a", "cherry_b" };
    private static readonly string[] Captions = {
        "PINE A · full / 7.0 m", "PINE B · slender / 6.0 m",
        "CHERRY A · spreading / 4.9 m", "CHERRY B · upright / 6.0 m" };
    private readonly Node3D[] _trees = new Node3D[4];
    private readonly Label[] _labels = new Label[4];
    private Camera3D _camera = null!;
    private Node3D _soldier = null!;
    private Label _soldierLabel = null!;
    private int _view = 1;
    private float _yaw = -8;
    private float _size = 15;

    public override void _Ready()
    {
        GetWindow().Title = "Stratezone — Pine and cherry tree props";
        _camera = GetNode<Camera3D>("Camera");
        var layer = new CanvasLayer(); AddChild(layer);
        var heading = MakeLabel("WORLD PROPS  |  Two pines + two summer cherry trees\n" +
            "1: all four   2: pines   3: cherries   Middle drag: rotate   Wheel: zoom   Home: north", 18);
        heading.Position = new Vector2(18, 16); layer.AddChild(heading);
        for (var i = 0; i < 4; i++)
        {
            _trees[i] = GD.Load<PackedScene>($"res://assets/models/props/trees/{Ids[i]}.glb").Instantiate<Node3D>();
            _trees[i].Name = Ids[i]; AddChild(_trees[i]);
            _labels[i] = MakeLabel(Captions[i], 15); layer.AddChild(_labels[i]);
        }
        _soldier = GD.Load<PackedScene>("res://assets/models/studies/rifleman/rifleman_study.glb").Instantiate<Node3D>();
        _soldier.Name = "Rifleman"; AddChild(_soldier);
        var player = new AnimationPlayer { Name = "SharedAnimations" }; _soldier.AddChild(player);
        player.AddAnimationLibrary("", GD.Load<AnimationLibrary>("res://assets/models/studies/rifleman/infantry_anims.glb"));
        player.Play("rifle_ready_idle");
        _soldierLabel = MakeLabel("Rifleman · 2.23 m", 15); layer.AddChild(_soldierLabel);
        SetView(1);
    }

    private static Label MakeLabel(string text, int size)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_outline_color", new Color(0.06f, 0.08f, 0.05f, 1));
        label.AddThemeConstantOverride("outline_size", 4);
        return label;
    }

    public void SetView(int view)
    {
        _view = view; _size = view == 1 ? 15 : view == 2 ? 13 : 11;
        for (var i = 0; i < 4; i++)
        {
            _trees[i].Visible = view == 1 || view == 2 && i < 2 || view == 3 && i >= 2;
            _trees[i].Position = new Vector3(view == 1 ? (i - 1.5f) * 5.4f : (i % 2 - .5f) * 5.8f, 0, 0);
            _labels[i].Visible = _trees[i].Visible;
        }
        _soldier.Position = new Vector3(0, 0, 3.1f);
        UpdateCamera();
    }

    public void SetYaw(float yaw) { _yaw = yaw; UpdateCamera(); }

    private void UpdateCamera()
    {
        var target = new Vector3(0, _view == 2 ? 3.2f : 2.5f, 0);
        var yaw = Mathf.DegToRad(_yaw);
        _camera.Position = target + new Vector3(18 * Mathf.Sin(yaw), 18, 18 * Mathf.Cos(yaw));
        _camera.LookAt(target); _camera.Size = _size;
    }

    public override void _Process(double delta)
    {
        for (var i = 0; i < 4; i++)
        {
            var compact = _camera.Size > 20;
            _labels[i].Text = compact ? Ids[i].Replace('_', ' ').ToUpperInvariant() : Captions[i];
            _labels[i].Position = _camera.UnprojectPosition(_trees[i].Position) + new Vector2(-85, 16);
            if (compact) _labels[i].Position += new Vector2(57, 0);
        }
        _soldierLabel.Position = _camera.UnprojectPosition(_soldier.Position) + new Vector2(-64, 12);
    }

    public override void _UnhandledInput(InputEvent input)
    {
        if (input is InputEventMouseMotion move && Input.IsMouseButtonPressed(MouseButton.Middle)) SetYaw(_yaw - move.Relative.X * .3f);
        if (input is InputEventMouseButton mouse && mouse.Pressed)
        {
            if (mouse.ButtonIndex == MouseButton.WheelUp) _size = Mathf.Max(5, _size * .9f);
            if (mouse.ButtonIndex == MouseButton.WheelDown) _size = Mathf.Min(32, _size / .9f);
            UpdateCamera();
        }
        if (input is not InputEventKey key || !key.Pressed || key.Echo) return;
        switch (key.PhysicalKeycode)
        {
            case Key.Key1: SetView(1); break;
            case Key.Key2: SetView(2); break;
            case Key.Key3: SetView(3); break;
            case Key.Home: SetYaw(0); break;
        }
    }
}
