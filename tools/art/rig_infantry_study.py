"""Mixamo round-trip, rigid gear weights, and shared animation export helpers.

Uses Blender 5.2's ufbx importer consistently for both body and clips. No
runtime constraints or bone-parented armor objects are exported.
"""
from pathlib import Path
import json
import math
import sys
import bpy
import bmesh
from mathutils import Vector, Matrix, Quaternion
from mathutils.kdtree import KDTree

sys.path.insert(0,str(Path(__file__).resolve().parent))
import infantry_study_common as C

RIG='SZ_Infantry'
EXPORT=C.ROOT/'game/assets/models/studies/rifleman'
PREFIX='mixamorig:'


def bone_name(semantic):
    if semantic in ('pauldron.L','pauldron.R','weapon'):return semantic
    if semantic[0] in ('L','R') and semantic[1:] in ('Shoulder','UpperArm','ForeArm','Hand','UpLeg','Leg','Foot','Toe'):
        tail={'UpperArm':'Arm','Toe':'ToeBase'}.get(semantic[1:],semantic[1:])
        return PREFIX+('Left' if semantic[0]=='L' else 'Right')+tail
    return PREFIX+semantic


def import_base(path):
    if bpy.data.objects.get(RIG):raise RuntimeError('Canonical armature already exists')
    before=set(bpy.data.objects)
    bpy.ops.wm.fbx_import(filepath=str(path),use_anim=False,ignore_leaf_bones=False)
    added=set(bpy.data.objects)-before
    rig=next(o for o in added if o.type=='ARMATURE'); rig.name=RIG
    body=next(o for o in added if o.type=='MESH');body.name='Mixamo_Body_Weights'
    # OBJ uses meters, whereas FBX labels these numbers as cm. Recover known
    # uploaded dimensions rather than accepting the importer's 100x shrink.
    rig.scale=(1,1,1)
    bpy.context.view_layer.update()
    rig.matrix_world=Matrix.Rotation(math.pi,4,'Z')@rig.matrix_world
    bpy.context.view_layer.update()
    body.hide_render=True;body.hide_set(True)
    rig['source']='Our custom infantry body, Mixamo No Fingers (25)'
    rig['canonical_shared_base']=True
    remove_end_bones(rig)
    rig['source_importer']='Blender wm.fbx_import (ufbx); remove named *_End bones only'
    return rig,body


def remove_end_bones(rig):
    bpy.context.view_layer.objects.active=rig
    rig.hide_set(False);rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    for b in list(rig.data.edit_bones):
        if b.name.endswith('_End'):rig.data.edit_bones.remove(b)
    bpy.ops.object.mode_set(mode='OBJECT')


def add_helpers():
    rig=bpy.data.objects[RIG]
    bpy.context.view_layer.objects.active=rig
    rig.hide_set(False);rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    inverse=rig.matrix_world.inverted()
    for side,sign in [('L',1),('R',-1)]:
        name='pauldron.'+side
        arm=rig.data.edit_bones[bone_name(side+'UpperArm')]
        b=rig.data.edit_bones.new(name)
        b.matrix=arm.matrix.copy()
        b.head=inverse@Vector((sign*.29,0,1.68))
        b.tail=b.head+(arm.tail-arm.head).normalized()*.14
        b.parent=rig.data.edit_bones[bone_name(side+'Shoulder')]
        b.use_deform=True
    hand=rig.data.edit_bones[bone_name('RHand')]
    b=rig.data.edit_bones.new('weapon');b.matrix=hand.matrix.copy()
    b.head=hand.head;b.tail=hand.head+(hand.tail-hand.head).normalized()*.1
    b.parent=hand;b.use_deform=True
    bpy.ops.object.mode_set(mode='OBJECT')
    return rig


