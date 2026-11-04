"""Check the exported character's rigid armour and material/part inventory after FBX reimport."""
import bpy, json
from pathlib import Path
root = Path('E:/Git/ant')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root / 'Assets/Resources/Kemono/Kemono_Character.fbx'))
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
parts = [o for o in bpy.data.objects if o.type == 'MESH' and o.name.startswith('Part_')]
plates = [o for o in parts if o.name.startswith('Part_Equipment_Armor_')]
assert len(plates) >= 10, len(plates)
assert all(n in [o.name for o in parts] for n in ['Part_Body_BASE_ARMOR', 'Part_Outfit_LINEN_ARMOR', 'Part_Hair_LONG_ARMOR'])
assert all('Part_Face_' + expression + '_00' in [o.name for o in parts] for expression in ['NEUTRAL', 'SLEEP', 'DEAD', 'PAIN'])
for plate in plates:
    assert any(m.type == 'ARMATURE' and m.object == rig for m in plate.modifiers), plate.name
    groups = {g.group for v in plate.data.vertices for g in v.groups if g.weight > 0}
    assert len(groups) == 1, (plate.name, groups)
    assert all(abs(sum(g.weight for g in v.groups) - 1) < .0001 for v in plate.data.vertices), plate.name
    assert plate.vertex_groups[next(iter(groups))].name in rig.data.bones
report = {'parts': len(parts), 'rigid_plates': len(plates), 'bones': len(rig.data.bones),
          'plate_names': [o.name for o in plates], 'result': 'PASS; Unity Play not run'}
(root / 'Temp/graphics-1024/character-reimport.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('PASS', len(parts), 'parts', len(plates), 'single-bone rigid plates', len(rig.data.bones), 'bones')
