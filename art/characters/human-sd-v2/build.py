"""Separate human SD study, reusing v1 construction without touching its scene."""
from pathlib import Path
import bpy, math
from mathutils import Vector

HERE = Path(__file__).resolve().parent
source = (HERE.parent/'human-sd-v1/build.py').read_text(encoding='utf-8')
source = source.replace("'Human_SD_Studio'", "'Human_SD_V2_Studio'").replace("'Human_SD_Model'", "'Human_SD_V2'")
source = source.replace("o.location.y=-offset", "o.location.y=-offset*.18")
source = source.replace("offset=.035", "offset=.014")
source = source.replace("# Short mantle,", "# Short mantle,")
# The first pass supplies body and projected facial meshes. Its hair and mantle are replaced below.
exec(compile(source, str(HERE/'build.py'), 'exec'), globals())
for o in list(root.children_recursive):
    if o.name.startswith(('HairCap','Fringe_','BobLock_','Ahoge','Capelet','CapeEdge','CapeBrooch')):
        bpy.data.objects.remove(o, do_unlink=True)

def smooth(o, levels=2):
    m=o.modifiers.new('Soft silhouette','SUBSURF');m.levels=levels;m.render_levels=levels
    return o

def lock(name, sections, material=hair):
    return smooth(f.bent_ear(name,sections,material,root,12))

# Wide, shallow locks overlap a continuous cap. Depth remains small at the fringe.
verts=[]; faces=[];S=64;R=24
for i in range(R+1):
    for j in range(S):
        a=math.tau*j/S;front=max(0,-math.sin(a));end=2.26-1.24*front**4
        t=.01+(end-.01)*i/R
        verts.append((1.02*math.sin(t)*math.cos(a),.15+.69*math.sin(t)*math.sin(a),2.24+.77*math.cos(t)))
for i in range(R):
    for j in range(S):
        a=i*S+j;b=i*S+(j+1)%S;faces.append((a,b,b+S,a+S))
smooth(f.mesh('V2_HairShell',verts,faces,hair,root,True),1)
lock('V2_FringeCenter',[(.12,-.19,2.96,.04,.035),(.15,-.45,2.83,.23,.08),(.11,-.61,2.61,.27,.08),(.02,-.64,2.40,.22,.07),(.00,-.63,2.31,.11,.04),(.10,-.61,2.30,.005,.006)])
lock('V2_FringeLeft',[(-.02,-.18,2.95,.02,.02),(-.28,-.45,2.80,.22,.08),(-.43,-.59,2.59,.24,.08),(-.53,-.62,2.37,.20,.07),(-.59,-.60,2.25,.10,.04),(-.51,-.59,2.22,.004,.005)])
lock('V2_FringeRight',[(.29,-.12,2.94,.02,.02),(.48,-.40,2.79,.21,.08),(.63,-.54,2.57,.23,.08),(.73,-.57,2.35,.20,.07),(.76,-.56,2.23,.10,.04),(.65,-.57,2.20,.004,.005)])
for s,label in [(-1,'L'),(1,'R')]:
    lock('V2_Temple'+label,[(s*.59,-.17,2.83,.04,.04),(s*.82,-.37,2.60,.20,.10),(s*.93,-.42,2.33,.19,.10),(s*.92,-.43,2.05,.17,.09),(s*.82,-.47,1.86,.13,.07),(s*.66,-.49,1.84,.004,.005)])
    for j in range(3):
        y=.02+j*.23
        lock('V2_Bob'+label+str(j),[(s*.76,y,2.61,.09,.08),(s*.99,y,2.34,.20,.14),(s*1.04,y,2.06,.20,.13),(s*1.01,y,1.80,.18,.10),(s*.91,y,1.65,.12,.08),(s*1.02,y,1.63,.004,.006)])
lock('V2_CrownCurl',[(.08,.05,2.93,.06,.045),(.19,.04,3.07,.075,.055),(.19,.03,3.20,.065,.04),(.04,.01,3.25,.04,.025),(-.06,0,3.20,.003,.004)])

# Short shoulder cape: broad front triangles and a curved back, rather than a long vest.
verts=[];faces=[];N=48
for row in range(4):
    for j in range(N+1):
        a=-math.pi/2+.12+j*(math.tau-.24)/N
        front=max(0,-math.sin(a));rx=[.22,.39,.58,.66][row];ry=[.23,.30,.37,.40][row]
        z=[1.47,1.40,1.25,1.10][row]+(.11*front if row==3 else 0)
        verts.append((rx*math.cos(a),ry*math.sin(a),z))
for i in range(3):
    for j in range(N):
        a=i*(N+1)+j;faces.append((a,a+1,a+N+2,a+N+1))
o=smooth(f.mesh('V2_CapeBack',verts,faces,cape,root,True))
m=o.modifiers.new('Hem thickness','SOLIDIFY');m.thickness=.018
for s in [-1,1]:
    # Broad triangular lapels, gently bowed toward the camera.
    pts=[(s*.02,-.31,1.40),(s*.30,-.32,1.47),(s*.59,-.39,1.25),(s*.30,-.43,1.12)]
    o=f.mesh('V2_CapeFront',pts,[(0,1,2,3)],cape,root,True)
    m=o.modifiers.new('Cloth','SOLIDIFY');m.thickness=.025
    m=o.modifiers.new('Soft hem','BEVEL');m.width=.035;m.segments=3
    f.tube('V2_Hem',[(s*.08,-.34,1.38),(s*.30,-.45,1.16),(s*.55,-.41,1.25)],.009,trim,root)
f.oval('V2_Brooch',(0,-.36,1.39),(.075,.025,.075),gold,root,24,12)
# Smaller, rounder blush; all face layers stay within millimetres of the cheek.
for o in root.children_recursive:
    if o.name.startswith('Blush_'):
        for v in o.data.vertices:
            center=-.66 if v.co.x<0 else .66
            v.co.x=center+(v.co.x-center)*1.15

# Soft pastel material values keep the facial plane legible at game scale.
for mat,col in [(skin,(1,.78,.64)),(hair,(.25,.38,.39)),(cloth,(.94,.85,.69)),(cape,(.57,.27,.19)),(blush,(1,.55,.48))]:
    mat.diffuse_color=(*col,1)
    p=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1)
    p.inputs['Roughness'].default_value=.95
cam.location=(0,-9,1.75);cam.rotation_euler=(Vector((0,0,1.65))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=3.75
scene.render.resolution_x=960;scene.render.resolution_y=960
scene.render.filepath=str(HERE/'front.png')
scene.world.color=(.8,.8,.8)
assert len(root.children_recursive)>40
print('V2 ready',len(root.children_recursive))
