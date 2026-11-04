"""Mask only _ARMOR skin continuously occluded by unchanged cloth/gear, with no mutual masking."""
import bpy,bmesh,json,math,itertools
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
R=Path('E:/Git/ant/codex-work~/round3'); bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend'))
rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='REST'; sc=bpy.context.scene; vl=bpy.context.view_layer; body=bpy.data.objects['Part_Body_BASE_ARMOR']; cloth=bpy.data.objects['Part_Outfit_LINEN_ARMOR']; gear=[o for o in bpy.data.objects if o.name.startswith('Armor_') and o.get('rigid') and '_Trim_' not in o.name]
for o in (body,cloth): o.hide_viewport=False; o.hide_set(False)
rest={b.name:b.matrix_local.copy() for b in rig.data.bones}; skin0=BVHTree.FromPolygons([v.co.copy() for v in cloth.data.vertices],[p.vertices[:] for p in cloth.data.polygons]); allowed={'forearm.L','forearm.R','upper_arm.L','upper_arm.R','thigh.L','thigh.R','pevis','spine','spine_01','spine_02'}; eligible=set()
for p in body.data.polygons:
 groups=[]
 for i in p.vertices:
  v=body.data.vertices[i]
  if v.groups: groups.append(body.vertex_groups[max(v.groups,key=lambda g:g.weight).group].name)
 if groups and all(n in allowed for n in groups):
  center=sum((body.data.vertices[i].co for i in p.vertices),Vector())/len(p.vertices)
  if skin0.find_nearest(center)[3]<.025: eligible.add(p.index)
proposed=sorted(eligible); excluded=set(proposed); dirs=[Vector(v).normalized() for v in itertools.product((-1,0,1),repeat=3) if v!=(0,0,0)]; samples=[]; acts={a.name.split('|',1)[1]:a for a in bpy.data.actions if a.name.startswith(('Kemono|','PK|'))}
print('SKIN PROPOSED',len(proposed),flush=True); rig.data.pose_position='POSE'
for clip in sorted(acts):
 a=acts[clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 for f in range(1,int(a.get('duration_frames',a.frame_range[1]))+1,3):
  sc.frame_set(f); vl.update(); samples.append([clip,f])
  if not eligible: continue
  dg=bpy.context.evaluated_depsgraph_get(); vs=[]; ps=[]; body_positions=None
  for obj in (body,cloth):
   ev=obj.evaluated_get(dg); me=ev.to_mesh(); points=[ev.matrix_world@v.co for v in me.vertices]; off=len(vs); vs+=points
   if obj==body: body_positions=points; faces=[p for p in me.polygons if p.index not in excluded]
   else: faces=list(me.polygons)
   ps += [[i+off for i in p.vertices] for p in faces]; ev.to_mesh_clear()
  for o in gear:
   M=rig.matrix_world@rig.pose.bones[o['bone']].matrix@rest[o['bone']].inverted(); off=len(vs); vs += [M@v.co for v in o.data.vertices]; ps += [[i+off for i in p.vertices] for p in o.data.polygons]
  tree=BVHTree.FromPolygons(vs,ps); rejected=[]
  for idx in eligible:
   p=body.data.polygons[idx]; pts=[body_positions[i] for i in p.vertices]; corners=pts[:]; pts += [sum(pts,Vector())/len(pts)]
   pts += [(p+q)*.5 for p,q in zip(corners,corners[1:]+corners[:1])]
   if any(tree.ray_cast(point+direction*.0002,direction,.5)[0] is None for point in pts for direction in dirs): rejected.append(idx)
  eligible.difference_update(rejected)
 print('SKIN',clip,len(samples),len(eligible),flush=True)
assert len(samples)==1230
rig.animation_data.action=None
for pb in rig.pose.bones: pb.matrix_basis.identity()
rig.data.pose_position='REST'; old=len(body.data.polygons); body.data=body.data.copy(); bm=bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table(); bmesh.ops.delete(bm,geom=[bm.faces[i] for i in sorted(eligible)],context='FACES_ONLY'); bm.to_mesh(body.data); bm.free(); body.data.update(); bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(R/'blender/candidate-skin-mask.blend'))
(R/'reports/skin-mask.json').write_text(json.dumps({'proposed':proposed,'deleted':sorted(eligible),'faces_before':old,'faces_after':len(body.data.polygons),'samples':samples,'directions':26,'occluders':'unchanged LINEN_ARMOR, original noncandidate skin, rigid armour; no candidate skin used as occluder'},indent=2),encoding='utf-8')
print('SKIN COMPLETE',old,len(body.data.polygons),len(eligible),flush=True)
