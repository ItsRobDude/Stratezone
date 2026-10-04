extends SceneTree

# Cross-export contract check, plus real 1280x800 Godot preview captures.
# Run through tools/godot.ps1; requires a graphical renderer for screenshots.

func _initialize():
    call_deferred("capture")

func capture():
    root.size = Vector2i(1280, 800)
    root.msaa_3d = Viewport.MSAA_4X
    var study = load("res://scenes/studies/RiflemanStudy.tscn").instantiate()
    root.add_child(study)
    current_scene = study
    for i in range(15): await process_frame
    var soldier = study.get_node("Rifleman")
    var player = soldier.get_node("SharedAnimations")
    var skeleton = soldier.find_children("*", "Skeleton3D", true, false)[0]
    assert(skeleton.get_bone_count() == 25, "Expected shared skeleton and baked helpers")
    var meshes = soldier.find_children("*", "MeshInstance3D", true, false)
    assert(meshes.size() == 1 and meshes[0].mesh.get_surface_count() == 1, "Expected one mesh/surface")
    print("RIFLEMAN_VERIFY: bones=", skeleton.get_bone_count(), " mesh=1 surface=1")
    assert(meshes[0].get_active_material(0) is ShaderMaterial, "Team shader must import with model")
    var lib = player.get_animation_library("")
    assert(lib.get_animation_list().size() == 3)
    for name in lib.get_animation_list():
        var anim = lib.get_animation(name)
        assert(anim.loop_mode == Animation.LOOP_LINEAR, "Clip must loop after suffix import")
        for tr in range(anim.get_track_count()):
            var path = anim.track_get_path(tr)
            var node_path = NodePath(str(path).split(":")[0])
            assert(soldier.has_node(node_path), "Unbound animation node: " + str(path))
            if path.get_subname_count() > 0:
                assert(skeleton.find_bone(str(path.get_subname(0))) >= 0, "Unbound bone")
        player.play(name)
        player.seek(0, true)
        skeleton.force_update_all_bone_transforms()
        var start = skeleton.get_bone_pose_rotation(skeleton.find_bone("mixamorig_LeftLeg"))
        player.seek(anim.length * 0.4, true)
        skeleton.force_update_all_bone_transforms()
        var changed = start.angle_to(skeleton.get_bone_pose_rotation(skeleton.find_bone("mixamorig_LeftLeg")))
        assert(changed > 0.001, "Animation must move the shared skeleton")
        print("RIFLEMAN_CLIP: ", name, " duration=", anim.length, " tracks=", anim.get_track_count(), " left_leg_motion_rad=", changed)
    study.call("SelectClip", "rifle_ready_idle"); player.seek(0, true); player.speed_scale=0
    await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png("res://../art/units/rifleman/previews/godot_hub_scale.png")
    study.call("SetClose", true)
    await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png("res://../art/units/rifleman/previews/godot_reference.png")
    for clip in ["rifle_aim_idle", "rifle_run"]:
        study.call("SelectClip", clip);player.seek(player.current_animation_length*.35, true);player.speed_scale=0
        await process_frame
        await RenderingServer.frame_post_draw
        root.get_texture().get_image().save_png("res://../art/units/rifleman/previews/godot_"+clip+".png")
    study.call("SelectClip", "rifle_ready_idle");player.seek(0, true);player.speed_scale=0
    meshes[0].set_instance_shader_parameter("team_color", Color("e0473f"))
    await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png("res://../art/units/rifleman/previews/godot_enemy.png")
    print("RIFLEMAN_VERIFY: shared library bindings and screenshots complete")
    quit()
