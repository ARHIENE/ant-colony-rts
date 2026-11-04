"""blender -b critter-kit.blend --python render_critters.py → previews/frames/<Species>@<Clip>/f*.png, previews/gif via make_gifs"""
import bpy, json, math
from pathlib import Path
from mathutils import Vector
KIT = Path(r"E:\Git\ant\art\critter-kit")
man = json.loads((KIT / 'critter-manifest.json').read_text())
scene = bpy.context.scene
for e in ('BLENDER_EEVEE_NEXT', 'BLENDER_EEVEE'):
    try: scene.render.engine = e; break
    except TypeError: pass
scene.render.resolution_x = scene.render.resolution_y = 320
scene.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new('W'); w.use_nodes = True; scene.world = w
bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND'); bg.inputs['Color'].default_value = (.93, .9, .84, 1); bg.inputs['Strength'].default_value = .6
sun = bpy.data.objects.new('Sun', bpy.data.lights.new('Sun', 'SUN')); sun.data.energy = 2.2; sun.rotation_euler = (math.radians(50), 0, math.radians(-40)); scene.collection.objects.link(sun)
cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam')); scene.collection.objects.link(cam); scene.camera = cam
cam.data.type = 'ORTHO'; cam.data.ortho_scale = 2.6
rigs = {n: bpy.data.objects[v['rig']] for n, v in man.items()}
for name, rig in rigs.items():
    for o in rigs.values():
        for c in [o, *o.children]: c.hide_render = o is not rig
    for clip, info in man[name]['clips'].items():
        act = bpy.data.actions[f'{name}|{clip}']; rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
        c = rig.location + Vector((0, 0, .7 if clip == 'Fly' else .45))
        d = Vector((1.3, -1.6, 1.0)).normalized(); cam.location = c + d * 20; cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
        out = KIT / 'previews' / 'frames' / f'{name}@{clip}'; out.mkdir(parents=True, exist_ok=True)
        n = info['frames']
        for i in range(12):
            f = 1 + round(i * n / 12); scene.frame_set(f)
            scene.render.filepath = str(out / f'f{f:03d}.png'); bpy.ops.render.render(write_still=True)
        print('done', name, clip, flush=True)
