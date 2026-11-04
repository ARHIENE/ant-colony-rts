"""Static design board of interchangeable pieces. No generation UI or randomizer."""
import bpy,sys,math,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
old=bpy.context.scene
scene=bpy.data.scenes.new('Appearance_Parts_Board');bpy.context.window.scene=scene;scene.world=old.world.copy()
ink=bpy.data.materials['ResidentInk'];base=bpy.data.objects['Resident_01']
catalog=[]
def pick(n,prefix):return [o for o in bpy.data.objects['Resident_%02d'%n].children if o.type=='MESH' and o.name.startswith(prefix)]
def label(text,x,z):
    d=bpy.data.curves.new(text,'FONT');d.body=text;d.align_x='CENTER';d.size=.15
    o=bpy.data.objects.new(text,d);scene.collection.objects.link(o);o.location=(x,-.8,z);o.rotation_euler=(math.pi/2,0,0);d.materials.append(ink)
def tile(code,row,col,objects,width=1,shift=0):
    r=bpy.data.objects.new(code,None);scene.collection.objects.link(r);r.location=((col-2.5)*2.7,0,(5-row)*2.85)
    for original in objects:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=r;o.name=code+'__'+original.name
        # Display copies retain a common origin in their root space; shift only for the board.
        for v in o.data.vertices:v.co.x*=width
        o.location.x*=width;o.location.z+=shift
        o['source_part']=original.name
    r['display_z_offset']=shift;r['category']=code.split('-')[0]
    label(code,r.location.x,r.location.z-.15)
    catalog.append({'id':code,'root':r.name,'display_z_offset':shift,'source_parts':[o.name for o in objects]})
    return r
body=pick(1,('V4_Body','V4_Paw','V4_Foot','Tail'))
for col,(code,w) in enumerate([('FACE-01 ROUND',1),('FACE-02 OVAL',.94),('FACE-03 CHEEK',1.045)]):tile(code,0,col,body,w)
ears=[(2,('EarRound','EarInner'),'ROUND'),(3,('EarFlop',),'SHORT'),(1,('EarFlop',),'LONG'),(4,('EarPoint','EarInner'),'POINT'),(7,('EarPoint','EarInner'),'UPRIGHT')]
for col,(n,prefix,name) in enumerate(ears):tile('EAR-%02d %s'%(col+1,name),1,col,pick(n,prefix),shift=-.65)
# Existing folded rabbit ears form the sixth interchangeable option.
fold=[o for o in bpy.data.objects['Toto_V5'].children if o.name.startswith(('V4_FoldedEar','V4_InnerEar'))]
tile('EAR-06 FOLDED',1,5,fold,shift=-.65)
tile('TOP-01 TUFT',2,0,pick(1,('CrownTuft',)),shift=-.85)
tile('TOP-02 LEAVES',2,1,pick(5,('Leaf',)),shift=-.85)
tile('TOP-03 FLOWER',2,2,pick(1,('Petal','FlowerCenter')),shift=-.6)
tile('TOP-04 SIDE FLUFF',2,3,pick(2,('CheekTuft',)),shift=-.25)
for col,(n,name) in enumerate([(1,'CALM'),(8,'BRAVE'),(22,'HAPPY'),(19,'WINK')]):
    tile('EXPR-%02d %s'%(col+1,name),3,col,pick(n,('V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue')),shift=-.30)
for col,n in enumerate([2,4,9]):tile('MARK-%02d BLAZE'%(col+1),4,col,pick(n,('FaceBlaze',)),shift=-.20)
# Demonstration assemblies are deliberate choices, not procedurally generated characters.
for col,(ear,top,face,pattern,color) in enumerate([(2,1,1,2,2),(1,5,8,4,6),(4,1,22,9,4),(3,5,19,2,18),(7,1,8,9,7),(2,1,22,4,22)]):
    ear_parts=pick(ear,('EarRound','EarInner','EarPoint','EarFlop'))
    objects=body+ear_parts+pick(top,('Leaf',) if top==5 else ('CrownTuft',))+pick(face,('V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue'))+pick(pattern,('FaceBlaze',))
    r=tile('MIX-%02d'%(col+1),5,col,objects)
    fur=bpy.data.materials['Fur_%02d'%color]
    for o in r.children:
        if o.type=='MESH':
            for i,m in enumerate(o.data.materials):
                if m.name.startswith('Fur_'):o.data.materials[i]=fur
    r['combination']=json.dumps({'ears_source':ear,'top_source':top,'expression_source':face,'mark_source':pattern,'color_source':color})
for ob in old.objects:
    if ob.type=='LIGHT':
        o=ob.copy();o.data=ob.data.copy();scene.collection.objects.link(o);o.location=(0,-8,10);o.data.size=20;o.data.energy=3500;o.data.use_shadow=False
cam=bpy.data.objects.new('PartsCamera',bpy.data.cameras.new('PartsCamera'));scene.collection.objects.link(cam);scene.camera=cam;cam.data.type='ORTHO';cam.data.ortho_scale=17
cam.location=(0,-30,8.2);cam.rotation_euler=(math.pi/2,0,0)
scene.render.resolution_x=1800;scene.render.resolution_y=1800;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'parts-board.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA'
# Remove the mistakenly introduced live generator panel; its draft files remain preserved.
for name in ['RESIDENT_PT_panel','RESIDENT_OT_generate','RESIDENT_OT_save','RESIDENT_OT_load']:
    cls=getattr(bpy.types,name,None)
    if cls:bpy.utils.unregister_class(cls)
(HERE/'catalog.json').write_text(json.dumps(catalog,indent=2),encoding='utf-8')
assert len(catalog)==26
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'appearance-parts.blend'))
bpy.ops.render.render(write_still=True)
print('Static board saved: 20 part options and 6 hand-composed examples')
