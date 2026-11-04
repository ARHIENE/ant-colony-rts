"""Only export clips that pass search and independent all-frame armour validation."""
import bpy,json,hashlib
from pathlib import Path
from mathutils import Vector
W=Path('E:/Git/ant/codex-work~'); R=W/'round2'; reports=json.loads((R/'reports/search-results.json').read_text()); delta=json.loads((R/'reports/fit-delta.json').read_text())
assert delta['passed'],delta
accepted=[n for n,v in reports.items() if v['accepted']]
results={}
for clip in accepted:
 bpy.ops.wm.open_mainfile(filepath=str(R/'blender/pk-motions-round2.blend'))
 rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'; rig.animation_data.action=None
 for b in rig.pose.bones: b.matrix_basis.identity()
 a=bpy.data.actions['PK|'+clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]; n=int(a.get('duration_frames',a.frame_range[1])); sc=bpy.context.scene; vl=bpy.context.view_layer; refs={}
 for f in range(1,n+1):
  sc.frame_set(f); vl.update(); refs[f]={b.name:((rig.matrix_world@b.matrix).translation.copy(),(rig.matrix_world@b.matrix).to_quaternion()) for b in rig.pose.bones}
 sc.frame_start=1; sc.frame_end=n
 for o in vl.objects: o.select_set(False)
 rig.hide_set(False); rig.hide_viewport=False; rig.select_set(True); vl.objects.active=rig
 # Export into staging. Publish into fbx only after reimport passes.
 target='Assets/_Project/Art/Kemono/Anim/Kemono@PK_'+clip+'.fbx'; stage=R/'blender'/('Kemono@PK_'+clip+'.fbx')
 bpy.ops.export_scene.fbx(filepath=str(stage),object_types={'ARMATURE'},bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_selection=True,apply_scale_options='FBX_SCALE_ALL')
 bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=str(stage)); rig=next(o for o in bpy.data.objects if o.type=='ARMATURE'); sc=bpy.context.scene; vl=bpy.context.view_layer; act=rig.animation_data.action; off=act.frame_range[0]-1; poserr=0; roterr=0
 for f,bones in refs.items():
  sc.frame_set(int(f+off)); vl.update()
  for name,(p,q) in bones.items():
   M=rig.matrix_world@rig.pose.bones[name].matrix; poserr=max(poserr,(M.translation-p).length); roterr=max(roterr,q.rotation_difference(M.to_quaternion()).angle)
 passed=poserr<.001 and roterr<.00174533 and abs(act.frame_range[1]-act.frame_range[0]-(n-1))<.01
 results[clip]={'passed':passed,'frames':n,'bones':len(refs[1]),'max_position_error_m':poserr,'max_rotation_error_rad':roterr,'import_range':list(act.frame_range),'target':target}
 assert passed,results[clip]
 out=W/'fbx'/target; out.parent.mkdir(parents=True,exist_ok=True); out.write_bytes(stage.read_bytes())
 results[clip]['sha256']=hashlib.sha256(out.read_bytes()).hexdigest(); results[clip]['bytes']=out.stat().st_size
 print('FBX PASS',clip,results[clip],flush=True)
(R/'reports/fbx-validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
