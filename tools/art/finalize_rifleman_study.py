"""Verify and re-export the saved, editable Rifleman study in Blender 5.2.

Pass art/units/rifleman/rifleman_study.blend to Blender --background before
--python. This is a saved-source export, not a new Mixamo upload. The final
shared-master extraction removes unrelated scenes in this disposable process.
"""
from pathlib import Path
import hashlib
import json
import sys
import bpy
import bmesh
from mathutils import Vector
from mathutils.kdtree import KDTree

sys.path.insert(0, str(Path(__file__).resolve().parent))
import infantry_study_common as C
import rig_infantry_study as R


def finalize():
    if not bpy.app.background:
        raise RuntimeError('Run this finalizer in a separate Blender --background process')
    scene = bpy.data.scenes[C.SCENE]
    bpy.context.window.scene = scene
    rig = bpy.data.objects[R.RIG]
    body = bpy.data.objects['Shared_Infantry_Base_TPose']
    actions = sorted(bpy.data.actions, key=lambda a: a.name)
    assert {a.name for a in actions} == {
        'rifle_ready_idle_loop', 'rifle_aim_idle_loop', 'rifle_run_loop'}
    assert len(rig.data.bones) == 25
    assert not any(b.constraints for b in rig.pose.bones)
    C.enum_set(rig.data, 'pose_position', 'REST')
    bpy.context.view_layer.update()
    for obj in bpy.data.collections[C.PARTS].objects:
        for vertex in obj.data.vertices:
            weights = [g.weight for g in vertex.groups if g.weight > 0]
            assert 1 <= len(weights) <= 4, (obj.name, vertex.index, weights)
            assert abs(sum(weights) - 1) < 1e-5, (obj.name, vertex.index)
            if obj.get('rigid_bone'):
                assert len(weights) == 1 and weights[0] == 1
    bm = bmesh.new(); bm.from_mesh(body.data)
    non_manifold = sum(not e.is_manifold for e in bm.edges); bm.free()
    assert non_manifold == 0
    body.data.calc_loop_triangles()
    reference = bpy.data.objects['Mixamo_Body_Weights']
    tree = KDTree(len(reference.data.vertices))
    for v in reference.data.vertices:
        tree.insert(reference.matrix_world @ v.co, v.index)
    tree.balance()
    transfer_distance = max(tree.find(body.matrix_world @ v.co)[2] for v in body.data.vertices)
    # Retain the exact geometry/axis convention of the successful first upload.
    lines = ['# Stratezone custom base, meters, Y up, front +Z', 'o infantry_base_upload']
    lines += [f'v {v.co.x:.7f} {v.co.z:.7f} {-v.co.y:.7f}' for v in body.data.vertices]
    lines += ['f ' + ' '.join(str(i + 1) for i in p.vertices) for p in body.data.polygons]
    (C.SHARED / 'infantry_base_upload.obj').write_text('\n'.join(lines) + '\n')
    old = bpy.data.objects.get('Rifleman')
    if old: bpy.data.objects.remove(old, do_unlink=True)
    merged = R.merged_mesh()
    triangles = len(merged.data.loop_triangles)
    assert len(merged.data.materials) == 1
    C.enum_set(rig.data, 'pose_position', 'POSE')
    clips = []
    support = Vector(rig['support_point_rest'])
    for action in actions:
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
        start, end = map(round, action.frame_range)
        errors = []
        for frame in range(start, end + 1):
            scene.frame_set(frame); bpy.context.view_layer.update()
            weapon = rig.pose.bones['weapon']
            world = rig.matrix_world
            target = world @ weapon.matrix @ weapon.bone.matrix_local.inverted() @ world.inverted() @ support
            hand = world @ rig.pose.bones[R.bone_name('LHand')].matrix @ Vector((0, .07, 0))
            errors.append((target - hand).length)
        assert max(errors) < .002, (action.name, max(errors))
        clips.append(dict(blender_action=action.name, godot_name=action.name.removesuffix('_loop'),
                          frames=end-start+1, fps=30, loop=True,
                          maximum_support_palm_error_m=max(errors)))
    R.export(merged)
    rig.animation_data.action = bpy.data.actions['rifle_ready_idle_loop']
    rig.animation_data.action_slot = rig.animation_data.action.slots[0]
    scene.frame_start = 1; scene.frame_end = 232; scene.frame_set(1)
    scene['status'] = 'ROUGH rig and scale study; major visual polish required against sprite before acceptance'
    scene['production_approved'] = False
    metrics = dict(blender_version=bpy.app.version_string, triangles=triangles, mesh_count=1,
                   material_surfaces=1, bones=25, mixamo_bones_after_end_removal=22,
                   helpers=['pauldron.L', 'pauldron.R', 'weapon'], maximum_influences=4,
                   rigid_gear_verified=True, shared_body_triangles=len(body.data.loop_triangles),
                   shared_body_non_manifold_edges=non_manifold,
                   weight_transfer_max_distance_m=transfer_distance,
                   authored_boot_bottom_m=.018, authored_helmet_top_m=2.225,
                   clips=clips, skeleton=[dict(name=b.name,parent=b.parent.name if b.parent else None)
                                         for b in rig.data.bones],
                   sources_sha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest()
                                   for p in sorted((C.SHARED/'mixamo').glob('*.fbx'))})
    (C.SOURCE/'study_metrics.json').write_text(json.dumps(metrics, indent=2)+'\n')
    # Save the complete editable source before extracting the separate master.
    # Avoid libraries.write from the live MCP timer (crashed during this study).
    bpy.ops.wm.save_as_mainfile(filepath=str(C.SOURCE/'rifleman_study.blend'), compress=True)
    # A minimal master file: our base, the exact shared skeleton, and its clips.
    shared = bpy.data.scenes.new('SZ_Shared_Infantry_Master')
    shared.collection.objects.link(rig); shared.collection.objects.link(body)
    shared.render.fps = 30
    shared['usage'] = 'Open this master for the next kit. Preserve SZ_Infantry and the rest hierarchy.'
    C.enum_set(rig.data, 'pose_position', 'REST')
    bpy.context.window.scene = shared
    for other in list(bpy.data.scenes):
        if other != shared: bpy.data.scenes.remove(other)
    for obj in list(bpy.data.objects):
        if obj not in (rig, body): bpy.data.objects.remove(obj, do_unlink=True)
    bpy.ops.wm.save_as_mainfile(filepath=str(C.SHARED/'shared_infantry.blend'), compress=True)
    print(json.dumps({k:v for k,v in metrics.items() if k not in ('skeleton','sources_sha256')}, indent=2))


if __name__ == '__main__':
    finalize()
