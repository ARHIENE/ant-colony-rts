import bpy, json
from pathlib import Path
from mathutils import Vector
out=Path('F:/UnityProjects/ant-colony-rts/art/enemy-concepts-1114/NeedleStalker')
bpy.ops.wm.open_mainfile(filepath=str(out/'NeedleStalker_5Motions.blend'))
s=bpy.context.scene; rig=bpy.data.objects['NeedleStalker_Rig']; mesh=bpy.data.objects['NeedleStalker_Mesh']
s.render.engine='CYCLES'; s.cycles.samples=16
s.render.resolution_x=640; s.render.resolution_y=640; s.render.resolution_percentage=100
s.world=bpy.data.worlds.new('PreviewWorld'); s.world.color=(.15,.15,.15)
bpy.ops.object.camera_add(location=(4,-6,3.1)); cam=bpy.context.object
cam.rotation_euler=(Vector((0,0,.8))-cam.location).to_track_quat('-Z','Y').to_euler(); cam.data.type='ORTHO'; cam.data.ortho_scale=4.5; s.camera=cam
for pos,power,size in [((1,-4,6),1000,5),((-4,1,4),750,4),((3,4,4),900,3)]:
    bpy.ops.object.light_add(type='AREA',location=pos); l=bpy.context.object; l.data.energy=power; l.data.shape='DISK'; l.data.size=size; l.rotation_euler=(Vector((0,0,.5))-l.location).to_track_quat('-Z','Y').to_euler()
audit=[]
for name,frame in [('Idle',1),('Move',8),('Attack',23),('Hit',4),('Death',61)]:
    rig.animation_data.action=bpy.data.actions[name]
    s.frame_set(frame)
    dg=bpy.context.evaluated_depsgraph_get(); ev=mesh.evaluated_get(dg); pts=[ev.matrix_world@v.co for v in ev.data.vertices]
    audit.append({'clip':name,'frame':frame,'minZ':min(p.z for p in pts),'maxZ':max(p.z for p in pts)})
    s.render.filepath=str(out/(name+'-preview.png')); bpy.ops.render.render(write_still=True)
(out/'preview-audit.json').write_text(json.dumps(audit,indent=2))


