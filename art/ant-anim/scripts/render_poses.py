"""포즈/클립 미리보기: blender -b ant-work-clips.blend --python render_poses.py -- <out.png> <clip:frame,...>
각 칸을 옆·3/4 두 방향으로 렌더해 한 장으로 합친다(Workbench)."""
import bpy, sys, math
from mathutils import Vector
from pathlib import Path

args = sys.argv[sys.argv.index('--') + 1:]
out, cells = Path(args[0]), [c.split(':') for c in args[1].split(',')]
scene = bpy.context.scene; rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
scene.render.engine = 'BLENDER_WORKBENCH'; scene.display.shading.light = 'STUDIO'; scene.display.shading.color_type = 'MATERIAL'
scene.display.shading.show_cavity = True
scene.render.resolution_x = scene.render.resolution_y = 360; scene.render.film_transparent = False
bpy.ops.mesh.primitive_plane_add(size=2); floor = bpy.context.object
cam = bpy.data.objects.new('cam', bpy.data.cameras.new('cam')); scene.collection.objects.link(cam); scene.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = .36
tiles = []
for i, (clip, frame) in enumerate(cells):
    rig.animation_data_create(); rig.animation_data.action = bpy.data.actions['Rig|' + clip]
    scene.frame_set(int(frame))
    for v, (d, label) in enumerate([((1, 0, .15), 'side'), ((1, -1, .6), 'q')]):
        target = Vector((0, -.02, .08)); cam.location = target + Vector(d).normalized() * 2
        cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
        p = out.parent / f'_tile_{i}_{v}.png'; scene.render.filepath = str(p)
        bpy.ops.render.render(write_still=True); tiles.append((i, v, p))
# 합치기
cols = len(cells); W = 360
img = bpy.data.images.new('sheet', W * cols, W * 2)
px = [0.0] * (W * cols * W * 2 * 4)
for i, v, p in tiles:
    t = bpy.data.images.load(str(p)); tp = list(t.pixels)
    for y in range(W):
        row = (y + (1 - v) * W) * W * cols + i * W
        px[row * 4:(row + W) * 4] = tp[y * W * 4:(y + 1) * W * 4]
    bpy.data.images.remove(t); p.unlink()
img.pixels = px; img.filepath_raw = str(out); img.file_format = 'PNG'; img.save()
print('SHEET', out)
