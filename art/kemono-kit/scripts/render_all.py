"""Headless: blender -b kemono-preview-interactions.blend --python render_all.py -- [scene names...]
Every 2nd frame (GIFs play in real time at 12 fps) → previews/frames/<scene>/f###.png. One process per scene (-S): background Blender
only animates the active scene (same reason as resident-kit/scripts/render_all.py)."""
import bpy, sys, subprocess
from pathlib import Path

OUT = Path('E:/Git/ant/art/kemono-kit/previews/frames')
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
if argv == ['--one']:
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_EEVEE'; sc.eevee.taa_render_samples = 8
    import shutil; shutil.rmtree(OUT / sc.name, ignore_errors=True)
    for f in range(sc.frame_start, sc.frame_end, 2):  # frame_end repeats frame_start on loops
        sc.frame_set(f); sc.render.filepath = str(OUT / sc.name / f'f{f:03d}.png')
        bpy.ops.render.render(write_still=True)
    sys.exit(0)
pick = argv or sorted(s.name for s in bpy.data.scenes if s.name.startswith(('Preview__', 'Swap__', 'Motion__')))
running = []
for n in pick:
    running.append((n, subprocess.Popen([bpy.app.binary_path, '-b', bpy.data.filepath, '-S', n, '--python', __file__, '--', '--one'],
                                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)))
    while len(running) >= 4 or (n == pick[-1] and running):
        p = running.pop(0); p[1].wait(); print('done', p[0], p[1].returncode, flush=True)
