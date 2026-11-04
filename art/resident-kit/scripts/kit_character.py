"""Build resident-character-kit.blend: common rig + swappable parts + manual combos. No facilities.

Run inside Blender (MCP exec). Starts from an empty file; source .blend files are only read.
"""
import bpy, sys, json, importlib, collections
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
sys.path.insert(0, r"E:\Git\ant\art\characters\toto-v1")
import kit_common as K
importlib.reload(K)
import forms

scene = K.empty_file()
scene.name = 'Resident_Character_Kit'
col_rig = K.collection('Rig')
col_parts = K.collection('Parts')
col_combo = K.collection('Combos')
col_src = K.collection('_Source')

# ---- load sources (read only) ----
with bpy.data.libraries.load(K.SRC_RESIDENTS) as (a, b):
    b.objects = list(a.objects)
roots = sorted([o for o in b.objects if o and o.name.startswith('Resident_') and o.parent is None], key=lambda o: o.name)
keep = set()
for r in roots:
    keep |= {r, *r.children_recursive}
for o in b.objects:
    if o in keep: col_src.objects.link(o)
    elif o: bpy.data.objects.remove(o)
with bpy.data.libraries.load(K.SRC_PARTS) as (a, b):
    b.scenes = ['Expressions_Extended', 'Hair_Scars_Injuries']
board_scenes = list(b.scenes)
bpy.context.view_layer.update()

rig = K.make_rig('ResidentRig', col_rig)

# Name prefix → (category, bind)
GROUPS = [
    ('Body', ('V4_Body', 'V4_Paw', 'V4_Foot'), None),
    ('Tail', ('Tail',), 'Tail'),
    ('Ears', ('EarFlop', 'EarRound', 'EarPoint', 'EarInner'), 'side:Ear'),
    ('TopCrown', ('CrownTuft',), 'Head'),
    ('TopCheek', ('CheekTuft',), 'Head'),
    ('TopLeaf', ('Leaf', 'Stem'), 'Head'),
    ('TopFlower', ('Petal', 'FlowerCenter'), 'Head'),
    ('Mark', ('FaceBlaze',), 'surface'),
    ('Face', ('V4_Eye', 'V4_Mouth', 'V4_Blush', 'HappyEye', 'OpenSmile', 'Tongue', 'ConfidentBrow'), 'surface'),
    ('Glasses', ('Glasses',), 'Head'),
    ('OutfitBag', ('Satchel', 'V4_Strap'), 'surface'),
    ('OutfitCape', ('ShortCape',), 'surface'),
    ('OutfitCollar', ('Collar',), 'surface'),
]
PROPS = ('Staff', 'SpearTip', 'RoundShield', 'ShieldRim', 'ShieldStripe', 'Book', 'Basket', 'Berry', 'WateringCan', 'Handle', 'Spout', 'FoldedCloth')
BODY_BIND = {'V4_Body': 'surface', 'V4_Paw': 'side:Hand', 'V4_Foot': 'side:Foot'}

def base(o):
    return o.name.split('.')[0]

def build_part(name, sources, offset, bind_of):
    """Copy sources into kit space (minus offset), skin each piece, join into one mesh."""
    pieces = []
    for s in sources:
        o = s.copy(); o.data = s.data.copy(); o.animation_data_clear()
        o.parent = None; o.constraints.clear()
        for m in list(o.modifiers): o.modifiers.remove(m)
        col_parts.objects.link(o)
        o.data.transform(Matrix.Translation(-offset) @ s.matrix_world)
        o.matrix_world = Matrix.Identity(4)
        K.skin(o, rig, bind_of(s))
        pieces.append(o)
    part = K.join(pieces)
    part.name = part.data.name = name
    part.parent = rig; part.matrix_parent_inverse = Matrix.Identity(4)
    return part

def signature(objs, offset):
    out = []
    for o in objs:
        pts = [o.matrix_world @ Vector(c) - offset for c in o.bound_box]
        out.append((base(o), len(o.data.vertices), tuple(round(min(p[i] for p in pts), 2) for i in range(3)) + tuple(round(max(p[i] for p in pts), 2) for i in range(3)), tuple(m.name.split('.')[0] for m in o.data.materials)))
    return tuple(sorted(out))

