using Godot;
using Stratezone.Simulation;

/// <summary>
/// Opt-in art comparison: the real mission and its existing unit sprites, with
/// the player Hub rendered from the study model at the content-to-world scale.
/// This class is used only by ColonyHubMissionStudy.tscn, never the main scene.
/// </summary>
public partial class ColonyHubMissionStudy : Main
{
    private const int RenderSize = 1024;
    private const float Supersampling = 2.0f;
    private SubViewport? _hubViewport;
    private Sprite2D? _original;
    private Sprite2D? _comparison;
    private bool _showModel = true;

    public override void _Ready()
    {
        base._Ready();
        GetWindow().Title = "Stratezone - Colony Hub scale comparison (F2: sprite / model)";
        CreateHubViewport();
        AttachToPlayerHub();
        if (_original?.GetParent() is Node2D hub)
        {
            GetNode<Camera2D>("GreyboxCamera").Position = hub.Position + new Vector2(120, 80);
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (!GodotObject.IsInstanceValid(_original))
        {
            _original = null;
            _comparison = null;
            AttachToPlayerHub();
        }
        ApplyComparisonVisibility();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventKey { Pressed: true, Echo: false, Keycode: Key.F2 })
        {
            _showModel = !_showModel;
            ApplyComparisonVisibility();
            GetViewport().SetInputAsHandled();
            GD.Print($"[HubStudy] showing {(_showModel ? "3D model" : "original 2D sprite")}");
            return;
        }
        base._UnhandledInput(inputEvent);
    }

    private void CreateHubViewport()
    {
        _hubViewport = new SubViewport
        {
            Name = "HubStudyRender",
            Size = new Vector2I(RenderSize, RenderSize),
            OwnWorld3D = true,
            TransparentBg = true,
            Msaa3D = Viewport.Msaa.Msaa4X,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        AddChild(_hubViewport);
        var stage = GD.Load<PackedScene>("res://scenes/studies/ColonyHubStudy.tscn").Instantiate<Node3D>();
        _hubViewport.AddChild(stage);
        stage.GetNode<MeshInstance3D>("Ground").Visible = false;
        var environment = stage.GetNode<WorldEnvironment>("WorldEnvironment");
        environment.Environment = (Godot.Environment)environment.Environment.Duplicate();
        environment.Environment.BackgroundMode = Godot.Environment.BGMode.ClearColor;
        // Frontal 45-degree view, matching the current south-facing Hub sprite.
        var camera = stage.GetNode<Camera3D>("Camera");
        camera.Position = new Vector3(0, 20, 20);
        camera.LookAt(Vector3.Zero);
        camera.Size = RenderSize / (RtsSimulation.ContentUnitScale * Supersampling);
        GD.Print($"[HubStudy] {RtsSimulation.ContentUnitScale} world pixels per model meter; existing unit sprites unchanged.");
    }

    private void AttachToPlayerHub()
    {
        if (_hubViewport is null)
        {
            return;
        }
        foreach (var child in GetNode<Node2D>("WorldRoot").GetChildren())
        {
            if (child is not GreyboxBuilding building ||
                !building.Name.ToString().EndsWith(ContentIds.Buildings.ColonyHub, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (var visual in building.GetChildren())
            {
                // The current building view uses white for the player and a red
                // tint for the enemy. This isolated study only swaps the player.
                if (visual is not Sprite2D { Texture: not null } sprite || sprite.Modulate != Colors.White)
                {
                    continue;
                }
                _original = sprite;
                _comparison = new Sprite2D
                {
                    Name = "HubStudyModel",
                    Texture = _hubViewport.GetTexture(),
                    Scale = Vector2.One / Supersampling,
                    ZIndex = sprite.ZIndex,
                    TextureFilter = TextureFilterEnum.Linear
                };
                building.AddChild(_comparison);
                // The render camera aims at model ground origin, so texture
                // center maps to the building's existing simulation position.
                GD.Print($"[HubStudy] attached to {building.Name} at {building.Position}");
                ApplyComparisonVisibility();
                return;
            }
        }
    }

    private void ApplyComparisonVisibility()
    {
        if (_original is null || _comparison is null || !GodotObject.IsInstanceValid(_comparison))
        {
            return;
        }
        _original.SelfModulate = _showModel ? Colors.Transparent : Colors.White;
        _comparison.Visible = _showModel && _original.Visible;
        _comparison.Modulate = _original.Modulate;
    }
}
