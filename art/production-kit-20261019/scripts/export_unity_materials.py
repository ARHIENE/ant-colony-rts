"""Export the existing Blender cloth colour graph to pixels and source material colours."""
import bpy, json
import numpy as np
from pathlib import Path
root = Path('E:/Git/ant')
bpy.ops.wm.open_mainfile(filepath=str(root / 'art/production-kit-20261019/pk-motions.blend'))
out = root / 'Assets/Resources/Kemono/Textures'
out.mkdir(parents=True, exist_ok=True)
materials = []
for material in bpy.data.materials:
    if not material.use_nodes:
        continue
    shader = next((n for n in material.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None)
    if shader is not None:
        materials.append({'name': material.name, 'color': list(shader.inputs['Base Color'].default_value),
                          'linked': bool(shader.inputs['Base Color'].links)})
cloth = bpy.data.materials['Settler_RecoloredSourceCloth.001']
nodes = cloth.node_tree.nodes
# Exact source graph: red minus green > threshold selects a constant cloth colour.
assert nodes['Math'].operation == 'SUBTRACT'
assert nodes['Math.001'].operation == 'GREATER_THAN'
assert nodes['Mix (Legacy)'].blend_type == 'MIX'
source = nodes['Image Texture'].image
pixels = np.empty(len(source.pixels), dtype=np.float32)
source.pixels.foreach_get(pixels)
pixels = pixels.reshape((-1, 4))
rgb = pixels[:, :3]
linear = np.where(rgb <= .04045, rgb / 12.92, ((rgb + .055) / 1.055) ** 2.4)
mask = linear[:, 0] - linear[:, 1] > nodes['Math.001'].inputs[1].default_value
color = nodes['Mix (Legacy)'].inputs[2].default_value[:3]
color = np.array(color, dtype=np.float32)
color = np.where(color <= .0031308, color * 12.92, 1.055 * color ** (1 / 2.4) - .055)
assert mask.any(), 'No recoloured cloth pixels'
pixels[mask, :3] = color
image = bpy.data.images.new('Linen_Baked', width=source.size[0], height=source.size[1], alpha=True)
image.pixels.foreach_set(pixels.ravel())
image.filepath_raw = str(out / 'Linen_Baked.png')
image.file_format = 'PNG'
image.save()
assert float(pixels[:, :3].max()) > .1
assert np.allclose(pixels[mask, :3], color)
(out / 'SourceMaterials.json').write_text(json.dumps({'materials': materials}, indent=2), encoding='utf-8')
print('PASS cloth graph pixels', int(mask.sum()), 'of', len(pixels), 'size', image.size[:], 'source colours', len(materials))
