namespace Stratezone.Simulation;

public readonly record struct FogCell(
    int X,
    int Y,
    SimVector2 Center,
    float Size
);
