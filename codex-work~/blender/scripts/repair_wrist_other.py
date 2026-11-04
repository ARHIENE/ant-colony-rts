"""Try a bounded handle roll; retain a clip only if wrist and mesh-clearance checks improve.

Uses the kit's existing two-bone solver. Hand positions, timing, left-hand grip and
tool length stay fixed. Rejected candidates do not change the source action.
"""
import ast, bpy, json, math, shutil
from pathlib import Path
from mathutils import Matrix, Vector

root = Path('E:/Git/ant/codex-work~')
kit = root / 'blender'
bpy.ops.wm.open_mainfile(filepath=str(kit / 'pk-motions.blend'))
rig = bpy.data.objects['KemonoRig']; vl = bpy.context.view_layer; scene = bpy.context.scene; E = bpy.data.objects
tree = ast.parse((kit / 'scripts/build_motions.py').read_text(encoding='utf-8-sig'))
for node in tree.body:
    if isinstance(node, ast.FunctionDef) and node.name in {'arm_cost', 'solve_arm', 'attach', 'hand_verts', 'clearance', 'wrist_bend', 'tool_length'}:
        exec(compile(ast.Module(body=[node], type_ignores=[]), 'kit_solver', 'exec'))
    if isinstance(node, ast.Assign) and any(isinstance(t, ast.Name) and t.id in {'CLEAR_PARTS', 'ARMOUR_PARTS'} for t in node.targets):
        exec(compile(ast.Module(body=[node], type_ignores=[]), 'kit_constants', 'exec'))
hand_in = rig.data.bones['Socket.Tool.R'].matrix_local.inverted() @ rig.data.bones['hand.R'].matrix_local
report = {}
accepted = []
for name, tool in [('Tool_Mine', 'Tool_Pickaxe_Steel'), ('Tool_Dig', 'Tool_Shovel_Steel')]:
    original = bpy.data.actions['PK|' + name]
    frames = list(range(1, int(original.get('duration_frames', original.frame_range[1])) + 2))
    def use(action):
        rig.animation_data.action = action
        rig.animation_data.action_slot = action.slots[0]
    use(original)
    prop = attach(tool, 'Socket.Tool.R')
    before_clear = clearance(prop, frames, 'RL')
    before_bend = 0
    poses = {}; rolls = []; previous = 0
    for frame in frames:
        scene.frame_set(frame); vl.update()
        before_bend = max(before_bend, wrist_bend('R'))
        socket = rig.pose.bones['Socket.Tool.R'].matrix.copy()
        shoulder = rig.pose.bones['upper_arm.R'].head.copy()
        def score(angle):
            h = socket @ Matrix.Rotation(math.radians(angle), 4, 'Y') @ hand_in
            return arm_cost('R', h, shoulder) + math.radians(.005 * abs(angle) + .01 * abs(angle - previous))
        angle = min(range(-45, 46, 5), key=score)
        if original.get('loop') and frame == frames[-1]: angle = rolls[0]
        previous = angle; rolls.append(angle)
        solve_arm('R', socket @ Matrix.Rotation(math.radians(angle), 4, 'Y') @ hand_in)
        poses[frame] = {n: rig.pose.bones[n].matrix.copy() for n in ('upper_arm.R', 'forearm.R', 'hand.R')}
    candidate = original.copy(); use(candidate)
    for frame in frames:
        scene.frame_set(frame)
        for name_bone, matrix in poses[frame].items():
            bone = rig.pose.bones[name_bone]; bone.matrix = matrix; vl.update()
            bone.keyframe_insert('rotation_euler', frame=frame, group=name_bone)
    after_clear = clearance(prop, frames, 'RL')
    after_bend = 0; left_error = 0; lengths = []
    for frame in frames:
        scene.frame_set(frame); vl.update()
        after_bend = max(after_bend, wrist_bend('R'))
        left = rig.matrix_world @ rig.pose.bones['Socket.Tool.L'].head
        left_error = max(left_error, (left - prop.matrix_world @ Vector(E[tool]['grip2'])).length)
        lengths.append(tool_length(prop))
    count = lambda hits: sum(v['frames'] for v in hits.values())
    keep = after_bend < 60 and after_bend < before_bend - 3 and all(v['frames'] <= before_clear.get(n, {}).get('frames', 0) for n, v in after_clear.items()) and left_error < .001 and max(lengths) - min(lengths) < .00001
    report[name] = {'accepted': keep, 'wrist_before_deg': before_bend, 'wrist_after_deg': after_bend,
                    'clearance_before': before_clear, 'clearance_after': after_clear, 'left_grip_error_m': left_error,
                    'rigid_length_drift_m': max(lengths) - min(lengths), 'roll_deg': rolls}
    if keep:
        bpy.data.actions.remove(original); candidate.name = 'PK|' + name; candidate.use_fake_user = True; accepted.append(name)
    else:
        use(original); bpy.data.actions.remove(candidate)
    bpy.data.objects.remove(prop)
rig.animation_data.action = None
for bone in rig.pose.bones:
    bone.matrix_basis.identity()
if False:
    backup = root / 'blender/pk-motions-before-wrist.blend'
    if not backup.exists(): shutil.copyfile(kit / 'pk-motions.blend', backup)
    bpy.ops.wm.save_as_mainfile(filepath=str(kit / 'pk-motions.blend'))
(root / 'reports/wrist-other.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
print('WRIST', {k: {'accepted': v['accepted'], 'before': round(v['wrist_before_deg'], 1), 'after': round(v['wrist_after_deg'], 1)} for k,v in report.items()})
