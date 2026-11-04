"""Spec equipment that had no model (장수·유닛 '장비 목록'):
hand: 산 분사기 Equip_AcidSprayer, 지휘 신호기 Equip_Signaller  → resident-tools.blend (socket-local, origin = grip)
worn: 보호 갑옷 Equip_Armor, 비행 장비 Equip_FlightGear, 장신구 4종(발목 띠·감각 보조·향기 주머니·지휘 표식)
      → resident-character-kit.blend as skinned parts (category Equip / Trinket), then FBX re-export.
Melee weapon = Prop_Spear, shield = Prop_Shield (already in tools)."""
import bpy, bmesh, sys, json, math, importlib
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
sys.path.insert(0, r"E:\Git\ant\art\characters\toto-v1")
import kit_common as K, forms as F
importlib.reload(K)


def mat(name, rgb, rough=.8, alpha=1.0, emit=0.0):
    m = bpy.data.materials.get('Equip_' + name) or F.material('Equip_' + name, rgb)
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Roughness'].default_value = rough
    if alpha < 1: p.inputs['Alpha'].default_value = alpha
    if emit: p.inputs['Emission Color'].default_value = (*rgb, 1); p.inputs['Emission Strength'].default_value = emit
    return m


def oval(name, loc, scale, m, col, seg=20, rings=10):
    bm = bmesh.new(); bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=rings, radius=1)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(loc) @ Matrix.Diagonal((*scale, 1)), verts=bm.verts)
    d = bpy.data.meshes.new(name); bm.to_mesh(d); bm.free(); d.materials.append(m)
    for p in d.polygons: p.use_smooth = True
    o = bpy.data.objects.new(name, d); col.objects.link(o); return o


def tube(name, pts, r, m, col, sides=10):
    o = F.tube(name, [Vector(p) for p in pts], r, m, None, sides)
    for c in list(o.users_collection): c.objects.unlink(o)
    col.objects.link(o); return o


def merged(name, objs):
    out = K.join(objs); out.name = out.data.name = name; return out


# ---------------- worn parts (character kit) ----------------
bpy.ops.wm.open_mainfile(filepath=str(K.KIT / 'resident-character-kit.blend'))
rig = bpy.data.objects['ResidentRig']
col = bpy.data.collections['Parts']
for o in [o for o in col.objects if o.name.startswith(('Part_Equip_', 'Part_Trinket_'))]:
    bpy.data.objects.remove(o)
bark = mat('Bark', (.52, .36, .22)); leaf = mat('Leaf', (.42, .62, .3)); strap = mat('Strap', (.4, .27, .18))
wingm = mat('Wing', (.86, .95, 1), .2, .6); gold = mat('Gold', (.95, .76, .3), .35); cloth = mat('Pouch', (.82, .55, .62))
lens = mat('Lens', (.55, .85, .95), .15); bead = mat('Bead', (.45, .7, .9), .3)
parts = {}
# 보호 갑옷: bark breastplate + leaf shoulder pads (front of the round body, above the belly).
parts['Part_Equip_Armor'] = (merged('Part_Equip_Armor', [
    oval('A1', (0, -.43, .64), (.56, .13, .3), bark, col, 24, 10),
    oval('A2', (-.62, -.18, .86), (.24, .26, .1), leaf, col, 16, 8),
    oval('A3', (.62, -.18, .86), (.24, .26, .1), leaf, col, 16, 8),
    tube('A4', [(-.7, -.1, .8), (0, .62, .78), (.7, -.1, .8)], .04, strap, col)]), 'surface')
# 비행 장비: backpack + two leaf wings on the back (rigid on Body).
parts['Part_Equip_FlightGear'] = (merged('Part_Equip_FlightGear', [
    oval('F1', (0, .62, .7), (.32, .16, .3), bark, col, 16, 8),
    oval('F2', (-.62, .78, 1.0), (.56, .06, .24), wingm, col, 20, 8),
    oval('F3', (.62, .78, 1.0), (.56, .06, .24), wingm, col, 20, 8),
    tube('F4', [(-.62, -.3, .9), (0, .7, .95), (.62, -.3, .9)], .035, strap, col)]), 'Body')
# 장신구: 가벼운 발목 띠 / 감각 보조 장신구(렌즈 고글) / 향기 주머니 / 지휘 표식(가슴 배지)
ring = []
for i in range(14):
    a = math.tau * i / 14; ring.append((.27 + .17 * math.cos(a), -.08 + .17 * math.sin(a), .12))
parts['Part_Trinket_Anklet'] = (merged('Part_Trinket_Anklet', [tube('T1', ring + [ring[0]], .03, bead, col, 8)]), 'Foot.R')
parts['Part_Trinket_SensorGoggles'] = (merged('Part_Trinket_SensorGoggles', [
    oval('G1', (-.22, -.6, 1.62), (.13, .05, .1), lens, col, 16, 8), oval('G2', (.22, -.6, 1.62), (.13, .05, .1), lens, col, 16, 8),
    tube('G3', [(-.86, -.05, 1.6), (-.35, -.58, 1.62), (.35, -.58, 1.62), (.86, -.05, 1.6)], .025, strap, col)]), 'Head')
parts['Part_Trinket_ScentPouch'] = (merged('Part_Trinket_ScentPouch', [
    oval('S1', (-.62, -.28, .48), (.12, .1, .15), cloth, col, 14, 8), oval('S2', (-.62, -.28, .64), (.06, .06, .04), gold, col, 10, 6)]), 'surface')
