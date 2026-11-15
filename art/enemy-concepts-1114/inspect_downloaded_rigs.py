import bpy, json
from pathlib import Path

ROOT = Path('F:/UnityProjects/ant-colony-rts')
OUT = ROOT / 'art/enemy-concepts-1114'
for src in (ROOT / 'MESHY').glob('*_1114_SmartRig.glb'):
    name = src.name.removesuffix('_1114_SmartRig.glb')
    dst = OUT / (name + '-rig.json')
    if dst.exists():
        continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(src))
    rows = []
    for o in bpy.context.scene.objects:
        row = {'name': o.name, 'type': o.type, 'dimensions': list(o.dimensions)}
        if o.type == 'ARMATURE':
            row['bones'] = [{'name': b.name, 'parent': b.parent.name if b.parent else None,
                             'head': list(b.head_local), 'tail': list(b.tail_local)} for b in o.data.bones]
        rows.append(row)
    dst.write_text(json.dumps(rows, indent=2))
    print('INSPECTED', name)
