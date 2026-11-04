"""Face-only masking candidate: projected armour coverage across the exact 1230 fit samples."""
import bpy,bmesh,json,math,hashlib
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'
bpy.ops.wm.open_mainfile(filepath=str(R/'blender/baseline.blend'))
rig=bpy.data.objects['KemonoRig']; vl=bpy.context.view_layer; sc=bpy.context.scene; rig.data.pose_position='REST'; vl.update()
names=['Part_Outfit_LINEN_ARMOR','Part_Body_BASE_ARMOR']; parts=[bpy.data.objects[n] for n in names]
armour=[o for o in bpy.data.objects if o.name.startswith('Armor_') and o.get('rigid') and '_Trim_' not in o.name]
rest={b.name:b.matrix_local.copy() for b in rig.data.bones}
def armour_tree(posed):
 vs=[]; ps=[]
 for o in armour:
  M=rig.matrix_world@rig.pose.bones[o['bone']].matrix@rest[o['bone']].inverted() if posed else rig.matrix_world
  off=len(vs); vs += [M@v.co for v in o.data.vertices]; ps += [[i+off for i in p.vertices] for p in o.data.polygons]
 return BVHTree.FromPolygons(vs,ps)
static=armour_tree(False); eligible={}; proposed={}
for o in parts:
 faces=set()
 for p in o.data.polygons:
  center=sum((o.matrix_world@o.data.vertices[i].co for i in p.vertices),Vector())/len(p.vertices)
  # Limit to garment/body under torso and hip plates; exposed limbs and neck remain untouched.
  if not (.29<center.z<.625): continue
  groups=[o.vertex_groups[max(o.data.vertices[i].groups,key=lambda g:g.weight).group].name for i in p.vertices if o.data.vertices[i].groups]
  if not groups or any(n not in ('pevis','spine','spine_01','spine_02','thigh.L','thigh.R') for n in groups): continue
  if static.find_nearest(center)[3]>.025: continue
  faces.add(p.index)
 eligible[o.name]=faces; proposed[o.name]=len(faces)
print('PROPOSED',proposed,flush=True)
# Opposite rays also allow removal of a surface poking just outside a plate: armour remains behind that pixel.
dirs=[Vector(v).normalized() for v in [(0,-1,.12),(1,0,.12),(0,1,.12),(-1,0,.12),(1,-1,.12),(1,1,.12),(-1,-1,.12),(-1,1,.12)]]
rig.data.pose_position='POSE'; acts={a.name.split('|',1)[1]:a for a in bpy.data.actions if a.name.startswith(('Kemono|','PK|'))}; samples=[]
for clip in sorted(acts):
 a=acts[clip]; rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 for f in range(1,int(a.get('duration_frames',a.frame_range[1]))+1,3):
  sc.frame_set(f); vl.update(); samples.append([clip,f]); tree=armour_tree(True); dg=bpy.context.evaluated_depsgraph_get()
  for o in parts:
   if not eligible[o.name]: continue
   o.hide_viewport=False; o.hide_set(False); vl.update(); ev=o.evaluated_get(dg); me=ev.to_mesh(); rejected=[]
   for idx in eligible[o.name]:
    p=me.polygons[idx]; points=[ev.matrix_world@me.vertices[i].co for i in p.vertices]; points += [sum(points,Vector())/len(points)]
    safe=True
    for point in points:
     for direction in dirs:
      if tree.ray_cast(point+direction*.0001,direction,.18)[0] is None and tree.ray_cast(point-direction*.0001,-direction,.18)[0] is None:
       safe=False; break
     if not safe: break
    if not safe: rejected.append(idx)
   eligible[o.name].difference_update(rejected); ev.to_mesh_clear()
 print('MASK',clip,len(samples),{n:len(v) for n,v in eligible.items()},flush=True)
assert len(samples)==1230,len(samples)
rig.animation_data.action=None
for pb in rig.pose.bones: pb.matrix_basis.identity()
rig.data.pose_position='REST'; changes={}
for o in parts:
 before=len(o.data.polygons); o.data=o.data.copy(); bm=bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table(); dead=[bm.faces[i] for i in sorted(eligible[o.name])]
 bmesh.ops.delete(bm,geom=dead,context='FACES_ONLY'); bm.to_mesh(o.data); bm.free(); o.data.update()
 changes[o.name]={'faces_before':before,'faces_after':len(o.data.polygons),'deleted_faces':sorted(eligible[o.name]),'vertices':len(o.data.vertices)}
sc.frame_set(1); vl.update(); bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(R/'blender/candidate.blend'))
(R/'reports/masking.json').write_text(json.dumps({'method':'face-only removal; original vertices/weights/gear retained','proposed':proposed,'changes':changes,'samples':samples,'projection_directions':[list(v) for v in dirs],'reach_m':.18},indent=2),encoding='utf-8')
print('CANDIDATE',changes,flush=True)
