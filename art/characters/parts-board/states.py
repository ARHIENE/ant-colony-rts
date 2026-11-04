"""Nervous and stylized death expression pieces; static design variants."""
from pathlib import Path
HERE=Path(__file__).resolve().parent
exec(compile((HERE/'crying.py').read_text(encoding='utf-8').split('made=[]')[0],str(HERE/'states.py'),'exec'),globals())
records=[]
for index,kind in enumerate(['NERVOUS','DEAD']):
    r=bpy.data.objects.new('EXPR-0%d %s'%(index+7,kind),None);scene.collection.objects.link(r)
    for s in [-1,1]:
        x=s*.30
        if index==0:
            patch('NervousEye',x,1.36,.042,.066,ink,r)
            curve('NervousBrow',[(s*.19,1.54),(s*.27,1.52),(s*.36,1.52)],ink,r,.011)
            patch('NervousBlush',s*.61,1.15,.12,.05,pink,r)
        else:
            # Small crossed closed eyes make death distinct from the relaxed sleeping face.
            curve('DeathEyeA',[(x-.045,1.40),(x+.045,1.32)],ink,r,.012)
            curve('DeathEyeB',[(x-.045,1.32),(x+.045,1.40)],ink,r,.012)
    if index==0:
        curve('TenseMouth',[(-.055,1.22),(-.025,1.227),(0,1.218),(.025,1.227),(.055,1.22)],ink,r,.011)
        patch('SweatDrop',.65,1.51,.041,.085,water,r)
        patch('SweatGlint',.638,1.52,.010,.032,shine,r,.045)
    else:
        patch('LimpMouth',0,1.21,.037,.026,ink,r)
    parts=list(r.children)
    for o in parts:o.location.z-=.30
    r.location=((index+.5)*2.7,0,2.85);r['display_z_offset']=-.30;r['category']='EXPR'
    label(r.name,r.location.x,2.70)
    demo=bpy.data.objects.new('STATE-MIX-%02d'%(index+1),None);scene.collection.objects.link(demo)
    for original in bpy.data.objects['MIX-04'].children:
        if any(k in original.name for k in ['V4_Eye','V4_Blush','V4_Mouth','ConfidentBrow','HappyEye','OpenSmile','Tongue']):continue
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=demo
    for original in parts:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=demo;o.location.z+=.30
    demo.location=((index+1.5)*2.7,0,8.55)
    label(kind,demo.location.x,8.40)
    records.append({'id':r.name,'preview':demo.name,'display_z_offset':-.30})
path=HERE/'catalog.json';data=json.loads(path.read_text());data.extend(records);path.write_text(json.dumps(data,indent=2),encoding='utf-8')
scene.render.filepath=str(HERE/'parts-board.png')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'appearance-parts.blend'))
bpy.ops.render.render(write_still=True)
print('Nervous and death expressions saved')
