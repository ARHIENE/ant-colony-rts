import bpy,json
from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); out={}
for label,path in [('before','E:/Git/ant/art/production-kit-20261019/pk-motions.blend'),('after',str(w/'blender/pk-motions.blend'))]:
 bpy.ops.wm.open_mainfile(filepath=path); rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; rig.animation_data.action=None
 for pb in rig.pose.bones: pb.matrix_basis.identity()
 a=bpy.data.actions['PK|Tool_Till']; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 bpy.context.scene.frame_set(16); bpy.context.view_layer.update()
 out[label]={'bones':{b.name:[list(r) for r in b.matrix] for b in rig.pose.bones},'meshes':{n:{'matrix':[list(r) for r in bpy.data.objects[n].matrix_world],'verts':[list(v.co) for v in bpy.data.objects[n].data.vertices][:3]} for n in ['Part_Outfit_LINEN_ARMOR','Armor_Backplate']}}
(w/'reports/pose-comparison.json').write_text(json.dumps(out),encoding='utf-8')
print('DIFF BONES',[n for n in out['before']['bones'] if out['before']['bones'][n]!=out['after']['bones'][n]])
print('DIFF MESH',[n for n in out['before']['meshes'] if out['before']['meshes'][n]!=out['after']['meshes'][n]])
