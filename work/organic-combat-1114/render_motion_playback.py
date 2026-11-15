"""Render actual-time animation loops to PNG frames for a review GIF."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
name=sys.argv[sys.argv.index('--')+1]
root=Path('F:/UnityProjects/ant-colony-rts'); source=root/'art/enemy-concepts-1114'/name
out=root/'work/organic-combat-1114/motion-audit'/name/'playback'
if len(sys.argv)>sys.argv.index('--')+2:
    source=Path(sys.argv[sys.argv.index('--')+2])/name
    out=root/'work/organic-combat-1114/polished-audit'/name/'playback'
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/(name+'_5Motions.blend')))
s=bpy.context.scene; original=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
original.animation_data.action=bpy.data.actions['Idle']; s.frame_set(1)
extent=max(mesh.dimensions); scale=2.2/extent
specs=json.loads((source/'clips.json').read_text())
body=bpy.data.materials.new('Review body'); body.diffuse_color=(.57,.33,.17,1)
ink=bpy.data.materials.new('Review label'); ink.diffuse_color=(.02,.03,.04,1)
for i,spec in enumerate(specs):
    rig=original.copy(); rig.data=original.data.copy(); s.collection.objects.link(rig)
    rig.animation_data_clear(); rig.animation_data_create()
    strip=rig.animation_data.nla_tracks.new().strips.new(spec['name'],1,bpy.data.actions[spec['name']])
    strip.repeat=90/(spec['frames']-1) if spec['name']!='Death' else 1
    strip.extrapolation='HOLD'; strip.blend_type='REPLACE'
    model=mesh.copy(); model.data=mesh.data.copy(); s.collection.objects.link(model)
    model.parent=rig; model.data.materials.clear(); model.data.materials.append(body)
    for m in model.modifiers:
        if m.type=='ARMATURE': m.object=rig
    rig.location=((i%3-1)*3.6,(.5-i//3)*4.7,0); rig.scale=(scale,scale,scale)
    bpy.ops.object.text_add(location=rig.location+Vector((-1.3,-1.8,.01)))
    label=bpy.context.object; label.data.body=spec['name']; label.data.size=.28; label.data.materials.append(ink)
original.hide_render=True; mesh.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=100)
floor=bpy.context.object; mat=bpy.data.materials.new('Floor'); mat.diffuse_color=(.65,.68,.72,1); floor.data.materials.append(mat)
bpy.ops.object.camera_add(location=(0,-16,21))
cam=bpy.context.object; cam.rotation_euler=(Vector((0,0,.6))-cam.location).to_track_quat('-Z','Y').to_euler()
cam.data.type='ORTHO'; cam.data.ortho_scale=12; s.camera=cam
s.render.engine='BLENDER_WORKBENCH'; s.display.shading.light='STUDIO'; s.display.shading.color_type='MATERIAL'
s.display.shading.show_shadows=False; s.display.shading.show_cavity=True
s.render.resolution_x=900; s.render.resolution_y=650; s.render.resolution_percentage=100
for f in range(1,91,2):
    s.frame_set(f); s.render.filepath=str(out/f'{f:03d}.png'); bpy.ops.render.render(write_still=True)
