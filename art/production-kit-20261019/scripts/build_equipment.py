"""blender -b --factory-startup --python scripts/build_equipment.py
→ pk-equipment.blend: kemono character copy + extra sockets + helmets/armour/belt/scabbard + handheld library."""
import bpy, sys, importlib, json
sys.path.insert(0, r"E:\Git\ant\art\production-kit-20261019\scripts")
import pk, equip_spec, rig_adapter, handheld, equipment
for m in (pk, equip_spec, rig_adapter, handheld, equipment): importlib.reload(m)
R = rig_adapter

s = pk.new_file()
rig = R.load_character(s)
acts = R.load_actions()
R.add_sockets(rig)
bpy.context.view_layer.update()
eq_col = pk.collection('Equipment')
info = equipment.build(eq_col)
hh_col = pk.collection('Handheld')
tools = handheld.build(hh_col)
for i, o in enumerate(tools): o.location = (1.2 + (i % 11) * .15, -(i // 11) * .55, 0)
bpy.ops.wm.save_as_mainfile(filepath=str(pk.KIT / 'pk-equipment.blend'))
pk.write_json(pk.KIT / 'previews' / 'equipment-build.json', {
    'helm_radii': info['radii'], 'shells': {k: v.get('holes') for k, v in info['shells'].items()},
    'armor': [o.name for o in info['armor']], 'armor_trims': len(info['armor_trims']), 'helm_trims': [o.name for o in info['helm_trims']],
    'belt': dict(info['belt']['fit']), 'handheld': len(tools), 'actions': len(acts),
    'hair_variants': {o.name: o['moved_vertices'] for o in info['hair_variants']},
    'outfit_variants': {o.name: {'moved_vertices': o.get('moved_vertices', 0), 'removed_faces': o.get('removed_faces', 0)} for o in info['outfit_variants']}})
print('EQUIP OK', info['radii'], len(info['armor']), len(tools))