parts['Part_Trinket_Insignia'] = (merged('Part_Trinket_Insignia', [
    oval('I1', (.3, -.6, .82), (.1, .03, .1), gold, col, 16, 6), oval('I2', (.3, -.63, .82), (.05, .02, .05), mat('Red', (.8, .25, .2)), col, 10, 6)]), 'surface')
for name, (o, bind) in parts.items():
    K.skin(o, rig, bind)
    o.hide_viewport = True; o.hide_render = True
    o['category'] = 'Equip' if 'Equip' in name else 'Trinket'
man_p = K.KIT / 'parts-manifest.json'
man = json.loads(man_p.read_text(encoding='utf-8'))
for name, (o, bind) in parts.items():
    man['parts'][name] = {'category': o['category'], 'source': 'kit_equipment.py', 'bind': bind, 'verts': len(o.data.vertices)}
man_p.write_text(json.dumps(man, ensure_ascii=False, indent=2), encoding='utf-8')
for o in [o for o in bpy.data.objects if not o.users_collection]: bpy.data.objects.remove(o)
bpy.ops.wm.save_mainfile()

# ---------------- hand equipment (tools kit) ----------------
bpy.ops.wm.open_mainfile(filepath=str(K.KIT / 'resident-tools.blend'))
tcol = bpy.data.collections['Tools']
for o in [o for o in tcol.objects if o.name.startswith('Equip_')]: bpy.data.objects.remove(o)
ref = K.make_rig('_SocketReference', K.collection('_Ref'))
sock = ref.data.bones['Socket.Tool.R'].matrix_local.copy()
d = (sock.to_3x3() @ Vector((0, 1, 0))).normalized()
fwd = (Vector((0, -1, 0)) - d * d.dot(Vector((0, -1, 0)))).normalized(); yw = -fwd; xw = yw.cross(d)
frame = Matrix((xw, yw, d)).transposed().to_4x4(); frame.translation = sock.translation
inv = sock.inverted()
tank = mat('AcidTank', (.6, .82, .3), .3, .8); acid = mat('Acid', (.55, .95, .25), .2, 1, 1.5); metal = mat('Metal', (.55, .58, .62), .35)
flag = mat('Flag', (.85, .3, .25)); pole = mat('Pole', (.55, .38, .22))
gold = mat('Gold', (.95, .76, .3), .35)


def hand_item(name, pieces):
    o = merged(name, pieces)
    o.data.transform(inv @ frame)       # authored in tool space (grip at origin, +Z handle, -Y front)
    o['socket'] = 'Socket.Tool.R'
    for c in list(o.users_collection): c.objects.unlink(o)
    tcol.objects.link(o); return o


# 산 분사기: grip + acid bottle + forward nozzle (sprays toward -Y)
hand_item('Equip_AcidSprayer', [
    tube('a1', [(0, 0, -.12), (0, 0, .12)], .05, pole, tcol),
    oval('a2', (0, .02, .26), (.13, .13, .17), tank, tcol, 16, 8),
    oval('a3', (0, .02, .3), (.08, .08, .1), acid, tcol, 12, 6),
    tube('a4', [(0, -.05, .2), (0, -.32, .26)], .035, metal, tcol),
    oval('a5', (0, -.35, .27), (.05, .05, .05), metal, tcol, 10, 6)])
# 지휘 신호기: short pole with a pennant and a bell
hand_item('Equip_Signaller', [
    tube('s1', [(0, 0, -.15), (0, 0, .62)], .025, pole, tcol),
    F.mesh('s2', [(0, 0, .62), (0, -.32, .52), (0, 0, .42)], [(0, 1, 2)], flag, None),
    oval('s3', (0, -.04, .66), (.05, .05, .06), gold, tcol, 10, 6)])
for o in [o for o in bpy.data.objects if not o.users_collection]: bpy.data.objects.remove(o)
for o in list(bpy.data.objects):
    if o.name in ('s2',): bpy.data.objects.remove(o)
bpy.data.objects.remove(ref); bpy.data.collections.remove(bpy.data.collections['_Ref'])
for i, o in enumerate(tcol.objects): o.location = (i * .8, 0, 0)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_mainfile()
out = K.KIT / 'exports' / 'tools'; vl = bpy.context.scene.view_layers[0]
for o in [o for o in tcol.objects if o.name.startswith('Equip_')]:
    keep = o.location.copy(); o.location = (0, 0, 0)
    for x in bpy.context.scene.objects: x.select_set(x == o, view_layer=vl)
    with bpy.context.temp_override(view_layer=vl, active_object=o, selected_objects=[o]):
        bpy.ops.export_scene.fbx(filepath=str(out / (o.name + '.fbx')), use_selection=True, object_types={'MESH'},
                                 axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS')
    o.location = keep
tm = json.loads((K.KIT / 'tools-manifest.json').read_text())
tm['Equip_AcidSprayer'] = {'socket': 'Socket.Tool.R', 'spec': '산 분사기(원거리 무기)'}
tm['Equip_Signaller'] = {'socket': 'Socket.Tool.R', 'spec': '지휘 신호기(지원 무기)'}
(K.KIT / 'tools-manifest.json').write_text(json.dumps(tm, indent=2, ensure_ascii=False), encoding='utf-8')
print('equipment parts', list(parts), 'hand', ['Equip_AcidSprayer', 'Equip_Signaller'])
