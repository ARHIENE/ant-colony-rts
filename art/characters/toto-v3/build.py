"""Animal silhouette + expressive SD face. v1/v2 are preserved."""
from pathlib import Path
import bpy, math, sys
from mathutils import Vector
HERE=Path(__file__).resolve().parent
p=HERE.parent/'toto-v2/build.py'
ns={'__file__':str(p)}
exec(compile(p.read_text(encoding='utf-8'),str(p),'exec'),ns)
f=ns['f'];scene=ns['scene'];root=ns['root'];head=ns['head'];cream=ns['cream'];dark=ns['dark'];white=ns['white'];pink=ns['pink']
scene.name='Toto_v3_Animal_SD';root.name='Toto_v3_Model'
for o in list(root.children_recursive):
    if any(k in o.name for k in ['Ear_','Eye_','Glint_','EyeSmile_','Brow_','BlushMark_','OpenSmile','MouthInside','Muzzle','Chin']):
        bpy.data.objects.remove(o,do_unlink=True)
cream.node_tree.nodes.get(next(n.name for n in cream.node_tree.nodes if n.type=='BSDF_PRINCIPLED')).inputs['Base Color'].default_value=(.95,.85,.67,1)
iris=f.material('V3_DeepTeal',(.035,.29,.32));irislight=f.material('V3_Aqua',(.16,.66,.63));inner=f.material('V3_Peach',(.95,.58,.43));tuft=f.material('V3_CreamTuft',(.99,.94,.81))
def patch(name,x,z,rx,rz,mat,offset=0):
    o=f.face_patch(name,head,x,z,rx,rz,mat,root);o.location.y=-offset;return o
def stroke(name,xz,r=.015,mat=dark,offset=.026):
    points=[]
    for x,z in xz:
        ok,hit,n,index=head.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
        assert ok
        points.append((x,hit.y-offset,z))
    return f.tube(name,points,r,mat,root,8)
for sign,label in [(-1,'L'),(1,'R')]:
    sections=[(sign*.51,.01,2.14,.16,.12),(sign*.58,.01,2.33,.205,.14),(sign*.67,0,2.50,.21,.14),(sign*.79,-.025,2.59,.18,.12),(sign*.91,-.065,2.57,.13,.10),(sign*.97,-.10,2.49,.07,.06),(sign*.97,-.12,2.45,.02,.02)]
    f.bent_ear('V3_FoldedEar_'+label,sections,cream,root,20)
    o=f.oval('V3_EarPeach_'+label,(sign*.68,-.13,2.43),(.085,.025,.17),inner,root,20,10);o.rotation_euler[1]=sign*.45
    patch('V3_EyeOutline_'+label,sign*.36,1.62,.225,.265,dark)
    patch('V3_Iris_'+label,sign*.36,1.61,.19,.225,iris,.009)
    patch('V3_IrisGlow_'+label,sign*.36,1.49,.145,.085,irislight,.017)
    patch('V3_LargeHighlight_'+label,sign*.36-.058,1.72,.087,.092,white,.025)
    patch('V3_TinyHighlight_'+label,sign*.36+.085,1.54,.027,.033,white,.027)
    stroke('V3_UpperLid_'+label,[(sign*.36-.20,1.76),(sign*.36-.13,1.85),(sign*.36,1.89),(sign*.36+.13,1.85),(sign*.36+.20,1.76)],.022)
    stroke('V3_ExpressiveBrow_'+label,[(sign*.36-.075,1.99),(sign*.36,2.01),(sign*.36+.075,1.98)],.018)
patch('V3_SmileOutline',0,1.30,.083,.09,dark)
patch('V3_SmilePink',0,1.285,.052,.055,inner,.01)
# Three soft fur locks, animal fur rather than a human haircut.
for name,loc,scale,angle in [('Center',(0,-.40,2.17),(.17,.13,.27),-.18),('Left',(-.21,-.39,2.22),(.16,.12,.22),-.45),('Right',(.22,-.39,2.23),(.16,.12,.20),.50)]:
    o=f.oval('V3_FurLock_'+name,loc,scale,tuft,root,24,12);o.rotation_euler[1]=angle
# Small leaf clasp keeps an original woodland identity.
leaf=f.material('V3_LeafGreen',(.24,.45,.18))
o=f.oval('V3_LeafClasp',(.72,-.22,2.27),(.10,.035,.19),leaf,root,16,8);o.rotation_euler[1]=.65
scene.camera.location=(1,-7,2.7);scene.camera.rotation_euler=(Vector((0,0,1.30))-scene.camera.location).to_track_quat('-Z','Y').to_euler();scene.camera.data.ortho_scale=3.35
scene.render.filepath=str(HERE/'preview.png')
root['design']='original animal silhouette, folded ears, fur tuft, large teal SD eyes; static concept'
bpy.context.view_layer.update()
print('V3 ready',len(root.children_recursive))
