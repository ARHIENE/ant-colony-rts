import bpy
import math
from pathlib import Path

out = Path('E:/Git/ant/art/characters/simple-resident-v1')
scene = bpy.context.scene
rig = scene.objects['SimpleResident_Rig']
for f in range(1, 26):
    t = (f - 1) / 24 * math.tau
    body = rig.pose.bones['Body']
    body.location = (0, .018 * (1 - math.cos(2*t)), 0)
    body.rotation_euler = (0, 0, .035 * math.sin(t))
    body.keyframe_insert('location', frame=f)
    body.keyframe_insert('rotation_euler', frame=f)
scene.frame_set(1)
bpy.data.libraries.write(str(out / 'simple-resident.blend'), {scene}, fake_user=True)
scene.render.resolution_percentage = 75
scene.cycles.samples = 12
(out / 'frames').mkdir(exist_ok=True)
for f in range(1, 25, 2):
    scene.frame_set(f)
    scene.render.filepath = str(out / 'frames' / ('walk_%02d.png' % f))
    bpy.ops.render.render(write_still=True)
scene.frame_set(1)
scene.render.resolution_percentage = 100
scene.render.filepath = str(out / 'preview.png')
print('Rendered 12 walk frames.')