def bind():
    rig=bpy.data.objects[RIG];reference=bpy.data.objects['Mixamo_Body_Weights']
    body=bpy.data.objects['Shared_Infantry_Base_TPose']
    tree=KDTree(len(reference.data.vertices))
    for v in reference.data.vertices:tree.insert(reference.matrix_world@v.co,v.index)
    tree.balance();body.vertex_groups.clear();max_distance=0
    for g in reference.vertex_groups:body.vertex_groups.new(name=g.name)
    for v in body.data.vertices:
        _,idx,dist=tree.find(v.co);max_distance=max(max_distance,dist)
        values=sorted(reference.data.vertices[idx].groups,key=lambda g:g.weight,reverse=True)[:4]
        total=sum(g.weight for g in values)
        if total<=0:raise RuntimeError('Unweighted base vertex')
        for g in values:body.vertex_groups[reference.vertex_groups[g.group].name].add([v.index],g.weight/total,'REPLACE')
    for obj in bpy.data.collections[C.PARTS].objects:
        if obj!=body:
            semantic=obj['rigid_bone'];name=bone_name(semantic)
            if name not in rig.data.bones:raise RuntimeError('Missing '+name)
            obj.vertex_groups.clear();g=obj.vertex_groups.new(name=name)
            g.add(list(range(len(obj.data.vertices))),1,'REPLACE')
        world=obj.matrix_world.copy();obj.parent=rig;obj.matrix_world=world
        mod=obj.modifiers.new('Shared infantry skin','ARMATURE');mod.object=rig
    body['production_rig']='Custom base auto-rigged once by Mixamo; SZ_Infantry shared skeleton'
    print('Skin transfer maximum position difference:',max_distance)


def import_clip(path,name):
    before=set(bpy.data.objects)
    bpy.ops.wm.fbx_import(filepath=str(path),use_anim=True,ignore_leaf_bones=False)
    added=set(bpy.data.objects)-before
    source=next(o for o in added if o.type=='ARMATURE')
    remove_end_bones(source)
    target=bpy.data.objects[RIG]
    expected={b.name for b in target.data.bones if b.name.startswith(PREFIX)}
    if expected!={b.name for b in source.data.bones}:
        raise RuntimeError('Animation and custom skeleton bone sets disagree')
    source.scale=(1,1,1)
    source_action=source.animation_data.action
    start,end=map(round,source_action.frame_range)
    action=bpy.data.actions.new(name);action.use_fake_user=True
    target.animation_data_create();target.animation_data.action=action
    # Mixamo's unskinned FBX normalizes the T pose differently from its skinned
    # T-pose download. Bake absolute skeletal poses instead of copying curves.
    # The animation was generated for THIS uploaded body, not a stock actor.
    for frame in range(start,end+1):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        conversion=target.matrix_world.inverted()@source.matrix_world
        desired={b.name:conversion@b.matrix for b in source.pose.bones}
        for bone in target.data.bones:
            if bone.name not in desired:continue
            pb=target.pose.bones[bone.name]
            basis=bone.matrix_local.inverted()@desired[bone.name]
            if bone.parent:
                basis=bone.matrix_local.inverted()@bone.parent.matrix_local@desired[bone.parent.name].inverted()@desired[bone.name]
            pb.matrix_basis=basis
            C.enum_set(pb,'rotation_mode','QUATERNION')
            pb.keyframe_insert('location',frame=frame,group=bone.name)
            pb.keyframe_insert('rotation_quaternion',frame=frame,group=bone.name)
            pb.keyframe_insert('scale',frame=frame,group=bone.name)
    action['source_fbx']=str(path)
    action['source_character']='Stratezone custom infantry base'
    action['bake_note']='Absolute pose bake onto single master; ufbx rest normalization verified'
    for obj in added:bpy.data.objects.remove(obj,do_unlink=True)
    if not source_action.users:bpy.data.actions.remove(source_action)
    bpy.context.scene.render.fps=30
    bpy.context.scene.frame_set(1)
    return action


