"""Real-speed clip previews with the equipment actually attached → previews/frames/<view>/<clip>/f####.png
then `uv run --with pillow python scripts/gifs.py` → previews/gif/<view>/<clip>.gif (24 fps).

blender -b --factory-startup --python scripts/render_motion.py -- [close|game] [clip ...]
close = 3/4 front framing the character; game = high RTS camera at play distance (character ≈ 90 px tall).
Props follow the clip's event frames the way the engine will swap them (hip ↔ hand, hand → floor, floor → head):
nothing is faked beyond parent/visibility swaps. Props not modelled in the library (peel, ladle) are left out and listed.
"""
import bpy, sys, importlib, json, math
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, review, dress, render_common as RC
for m in (pk, review, dress, RC): importlib.reload(m)
from mathutils import Vector, Matrix

args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
view = args[0] if args else 'close'; only = set(args[1:])
rig = review.setup(str(pk.KIT / 'pk-motions.blend')); rig.data.pose_position = 'POSE'
worn = review.dress_one(rig, hair='BOB', ears='RABBIT', tail='FOX', tier='Steel')
review.hide_variants()
carried = review.carry(rig)  # sheathed sword + spear + shield at their holders
for o in worn + carried: o.hide_render = False  # copies inherit the library originals' render-hidden flag
col = pk.collection('Review'); E = bpy.data.objects
for src in [o for o in bpy.data.collections['Equipment'].objects if o.name.startswith('Holder_')]:  # straps / frog / back pad
    dress.wear(rig, src, 'Leather', col, 'A').hide_render = False
with bpy.data.libraries.load(str(pk.KIT / 'pk-resources.blend'), link=False) as (a, b):
    b.objects = [n for n in ('Drink_Coffee', 'Fossil_NaturalGas', 'Proc_WheatFlour', 'Fossil_CrudeOil') if n in a.objects]
for o in b.objects: col.objects.link(o); o.hide_render = True
with bpy.data.libraries.load(str(pk.KIT / 'pk-handheld.blend'), link=False) as (a, b):  # 2026-10-24 단궁·화살
    b.objects = [n for n in ('Weapon_ShortBow', 'Ammo_Arrow') if n in a.objects]
for o in b.objects: col.objects.link(o); o.hide_render = True
cam = RC.setup(360 if view == 'close' else 300, transparent=False)
sc = bpy.context.scene; sc.render.fps = 24
report = json.loads((pk.KIT / 'previews' / 'motion-report.json').read_text(encoding='utf-8'))['clips']
TOOLS = {'Tool_Axe', 'Tool_Pickaxe', 'Tool_Hammer', 'Tool_Shovel', 'Tool_Hoe', 'Tool_Sickle'}
MISSING = {'peel': '화덕 삽(시설 소품, 미제작)', 'ladle': '국자(시설 소품, 미제작)', 'cloth': '불 끄는 천(미제작)', 'food': '식량(엔진 소지품 표시)',
           'bandage': '붕대(미제작)', 'surgery_kit': '수술 도구(미제작)'}


def hand_prop(src, socket='Socket.Tool.R'):
    o = dress.hold(rig, src, socket, col, 'clip'); o.hide_render = False; return o


def floor_copy(src, at):
    o = src.copy(); col.objects.link(o); o.parent = None; o.matrix_world = Matrix.Translation(at); o.hide_render = True; return o


