extends RefCounted

# Shared review output; never overwrites the tracked art previews.
var output_dir: String
var tree: SceneTree
var captures: Array = []

func begin(scene_tree: SceneTree) -> bool:
    tree = scene_tree
    var args = OS.get_cmdline_user_args()
    if args.size() != 2 or args[0] != "--output-dir" or not args[1].is_absolute_path():
        return fail("Use check_art.py, or pass -- --output-dir <absolute artifacts/art-check/run/asset path>")
    output_dir = args[1].replace("\\", "/").simplify_path()
    var allowed = ProjectSettings.globalize_path("res://../artifacts/art-check").replace("\\", "/").simplify_path()
    if not output_dir.begins_with(allowed + "/"):
        return fail("Capture output must be below artifacts/art-check")
    if DirAccess.make_dir_recursive_absolute(output_dir) != OK:
        return fail("Cannot create capture output: " + output_dir)
    if DisplayServer.get_name() == "headless":
        return fail("Art captures need a graphical renderer; omit --headless")
    # Assertions can stop a coroutine without quitting the SceneTree. The launcher
    # also has a process timeout and scans engine, C# and assertion errors.
    tree.create_timer(90.0).timeout.connect(func(): fail("Art verifier did not finish within 90 seconds"))
    return true

func fail(message: String) -> bool:
    push_error("ART_CHECK: " + message)
    tree.quit(1)
    return false

func capture(name: String) -> bool:
    if not name.is_valid_filename():
        return fail("Invalid capture filename")
    await tree.process_frame
    await RenderingServer.frame_post_draw
    var image = tree.root.get_texture().get_image()
    if image == null or image.is_empty():
        return fail("Empty viewport capture: " + name)
    var path = output_dir.path_join(name + ".png")
    var result = image.save_png(path)
    if result != OK:
        return fail("PNG write failed (%s): %s" % [result, path])
    var camera = tree.root.get_camera_3d()
    if camera == null:
        return fail("Missing study camera")
    captures.append({
        "file": name + ".png", "viewport": [image.get_width(), image.get_height()],
        "msaa_3d": tree.root.msaa_3d,
        "camera": {
            "position_m": vector(camera.global_position),
            "rotation_degrees": vector(camera.global_rotation_degrees),
            "projection": "orthographic" if camera.projection == Camera3D.PROJECTION_ORTHOGONAL else "perspective",
            "size_m": camera.size, "near_m": camera.near, "far_m": camera.far,
        },
    })
    return true

func vector(value: Vector3) -> Array:
    return [value.x, value.y, value.z]

func finish(asset: String) -> bool:
    var metadata = {
        "asset": asset, "godot_version": Engine.get_version_info(),
        "platform": OS.get_name(),
        "rendering": {
            "method": RenderingServer.get_current_rendering_method(),
            "driver": RenderingServer.get_current_rendering_driver_name(),
            "adapter": RenderingServer.get_video_adapter_name(),
            "vendor": RenderingServer.get_video_adapter_vendor(),
            "api_version": RenderingServer.get_video_adapter_api_version(),
        },
        "captures": captures,
    }
    var file = FileAccess.open(output_dir.path_join("capture_metadata.json"), FileAccess.WRITE)
    if file == null:
        return fail("Cannot write capture metadata")
    file.store_string(JSON.stringify(metadata, "  ") + "\n")
    file.close()
    if FileAccess.get_file_as_string(output_dir.path_join("capture_metadata.json")).is_empty():
        return fail("Capture metadata write failed")
    print("ART_CHECK_PASS: ", asset)
    tree.quit(0)
    return true
