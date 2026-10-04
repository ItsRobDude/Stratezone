extends SceneTree
## Run through tools/godot.ps1 -Check -Headless -Script res://../tools/verify_fixed_step_presentation.gd.
## Drives the real Main input handlers and rendering adapter with explicit frame times.
## Optional rendered capture: omit -Headless and pass -UserArgsJson '["--capture"]'.

var failures := 0

func _initialize():
    call_deferred("run")

func check(condition: bool, message: String):
    if not condition:
        failures += 1
        push_error(message)

func unit_view(main: Node, suffix: String) -> Node2D:
    for child in main.get_node("WorldRoot").get_children():
        if str(child.name).begins_with("SimUnit_") and str(child.name).ends_with(suffix) and not child.is_queued_for_deletion():
            return child
    return null

func building_count(main: Node, suffix: String) -> int:
    var count := 0
    for child in main.get_node("WorldRoot").get_children():
        if str(child.name).begins_with("Building_") and str(child.name).ends_with(suffix) and not child.is_queued_for_deletion():
            count += 1
    return count

func has_building_at(main: Node, suffix: String, position: Vector2) -> bool:
    for child in main.get_node("WorldRoot").get_children():
        if str(child.name).begins_with("Building_") and str(child.name).ends_with(suffix) and not child.is_queued_for_deletion():
            if child.position.distance_to(position) < 0.001:
                return true
    return false

func cancel_placement(main: Node):
    var right_click := InputEventMouseButton.new()
    right_click.button_index = MOUSE_BUTTON_RIGHT
    right_click.pressed = true
    main.call("_UnhandledInput", right_click)

func load_mission(main: Node, mission: String):
    main.call("LoadMission", mission)
    await process_frame
    main.set_process(false)

