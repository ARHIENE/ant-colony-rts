"""blender -b --factory-startup --python scripts/build_crops.py → pk-crops.blend + previews/crops-build.json
Every seed crop in the catalog × stages S1–S5 (crops.py). Collections Crop_<form>; object Crop_<id>__S<n>."""
import bpy, sys, importlib, traceback, zlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, catalog, crops, build_resources_colors as BC
for m in (pk, catalog, crops, BC): importlib.reload(m)

pk.new_file()
report = {'stages': crops.STAGES, 'crops': {}, 'failed': {}, 'missing_plant_def': []}
row = 0
for gid, gko, iid, ko, recipe, p in catalog.all_items():
    if not p.get('seed'): continue
    if iid not in crops.PLANTS: report['missing_plant_def'].append(iid); continue
    out = []
    for s in range(1, 6):
        try:
            m = pk.Mesh(f'Crop_{iid}__S{s}'); crops.R.seed(zlib.crc32(f'{iid}{s}'.encode()))
            form = crops.build(m, iid, s, BC.main_color(p))
            o = m.to_object(pk.collection('Crop_' + form)); o.location = ((s - 1) * .5, -row * .6, 0)
            o['id'], o['ko'], o['stage'], o['stage_ko'], o['growth'], o['form'] = iid, ko, s, crops.STAGES[s][0], crops.STAGES[s][1], form
            o['origin'] = 'bottom centre (field tile surface)'; out.append(o.name)
        except Exception as e:
            report['failed'][f'{iid}_S{s}'] = f'{type(e).__name__}: {e}'; traceback.print_exc()
    report['crops'][iid] = {'ko': ko, 'form': crops.PLANTS[iid][0], 'objects': out}
    row += 1
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-crops.blend'))
pk.write_json(pk.KIT / 'previews' / 'crops-build.json', report)
print('CROPS', len(report['crops']), 'failed', len(report['failed']), 'missing', report['missing_plant_def'])
