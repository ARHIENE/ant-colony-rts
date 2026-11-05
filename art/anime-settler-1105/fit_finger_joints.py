import bpy, json
from pathlib import Path
from mathutils import Vector
root=Path('F:/UnityProjects/ant-colony-rts/art/anime-settler-1105')
s=bpy.data.scenes['Textured_Modular_Settler'];bpy.context.window.scene=s
r=s.objects['KemonoRig'];r.hide_set(False)
backup=root/'before-finger-rest.json'
if not backup.exists():backup.write_text(json.dumps({b.name:{'head':list(b.head_local),'tail':list(b.tail_local),'matrix':[list(row) for row in b.matrix_local]} for b in r.data.bones}))
# Landmarks measured on the actual Meshy hand, rather than the previous character's fingers.
fingers={
 'f_index':((.250,-.016,.414),(.258,-.017,.390)),
 'f_middle':((.243,.004,.412),(.243,.004,.376)),
 'f_ring':((.227,.020,.411),(.228,.021,.375)),
 'f_pinky':((.210,.031,.412),(.201,.033,.386)),
 'thumb':((.236,-.025,.438),(.257,-.055,.418))}
bpy.ops.object.select_all(action='DESELECT');r.select_set(True);bpy.context.view_layer.objects.active=r
bpy.ops.object.mode_set(mode='EDIT')
for side,sign in [('L',1),('R',-1)]:
 for name,(start,end) in fingers.items():
  a=Vector((start[0]*sign,start[1],start[2]));b=Vector((end[0]*sign,end[1],end[2]))
  for j in range(3):
   bone=r.data.edit_bones[name+'.0'+str(j+1)+'.'+side]
   bone.use_connect=False;bone.head=a.lerp(b,j/3);bone.tail=a.lerp(b,(j+1)/3)
   bone.align_roll(Vector((0,-sign,0)))
bpy.ops.object.mode_set(mode='OBJECT')
exec(compile((root/'repair_hand_weights.py').read_text(),'repair_hand_weights.py','exec'))
print('Fitted 30 finger joints to Meshy hand landmarks')
