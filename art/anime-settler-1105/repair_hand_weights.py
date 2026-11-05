import bpy, math, json
from pathlib import Path
from mathutils import Vector

root=Path('F:/UnityProjects/ant-colony-rts/art/anime-settler-1105')
scene=bpy.data.scenes['Textured_Modular_Settler']
bpy.context.window.scene=scene
rig=scene.objects['KemonoRig'];body=scene.objects['Part_Body_Meadow_00']
# Keep the original skinning for reversibility and for repeatable fitting.
backup=root/'before-hand-weights.json'
if not backup.exists():
 backup.write_text(json.dumps({str(v.index):[(body.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in body.data.vertices}))
old=json.loads(backup.read_text())
for v in body.data.vertices:
 for g in list(v.groups):body.vertex_groups[g.group].remove([v.index])
 for name,w in old[str(v.index)]:body.vertex_groups[name].add([v.index],w,'REPLACE')
changed=0
for side in ['L','R']:
 names=['hand.'+side]+[b.name for b in rig.data.bones if b.name.endswith('.'+side) and b.name.startswith(('f_','thumb.'))]
 for name in names:
  if name not in body.vertex_groups:body.vertex_groups.new(name=name)
 for v in body.data.vertices:
  if v.co.x*(1 if side=='L' else -1)<.19 or v.co.z>.47 or v.co.z<.34:continue
  thumb_region=v.co.y<-.032 and v.co.z>.405
  blend=max(0,min(1,(-v.co.y-.025)/.017)) if thumb_region else max(0,min(1,(.419-v.co.z)/.014))
  ranked=[]
  for name in names:
   if name.startswith('hand.'):continue
   if thumb_region != name.startswith('thumb.'):continue
   b=rig.data.bones[name];a=b.head_local;d=b.tail_local-a
   t=max(0,min(1,(v.co-a).dot(d)/d.length_squared))
   distance=(v.co-a-t*d).length
   ranked.append((distance,name))
  ranked.sort()
  family=ranked[0][1].split('.')[0]
  nearest=[item for item in ranked if item[1].split('.')[0]==family][:3]
  values=[(name,math.exp(-(d-nearest[0][0])/.004)) for d,name in nearest]
  total=sum(w for _,w in values)
  wrist=max(0,min(1,(v.co.z-.438)/.032))
  merged={'hand.'+side:(1-blend)*(1-wrist),'forearm.'+side:(1-blend)*wrist}
  for name,w in values:merged[name]=merged.get(name,0)+blend*w/total
  for g in list(v.groups):body.vertex_groups[g.group].remove([v.index])
  for name,w in merged.items():
   if w>.001:body.vertex_groups[name].add([v.index],w,'REPLACE')
  changed+=1
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o==rig or o.name.startswith('Part_'):o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(root/'Kemono_Character_HandFix.fbx'),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False)
print('Hand skin vertices repaired:',changed)
