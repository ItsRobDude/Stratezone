using Stratezone.Simulation;

namespace Stratezone.Simulation.Tools;

internal static class MapEditorShapeResizeHelper
{
    public static MapEditorResizeHandle HitTest(
        string shape,
        SimVector2 center,
        SimVector2 size,
        float radius,
        SimVector2 position,
        float tolerance)
    {
        if (shape == "circle")
        {
            var distance = MathF.Abs(center.DistanceTo(position) - radius);
            return distance <= tolerance ? MapEditorResizeHandle.CircleRadius : MapEditorResizeHandle.None;
        }

        if (shape != "rect")
        {
            return MapEditorResizeHandle.None;
        }

        var halfWidth = size.X * 0.5f;
        var halfHeight = size.Y * 0.5f;
        var insideY = MathF.Abs(position.Y - center.Y) <= halfHeight + tolerance;
        var insideX = MathF.Abs(position.X - center.X) <= halfWidth + tolerance;
        if (insideY && MathF.Abs(position.X - (center.X - halfWidth)) <= tolerance)
        {
            return MapEditorResizeHandle.RectLeft;
        }

        if (insideY && MathF.Abs(position.X - (center.X + halfWidth)) <= tolerance)
        {
            return MapEditorResizeHandle.RectRight;
        }

        if (insideX && MathF.Abs(position.Y - (center.Y - halfHeight)) <= tolerance)
        {
            return MapEditorResizeHandle.RectTop;
        }

        if (insideX && MathF.Abs(position.Y - (center.Y + halfHeight)) <= tolerance)
        {
            return MapEditorResizeHandle.RectBottom;
        }

        return MapEditorResizeHandle.None;
    }

    public static ShapeResizeResult Resize(
        string shape,
        SimVector2 center,
        SimVector2 size,
        float radius,
        MapEditorResizeHandle handle,
        SimVector2 position,
        float minimumDimension)
    {
        if (shape == "circle" && handle == MapEditorResizeHandle.CircleRadius)
        {
            return new ShapeResizeResult(center, size, MathF.Max(minimumDimension * 0.5f, center.DistanceTo(position)));
        }

        if (shape != "rect")
        {
            return new ShapeResizeResult(center, size, radius);
        }

        var left = center.X - (size.X * 0.5f);
        var right = center.X + (size.X * 0.5f);
        var top = center.Y - (size.Y * 0.5f);
        var bottom = center.Y + (size.Y * 0.5f);

        switch (handle)
        {
            case MapEditorResizeHandle.RectLeft:
                left = MathF.Min(position.X, right - minimumDimension);
                break;
            case MapEditorResizeHandle.RectRight:
                right = MathF.Max(position.X, left + minimumDimension);
                break;
            case MapEditorResizeHandle.RectTop:
                top = MathF.Min(position.Y, bottom - minimumDimension);
                break;
            case MapEditorResizeHandle.RectBottom:
                bottom = MathF.Max(position.Y, top + minimumDimension);
                break;
        }

        var newSize = new SimVector2(MathF.Max(minimumDimension, right - left), MathF.Max(minimumDimension, bottom - top));
        var newCenter = new SimVector2((left + right) * 0.5f, (top + bottom) * 0.5f);
        return new ShapeResizeResult(newCenter, newSize, radius);
    }
}

internal readonly record struct ShapeResizeResult(SimVector2 Center, SimVector2 Size, float Radius);
