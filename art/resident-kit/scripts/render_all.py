"""Headless: blender -b resident-preview.blend --python render_all.py -- [start] [end] | [scene names...]
Renders 12 frames per preview scene into previews/frames/<scene>/f###.png.

Each scene is rendered in its own Blender process started with -S <scene>: in background mode only the
active scene is evaluated on frame_set, so rendering other scenes from one process froze every rig and
hinge on its rest pose (the 2026-10-18 first-pass GIFs were static because of this)."""
import bpy, sys, subprocess
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_render as R
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
if argv == ['--one']:
    R.render([bpy.context.scene.name], out=R.OUT / 'frames', count=12)
    sys.exit(0)
names = sorted(s.name for s in bpy.data.scenes if s.name.startswith(('Preview__', 'Swap__', 'Motion__', 'Equip__')))
if argv and not argv[0].isdigit():
    pick = argv
elif len(argv) == 2:
    pick = names[int(argv[0]):int(argv[1])]
else:
    pick = names
JOBS = 4
running = []
for n in pick:
    running.append((n, subprocess.Popen([bpy.app.binary_path, '-b', bpy.data.filepath, '-S', n, '--python', __file__, '--', '--one'],
                                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)))
    while len(running) >= JOBS or (n == pick[-1] and running):
        for item in list(running):
            if item[1].poll() is not None:
                running.remove(item); print('done', item[0], item[1].returncode, flush=True)
        if len(running) >= JOBS or (n == pick[-1] and running):
            running[0][1].wait()
