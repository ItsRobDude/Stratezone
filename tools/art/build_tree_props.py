"""Four original low-poly world props; deterministic Blender source and GLBs.

Meters, Z up, origin at ground level. Rebuild in an empty Blender session or
call build() through MCP. Other open scenes are preserved.
"""
from pathlib import Path
import json
import math
import random

import bpy
import bmesh
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'art/props/trees'
EXPORT = ROOT / 'game/assets/models/props/trees'
SCENE = 'SZ_TreeProps_Study'
MATERIAL = 'SZ_TreeProps'
IDS = ('pine_a', 'pine_b', 'cherry_a', 'cherry_b')
PALETTE = {
    'pine_bark': ('514435', '65513D', '796347'),
    'cherry_bark': ('51463F', '6A5B50', '847260'),
    'pine': ('233C2D', '2B4A35', '355941', '42664A'),
    'pine_tip': ('2F4C34', '3B5E3E', '4B7049'),
    'cherry': ('34522A', '436732', '567B3C', '6B8C47'),
    'fruit': ('681F22', '8A2C2C', 'A84132'),
}


def rgba(value):
    rgb = [int(value[i:i+2], 16)/255 for i in (0, 2, 4)]
    return tuple(c/12.92 if c <= .04045 else ((c+.055)/1.055)**2.4 for c in rgb)+(1,)


def enum_set(owner, name, value):
    allowed = [i.identifier for i in owner.bl_rna.properties[name].enum_items]
    assert value in allowed, (name, allowed)
    setattr(owner, name, value)


def setup():
    if SCENE in bpy.data.scenes:
        raise RuntimeError('Tree prop scene already exists; preserve it before rebuilding')
    scene = bpy.data.scenes.new(SCENE)
    scene.world = bpy.context.scene.world
    bpy.context.window.scene = scene
    mat = bpy.data.materials.new(MATERIAL)
    shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    colors = mat.node_tree.nodes.new('ShaderNodeVertexColor'); colors.layer_name = 'Tint'
    mat.node_tree.links.new(colors.outputs['Color'], shader.inputs['Base Color'])
    shader.inputs['Roughness'].default_value = .93
    shader.inputs['Metallic'].default_value = 0
    scene['status'] = 'Basic world props: two pines and two summer-foliage cherry trees'
    scene['units'] = 'meters; origins at ground; no gameplay collision'
    return scene


class Tree:
    def __init__(self, asset_id, seed):
        self.asset_id = asset_id
        self.rng = random.Random(seed)
        self.parts = []
        self.collection = bpy.data.collections.new('Tree_' + asset_id)
        bpy.context.scene.collection.children.link(self.collection)

    def mesh(self, name, vertices, faces, palette):
        data = bpy.data.meshes.new(self.asset_id + '_' + name)
        data.from_pydata(vertices, [], faces)
        bm = bmesh.new(); bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(data); bm.free(); data.update()
        data.materials.append(bpy.data.materials[MATERIAL])
        tint = data.color_attributes.new(name='Tint', type='FLOAT_COLOR', domain='CORNER')
        colors = PALETTE.get(palette, (palette,))
        for face in data.polygons:
            # Broad, restrained canopy light variation; real lighting still shades it.
            index = max(0, min(len(colors)-1, int((face.normal.z+.8)*.8 + self.rng.uniform(-.4, 1))))
            color = rgba(colors[index])
            for i in face.loop_indices: tint.data[i].color = color
        obj = bpy.data.objects.new(data.name, data)
        obj['tree_asset'] = self.asset_id
        self.collection.objects.link(obj); self.parts.append(obj)
        return obj

    def limb(self, name, points, radii, sides=6, palette='pine_bark'):
        points = [Vector(p) for p in points]
        vertices = []
        for j, (point, radius) in enumerate(zip(points, radii)):
            direction = points[min(j+1, len(points)-1)] - points[max(0, j-1)]
            q = direction.to_track_quat('Z', 'Y')
            for i in range(sides):
                vertex=point + q @ Vector((radius*math.cos(i*math.tau/sides),radius*math.sin(i*math.tau/sides),0))
                if j==0 and abs(point.z)<.0001:vertex.z=0
                vertices.append(tuple(vertex))
        faces = [tuple(reversed(range(sides))), tuple(range(len(vertices)-sides, len(vertices)))]
        for j in range(len(points)-1):
            faces.extend((j*sides+i, j*sides+(i+1)%sides,
                          (j+1)*sides+(i+1)%sides, (j+1)*sides+i) for i in range(sides))
        return self.mesh(name, vertices, faces, palette)

    def roots(self, radius, palette):
        for j in range(3):
            a = j*math.tau/3 + .4
            self.limb('root_'+str(j), [(0,0,.2), (radius*math.cos(a),radius*math.sin(a),.07)],
                      [.16,.055], 4, palette)

    def pine_whorl(self, name, center, radius, height, phase, palette):
        # Scalloped, uneven bough edges with a broken lower skirt, not perfect cones.
        n=14; cx,cy,z=center; vertices=[]
        noise=[self.rng.uniform(.87,1.10) for _ in range(n)]
        for layer in range(3):
            for i in range(n):
                angle=phase+i*math.tau/n
                if layer==0:
                    r=radius*noise[i]*(1 if i%2==0 else .72)
                    zz=z+self.rng.uniform(-.12,.12)*height
                elif layer==1:
                    r=radius*noise[i]*.67;zz=z+height*.38+self.rng.uniform(-.05,.05)
                else:
                    r=radius*.21;zz=z+height*.80+self.rng.uniform(-.025,.025)
                vertices.append((cx+r*math.cos(angle),cy+r*math.sin(angle),zz))
        vertices.append((cx+.045,cy-.035,z+height))
        faces=[tuple(reversed(range(n)))]
        for j in range(2):
            faces.extend((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for i in range(n))
        faces.extend((2*n+i,2*n+(i+1)%n,3*n) for i in range(n))
        return self.mesh(name,vertices,faces,palette)

    def foliage(self, name, center, radius):
        bm=bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1)
        bm.verts.ensure_lookup_table();bm.verts.index_update()
        vertices=[]
        for v in bm.verts:
            jitter=self.rng.uniform(.90,1.09)
            vertices.append(tuple(Vector(center)+Vector((v.co.x*radius[0],v.co.y*radius[1],v.co.z*radius[2]))*jitter))
        faces=[tuple(v.index for v in f.verts) for f in bm.faces]
        bm.free()
        return self.mesh(name,vertices,faces,'cherry')

    def fruit(self, center, name):
        r=.065
        points=[(r,0,0),(-r,0,0),(0,r,0),(0,-r,0),(0,0,r),(0,0,-r)]
        faces=[(0,2,4),(2,1,4),(1,3,4),(3,0,4),(2,0,5),(1,2,5),(3,1,5),(0,3,5)]
        self.mesh(name,[tuple(Vector(center)+Vector(p)) for p in points],faces,'fruit')


