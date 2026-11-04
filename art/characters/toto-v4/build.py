"""Toto reference study: small facial marks, broad cheeks, folded ears."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent/'toto-v1'))
source=(HERE.parent/'toto-v1/build.py').read_text(encoding='utf-8')
source=source.replace("'Toto_Character_Studio'","'Toto_V4_Studio'").replace("'Toto_Model'","'Toto_V4'")
exec(compile(source,str(HERE/'build.py'),'exec'),globals())
# Keep studio and accessories, rebuild the body and reference-specific ears.
for o in list(root.children_recursive):
    if o.name.startswith(('Toto_Body','Eye','Cheek','Mouth','Ear_','InnerEar','Hand_','Foot_')):
        bpy.data.objects.remove(o,do_unlink=True)
def soft(o,n=2):
    m=o.modifiers.new('Soft form','SUBSURF');m.levels=n;m.render_levels=n
    return o
sections=[(0,0,.09,.18,.16),(0,0,.12,.39,.28),(0,0,.23,.49,.36),(0,0,.40,.51,.39),(0,0,.60,.48,.38),(0,0,.73,.45,.36),(0,0,.80,.48,.40),(0,0,.87,.70,.47),(0,0,1.0,.91,.54),(0,0,1.16,.99,.58),(0,0,1.34,.98,.60),(0,0,1.54,.88,.58),(0,0,1.75,.73,.51),(0,0,1.94,.54,.41),(0,0,2.04,.32,.26),(0,0,2.07,.02,.02)]
body=soft(f.rings('V4_Body',sections,cream,root,48))
bpy.context.view_layer.objects.active=body;bpy.ops.object.modifier_apply(modifier=body.modifiers[0].name)
for s in [-1,1]:
    soft(f.oval('V4_Foot',(s*.27,-.13,.115),(.20,.25,.115),cream,root,24,12),1)
    hand=f.oval('V4_Paw',(s*.42,-.42,.66),(.16,.16,.20),cream,root,24,12);hand.rotation_euler[1]=s*.5
    ear=[(s*.30,.01,1.93,.10,.08),(s*.35,.01,2.13,.14,.105),(s*.44,.01,2.34,.19,.12),(s*.61,0,2.46,.24,.13),(s*.79,-.035,2.39,.23,.12),(s*.91,-.09,2.24,.16,.10),(s*.92,-.13,2.16,.045,.04)]
    if s<0:ear=[(x,y,z-.05*max(0,i-1),w,d) for i,(x,y,z,w,d) in enumerate(ear)]
    o=soft(f.bent_ear('V4_FoldedEar',ear,cream,root,20))
    bpy.context.view_layer.objects.active=o;bpy.ops.object.modifier_apply(modifier=o.modifiers[0].name)
    # Surface-projected pink markings follow the bend rather than floating as an oval.
    px=s*.43;pz=2.14 if s>0 else 2.06
    try:f.face_patch('V4_InnerEar',o,px,pz,.055,.12,inner,root)
    except AssertionError:pass
    f.face_patch('V4_Eye',body,s*.30,1.36,.052,.088,dark,root)
    f.face_patch('V4_Blush',body,s*.62,1.15,.135,.12,pink,root)
points=[]
for x,z in [(-.065,1.27),(-.04,1.25),(-.02,1.25),(0,1.267),(.02,1.25),(.04,1.25),(.065,1.27)]:
    ok,p,n,i=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)));assert ok;points.append((x,p.y-.012,z))
soft(f.tube('V4_Mouth',points,.010,dark,root,8),1)
# Strap lies on the narrow chest; bag hangs at the hip.
old=next(o for o in root.children if o.name.startswith('ShoulderStrap'));bpy.data.objects.remove(old,do_unlink=True)
f.tube('V4_Strap',[(-.45,-.17,.84),(-.43,-.38,.79),(-.23,-.44,.69),(0,-.43,.60),(.25,-.39,.52),(.50,-.22,.48),(.70,.01,.54),(.43,.39,.71),(0,.41,.85),(-.42,.20,.87),(-.45,-.17,.84)],.025,bag,root,10)
for mat,col in [(cream,(.98,.91,.77)),(pink,(.97,.57,.46)),(inner,(.95,.63,.51)),(bag,(.32,.19,.105)),(flap,(.45,.29,.16))]:
    mat.diffuse_color=(*col,1);p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1)
cam.location=(.45,-8,2.25);cam.rotation_euler=(Vector((0,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.05
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.filepath=str(HERE/'preview.png')
for o in root.children:
    if o.name.startswith(('Satchel','SatchelFlap','SatchelButton')):
        o.scale*=.82
        o.location=Vector((.605,.08,.58))+(o.location-Vector((.68,.08,.58)))*.82
        if not o.name.startswith('SatchelButton'):soft(o,1)
print('Toto v4 ready')
