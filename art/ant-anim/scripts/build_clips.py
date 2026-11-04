"""QuirkyAnt 리그에 작업 클립을 추가해 ant-work-clips.blend / exports/Ant_WorkClips.fbx 생성.
원본 Ant_Animations.fbx는 읽기만 한다. 실행: blender -b --python build_clips.py
좌표(아마추어 공간): 앞 -Y, 위 +Z, 왼쪽 +X. 각도는 도(deg), 축은 아마추어 공간 기준."""
import bpy, math, sys
from mathutils import Quaternion, Vector
from pathlib import Path

BASE = Path('E:/Git/ant/art/ant-anim')
SRC = r'E:\Git\ant\Assets\ThirdParty\Quirky Series\QuirkyAnt\Animations\Ant_Animations.fbx'
X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
sys.path.insert(0, str(BASE / 'scripts'))
from poses import CLIPS  # noqa: E402

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
scene = bpy.context.scene; scene.render.fps = 24
rig = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
keep = {a.name for a in bpy.data.actions}

def local_q(bone, rots):
    """아마추어 공간 회전 목록 → 본 로컬 쿼터니언(레스트 기준)."""
    q = Quaternion()
    inv = bone.matrix_local.to_3x3().inverted()
    for axis, deg in rots:
        q = Quaternion((inv @ axis).normalized(), math.radians(deg)) @ q
    return q

def apply(pose):
    for pb in rig.pose.bones:
        pb.rotation_mode = 'QUATERNION'
        spec = pose.get(pb.name, {})
        pb.rotation_quaternion = local_q(pb.bone, spec.get('rot', []))
        pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector(spec.get('loc', (0, 0, 0)))
        pb.scale = (1, 1, 1)

def channelbags(act):
    return [cb for layer in act.layers for strip in layer.strips for cb in strip.channelbags]

def from_base(name, base, override):
    """기존 클립(base)을 복제하고 override에 있는 본만 고정 포즈로 덮어쓴다(예: Walk + 운반 앞다리)."""
    act = bpy.data.actions['Rig|' + base].copy(); act.name = 'Rig|' + name; act.use_fake_user = True
    bones = {k for k, v in override.items() if v is not None}
    for cb in channelbags(act):
        for fc in [f for f in cb.fcurves if any(f.data_path.startswith(f'pose.bones["{b}"]') for b in bones)]: cb.fcurves.remove(fc)
    rig.animation_data.action = act
    start, end = act.frame_range
    for pb in rig.pose.bones:
        if pb.name not in bones: continue
        spec = override[pb.name]; pb.rotation_mode = 'QUATERNION'
        pb.rotation_quaternion = local_q(pb.bone, spec.get('rot', []))
        pb.location = pb.bone.matrix_local.to_3x3().inverted() @ Vector(spec.get('loc', (0, 0, 0))); pb.scale = (1, 1, 1)
        for f in (start, end):
            for path in ('rotation_quaternion', 'location', 'scale'): pb.keyframe_insert(path, frame=f, group=pb.name)
    return act

rig.animation_data_create()
for name, keys, *rest in CLIPS:
    if isinstance(keys, str):
        act = from_base(name, keys, rest[0]); act.use_cyclic = rest[1]
        print('CLIP', name, 'from', keys); continue
    loop = rest[0]
    act = bpy.data.actions.new('Rig|' + name); act.use_fake_user = True
    rig.animation_data.action = act
    for frame, pose in keys:
        apply(pose)
        for pb in rig.pose.bones:
            pb.keyframe_insert('rotation_quaternion', frame=frame, group=pb.name)
            pb.keyframe_insert('location', frame=frame, group=pb.name)
            pb.keyframe_insert('scale', frame=frame, group=pb.name)
    act.use_frame_range = True; act.frame_start, act.frame_end = keys[0][0], keys[-1][0]
    act.use_cyclic = loop
    print('CLIP', name, keys[-1][0] - keys[0][0], 'loop' if loop else '')

rig.animation_data.action = None
apply({})
bpy.ops.wm.save_as_mainfile(filepath=str(BASE / 'ant-work-clips.blend'))

# Unity: 새 클립만 담은 FBX(같은 Rig 계층 → Generic Avatar 재사용). 원본 클립은 제외.
for a in list(bpy.data.actions):
    if a.name in keep: bpy.data.actions.remove(a)
bpy.ops.export_scene.fbx(filepath=str(BASE / 'exports' / 'Ant_WorkClips.fbx'), object_types={'ARMATURE', 'MESH'},
    add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0)
print('DONE')
