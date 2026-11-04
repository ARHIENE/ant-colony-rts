"""colony-core.blend을 열어 둔 상태에서 실행: 작업 지점 Empty + 경첩 클립 생성 → colony-interaction.blend로 따로 저장.
원본 colony-core.blend는 덮어쓰지 않음. 결과: interaction_points.json, exports-interaction/<id>.fbx(경첩 있는 6종)."""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector
BASE = Path('E:/Git/ant/art/colony-core')
sys.path.insert(0, str(BASE / 'scripts'))
import importlib, interaction_spec as S
importlib.reload(S)

FPS, LOOP = 24, 48
# 작업 자리 예외(루트 로컬 좌표). 기본은 앞(-Y) 가장자리에서 0.5 떨어진 중앙
WP_OVERRIDE = {'CC_TreeBroadleaf': (0, -.75), 'CC_TreeTall': (0, -.75), 'CC_Watchtower': (0, -1.5),
               'CC_Campfire': (0, -1.25), 'CC_Shower': (0, 0), 'CC_DoorFrame': (0, -.9), 'CC_CastleGate': (0, -1.4), 'CC_RanchGate': (0, -.9)}


def local_bounds(root):
    inv = root.matrix_world.inverted()
    pts = [inv @ (o.matrix_world @ Vector(c)) for o in root.children_recursive if o.type == 'MESH' for c in o.bound_box]
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


def add_point(root, name, loc, face=(0, 1, 0)):
    e = bpy.data.objects.new(name, None); e.empty_display_type = 'SINGLE_ARROW'; e.empty_display_size = .4
    root.users_collection[0].objects.link(e); e.parent = root
    e.location = loc; e.rotation_euler = Vector(face).to_track_quat('Z', 'Y').to_euler()  # 화살표(+Z) = 바라볼 방향
    return e


def build_points():
    out = {}
    for rid, (clip, tool, extras, note) in S.SPEC.items():
        root = bpy.data.objects[rid]
        for c in [c for c in root.children if c.name.split('.')[0] in ('WorkPoint_0', 'InputPoint', 'OutputPoint', 'SeatPoint', 'PatientPoint', 'CarryAnchor')]:
            bpy.data.objects.remove(c)
        lo, hi = local_bounds(root); cx = (lo.x + hi.x) / 2; cy = (lo.y + hi.y) / 2; h = hi.z
        pts = {}
        if clip and clip != 'Carry':
            x, y = WP_OVERRIDE.get(rid, (cx, lo.y - .5))
            pts['WorkPoint_0'] = add_point(root, 'WorkPoint_0', (x, y, 0))
        for k in extras:
            if k == 'Input': pts['InputPoint'] = add_point(root, 'InputPoint', (lo.x + (hi.x - lo.x) * .3, lo.y + .15, h * .6))
            if k == 'Output': pts['OutputPoint'] = add_point(root, 'OutputPoint', (hi.x - (hi.x - lo.x) * .3, lo.y + .15, h * .6))
            if k == 'Seat': pts['SeatPoint'] = add_point(root, 'SeatPoint', (cx, cy, h if h < .5 else h * .45))
            if k == 'Patient': pts['PatientPoint'] = add_point(root, 'PatientPoint', (cx, cy, h * .55))
            if k == 'Carry': pts['CarryAnchor'] = add_point(root, 'CarryAnchor', (cx, cy, h), (0, 0, 1))
        out[rid] = {'clip': clip, 'tool': tool, 'note': note,
                    'points': {n: {'pos': [round(v, 3) for v in e.location], 'forward': [round(v, 3) for v in (e.matrix_basis.to_3x3() @ Vector((0, 0, 1)))]} for n, e in pts.items()},
                    'hinges': [{'pivot': p, 'axis': 'XYZ'[a], 'open_deg': d} for p, a, d in S.HINGES.get(rid, [])]}
    return out


def build_hinges():
    for rid, hinges in S.HINGES.items():
        for pivot, axis, deg in hinges:
            o = bpy.data.objects[pivot]; o.rotation_mode = 'XYZ'
            o.animation_data_clear(); o.animation_data_create()
            act = bpy.data.actions.new(f'{rid}|{"Aim" if rid == "CC_AcidTower" else "OpenClose"}')
            o.animation_data.action = act
            base = o.rotation_euler.copy()
            # 개폐: 0.5초 열림 → 유지 → 0.5초 닫힘. 포탑: 좌→우 조준 왕복
            keys = [(1, 0), (13, 1), (25, -1), (37, 0)] if rid == 'CC_AcidTower' else [(1, 0), (13, 1), (29, 1), (41, 0)]
            for f, t in keys:
                r = base.copy(); r[axis] = base[axis] + math.radians(deg) * t; o.rotation_euler = r
                o.keyframe_insert('rotation_euler', index=axis, frame=f)
            o.rotation_euler = base


def export(points):
    (BASE / 'interaction_points.json').write_text(json.dumps(points, ensure_ascii=False, indent=1), encoding='utf-8')
    ex = BASE / 'exports-interaction'; ex.mkdir(exist_ok=True)
    for rid in S.HINGES:
        root = bpy.data.objects[rid]; loc = root.location.copy(); root.location = (0, 0, 0)
        bpy.ops.object.select_all(action='DESELECT')
        for o in [root, *root.children_recursive]: o.select_set(True)
        bpy.ops.export_scene.fbx(filepath=str(ex / f'{rid}.fbx'), use_selection=True, object_types={'EMPTY', 'MESH'},
                                 bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                                 apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
                                 add_leaf_bones=False, mesh_smooth_type='FACE')
        root.location = loc


bpy.context.scene.render.fps = FPS; bpy.context.scene.frame_start = 1; bpy.context.scene.frame_end = LOOP
build_hinges(); pts = build_points(); export(pts)
bpy.ops.wm.save_as_mainfile(filepath=str(BASE / 'colony-interaction.blend'), copy=False)
print('points', len(pts), 'hinged', len(S.HINGES))
