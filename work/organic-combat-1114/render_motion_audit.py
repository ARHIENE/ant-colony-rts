"""Fixed-scale 3D pose sequences; no per-frame normalization hiding movement."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
name=sys.argv[sys.argv.index('--')+1]
root=Path('F:/UnityProjects/ant-colony-rts')
source=root/'art/enemy-concepts-1114'/name
out=root/'work/organic-combat-1114/motion-audit'/name
if len(sys.argv)>sys.argv.index('--')+2:
    source=Path(sys.argv[sys.argv.index('--')+2])/name
    out=root/'work/organic-combat-1114/polished-audit'/name
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/(name+'_5Motions.blend')))
s=bpy.context.scene; rig=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
rig.animation_data.action=bpy.data.actions['Idle']; s.frame_set(1)
ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
pts=[ev.matrix_world@v.co for v in ev.data.vertices]
lo=Vector(tuple(min(p[i] for p in pts) for i in range(3)))
hi=Vector(tuple(max(p[i] for p in pts) for i in range(3)))
scale=2.3/max(hi-lo); origin=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,0))
rot=Matrix.Rotation(math.radians(-25),3,'Z')
body=bpy.data.materials.new('Review clay'); body.diffuse_color=(.57,.33,.17,1)
ink=bpy.data.materials.new('Review labels'); ink.diffuse_color=(.025,.03,.04,1)
specs=json.loads((source/'clips.json').read_text())
for row,spec in enumerate(specs):
    for col,fraction in enumerate([0,.16,.32,.50,.66,.83,1]):
        frame=1+round((spec['frames']-1)*fraction)
        rig.animation_data.action=bpy.data.actions[spec['name']]; s.frame_set(frame)
        ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        data=bpy.data.meshes.new_from_object(ev)
        for v in data.vertices: v.co=rot@((ev.matrix_world@v.co-origin)*scale)
        data.materials.clear(); data.materials.append(body)
        inst=bpy.data.objects.new(spec['name']+str(frame),data); s.collection.objects.link(inst)
        inst.location=((col-3)*3.5,((len(specs)-1)/2-row)*4.5,0)
        bpy.ops.object.text_add(location=inst.location+Vector((-1.5,-1.6,.01)))
        text=bpy.context.object; text.data.body=spec['name']+' '+str(frame); text.data.size=.23; text.data.materials.append(ink)
mesh.hide_render=True; rig.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=100)
floor=bpy.context.object; mat=bpy.data.materials.new('Review floor'); mat.diffuse_color=(.62,.65,.68,1); floor.data.materials.append(mat)
bpy.ops.object.camera_add(location=(0,-34,48))
cam=bpy.context.object; cam.rotation_euler=(Vector((0,0,.5))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=27; s.camera=cam
s.render.engine='BLENDER_WORKBENCH'
s.display.shading.light='STUDIO'; s.display.shading.color_type='MATERIAL'; s.display.shading.show_shadows=True
s.display.shading.show_cavity=True; s.display.shading.cavity_type='BOTH'
s.render.resolution_x=2400; s.render.resolution_y=1900; s.render.resolution_percentage=100
s.render.filepath=str(out/'sequence.png'); bpy.ops.render.render(write_still=True)
