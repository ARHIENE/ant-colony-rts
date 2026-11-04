import bpy,json,collections
from pathlib import Path
from mathutils.bvhtree import BVHTree
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'; bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend')); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; sc=bpy.context.scene; vl=bpy.context.view_layer
result={}
for item,part,clip,f in [('Armor_Breastplate_Lower','Part_Outfit_LINEN_ARMOR','Knead',13),('Armor_Fauld','Part_Body_BASE_ARMOR','TapFill',40),('Armor_Bracer_L','Part_Outfit_LINEN_ARMOR','CanisterConnect',28)]:
 a=next(a for a in bpy.data.actions if a.name.split('|',1)[-1]==clip); rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]; sc.frame_set(f); vl.update()
 o=bpy.data.objects[part]; o.hide_viewport=False; o.hide_set(False); vl.update(); ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get()); me=ev.to_mesh(); tree=BVHTree.FromPolygons([ev.matrix_world@v.co for v in me.vertices],[p.vertices[:] for p in me.polygons])
 plate=bpy.data.objects[item]; M=rig.matrix_world@rig.pose.bones[plate['bone']].matrix@rig.data.bones[plate['bone']].matrix_local.inverted()@rig.matrix_world.inverted()@plate.matrix_world
 pt=BVHTree.FromPolygons([M@v.co for v in plate.data.vertices],[p.vertices[:] for p in plate.data.polygons]); hits=pt.overlap(tree); counts=collections.Counter(); faces=set()
 for _,idx in hits:
  faces.add(idx); votes=collections.Counter()
  for i in o.data.polygons[idx].vertices:
   v=o.data.vertices[i]
   if v.groups: votes[o.vertex_groups[max(v.groups,key=lambda g:g.weight).group].name]+=1
  counts[votes.most_common(1)[0][0]]+=1
 result[item+'|'+part]={'clip':clip,'frame':f,'pairs':len(hits),'faces':len(faces),'pairs_by_dominant_bone':dict(counts),'face_ids':sorted(faces)}; ev.to_mesh_clear()
(R/'reports/target-anatomy.json').write_text(json.dumps(result,indent=2),encoding='utf-8'); print(result,flush=True)