def bake_helpers(action):
    rig=bpy.data.objects[RIG]
    rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    start,end=map(round,action.frame_range)
    for frame in range(start,end+1):
        bpy.context.scene.frame_set(frame)
        for side in ('L','R'):
            b=rig.pose.bones['pauldron.'+side];C.enum_set(b,'rotation_mode','QUATERNION')
            arm=rig.pose.bones[bone_name(side+'UpperArm')]
            q=arm.matrix_basis.to_quaternion()
            b.rotation_quaternion=Quaternion().slerp(q,.42)
            b.keyframe_insert('rotation_quaternion',frame=frame,group=b.name)
        b=rig.pose.bones['weapon'];C.enum_set(b,'rotation_mode','QUATERNION')
        b.rotation_quaternion=Quaternion();b.keyframe_insert('rotation_quaternion',frame=frame,group='weapon')
    bpy.context.scene.frame_set(start)


def align_weapon():
    rig=bpy.data.objects[RIG]
    bpy.context.scene.frame_set(1);bpy.context.view_layer.update()
    def palm(side):
        return rig.matrix_world@rig.pose.bones[bone_name(side+'Hand')].matrix@Vector((0,.07,0))
    right,left=palm('R'),palm('L')
    forward=(left-right).normalized()
    y=-forward;x=y.cross(Vector((0,0,1))).normalized();z=x.cross(y).normalized()
    desired=Matrix((x,y,z)).transposed().to_4x4()
    desired.translation=right+z*.058
    bone=rig.pose.bones['weapon']
    rest=rig.matrix_world@bone.bone.matrix_local
    posed=rig.matrix_world@bone.matrix
    transform=rest@posed.inverted()@desired
    rig['support_point_rest']=list(transform@Vector((0,-.265,.025)))
    for obj in bpy.data.collections[C.PARTS].objects:
        if obj.get('weapon_local'):
            obj.data.transform(transform);obj['weapon_local']=False
            obj.hide_render=False;obj.hide_set(False)
    print('Weapon grip, support palm:',list(right),list(left),'distance',(left-right).length)


def fit_support_hand(action):
    """Analytical two-bone correction, baked into clips; no runtime IK.

    Keep the sampled elbow side, preserve limb lengths, and place the left
    palm under this bullpup's foreguard instead of a stock clip's long rifle.
    """
    rig=bpy.data.objects[RIG];rig.animation_data.action=action
    if action.slots:rig.animation_data.action_slot=action.slots[0]
    start,end=map(round,action.frame_range)
    samples=[]
    names=[bone_name('LUpperArm'),bone_name('LForeArm'),bone_name('LHand')]
    support=Vector(rig['support_point_rest'])
    for frame in range(start,end+1):
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        a,b,h=[rig.pose.bones[n] for n in names]
        world=rig.matrix_world
        matrices=[world@p.matrix for p in (a,b,h)]
        shoulder,elbow,wrist=[m.translation for m in matrices]
        weapon=rig.pose.bones['weapon']
        deformation=world@weapon.matrix@weapon.bone.matrix_local.inverted()@world.inverted()
        palm_target=deformation@support
        hand_offset=matrices[2].to_3x3()@Vector((0,.07,0))
        target=palm_target-hand_offset
        upper=(elbow-shoulder).length;lower=(wrist-elbow).length
        direction=(target-shoulder).normalized()
        d=min((target-shoulder).length,upper+lower-.001)
        along=(upper*upper-lower*lower+d*d)/(2*d)
        height=math.sqrt(max(0,upper*upper-along*along))
        pole=(elbow-shoulder)-direction*(elbow-shoulder).dot(direction)
        if pole.length<.001:pole=Vector((1,0,-1))
        new_elbow=shoulder+direction*along+pole.normalized()*height
        new_mats=[]
        for old,origin,old_dir,new_dir in [(matrices[0],shoulder,elbow-shoulder,new_elbow-shoulder),
                                         (matrices[1],new_elbow,wrist-elbow,target-new_elbow)]:
            rotation=old_dir.rotation_difference(new_dir)
            m=rotation.to_matrix().to_4x4()@old;m.translation=origin;new_mats.append(m)
        hand=matrices[2].copy();hand.translation=target;new_mats.append(hand)
        samples.append((frame,[world.inverted()@m for m in new_mats]))
    for frame,matrices in samples:
        bpy.context.scene.frame_set(frame);bpy.context.view_layer.update()
        desired=dict(zip(names,matrices))
        for name in names:
            pb=rig.pose.bones[name];bone=pb.bone
            parent=desired.get(pb.parent.name,pb.parent.matrix)
            pb.matrix_basis=bone.matrix_local.inverted()@bone.parent.matrix_local@parent.inverted()@desired[name]
            pb.keyframe_insert('location',frame=frame,group=name)
            pb.keyframe_insert('rotation_quaternion',frame=frame,group=name)
            pb.keyframe_insert('scale',frame=frame,group=name)
    bpy.context.scene.frame_set(start)


