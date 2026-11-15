"""Render five animated 3D instances together; no raster compositing."""
import bpy,sys,math
from pathlib import Path
from mathutils import Vector

name=sys.argv[sys.argv.index('--')+1]
out=Path(__file__).resolve().parent/name
bpy.ops.wm.open_mainfile(filepath=str(out/(name+'_5Motions.blend')))
s=bpy.context.scene
original=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
poses=[('Idle',1),('Move',8),('Attack',23),('Hit',4),('Death',61)]
original.animation_data.action=bpy.data.actions['Idle']; s.frame_set(1)
rest=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
rest_points=[rest.matrix_world@v.co for v in rest.data.vertices]
rest_size=max(max(p[i] for p in rest_points)-min(p[i] for p in rest_points) for i in range(3))
for index,(clip,frame) in enumerate(poses):
    original.animation_data.action=bpy.data.actions[clip]; s.frame_set(frame)
    evaluated=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    data=bpy.data.meshes.new_from_object(evaluated)
    instance=bpy.data.objects.new(clip,data); s.collection.objects.link(instance)
    instance.matrix_world=mesh.matrix_world.copy()
    points=[instance.matrix_world@v.co for v in data.vertices]
    minimum=Vector(tuple(min(p[i] for p in points) for i in range(3)))
    maximum=Vector(tuple(max(p[i] for p in points) for i in range(3)))
    center=(minimum+maximum)/2
    size=max(maximum.x-minimum.x,maximum.y-minimum.y,maximum.z-minimum.z)
    scale=2.3/max(size,rest_size)
    # Freeze only the preview mesh. Production assets retain their armature.
    for vertex,point in zip(data.vertices,points):
        vertex.co=(point-Vector((center.x,center.y,minimum.z)))*scale
    instance.matrix_world.identity(); instance.location.x=(index-2)*3.1
    bpy.ops.object.text_add(location=((index-2)*3.1-1.0,-1.6,.015),rotation=(math.pi/2,0,0))
    label=bpy.context.object; label.data.body=clip; label.data.size=.26
    ink=bpy.data.materials.get('LabelInk') or bpy.data.materials.new('LabelInk')
    ink.use_nodes=True; ink.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.03,.03,.035,1)
    label.data.materials.append(ink)
mesh.hide_render=True
original.hide_render=True
s.world=bpy.data.worlds.new('MotionPreviewWorld'); s.world.use_nodes=True
s.world.node_tree.nodes['Background'].inputs['Color'].default_value=(.09,.11,.15,1)
bpy.ops.mesh.primitive_plane_add(size=200)
floor=bpy.context.object
mat=bpy.data.materials.new('Preview floor'); mat.use_nodes=True
mat.node_tree.nodes.get('Principled BSDF').inputs['Base Color'].default_value=(.085,.09,.105,1)
floor.data.materials.append(mat)
for pos,power,size in [((0,-4,10),1400,12),((-6,3,7),900,8),((6,3,6),900,8)]:
    bpy.ops.object.light_add(type='AREA',location=pos)
    light=bpy.context.object; light.data.energy=power; light.data.shape='DISK'; light.data.size=size
    light.rotation_euler=(Vector((0,0,.5))-light.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(0,-15,10))
cam=bpy.context.object; cam.rotation_euler=(Vector((0,0,.8))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=16; s.camera=cam
s.render.engine='CYCLES'; s.cycles.samples=12
s.render.resolution_x=1600; s.render.resolution_y=430; s.render.resolution_percentage=100
s.render.filepath=str(out/'motion-strip.png')
bpy.ops.render.render(write_still=True)
