"""Sprite-faithful Rifleman gear and presentation, on the shared infantry body.

Run via Blender MCP/runpy. The canonical rig comes from our Mixamo upload;
gear tags use semantic names until mapped to that returned skeleton.
"""
from pathlib import Path
import sys
import math
import json
import bpy
from mathutils import Vector, Matrix

sys.path.insert(0, str(Path(__file__).resolve().parent))
import infantry_study_common as C
from infantry_study_common import box, mesh, rings, rod


def plate(name, outline, front, back, color='armor', bone='Spine2'):
    # Profile is XZ, allowing actual chest/armpit cutaways rather than a box vest.
    verts=[(x,y,z) for y in (front,back) for x,z in outline]
    n=len(outline)
    faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(name,verts,faces,color,bone,bevel=.008)


def equipment():
    # Two-tone fatigues and a dark plate carrier, as in all eight reference views.
    rings('Uniform_collar',[((0,0,1.76),.14,.12),((0,0,1.885),.09,.083)],'cloth_dark','Neck',10)
    plate('Carrier_front', [(-.185,1.31),(.185,1.31),(.245,1.46),(.215,1.59),
          (.16,1.68),(-.16,1.68),(-.215,1.59),(-.245,1.46)],-.204,-.135)
    plate('Carrier_back',[(-.20,1.32),(.20,1.32),(.235,1.60),(.15,1.70),(-.15,1.70),(-.235,1.60)],.135,.19)
    box('Upper_plate_inset',(0,-.217,1.565),(.30,.028,.14),'edge','Spine2',.012)
    box('Carrier_team_tab',(.137,-.237,1.54),(.027,.012,.047),'team','Spine2',.003,1)
    for s in (-1,1):
        box(f'Shoulder_webbing_{s}',(s*.173,-.105,1.715),(.072,.27,.054),'webbing','Spine2',.009)
        box(f'Webbing_buckle_{s}',(s*.176,-.222,1.65),(.058,.037,.067),'steel','Spine2',.006)
        box(f'Carrier_side_{s}',(s*.212,.02,1.39),(.09,.24,.19),'dark','Spine2',.01)
        for j in range(2):
            box(f'Belt_pouch_{s}_{j}',(s*(.18+j*.052),-.063+j*.10,1.205),(.073,.095,.12),'pouch','Hips',.008)
    for i,x in enumerate((-.125,0,.125)):
        box(f'Front_mag_pouch_{i}',(x,-.223,1.29),(.103,.10,.183),'pouch','Spine2',.009)
        box(f'Pouch_flap_{i}',(x,-.278,1.347),(.102,.016,.048),'cloth_light','Spine2',.003)
        box(f'Pouch_strap_{i}',(x,-.282,1.275),(.026,.012,.07),'webbing','Spine2',0)
    rings('Utility_belt',[((0,0,1.165),.227,.161),((0,0,1.205),.227,.161)],'dark','Hips',12)
    box('Belt_clasp',(0,-.169,1.18),(.066,.026,.047),'steel','Hips',.003)
    # Segmented rectangular pack; the radio is offset, not a Guardian power core.
    box('Pack_padding',(0,.237,1.485),(.405,.14,.51),'dark','Spine2',.025)
    box('Radio_pack',(0,.31,1.495),(.445,.19,.535),'pouch','Spine2',.018)
    for i,x in enumerate((-.145,0,.145)):
        box(f'Pack_channel_{i}',(x,.414,1.49),(.125,.035,.445),'webbing','Spine2',.007)
        for j,z in enumerate((1.36,1.51,1.66)):
            box(f'Pack_webbing_{i}_{j}',(x,.435,z),(.121,.014,.022),'cloth_light','Spine2',0)
    for s in (-1,1):
        box(f'Pack_side_pocket_{s}',(s*.249,.28,1.4),(.08,.12,.24),'pouch','Spine2',.009)
    box('Radio_unit',(-.175,.242,1.77),(.13,.14,.155),'webbing','Spine2',.009)
    box('Radio_indicator',(-.18,.161,1.782),(.032,.012,.035),'team','Spine2',.003,1)
    rod('Antenna_base',(-.181,.254,1.81),(-.181,.254,1.92),.022,'dark','Spine2')
    rod('Radio_antenna',(-.181,.254,1.90),(-.181,.254,2.16),.009,'dark','Spine2',6)
    for side,sign in [('L',1),('R',-1)]:
        # Curved shoulder caps cover the top/front/back, leaving the armpit open.
        verts=[]
        for x,r in ((.272,.12),(.305,.165),(.405,.157),(.455,.10)):
            for i in range(7):
                theta=math.radians(-15+i*35)
                verts.append((sign*x,-r*math.cos(theta),1.645+r*math.sin(theta)))
        faces=[]
        for j in range(3):
            faces += [(j*7+i,j*7+i+1,(j+1)*7+i+1,(j+1)*7+i) for i in range(6)]
        faces += [tuple(reversed(range(7))),tuple(range(21,28)),(0,7,14,21,27,20,13,6)]
        mesh('Pauldron_'+side,verts,faces,'armor','pauldron.'+side,smooth=True)
        box('Upper_arm_patch_'+side,(sign*.47,-.097,1.64),(.064,.015,.062),'webbing',side+'UpperArm',.003)
        box('Elbow_guard_'+side,(sign*.611,.035,1.722),(.117,.12,.041),'armor',side+'ForeArm',.014)
        rings('Glove_cuff_'+side,[((sign*.91,0,1.645),.068,.071),((sign*.951,0,1.645),.07,.072)],'dark',side+'Hand',8,'X')
        box('Glove_knuckle_'+side,(sign*1.015,-.008,1.69),(.12,.09,.036),'armor',side+'Hand',.008)
        box('Thumb_'+side,(sign*.998,-.058,1.615),(.066,.05,.054),'dark',side+'Hand',.01)
        box('Cargo_thigh_pouch_'+side,(sign*.259,-.007,.961),(.08,.16,.16),'pouch',side+'UpLeg',.007)
        box('Cargo_flap_'+side,(sign*.301,-.007,1.019),(.013,.157,.044),'cloth_light',side+'UpLeg',.003)
        box('Knee_backing_'+side,(sign*.145,-.085,.668),(.164,.07,.19),'dark',side+'Leg',.02)
        plate('Kneecap_'+side,[(sign*.145-.067,.60),(sign*.145+.067,.60),
              (sign*.145+.073,.704),(sign*.145+.038,.751),(sign*.145-.047,.744),(sign*.145-.074,.69)],-.14,-.10,'armor',side+'Leg')
        box('Knee_ridge_'+side,(sign*.145,-.15,.715),(.089,.018,.032),'edge',side+'Leg',.005)
        rings('Boot_sole_'+side,[((sign*.145,-.052,.018),.099,.167),((sign*.145,-.052,.058),.101,.17)],'dark',side+'Foot',8)
        rings('Boot_upper_'+side,[((sign*.145,-.055,.058),.097,.162),((sign*.145,-.043,.108),.093,.15),
                                ((sign*.145,-.015,.158),.075,.104)],'boot',side+'Foot',8)
        box('Boot_toe_'+side,(sign*.145,-.171,.079),(.12,.072,.054),'edge',side+'Toe',.015)
        for j in range(3):
            box('Boot_lace_'+side+str(j),(sign*.145,-.081+j*.023,.143+j*.008),(.085,.013,.012),'dark',side+'Foot',0)


