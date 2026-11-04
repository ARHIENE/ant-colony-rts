"""Two hand-authored crying expression parts and assembled previews."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,'E:/Git/ant/art/characters/toto-v1');import forms as f
scene=bpy.context.scene
assert scene.name.startswith('Appearance_Parts_Board')
body=next(o for o in bpy.data.objects['FACE-01 ROUND'].children if 'V4_Body' in o.name)
ink=bpy.data.materials['ResidentInk']
water=f.material('Tear_SoftBlue',(.39,.72,.91));shine=f.material('Tear_Highlight',(.94,.98,1));pink=f.material('Crying_MouthPink',(.81,.35,.31))
def point(x,z,offset=.03):
    ok,v,n,i=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)));assert ok
    return (x,v.y-offset,z)
def curve(name,pts,mat,r,thickness=.012):return f.tube(name,[point(x,z) for x,z in pts],thickness,mat,r,10)
def patch(name,x,z,rx,rz,mat,r,offset=.03):
    o=f.face_patch(name,body,x,z,rx,rz,mat,r);o.location.y=-offset;return o
def label(text,x,z):
    d=bpy.data.curves.new(text,'FONT');d.body=text;d.align_x='CENTER';d.size=.15
    o=bpy.data.objects.new(text,d);scene.collection.objects.link(o);o.location=(x,-.8,z);o.rotation_euler=(math.pi/2,0,0);d.materials.append(ink)
made=[]
for index,kind in enumerate(['TEARY','SOBBING']):
    r=bpy.data.objects.new('EXPR-0%d %s'%(index+5,kind),None);scene.collection.objects.link(r)
    r['category']='EXPR';r['display_z_offset']=-.30
    for s in [-1,1]:
        x=s*.30
        patch('Crying_Blush',s*.61,1.15,.14,.085,bpy.data.materials['Toto_Blush'],r)
        # Raised inner brow: sadness without an angry or adult-looking face.
        curve('SadBrow',[(s*.18,1.56),(s*.25,1.53),(s*.36,1.51)],ink,r,.012)
        if index==0:
            patch('WetEye',x,1.36,.058,.078,ink,r)
            patch('Waterline',x,1.31,.073,.030,water,r,.045)
            patch('EyeSpark',x-.015,1.39,.018,.022,shine,r,.052)
            patch('LittleTear',x,1.24,.047,.070,water,r,.035)
            patch('TearShine',x-.014,1.25,.013,.032,shine,r,.049)
        else:
            curve('SqueezedEye',[(x-s*.075,1.40),(x+s*.02,1.35),(x-s*.07,1.32)],ink,r,.015)
            # Tapered tear stream following cheek surface.
            rows=[(1.32,.045),(1.25,.050),(1.12,.045),(1.01,.060),(.97,.020)]
            vs=[]
            for z,w in rows:
                for q in range(9):vs.append(point(x-w+2*w*q/8,z,.045))
            fs=[]
            for j in range(len(rows)-1):
                for q in range(8):a=j*9+q;fs.append((a,a+9,a+10,a+1))
            f.mesh('TearStream',vs,fs,water,r,True)
            curve('TearStreakShine',[(x-.014,1.28),(x-.014,1.20),(x-.014,1.09)],shine,r,.009)
    if index==0:
        curve('WobblyPout',[(-.065,1.21),(-.035,1.225),(0,1.213),(.035,1.225),(.065,1.21)],ink,r,.010)
    else:
        patch('TinySobMouth',0,1.20,.055,.042,ink,r)
        patch('SobMouthInner',0,1.188,.033,.017,pink,r,.045)
    # Display the expressions by themselves on the existing expression row.
    expression=list(r.children)
    for o in expression:o.location.z-=.30
    r.location=((index+1.5)*2.7,0,5.70)
    label(r.name,r.location.x,5.55)
    # Show both on complete characters in the unused top-right cells.
    demo=bpy.data.objects.new('CRY-MIX-%02d'%(index+1),None);scene.collection.objects.link(demo)
    template=bpy.data.objects['MIX-01' if index==0 else 'MIX-04']
    for original in template.children:
        if any(k in original.name for k in ['V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue']):continue
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=demo
    for original in expression:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=demo;o.location.z+=.30
    demo.location=((index+1.5)*2.7,0,14.25)
    label(demo.name,demo.location.x,14.10)
    made.append({'id':r.name,'preview':demo.name,'display_z_offset':-.30})
path=HERE/'catalog.json';catalog=json.loads(path.read_text());catalog.extend(made);path.write_text(json.dumps(catalog,indent=2),encoding='utf-8')
scene.render.filepath=str(HERE/'parts-board.png')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'appearance-parts.blend'))
bpy.ops.render.render(write_still=True)
print('Two crying expressions and two assembled previews saved')
