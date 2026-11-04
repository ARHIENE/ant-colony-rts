"""Refine projected facial artwork and garment silhouette on the v2 scene."""
import bpy,sys,math
from mathutils import Vector
sys.path.insert(0,'E:/Git/ant/art/characters/toto-v1')
import forms as f
scene=bpy.context.scene
root=next(o for o in scene.objects if o.name.startswith('Human_SD_V2'))
head=next(o for o in root.children if o.name.startswith('Head'))
def mat(prefix):
    return next(o for o in root.children if o.type=='MESH' and any(m and m.name.startswith(prefix) for m in o.data.materials)).data.materials[0]
skin=head.data.materials[0]
hair=next(o for o in root.children if o.name.startswith('V2_Fringe')).data.materials[0]
for o in list(root.children):
    if o.name.startswith(('Eye','Iris','Pupil','Lash','Brow','Blush','Smile')):bpy.data.objects.remove(o,do_unlink=True)
bpy.context.view_layer.objects.active=head
m=head.modifiers.new('Smooth cheeks','SUBSURF');m.levels=2
bpy.ops.object.modifier_apply(modifier=m.name)

def pigment(name,color):
    m=f.material(name,color)
    nodes=m.node_tree.nodes;nodes.clear();out=nodes.new('ShaderNodeOutputMaterial');e=nodes.new('ShaderNodeEmission');e.inputs[0].default_value=(*color,1);e.inputs[1].default_value=.8;m.node_tree.links.new(e.outputs[0],out.inputs[0]);return m
ink=pigment('V2_FaceInk',(.065,.040,.035));white=pigment('V2_EyeCream',(1,.96,.87))
iris=pigment('V2_DeepTeal',(.065,.16,.17));glow=pigment('V2_SoftJade',(.23,.42,.38));pink=pigment('V2_Cheek',(.96,.49,.41));mouth=pigment('V2_Mouth',(.40,.15,.12));tongue=pigment('V2_SmilePink',(.93,.39,.29))
def project(x,z,offset):
    ok,p,n,i=head.ray_cast(Vector((x,-3,z)),Vector((0,1,0)))
    assert ok,(x,z)
    return (x,p.y-offset,z)
def patch(name,x,z,rx,rz,material,offset):
    vs=[project(x,z,offset)];N=64;R=10
    for r in range(1,R+1):
        for j in range(N):
            a=j*math.tau/N;vs.append(project(x+rx*r/R*math.cos(a),z+rz*r/R*math.sin(a),offset))
    faces=[(0,j+1,(j+1)%N+1) for j in range(N)]
    for r in range(R-1):
        for j in range(N):
            a=1+r*N+j;b=1+r*N+(j+1)%N;faces.append((a,a+N,b+N,b))
    return f.mesh(name,vs,faces,material,root,True)
for s,l in [(-1,'L'),(1,'R')]:
    x=s*.37
    patch('V2_White'+l,x,2.015,.225,.235,white,.008)
    patch('V2_Iris'+l,x,1.995,.163,.205,iris,.014)
    patch('V2_Glow'+l,x,1.885,.125,.080,glow,.020)
    patch('V2_Pupil'+l,x,2.035,.064,.100,ink,.026)
    patch('V2_Glint'+l,x+.045,2.12,.049,.060,white,.032)
    patch('V2_Spark'+l,x-.073,1.91,.020,.024,white,.032)
    # Continuous arched upper lid, no dark circle around the lower eye.
    points=[]
    for j in range(33):
        a=.08+(math.pi-.16)*j/32
        points.append(project(x+.229*math.cos(a),2.015+.24*math.sin(a),.022))
    f.tube('V2_UpperLid'+l,points,.019,ink,root,10)
    f.tube('V2_LashTip'+l,[project(x+s*.22,2.08,.02),project(x+s*.265,2.12,.02)],.016,ink,root)
    patch('V2_Blush'+l,s*.65,1.805,.13,.052,pink,.010)
    points=[project(x-.12+.24*j/20,2.35+.033*math.sin(math.pi*j/20),.013) for j in range(21)]
    f.tube('V2_Brow'+l,points,.009,hair,root,8)
# Smile has a shallow top edge and rounded bowl, distinct from the previous ring mouth.
outline=[(-.083,1.76),(-.04,1.765),(0,1.77),(.04,1.78),(.083,1.785),(.077,1.72),(.045,1.68),(0,1.665),(-.045,1.68),(-.077,1.72)]
center=(0,1.725);vs=[project(*center,.014)]+[project(x,z,.014) for x,z in outline]
o=f.mesh('V2_Smile',vs,[(0,1+j,1+(j+1)%len(outline)) for j in range(len(outline))],mouth,root,True)
patch('V2_Tongue',0,1.704,.050,.027,tongue,.020)
for o in root.children:
    if o.name.startswith(('Tunic','Sleeve_')):
        mod=o.modifiers.new('Soft cloth','SUBSURF');mod.levels=1
    if o.name.startswith('V2_CrownCurl'):
        for v in o.data.vertices:v.co.z=2.96+(v.co.z-2.96)*.65
scene.render.filepath='E:/Git/ant/art/characters/human-sd-v2/front.png'
print('Refined face and cloth')