manifest = {'parts': {}, 'fur_materials': [], 'props_excluded': list(PROPS)}
seen = {}
fur = []
palette_inner = []
for r in roots:
    off = r.matrix_world.translation.copy()
    kids = [c for c in r.children if c.type == 'MESH']
    inner = next((m for c in kids for m in c.data.materials if m.name.startswith('Inner')), None)
    if inner and inner not in palette_inner: palette_inner.append(inner)
    for cat, prefixes, bind in GROUPS:
        objs = [c for c in kids if base(c) in prefixes]
        if not objs: continue
        if cat == 'Body':
            m = objs[0].data.materials[0]
            if m not in fur: fur.append(m)
            if 'Body' in seen: continue
        sig = signature(objs, off) if cat != 'Body' else 'body'
        # Fur-coloured parts differ only by fur colour: same shape = same part.
        sig = tuple((n, v, b, tuple(x[:3] if x.startswith(('Fur', 'Inn')) else x for x in mats)) for n, v, b, mats in sig) if cat != 'Body' else sig
        if (cat, sig) in seen: continue
        idx = sum(1 for k in seen if k[0] == cat) + 1
        name = f'Part_{cat}_{idx:02d}'
        bind_of = (lambda s: BODY_BIND[base(s)]) if cat == 'Body' else (lambda s, b=bind: b)
        part = build_part(name, objs, off, bind_of)
        seen[(cat, sig)] = name
        manifest['parts'][name] = {'category': cat, 'source': r.name, 'bind': bind or 'Body/Hand/Foot', 'verts': len(part.data.vertices)}

# Soft forearms belong to the body part (resident-anim/reach.py made them only in previews).
body = bpy.data.objects['Part_Body_01']
bmat = body.data.materials[0]
for side in 'LR':
    a = Vector(K.BONES['Arm.' + side][0]); h = Vector(K.BONES['Hand.' + side][0])
    o = forms.tube('SoftArm.' + side, [a.lerp(h, t / 4) for t in range(5)], .085, bmat, None, 12)
    for c in list(o.users_collection): c.objects.unlink(o)
    col_parts.objects.link(o)
    up = o.vertex_groups.new(name='Arm.' + side); hand = o.vertex_groups.new(name='Hand.' + side)
    for v in o.data.vertices:
        t = (v.index // 12) / 4
        if t < 1: up.add([v.index], 1 - t, 'REPLACE')
        if t > 0: hand.add([v.index], t, 'REPLACE')
    K.join([body, o])
mod = [m for m in body.modifiers if m.type == 'ARMATURE']
for m in mod[1:]: body.modifiers.remove(m)

# ---- parts board: expressions, overlays, hair, scars/scratches/bruises/bandages ----
BOARD = {'EXPR': ('Expr', 'surface'), 'OVERLAY': ('Overlay', 'surface'), 'HAIR': ('Hair', 'Head'),
         'SCAR': ('Injury', None), 'SCRATCH': ('Injury', None), 'BRUISE': ('Injury', None), 'BANDAGE': ('Injury', None)}
def injury_bind(label):
    region = label.split('-', 1)[1]
    return {'ARM-L': 'Hand.L', 'ARM-R': 'Hand.R', 'LEG': 'side:Foot'}.get(region, 'surface')
for s in board_scenes:
    for e in [o for o in s.objects if o.parent is None and o.type == 'EMPTY' and '.' not in o.name and o.name.split('-')[0] in BOARD]:
        pb = s.objects.get('PreviewBody_' + e.name) or s.objects.get('DisplayBody_' + e.name)
        if not pb: continue
        pts = [pb.matrix_world @ Vector(c) for c in pb.bound_box]
        lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
        off = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z - .09))
        cat, bind = BOARD[e.name.split('-')[0]]
        bind = bind or injury_bind(e.name)
        objs = [c for c in e.children_recursive if c.type == 'MESH']
        label = e.name if cat == 'Injury' else e.name.split('-', 1)[1]
        name = 'Part_' + cat + '_' + label.replace(' ', '_').replace('-', '_')
        part = build_part(name, objs, off, lambda o, b=bind: b)
        manifest['parts'][name] = {'category': cat, 'source': 'appearance-parts.blend:' + e.name, 'bind': bind, 'verts': len(part.data.vertices)}

# Remove sources from this file.
for o in list(col_src.objects): bpy.data.objects.remove(o)
bpy.data.collections.remove(col_src)
for s in board_scenes: bpy.data.scenes.remove(s)
for m in fur + palette_inner: m.use_fake_user = True
manifest['fur_materials'] = [m.name for m in fur]
manifest['inner_materials'] = [m.name for m in palette_inner]
for o in col_parts.objects: o.hide_set(True); o.hide_render = True
print('parts', len(manifest['parts']), 'fur', len(fur))
print(json.dumps({k: v['category'] for k, v in manifest['parts'].items()}, ensure_ascii=False))
(K.KIT / 'parts-manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
for o in [o for o in bpy.data.objects if not o.users_collection]:
    bpy.data.objects.remove(o)
bpy.ops.outliner.orphans_purge(do_recursive=True)
