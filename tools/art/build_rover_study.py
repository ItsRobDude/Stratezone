"""Build the sprite-based Rover scale study via Blender MCP or background mode.

Requires our shared infantry source for the visible seated driver. No stock
mesh, humanoid rig, or new runtime vehicle skeleton is introduced.
"""
from pathlib import Path
import sys
import math
import json
import bpy
from mathutils import Vector,Matrix
sys.path.insert(0,str(Path(__file__).resolve().parent))
import rover_study_common as C
from rover_study_common import box,rod,mesh,cross_sections


def chassis():
    cross_sections('Belly',[(-2.08,.63,.42,.58),(-1.6,.84,.40,.52),(1.7,.84,.40,.52),(2.12,.69,.42,.58)],'dark')
    box('Cabin_floor',(0,.05,.49),(1.60,2.30,.12),'metal')
    cross_sections('Engine_hood',[(-2.15,.66,.83,1.13),(-1.83,.85,.93,1.27),(-.96,.85,1.04,1.39)],bevel=.035)
    cross_sections('Hood_center',[(-2.13,.36,1.12,1.16),(-1.8,.48,1.25,1.30),(-1.01,.43,1.38,1.42)],'upper')
    for i in range(5):box('Hood_vent_'+str(i),(0,-1.05-i*.075,1.418-i*.011),(.64,.025,.015),'dark')
    for s in (-1,1):
        rod('Hood_latch_'+str(s),(s*.60,-1.34,1.365),(s*.60,-1.46,1.343),.025,'metal',sides=6)
        for y in (-1.46,1.42):
            # Segmented fender follows the tire instead of intersecting it.
            pts=[(y-.74,.88),(y-.52,1.20),(y+.47,1.22),(y+.71,.94)]
            verts=[(s*x,yy,z+dz) for x in (.80,1.26) for yy,z in pts for dz in (0,.07)]
            faces=[]
            for j in range(3):
                a=2*j;faces += [(a,a+2,a+10,a+8),(a+1,a+9,a+11,a+3),
                                (a,a+1,a+3,a+2),(a+8,a+10,a+11,a+9)]
            faces += [(0,8,9,1),(6,7,15,14)]
            mesh(f'Fender_{s}_{y}',verts,faces,'metal')
        box('Sill_'+str(s),(s*.89,-.02,.60),(.16,1.51,.16),'dark',bevel=.02)
        rod('Step_'+str(s),(s*1.03,-.64,.50),(s*1.03,.61,.50),.065,'metal')
        # Open door, angled lower armor and a distinct equipment pod.
        cross_sections('Rear_quarter_'+str(s),[(.57,.78,.70,1.30),(1.72,.78,.73,1.26)],'paint') if s==1 else None
        box('Side_pod_'+str(s),(s*.91,.63,1.06),(.27,.44,.49),'paint')
        box('Pod_inset_'+str(s),(s*1.057,.62,1.07),(.016,.30,.28),'dark')
        box('Pod_lid_'+str(s),(s*1.076,.62,1.14),(.025,.24,.25),'upper')
        rod('Pod_handle_'+str(s),(s*1.10,.55,1.16),(s*1.10,.69,1.16),.017,'metal',sides=6)
        box('Front_pod_'+str(s),(s*.92,-.83,.97),(.27,.40,.45),'paint')
    box('Rear_deck',(0,1.46,1.30),(1.63,1.18,.14),'upper',bevel=.025)
    box('Tailgate',(0,2.03,1.05),(1.69,.16,.53),'paint',bevel=.025)
    box('Rear_bumper',(0,2.26,.62),(1.93,.22,.17),'dark',bevel=.035)
    box('Front_bumper',(0,-2.28,.73),(1.99,.19,.16),'metal',bevel=.025)
    box('Front_grille',(0,-2.17,.95),(.86,.045,.24),'dark')
    for i in range(6):box('Grille_slat_'+str(i),(-.34+i*.136,-2.199,.95),(.035,.035,.18),'metal')
    box('Winch',(0,-2.31,.61),(.49,.17,.17),'dark',bevel=.025)
    rod('Winch_roller',(-.22,-2.413,.64),(.22,-2.413,.64),.044,'metal')
    for s in (-1,1):
        rod('Bumper_upright_'+str(s),(s*.54,-2.31,.74),(s*.54,-2.26,1.21),.052,'metal')
        box('Headlight_housing_'+str(s),(s*.71,-2.125,1.03),(.40,.11,.22),'dark',bevel=.035)
        box('Headlight_'+str(s),(s*.70,-2.188,1.04),(.18,.015,.11),'team',mask=1)
        box('Marker_'+str(s),(s*.87,-2.17,1.04),(.045,.02,.075),'team',mask=1)
        box('Rear_lamp_case_'+str(s),(s*.72,2.14,.94),(.24,.10,.23),'dark',bevel=.015)
        for z in (.89,.98):box('Rear_lamp_'+str(s)+str(z),(s*.72,2.195,z),(.14,.015,.045),'red')
        rod('Axle_'+str(s),(-1.05,s*1.44,.565),(1.05,s*1.44,.565),.085,'dark')
        for x in (-.67,.67):rod('Shock_'+str(s)+str(x),(x,s*1.44,.51),(x*.86,s*1.1,.90),.045,'metal')