def headgear():
    box('Helmet_nape',(0,.102,2.015),(.247,.077,.135),'armor','Head',.012)
    rings('Helmet_rim',[((0,0,2.06),.165,.151),((0,0,2.089),.168,.151)],'dark','Head',12)
    rings('Helmet_shell',[((0,0,2.083),.167,.148),((0,0,2.16),.154,.136),
          ((0,.005,2.207),.111,.104),((0,.008,2.225),.045,.048)],'webbing','Head',12)
    box('Helmet_front_mount',(0,-.146,2.107),(.059,.034,.042),'armor','Head',.006)
    for sign in (-1,1):
        box(f'Helmet_ear_{sign}',(sign*.143,.007,2.004),(.058,.104,.121),'armor','Head',.014)
        box(f'Helmet_rail_{sign}',(sign*.158,.016,2.103),(.029,.103,.037),'dark','Head',.005)
        rod(f'Chin_strap_{sign}',(sign*.135,-.03,2.017),(sign*.081,-.079,1.911),.013,'dark','Head',6)
    # One broad goggle shape with a central nose notch; exposed cheeks/chin.
    plate('Goggle_frame',[(-.13,1.994),(-.072,1.974),(0,1.992),(.072,1.974),(.13,1.994),(.134,2.053),(-.134,2.053)],-.129,-.095,'dark','Head')
    plate('Goggle_lens',[(-.119,2.002),(-.072,1.986),(0,2.006),(.072,1.986),(.119,2.002),(.122,2.042),(-.122,2.042)],-.137,-.13,'lens','Head')
    for sign in (-1,1):
        box(f'Goggle_glint_{sign}',(sign*.083,-.143,2.032),(.046,.006,.008),'team','Head',0,.45)
    mesh('Nose',[(0,-.135,2.008),(-.03,-.13,1.97),(.03,-.13,1.97),(0,-.165,1.968)],[(0,1,3),(0,3,2),(1,2,3),(0,2,1)],'skin','Head')
    box('Chin_guard',(0,-.086,1.907),(.105,.061,.039),'armor','Head',.009)