def pine(asset_id, slim=False):
    tree=Tree(asset_id, 223 if slim else 171)
    h=6.0 if slim else 7.0
    def axis(z): return (.34*(z/h)**1.5 if slim else .065*math.sin(z), .06*math.sin(z*.7), z)
    tree.limb('trunk',[axis(z) for z in (0,1.3,2.6,4.2,h)],
              [.23,.18,.13,.08,.018] if not slim else [.19,.15,.11,.065,.014],7)
    tree.roots(.43,'pine_bark')
    layers=[(1.35,1.68,2.25),(2.65,1.42,1.95),(3.8,1.10,1.65),(4.95,.73,1.4),(5.9,.42,1.1)]
    if slim:layers=[(1.9,1.12,1.7),(2.9,1.0,1.55),(3.9,.75,1.3),(4.8,.45,1.2)]
    for j,(z,r,span) in enumerate(layers):
        cx,cy,_=axis(z)
        tree.pine_whorl('boughs_'+str(j),(cx,cy,z),r,span,j*.63,'pine_tip' if j==len(layers)-1 else 'pine')
        if j<3:
            for k in range(3):
                angle=k*math.tau/3+j*.61
                tree.limb(f'branch_{j}_{k}',[axis(z+.12),(cx+r*.8*math.cos(angle),cy+r*.8*math.sin(angle),z-.06)],
                          [.068,.018],4)
    if slim:
        tree.limb('old_branch',[axis(1.35),(.65,.20,1.55),(.79,.25,1.49)],[.055,.029,.008],4)
    return tree


def cherry(asset_id, tall=False):
    tree=Tree(asset_id,415 if tall else 371)
    palette='cherry_bark'
    if tall:
        trunk=[(0,0,0),(.08,0,1.15),(.22,.04,2.25),(.48,.05,3.4)]
        tips=[(-1.05,-.2,4.0),(1.35,.05,4.05),(.2,-1.15,4.3),(.2,1.15,4.35),(.65,.12,5.0)]
        crowns=[(-1.15,-.16,4.05,1.08,.96,.91),(1.15,0,4.15,1.1,1.04,1.0),
                (.10,-.95,4.4,1.08,.92,.90),(.1,1.0,4.47,1.2,.95,.90),
                (.65,.16,5.08,1.15,1.10,.87),(-.50,.25,4.85,.87,.91,.78)]
    else:
        trunk=[(0,0,0),(-.04,.03,.75),(-.14,.01,1.4),(-.25,.02,2.25)]
        tips=[(-1.4,-.15,2.9),(1.45,.0,3.1),(-.12,-1.2,3.3),(.1,1.18,3.4),(-.25,.15,4.0)]
        crowns=[(-1.38,-.10,3.1,1.2,1.04,1.02),(1.35,.02,3.23,1.15,1.05,1.0),
                (-.18,-1.1,3.50,1.22,1.05,.91),(.02,1.12,3.55,1.26,1.0,.94),
                (-.52,.10,4.08,1.12,1.1,.88),(.66,.46,3.96,1.03,1.0,.82)]
    tree.limb('trunk',trunk,[.29,.24,.19,.09] if tall else [.29,.25,.20,.10],7,palette)
    tree.roots(.54,palette)
    for j,tip in enumerate(tips):
        start=Vector(trunk[2]);end=Vector(tip)
        midpoint=start.lerp(end,.55);midpoint.z-=.13
        tree.limb('fork_'+str(j),[start,midpoint,end],[.13,.09,.025],5,palette)
    for j,c in enumerate(crowns):tree.foliage('foliage_'+str(j),c[:3],c[3:])
    # A few small clusters distinguish a summer cherry without pink blossoms.
    for j in range(3):
        x,y,z,*_=crowns[j]
        tree.fruit((x-.13,y-.85,z-.2),'cherry_'+str(j)+'a')
        tree.fruit((x-.025,y-.84,z-.235),'cherry_'+str(j)+'b')
    # Two narrow bark marks on the exposed trunk suggest cherry lenticels.
    for j,z in enumerate((.65,1.02)):
        tree.mesh('bark_mark_'+str(j),[(-.18,-.25,z),(.12,-.24,z+.015),(.10,-.24,z+.035),(-.18,-.25,z+.02)],[(0,1,2,3)],'948272')
    return tree