def setup_props(name, c):
    """→ list of (object, visible(frame)) for this clip."""
    ev = c.get('events', {}); prop = c.get('prop') or ''; out = []
    sheathed = [o for o in carried if o.name.startswith('Weapon_Sword')]
    for o in carried: o.hide_render = False
    if prop in TOOLS:
        t = hand_prop(E[prop + '_Steel']); out.append((t, lambda f: True))
    elif prop == 'Weapon_Sword':
        s = hand_prop(E['Weapon_Sword_Steel'])
        g = ev.get('weapon_grab'); r = ev.get('weapon_release'); d = ev.get('weapon_drop')
        if g: vis = lambda f, g=g: f >= g
        elif r: vis = lambda f, r=r: f < r
        elif d: vis = lambda f, d=d: f < d
        else: vis = lambda f: True
        out.append((s, vis))
        for o in sheathed: out.append((o, (lambda f: False) if d else (lambda f, v=vis: not v(f))))  # scabbard keeps no 2nd sword
        if d:  # weapon leaves the hand at the drop event and lies on the floor where the hand was
            sc.frame_set(d); bpy.context.view_layer.update()
            p = rig.matrix_world @ rig.pose.bones['Socket.Tool.R'].head
            fl = E['Weapon_Sword_Steel'].copy(); col.objects.link(fl); fl.parent = None
            fl.matrix_world = Matrix.Translation((p.x, p.y - .05, .012)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Rotation(math.pi / 2, 4, 'Z')
            out.append((fl, lambda f, d=d: f >= d))
    elif prop == 'Weapon_Spear':
        s = hand_prop(E['Weapon_Spear_Steel']); out.append((s, lambda f: True))
        out += [(o, lambda f: False) for o in carried if o.name.startswith('Weapon_Spear')]
    elif prop == 'Weapon_Shield':
        s = hand_prop(E['Weapon_Shield_Steel'], 'Socket.Tool.L'); out.append((s, lambda f: True))
        out += [(o, lambda f: False) for o in carried if o.name.startswith('Weapon_Shield')]
    elif prop == 'tool':
        t = hand_prop(E['Tool_Hammer_Steel']); sh, hi = ev.get('tool_show'), ev.get('tool_hide')
        out.append((t, (lambda f, s=sh: f >= s) if sh else (lambda f, h=hi: f < h)))
    elif prop == 'cup':
        cp = hand_prop(E['Drink_Coffee']); cp.location = (0, -rig.data.bones['Socket.Tool.R'].length, -.03); out.append((cp, lambda f: True))
    elif prop == 'Weapon_ShortBow':
        out.append((hand_prop(E['Weapon_ShortBow'], 'Socket.Tool.L'), lambda f: True))
        ar = hand_prop(E['Ammo_Arrow']); n, r = ev.get('nock', 0), ev.get('release', 999)
        out.append((ar, lambda f, n=n, r=r: n <= f < r))
    elif prop in ('canister', 'sack'):
        src = E['Fossil_NaturalGas' if prop == 'canister' else 'Proc_WheatFlour']; h = hand_prop(src)
        put = ev.get('place') or ev.get('pour_end') or 999
        out.append((h, lambda f, p=put: f < p))
    return out


views = {'close': (Vector((.55, -1.0, .45)).normalized(), 2.6), 'game': (Vector((.45, -.75, 1.0)).normalized(), 9.0)}
vdir, dist = views[view]
for name, c in report.items():
    if only and name not in only: continue
    act = bpy.data.actions.get('PK|' + name)
    if not act: continue
    for o in [o for o in col.objects if o.name.endswith('__clip') or o.name.startswith('Weapon_Sword_Steel.')]: bpy.data.objects.remove(o)
    rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
    props = setup_props(name, c)
    cam.location = Vector((0, -.05, .55)) + vdir * dist; cam.rotation_euler = (-vdir).to_track_quat('-Z', 'Y').to_euler(); cam.data.lens = 50 if view == 'close' else 85
    d = pk.KIT / 'previews' / 'frames' / view / name; d.mkdir(parents=True, exist_ok=True)
    for f in range(1, c['frames'] + 1):
        sc.frame_set(f)
        for o, vis in props: o.hide_render = not vis(f)
        RC.shot(d / f'f{f:04d}.png')
    print('CLIP', view, name, c['frames'])
pk.write_json(pk.KIT / 'previews' / f'motion-render-{view}.json', {'missing_props': MISSING})
