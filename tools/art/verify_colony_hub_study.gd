extends SceneTree

# Current export against saved Blender metrics, plus fresh engine look samples.
const CaptureCheck = preload("res://../tools/art/capture_check.gd")
var output = CaptureCheck.new()

func _initialize():
    call_deferred("verify")

func view(camera: Camera3D, target: Vector3, yaw: float, size: float):
    var angle = deg_to_rad(yaw)
    camera.position = target + Vector3(18 * sin(angle), 18, 18 * cos(angle))
    camera.look_at(target)
    camera.size = size

func verify():
    if not output.begin(self): return
    root.size = Vector2i(1280, 800)
    root.msaa_3d = Viewport.MSAA_4X
    var metrics = JSON.parse_string(FileAccess.get_file_as_string("res://../art/buildings/colony_hub/study_metrics.json"))
    var study = load("res://scenes/studies/ColonyHubStudy.tscn").instantiate()
    root.add_child(study); current_scene = study
    for i in range(8): await process_frame
    var hub = study.get_node("Hub")
    assert(hub.scale.is_equal_approx(Vector3.ONE), "Hub must keep meter scale")
    assert(hub.find_children("*", "Skeleton3D", true, false).is_empty(), "Hub is a static study")
    var meshes = hub.find_children("*", "MeshInstance3D", true, false)
    if hub is MeshInstance3D: meshes.append(hub)
    assert(meshes.size() == 1, "Only the merged Hub should be exported")
    var mesh = meshes[0]
    assert(mesh.mesh.get_surface_count() == metrics.materials, "Hub material surfaces changed")
    var triangles = 0
    for i in range(mesh.mesh.get_surface_count()):
        var material = mesh.get_active_material(i)
        assert(material is StandardMaterial3D, "Expected persistent Hub material")
        assert(material.vertex_color_use_as_albedo and not material.vertex_color_is_srgb, "Hub vertex colors must remain linear")
        var arrays = mesh.mesh.surface_get_arrays(i)
        triangles += arrays[Mesh.ARRAY_INDEX].size() / 3
    assert(triangles == metrics.triangles and triangles <= 12000, "Hub triangle count/budget changed")
    var bounds = hub.global_transform.affine_inverse() * mesh.global_transform * mesh.get_aabb()
    var expected = Vector3(metrics.dimensions[0], metrics.dimensions[2], metrics.dimensions[1])
    assert(bounds.size.distance_to(expected) < .003, "Hub Blender/Godot dimensions differ")
    assert(abs(bounds.position.y - metrics.bounds_min[2]) < .003, "Hub ground origin changed")
    print("HUB_VERIFY: triangles=", triangles, " surfaces=", metrics.materials, " bounds=", bounds)
    var camera = study.get_node("Camera")
    view(camera, Vector3(0, 2.9, 0), 38, 13.125)
    if not await output.capture("godot_front"): return
    view(camera, Vector3(0, 2.9, 0), 218, 13.125)
    if not await output.capture("godot_rear"): return
    view(camera, Vector3(0, 2.9, 0), 38, 25)
    if not await output.capture("godot_far"): return
    view(camera, Vector3(0, 2.9, 0), 38, 13.125)
    for i in range(mesh.mesh.get_surface_count()):
        var material = mesh.get_active_material(i)
        if material.resource_name.ends_with("Team") or material.resource_name.ends_with("Glass"):
            var red = material.duplicate() as StandardMaterial3D
            red.vertex_color_use_as_albedo = false
            red.albedo_color = Color("9f3030") if material.resource_name.ends_with("Glass") else Color("e55842")
            red.emission = red.albedo_color * .4
            mesh.set_surface_override_material(i, red)
    if not await output.capture("godot_enemy"): return
    output.finish("colony_hub")
