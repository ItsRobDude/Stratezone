extends SceneTree

# Cross-format pivot/dimension checks and real 1280x800 review captures.
const OUT = "res://../art/units/rover/previews/"

func _initialize():
    call_deferred("verify")

func capture(name):
    await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(OUT + name + ".png")

func key(code):
    var event = InputEventKey.new()
    event.physical_keycode = code
    event.pressed = true
    Input.parse_input_event(event)
    await process_frame

func verify():
    root.size = Vector2i(1280,800)
    root.msaa_3d = Viewport.MSAA_4X
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT))
    var study = load("res://scenes/studies/RoverScaleStudy.tscn").instantiate()
    root.add_child(study); current_scene = study
    for i in range(10): await process_frame
    var rover = study.get_node("Rover")
    assert(rover.scale.is_equal_approx(Vector3.ONE))
    assert(rover.find_children("*", "Skeleton3D", true, false).is_empty())
    var visible = rover.find_children("*", "MeshInstance3D", true, false).filter(func(m): return m.visible)
    assert(visible.size() == 2, "Chassis and radar should be the only visible individual meshes")
    var radar = rover.find_child("Radar", true, false)
    assert(radar != null and rover.find_child("Turret", true, false) == null, "Gun export must be replaced")
    var radar_bounds = rover.global_transform.affine_inverse() * radar.global_transform * radar.get_aabb()
    assert(radar_bounds.position.y >= 2.299, "Radar must clear the roof bridge")
    assert(abs(radar_bounds.end.y - 3.28519) < .002, "Radar transform changed on import")
    var wheels = rover.get_node("Wheels")
    assert(wheels.multimesh.instance_count == 4)
    assert(wheels.multimesh.mesh.get_surface_count() == 1)
    assert(study.get_node("Rifleman").scale.is_equal_approx(Vector3.ONE))
    # Compare each instanced tire's bounds with its independently exported part.
    var names = ["Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR"]
    for i in range(4):
        var original = rover.find_child(names[i], true, false)
        var original_transform = rover.global_transform.affine_inverse() * original.global_transform
        var expected = original_transform * original.get_aabb()
        var actual = wheels.multimesh.get_instance_transform(i) * wheels.multimesh.mesh.get_aabb()
        assert(expected.position.distance_to(actual.position) < .001, "Wheel pivot/axis mismatch")
        assert(expected.size.distance_to(actual.size) < .001, "Wheel geometry must match source")
    await capture("all_three")
    study.call("SetView", 3)
    await capture("hub_entrance")
    study.call("SetView", 1)
    await capture("rover_reference")
    study.call("SetYaw", 145.0)
    await capture("rover_rear")
    study.call("SetYaw", 90.0)
    await capture("rover_side")
    var before = wheels.multimesh.get_instance_transform(0)
    var radar_before = radar.transform
    await key(KEY_R)
    for i in range(40): await process_frame
    assert(not wheels.multimesh.get_instance_transform(0).is_equal_approx(before), "Wheel preview motion failed")
    assert(not radar.transform.is_equal_approx(radar_before), "Radar scan motion failed")
    await key(KEY_R)
    await key(KEY_T)
    await capture("rover_motion_red")
    study.call("SetView", 1)
    study.call("SetYaw", -35.0)
    await key(KEY_T)
    radar.rotation.y = PI / 4
    await capture("radar_scan")
    print("ROVER_VERIFY: unscaled units, radar replaces gun, radar clearance/scan, four wheel pivots, three visible surfaces passed")
    quit()
