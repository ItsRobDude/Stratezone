using Godot;
using Stratezone.Simulation.Tools;

public partial class Main
{
    private const float MapEditorEdgePanThresholdPixels = 24.0f;
    private const float MapEditorEdgePanSpeedWorldUnitsPerSecond = 600.0f;

    private bool _mapEditorCameraDragging;

    private bool TryHandleMapEditorNavigationMouse(InputEvent inputEvent)
    {
        if (!_mapEditorEnabled || _camera is null)
        {
            return false;
        }

        if (inputEvent is InputEventMouseButton mouseButton)
        {
            var canStartPan = CanUseMapEditorCameraPan();
            if (mouseButton.ButtonIndex == MouseButton.Middle)
            {
                if (mouseButton.Pressed && canStartPan)
                {
                    BeginMapEditorCameraDrag();
                    return true;
                }

                if (!mouseButton.Pressed && _mapEditorCameraDragging)
                {
                    EndMapEditorCameraDrag();
                    return true;
                }
            }

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed && Input.IsKeyPressed(Key.Space) && canStartPan)
                {
                    BeginMapEditorCameraDrag();
                    return true;
                }

                if (!mouseButton.Pressed && _mapEditorCameraDragging)
                {
                    EndMapEditorCameraDrag();
                    return true;
                }
            }

            return _mapEditorCameraDragging;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion && _mapEditorCameraDragging)
        {
            PanMapEditorCameraByScreenDelta(mouseMotion.Relative);
            return true;
        }

        return false;
    }

    private void HandleMapEditorEdgePan(float deltaSeconds)
    {
        if (!_mapEditorEnabled ||
            _camera is null ||
            _mapEditorCameraDragging ||
            !CanUseMapEditorCameraPan())
        {
            return;
        }

        var mousePosition = GetViewport().GetMousePosition();
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var direction = Vector2.Zero;
        if (mousePosition.X <= MapEditorEdgePanThresholdPixels)
        {
            direction.X -= 1.0f;
        }
        else if (mousePosition.X >= viewportSize.X - MapEditorEdgePanThresholdPixels)
        {
            direction.X += 1.0f;
        }

        if (mousePosition.Y <= MapEditorEdgePanThresholdPixels)
        {
            direction.Y -= 1.0f;
        }
        else if (mousePosition.Y >= viewportSize.Y - MapEditorEdgePanThresholdPixels)
        {
            direction.Y += 1.0f;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        var speed = MapEditorEdgePanSpeedWorldUnitsPerSecond / Mathf.Max(0.01f, _camera.Zoom.X);
        MoveCameraByWorldDelta(direction.Normalized() * speed * deltaSeconds);
    }

    private bool CanUseMapEditorCameraPan()
    {
        if (_mapEditorOverlay?.IsDragging == true ||
            _pendingMapEditorDelete is not null ||
            _pendingMapEditorMissionSwitchId is not null ||
            _mapEditorContextMenu?.Visible == true ||
            EditorTextInputHasFocus())
        {
            return false;
        }

        return !IsPointerOverMapEditorUi();
    }

    private bool EditorTextInputHasFocus()
    {
        var focusOwner = GetViewport().GuiGetFocusOwner();
        return focusOwner is LineEdit or TextEdit;
    }

    private bool IsPointerOverMapEditorUi()
    {
        var position = GetViewport().GetMousePosition();
        return IsVisibleControlUnderPointer(_mapEditorPalettePanel, position) ||
            IsVisibleControlUnderPointer(_mapEditorInspectorPanel, position) ||
            IsVisibleControlUnderPointer(_mapEditorPanel, position);
    }

    private static bool IsVisibleControlUnderPointer(Control? control, Vector2 viewportPosition)
    {
        return control is not null &&
            control.Visible &&
            control.GetGlobalRect().HasPoint(viewportPosition);
    }

    private void BeginMapEditorCameraDrag()
    {
        _mapEditorCameraDragging = true;
        Input.SetDefaultCursorShape(Input.CursorShape.Drag);
        DisplayServer.CursorSetShape(DisplayServer.CursorShape.Drag);
    }

    private void EndMapEditorCameraDrag()
    {
        _mapEditorCameraDragging = false;
        ApplyMapEditorCursor(_mapEditorOverlay?.ToolMode ?? MapEditorToolMode.Select);
    }

    private void PanMapEditorCameraByScreenDelta(Vector2 screenDelta)
    {
        if (_camera is null)
        {
            return;
        }

        MoveCameraByWorldDelta(-screenDelta / Mathf.Max(0.01f, _camera.Zoom.X));
    }

    private void MoveCameraByWorldDelta(Vector2 worldDelta)
    {
        if (_camera is null)
        {
            return;
        }

        _camera.Position += worldDelta;
        ApplyPresentationZoom();
    }
}
