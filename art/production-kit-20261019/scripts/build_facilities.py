"""blender -b --factory-startup --python scripts/build_facilities.py → pk-facilities.blend + previews/facilities-build.json

New facilities / pipes / deposits / overlay (fac_*.py, deposits.py) + the reuse table for resident-kit facilities that
already cover a requested one (kept in resident-facility-kit.blend; resident units → kit m ×0.625, see manifest)."""
import bpy, sys, importlib, traceback
sys.path.insert(0, r"E:\Git\ant\art\production-kit-20261019\scripts")
import pk
importlib.reload(pk); pk.new_file()  # before the fac modules load: they create shared materials at import time
import facilities, fac_workshop, fac_food, fac_extract, fac_fluid, deposits, fac_civic
for m in (facilities, fac_workshop, fac_food, fac_extract, fac_fluid, deposits, fac_civic): importlib.reload(m)

REUSE = {  # request item → existing resident-kit root (not rebuilt here)
    '조리대·조리 시설': ['CC_Kitchen', 'CC_Hearth'], '목공 작업대': ['CC_Carpentry'], '석재 작업대': ['CC_Stonecutter'],
    '제련로': ['CC_Smelter'], '도자기 가마': ['CC_Kiln'], '냉장고': ['CC_Refrigerator'], '냉동고': ['CC_Freezer'],
    '퇴비통': ['CC_CompostBin'], '기존 발전기': ['CC_WoodGenerator', 'CC_Battery', 'CC_PowerWire'],
}
groups = [('Fac_Workshop', fac_workshop.ALL), ('Fac_Food', fac_food.ALL), ('Fac_Extract', fac_extract.ALL),
          ('Fac_Fluid', fac_fluid.ALL), ('Fac_Deposit', deposits.ALL), ('Fac_Civic', fac_civic.ALL)]
report = {'facilities': {}, 'failed': {}, 'reuse_resident': REUSE, 'actions': []}
row = 0
for cname, fns in groups:
    col = pk.collection(cname); x = 0
    for fn in fns:
        try:
            res = fn(col); roots = res if isinstance(res, list) else [res]
            for r in roots:
                w = r['bounds'][1][0] - r['bounds'][0][0]
                r.location = (x + w / 2, -row * 4.0, 0); x += w + .8
                report['facilities'][r['id']] = {k: pk.to_py(r[k]) for k in r.keys() if k not in ('_RNA_UI',)}
                report['facilities'][r['id']]['collection'] = cname
        except Exception as e:
            report['failed'][getattr(fn, '__name__', str(fn))] = f'{type(e).__name__}: {e}'; traceback.print_exc()
    row += 1
# stand points from contact_check (shared clip + per-facility offset): WorkPoint_0 moves so the palm lands on HandTarget
cc = pk.KIT / 'previews' / 'facility-contacts.json'
if cc.exists():
    import json
    for fid, v in json.loads(cc.read_text(encoding='utf-8')).items():
        e = bpy.data.objects.get(f'{fid}.WorkPoint_0'); rec = v.get('recommended_workpoint')
        if not (e and rec) or fid == 'DeepDrill': continue  # the driver sits on SeatPoint
        e.location = rec; r = bpy.data.objects['FAC_' + fid]; pts = r['points'].to_dict()
        pts['WorkPoint_0']['pos'] = rec; pts['WorkPoint_0']['source'] = 'contact_check'; r['points'] = pts
        report['facilities'][fid]['points'] = pts; report['facilities'][fid]['contact'] = {k: v[k] for k in ('clip', 'event', 'frame', 'residual_z_m')}
report['actions'] = sorted(a.name for a in bpy.data.actions)
for o in bpy.data.objects:  # parts keep their rest pose; actions are stored, not left playing
    if o.animation_data: o.animation_data.action = None
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-facilities.blend'))
pk.write_json(pk.KIT / 'previews' / 'facilities-build.json', report)
print('FACILITIES', len(report['facilities']), 'actions', len(report['actions']), 'failed', report['failed'])
