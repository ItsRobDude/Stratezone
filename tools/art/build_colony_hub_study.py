"""Author the first Colony Hub look study in Blender, without replacing other scenes.

Run through Blender MCP with runpy.run_path, then call build() in the returned
namespace. Also runs headlessly with Blender --background --python this_file.
The editable study and its GLB are deliberately outside the live mission wiring.
"""
from pathlib import Path
import json
import math

import bpy
import bmesh
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "art/buildings/colony_hub"
PREVIEWS = ROOT / "artifacts/colony-hub-study"
EXPORT = ROOT / "game/assets/models/studies/colony_hub/colony_hub_study.glb"
SCENE_NAME = "SZ_ColonyHub_Study"
MATERIALS = {}
PALETTE = {
    "armor": "656347", "upper": "7B7956", "edge": "94977A",
    "dark": "252C29", "frame": "3E4840", "steel": "69776E",
    "light": "46C2CC", "glass": "278998", "ivory": "CFCA9C",
    "hazard": "C9A227", "ground": "52584C",
}


def linear_color(hex_value):
    srgb = [int(hex_value[i:i+2], 16) / 255 for i in (0, 2, 4)]
    return tuple(v / 12.92 if v <= .04045 else ((v + .055) / 1.055) ** 2.4
                 for v in srgb) + (1.0,)


def socket(sockets, identifier):
    return next(s for s in sockets if s.identifier == identifier)


def enum_set(owner, key, value):
    # Read live RNA instead of depending on enum spelling across Blender versions.
    available = [i.identifier for i in owner.bl_rna.properties[key].enum_items]
    if value not in available:
        raise ValueError(f"{key}: {value!r} not in {available}")
    setattr(owner, key, value)


def material(name, roughness, metallic, emission=0):
    m = bpy.data.materials.new("SZ_Hub_" + name)
    shader = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    color = m.node_tree.nodes.new("ShaderNodeVertexColor")
    color.layer_name = "Tint"
    m.node_tree.links.new(socket(color.outputs, "Color"), socket(shader.inputs, "Base Color"))
    socket(shader.inputs, "Roughness").default_value = roughness
    socket(shader.inputs, "Metallic").default_value = metallic
    if emission:
        # glTF emission is a material constant; vertex colors still drive albedo.
        socket(shader.inputs, "Emission Color").default_value = linear_color(
            PALETTE["glass" if name == "Glass" else "light"])
        socket(shader.inputs, "Emission Strength").default_value = emission
    MATERIALS[name] = m
    return m


def setup():
    if bpy.data.scenes.get(SCENE_NAME):
        raise RuntimeError("Study scene already exists; edit it or rename it before rebuilding.")
    old_scene = bpy.context.scene
    scene = bpy.data.scenes.new(SCENE_NAME)
    scene.render.engine = old_scene.render.engine
    scene.view_settings.view_transform = old_scene.view_settings.view_transform
    bpy.context.window.scene = scene
    for name in ("Hub_Editable", "Study_Stage", "Reference_Notes"):
        scene.collection.children.link(bpy.data.collections.new(name))
    material("Paint", .67, .25)
    material("Frame", .56, .55)
    material("Team", .34, .12, 1.5)
    material("Glass", .24, .2, .38)
    scene["status"] = "Look study v01 - not mission integrated or production approved"
    scene["source_reference"] = "game/assets/buildings/colony_hub/directional/"
    scene["scale_note"] = "Provisional meters; footprint migration is not part of this study"
    return scene


