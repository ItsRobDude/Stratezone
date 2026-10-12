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

        var color = _isLegal
            ? new Color(0.25f, 1.0f, 0.45f, 0.9f)
            : new Color(1.0f, 0.24f, 0.18f, 0.9f);

        // Solid outline is what blocks; the faint ring is the buffer other buildings keep clear of.
        var footprint = _definition.Footprint;
        FootprintDrawing.DrawFill(this, footprint, Vector2.Zero, color with { A = 0.18f });
        FootprintDrawing.DrawOutline(this, footprint, Vector2.Zero, color, 3.0f);
        FootprintDrawing.DrawOutline(this, footprint, Vector2.Zero, color with { A = 0.35f }, 1.0f, grow: RtsSimulation.ToWorldRadius(_definition.PlacementBuffer));
    }
}
