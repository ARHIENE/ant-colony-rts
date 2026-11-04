"""도감용 상호작용 미리보기 프레임: blender -b colony-interaction.blend --python render_interaction.py [-- CC_Id ...]
interaction_points.json 기준으로 개미(ant-work-clips)를 작업 지점에 세우고 클립·도구·경첩을 함께 렌더.
결과: previews/interaction/<id>/f00..f11.png (GIF 합치기는 make_gifs.py)."""
import bpy, json, math, sys
from pathlib import Path
from mathutils import Vector, Matrix
BASE = Path('E:/Git/ant/art/colony-core'); ANIM = Path('E:/Git/ant/art/ant-anim')
ANT_SCALE, FRAMES, SIZE = 4.0, 12, 360
spec = json.loads((BASE / 'interaction_points.json').read_text(encoding='utf-8'))
args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
ids = args or list(spec)
scene = bpy.context.scene

with bpy.data.libraries.load(str(ANIM / 'ant-work-clips.blend')) as (s, d): d.objects = ['Rig', 'Ant_LOD1']; d.actions = list(s.actions)
with bpy.data.libraries.load(str(ANIM / 'ant-tools.blend')) as (s, d): d.objects = list(s.objects)
tools = {o.name: o for o in d.objects}
for o in [o for o in bpy.data.objects if o.name in ('Rig', 'Ant_LOD1') or o.name in tools]:
    if not o.users_collection: scene.collection.objects.link(o)
rig = bpy.data.objects['Rig']; antmesh = bpy.data.objects['Ant_LOD1']
for a in bpy.data.actions:  # 짧은 클립도 48프레임 동안 반복
    if a.name.startswith('Rig|'):
        bag = a.layers[0].strips[0].channelbags[0]
        for fc in list(bag.fcurves):
            if not fc.data_path.startswith('pose'): bag.fcurves.remove(fc); continue  # 원본 클립의 Rig 위치 고정 키 → 배치 방해
            if not fc.modifiers: fc.modifiers.new('CYCLES')

scene.render.engine = 'BLENDER_WORKBENCH'; sh = scene.display.shading
sh.light = 'STUDIO'; sh.color_type = 'MATERIAL'; sh.show_cavity = True; sh.show_shadows = True
scene.render.resolution_x = scene.render.resolution_y = SIZE; scene.render.film_transparent = False
cam = scene.camera; cam.data.type = 'ORTHO'
roots = {o.name: o for o in bpy.data.objects if o.name in spec}


def show_only(keep):
    for o in scene.objects:
        if o.type in {'CAMERA', 'LIGHT'} or o.name == 'Studio floor': continue
        o.hide_render = o not in keep


def frame_camera(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == 'MESH' and not o.hide_render for c in o.bound_box]
    center = sum(pts, Vector()) / len(pts)
    d = Vector((1, -1.5, 1.1)).normalized(); cam.location = center + d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler(); bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted(); pp = [inv @ p for p in pts]
    w = max(p.x for p in pp) - min(p.x for p in pp); h = max(p.y for p in pp) - min(p.y for p in pp)
    cam.location += cam.matrix_world.to_3x3() @ Vector(((max(p.x for p in pp) + min(p.x for p in pp)) / 2, (max(p.y for p in pp) + min(p.y for p in pp)) / 2, 0))
    cam.data.ortho_scale = max(w, h) * 1.15


def world_point(root, p):
    return root.matrix_world @ Vector(p['pos']), (root.matrix_world.to_3x3() @ Vector(p['forward'])).normalized()


def place_ant(pos, fwd, clip):
    rig.animation_data_create(); rig.animation_data.action = bpy.data.actions['Rig|' + clip]
    rig.scale = (ANT_SCALE,) * 3; rig.location = pos
    rig.rotation_euler = (0, 0, math.atan2(fwd.y, fwd.x) + math.pi / 2)  # 개미 정면 = -Y


for rid in ids:
    info = spec[rid]; root = roots[rid]; pts = info['points']; clip = info['clip']
    keep = {root, *root.children_recursive}; extras = []
    if clip:
        if 'SeatPoint' in pts and clip in ('Sit', 'Rest', 'Sleep', 'Eat', 'StandIdle'):
            pos, _ = world_point(root, pts['SeatPoint']); fwd = root.matrix_world.to_3x3() @ Vector((0, 1, 0))
            if clip == 'Eat': pos, fwd = world_point(root, pts['WorkPoint_0'])  # 식탁 앞에 앉아 먹기
            place_ant(pos, -fwd if clip != 'Eat' else fwd, clip)
        elif clip == 'Carry':
            place_ant(root.matrix_world.translation + Vector((0, 0, 0)), Vector((1, 0, 0)), 'Carry')
        else:
            pos, fwd = world_point(root, pts['WorkPoint_0']); place_ant(pos, fwd, clip)
        keep |= {rig, antmesh}
        if info['tool']:
            t = tools[info['tool']]; t.parent = rig; t.parent_type = 'BONE'; t.parent_bone = 'leg.01.R'
            t.location = (0, .022, 0); t.rotation_euler = (math.radians(-90), 0, 0); keep |= {t, *t.children_recursive}
    saved = root.matrix_world.copy()
    if clip == 'Carry':  # 물건을 등 위로 옮겨 body 본에 붙임(표시용)
        bpy.context.view_layer.update(); scene.frame_set(1)
        dims = [o.dimensions for o in root.children_recursive if o.type == 'MESH']
        span = max(max(d) for d in dims) if dims else 1
        sc = min(1.0, .5 / span)
        body = rig.pose.bones['body']; anchor = rig.matrix_world @ body.head
        root.parent = rig; root.parent_type = 'BONE'; root.parent_bone = 'body'
        root.matrix_world = Matrix.Translation(anchor + Vector((0, 0, .25))) @ Matrix.Scale(sc, 4)
    show_only(keep)
    scene.frame_set(1); bpy.context.view_layer.update(); frame_camera(list(keep))
    out = BASE / 'previews' / 'interaction' / rid; out.mkdir(parents=True, exist_ok=True)
    for i in range(FRAMES):
        scene.frame_set(1 + i * 4); scene.render.filepath = str(out / f'f{i:02d}.png')
        bpy.ops.render.render(write_still=True)
    if clip == 'Carry':
        root.parent = None; root.matrix_world = saved
    for t in tools.values(): t.parent = None
    print('done', rid, clip)
