using Godot;
using Stratezone.Simulation;
using Stratezone.Simulation.Content;

public partial class PlacementGhost : Node2D
{
    private BuildingDefinition? _definition;
    private bool _isLegal;

    public void SetPreview(BuildingDefinition definition, Vector2 position, bool isLegal)
    {
        _definition = definition;
        Position = position;
        _isLegal = isLegal;
        Visible = true;
        QueueRedraw();
    }

    public void Clear()
    {
        _definition = null;
        Visible = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_definition is null)
        {
            return;
        }

        var radius = RtsSimulation.ToWorldRadius(_definition.FootprintRadius + _definition.PlacementBuffer);
        var color = _isLegal
            ? new Color(0.25f, 1.0f, 0.45f, 0.32f)
            : new Color(1.0f, 0.24f, 0.18f, 0.32f);

        DrawRect(new Rect2(new Vector2(-radius, -radius), new Vector2(radius * 2.0f, radius * 2.0f)), color);
        DrawRect(new Rect2(new Vector2(-radius, -radius), new Vector2(radius * 2.0f, radius * 2.0f)), color with { A = 0.85f }, false, 3.0f);
    }
}