def cabin():
    for s in (-1,1):
        x=s*.85
        path=[(x,-1.06,1.29),(x,-.59,2.24),(x,.55,2.27),(x,1.11,1.40)]
        for i in range(3):rod(f'Cage_{s}_{i}',path[i],path[i+1],.061,'metal')
        rod('Bpillar_'+str(s),(x,.53,.79),(x,.55,2.27),.058,'metal')
        rod('Door_diagonal_'+str(s),(x,-.91,.83),(x,-.25,.63),.055,'metal')
        rod('Door_sill_'+str(s),(x,-.25,.63),(x,.51,.83),.055,'metal')
        rod('Rear_brace_'+str(s),(x,.55,2.27),(x,1.78,1.37),.048,'metal')
        box('Mirror_arm_'+str(s),(s*1.01,-.72,1.48),(.32,.055,.055),'metal')
        box('Mirror_'+str(s),(s*1.17,-.72,1.54),(.10,.12,.19),'dark',bevel=.025)
        box('Mirror_glass_'+str(s),(s*1.17,-.65,1.54),(.064,.008,.13),'lens')
    for y,z in ((-.59,2.24),(.55,2.27),(-1.03,1.39)):
        rod('Cage_cross_'+str(y),(-.85,y,z),(.85,y,z),.061,'metal')
    box('Sensor_bridge',(0,.25,2.255),(1.56,.60,.09),'paint',bevel=.018)
    for s in (-1,1):
        box('Seat_base_'+str(s),(s*.43,.20,.89),(.62,.64,.18),'seat',bevel=.045)
        back=box('Seat_back_'+str(s),(s*.43,.56,1.30),(.61,.15,.79),'dark',bevel=.04)
        box('Seat_pad_'+str(s),(s*.43,.46,1.32),(.48,.06,.53),'seat',bevel=.025)
        box('Headrest_'+str(s),(s*.43,.56,1.81),(.31,.14,.22),'seat',bevel=.025)
    box('Dashboard',(0,-.79,1.25),(1.50,.28,.27),'dark',bevel=.02)
    box('Instrument_panel',(-.43,-.61,1.33),(.45,.045,.19),'metal')
    box('Display',(-.43,-.582,1.35),(.18,.012,.09),'lens')
    for x in (-.71,-.16):rod('Dial_'+str(x),(x,-.577,1.34),(x,-.567,1.34),.046,'ivory',sides=8)
    rod('Steering_column',(-.43,-.78,1.11),(-.43,-.43,1.35),.035,'metal')
    # Steering ring, tilted toward the driver.
    center=Vector((-.43,-.46,1.39));q=Vector((0,.7,.7)).to_track_quat('Z','Y')
    points=[center+q@Vector((.20*math.cos(i*math.tau/12),.20*math.sin(i*math.tau/12),0)) for i in range(12)]
    for i in range(12):rod('Steering_rim_'+str(i),points[i],points[(i+1)%12],.018,'dark',sides=5)
    for i in (0,4,8):rod('Steering_spoke_'+str(i),center,points[i],.018,'metal',sides=5)
    rod('Gear_lever',(.07,-.29,.80),(.07,-.38,1.17),.019,'metal',sides=6)
    box('Gear_knob',(.07,-.38,1.18),(.065,.065,.075),'dark',bevel=.01)


