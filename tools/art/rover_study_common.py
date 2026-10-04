"""Editable vertex-colored Rover geometry; meters, Z up, front -Y."""
from pathlib import Path
import math
import bpy
import bmesh
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT/'art/units/rover'
EXPORT = ROOT/'game/assets/models/studies/rover/rover_study.glb'
SCENE = 'SZ_Rover_Study'
PARTS = 'Rover_Editable'
MAT = 'SZ_Rover_Study'
PALETTE = dict(paint='6A6149', upper='877A58', edge='9B906C', dark='252B27',
               rubber='282B26', tread='3C4036', rim='777454', metal='555F58',
               seat='494C38', fabric='777453', team='46C2CC', red='C57031',
               lens='244A4E', ivory='C9C3A0', skin='BB8962')
WHEELS = {'Wheel_FL':(-1.0,-1.46,.58),'Wheel_FR':(1.0,-1.46,.58),
          'Wheel_RL':(-1.0,1.42,.58),'Wheel_RR':(1.0,1.42,.58)}


def rgba(key, mask=0):
    h=PALETTE.get(key,key)
    rgb=[int(h[i:i+2],16)/255 for i in (0,2,4)]
    return tuple(v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4 for v in rgb)+(mask,)


def setup():
    if SCENE in bpy.data.scenes: raise RuntimeError('Rover scene already exists; edit instead of rebuilding')
    old=bpy.context.scene
    scene=bpy.data.scenes.new(SCENE)
    scene.render.engine=old.render.engine
    scene.view_settings.view_transform=old.view_settings.view_transform
    scene.world=old.world
    bpy.context.window.scene=scene
    scene.collection.children.link(bpy.data.collections.new(PARTS))
    m=bpy.data.materials.new(MAT)
    shader=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    attr=m.node_tree.nodes.new('ShaderNodeVertexColor');attr.layer_name='Tint'
    m.node_tree.links.new(attr.outputs['Color'],shader.inputs['Base Color'])
    m.node_tree.links.new(attr.outputs['Color'],shader.inputs['Emission Color'])
    m.node_tree.links.new(attr.outputs['Alpha'],shader.inputs['Emission Strength'])
    shader.inputs['Roughness'].default_value=.72
    shader.inputs['Metallic'].default_value=.16
    scene['status']='Rover scale/design study; proportions provisional, materials unfinished'
    return scene


def mesh(name, vertices, faces, color='paint', part='Chassis', smooth=False, bevel=0, mask=0):
    data=bpy.data.meshes.new('Rover_'+name);data.from_pydata(vertices,[],faces)
    bm=bmesh.new();bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(data);bm.free()
    obj=bpy.data.objects.new('Rover_'+name,data)
    bpy.data.collections[PARTS].objects.link(obj);obj['rover_part']=part
    data.materials.append(bpy.data.materials[MAT])
    tint=data.color_attributes.new(name='Tint',type='FLOAT_COLOR',domain='POINT')
    for c in tint.data:c.color=rgba(color,mask)
    for p in data.polygons:p.use_smooth=smooth and len(p.vertices)==4
    if bevel:
        mod=obj.modifiers.new('Machined edge','BEVEL');mod.width=bevel;mod.segments=1
        allowed=[i.identifier for i in mod.bl_rna.properties['limit_method'].enum_items]
        assert 'ANGLE' in allowed;mod.limit_method='ANGLE'
    return obj


def box(name, center, size, color='paint', part='Chassis', bevel=0, mask=0):
    x,y,z=[n/2 for n in size]
    points=[(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),(-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    return mesh(name,[tuple(Vector(v)+Vector(center)) for v in points],
                [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],
                color,part,bevel=bevel if min(size)>.20 else 0,mask=mask)


def rod(name,a,b,radius,color='dark',part='Chassis',sides=8):
    a,b=Vector(a),Vector(b);q=(b-a).to_track_quat('Z','Y')
    v=[tuple(a+q@Vector((radius*math.cos(i*math.tau/sides),radius*math.sin(i*math.tau/sides),z)))
       for z in (0,(b-a).length) for i in range(sides)]
    f=[tuple(reversed(range(sides))),tuple(range(sides,sides*2))]
    f += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    return mesh(name,v,f,color,part,smooth=True)


def cross_sections(name,sections,color='paint',part='Chassis',bevel=0):
    # Each section is y, half-width, lower-z, upper-z. Solid tapered hull.
    verts=[]
    for y,w,lo,hi in sections:verts.extend([(-w,y,lo),(w,y,lo),(w,y,hi),(-w,y,hi)])
    faces=[(3,2,1,0),tuple(range(len(verts)-4,len(verts)))]
    for j in range(len(sections)-1):
        faces += [(4*j+i,4*j+(i+1)%4,4*(j+1)+(i+1)%4,4*(j+1)+i) for i in range(4)]
    return mesh(name,verts,faces,color,part,bevel=bevel)


def merge_part(part,origin):
    bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
    verts,faces,colors,smooth=[],[],[],[]
    for obj in bpy.data.collections[PARTS].objects:
        if obj.get('rover_part')!=part:continue
        evaluated=obj.evaluated_get(dg);data=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=dg)
        offset=len(verts)
        verts += [tuple(obj.matrix_world@v.co-Vector(origin)) for v in data.vertices]
        colors += [tuple(c.color) for c in data.color_attributes['Tint'].data]
        for p in data.polygons:
            faces.append(tuple(offset+i for i in p.vertices));smooth.append(p.use_smooth)
        evaluated.to_mesh_clear()
    data=bpy.data.meshes.new('RoverExport_'+part);data.from_pydata(verts,[],faces)
    data.materials.append(bpy.data.materials[MAT])
    tint=data.color_attributes.new(name='Tint',type='FLOAT_COLOR',domain='POINT')
    for dst,src in zip(tint.data,colors):dst.color=src
    for p,s in zip(data.polygons,smooth):p.use_smooth=s
    uv=data.uv_layers.new(name='TeamMask')
    for loop in data.loops:uv.data[loop.index].uv=(colors[loop.vertex_index][3],0)
    data.calc_loop_triangles()
    obj=bpy.data.objects.new(part,data);bpy.context.scene.collection.objects.link(obj)
    obj.location=origin
    return obj