def rifle():
    """Bullpup in authoring coordinates; aligned to the animated hands later.

    Local +Y is stock, -Y muzzle; grip center at origin. No extra material.
    """
    before=set(bpy.data.objects)
    box('Rifle_receiver',(0,.06,.085),(.105,.37,.136),'rifle','weapon',.016)
    box('Rifle_stock',(0,.272,.113),(.104,.14,.19),'rifle','weapon',.013)
    box('Rifle_butt',(0,.349,.113),(.113,.035,.191),'dark','weapon',.007)
    box('Rifle_foreguard',(0,-.228,.094),(.097,.23,.12),'rifle','weapon',.012)
    box('Rifle_top_rail',(0,-.084,.176),(.055,.57,.025),'dark','weapon',.002)
    for i in range(8):
        box(f'Rifle_rail_tooth_{i}',(0,-.31+i*.065,.193),(.066,.018,.012),'steel','weapon',0)
    box('Rifle_magazine',(0,.153,-.057),(.066,.103,.171),'dark','weapon',.01)
    box('Rifle_pistol_grip',(0,0,-.067),(.061,.072,.139),'armor','weapon',.008)
    box('Rifle_trigger_guard',(0,-.061,-.034),(.027,.096,.018),'dark','weapon',.002)
    rod('Rifle_barrel',(0,-.335,.104),(0,-.488,.104),.022,'dark','weapon')
    rod('Rifle_muzzle',(0,-.471,.104),(0,-.533,.104),.032,'armor','weapon')
    rod('Rifle_bore',(0,-.534,.104),(0,-.536,.104),.018,'dark','weapon')
    rod('Optic_body',(0,.035,.231),(0,-.115,.231),.039,'webbing','weapon')
    rod('Optic_lens',(0,-.116,.231),(0,-.12,.231),.027,'lens','weapon',8,.35)
    for sign in (-1,1):
        box(f'Rifle_side_inset_{sign}',(sign*.054,-.16,.105),(.006,.208,.035),'dark','weapon',.002)
        box(f'Rifle_port_{sign}',(sign*.055,.093,.122),(.006,.071,.027),'dark','weapon',0)
    for obj in set(bpy.data.objects)-before:
        obj['weapon_local']=True


def pack_refs():
    for path in sorted((C.ROOT/'game/assets/units/rifleman/directional').glob('*.png')):
        img=bpy.data.images.load(str(path),check_existing=True)
        if not img.packed_file:img.pack()


def view(yaw=25,pitch=15,scale=2.9,target=(0,0,1.15)):
    cam=bpy.context.scene.camera
    a,p=math.radians(yaw),math.radians(pitch)
    cam.location=Vector(target)+Vector((8*math.sin(a)*math.cos(p),-8*math.cos(a)*math.cos(p),8*math.sin(p)))
    cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler()
    cam.data.ortho_scale=scale


def render(name,yaw=25,pitch=15,scale=2.9,target=(0,0,1.15)):
    path=C.SOURCE/'previews';path.mkdir(parents=True,exist_ok=True)
    view(yaw,pitch,scale,target)
    bpy.context.scene.render.filepath=str(path/name)
    bpy.ops.render.render(write_still=True)


def gear():
    equipment();headgear();rifle();pack_refs()
    print('Rifleman gear authored from front, side and rear sprite references.')