def stowage():
    for i,x in enumerate((-.43,.43)):
        box('Cargo_crate_'+str(i),(x,1.62,1.62),(.73,.74,.51),'paint',bevel=.035)
        box('Cargo_lid_'+str(i),(x,1.62,1.90),(.78,.78,.085),'upper',bevel=.01)
        for dx in (-.22,.22):
            box(f'Cargo_strap_top_{i}_{dx}',(x+dx,1.62,1.948),(.055,.79,.016),'dark')
            box(f'Cargo_strap_rear_{i}_{dx}',(x+dx,2.002,1.64),(.055,.026,.50),'dark')
        box('Cargo_inset_'+str(i),(x,2.015,1.66),(.40,.025,.29),'dark')
        box('Cargo_rear_plate_'+str(i),(x,2.038,1.68),(.32,.021,.22),'upper')
        box('Cargo_handle_'+str(i),(x,2.065,1.70),(.20,.035,.045),'metal')
    for s in (-1,1):
        box('Rear_can_'+str(s),(s*.92,1.64,1.41),(.23,.40,.58),'fabric',bevel=.035)
        rod('Can_handle_'+str(s),(s*.92,1.56,1.72),(s*.92,1.73,1.72),.035,'dark',sides=6)
        rod('Radio_base_'+str(s),(s*.78,.85,1.38),(s*.78,.85,1.81),.035,'metal')
        rod('Radio_whip_'+str(s),(s*.78,.85,1.81),(s*.78,.85,3.07 if s==-1 else 2.84),.011,'dark',sides=5)


def radar():
    """Original compact surveillance panel, informed by FLIR Ranger references.

    The sealed radome, short pan pedestal, rear fins and cable replace the gun.
    All parts turn together; no deployable mast or gameplay mechanic is added.
    """
    p='Radar'
    rod('Radar_azimuth_base',(0,.23,2.30),(0,.23,2.40),.23,'dark',p,8)
    rod('Radar_pedestal',(0,.23,2.39),(0,.23,2.62),.125,'paint',p,8)
    box('Radar_drive_case',(0,.23,2.65),(.30,.29,.25),'metal',p)
    for s in (-1,1):
        box('Radar_yoke_'+str(s),(s*.19,.23,2.78),(.07,.20,.32),'dark',p)

    panel_parts=[]
    def panel(name,w,h,front,back,color):
        x,z=w/2,h/2;c=.045
        ring=[(-x+c,-z),(x-c,-z),(x,-z+c),(x,z-c),
              (x-c,z),(-x+c,z),(-x,z-c),(-x,-z+c)]
        vertices=[(xx,yy,2.98+zz) for yy in (front,back) for xx,zz in ring]
        faces=[tuple(reversed(range(8))),tuple(range(8,16))]
        faces += [(i,(i+1)%8,(i+1)%8+8,i+8) for i in range(8)]
        obj=mesh(name,vertices,faces,color,p);panel_parts.append(obj)
    panel('Radar_housing',1.16,.57,.065,.345,'paint')
    panel('Radar_radome_gasket',1.11,.53,.040,.066,'dark')
    panel('Radar_radome',1.055,.475,.025,.041,'969580')
    panel_parts.append(box('Radar_edge_reinforcement',(-.41,.018,2.98),(.07,.025,.46),'paint',p))
    for x in (-.54,.54):
        for z in (2.76,3.20):
            panel_parts.append(rod(f'Radar_face_fastener_{x}_{z}',(x,.04,z),(x,.019,z),.015,'metal',p,6))
    # The radome stays featureless: exposed grid detail would misrepresent it.
    for i in range(4):
        panel_parts.append(box('Radar_cooling_fin_'+str(i),(-.33+i*.22,.39,2.98),(.045,.14,.42),'metal',p))
    panel_parts.append(box('Radar_rear_spine',(0,.39,2.98),(.88,.055,.075),'dark',p))
    panel_parts.append(box('Radar_service_plate',(.40,.422,2.98),(.13,.018,.16),'ivory',p))
    tilt=Matrix.Translation((0,.23,2.98))@Matrix.Rotation(math.radians(-8),4,'X')@Matrix.Translation((0,-.23,-2.98))
    for obj in panel_parts:
        for v in obj.data.vertices:v.co=tilt@v.co
    cable=[(-.39,.39,2.80),(-.38,.47,2.67),(-.23,.46,2.59),(-.14,.34,2.57)]
    for i in range(3):rod('Radar_cable_'+str(i),cable[i],cable[i+1],.019,'dark',p,5)
    bpy.context.scene['sensor_revision']=1
    bpy.context.scene['sensor_reference']='https://defense.flir.com/defense-products/ranger-r20ss/'


