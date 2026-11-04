"""24 modular static residents. Run in the Blender file containing cast-v5."""
import bpy,sys,math,json,importlib
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE));import parts as p;importlib.reload(p)
f=p.f
template=bpy.data.objects.get('Toto_V5');assert template,'Open mascot-cast-v5.blend first'
old=bpy.context.scene
scene=bpy.data.scenes.new('Residents_24_Studio');bpy.context.window.scene=scene
scene.world=old.world.copy()
cream=f.material('ResidentCream',(.98,.88,.69));ink=f.material('ResidentInk',(.09,.047,.03));green=f.material('SageCloth',(.28,.39,.20));blue=f.material('BlueCloth',(.28,.39,.56));red=f.material('RustCloth',(.58,.27,.20));gold=f.material('Gold',(.75,.51,.22));wood=f.material('WarmLeather',(.33,.19,.10))
pal=[(.98,.88,.69),(.76,.48,.26),(.94,.88,.75),(.91,.56,.23),(.62,.70,.40),(.75,.62,.77),(.92,.63,.32),(.63,.43,.27),(.49,.39,.32),(.94,.62,.52),(.68,.78,.87),(.70,.47,.28),(.95,.79,.50),(.58,.40,.27),(.95,.83,.66),(.62,.60,.57),(.95,.63,.49),(.58,.73,.87),(.96,.69,.60),(.52,.52,.57),(.95,.65,.28),(.73,.59,.78),(.57,.39,.26),(.60,.65,.41)]
kinds=['long','round','short','point','short','long','upright','round','short','long','round','round','short','long','point','round','short','long','long','round','short','point','short','round']
props={7:'spear',8:'shield',9:'spear',11:'shield',13:'leaf',14:'basket',15:'watering',16:'book',17:'cloth',21:'leaf',23:'leaf'}
roots=[]
for idx in range(24):
    num=idx+1;r=bpy.data.objects.new('Resident_%02d'%num,None);scene.collection.objects.link(r)
    r['concept_number']=num;r['status']='Static model, unrigged';r['ear_type']=kinds[idx]
    fur=f.material('Fur_%02d'%num,pal[idx]);inner=f.material('Inner_%02d'%num,tuple(.65*c+.35 for c in pal[idx]))
    body=None
    for orig in template.children:
        if orig.name.startswith(('V4_FoldedEar','V4_InnerEar')):continue
        o=orig.copy();o.data=orig.data.copy();scene.collection.objects.link(o);o.parent=r
        if o.name.startswith('V4_Body'):body=o
        if o.data.materials and o.data.materials[0].name.startswith('Toto_Pastel'):o.data.materials[0]=fur
    assert body
    # Pale lower-face blaze mapped directly onto the mesh surface, with a scalloped forehead.
    vs=[];faces=[];N=64;R=10;cx=0;cz=1.35
    def project(x,z,offset=.010):
        ok,v,n,i=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)));assert ok,(num,x,z)
        return (x,v.y-offset,z)
    vs.append(project(cx,cz))
    for k in range(1,R+1):
        for j in range(N):
            a=j*math.tau/N
            x=.72*math.cos(a);z=.43*math.sin(a)
            if z>0:z*=1+.13*math.cos(a*7+idx*.5)
            vs.append(project(x*k/R,cz+z*k/R))
    faces=[(0,j+1,(j+1)%N+1) for j in range(N)]
    for k in range(R-1):
        for j in range(N):a=1+k*N+j;b=1+k*N+(j+1)%N;faces.append((a,a+N,b+N,b))
    f.mesh('FaceBlaze',vs,faces,cream,r,True)
    # Bring ink/blush just in front of the blaze, without protruding eyeballs.
    for o in r.children:
        if o.name.startswith(('V4_Eye','V4_Blush','V4_Mouth')):o.location.y-=.018
    p.ears(kinds[idx],fur,inner,r);p.tuft(fur,r)
    # Small overlapping cheek tufts soften the silhouette, keeping broad shared proportions.
    for s in [-1,1]:
        for j in range(3):
            z=1.18+j*.13
            p.lock('CheekTuft',[(s*.78,.02,z+.08,.09,.08),(s*.94,-.01,z,.10,.085),(s*.91,-.04,z-.08,.005,.005)],fur,r)
    cape=[green,red,blue][idx%3];p.cape(cape,r)
    if 7<=num<=12:
        for s in [-1,1]:
            pts=[project(s*.22,1.50,.03),project(s*.36,1.54 if num%2==0 else 1.52,.03)]
            f.tube('ConfidentBrow',pts,.017,ink,r)
    if num in (1,6,15,22):p.flower(-.64,-.30,1.83,red if num==1 else blue,gold,r)
    if num in (5,13,21,24):
        p.leaf(.0,2.05,green,r)
        o=p.leaf(0,2.05,green,r);o.scale.x=-1
    if num==16:
        for s in [-1,1]:
            pts=[project(s*.30+.18*math.cos(a*math.tau/48),1.37+.20*math.sin(a*math.tau/48),.035) for a in range(49)]
            f.tube('Glasses',pts,.012,wood,r)
        f.tube('GlassesBridge',[project(-.12,1.39,.035),project(.12,1.39,.035)],.012,wood,r)
    if num in props:p.prop(props[num],r,wood,green,cream,ink)
    # Human-readable labels outside the exportable character root.
    r.location=((idx%6-2.5)*2.55,0,(3-idx//6)*2.9)
    data=bpy.data.curves.new('Number','FONT');data.body='%02d'%num;data.align_x='CENTER';data.size=.17;data.extrude=0
    label=bpy.data.objects.new('Label_%02d'%num,data);scene.collection.objects.link(label);label.location=(r.location.x,-.2,r.location.z-.20);label.rotation_euler=(math.pi/2,0,0);data.materials.append(ink)
    roots.append(r)
for obj in old.objects:
    if obj.type=='LIGHT':
        o=obj.copy();o.data=obj.data.copy();scene.collection.objects.link(o);o.location=(obj.location.x,-5,8);o.data.size=12;o.data.energy=1000
data=bpy.data.cameras.new('ResidentsCamera');cam=bpy.data.objects.new('ResidentsCamera',data);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(0,-30,5.35);cam.rotation_euler=(math.pi/2,0,0);data.type='ORTHO';data.ortho_scale=16.2
scene.render.resolution_x=2100;scene.render.resolution_y=1550;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'overview.png')
scene.render.film_transparent=False
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
assert len(roots)==24
print('24 residents built',sum(len(r.children) for r in roots),'parts')
