import bpy,json
from pathlib import Path
from mathutils import Vector
root=Path('E:/Git/ant'); out=[]
for path in sorted((root/'Assets/Resources/Kemono/Handheld').glob('*.fbx')):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path))
 meshes=[]
 for ob in bpy.context.scene.objects:
  if ob.type!='MESH':continue
  pts=[ob.matrix_world@v.co for v in ob.data.vertices]
  mats=[]
  for i,mat in enumerate(ob.data.materials):
   ids={v for p in ob.data.polygons if p.material_index==i for v in p.vertices}
   p=[ob.data.vertices[v].co for v in ids]
   if p:mats.append({'name':mat.name if mat else '', 'lo':[min(v[k] for v in p) for k in range(3)],'hi':[max(v[k] for v in p) for k in range(3)]})
  meshes.append({'name':ob.name,'scale':list(ob.scale),'lo':[min(v[k] for v in pts) for k in range(3)],'hi':[max(v[k] for v in pts) for k in range(3)],'materials':mats,'modifiers':[m.type for m in ob.modifiers]})
 out.append({'file':path.name,'meshes':meshes})
 (root/'work/auto1026/equipment-audit.json').write_text(json.dumps(out,indent=2),encoding='utf-8')
print('AUDIT_EQUIPMENT',len(out),flush=True)
