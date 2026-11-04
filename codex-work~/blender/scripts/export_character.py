"""Unity 캐릭터 교체용(2026-10-24): 현재 kemono 리그(+소켓) 캐릭터와 kemono 기존 동작을 PK 클립과 같은 리그로 내보낸다.

blender -b --factory-startup --python scripts/export_character.py
→ exports/character/Kemono_Character.fbx (KemonoRig + 모든 Part_* 부품, 엔진이 조합별로 켬)
→ exports/character/Kemono@<clip>.fbx (kemono-animations.blend의 Kemono|<clip>, 리그만)
축·옵션은 export_fbx.py와 같음(-Z 앞, Y 위, 리프 뼈 없음). 갑옷용 _ARMOR 부품은 장비 착용 시만 쓰므로 포함한다.
"""
import bpy, sys, importlib
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk, rig_adapter as R
for m in (pk, R): importlib.reload(m)

bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / 'pk-motions.blend'))
rig = bpy.data.objects['KemonoRig']; rig.data.pose_position = 'POSE'
if rig.animation_data: rig.animation_data.action = None
for pb in rig.pose.bones: pb.matrix_basis.identity()
out = pk.KIT / 'exports' / 'character'; out.mkdir(parents=True, exist_ok=True)
COMMON = dict(axis_forward='-Z', axis_up='Y', add_leaf_bones=False, use_selection=True, apply_scale_options='FBX_SCALE_ALL')
vl = bpy.context.view_layer


def select(objs):
    for o in vl.objects: o.select_set(False)
    for o in objs:
        if o.name not in vl.objects: bpy.context.scene.collection.objects.link(o)
        o.hide_set(False); o.hide_viewport = False; o.select_set(True)
    vl.objects.active = objs[0]


parts = [o for o in R.parts() if o.parent == rig or any(m.type == 'ARMATURE' and m.object == rig for m in o.modifiers)]
# Rigid plates use the same skeleton, with one bone weight per vertex. This avoids
# recreating Blender bone-parent inverse matrices in the Unity equipment code.
for src in list(bpy.data.objects):
    if not src.name.startswith('Armor_') or not src.get('rigid') or src.name.startswith('Armor_Breastplate_Trim_'):
        continue
    bone = src.get('bone')
    if bone not in rig.data.bones:
        continue
    obj = src.copy(); obj.data = src.data.copy()
    bpy.context.scene.collection.objects.link(obj)
    obj.name = 'Part_Equipment_' + src.name
    obj.data.transform(rig.matrix_world.inverted() @ src.matrix_world)
    obj.parent = rig; obj.matrix_parent_inverse.identity(); obj.matrix_basis.identity()
    obj.modifiers.clear(); obj.vertex_groups.clear()
    group = obj.vertex_groups.new(name=bone)
    group.add(list(range(len(obj.data.vertices))), 1, 'REPLACE')
    modifier = obj.modifiers.new('RigidBone', 'ARMATURE'); modifier.object = rig
    parts.append(obj)
select([rig] + parts)
bpy.ops.export_scene.fbx(filepath=str(out / 'Kemono_Character.fbx'), object_types={'MESH', 'ARMATURE'}, bake_anim=False, path_mode='COPY', embed_textures=True, **COMMON)  # 얼굴·눈·옷 텍스처 포함
listing = {'Kemono_Character.fbx': {'parts': sorted(o.name for o in parts), 'bones': len(rig.data.bones)}}

acts = {} if '--model-only' in sys.argv else R.load_actions()
rig.animation_data_create(); sc = bpy.context.scene
for name, act in acts.items():
    rig.animation_data.action = act
    if act.slots: rig.animation_data.action_slot = act.slots[0]
    sc.frame_start, sc.frame_end = 1, int(act.get('duration_frames', act.frame_range[1])) + 1
    select([rig]); f = out / f'Kemono@{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(f), object_types={'ARMATURE'}, bake_anim=True, bake_anim_use_all_actions=False,
                             bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0, **COMMON)
    listing[f.name] = {'frames': [sc.frame_start, sc.frame_end]}
pk.write_json(out / 'export-list.json', listing)
print('CHARACTER', len(parts), 'parts', len(acts), 'clips')
