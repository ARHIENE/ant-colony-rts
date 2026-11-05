"""Temporary export retarget: give the Meshy arms room to reach a two-handed tool."""
import bpy
from mathutils import Matrix,Vector
def extend_arm_reach(scene,rig):
 original={b.name:(b.head_local.copy(),b.tail_local.copy(),b.matrix_local.copy(),b.length) for b in rig.data.bones}
 bpy.context.view_layer.objects.active=rig
 bpy.ops.object.mode_set(mode='EDIT')
 for side in ['L','R']:
  upper=rig.data.edit_bones['upper_arm.'+side];lower=rig.data.edit_bones['forearm.'+side]
  a,b=original[upper.name][0:2];c=original[lower.name][1]
  elbow=a+(b-a)*1.20;wrist=elbow+(c-b)*1.30;delta=wrist-c
  upper.tail=elbow;lower.head=elbow;lower.tail=wrist
  for bone in rig.data.edit_bones:
   if bone.name.endswith('.'+side) and bone.name.startswith(('hand.','f_','thumb.','Socket.Tool.')):
    bone.head=original[bone.name][0]+delta;bone.tail=original[bone.name][1]+delta
 bpy.ops.object.mode_set(mode='OBJECT')
 transforms={}
 for name,(head,tail,matrix,length) in original.items():
  if name.startswith(('upper_arm.','forearm.','hand.','f_','thumb.')):
   bone=rig.data.bones[name];stretch=Matrix.Diagonal((1,bone.length/length,1,1))
   transforms[name]=bone.matrix_local@stretch@matrix.inverted()
 saved={}
 for obj in scene.objects:
  if obj.type!='MESH' or not obj.name.startswith('Part_'):continue
  changed={}
  for v in obj.data.vertices:
   influences=[(obj.vertex_groups[g.group].name,g.weight) for g in v.groups]
   if not any(n in transforms for n,w in influences):continue
   changed[v.index]=v.co.copy();point=Vector();total=0
   for n,w in influences:
    point+=(transforms[n]@v.co if n in transforms else v.co)*w;total+=w
   if total:v.co=point/total
  if changed:saved[obj]=changed
 def restore():
  for obj,vertices in saved.items():
   for i,p in vertices.items():obj.data.vertices[i].co=p
  bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
  for bone in rig.data.edit_bones:
   head,tail,matrix,length=original[bone.name];bone.head=head;bone.tail=tail;bone.align_roll(matrix.to_3x3().col[2])
  bpy.ops.object.mode_set(mode='OBJECT')
 return restore
