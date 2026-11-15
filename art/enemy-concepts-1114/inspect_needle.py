import bpy, json
from pathlib import Path
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='F:/UnityProjects/ant-colony-rts/MESHY/NeedleStalker_1114_SmartRig.glb')
rows=[]
for o in bpy.context.scene.objects:
    row={'name':o.name,'type':o.type,'dimensions':list(o.dimensions)}
    if o.type=='ARMATURE':
        row['bones']=[{'name':b.name,'parent':b.parent.name if b.parent else None,'head':list(b.head_local),'tail':list(b.tail_local)} for b in o.data.bones]
    rows.append(row)
Path('F:/UnityProjects/ant-colony-rts/art/enemy-concepts-1114/NeedleStalker-rig.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(rows))

