extends SceneTree

const IDS = ["pine_a", "pine_b", "cherry_a", "cherry_b"]
const OUT = "res://../art/props/trees/previews/"

func _initialize():
    call_deferred("verify")

func capture(name):
    await process_frame
    await RenderingServer.frame_post_draw
    root.get_texture().get_image().save_png(OUT + name + ".png")

func verify():
    root.size = Vector2i(1280, 800)
    root.msaa_3d = Viewport.MSAA_4X
    DirAccess.make_dir_recursive_absolute(ProjectSettings.globalize_path(OUT))
    var material = load("res://assets/models/props/trees/tree_props.tres")
    var metrics = JSON.parse_string(FileAccess.get_file_as_string("res://../art/props/trees/tree_metrics.json"))
    var study = load("res://scenes/studies/TreePropsStudy.tscn").instantiate()
    root.add_child(study); current_scene = study
    for i in range(8): await process_frame
    for i in range(4):
        var tree = study.get_node(IDS[i])
        assert(tree.scale.is_equal_approx(Vector3.ONE))
        assert(tree.find_children("*", "Skeleton3D", true, false).is_empty())
        var meshes = tree.find_children("*", "MeshInstance3D", true, false)
        if tree is MeshInstance3D: meshes.append(tree)
        assert(meshes.size() == 1, "One merged mesh per prop")
        var mesh = meshes[0]
        assert(mesh.mesh.get_surface_count() == 1)
        assert(mesh.get_active_material(0) == material, "All variants share the persistent external material")
        var bounds = tree.global_transform.affine_inverse() * mesh.global_transform * mesh.get_aabb()
        assert(abs(bounds.position.y) < .002, "Ground-level origin")
        assert(abs(bounds.size.y - metrics[i].dimensions[2]) < .003, "Height changed on import")
        var arrays = mesh.mesh.surface_get_arrays(0)
        var tris = arrays[Mesh.ARRAY_INDEX].size() / 3
        assert(tris == metrics[i].triangles and tris <= 800, "Triangle budget changed on export")
        # MultiMesh accepts each exported mesh directly, with no rig or extra surfaces.
        var batch = MultiMesh.new()
        batch.transform_format = MultiMesh.TRANSFORM_3D
        batch.mesh = mesh.mesh; batch.instance_count = 2
        batch.set_instance_transform(0, Transform3D.IDENTITY)
        batch.set_instance_transform(1, Transform3D(Basis(Vector3.UP, PI / 2), Vector3(6,0,0)))
        assert(batch.mesh.get_surface_count() == 1 and batch.instance_count == 2)
        print("TREE_PROP: ", IDS[i], " triangles=", tris, " height=", bounds.size.y)
    await capture("lineup")
    study.call("SetView", 2); await capture("pines")
    study.call("SetView", 3); await capture("cherries")
    study.call("SetYaw", 180.0); await capture("cherries_rear")
    study.call("SetView", 2); await capture("pines_rear")
    study.call("SetView", 1); study.call("SetYaw", 0.0)
    study.get_node("Camera").size = 25
    await capture("far_lineup")
    print("TREE_PROPS_PASS: four distinct props, dimensions, ground origins, budgets, shared material and MultiMesh compatibility")
    quit()
