"""Static expression library plus independent overlay pieces and manual examples."""
import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,'E:/Git/ant/art/characters/toto-v1');import forms as f
old=bpy.context.scene;scene=bpy.data.scenes.new('Expressions_Extended');bpy.context.window.scene=scene;scene.world=old.world.copy()
template=bpy.data.objects['MIX-04']
ink=bpy.data.materials['ResidentInk'];pink=f.material('ExprRose',(.88,.42,.39));blue=f.material('ExprWater',(.36,.70,.90));white=f.material('ExprWhite',(.95,.97,1))
body_source=next(o for o in template.children if 'V4_Body' in o.name)
body=body_source.copy();body.data=body_source.data.copy();scene.collection.objects.link(body);body.parent=None;body.location=(0,0,0);body.hide_render=True
bpy.context.view_layer.update()
def project(x,z,offset=.035):
    ok,v,n,i=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)));assert ok,(x,z)
    return (x,v.y-offset,z)
def line(name,pts,r,mat=ink,radius=.012):return f.tube(name,[project(x,z) for x,z in pts],radius,mat,r,10)
def patch(name,x,z,rx,rz,r,mat=ink,offset=.035):
    o=f.face_patch(name,body,x,z,rx,rz,mat,r);o.location.y=-offset;return o
def arc(name,x,z,w,h,r):return line(name,[(x-w+2*w*j/24,z+h*math.sin(math.pi*j/24)) for j in range(25)],r)
def empty(name):
    r=bpy.data.objects.new(name,None);scene.collection.objects.link(r);return r
def label(text,x,z):
    d=bpy.data.curves.new(text,'FONT');d.body=text;d.align_x='CENTER';d.size=.13
    o=bpy.data.objects.new(text,d);scene.collection.objects.link(o);o.location=(x,-.8,z);o.rotation_euler=(math.pi/2,0,0);d.materials.append(ink)
def display(r,idx):
    r.location=((idx%4-1.5)*2.65,0,(4-idx//4)*2.65)
    b=empty('DisplayBody_'+r.name);b.location=r.location
    for original in template.children:
        if any(k in original.name for k in ['V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue']):continue
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=b
    label(r.name,r.location.x,r.location.z-.16)
def expression(kind,r):
    for s in [-1,1]:
        x=s*.30
        if kind in ('SLEEP','PROUD','TASTY'):arc('ClosedEye',x,1.36,.065,.035 if kind!='SLEEP' else -.025,r)
        elif kind=='PAIN' and s<0:line('Wince',[(x-.05,1.40),(x+.035,1.36),(x-.05,1.32)],r)
        elif kind in ('SLEEPY','EMPTY'):line('FlatLid',[(x-.06,1.39),(x+.06,1.39)],r);patch('LowEye',x,1.355,.035,.028,r)
        elif kind=='SURPRISE':patch('RoundEye',x,1.36,.065,.065,r)
        else:patch('Eye',x,1.36,.048,.07,r)
        if kind in ('ANGRY','PAIN'):line('Brow',[(s*.18,1.48),(s*.37,1.54)],r)
        elif kind=='SAD':line('Brow',[(s*.18,1.55),(s*.37,1.50)],r)
        elif kind=='CURIOUS':line('Brow',[(x-.07,1.53+(.05 if s>0 else 0)),(x+.07,1.55+(.05 if s>0 else 0))],r)
    if kind in ('SURPRISE','SLEEPY','CURIOUS'):patch('OpenMouth',0,1.22,.040,.057,r)
    elif kind=='EMPTY':line('EmptyMouth',[(-.035,1.22),(.035,1.22)],r,radius=.009)
    elif kind=='SAD':arc('Frown',0,1.20,.055,.022,r)
    elif kind=='PAIN':line('WobblyMouth',[(-.06,1.22),(-.025,1.235),(0,1.21),(.03,1.23),(.06,1.22)],r)
    elif kind=='ANGRY':
        arc('Pout',0,1.22,.045,.017,r)
        for s in [-1,1]:patch('PuffedCheek',s*.61,1.15,.15,.095,r,pink)
    elif kind=='FLUSTERED':line('TightMouth',[(-.06,1.22),(.06,1.22)],r)
    elif kind=='TASTY':
        arc('Smile',0,1.24,.07,-.035,r);patch('Tongue',.035,1.20,.025,.035,r,pink,.05)
    else:arc('Smile',0,1.24,.055,-.022,r)
    if kind=='PROUD':
        for s in [-1,1]:arc('ProudBrow',s*.30,1.52,.065,.015,r)
    if kind=='FLUSTERED':
        for s in [-1,1]:patch('SoftCheek',s*.60,1.15,.13,.06,r,pink)
def overlay(kind,r):
    if kind=='SWEAT':
        patch('Sweat',.64,1.51,.040,.085,r,blue);patch('Shine',.63,1.52,.010,.03,r,white,.05)
    else:
        for s in [-1,1]:
            if kind=='TEARS':
                patch('Tear',s*.30,1.22,.045,.11,r,blue);patch('Shine',s*.30-.01,1.24,.010,.05,r,white,.05)
            else:
                patch('Blush',s*.60,1.15,.14,.075,r,pink)
                for j in range(3):line('Hatch',[(s*.60+(j-1)*.05-.01,1.12),(s*.60+(j-1)*.05+.01,1.17)],r,pink,.008)
kinds=['SURPRISE','ANGRY','PAIN','SLEEPY','SLEEP','SAD','PROUD','CURIOUS','FLUSTERED','TASTY','EMPTY']
records=[]
for i,kind in enumerate(kinds):
    r=empty('EXPR-'+kind);expression(kind,r);r['category']='expression';display(r,i);records.append({'id':r.name,'type':'expression'})
for j,kind in enumerate(['SWEAT','TEARS','BLUSH']):
    r=empty('OVERLAY-'+kind);overlay(kind,r);r['category']='overlay';display(r,11+j);records.append({'id':r.name,'type':'overlay'})
for j,(face,extra) in enumerate([('PAIN','SWEAT'),('PROUD','BLUSH'),('SURPRISE','TEARS')]):
    r=empty('MIX-'+face+'-'+extra);expression(face,r);overlay(extra,r);display(r,14+j)
body.hide_set(True)
for ob in old.objects:
    if ob.type=='LIGHT':
        o=ob.copy();o.data=ob.data.copy();scene.collection.objects.link(o);o.location=(0,-6,8);o.data.energy=2500;o.data.size=15;o.data.use_shadow=False
cam=bpy.data.objects.new('ExpressionsCamera',bpy.data.cameras.new('ExpressionsCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.location=(0,-30,6.3);cam.rotation_euler=(math.pi/2,0,0);cam.data.type='ORTHO';cam.data.ortho_scale=13.8
scene.render.resolution_x=1500;scene.render.resolution_y=1850;scene.render.resolution_percentage=100;scene.render.filepath=str(HERE/'expressions-extended.png');scene.render.image_settings.file_format='PNG'
(HERE/'expression-catalog.json').write_text(json.dumps(records,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'appearance-parts.blend'))
bpy.ops.render.render(write_still=True)
print('11 expressions, 3 overlays, 3 manual combinations')
