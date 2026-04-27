using Godot;
using Stratezone.Simulation;

namespace Stratezone.Presentation.Main;

public partial class Main : Node2D
{
    public override void _Ready()
    {
        GD.Print($"{GameInfo.Title} scaffold ready. Mission target: {ContentIds.Missions.FirstLanding}");
    }
}

