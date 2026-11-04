"""Character x facility interpenetration check for kemono-preview-interactions.blend.

Headless: blender -b kemono-preview-interactions.blend --python overlap.py -- [scene names...]
One process per scene (-S), like render_all.py: background Blender only animates the active scene.
Per sampled frame: triangle intersections (BVH overlap) between body parts and facility meshes, and
how deep body vertices sit behind facility faces. Results → previews/overlap/<scene>.json."""
import bpy, sys, json, subprocess
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = Path('E:/Git/ant/art/kemono-kit/previews/overlap')
MAX_DEPTH = .35  # deeper than this behind an open face is "far side", not penetration
argv = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def world_mesh(o, dg):
    ev = o.evaluated_get(dg); me = ev.to_mesh()
    vs = [ev.matrix_world @ v.co for v in me.vertices]
    fs = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    return vs, fs


if argv == ['--one']:
    sc = bpy.context.scene
    rig = next(o for o in sc.objects if o.type == 'ARMATURE')
    body = [o for o in rig.children if o.type == 'MESH' and not o.name.split('__')[-1].startswith('Part_Face')]
    fac = [o for o in sc.objects if o.type == 'MESH' and o.parent is not rig
           and not o.name.startswith(('Tool_', 'Prop_')) and not o.name.startswith(sc.name + '__')]
    res = {'scene': sc.name, 'clip': sc.get('clip'), 'frames': []}
    for f in range(sc.frame_start, sc.frame_end + 1, 2):
        sc.frame_set(f); dg = bpy.context.evaluated_depsgraph_get()
        ftrees = []
        for o in fac:
            vs, fs = world_mesh(o, dg)
            if fs: ftrees.append((o.name, BVHTree.FromPolygons(vs, fs), vs))
        hits = []
        for b in body:
            vs, fs = world_mesh(b, dg)
            if not fs: continue
            bt = BVHTree.FromPolygons(vs, fs)
            for fname, ft, fvs in ftrees:
                pairs = bt.overlap(ft)
                if not pairs: continue
                depth = 0.0
                for v in {i for p, _ in pairs for i in fs[p]} | set(range(len(vs))):
                    loc, n, _, d = ft.find_nearest(vs[v], MAX_DEPTH)
                    if loc is not None and (vs[v] - loc).dot(n) < 0: depth = max(depth, d)
                hits.append({'body': b.name.split('__')[-1], 'fac': fname, 'tris': len(pairs), 'depth': round(depth, 3)})
        if hits: res['frames'].append({'f': f, 'hits': hits})
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / f'{sc.name}.json').write_text(json.dumps(res, ensure_ascii=False), encoding='utf-8')
    sys.exit(0)

pick = argv or sorted(s.name for s in bpy.data.scenes if s.name.startswith('Preview__'))
running = []
for n in pick:
    running.append((n, subprocess.Popen([bpy.app.binary_path, '-b', bpy.data.filepath, '-S', n, '--python', __file__, '--', '--one'],
                                        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)))
    while len(running) >= 4 or (n == pick[-1] and running):
        p = running.pop(0); p[1].wait(); print('done', p[0], p[1].returncode, flush=True)