def mesh(name, vertices, faces, color="armor", surface="Paint", bevel=0,
         collection="Hub_Editable", smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(vertices, [], faces)
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(name, data)
    bpy.data.collections[collection].objects.link(obj)
    data.materials.append(MATERIALS[surface])
    tint = data.color_attributes.new(name="Tint", type="FLOAT_COLOR", domain="POINT")
    rgba = linear_color(PALETTE.get(color, color))
    for value in tint.data:
        value.color = rgba
    obj.color = rgba
    for p in data.polygons:
        p.use_smooth = smooth and len(p.vertices) == 4
    if bevel:
        mod = obj.modifiers.new("Small machined edge", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        enum_set(mod, "limit_method", "ANGLE")
    return obj


def box(name, center, size, color="armor", surface="Paint", bevel=.035,
        collection="Hub_Editable"):
    x, y, z = (v / 2 for v in size)
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(3,2,1,0),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
    obj = mesh(name, verts, faces, color, surface, bevel, collection)
    obj.location = center
    return obj


def outline(width, depth, cut):
    x, y = width / 2, depth / 2
    return [(-x+cut,-y),(x-cut,-y),(x,-y+cut),(x,y-cut),
            (x-cut,y),(-x+cut,y),(-x,y-cut),(-x,-y+cut)]


def shell(name, bottom, top, z0, z1, color="armor", surface="Paint", bevel=.03):
    low, high = outline(*bottom), outline(*top)
    verts = [(x,y,z0) for x,y in low] + [(x,y,z1) for x,y in high]
    faces = [tuple(reversed(range(8))), tuple(range(8,16))]
    faces += [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
    return mesh(name, verts, faces, color, surface, bevel)


def rod(name, start, end, radius, color="dark", surface="Frame", sides=10, radius_top=None):
    a, b = Vector(start), Vector(end)
    top = radius if radius_top is None else radius_top
    verts = [(r*math.cos(i*math.tau/sides),r*math.sin(i*math.tau/sides),z)
             for r,z in ((radius,0),(top,(b-a).length)) for i in range(sides)]
    faces = [tuple(reversed(range(sides))),tuple(range(sides,2*sides))]
    faces += [(i,(i+1)%sides,(i+1)%sides+sides,i+sides) for i in range(sides)]
    obj = mesh(name, verts, faces, color, surface, smooth=True)
    obj.location = a
    obj.rotation_euler = (b-a).to_track_quat("Z","Y").to_euler()
    return obj


def panel(name, a, b, z, height, thickness, color="armor", surface="Paint", bevel=.025):
    a, b = Vector(a), Vector(b)
    mid = (a+b)/2
    obj = box(name, (mid.x,mid.y,z), ((b-a).length,thickness,height), color,surface,bevel)
    obj.rotation_euler.z = math.atan2(b.y-a.y,b.x-a.x)
    return obj


def base():
    shell("Foundation_octagonal", (8.4,7.8,.82),(8.2,7.6,.8), .06,.38,"dark","Frame",.04)
    shell("Lower_armored_core", (7.75,7.15,.65),(7.05,6.45,.55), .38,2.95,"armor",bevel=.065)
    shell("Shoulder_shadow_band", (7.75,7.15,.66),(7.75,7.15,.66),2.75,2.91,"dark","Frame")
    shell("Shoulder_deck", (7.95,7.35,.68),(7.7,7.1,.64),2.91,3.23,"upper",bevel=.045)
    shell("Command_deck_plinth", (6.5,5.9,.62),(6.5,5.9,.62),3.2,3.56,"dark","Frame")
    shell("Command_lower_armor",(6.35,5.75,.61),(6.35,5.75,.61),3.5,3.79,"armor")
    shell("Continuous_window_gasket",(6.35,5.75,.65),(6.35,5.75,.65),3.76,4.67,"dark","Frame")
    # Individual inset panes preserve dark mullions on straight and chamfered faces.
    perimeter = outline(6.39,5.79,.66)
    for side in range(8):
        a, b = Vector(perimeter[side]),Vector(perimeter[(side+1)%8])
        count = 3 if side % 2 == 0 else 1
        for j in range(count):
            p = a.lerp(b,j/count) + (b-a).normalized()*.085
            q = a.lerp(b,(j+1)/count) - (b-a).normalized()*.085
            panel(f"Command_glass_{side}_{j}",p,q,4.2,.65,.042,"glass","Glass",.025)
    shell("Window_lower_lip",(6.52,5.92,.68),(6.52,5.92,.68),3.72,3.84,"frame","Frame")
    shell("Roof_overhang_dark",(6.73,6.13,.76),(6.73,6.13,.76),4.62,4.86,"dark","Frame",.045)
    shell("Roof_armored_cap",(6.6,6,.74),(6.28,5.68,.68),4.86,5.15,"upper",bevel=.055)
    shell("Roof_recess",(5.9,5.3,.62),(5.9,5.3,.62),5.12,5.17,"dark","Frame",.02)
    shell("Roof_center_panel",(5.72,5.12,.6),(5.72,5.12,.6),5.16,5.24,"armor",bevel=.025)
    # Four large splayed corner buttresses, with separate olive shoulders.
    for sx in (-1,1):
        for sy in (-1,1):
            x,y = sx*3.18,sy*2.85
            foot = box(f"Corner_{sx}_{sy}_foot",(x,y,.45),(1.45,1.45,.45),"dark","Frame",.12)
            p = shell(f"Corner_{sx}_{sy}_armor",(1.38,1.35,.18),(1.02,1.02,.14),.6,2.96,"armor",bevel=.06)
            p.location.x,p.location.y=x,y
            box(f"Corner_{sx}_{sy}_shoulder",(x,y,2.98),(1.26,1.25,.46),"upper",bevel=.07)
            box(f"Corner_{sx}_{sy}_inset",(x,y+sy*.69,1.21),(.7,.08,1.02),"dark","Frame",.06)
            box(f"Corner_{sx}_{sy}_cap",(x,y+sy*.75,1.4),(.43,.09,.65),"frame","Frame",.04)
            box(f"Corner_{sx}_{sy}_status",(x,y+sy*.805,1.5),(.26,.035,.095),"light","Team",.012)
            box(f"Corner_{sx}_{sy}_toe",(x,y+sy*.55,.55),(.73,.42,.28),"frame","Frame",.04)
    # Entrance proud of the solid base wall. Dark aperture and inset split doors.
    box("Entry_shadow",(0,-3.51,1.48),(2.08,.16,2.37),"dark","Frame",.12)
    for sx in (-1,1):
        box(f"Entry_jamb_{sx}",(sx*1.06,-3.69,1.46),(.28,.46,2.44),"upper",bevel=.07)
        box(f"Door_leaf_{sx}",(sx*.44,-3.64,1.31),(.86,.10,1.78),"frame","Frame",.035)
        box(f"Door_recess_{sx}",(sx*.44,-3.705,1.33),(.64,.04,1.33),"dark","Frame",.06)
        box(f"Door_guide_{sx}",(sx*1.065,-3.93,1.67),(.07,.025,.45),"light","Team",.007)
    box("Entry_header",(0,-3.72,2.74),(2.58,.59,.67),"upper",bevel=.085)
    box("Entry_lintel_light",(0,-4.023,2.41),(.9,.025,.065),"light","Team",.008)
    # Ramp is presentation-only walkable decoration, not a collision footprint.
    verts=[(-.94,-5.58,.08),(.94,-5.58,.08),(.94,-3.45,.08),(-.94,-3.45,.08),
           (-.94,-5.58,.16),(.94,-5.58,.16),(.94,-3.45,.52),(-.94,-3.45,.52)]
    mesh("Entry_ramp_walkable",verts,[(0,3,2,1),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)],"frame","Frame",.025)
    slope=math.atan2(.36,2.13)
    for j in range(9):
        y=-5.43+j*.215
        z=.16+(y+5.58)*.36/2.13+.025
        tread=box(f"Ramp_tread_{j}",(0,y,z),(1.63,.10,.035),"steel","Frame",.008)
        tread.rotation_euler.x=slope
    for sx in (-1,1):
        rail=box(f"Ramp_edge_{sx}",(sx*.96,-4.5,.37),(.11,2.21,.16),"armor",bevel=.025)
        rail.rotation_euler.x=slope


def details():
    # Broad wall service panels remain readable at gameplay zoom.
    for sx in (-1,1):
        for j,y in enumerate((-1.25,1.1)):
            box(f"Side_service_frame_{sx}_{j}",(sx*3.64,y,1.68),(.15,1.71,1.62),"dark","Frame",.09)
            box(f"Side_service_plate_{sx}_{j}",(sx*3.735,y,1.84),(.12,1.44,1.15),"armor",bevel=.08)
            box(f"Side_vent_well_{sx}_{j}",(sx*3.809,y,1.94),(.04,1.08,.51),"dark","Frame",.03)
            for k in range(4):
                box(f"Side_vent_slat_{sx}_{j}_{k}",(sx*3.84,y,1.76+k*.11),(.055,.96,.045),"steel","Frame",.005)
            box(f"Side_indicator_{sx}_{j}",(sx*3.83,y-.4,1.44),(.04,.26,.075),"light","Team",.006)
        box(f"Side_sill_{sx}",(sx*3.78,0,.54),(.34,4.3,.25),"dark","Frame",.045)
    for x in (-2.17,2.17):
        box(f"Front_cheek_{x}",(x,-3.48,2.42),(1.10,.31,.86),"upper",bevel=.065)
        box(f"Front_cheek_seam_{x}",(x,-3.65,2.39),(.085,.022,.43),"dark","Frame",.005)
        box(f"Front_lower_panel_{x}",(x,-3.67,1.25),(.8,.14,1.1),"dark","Frame",.07)
        box(f"Front_lower_plate_{x}",(x,-3.77,1.32),(.53,.08,.7),"armor",bevel=.04)
        box(f"Front_status_{x}",(x,-3.82,1.40),(.28,.025,.10),"light","Team",.008)
    box("Rear_service_surround",(0,3.47,1.59),(3.64,.18,2.1),"dark","Frame",.10)
    box("Rear_service_panel",(0,3.59,1.85),(3.18,.12,1.24),"armor",bevel=.06)
    for i in range(7):
        box(f"Rear_louver_{i}",(-1.18+i*.395,3.675,1.92),(.22,.07,.77),"frame","Frame",.02)
    for x in (-.7,0,.7):
        box(f"Rear_status_{x}",(x,3.68,.96),(.29,.04,.095),"light","Team",.008)
    # Corner conduits and capped power couplings.
    for sx in (-1,1):
        for sy in (-1,1):
            x,y=sx*2.97,sy*2.6
            rod(f"Deck_socket_{sx}_{sy}",(x,y,3.23),(x,y,3.35),.25,"frame",sides=12)
            rod(f"Deck_cyan_ring_{sx}_{sy}",(x,y,3.34),(x,y,3.395),.185,"light","Team",12)
            rod(f"Deck_post_{sx}_{sy}",(x,y,3.39),(x,y,3.88),.115,"dark",sides=8)
            rod(f"Deck_post_cap_{sx}_{sy}",(x,y,3.86),(x,y,3.96),.16,"frame",sides=8)
    # Roof rim clamps and a front ventilation box.
    for x in (-2.10,-.85,.85,2.1):
        box(f"Roof_front_clamp_{x}",(x,-2.88,4.9),(.24,.42,.27),"frame","Frame",.025)
        box(f"Roof_rear_clamp_{x}",(x,2.88,4.9),(.24,.42,.27),"frame","Frame",.025)
    box("Roof_vent_case",(0,-1.87,5.33),(1.65,.81,.27),"frame","Frame",.065)
    box("Roof_vent_well",(0,-1.87,5.48),(1.37,.61,.035),"dark","Frame",.015)
    for j in range(6):
        box(f"Roof_vent_blade_{j}",(-.54+j*.217,-1.87,5.515),(.10,.53,.045),"steel","Frame",.008)
    # Winged badge rebuilt as shallow metal inlays, never an external logo asset.
    y=-4.024
    for sx in (-1,1):
        for j in range(3):
            z=2.90-j*.13
            points=[(sx*.14,y,z-.025),(sx*(.54-j*.085),y,z+.085),
                    (sx*(.62-j*.085),y,z+.18),(sx*.20,y,z+.065)]
            mesh(f"Insignia_wing_{sx}_{j}",points,[(0,1,2,3)],"ivory")
    mesh("Insignia_shield",[(-.10,y,2.96),(.10,y,2.96),(.13,y,2.77),(0,y,2.64),(-.13,y,2.77)],[(0,1,2,3,4)],"ivory")
    # Discrete fasteners on the front header and cheeks.
    for x in (-1.10,1.10,-2.54,-1.8,1.8,2.54):
        z=2.91 if abs(x)<1.5 else 2.64
        fy=-4.025 if abs(x)<1.5 else -3.651
        rod(f"Front_fastener_{x}",(x,fy,z),(x,fy-.045,z),.055,"steel",sides=6)
    # Flat hazard inserts on the ramp curb, kept thick enough to read.
    for sx in (-1,1):
        for j in range(4):
            y=-5.25+j*.43
            z=.16+(y+5.58)*.36/2.13+.123
            mark=box(f"Ramp_hazard_{sx}_{j}",(sx*.96,y,z),(.115,.19,.015),"hazard",bevel=0)
            mark.rotation_euler.x=math.atan2(.36,2.13)


def communications():
    # Main mast on the rear-left roof, as in the directional source set.
    x,y=-1.83,1.35
    box("Mast_armored_socket",(x,y,5.4),(.77,.77,.41),"upper",bevel=.07)
    rod("Mast_ring",(x,y,5.59),(x,y,5.69),.26,"light","Team",12)
    rod("Mast_lower",(x,y,5.69),(x,y,6.35),.20,"frame",sides=8,radius_top=.16)
    rod("Mast_upper",(x,y,6.35),(x,y,7.53),.095,"dark",sides=8,radius_top=.045)
    rod("Mast_tip",(x,y,7.53),(x,y,7.60),.054,"steel",sides=8)
    for dx,dy,h in ((.25,0,6.76),(-.17,.13,6.54)):
        rod(f"Mast_aux_{dx}",(x+dx,y+dy,5.78),(x+dx,y+dy,h),.045,"dark",sides=6)
    # Dish pivot and supports; bowl opens toward the front, tilted 40 degrees.
    center=Vector((.69,.65,6.14))
    rotation=Matrix.Rotation(math.radians(40),4,"X")
    rod("Dish_pedestal",(.69,.65,5.23),(.69,.65,5.83),.31,"frame",sides=12,radius_top=.24)
    box("Dish_yoke",(.69,.65,5.79),(.91,.63,.35),"dark","Frame",.075)
    rings,segments=6,28
    vertices=[]
    for j in range(rings+1):
        r=.045+1.13*j/rings
        for k in range(segments):
            a=k*math.tau/segments
            v=Vector((r*math.cos(a),r*math.sin(a),.32*(r/1.175)**2))
            vertices.append(tuple(center+rotation.to_3x3()@v))
    faces=[]
    for j in range(rings):
        for k in range(segments):
            q=j*segments+k; n=j*segments+(k+1)%segments
            faces.append((q,n,n+segments,q+segments))
    faces.append(tuple(reversed(range(segments))))
    mesh("Dish_concave_reflector",vertices,faces,"upper","Paint",smooth=True)
    def world(v):
        return center+rotation.to_3x3()@Vector(v)
    # Faceted rim, radial ribs, and a three-arm feed support.
    for k in range(segments):
        a,b=k*math.tau/segments,(k+1)*math.tau/segments
        rod(f"Dish_rim_{k}",world((1.175*math.cos(a),1.175*math.sin(a),.32)),
            world((1.175*math.cos(b),1.175*math.sin(b),.32)),.047,"frame",sides=6)
    for k in range(6):
        a=k*math.tau/6
        rod(f"Dish_rib_{k}",world((.25*math.cos(a),.25*math.sin(a),.04)),
            world((1.12*math.cos(a),1.12*math.sin(a),.29)),.035,"frame",sides=6)
    for k in range(3):
        a=k*math.tau/3
        rod(f"Dish_feed_strut_{k}",world((.88*math.cos(a),.88*math.sin(a),.2)),world((0,0,.76)),.035,"dark",sides=6)
    rod("Dish_receiver",world((0,0,.66)),world((0,0,.87)),.12,"dark",sides=12)
    rod("Dish_receiver_cyan",world((0,0,.87)),world((0,0,.92)),.088,"light","Team",12)


def refine_silhouette():
    # Reference review: the command deck is roughly two thirds of the base width.
    bpy.context.view_layer.update()
    upper_prefixes=("Command_", "Continuous_window", "Window_", "Roof_", "Mast_", "Dish_")
    shrink=Matrix.Diagonal((.83,.83,1,1))
    for obj in list(bpy.data.collections["Hub_Editable"].objects):
        if obj.name.startswith(upper_prefixes):
            obj.matrix_world=shrink@obj.matrix_world
    for obj in list(bpy.data.collections["Hub_Editable"].objects):
        if obj.name.startswith("Corner_"):
            # Parse both signs instead of depending on object locations.
            parts=obj.name.split("_")
            sx,sy=int(parts[1]),int(parts[2])
            obj.location.x+=sx*.16
            obj.location.y+=sy*.14
    # Large sloped equipment housings and boot armor; these carry the lower silhouette.
    for sx in (-1,1):
        for j,y in enumerate((-1.25,1.1)):
            prefix=(f"Side_service_frame_{sx}_{j}",f"Side_service_plate_{sx}_{j}",
                    f"Side_vent_well_{sx}_{j}",f"Side_vent_slat_{sx}_{j}",f"Side_indicator_{sx}_{j}")
            pivot=Vector((sx*3.66,y,1.65))
            transform=Matrix.Translation(pivot)@Matrix.Rotation(math.radians(-sx*13),4,"Y")@Matrix.Translation(-pivot)
            for obj in list(bpy.data.collections["Hub_Editable"].objects):
                if obj.name.startswith(prefix): obj.matrix_world=transform@obj.matrix_world
            block=box(f"Side_boot_{sx}_{j}",(sx*3.77,y,.81),(.72,1.75,.55),"dark","Frame",.09)
            box(f"Side_boot_plate_{sx}_{j}",(sx*4.15,y,.81),(.09,1.26,.28),"frame","Frame",.035)
            box(f"Shoulder_panel_{sx}_{j}",(sx*3.13,y,3.245),(1.08,1.68,.07),"armor",bevel=.03)
    # Visible deck pipes beside the smaller upper story.
    for sx in (-1,1):
        x=sx*2.95
        rod(f"Shoulder_pipe_lower_{sx}",(x,.98,3.29),(x,.98,3.52),.095,"dark",sides=8)
        rod(f"Shoulder_pipe_run_{sx}",(x,.98,3.52),(x,1.94,3.52),.095,"dark",sides=8)
        rod(f"Shoulder_pipe_upper_{sx}",(x,1.94,3.52),(sx*2.53,1.94,3.71),.095,"dark",sides=8)
        for y in (1.16,1.76):
            box(f"Pipe_bracket_{sx}_{y}",(x,y,3.43),(.29,.105,.33),"frame","Frame",.025)
    # Roof access plates break up the broad, otherwise featureless painted surface.
    for x,y,w,d in ((-1.45,-.62,1.04,1.03),(1.70,-.65,.51,1.42)):
        box(f"Roof_access_gasket_{x}",(x,y,5.255),(w+.065,d+.065,.035),"dark","Frame",.03)
        box(f"Roof_access_plate_{x}",(x,y,5.283),(w,d,.036),"upper",bevel=.025)
        box(f"Roof_access_pull_{x}",(x,y-.22,5.32),(.28,.08,.06),"frame","Frame",.012)
    # Extra rim breaks and seam divisions on the lower deck.
    for sx in (-1,1):
        for y in (-2.1,0,2.1):
            box(f"Shoulder_seam_{sx}_{y}",(sx*3.52,y,3.247),(.81,.035,.018),"dark","Frame",0)


def stage():
    scene=bpy.context.scene
    ground=box("Study_ground",(0,0,-.085),(200,200,.1),"ground",bevel=0,collection="Study_Stage")
    world=bpy.data.worlds.new("SZ_Hub_Daylight")
    world.use_nodes=True
    bg=next(n for n in world.node_tree.nodes if n.type=="BACKGROUND")
    bg.inputs[0].default_value=(.32,.40,.50,1)
    bg.inputs[1].default_value=.6
    scene.world=world
    for name,kind,loc,energy,color,size in [
        ("Sun","SUN",(8,-12,16),2.4,(1,.93,.80),0),
        ("Sky_fill","AREA",(-9,-3,12),1700,(.74,.87,1),9),
        ("Back_bounce","AREA",(2,9,13),2100,(1,.93,.8),8),
    ]:
        data=bpy.data.lights.new("SZ_"+name,kind)
        data.energy=energy; data.color=color
        if kind=="AREA": data.size=size
        if kind=="SUN": data.angle=math.radians(12)
        obj=bpy.data.objects.new(data.name,data)
        bpy.data.collections["Study_Stage"].objects.link(obj)
        obj.location=loc
        obj.rotation_euler=(Vector((0,0,2))-obj.location).to_track_quat("-Z","Y").to_euler()
    data=bpy.data.cameras.new("SZ_Hub_45deg_Ortho")
    enum_set(data,"type","ORTHO")
    data.ortho_scale=16.8
    camera=bpy.data.objects.new(data.name,data)
    bpy.data.collections["Study_Stage"].objects.link(camera)
    scene.camera=camera
    camera_view(38)
    scene.render.resolution_x=1280
    scene.render.resolution_y=1000
    scene.render.resolution_percentage=100
    enum_set(scene.render.image_settings,"file_format","PNG")
    # Read the live viewport enum values before assigning.
    for screen in bpy.data.screens:
        for area in screen.areas:
            if area.type=="VIEW_3D":
                space=area.spaces.active
                enum_set(space.shading,"type","MATERIAL")
                space.shading.use_scene_world=True
                space.shading.use_scene_lights=True
                space.overlay.show_overlays=False
                r=space.region_3d
                r.view_rotation=camera.rotation_euler.to_quaternion()
                r.view_distance=20
                r.view_location=(0,0,2.6)
                enum_set(r,"view_perspective","CAMERA")
                r.view_camera_zoom=0
    scene.render.film_transparent=False
    return ground


def camera_view(yaw=38, target=(0,-.25,2.9), scale=16.8):
    cam=bpy.context.scene.camera
    yaw=math.radians(yaw)
    cam.location=Vector(target)+Vector((18*math.sin(yaw),-18*math.cos(yaw),18))
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat("-Z","Y").to_euler()
    cam.data.ortho_scale=scale


def enlarge_entry():
    """Fit the entrance to the 2.23 m equipped infantry, keeping the Hub body."""
    scene = bpy.data.scenes[SCENE_NAME]
    if scene.get('entry_revision', 0) >= 2:
        return

    def fit(name, center, size):
        obj = bpy.data.objects[name]
        for axis in range(3):
            low = min(v.co[axis] for v in obj.data.vertices)
            high = max(v.co[axis] for v in obj.data.vertices)
            for vertex in obj.data.vertices:
                vertex.co[axis] = (vertex.co[axis] - (low+high)/2) * size[axis]/(high-low)
        obj.location = center

    fit('Entry_shadow', (0,-3.51,1.885), (2.66,.16,2.79))
    for side in (-1,1):
        fit(f'Entry_jamb_{side}', (side*1.46,-3.69,1.87), (.30,.46,2.78))
        fit(f'Door_leaf_{side}', (side*.655,-3.64,1.87), (1.29,.10,2.70))
        fit(f'Door_recess_{side}', (side*.655,-3.705,1.88), (.99,.04,2.08))
        fit(f'Door_guide_{side}', (side*1.465,-3.93,2.24), (.07,.025,.60))
    fit('Entry_header', (0,-3.72,3.49), (3.34,.59,.54))
    fit('Entry_lintel_light', (0,-4.023,3.27), (1.35,.025,.05))
    for obj in bpy.data.collections['Hub_Editable'].objects:
        if obj.name.startswith(('Entry_ramp_', 'Ramp_tread_')):
            for vertex in obj.data.vertices: vertex.co.x *= 1.5
        elif obj.name.startswith(('Ramp_edge_', 'Ramp_hazard_')):
            obj.location.x *= 1.5
        elif obj.name.startswith('Insignia_'):
            for vertex in obj.data.vertices:
                vertex.co.z = 3.49 + (vertex.co.z - 2.74) * .78
                vertex.co.x *= 1.20
        elif obj.name in ('Front_fastener_-1.1', 'Front_fastener_1.1'):
            for vertex in obj.data.vertices:
                vertex.co.x *= 1.25
                vertex.co.z += .71
    scene['entry_revision'] = 2
    scene['door_clear_height_m'] = 2.70
    scene['door_clear_width_m'] = 2.62
    scene['entry_threshold_z_m'] = .52
    bpy.context.view_layer.update()
    header = bpy.data.objects['Entry_header']
    underside = min((header.matrix_world@v.co).z for v in header.data.vertices)
    assert abs(underside - .52 - 2.70) < .001


def raise_stories():
    """Keep the entrance/footprint; give the lower story its own headroom."""
    scene = bpy.data.scenes[SCENE_NAME]
    if scene.get('height_revision', 0) >= 1:
        return
    assert scene.get('entry_revision', 0) >= 2, 'Fit the personnel entrance first'
    bpy.context.view_layer.update()
    for obj in bpy.data.collections['Hub_Editable'].objects:
        name = obj.name
        transform_z = None
        if name == 'Lower_armored_core':
            transform_z = lambda z: .38 + (z-.38) * (3.85-.38)/(2.95-.38)
        elif name.startswith('Corner_') and name.endswith('_armor'):
            transform_z = lambda z: .6 + (z-.6) * (3.86-.6)/(2.96-.6)
        elif (name.startswith(('Shoulder_', 'Deck_', 'Pipe_bracket_', 'Front_cheek_'))
              or name.startswith('Corner_') and name.endswith('_shoulder')):
            transform_z = lambda z: z + .9
        elif name.startswith(('Command_', 'Continuous_window', 'Window_')):
            # Add 30 cm to the glazed upper story, between its sill and roof.
            transform_z = lambda z: z + .9 + .3 * max(0, min(1, (z-3.79)/(4.62-3.79)))
        elif name.startswith(('Roof_', 'Mast_', 'Dish_')):
            transform_z = lambda z: z + 1.2
        elif name.startswith(('Side_service_', 'Side_vent_', 'Side_indicator_')):
            transform_z = lambda z: z + .445
        elif name.startswith(('Rear_service_', 'Rear_louver_')):
            transform_z = lambda z: z + .4
        if transform_z:
            inverse = obj.matrix_world.inverted()
            for vertex in obj.data.vertices:
                world = obj.matrix_world @ vertex.co
                world.z = transform_z(world.z)
                vertex.co = inverse @ world
            obj.data.update()
    scene['height_revision'] = 1
    scene['lower_wall_top_m'] = 3.85
    scene['main_roof_top_m'] = 6.44
    scene['status'] = 'Height revision 1; entry revision 2; provisional look study'
    bpy.context.view_layer.update()
    roof = bpy.data.objects['Roof_center_panel']
    assert abs(max((roof.matrix_world @ v.co).z for v in roof.data.vertices)-6.44) < .001


def stats():
    bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get()
    triangles=0
    points=[]
    objects=list(bpy.data.collections["Hub_Editable"].objects)
    for obj in objects:
        evaluated=obj.evaluated_get(dg)
        data=evaluated.to_mesh()
        data.calc_loop_triangles()
        triangles+=len(data.loop_triangles)
        points.extend(obj.matrix_world@v.co for v in data.vertices)
        evaluated.to_mesh_clear()
    minimum=[min(p[i] for p in points) for i in range(3)]
    maximum=[max(p[i] for p in points) for i in range(3)]
    result={"blender":bpy.app.version_string,"triangles":triangles,"editable_objects":len(objects),
            "materials":4,"bounds_min":minimum,"bounds_max":maximum,
            "dimensions":[maximum[i]-minimum[i] for i in range(3)]}
    if bpy.context.scene.get('entry_revision'):
        result.update(door_clear_height_m=2.70, door_clear_width_m=2.62, entry_threshold_z_m=.52)
    if bpy.context.scene.get('height_revision'):
        result.update(height_revision=1, lower_wall_top_m=3.85, main_roof_top_m=6.44)
    return result


def save():
    SOURCE.mkdir(parents=True,exist_ok=True)
    PREVIEWS.mkdir(parents=True,exist_ok=True)
    result=stats()
    (SOURCE/"study_metrics.json").write_text(json.dumps(result,indent=2)+"\n")
    bpy.ops.wm.save_as_mainfile(filepath=str(SOURCE/"colony_hub_study.blend"))
    print(json.dumps(result,indent=2))


def render(filename="hub_front.png",yaw=38,scale=16.8):
    PREVIEWS.mkdir(parents=True,exist_ok=True)
    camera_view(yaw,scale=scale)
    bpy.context.scene.render.filepath=str(PREVIEWS/filename)
    bpy.ops.render.render(write_still=True)


def export_glb():
    """Bake an isolated export mesh with four surfaces; retain the editable source."""
    import io_scene_gltf2
    bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get()
    vertices,faces,colors,indices,smoothing,normals=[],[],[],[],[],[]
    mats=[bpy.data.materials["SZ_Hub_"+k] for k in ("Paint","Frame","Team","Glass")]
    for obj in bpy.data.collections["Hub_Editable"].objects:
        evaluated=obj.evaluated_get(dg)
        data=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=dg)
        offset=len(vertices)
        vertices.extend(tuple(obj.matrix_world@v.co) for v in data.vertices)
        attr=data.color_attributes["Tint"]
        colors.extend(tuple(c.color) for c in attr.data)
        for poly in data.polygons:
            faces.append(tuple(offset+i for i in poly.vertices))
            indices.append([m.name for m in mats].index(data.materials[poly.material_index].name))
            smoothing.append(poly.use_smooth)
        nmat=obj.matrix_world.to_3x3().inverted().transposed()
        normals.extend(tuple((nmat@n.vector).normalized()) for n in data.corner_normals)
        evaluated.to_mesh_clear()
    data=bpy.data.meshes.new("ColonyHub_Study_Export")
    data.from_pydata(vertices,[],faces)
    for mat in mats: data.materials.append(mat)
    attr=data.color_attributes.new(name="Tint",type="FLOAT_COLOR",domain="POINT")
    for dest,src in zip(attr.data,colors):dest.color=src
    for p,idx,smooth in zip(data.polygons,indices,smoothing):
        p.material_index=idx
        p.use_smooth=smooth
    data.normals_split_custom_set(normals)
    obj=bpy.data.objects.new("ColonyHub_Study",data)
    bpy.context.scene.collection.objects.link(obj)
    obj["study_only"]=True
    obj["front_axis_blender"]="-Y"
    obj["source"]="art/buildings/colony_hub/colony_hub_study.blend"
    selected=list(bpy.context.selected_objects)
    active=bpy.context.view_layer.objects.active
    try:
        for other in selected:other.select_set(False)
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        EXPORT.parent.mkdir(parents=True,exist_ok=True)
        formats=[item[0] for item in io_scene_gltf2.get_format_items(None,bpy.context)]
        if "GLB" not in formats:raise RuntimeError(f"GLB unsupported: {formats}")
        bpy.ops.export_scene.gltf(filepath=str(EXPORT),export_format="GLB",use_selection=True,use_active_scene=True,
            export_animations=False,export_cameras=False,export_lights=False,
            export_extras=True,export_yup=True)
    finally:
        bpy.data.objects.remove(obj,do_unlink=True)
        bpy.data.meshes.remove(data)
        for other in selected:other.select_set(True)
        bpy.context.view_layer.objects.active=active
    print(f"Exported {EXPORT}")


def pack_references():
    reference_dir=ROOT/"game/assets/buildings/colony_hub/directional"
    for path in sorted(reference_dir.glob("*.png")):
        image=bpy.data.images.load(str(path),check_existing=True)
        if not image.packed_file:image.pack()


def build():
    setup()
    base()
    details()
    communications()
    refine_silhouette()
    enlarge_entry()
    raise_stories()
    stage()
    pack_references()
    export_glb()
    save()


if __name__=="__main__":
    build()
