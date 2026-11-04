"""blender -b --factory-startup --python scripts/build_resources.py → pk-resources.blend + previews/resources-build.json

Every catalog entry (305) → single model (origin bottom centre, real size) + carry/floor stack.
Perishable foods get a rotten variant (same shape, browned colours + mould spots); seed crops get a seed packet.
Collections: Res_<group> (singles), Res_Stack, Res_Rotten, Res_Seed. Each object carries manifest props.
Failures are recorded per item instead of stopping the build.
"""
import bpy, sys, importlib, traceback
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, catalog, resources, build_resources_colors
for m in (pk, catalog, resources, build_resources_colors): importlib.reload(m)
main_color = build_resources_colors.main_color
from mathutils import Vector

RES = resources; R = RES.R
pk.new_file()
cols = {g: pk.collection('Res_' + g) for g, *_ in catalog.GROUPS}
c_stack, c_rot, c_seed = pk.collection('Res_Stack'), pk.collection('Res_Rotten'), pk.collection('Res_Seed')


def props(o, gid, gko, iid, ko, recipe, variant):
    o['id'], o['ko'], o['group'], o['group_ko'], o['recipe'], o['variant'] = iid, ko, gid, gko, recipe, variant
    lo = Vector(o.bound_box[0]); hi = Vector(o.bound_box[6]); o['size_m'] = [round(v, 3) for v in hi - lo]
    o['origin'] = 'bottom centre'; o['axes'] = '+Z up, front -Y'


report = {'items': {}, 'failed': {}}
row = 0
for gid, gko, items in catalog.GROUPS:
    for col_i, (iid, ko, recipe, p) in enumerate(items):
        x, y = col_i * .3, -row * .3
        try:
            fn = RES.recipe_fn(recipe, gid)
            m = pk.Mesh(iid); fn(m, p); o = m.to_object(cols[gid]); o.location = (x, y, 0)
            props(o, gid, gko, iid, ko, recipe, 'single'); out = {'single': o.name}

            s = RES.stack(lambda mm: fn(mm, p), recipe, p, gid); s.name = iid + '__Stack'
            so = s.to_object(c_stack); so.location = (x, y - .15, 0); props(so, gid, gko, iid, ko, recipe, 'stack'); out['stack'] = so.name

            perish = gid in catalog.FOOD_GROUPS and gid != 'alcohol' and iid not in catalog.KEEPS and iid not in catalog.NON_FOOD_IN_FOOD
            if perish:
                r = pk.Mesh(iid + '__Rotten'); fn(r, p); lo, hi = r.bounds()
                n_mats = len(r.mats)
                for k in range(5):  # mould spots on the upper half
                    r.ball((R.uniform(lo.x, hi.x) * .7, R.uniform(lo.y, hi.y) * .7, R.uniform((lo.z + hi.z) / 2, hi.z)),
                           (.004, .004, .002), RES.MOLD, seg=6, rings=3)
                ro = r.to_object(c_rot); ro.location = (x + .5 * 0, y - .3 * 0, 0)
                for i, mt in enumerate(RES.rot_materials(list(ro.data.materials)[:n_mats])): ro.data.materials[i] = mt
                ro.location = (x, y + .15, 0); props(ro, gid, gko, iid, ko, recipe, 'rotten'); out['rotten'] = ro.name
            if p.get('seed'):
                sp = pk.Mesh(iid + '__Seed'); RES.seed_packet(sp, main_color(p), '#7a6a50')
                spo = sp.to_object(c_seed); spo.location = (x + .12, y, 0); props(spo, gid, gko, iid, ko + ' 씨앗', 'seed_packet', 'seed'); out['seed'] = spo.name
            report['items'][iid] = out
        except Exception as e:
            report['failed'][iid] = f'{type(e).__name__}: {e}'; traceback.print_exc()
    row += 2 + len(items) // 20

bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-resources.blend'))
n = lambda v: sum(1 for x in report['items'].values() if v in x)
report['counts'] = {'single': n('single'), 'stack': n('stack'), 'rotten': n('rotten'), 'seed': n('seed'), 'failed': len(report['failed'])}
pk.write_json(pk.KIT / 'previews' / 'resources-build.json', report)
print('RESOURCES', report['counts'])
