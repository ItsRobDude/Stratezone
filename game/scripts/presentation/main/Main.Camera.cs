using Godot;
using Stratezone.Simulation;

public partial class Main
{
    private void SetupCamera()
    {
        _camera = new Camera2D
        {
            Name = "GreyboxCamera",
            Zoom = new Vector2(1.0f, 1.0f),
            Enabled = true
        };
        AddChild(_camera);
        ResetCameraToMissionStart();
        _camera.MakeCurrent();
    }

    private void ResetCameraToMissionStart()
    {
        if (_camera is null)
        {
            return;
        }

        var playerStart = FindMissionMarkerPosition("player_landing_zone") ??
            FindMissionMarkerPosition("player_base") ??
            new Vector2(140, 20);
        _camera.Position = playerStart;
        _camera.Zoom = new Vector2(1.0f, 1.0f);
        ApplyPresentationZoom();
    }

    private void FocusCameraOn(Vector2 position)
    {
        if (_camera is null)
        {
            return;
        }

        _camera.Position = position;
        ApplyPresentationZoom();
    }

    private Vector2? FindMissionMarkerPosition(string markerId)
    {
        var marker = _activeMission?.Markers.FirstOrDefault(marker => string.Equals(marker.Id, markerId, StringComparison.Ordinal));
        return marker is null
            ? null
            : new Vector2(marker.Position.X, marker.Position.Y);
    }

    private void HandleCameraPan(double delta)
    {
        if (_camera is null || (_mapEditorEnabled && EditorTextInputHasFocus()))
        {
            return;
        }

        var direction = Vector2.Zero;

        if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left))
        {
            direction.X -= 1;
        }
        if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right))
        {
            direction.X += 1;
        }
        if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up))
        {
            direction.Y -= 1;
        }
        if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down))
        {
            direction.Y += 1;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        var speed = 520.0f / _camera.Zoom.X;
        _camera.Position += direction.Normalized() * speed * (float)delta;
    }

    private void AdjustZoom(float delta)
    {
        if (_camera is null)
        {
            return;
        }

        var zoomValue = Mathf.Clamp(_camera.Zoom.X + delta, 0.55f, 1.8f);
        _camera.Zoom = new Vector2(zoomValue, zoomValue);
        ApplyPresentationZoom();
    }

    private void ApplyPresentationZoom()
    {
        var cameraZoom = CurrentCameraZoom();
        foreach (var buildingView in _buildingViews.Values)
        {
            buildingView.SetCameraZoom(cameraZoom);
        }

        foreach (var wellView in _resourceWellViews)
        {
            wellView.SetCameraZoom(cameraZoom);
        }

        foreach (var unitView in _simUnitViews.Values)
        {
            unitView.SetCameraZoom(cameraZoom);
        }
    }

    private float CurrentCameraZoom()
    {
        return _camera?.Zoom.X ?? 1.0f;
    }

    private Rect2 GetExpandedCameraWorldBounds(float padding)
    {
        if (_camera is null)
        {
            return new Rect2(new Vector2(-100000.0f, -100000.0f), new Vector2(200000.0f, 200000.0f));
        }

        var viewportSize = GetViewport().GetVisibleRect().Size;
        var zoom = new Vector2(Mathf.Max(0.01f, _camera.Zoom.X), Mathf.Max(0.01f, _camera.Zoom.Y));
        var worldSize = new Vector2(viewportSize.X / zoom.X, viewportSize.Y / zoom.Y);
        return new Rect2(
            _camera.GlobalPosition - (worldSize * 0.5f) - new Vector2(padding, padding),
            worldSize + new Vector2(padding * 2.0f, padding * 2.0f));
    }

    private static bool IsCircleInsideWorldBounds(SimVector2 position, float radius, Rect2 bounds)
    {
        return bounds.Grow(radius).HasPoint(new Vector2(position.X, position.Y));
    }
}
