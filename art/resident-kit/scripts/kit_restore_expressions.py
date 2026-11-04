"""Restore the original four expression-row parts, without rebuilding equipment."""
import bpy, json
from mathutils import Matrix, Vector
import kit_common as K

def restore():
    rig = bpy.data.objects['ResidentRig']
    collection = bpy.data.collections['Parts']
    manifest_path = K.KIT / 'parts-manifest.json'
    manifest = json.loads(manifest_path.read_text(encoding='utf-8'))
    with bpy.data.libraries.load(K.SRC_PARTS) as (src, dst):
        dst.scenes = ['Appearance_Parts_Board']
    source = dst.scenes[0]
    for label in ('TEARY', 'SOBBING', 'NERVOUS', 'DEAD'):
        name = 'Part_Expr_' + label
        if name in bpy.data.objects:
            continue
        root = next(o for o in source.objects if o.type == 'EMPTY' and o.name.startswith('EXPR-') and o.name.endswith(' ' + label))
        offset = root.matrix_world.translation.copy()
        offset.z += root['display_z_offset']
        pieces = []
        for original in root.children_recursive:
            if original.type != 'MESH':
                continue
            obj = original.copy(); obj.data = original.data.copy()
            obj.animation_data_clear(); obj.parent = None
            obj.data.transform(Matrix.Translation(-offset) @ original.matrix_world)
            obj.matrix_world = Matrix.Identity(4)
            collection.objects.link(obj)
            K.skin(obj, rig, 'surface')
            pieces.append(obj)
        part = K.join(pieces)
        part.name = part.data.name = name
        part.hide_set(True); part.hide_render = True
        manifest['parts'][name] = {'category': 'Expr', 'source': 'appearance-parts.blend:' + root.name,
                                   'bind': 'surface', 'verts': len(part.data.vertices)}
    # Remove only the temporary source scene and its objects.
    source_objects = list(source.objects)
    bpy.data.scenes.remove(source)
    for obj in source_objects:
        bpy.data.objects.remove(obj, do_unlink=True)
    # The display-board face surface differs slightly from the assembled body.
    body = bpy.data.objects['Part_Body_01']
    for label in ('TEARY', 'SOBBING', 'NERVOUS', 'DEAD'):
        part = bpy.data.objects['Part_Expr_' + label]
        for vertex in part.data.vertices:
            hit, point, normal, index = body.ray_cast(Vector((vertex.co.x, -3, vertex.co.z)), Vector((0, 1, 0)))
            if hit:
                vertex.co.y = min(vertex.co.y, point.y - .015)
    manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    assert all('Part_Expr_' + label in bpy.data.objects for label in ('TEARY', 'SOBBING', 'NERVOUS', 'DEAD'))

if __name__ == '__main__':
    bpy.ops.wm.open_mainfile(filepath=str(K.KIT / 'resident-character-kit.blend'))
    restore()
    bpy.ops.wm.save_mainfile()
