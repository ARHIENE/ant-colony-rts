"""Per-facility contact data (BLENDER-1019 §4: one shared clip + per-facility contact/offset values, not one forced bake).

blender -b --factory-startup --python scripts/contact_check.py → previews/facility-contacts.json
The character stands on the facility's WorkPoint_0 facing +Y (into the machine). At the clip's contact event the palm
socket (or the tool's contact point) is compared with the facility HandTarget:
  stand_shift  = horizontal move of the stand point that removes the horizontal error,
  residual_z   = height difference left for the engine's hand IK (or a different clip) after that shift.
Nothing is re-baked per facility here; these are the values the engine applies on top of the shared clip.
"""
import bpy, sys, json, importlib, math
sys.path.insert(0, r"E:\Git\ant\codex-work~\blender\scripts")
import pk
importlib.reload(pk)
from mathutils import Vector, Matrix

PAIRS = {  # facility → (clip, event, measured point, hand)
    'SewingTable': ('SewRepair', 'stitch', 'palm', 'R'), 'Blacksmith': ('SmithRepair', 'hit', 'Tool_Hammer', 'R'),
    'OilPress': ('PressLever', 'press', 'palm', 'R'), 'Still': ('ValveTurn', 'turn', 'palm', 'R'),
    'BrewingBarrel': ('TapFill', 'tap_open', 'palm', 'R'), 'GasExtractor': ('ValveTurn', 'turn', 'palm', 'R'),
    'Prospector': ('ControlPanel', 'press', 'palm', 'R'), 'AdvancedDrill': ('ControlPanel', 'press', 'palm', 'R'),
    'Refinery': ('ControlPanel', 'press', 'palm', 'R'), 'TitaniumRefinery': ('ControlPanel', 'press', 'palm', 'R'),
    'ElectricWorkshop': ('ControlPanel', 'press', 'palm', 'R'), 'ChemistryBench': ('SewRepair', 'stitch', 'palm', 'R'),
    'FillingStation': ('CanisterConnect', 'connect', 'palm', 'R'), 'Mill': ('Kemono|Grind', None, 'palm', 'R'),
    'CharcoalKiln': ('OvenLoad', 'insert', 'palm', 'R'), 'CokeOven': ('OvenLoad', 'insert', 'palm', 'R'),
    'AlloyFurnace': ('OvenLoad', 'insert', 'palm', 'R'), 'DeepDrill': ('DriveSeated', None, 'palm', 'R'),
}
bpy.ops.wm.open_mainfile(filepath=str(pk.KIT / 'pk-motions.blend'))
rig = bpy.data.objects['KemonoRig']; rig.data.pose_position = 'POSE'; sc = bpy.context.scene
fac = json.loads((pk.KIT / 'previews' / 'facilities-build.json').read_text(encoding='utf-8'))['facilities']
mrep = json.loads((pk.KIT / 'previews' / 'motion-report.json').read_text(encoding='utf-8'))['clips']
out = {}
for fid, (clip, ev, what, side) in PAIRS.items():
    pts = fac[fid]['points']; wp = pts.get('WorkPoint_0'); ht = pts.get('HandTarget')
    if 'SeatPoint' in pts and fid == 'DeepDrill': wp = {'pos': pts['SeatPoint']['pos'], 'face': [0, 1, 0]}
    if not (wp and ht): out[fid] = {'error': 'missing WorkPoint_0/HandTarget'}; continue
    act = bpy.data.actions[clip if '|' in clip else 'PK|' + clip]; rig.animation_data.action = act; rig.animation_data.action_slot = act.slots[0]
    f = mrep[clip]['events'].get(ev) if ev else 1
    face = Vector(wp['face']); yaw = math.atan2(-face.x, face.y)  # rig front is -Y; turn it to face `face`
    rig.matrix_world = Matrix.Translation(Vector(wp['pos']) - (Vector((0, 0, .41)) if fid == 'DeepDrill' else Vector())) @ Matrix.Rotation(yaw + math.pi, 4, 'Z')
    sc.frame_set(f); bpy.context.view_layer.update()
    sock = rig.matrix_world @ rig.pose.bones[f'Socket.Tool.{side}'].matrix
    if what == 'palm': p = sock.translation
    else:
        src = bpy.data.objects[what + '_Steel']
        tool_m = sock @ Matrix.Rotation(0, 4, 'Z'); p = tool_m @ Vector(src['contact'])
    err = Vector(ht['pos']) - p
    shift = Vector((err.x, err.y, 0))
    out[fid] = {'clip': clip, 'event': ev, 'frame': f, 'measured': what, 'hand_world': [round(x, 3) for x in p],
                'hand_target': ht['pos'], 'error_m': round(err.length, 3), 'stand_shift_m': [round(shift.x, 3), round(shift.y, 3)],
                'residual_z_m': round(err.z, 3), 'recommended_workpoint': [round(wp['pos'][0] + shift.x, 3), round(wp['pos'][1] + shift.y, 3), 0],
                'verdict': 'ok (<3 cm)' if err.length < .03 else ('stand shift fixes it' if abs(err.z) < .04 else 'needs hand IK / clip variant')}
rig.animation_data.action = None
pk.write_json(pk.KIT / 'previews' / 'facility-contacts.json', out)
for k, v in out.items(): print('CONTACT', k, v.get('error_m'), v.get('residual_z_m'), v.get('verdict'))
