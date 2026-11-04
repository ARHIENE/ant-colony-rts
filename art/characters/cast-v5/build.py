"""Six reference mascots, shared rounder head with independent editable meshes."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent/'toto-v1'))
import forms as f
source=(HERE.parent/'toto-v4/build.py').read_text(encoding='utf-8')
# Execute the existing builder with its real location, then retain its newly created scene.
ns={'__file__':str(HERE.parent/'toto-v4/build.py')}
exec(compile(source,ns['__file__'],'exec'),ns)
scene=ns['scene'];scene.name='Mascot_Cast_V5';base=ns['root'];base.name='Toto_V5'
def smooth(o):
    m=o.modifiers.new('Rounded','SUBSURF');m.levels=2;return o
def width(z):
    # Reduce widest cheek 10%, fill upper temples to remove the triangular taper.
    def ease(t):
        t=max(0,min(1,t));return t*t*(3-2*t)
    return 1-.10*ease((z-.75)/.35)+.14*ease((z-1.30)/.60)
for o in base.children_recursive:
    if o.type=='MESH' and o.name.startswith(('V4_Body','V4_Eye','V4_Blush','V4_Mouth')):
        for v in o.data.vertices:v.co.x*=width(v.co.z)
def tint(name,col):return f.material(name,col)
variants=[('Toto',(.98,.91,.77),'bunny'),('Popo',(.98,.59,.58),'round'),('Kongji',(.98,.82,.45),'brown'),('Lulu',(.57,.69,.94),'droop'),('Pico',(.73,.57,.91),'fold'),('Nyamnyam',(.95,.91,.63),'leaf')]
roots=[]
for index,(name,color,kind) in enumerate(variants):
    if index==0:r=base
    else:
        r=bpy.data.objects.new(name+'_V5',None);scene.collection.objects.link(r)
        for original in list(base.children):
            if original.name.startswith(('V4_FoldedEar','V4_InnerEar')):continue
            o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=r
    bodymat=tint(name+'_Pastel',color);earmat=tint(name+'_Ear',(.44,.27,.13) if kind=='brown' else color)
    for o in r.children:
        if o.type=='MESH' and o.data.materials and o.data.materials[0].name.startswith(('Toto_Cream','Toto_Pastel')):
            o.data.materials[0]=bodymat
    if kind in ('round','brown','fold'):
        for s in [-1,1]:
            points=[(s*.27,.04,1.97,.075,.07),(s*.30,.03,2.13,.09,.085),(s*.36,.02,2.27,.09,.08)]
            smooth(f.bent_ear(name+'_EarStem',points,earmat,r,16))
            tip=f.oval(name+'_EarTip',(s*.39,.02,2.29),(.20 if kind!='fold' else .24,.12,.18),earmat,r,24,16)
            tip.rotation_euler[1]=-s*.35
            if kind=='fold':
                tip.location.x=s*.43;tip.location.z=2.25
    if kind=='droop':
        for s in [-1,1]:
            smooth(f.bent_ear('Lulu_LongEar',[(s*.48,.02,1.94,.08,.08),(s*.72,-.01,1.78,.16,.13),(s*.87,-.02,1.50,.20,.13),(s*.94,-.05,1.20,.21,.13),(s*.93,-.08,.99,.18,.12),(s*.81,-.10,.91,.06,.06)],bodymat,r,20))
        smooth(f.bent_ear('Lulu_Tuft',[(0,.08,2.0,.07,.06),(.03,.08,2.15,.07,.05),(.10,.07,2.12,.005,.005)],bodymat,r,16))
    if kind=='leaf':
        green=tint('Nyam_Leaf',(.36,.52,.13));vein=tint('Nyam_Vein',(.56,.66,.24))
        for s in [-1,1]:
            f.tube('Nyam_Stem',[(s*.06,0,1.99),(s*.12,0,2.16),(s*.30,0,2.32)],.032,green,r)
            # Thin biconvex leaf, broad centre and pointed tip.
            smooth(f.bent_ear('Nyam_Leaf',[(s*.12,0,2.08,.015,.012),(s*.31,0,2.28,.20,.065),(s*.52,0,2.46,.19,.055),(s*.72,0,2.59,.005,.005)],green,r,16))
            f.tube('Nyam_LeafVein',[(s*.13,-.055,2.13),(s*.32,-.07,2.31),(s*.58,-.045,2.50)],.012,vein,r)
        for o in r.children:
            if o.name.startswith(('Satchel','SatchelFlap')):o.data.materials[0]=green
    r.location.x=(index-2.5)*2.35
    roots.append(r)
cam=scene.camera;cam.location=(0,-20,4.2);cam.rotation_euler=(Vector((0,0,1.25))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=14.6
scene.render.resolution_x=2100;scene.render.resolution_y=650;scene.render.filepath=str(HERE/'lineup.png')
for o in scene.objects:
    if o.type=='LIGHT':o.data.size=8;o.data.energy*=2;o.location.z+=3
assert len(roots)==6 and all(len(r.children)>10 for r in roots)
print('Six models ready',[(r.name,len(r.children)) for r in roots])