def replace_gun_with_radar():
    """Revise the existing study without rebuilding its chassis or crew."""
    scene=bpy.data.scenes[C.SCENE]
    assert bpy.context.scene==scene
    if scene.get('sensor_revision',0)>=1:return
    old=[o for o in bpy.data.collections[C.PARTS].objects if o.get('rover_part')=='Turret']
    assert old, 'Expected the original roof gun assembly'
    for obj in old:bpy.data.objects.remove(obj,do_unlink=True)
    bridge=bpy.data.objects.get('Rover_Turret_bridge')
    if bridge:bridge.name='Rover_Sensor_bridge'
    radar()


def wheels():
    p='Wheel_FR';origin=Vector(C.WHEELS[p]);n=16
    # Five profile rings give tread, rounded sidewalls and inset rims.
    v=[tuple(origin+Vector((x,r*math.sin(i*math.tau/n),r*math.cos(i*math.tau/n))))
       for x,r in [(-.235,.37),(-.225,.47),(-.15,.55),(.15,.55),(.225,.47),(.235,.37)] for i in range(n)]
    f=[]
    for j in range(5):f += [(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for i in range(n)]
    mesh('Tire',v,f,'rubber',p,smooth=True)
    for j in range(12):
        a=j*math.tau/12
        for side in (-1,1):
            # Chevron tread prisms, eight triangles each.
            pts=[(-.02,-.075,0),(.19,-.015,0),(.19,.09,0),(-.02,-.075,.022),(.19,-.015,.022),(.19,.09,.022)]
            verts=[]
            for x,t,z in pts:
                x*=side;radius=.55+z
                verts.append(tuple(origin+Vector((x,radius*math.sin(a)+t*math.cos(a),radius*math.cos(a)-t*math.sin(a)))))
            mesh(f'Tread_{j}_{side}',verts,[(2,1,0),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],'tread',p)
    for side in (1,):
        x=origin.x+side*.235
        rings=[(x,.37,'rim'),(x+.02,.31,'rim'),(x+.021,.27,'dark'),(x+.04,.13,'dark'),(x+.065,.11,'metal')]
        verts=[(xx,origin.y+r*math.sin(i*math.tau/12),origin.z+r*math.cos(i*math.tau/12))
               for xx,r,_ in rings for i in range(12)]
        faces=[(j*12+i,j*12+(i+1)%12,(j+1)*12+(i+1)%12,(j+1)*12+i) for j in range(4) for i in range(12)]
        faces.append(tuple(range(48,60)))
        rim=mesh('Wheel_rim',verts,faces,'rim',p)
        for j,(_,_,color) in enumerate(rings):
            for i in range(12):rim.data.color_attributes['Tint'].data[j*12+i].color=C.rgba(color)
        for i in range(6):
            angle=i*math.tau/6
            yy,zz=origin.y+.203*math.sin(angle),origin.z+.203*math.cos(angle)
            mesh('Wheel_lug_'+str(i),[(x+.037,yy+dy,zz+dz) for dy,dz in [(-.018,-.018),(.018,-.018),(.018,.018),(-.018,.018)]],[(0,1,2,3)],'rim',p)
    sources=[o for o in bpy.data.collections[C.PARTS].objects if o.get('rover_part')==p]
    for part,point in C.WHEELS.items():
        if part==p:continue
        for source in sources:
            obj=source.copy();obj.data=source.data
            bpy.data.collections[C.PARTS].objects.link(obj)
            obj.name=source.name.replace('Rover_','Rover_'+part+'_',1)
            facing=Matrix.Rotation(math.pi,4,'Z') if point[0]<0 else Matrix.Identity(4)
            obj.matrix_world=Matrix.Translation(Vector(point))@facing@Matrix.Translation(-origin)
            obj['rover_part']=part


def driver():
    """Bake a seated pose of our existing base into chassis geometry."""
    prefixes=('Helmet_','Goggle_','Chin_strap_','Chin_guard','Nose','Uniform_collar')
    loaded=[]
    rig=bpy.data.objects.get('SZ_Infantry')
    if not rig:
        with bpy.data.libraries.load(str(C.ROOT/'art/units/rifleman/rifleman_study.blend')) as (src,dst):
            dst.objects=[n for n in src.objects if n in ('SZ_Infantry','Shared_Infantry_Base_TPose') or n.startswith(prefixes)]
        loaded=[o for o in dst.objects if o]
        for o in loaded:bpy.context.scene.collection.objects.link(o)
        bpy.context.view_layer.update()
        rig=bpy.data.objects['SZ_Infantry']
    offset=Vector((-.43,.22,-.09));transforms={}
    for bone in rig.data.bones:transforms[bone.name]=Matrix.Translation(offset)
    def limb(name,head,tail):
        b=rig.data.bones['mixamorig:'+name]
        a=rig.matrix_world@b.head_local;t=rig.matrix_world@b.tail_local
        q=(t-a).rotation_difference(Vector(tail)-Vector(head))
        transforms[b.name]=Matrix.Translation(Vector(head))@q.to_matrix().to_4x4()@Matrix.Translation(-a)
    for side,sign in [('Left',1),('Right',-1)]:
        thigh=rig.matrix_world@rig.data.bones['mixamorig:'+side+'UpLeg'].head_local+offset
        knee=thigh+Vector((0,-.505,.08));ankle=knee+Vector((0,-.10,-.535))
        limb(side+'UpLeg',thigh,knee);limb(side+'Leg',knee,ankle)
        b=rig.data.bones['mixamorig:'+side+'Foot'];a=rig.matrix_world@b.head_local
        transforms[b.name]=Matrix.Translation(ankle-a)
        transforms['mixamorig:'+side+'ToeBase']=transforms[b.name]
        shoulder=rig.matrix_world@rig.data.bones['mixamorig:'+side+'Arm'].head_local+offset
        elbow=shoulder+Vector((sign*.04,-.195,-.286));wrist=elbow+Vector((-sign*.07,-.287,.151))
        limb(side+'Arm',shoulder,elbow);limb(side+'ForeArm',elbow,wrist)
        limb(side+'Hand',wrist,wrist+Vector((0,-.30,.13)))
    sources=[bpy.data.objects['Shared_Infantry_Base_TPose']]
    sources += [o for o in bpy.data.objects if o.name.startswith(prefixes) and o.type=='MESH' and o.vertex_groups]
    for source in sources:
        verts=[]
        for v in source.data.vertices:
            point=source.matrix_world@v.co
            result=Vector((0,0,0))
            for g in v.groups:result+=transforms.get(source.vertex_groups[g.group].name,Matrix.Translation(offset))@point*g.weight
            verts.append(tuple(result))
        obj=mesh('Driver_'+source.name,verts,[tuple(p.vertices) for p in source.data.polygons],'fabric')
        for dst,src in zip(obj.data.color_attributes['Tint'].data,source.data.color_attributes['Tint'].data):dst.color=src.color
        for dst,src in zip(obj.data.polygons,source.data.polygons):dst.use_smooth=src.use_smooth
        obj['source']='Our shared infantry base; seated static snapshot, same scale'
        if source.name=='Shared_Infantry_Base_TPose':
            mod=obj.modifiers.new('Seated crew mesh reduction','DECIMATE');mod.ratio=.68
    for o in loaded:bpy.context.scene.collection.objects.unlink(o)


def export():
    root=bpy.data.objects.new('Rover',None);bpy.context.scene.collection.objects.link(root)
    parts=[]
    for part,origin in [('Chassis',(0,0,0)),('Radar',(0,.23,2.30)),*C.WHEELS.items()]:
        obj=C.merge_part(part,origin);obj.parent=root;parts.append(obj)
    counts={o.name:len(o.data.loop_triangles) for o in parts}
    total=sum(counts.values())
    if total>6000:
        for o in [root,*parts]:bpy.data.objects.remove(o,do_unlink=True)
        raise RuntimeError(f'Rover budget exceeded: {total}, {counts}')
    points=[o.matrix_world@v.co for o in parts for v in o.data.vertices]
    # Ensure matrices include the recently assigned wheel and radar origins.
    bpy.context.view_layer.update()
    points=[o.matrix_world@v.co for o in parts for v in o.data.vertices]
    low=[min(p[i] for p in points) for i in range(3)];high=[max(p[i] for p in points) for i in range(3)]
    assert low[2]>-.05 and high[0]-low[0]<3.5 and high[1]-low[1]<5.5, 'Unexpected Rover/crew transform'
    C.EXPORT.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT');root.select_set(True)
    for o in parts:o.select_set(True)
    bpy.ops.export_scene.gltf(filepath=str(C.EXPORT),export_format='GLB',use_selection=True,
        use_active_scene=True,export_animations=False,export_cameras=False,export_lights=False,export_extras=True)
    radar_obj=next(o for o in parts if o.name=='Radar')
    radar_top=max((radar_obj.matrix_world@v.co).z for v in radar_obj.data.vertices)
    for o in [root,*parts]:bpy.data.objects.remove(o,do_unlink=True)
    metrics=dict(triangles=total,part_triangles=counts,bounds_min=low,bounds_max=high,
                 dimensions=[high[i]-low[i] for i in range(3)],wheel_centers=C.WHEELS,
                 wheelbase_m=2.88,cage_top_m=2.331,radar_top_m=radar_top,sensor_revision=1,
                 driver_source='Shared infantry base, unscaled, static seated pose',
                 runtime_surfaces=3,runtime_note='Four identical wheels are one MultiMesh in review scene')
    C.SOURCE.mkdir(parents=True,exist_ok=True)
    (C.SOURCE/'study_metrics.json').write_text(json.dumps(metrics,indent=2)+'\n')
    print(json.dumps(metrics,indent=2))


def build():
    C.setup();chassis();cabin();stowage();radar();wheels();driver()
    for path in (C.ROOT/'game/assets/units/rover/directional').glob('*.png'):
        img=bpy.data.images.load(str(path),check_existing=True)
        if not img.packed_file:img.pack()
    export()
    bpy.ops.wm.save_as_mainfile(filepath=str(C.SOURCE/'rover_study.blend'),compress=True)


if __name__=='__main__':build()