func run():
    root.size = Vector2i(1280, 800) # Headless has no window to establish selection/culling bounds.
    var main = load("res://scenes/main/Main.tscn").instantiate()
    root.add_child(main)
    main.set_process(false)
    await process_frame
    # Headless CanvasItem draw signals still expose redraw requests. Let each
    # previous request drain before exercising a reveal change or a fresh fog instance.
    var fog = main.get_node("WorldRoot/FogOfWarView")
    var fog_draws := [0]
    fog.draw.connect(func(): fog_draws[0] += 1)
    for frame in range(3): await process_frame
    var before_draws: int = fog_draws[0]
    main.call("ToggleMapEditor")
    for frame in range(3): await process_frame
    check(fog_draws[0] > before_draws, "editor reveal did not redraw fog")
    before_draws = fog_draws[0]
    main.call("ToggleMapEditor")
    for frame in range(3): await process_frame
    check(fog_draws[0] > before_draws, "editor resume did not redraw the restored fog")
    before_draws = fog_draws[0]
    await load_mission(main, "mission_first_landing")
    for frame in range(3): await process_frame
    check(fog_draws[0] > before_draws, "same-mission reload did not redraw its new fog instance")
    var expected: Variant = null
    for fps in [30, 40, 60, 120, 144, 240]:
        await load_mission(main, "mission_first_landing")
        var rover := unit_view(main, "unit_rover")
        var start := rover.position
        main.call("SelectSingleAt", start)
        main.call("CommandSelectedUnits", start + Vector2(400, 0))
        check(rover.position == start, "input must not move a unit before its simulation step")
        var changes := 0
        var previous := start
        for frame in range(fps):
            main.call("ProcessSimulation", 1.0 / fps)
            if rover.position.distance_to(previous) > 0.00001:
                changes += 1
            previous = rover.position
        check(changes > fps * 0.8, "movement must update on rendered frames, including above 20 FPS")
        if expected == null:
            expected = rover.position
        check(rover.position.distance_to(expected) < 0.001, "frame schedule changed the displayed position after equal elapsed time")
        print("PRESENTATION ", fps, " FPS: moving_frames=", changes, " position=", rover.position)

    await load_mission(main, "mission_first_landing")
    var selected_rover := unit_view(main, "unit_rover")
    selected_rover.set_process(false) # Isolate adapter redraw from animation/hover updates.
    var path_draws := [0]
    selected_rover.draw.connect(func(): path_draws[0] += 1)
    main.call("SelectSingleAt", selected_rover.position)
    main.call("CommandSelectedUnits", selected_rover.position + Vector2(400, 0))
    main.call("ProcessSimulation", 0.05)
    for frame in range(3): await process_frame
    var before_path_draws: int = path_draws[0]
    var before_pose := selected_rover.position
    main.call("ProcessSimulation", 0.01) # No simulation step; only the displayed pose changes.
    for frame in range(3): await process_frame
    check(selected_rover.position.distance_to(before_pose) > 0.001, "intermediate frame did not interpolate the selected unit")
    check(path_draws[0] > before_path_draws, "selected path did not redraw with the interpolated pose")

    await load_mission(main, "mission_first_landing")
    var grunt := unit_view(main, "unit_grunt")
    main.call("SelectSingleAt", grunt.position)
    var before := building_count(main, "building_power_plant")
    main.call("EnterPlacementMode", "building_power_plant")
    main.call("TryPlaceSelectedBuilding", Vector2(-170, 40))
    main.call("ProcessSimulation", 0.049)
    check(building_count(main, "building_power_plant") == before, "placement ran before its step")
    main.call("ProcessSimulation", 0.001)
    check(building_count(main, "building_power_plant") == before + 1, "placement did not execute on the step")

    await load_mission(main, "mission_first_landing")
    grunt = unit_view(main, "unit_grunt")
    main.call("SelectSingleAt", grunt.position)
    before = building_count(main, "building_power_plant")
    main.call("EnterPlacementMode", "building_power_plant")
    main.call("TryPlaceSelectedBuilding", Vector2(-170, 40))
    main.call("ProcessSimulation", 0.049)
    cancel_placement(main)
    main.call("ProcessSimulation", 0.001)
    check(building_count(main, "building_power_plant") == before, "right-click cancellation still placed the queued building")

    # A replacement click is accepted immediately; an old cancellation receipt must
    # not clear the new pending order before it executes.
    await load_mission(main, "mission_first_landing")
    grunt = unit_view(main, "unit_grunt")
    main.call("SelectSingleAt", grunt.position)
    before = building_count(main, "building_power_plant")
    main.call("EnterPlacementMode", "building_power_plant")
    main.call("TryPlaceSelectedBuilding", Vector2(-170, 40))
    cancel_placement(main)
    main.call("EnterPlacementMode", "building_power_plant")
    main.call("TryPlaceSelectedBuilding", Vector2(-170, 60))
    main.call("ProcessSimulation", 0.049)
    cancel_placement(main)
    main.call("EnterPlacementMode", "building_power_plant")
    main.call("TryPlaceSelectedBuilding", Vector2(-170, 80))
    main.call("ProcessSimulation", 0.001)
    check(building_count(main, "building_power_plant") == before + 1, "replacement should create exactly one building")
    check(has_building_at(main, "building_power_plant", Vector2(-170, 80)), "replacement click did not place at the latest site")

    # Loading a different mission must not inherit a fractional step or pending input.
    var rover := unit_view(main, "unit_rover")
    main.call("SelectSingleAt", rover.position)
    main.call("CommandSelectedUnits", rover.position + Vector2(400, 0))
    main.call("ToggleMapEditor")
    main.call("ProcessSimulation", 30.0)
    main.call("ToggleMapEditor")
    await process_frame
    rover = unit_view(main, "unit_rover")
    var reset_position := rover.position
    main.call("ProcessSimulation", 0.049)
    check(rover.position == reset_position, "editor resume inherited timing or commands")

    await load_mission(main, "mission_wells_at_the_ridge")
    rover = unit_view(main, "unit_grunt")
    main.call("SelectSingleAt", rover.position)
    var ridge_start := rover.position
    main.call("CommandSelectedUnits", rover.position + Vector2(140, 0))
    for frame in range(120):
        main.call("ProcessSimulation", 1.0 / 120)
    check(rover.position.distance_to(ridge_start) > 10, "Level 2 movement did not advance")

    # A hitch must leave interpolation valid; terminal states must freeze cleanly.
    main.call("ProcessSimulation", 1.025)
    check(is_finite(rover.position.x) and is_finite(rover.position.y), "hitch produced an invalid visual pose")
    main.call("HandleDebugHotkey", KEY_F7)
    main.call("ProcessSimulation", 0.05)
    var terminal_position := rover.position
    main.call("ProcessSimulation", 2.0)
    check(rover.position == terminal_position, "terminal mission kept moving")

    if "--capture" in OS.get_cmdline_user_args():
        if DisplayServer.get_name() == "headless":
            check(false, "--capture requires a graphical renderer; omit --headless")
        else:
            await load_mission(main, "mission_first_landing")
            main.get_node("GreyboxCamera").position = Vector2(-100, 70)
            main.get_node("GreyboxCamera").zoom = Vector2(0.85, 0.85)
            main.call("ProcessSimulation", 0.025)
            await RenderingServer.frame_post_draw
            var output_dir := ProjectSettings.globalize_path("res://../artifacts/fixed-step-review").simplify_path()
            check(DirAccess.make_dir_recursive_absolute(output_dir) == OK, "could not create capture directory")
            check(root.get_texture().get_image().save_png(output_dir.path_join("presentation.png")) == OK, "could not save capture")
    print("FIXED_STEP_PRESENTATION_", "PASS" if failures == 0 else "FAIL")
    quit(0 if failures == 0 else 1)
