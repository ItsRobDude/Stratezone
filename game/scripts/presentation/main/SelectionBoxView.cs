using Godot;

public partial class SelectionBoxView : Node2D
{
    private bool _active;
    private Vector2 _start;
    private Vector2 _end;

    public void Begin(Vector2 start)
    {
        _active = true;
        _start = start;
        _end = start;
        QueueRedraw();
    }

    public void UpdateEnd(Vector2 end)
    {
        if (!_active)
        {
            return;
        }

        _end = end;
        QueueRedraw();
    }

    public void Clear()
    {
        _active = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!_active)
        {
            return;
        }

        var rect = new Rect2(_start, _end - _start).Abs();
        DrawRect(rect, new Color(0.35f, 0.8f, 1.0f, 0.12f), true);
        DrawRect(rect, new Color(0.35f, 0.8f, 1.0f, 0.78f), false, 2.0f);
    }
}