def merge(tree):
    vertices,faces,colors=[],[],[]
    for obj in tree.parts:
        offset=len(vertices);data=obj.data
        vertices.extend(tuple(v.co) for v in data.vertices)
        faces.extend(tuple(offset+i for i in f.vertices) for f in data.polygons)
        colors.extend(tuple(c.color) for c in data.color_attributes['Tint'].data)
    data=bpy.data.meshes.new(tree.asset_id+'_export')
    data.from_pydata(vertices,[],faces);data.materials.append(bpy.data.materials[MATERIAL])
    tint=data.color_attributes.new(name='Tint',type='FLOAT_COLOR',domain='CORNER')
    for dst,src in zip(tint.data,colors):dst.color=src
    data.calc_loop_triangles()
    obj=bpy.data.objects.new(tree.asset_id,data);bpy.context.scene.collection.objects.link(obj)
    obj['asset_id']=tree.asset_id;obj['units']='meters';obj['prop_only']=True
    return obj


def export_tree(tree):
    import io_scene_gltf2
    obj=merge(tree);data=obj.data
    bounds_min=[min(v.co[i] for v in data.vertices) for i in range(3)]
    bounds_max=[max(v.co[i] for v in data.vertices) for i in range(3)]
    count=len(data.loop_triangles)
    assert 200 <= count <= 800, (tree.asset_id,count)
    assert abs(bounds_min[2]) < .01, (tree.asset_id,'origin must sit on ground',bounds_min)
    assert 'GLB' in [item[0] for item in io_scene_gltf2.get_format_items(None,bpy.context)]
    allowed=[i.identifier for i in bpy.ops.object.select_all.get_rna_type().properties['action'].enum_items]
    assert 'DESELECT' in allowed;bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True);bpy.context.view_layer.objects.active=obj
    EXPORT.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=str(EXPORT/(tree.asset_id+'.glb')),export_format='GLB',
        use_selection=True,use_active_scene=True,export_animations=False,export_cameras=False,
        export_lights=False,export_extras=True,export_yup=True)
    result=dict(id=tree.asset_id,triangles=count,bounds_min=bounds_min,bounds_max=bounds_max,
                dimensions=[bounds_max[i]-bounds_min[i] for i in range(3)],surfaces=1)
    bpy.data.objects.remove(obj,do_unlink=True);bpy.data.meshes.remove(data)
    return result


def stage(trees):
    scene=bpy.context.scene
    for i,tree in enumerate(trees):
        for obj in tree.parts:obj.location.x=(i-1.5)*5.4
    for name,kind,location,power in [('TreeSun','SUN',(8,-12,16),2.0),('TreeFill','AREA',(-7,-3,12),1500)]:
        data=bpy.data.lights.new(name,kind);data.energy=power
        if kind=='AREA':data.shape='DISK' if 'DISK' in [i.identifier for i in data.bl_rna.properties['shape'].enum_items] else data.shape;data.size=12
        obj=bpy.data.objects.new(name,data);scene.collection.objects.link(obj);obj.location=location
        obj.rotation_euler=(Vector((0,0,2))-obj.location).to_track_quat('-Z','Y').to_euler()
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=='VIEW_3D':
                space=area.spaces.active;enum_set(space.shading,'type','MATERIAL')
                space.shading.use_scene_world=False;space.shading.use_scene_lights=True
                space.overlay.show_overlays=False;r=space.region_3d
                enum_set(r,'view_perspective','ORTHO');r.view_location=(0,0,2.7);r.view_distance=22
                r.view_rotation=Vector((1,-15,15)).to_track_quat('Z','Y')


def build():
    setup()
    trees=[pine('pine_a'),pine('pine_b',True),cherry('cherry_a'),cherry('cherry_b',True)]
    results=[export_tree(t) for t in trees]
    stage(trees)
    SOURCE.mkdir(parents=True,exist_ok=True)
    (SOURCE/'tree_metrics.json').write_text(json.dumps(results,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/'tree_props.blend'),compress=True)
    print(json.dumps(results,indent=2))


if __name__=='__main__':build()
