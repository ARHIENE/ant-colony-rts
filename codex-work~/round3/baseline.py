import bpy,json,hashlib
from pathlib import Path
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/source.blend'))
for name,path in [('PK|Tool_Till',W/'blender/pk-motions.blend'),('PK|Tool_Mine',W/'round2/blender/pk-motions-round2.blend')]:
 original=bpy.data.actions.get(name)
 if original: bpy.data.actions.remove(original)
 with bpy.data.libraries.load(str(path),link=False) as (a,b): b.actions=[name]
 action=b.actions[0]; action.name=name; action.use_fake_user=True
rig=bpy.data.objects['KemonoRig']; rig.animation_data.action=None
for b in rig.pose.bones: b.matrix_basis.identity()
rig.data.pose_position='REST'; bpy.context.view_layer.update()
info={}
for name in ['Part_Body_BASE_00','Part_Body_BASE_ARMOR','Part_Outfit_LINEN_00','Part_Outfit_LINEN_ARMOR','Armor_Breastplate_Lower','Armor_Fauld','Armor_Tasset_L','Armor_Tasset_R']:
 o=bpy.data.objects[name]; groups={}
 for v in o.data.vertices:
  if v.groups:
   g=max(v.groups,key=lambda g:g.weight); n=o.vertex_groups[g.group].name
   groups.setdefault(n,[]).append(list(v.co))
 info[name]={'vertices':len(o.data.vertices),'faces':len(o.data.polygons),'bounds':[[min(v.co[i] for v in o.data.vertices),max(v.co[i] for v in o.data.vertices)] for i in range(3)],'bone':o.get('bone'),'groups':{k:{'count':len(vs),'bounds':[[min(v[i] for v in vs),max(v[i] for v in vs)] for i in range(3)]} for k,vs in groups.items()}}
(R/'reports/inventory.json').write_text(json.dumps(info,indent=2),encoding='utf-8')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(R/'blender/baseline.blend'))
print('INVENTORY',json.dumps({k:{kk:vv for kk,vv in v.items() if kk!='groups'} for k,v in info.items()}),flush=True)
