"""Re-export the runtime hand library at the grip origin, without preview-grid offsets."""
import bpy,json,shutil,math
from pathlib import Path
from mathutils import Matrix
ROOT=Path('E:/Git/ant'); kit=ROOT/'art/production-kit-20261019'
src=ROOT/'Assets/Resources/Kemono/Handheld'; backup=ROOT/'work/auto1026/equipment-before'; backup.mkdir(parents=True,exist_ok=True)
report=[]
for path in sorted(src.glob('*.fbx')):
 if not (backup/path.name).exists():shutil.copy2(path,backup/path.name)
 bpy.ops.wm.read_factory_settings(use_empty=True)
 bpy.ops.import_scene.fbx(filepath=str(path))
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
 before=[list(o.matrix_world.translation) for o in meshes]
 for ob in meshes:
  ob.parent=None;ob.matrix_world=Matrix.Identity(4)
  # The shaft/head mesh coordinates already share the grip; exported layout belongs to the library display only.
  ob.select_set(True)
  if not ob.data.vertices:raise ValueError('Empty equipment '+path.name)
 bpy.context.view_layer.objects.active=meshes[0]
 out=kit/'exports/handheld'/path.name;out.parent.mkdir(parents=True,exist_ok=True)
 bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',add_leaf_bones=False)
 shutil.copy2(out,path)
 report.append({'file':path.name,'old_offset':before,'mesh_count':len(meshes),'origin':'grip zero','rigid':all(not o.modifiers for o in meshes)})
print('REPAIRED_HANDHELD',len(report),flush=True)
(ROOT/'work/auto1026/equipment-repair.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