def prune_body(obj):
    """Remove only faces fully covered by this kit; shared source stays intact."""
    bm=bmesh.new();bm.from_mesh(obj.data)
    hidden=[]
    for f in bm.faces:
        pts=[v.co for v in f.verts]
        if all(p.z<.16 or p.z>2.10 for p in pts):hidden.append(f)
        elif all(1.33<p.z<1.69 and abs(p.x)<.2 for p in pts):hidden.append(f)
    bmesh.ops.delete(bm,geom=hidden,context='FACES')
    bm.to_mesh(obj.data);bm.free()


def merged_mesh():
    rig=bpy.data.objects[RIG]
    C.enum_set(rig.data,'pose_position','REST')
    copies=[]
    for source in bpy.data.collections[C.PARTS].objects:
        obj=source.copy();obj.data=source.data.copy();bpy.context.scene.collection.objects.link(obj)
        obj.hide_render=False;obj.hide_set(False)
        for mod in list(obj.modifiers):
            if mod.type=='ARMATURE':obj.modifiers.remove(mod)
        if source.name=='Shared_Infantry_Base_TPose':prune_body(obj)
        bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers):bpy.ops.object.modifier_apply(modifier=mod.name)
        copies.append(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in copies:obj.select_set(True)
    bpy.context.view_layer.objects.active=copies[0]
    bpy.ops.object.join()
    merged=bpy.context.object;merged.name='Rifleman'
    # Transform is already at the shared armature's inverse; joining preserves it.
    mod=merged.modifiers.new('Shared infantry skin','ARMATURE');mod.object=rig
    merged.data.calc_loop_triangles()
    if len(merged.data.loop_triangles)>4000:raise RuntimeError('Infantry triangle budget exceeded: '+str(len(merged.data.loop_triangles)))
    return merged


def export(merged):
    import io_scene_gltf2
    rig=bpy.data.objects[RIG]
    # Opaque glTF vertex colors can drop alpha. Preserve the team mask in UV0.x.
    uv=merged.data.uv_layers.get('TeamMask') or merged.data.uv_layers.new(name='TeamMask')
    tint=merged.data.color_attributes['Tint']
    for loop in merged.data.loops:uv.data[loop.index].uv=(tint.data[loop.vertex_index].color[3],0)
    EXPORT.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);merged.select_set(True)
    C.enum_set(rig.data,'pose_position','REST')
    options=dict(export_format='GLB',use_selection=True,use_active_scene=True,
                 export_cameras=False,export_lights=False,export_extras=True,
                 export_skins=True,export_influence_nb=4,export_rest_position_armature=True)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT/'rifleman_study.glb'),export_animations=False,**options)
    merged.select_set(False)
    C.enum_set(rig.data,'pose_position','POSE')
    bpy.ops.export_scene.gltf(filepath=str(EXPORT/'infantry_anims.glb'),export_animations=True,
        export_animation_mode='ACTIONS',export_anim_single_armature=True,
        export_bake_animation=True,export_optimize_animation_size=False,**options)
    merged.hide_render=True;merged.hide_set(True)
    return EXPORT
