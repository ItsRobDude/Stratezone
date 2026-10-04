"""Custom shared infantry study geometry.

Meters, Z up, forward -Y. Custom authored meshes; no downloaded characters.
The T-pose body was auto-rigged once in Mixamo; reuse the saved shared master.
"""
from pathlib import Path
import math
import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/units/rifleman'
SHARED = ROOT / 'art/units/shared_infantry'
SCENE = 'SZ_Rifleman_Study'
PARTS = 'Rifleman_Editable'
MAT = 'SZ_Infantry_Study'
PALETTE = dict(cloth='797052', cloth_dark='5C5741', cloth_light='938665',
               armor='393E3C', edge='58605A', webbing='666247', pouch='716B4C',
               skin='BB8962', dark='242B29', boot='50483C', steel='656C64',
               lens='277886', team='46C2CC', rifle='777457')


def enum_set(owner, key, value):
    allowed = [i.identifier for i in owner.bl_rna.properties[key].enum_items]
    if value not in allowed:
        raise ValueError(f'{key}: {value} not in {allowed}')
    setattr(owner, key, value)


def rgba(color, mask=0):
    h = PALETTE.get(color, color)
    vals = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    return tuple(v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in vals) + (mask,)


def setup():
    if SCENE in bpy.data.scenes:
        raise RuntimeError('Study already exists; do not overwrite it implicitly.')
    previous = bpy.context.scene
    scene = bpy.data.scenes.new(SCENE)
    scene.render.engine = previous.render.engine
    scene.view_settings.view_transform = previous.view_settings.view_transform
    bpy.context.window.scene = scene
    for name in (PARTS, 'Rifleman_Stage', 'Infantry_Shared_Source'):
        scene.collection.children.link(bpy.data.collections.new(name))
    scene['status'] = 'Sprite fidelity and articulation study; see shared infantry master'
    scene['height_m'] = 2.23
    m = bpy.data.materials.new(MAT)
    shader = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    tint = m.node_tree.nodes.new('ShaderNodeVertexColor')
    tint.layer_name = 'Tint'
    m.node_tree.links.new(tint.outputs['Color'], shader.inputs['Base Color'])
    m.node_tree.links.new(tint.outputs['Color'], shader.inputs['Emission Color'])
    m.node_tree.links.new(tint.outputs['Alpha'], shader.inputs['Emission Strength'])
    shader.inputs['Roughness'].default_value = .78
    shader.inputs['Metallic'].default_value = .10
    return scene


def mesh(name, vertices, faces, color='cloth', bone=None, weights=None,
         bevel=0, smooth=False, mask=0, collection=PARTS):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.data.collections[collection].objects.link(obj)
    data.materials.append(bpy.data.materials[MAT])
    attr = data.color_attributes.new(name='Tint', type='FLOAT_COLOR', domain='POINT')
    colors = color if isinstance(color, list) else [color] * len(vertices)
    for item, c in zip(attr.data, colors):
        item.color = rgba(c, mask)
    for p in data.polygons:
        p.use_smooth = smooth and len(p.vertices) == 4
    if bone:
        weights = [{bone: 1.0}] * len(vertices)
        obj['rigid_bone'] = bone
    if weights:
        for group in sorted({k for row in weights for k in row}):
            g = obj.vertex_groups.new(name=group)
            for i, row in enumerate(weights):
                if row.get(group, 0) > 0:
                    g.add([i], row[group], 'REPLACE')
    if bevel:
        mod = obj.modifiers.new('Armor edge', 'BEVEL')
        mod.width = bevel
        mod.segments = 1
        enum_set(mod, 'limit_method', 'ANGLE')
    return obj


