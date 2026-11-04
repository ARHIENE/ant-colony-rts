"""Report source materials and equipment anchors without changing the kit."""
import bpy, json
from pathlib import Path
root = Path('E:/Git/ant')
bpy.ops.wm.open_mainfile(filepath=str(root / 'art/production-kit-20261019/pk-motions.blend'))
materials = {}
for obj in bpy.data.objects:
    if not obj.name.startswith('Part_') or obj.type != 'MESH':
        continue
    for material in obj.data.materials:
        if material is None or material.name in materials:
            continue
        nodes = material.node_tree.nodes if material.use_nodes else []
        materials[material.name] = {'diffuse': list(material.diffuse_color), 'nodes': [
            {'name': n.name, 'type': n.type, 'image': n.image.name if n.type == 'TEX_IMAGE' and n.image else None,
             'inputs': {s.name: list(s.default_value) if hasattr(s.default_value, '__len__') and not isinstance(s.default_value, str) else s.default_value
                        for s in n.inputs if hasattr(s, 'default_value') and s.type in {'RGBA', 'VALUE'}},
             'links': [f'{l.from_node.name}.{l.from_socket.name}->{l.to_socket.name}' for s in n.inputs for l in s.links]}
            for n in nodes]}
report = {'materials': materials, 'parts': sorted(o.name for o in bpy.data.objects if o.name.startswith('Part_'))}
out = root / 'Temp/kemono-source-report.json'
out.write_text(json.dumps(report, indent=2), encoding='utf-8')
print('REPORT', out, len(materials))
