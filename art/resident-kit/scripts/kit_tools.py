"""Build resident-tools.blend: hand tools + hand props, each one mesh in its socket's local frame.

Convention: object origin = socket head = grip point. Parent to the socket bone head with an identity
local transform and the tool sits in the hand at rest (handle along the socket bone, working end/
blade toward the character's front). No character, no facility.
"""
import bpy, sys, json, math, importlib
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K
importlib.reload(K)

# name: (target length, grip fraction from handle end, yaw about handle in degrees)
TOOLS = {
    'Tool_Hammer': (.62, .28, 90), 'Tool_Axe': (.74, .28, 0), 'Tool_Pickaxe': (.74, .28, 0),
    'Tool_Saw': (.56, .18, 90), 'Tool_Spoon': (.46, .30, 0), 'Tool_Pestle': (.40, .40, 0),
    'Tool_Scalpel': (.28, .35, 0), 'Tool_Stamp': (.30, .40, 0),
}
# Hand props carried by source residents: (source resident, mesh prefixes, socket)
PROPS = {
    'Prop_Spear': ('Resident_07', ('Staff', 'SpearTip'), 'Socket.Tool.R'),
    'Prop_Shield': ('Resident_08', ('RoundShield', 'ShieldRim', 'ShieldStripe'), 'Socket.Tool.L'),
    'Prop_Book': ('Resident_16', ('Book',), None),
    'Prop_Basket': ('Resident_14', ('Basket', 'Berry'), None),
    'Prop_WateringCan': ('Resident_15', ('WateringCan', 'Handle', 'Spout'), None),
    'Prop_Cloth': ('Resident_17', ('FoldedCloth',), None),
}

scene = K.empty_file()
scene.name = 'Resident_Tools'
col_tools = K.collection('Tools')
col_props = K.collection('Props')
rig = K.make_rig('_SocketReference', K.collection('_Ref'))
sock = {n: rig.data.bones[n].matrix_local.copy() for n in K.SOCKETS}


def place(objs, name, socket, world_of, collection):
    """Join meshes, bake world_of(piece) into the socket-local frame, origin = socket head."""
    pieces = []
    inv = sock[socket].inverted()
    for s in objs:
        o = s.copy(); o.data = s.data.copy(); o.parent = None; o.animation_data_clear()
        for m in list(o.modifiers): o.modifiers.remove(m)
        collection.objects.link(o)
        o.data.transform(inv @ world_of(s))
        o.matrix_world = Matrix.Identity(4)
        pieces.append(o)
    out = K.join(pieces)
    out.name = out.data.name = name
    out['socket'] = socket
    return out


with bpy.data.libraries.load(K.SRC_TOOLS) as (a, b):
    b.objects = list(a.objects)
for o in b.objects:
    if o: o['src_name'] = o.name; o.name = '_src_' + o.name
bpy.context.view_layer.update()
report = {}
for name, (length, grip, yaw) in TOOLS.items():
    root = next(o for o in b.objects if o and o.get('src_name') == name)
    meshes = [c for c in root.children_recursive if c.type == 'MESH']
    inv_root = root.matrix_world.inverted()
    pts = [inv_root @ c.matrix_world @ Vector(v) for c in meshes for v in c.bound_box]
    zlo, zhi = min(p.z for p in pts), max(p.z for p in pts)
    s = length / (zhi - zlo)
    # tool local: handle along +Z from zlo; grip moved to origin, yaw about the handle.
    local = Matrix.Rotation(math.radians(yaw), 4, 'Z') @ Matrix.Scale(s, 4) @ Matrix.Translation((0, 0, -(zlo + grip * (zhi - zlo))))
    # world frame at rest: +Z(tool) → socket bone direction, -Y(tool, blade side) → character front.
    m = sock['Socket.Tool.R']
    d = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    fwd = (Vector((0, -1, 0)) - d * d.dot(Vector((0, -1, 0)))).normalized()
    yw = -fwd; xw = yw.cross(d)
    frame = Matrix((xw, yw, d)).transposed().to_4x4()
    frame.translation = m.translation
    world_of = lambda piece, f=frame, l=local, ir=inv_root: f @ l @ ir @ piece.matrix_world
    tool = place(meshes, name, 'Socket.Tool.R', world_of, col_tools)
    tool['length'] = length; tool['grip_fraction'] = grip
    report[name] = {'socket': 'Socket.Tool.R (L also valid)', 'length': length, 'grip_fraction': grip}

# Props are kept exactly where the source resident held them, re-expressed in the nearest socket frame.
with bpy.data.libraries.load(K.SRC_RESIDENTS) as (a, b2):
    b2.objects = list(a.objects)
bpy.context.view_layer.update()
for name, (res, prefixes, socket) in PROPS.items():
    r = next(o for o in b2.objects if o and o.name == res)
    meshes = [c for c in r.children if c.type == 'MESH' and c.name.split('.')[0] in prefixes]
    off = Matrix.Translation(-r.matrix_world.translation)
    c = sum((m.matrix_world.translation for m in meshes), Vector()) / len(meshes) - r.matrix_world.translation
    socket = socket or ('Socket.Tool.L' if c.x < 0 else 'Socket.Tool.R')
    if name in ('Prop_Basket', 'Prop_Book', 'Prop_Cloth') and abs(c.x) < .3:
        socket = 'Socket.Carry'
    prop = place(meshes, name, socket, lambda piece, o=off: o @ piece.matrix_world, col_props)
    if name == 'Prop_WateringCan':
        from kit_watering_can import fit_can
        fit_can(prop, rig)
    report[name] = {'socket': socket, 'source': res}

from tool_shapes import repair_tools
repair_tools()

for o in [o for o in bpy.data.objects if not o.users_collection]:
    bpy.data.objects.remove(o)
bpy.data.objects.remove(rig)
bpy.data.collections.remove(bpy.data.collections['_Ref'])
bpy.ops.outliner.orphans_purge(do_recursive=True)
# Spread out for viewing only (export resets the location).
for i, o in enumerate(list(col_tools.objects) + list(col_props.objects)):
    o.location = (i * .8, 0, 0)
bpy.ops.wm.save_as_mainfile(filepath=str(K.KIT / 'resident-tools.blend'))

out = K.KIT / 'exports' / 'tools'
out.mkdir(parents=True, exist_ok=True)
vl = scene.view_layers[0]
for o in list(col_tools.objects) + list(col_props.objects):
    keep = o.location.copy(); o.location = (0, 0, 0)
    for x in scene.objects: x.select_set(x == o, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=o, selected_objects=[o]):
        bpy.ops.export_scene.fbx(filepath=str(out / (o.name + '.fbx')), use_selection=True, object_types={'MESH'},
                                 axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
    o.location = keep
(K.KIT / 'tools-manifest.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print(json.dumps(report))