def box(name, center, size, color='armor', bone=None, bevel=.01, mask=0):
    x, y, z = (v / 2 for v in size)
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    v = [tuple(Vector(p) + Vector(center)) for p in v]
    f = [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
    return mesh(name, v, f, color, bone, bevel=bevel if min(size)>.075 else 0, mask=mask)


def rings(name, sections, color='cloth', bone=None, sides=8, axis='Z', smooth=True, weights=None):
    verts, faces = [], []
    for center, a, b in sections:
        for i in range(sides):
            theta = math.tau * i / sides
            p = (a*math.cos(theta), b*math.sin(theta), 0) if axis == 'Z' else (0, a*math.cos(theta), b*math.sin(theta))
            verts.append(tuple(Vector(center)+Vector(p)))
    for j in range(len(sections)-1):
        for i in range(sides):
            faces.append((j*sides+i, j*sides+(i+1)%sides, (j+1)*sides+(i+1)%sides, (j+1)*sides+i))
    faces.extend([tuple(reversed(range(sides))), tuple(range((len(sections)-1)*sides, len(sections)*sides))])
    return mesh(name, verts, faces, color, bone, weights=weights, smooth=smooth)


def rod(name, a, b, radius, color='dark', bone=None, sides=8, mask=0):
    a, b = Vector(a), Vector(b)
    q = (b-a).to_track_quat('Z','Y')
    verts = [tuple(a + q @ Vector((radius*math.cos(i*math.tau/sides), radius*math.sin(i*math.tau/sides), z)))
             for z in (0, (b-a).length) for i in range(sides)]
    faces = [tuple(reversed(range(sides))), tuple(range(sides,sides*2))]
    faces += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    return mesh(name, verts, faces, color, bone, smooth=True, mask=mask)


def shared_body():
    """Connected closed T-pose mesh: torso branches into arms and legs.

    Explicit joint rings stay editable; all branches are bridged, not overlapped.
    The two crotch fans share an edge. No remeshing or downloaded base is used.
    """
    v, f, w, colors = [], [], [], []

    def vertex(p, weights, color='cloth'):
        v.append(p); w.append(weights); colors.append(color)
        return len(v)-1

    def ring(center, a, b, weights, color='cloth', n=12, axis='Z', phase=0):
        result = []
        for i in range(n):
            t = math.tau*i/n+phase
            p = (a*math.sin(t), -b*math.cos(t), 0) if axis == 'Z' else (0, -a*math.cos(t), b*math.sin(t))
            result.append(vertex(tuple(Vector(center)+Vector(p)), weights, color))
        return result

    def bridge(a, b, skip=()):
        # Unequal ring counts introduce a few triangles at limb roots only.
        ia = ib = 0
        while ia < len(a) or ib < len(b):
            na, nb = (ia+1)/len(a), (ib+1)/len(b)
            if abs(na-nb) < 1e-7:
                if ia not in skip:
                    f.append((a[ia%len(a)],a[(ia+1)%len(a)],b[(ib+1)%len(b)],b[ib%len(b)]))
                ia += 1; ib += 1
            elif na < nb:
                f.append((a[ia%len(a)],a[(ia+1)%len(a)],b[ib%len(b)])); ia += 1
            else:
                f.append((a[ia%len(a)],b[(ib+1)%len(b)],b[ib%len(b)])); ib += 1

    torso = []
    for z,a,b,weights in [(1.08,.245,.145,{'Hips':1}), (1.18,.21,.135,{'Hips':.6,'Spine':.4}),
        (1.29,.202,.14,{'Spine':1}), (1.51,.265,.16,{'Spine1':.25,'Spine2':.75}),
        (1.71,.27,.145,{'Spine2':1}), (1.79,.17,.11,{'Spine2':.65,'Neck':.35}),
        (1.84,.084,.08,{'Neck':1}), (1.88,.09,.087,{'Neck':.4,'Head':.6}),
        (1.91,.105,.095,{'Head':1}), (1.98,.14,.114,{'Head':1}),
        (2.075,.14,.118,{'Head':1}), (2.14,.103,.09,{'Head':1}), (2.17,.04,.045,{'Head':1})]:
        torso.append(ring((0,0,z),a,b,weights,'skin' if z>=1.84 else 'cloth'))
    for j in range(len(torso)-1):
        bridge(torso[j],torso[j+1],skip=(2,3,8,9) if j==3 else ())
    f.append(tuple(torso[-1]))
    for side, sign, inds in [('L',1,[2,3,4]),('R',-1,[8,9,10])]:
        hole = [torso[3][i] for i in inds] + [torso[4][i] for i in reversed(inds)]
        arm = []
        for x, a, b, weights in [(.30,.102,.117,{f'{side}UpperArm':1}),
            (.42,.10,.113,{f'{side}UpperArm':1}),(.565,.081,.086,{f'{side}UpperArm':.85,f'{side}ForeArm':.15}),
            (.61,.081,.08,{f'{side}UpperArm':.5,f'{side}ForeArm':.5}),
            (.65,.082,.083,{f'{side}UpperArm':.15,f'{side}ForeArm':.85}),
            (.79,.077,.082,{f'{side}ForeArm':1}),(.91,.059,.062,{f'{side}ForeArm':1}),
            (.945,.06,.06,{f'{side}ForeArm':.3,f'{side}Hand':.7}),
            (1.015,.059,.047,{f'{side}Hand':1}),(1.105,.045,.035,{f'{side}Hand':1})]:
            arm.append(ring((sign*x,0,1.645),a,b,weights,'dark' if x>.93 else 'cloth',n=8,axis='X',phase=math.pi*.25))
        # Order the arm-root vertices by nearest angle around the opening.
        root_order = sorted(hole,key=lambda idx:math.atan2(v[idx][2]-1.61, -v[idx][1]))
        arm0 = sorted(arm[0],key=lambda idx:math.atan2(v[idx][2]-1.645,-v[idx][1]))
        bridge(root_order,arm0)
        for a,b in zip(arm,arm[1:]): bridge(a,b)
        f.append(tuple(arm[-1]))
    crotch = vertex((0,0,1.01),{'Hips':1})
    for side,sign,indices in [('L',1,list(range(7))),('R',-1,[0,11,10,9,8,7,6])]:
        hole = [torso[0][i] for i in indices]+[crotch]
        leg=[]
        for z,a,b,weights in [(1.02,.127,.138,{'Hips':.45,f'{side}UpLeg':.55}),
            (.91,.12,.13,{f'{side}UpLeg':1}),(.74,.097,.11,{f'{side}UpLeg':1}),
            (.68,.094,.101,{f'{side}UpLeg':.7,f'{side}Leg':.3}),
            (.635,.092,.095,{f'{side}UpLeg':.3,f'{side}Leg':.7}),
            (.57,.09,.09,{f'{side}Leg':1}),(.40,.087,.094,{f'{side}Leg':1}),
            (.25,.073,.072,{f'{side}Leg':1}),(.19,.07,.07,{f'{side}Leg':.3,f'{side}Foot':.7}),
            (.13,.083,.105,{f'{side}Foot':1}),(.035,.085,.145,{f'{side}Foot':1})]:
            r=[]
            for i in range(8):
                t=math.radians(-135+i*45)
                r.append(vertex((sign*(.145+a*math.cos(t)), b*math.sin(t)-(.05 if z<.15 else 0), z),weights,'boot' if z<.2 else 'cloth'))
            leg.append(r)
        bridge(hole,leg[0])
        for a,b in zip(leg,leg[1:]):bridge(a,b)
        f.append(tuple(leg[-1]))
    obj=mesh('Shared_Infantry_Base_TPose',v,f,colors,weights=w,smooth=True)
    for face in obj.data.polygons: face.use_smooth=True
    obj['shared_base']='Reusable base for Grunt, Cadet, Rifleman, Guardian, Commander'
    obj['production_rig']='Bind to the saved SZ_Infantry master; do not auto-rig each kit'
    return obj
