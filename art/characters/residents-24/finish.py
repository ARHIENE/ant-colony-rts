import bpy,sys,math,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE));import parts as p
f=p.f;scene=bpy.context.scene
ink=bpy.data.materials['ResidentInk'];pink=f.material('SmilePink',(.87,.37,.30))
for num in [4,19,21,22]:
    r=bpy.data.objects['Resident_%02d'%num]
    body=next(o for o in r.children if o.name.startswith('V4_Body'))
    def point(x,z):
        ok,v,n,i=body.ray_cast(Vector((x,-2,z)),Vector((0,1,0)));assert ok
        return (x,v.y-.035,z)
    for o in list(r.children):
        if o.name.startswith('V4_Mouth'):bpy.data.objects.remove(o,do_unlink=True)
        elif o.name.startswith('V4_Eye') and num in [19,22]:
            center=sum(v.co.x for v in o.data.vertices)/len(o.data.vertices)
            if num==22 or center>0:
                bpy.data.objects.remove(o,do_unlink=True)
                pts=[point(center-.065+.13*j/20,1.35+.055*math.sin(math.pi*j/20)) for j in range(21)]
                f.tube('HappyEye',pts,.012,ink,r)
    o=f.face_patch('OpenSmile',body,0,1.25,.057,.071,ink,r);o.location.y=-.027
    o=f.face_patch('Tongue',body,0,1.22,.035,.024,pink,r);o.location.y=-.038
# No destructive edits to the earlier cast. New file contains all prior scenes too.
assert bpy.data.objects.get('Toto_V5')
roots=[bpy.data.objects['Resident_%02d'%n] for n in range(1,25)]
assert all(r.children for r in roots)
manifest=[]
for r in roots:
    meshes=[o for o in r.children if o.type=='MESH']
    assert all(len(o.data.vertices)>0 for o in meshes)
    manifest.append({'id':r['concept_number'],'root':r.name,'parts':len(meshes),'ear':r['ear_type'],'rigged':False})
(HERE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
scene.render.filepath=str(HERE/'overview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'residents-24.blend'))
bpy.ops.render.render(write_still=True)
print('Saved 24 static residents')
