"""New key-pose clips for BLENDER-1019 §4, same conventions/helpers as art/kemono-kit/scripts/motions.py (imported, not
modified). Existing kemono clips are reused where they already fit (REUSE below) and are not rewritten.

Each clip: name -> dict(frames, loop, keys=[(frame, pose)], grip={'R': [(frame, curl)], 'L': ...}, events={name: frame},
          prop=(tool/weapon id or None), two_hand=bool, target=description of what the contact frame touches).
Event frames are where the engine swaps parents/visibility or applies a hit; nothing in Blender fakes the game logic.
"""
import math, sys
sys.path.insert(0, r"E:\Git\ant\art\kemono-kit\scripts")
import motions as K  # P, merge, spine, look, hips, crouch, SEAT, breathe, cyc, walk, RELAX

P, merge, spine, look, hips, crouch, SEAT = K.P, K.merge, K.spine, K.look, K.hips, K.crouch, K.SEAT
TAU = math.tau
S = math.sin

REUSE = {
    'carry_unfinished': ['Carry', 'PutDown', 'Pickup'],  # 미완성품 운반·내려놓기·집기
    'grind': ['Grind'], 'stir': ['Stir'], 'walk_with_sheathed_weapon': ['Walk', 'Run', 'Idle', 'StandIdle'],
    'downed_idle': ['Downed'], 'operate_generic': ['Operate'], 'sow_by_hand': ['GroundWork'],
    'eat_G61': ['Eat'], 'surgery_G55': ['Treat'],  # 2026-10-24: 식사·타인 수술은 kemono 기존 클립
}

IDLE = merge(K.breathe(0), P(upper_arm_L=(0, 0, 0)))
GUARD = P(upper_arm_R=(-.85, .15, 0), forearm_R=(-.75, 0, .1), hand_R=(.15, 0, 0), upper_arm_L=(-.35, -.1, 0), forearm_L=(-.7, 0, -.3))
REACH_HIP_R = merge(spine(z=-.18, x=.05), look(z=-.15), P(upper_arm_R=(.45, .35, .2), forearm_R=(-.55, 0, 0), hand_R=(.2, 0, 0)))
CARRY_POSE = P(upper_arm_L=(-.55, .2, 0), upper_arm_R=(-.55, -.2, 0), forearm_L=(-1.1, 0, -.25), forearm_R=(-1.1, 0, .25))
DOWN_REACH = merge(crouch(.75), spine(x=.45), look(x=.25), P(upper_arm_L=(-.7, -.05, 0), upper_arm_R=(-.7, .05, 0), forearm_L=(-.3, 0, 0), forearm_R=(-.3, 0, 0)))
HANDS_HEAD = merge(look(x=-.05), P(upper_arm_L=(-2.5, -.55, 0), upper_arm_R=(-2.5, .55, 0), forearm_L=(-1.2, 0, -.3), forearm_R=(-1.2, 0, .3)))
BENCH = merge(spine(x=.12), look(x=.22), P(upper_arm_L=(-.95, -.1, 0), upper_arm_R=(-.95, .1, 0), forearm_L=(-.6, 0, -.15), forearm_R=(-.6, 0, .15)))
LIE_SIDE = P(Root=(0, -math.pi / 2, 0), Root_loc=(0, 0, .20))
SQUAT = merge(hips(-.24), {f'thigh.{s}': (-1.5, .12 if s == 'L' else -.12, 0) for s in 'LR'}, {f'shin.{s}': (2.3, 0, 0) for s in 'LR'},
              {f'foot.{s}': (-.8, 0, 0) for s in 'LR'})  # kemono clips() 안의 SQUAT과 같은 값
FLOOR_SIT = merge(hips(-.26), {f'thigh.{s}': (-1.45, 0, 0) for s in 'LR'}, {f'shin.{s}': (.15, 0, 0) for s in 'LR'})  # 바닥에 다리 뻗고 앉기
OPEN, FIST = .05, .9  # finger curl (bone-local X radians); FIST closes round a 7 mm handle at the moved Socket.Tool.*
PINKY, THUMB = .61, .44  # pinky / thumb curl as a fraction of the finger curl (shorter fingers would sink into the handle)


# Two-handed tools are driven by the TOOL path, not by arm angles (chibi arms are 0.26 m and the head is large, so the tool
# swings from over the right shoulder, never over the head). key = (frame, grip xyz, handle dir, strike hint), rig space.
# grip = Socket.Tool.R / tool origin; dir = tool +Y; strike hint = where the tool's -Z (edge/face) should point.
# build_motions solves both arms onto it (wrist IK + exact hand rotation) and bakes the keys.
TOOL_KEYS = {
    # tool paths keep the whole handle (incl. the butt behind the grip) outside the torso plates: grips sit right of the body
    # and in front of the belly plate (y ≤ -.2); raised keys lift in front of the chest centre with the head tipped forward
    # (an over-the-shoulder raise runs the handle through the big head/hair and the chest plate, and the short left arm
    # cannot cross the body to reach it).
    'Tool_Chop': [(1, (-.12, -.24, .50), (.1, -.85, .45), (0, 0, -1)), (14, (-.06, -.22, .64), (-.2, -.5, .85), (.3, -.6, -.5)),
                  (19, (-.08, -.26, .47), (.08, -.95, -.15), (.35, 0, -1)), (23, (-.08, -.27, .45), (.08, -.9, -.3), (.35, 0, -1)),
                  (32, (-.12, -.24, .52), (.05, -.8, .3), (0, 0, -1)), (41, (-.12, -.24, .50), (.1, -.85, .45), (0, 0, -1))],
    'Tool_Mine': [(1, (-.1, -.24, .50), (.05, -.7, .5), (0, 0, -1)), (14, (-.06, -.22, .65), (-.15, -.5, .85), (.1, -.7, -.4)),
                  (19, (-.07, -.26, .44), (.03, -.8, -.55), (.1, -.4, -1)), (23, (-.07, -.26, .42), (.03, -.75, -.65), (.1, -.4, -1)),
                  (32, (-.1, -.23, .50), (.03, -.75, .2), (0, 0, -1)), (41, (-.1, -.24, .50), (.05, -.7, .5), (0, 0, -1))],
    # hoe breaks ground with a chopping stroke (blade down at impact) — a pulled stroke would point the fingers at the body
    'Tool_Till': [(1, (-.1, -.25, .40), (0, -.75, -.45), (0, -.2, -1)), (12, (-.08, -.18, .56), (-.1, -.35, .93), (0, -1, -.3)),
                  (17, (-.08, -.27, .34), (0, -.8, -.6), (0, -.3, -1)), (24, (-.08, -.26, .36), (0, -.75, -.65), (0, -.3, -1)),
                  (37, (-.1, -.25, .40), (0, -.75, -.45), (0, -.2, -1))],
    # shovel: fingers point forward-inward on the upright shaft, down while carrying the load
    'Tool_Dig': [(1, (-.16, -.2, .50), (.35, -.85, .1), (0, -.5, -1)), (10, (-.1, -.24, .54), (0, -.45, -.89), (.5, -1, 0)),
                 (16, (-.09, -.25, .46), (0, -.4, -.92), (.5, -1, 0)), (24, (-.09, -.22, .48), (0, -.6, -.8), (.5, -1, 0)),
                 (32, (-.16, -.2, .50), (.35, -.85, .1), (0, -.5, -1)), (41, (-.16, -.2, .50), (.35, -.85, .1), (0, -.5, -1))],
    # one-handed: hammer on the anvil workpiece / construction surface, sickle sweep through stalks (right arm solved only)
    'SmithRepair': [(1, (-.09, -.22, .36), (0, -1, -.15), (0, 0, -1)), (5, (-.09, -.22, .40), (0, -.9, .3), (0, -.3, -1)),
                    (15, (-.14, -.14, .56), (-.2, -.2, 1), (0, -1, -.1)), (20, (-.09, -.22, .36), (0, -1, -.15), (0, 0, -1)),
                    (25, (-.09, -.22, .36), (0, -1, -.15), (0, 0, -1))],
    'Tool_Hammer': [(1, (-.1, -.22, .44), (0, -1, -.1), (0, 0, -1)), (5, (-.1, -.22, .48), (0, -.9, .35), (0, -.3, -1)),
                    (14, (-.18, -.12, .62), (-.3, -.2, 1), (0, -1, -.1)), (19, (-.1, -.22, .44), (0, -1, -.1), (0, 0, -1)),
                    (23, (-.1, -.22, .44), (0, -1, -.1), (0, 0, -1))],
    'Tool_Harvest': [(1, (-.2, -.2, .26), (-.5, -.85, .1), (1, -.3, -.2)), (9, (-.2, -.2, .26), (-.5, -.85, .1), (1, -.3, -.2)),
                     (15, (-.04, -.27, .22), (.2, -1, .05), (1, .1, -.2)), (20, (.04, -.25, .23), (.5, -.85, .05), (1, .4, -.2)),
                     (29, (-.2, -.2, .26), (-.5, -.85, .1), (1, -.3, -.2))],
    # spear along the right side, outside the hip and tail: the butt (34 cm behind the grip) passes beside the body
    'SpearThrust': [(1, (-.2, -.06, .44), (.22, -1, .08), (0, -.5, -1)), (10, (-.2, -.06, .44), (.22, -1, .08), (0, -.5, -1)),
                    (14, (-.17, -.22, .47), (.2, -1, .05), (0, -.6, -1)), (18, (-.17, -.22, .47), (.2, -1, .05), (0, -.6, -1)),
                    (25, (-.2, -.06, .44), (.22, -1, .08), (0, -.5, -1))],
}


MOUTH = (0, -.154, .76)  # kemono mouth part (Part_Face_*_01) at rest; drinking brings the cup rim here
DOWN, FWD, IN_R, IN_L, UP, FRONT = (0, .25, -1), (0, -1, -.45), (1, 0, 0), (-1, 0, 0), (0, 0, 1), (0, -1, 0)  # FRONT = palm faces forward


def _inn(s): return IN_R if s == 'R' else IN_L


# Hands-only contact clips: per side (frame, palm-socket position, finger direction, palm normal), rig space (front -Y).
# build_motions solves that arm onto the keys (two-bone, straightest wrist) and bakes it; unlisted arms keep their FK keys.
HAND_KEYS = {
    # centre-grip shield held out in front-left of the chest: knuckles (and the shield face) forward, wrist straight
    'ShieldBlock': {'L': [(1, (.1, -.22, .52), (0, -1, .1), IN_L), (12, (.1, -.23, .53), (0, -1, .1), IN_L), (25, (.1, -.22, .52), (0, -1, .1), IN_L)]},
    'SewRepair': {'R': [(1, (-.04, -.2, .43), FWD, DOWN), (12, (-.03, -.21, .425), FWD, DOWN), (24, (-.1, -.16, .52), (0, -.6, .2), (.5, 0, -1)),
                        (36, (-.03, -.21, .425), FWD, DOWN), (49, (-.04, -.2, .43), FWD, DOWN)],
                  'L': [(1, (.06, -.19, .43), FWD, DOWN), (49, (.06, -.19, .43), FWD, DOWN)]},
    'Knead': {s: [(1, (x, -.2, .45), FWD, DOWN), (8, (x, -.23, .42), FWD, DOWN), (16, (x, -.2, .45), FWD, DOWN), (24, (x, -.23, .42), FWD, DOWN),
                  (33, (x, -.2, .45), FWD, DOWN)] for s, x in (('R', -.05), ('L', .05))},
    'PressLever': {s: [(1, (x, -.2, .74), FWD, DOWN), (18, (x, -.22, .66), FWD, DOWN), (24, (x, -.22, .66), FWD, DOWN), (33, (x, -.2, .74), FWD, DOWN)]
                   for s, x in (('R', -.05), ('L', .05))},
    'ValveTurn': {s: [(f, (x + .035 * math.cos(math.tau * (f - 1) / 24 + ph), -.21, .62 + .035 * math.sin(math.tau * (f - 1) / 24 + ph)), (0, -1, .3), _inn(s))
                      for f in range(1, 26, 3)] for s, x, ph in (('R', -.04, math.pi), ('L', .04, 0))},  # steering-wheel grip, palms inward
    'ControlPanel': {'R': [(1, (-.05, -.2, .46), FWD, (0, -.6, -.8)), (9, (-.04, -.23, .46), FWD, (0, -.6, -.8)), (18, (-.05, -.2, .46), FWD, (0, -.6, -.8)),
                           (27, (0, -.21, .5), (0, -1, .3), IN_R), (37, (-.05, -.2, .46), FWD, (0, -.6, -.8))]},
    'TapFill': {'R': [(1, (-.1, -.08, .40), (0, 0, -1), IN_R), (10, (-.04, -.2, .31), FWD, DOWN), (14, (-.04, -.21, .30), FWD, IN_R),
                      (28, (-.04, -.21, .30), FWD, IN_R), (32, (-.04, -.2, .31), FWD, DOWN), (41, (-.1, -.12, .42), FWD, IN_R)],
                'L': [(1, (.1, -.08, .40), (0, 0, -1), IN_L), (10, (.03, -.2, .28), FWD, IN_L), (32, (.03, -.2, .28), FWD, IN_L), (41, (.1, -.12, .42), FWD, IN_L)]},
    'OvenLoad': {s: [(1, (x, -.18, .45), FWD, _inn(s)), (12, (x, -.14, .47), FWD, _inn(s)), (18, (x, -.25, .44), FWD, _inn(s)),
                     (22, (x, -.25, .44), FWD, _inn(s)), (33, (x, -.18, .45), FWD, _inn(s))] for s, x in (('R', -.06), ('L', .06))},
    'BenchPlace': {s: [(1, (x, -.15, .45), FWD, _inn(s)), (12, (x, -.21, .44), FWD, DOWN), (16, (x, -.21, .43), FWD, DOWN),
                       (29, (x * 2.2, -.04, .42), (0, 0, -1), _inn(s))] for s, x in (('R', -.06), ('L', .06))},
    'BenchPickup': {s: [(1, (x * 2.2, -.04, .42), (0, 0, -1), _inn(s)), (12, (x, -.21, .43), FWD, DOWN), (15, (x, -.21, .44), FWD, DOWN),
                        (29, (x, -.15, .45), FWD, _inn(s))] for s, x in (('R', -.06), ('L', .06))},
    'CanisterConnect': {'R': [(1, (-.08, -.14, .42), FWD, IN_R), (12, (-.06, -.2, .30), FWD, IN_R), (18, (-.06, -.2, .30), FWD, IN_R),
                              (26, (-.04, -.22, .32), FWD, DOWN), (30, (-.04, -.22, .32), (.5, -1, -.45), DOWN), (41, (-.13, -.02, .42), (0, 0, -1), IN_R)],
                        'L': [(1, (.08, -.14, .42), FWD, IN_L), (12, (.06, -.2, .30), FWD, IN_L), (41, (.13, -.02, .42), (0, 0, -1), IN_L)]},
    'CanisterDisconnect': {'R': [(1, (-.13, -.02, .42), (0, 0, -1), IN_R), (10, (-.04, -.22, .32), (.5, -1, -.45), DOWN), (16, (-.06, -.2, .30), FWD, IN_R),
                                 (26, (-.06, -.2, .30), FWD, IN_R), (41, (-.08, -.14, .42), FWD, IN_R)],
                           'L': [(1, (.13, -.02, .42), (0, 0, -1), IN_L), (16, (.06, -.2, .30), FWD, IN_L), (41, (.08, -.14, .42), FWD, IN_L)]},
    'Drink': {'R': [(1, (-.1, -.14, .45), FWD, IN_R), (8, (-.1, -.14, .46), FWD, IN_R), (18, (-.04, -.2, .71), (.2, -.4, .9), IN_R),
                    (30, (-.04, -.19, .72), (.2, -.2, 1), IN_R), (38, (-.1, -.14, .46), FWD, IN_R), (49, (-.1, -.14, .45), FWD, IN_R)]},
    'HangoverIdle': {'R': [(1, (-.06, -.16, .86), (.5, .2, .8), (.3, 1, .1)), (37, (-.06, -.17, .85), (.5, .2, .8), (.3, 1, .1)),
                           (73, (-.06, -.16, .86), (.5, .2, .8), (.3, 1, .1))]},
    'PourSack': {s: [(1, (x, -.15, .45), FWD, _inn(s)), (10, (x, -.18, .62), FWD, _inn(s)), (16, (x, -.24, .6), (0, -.6, -.8), _inn(s)),
                     (26, (x, -.24, .6), (0, -.6, -.8), _inn(s)), (33, (x * 2.2, -.04, .42), (0, 0, -1), _inn(s))] for s, x in (('R', -.08), ('L', .08))},
    'DriveSeated': {s: [(f, (x, -.2 - .03 * math.sin(math.tau * (f - 1) / 48 + ph), .5), FWD, _inn(s)) for f in range(1, 50, 8)]
                    for s, x, ph in (('R', -.1, 0), ('L', .1, math.pi))},
}


def hand_target(name, side, f):
    """(palm position, fingers, palm normal) for one hand at frame f from HAND_KEYS (smoothstep between keys), or None."""
    from mathutils import Vector
    keys = HAND_KEYS.get(name, {}).get(side)
    if not keys: return None
    k0 = max((k for k in keys if k[0] <= f), key=lambda k: k[0], default=keys[0])
    k1 = min((k for k in keys if k[0] >= f), key=lambda k: k[0], default=keys[-1])
    t = 0 if k1[0] == k0[0] else (f - k0[0]) / (k1[0] - k0[0]); t = t * t * (3 - 2 * t)
    lerp = lambda a, b: Vector(a).lerp(Vector(b), t)
    return (lerp(k0[1], k1[1]), lerp(Vector(k0[2]).normalized(), Vector(k1[2]).normalized()).normalized(),
            lerp(Vector(k0[3]).normalized(), Vector(k1[3]).normalized()).normalized())


def tool_matrix(name, f):
    """Tool world matrix at frame f from TOOL_KEYS (smoothstep between keys)."""
    from mathutils import Vector, Matrix
    keys = TOOL_KEYS[name]
    k0 = max((k for k in keys if k[0] <= f), key=lambda k: k[0]); k1 = min((k for k in keys if k[0] >= f), key=lambda k: k[0])
    t = 0 if k1[0] == k0[0] else (f - k0[0]) / (k1[0] - k0[0]); t = t * t * (3 - 2 * t)
    lerp = lambda a, b: Vector(a).lerp(Vector(b), t)
    g = lerp(k0[1], k1[1]); y = lerp(Vector(k0[2]).normalized(), Vector(k1[2]).normalized()).normalized()
    h = lerp(Vector(k0[3]).normalized(), Vector(k1[3]).normalized())
    z = -(h - y * h.dot(y)).normalized(); x = y.cross(z)
    return Matrix.Translation(g) @ Matrix((x, y, z)).transposed().to_4x4()


def two_hand_tool(up, hit, stuck):
    return [(1, merge(stuck, spine(x=-.05))), (14, up), (19, hit), (23, stuck), (32, merge(stuck, spine(x=-.08))), (41, merge(stuck, spine(x=-.05)))]


def clips():
    C = {}
    # ---------------- weapons: draw / sheathe / attack / block --------------------------------------------------
    C['DrawSword'] = dict(frames=20, loop=False, prop='Weapon_Sword', keys=[(1, IDLE), (8, REACH_HIP_R), (10, REACH_HIP_R),
                     (15, merge(GUARD, spine(z=.05))), (21, GUARD)],
                     grip={'R': [(1, OPEN), (9, OPEN), (10, FIST)]}, events={'weapon_grab': 10, 'ready': 15},
                     target='sword grip at Socket.Hip.R → Socket.Tool.R')
    C['SheatheSword'] = dict(frames=20, loop=False, prop='Weapon_Sword', keys=[(1, GUARD), (8, REACH_HIP_R), (11, REACH_HIP_R), (21, IDLE)],
                     grip={'R': [(1, FIST), (10, FIST), (11, OPEN)]}, events={'weapon_release': 11}, target='Socket.Tool.R → Socket.Hip.R')
    wind = merge(GUARD, spine(z=.35, x=-.08), look(z=.2), P(upper_arm_R=(-2.4, .5, .4), forearm_R=(-.9, 0, 0)))
    slash = merge(spine(z=-.4, x=.2), look(z=-.1), crouch(.2), P(upper_arm_R=(-.9, -.25, -.5), forearm_R=(-.2, 0, 0), hand_R=(.4, 0, 0), upper_arm_L=(-.2, -.1, 0)))
    C['SwordAttack'] = dict(frames=26, loop=True, prop='Weapon_Sword', keys=[(1, GUARD), (9, wind), (13, slash), (17, merge(slash, spine(x=.05))), (27, GUARD)],
                     grip={'R': [(1, FIST)]}, events={'hit': 13}, target='enemy at 0.35 m in front')
    block = merge(spine(x=-.05), look(x=-.05), crouch(.15), P(upper_arm_L=(-1.1, -.35, .6), forearm_L=(-1.4, 0, -.4), hand_L=(0, 0, .3)))
    C['ShieldBlock'] = dict(frames=24, loop=True, prop='Weapon_Shield', keys=[(1, block), (12, merge(block, spine(x=-.02))), (25, block)],
                     grip={'L': [(1, FIST)]}, events={}, target='shield on Socket.Tool.L')
    thrust_back = merge(spine(z=.2), P(upper_arm_R=(.2, .2, 0), forearm_R=(-1.2, 0, 0), upper_arm_L=(-1.0, -.1, .3), forearm_L=(-.5, 0, 0)))
    thrust = merge(spine(z=-.15, x=.15), crouch(.25), P(upper_arm_R=(-1.2, .1, 0), forearm_R=(-.1, 0, 0), upper_arm_L=(-1.3, -.05, .2), forearm_L=(-.2, 0, 0)))
    C['SpearThrust'] = dict(frames=24, loop=True, prop='Weapon_Spear', two_hand=True, keys=[(1, thrust_back), (10, thrust_back), (14, thrust), (18, thrust), (25, thrust_back)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'hit': 14}, target='enemy at 0.5 m in front')

    # ---------------- muster: put the load down → hide tool → draw weapon --------------------------------------------
    C['Muster'] = dict(frames=40, loop=False, prop='Weapon_Sword', keys=[(1, CARRY_POSE), (9, merge(CARRY_POSE, crouch(.55), spine(x=.25))),
                     (12, merge(DOWN_REACH, P(upper_arm_L=(-.4, 0, 0), upper_arm_R=(-.4, 0, 0)))), (18, IDLE), (25, REACH_HIP_R), (27, REACH_HIP_R),
                     (34, GUARD), (41, GUARD)], grip={'R': [(1, FIST), (12, OPEN), (26, OPEN), (27, FIST)], 'L': [(1, FIST), (12, OPEN)]},
                     events={'drop_load': 12, 'tool_hide': 17, 'weapon_grab': 27, 'ready': 34}, target='load placed on the floor in front')
    C['ToolOut'] = dict(frames=12, loop=False, prop='tool', keys=[(1, IDLE), (6, merge(REACH_HIP_R, spine(z=.08))), (13, P(upper_arm_R=(-.55, .05, 0), forearm_R=(-.6, 0, 0)))],
                     grip={'R': [(1, OPEN), (6, FIST)]}, events={'tool_show': 6}, target='tool shown in Socket.Tool.R only while working')
    C['ToolAway'] = dict(frames=12, loop=False, prop='tool', keys=[(1, P(upper_arm_R=(-.55, .05, 0), forearm_R=(-.6, 0, 0))), (7, REACH_HIP_R), (13, IDLE)],
                     grip={'R': [(1, FIST), (7, OPEN)]}, events={'tool_hide': 7}, target='tool hidden (still owned)')

    # ---------------- downed: weapon leaves the hand, armour stays ---------------------------------------------------
    C['CollapseDropWeapon'] = dict(frames=36, loop=False, prop='Weapon_Sword', keys=[(1, GUARD), (8, merge(GUARD, spine(x=.3), crouch(.3))),
                     (12, merge(spine(x=.42), look(x=.3), crouch(.65), P(upper_arm_R=(-.3, .3, 0), forearm_R=(-.2, 0, 0), upper_arm_L=(-.4, 0, 0)))),
                     (22, merge(spine(x=.4), crouch(.9))), (30, merge(LIE_SIDE, spine(x=.12), P(upper_arm_L=(-.6, .1, 0), forearm_L=(-1.2, 0, 0), thigh_L=(-.5, 0, 0), shin_L=(.9, 0, 0)))),
                     (37, merge(LIE_SIDE, spine(x=.15), P(upper_arm_L=(-.6, .1, 0), forearm_L=(-1.2, 0, 0), thigh_L=(-.6, 0, 0), shin_L=(1.0, 0, 0))))],
                     grip={'R': [(1, FIST), (11, FIST), (12, OPEN)]}, events={'weapon_drop': 12}, target='weapon detaches to the floor; armour/clothes stay')

    # ---------------- equip / unequip from the floor -------------------------------------------------------------------
    C['EquipFromGround'] = dict(frames=40, loop=False, prop='helmet', keys=[(1, IDLE), (12, DOWN_REACH), (14, DOWN_REACH), (22, IDLE), (30, HANDS_HEAD), (33, HANDS_HEAD), (41, IDLE)],
                     grip={'R': [(1, OPEN), (13, FIST), (32, FIST), (33, OPEN)], 'L': [(1, OPEN), (13, FIST), (32, FIST), (33, OPEN)]},
                     events={'pick': 13, 'equip': 32}, target='item on the floor 0.25 m in front')
    C['UnequipToGround'] = dict(frames=40, loop=False, prop='helmet', keys=[(1, IDLE), (8, HANDS_HEAD), (10, HANDS_HEAD), (20, IDLE), (28, DOWN_REACH), (30, DOWN_REACH), (41, IDLE)],
                     grip={'R': [(1, OPEN), (9, FIST), (29, FIST), (30, OPEN)], 'L': [(1, OPEN), (9, FIST), (29, FIST), (30, OPEN)]},
                     events={'unequip': 9, 'release': 30}, target='item placed 0.25 m in front')

    # ---------------- repair hand-off and repair work ------------------------------------------------------------------
    C['BenchPlace'] = dict(frames=28, loop=False, prop='repair_item', keys=[(1, CARRY_POSE), (12, BENCH), (16, BENCH), (29, IDLE)],
                     grip={'R': [(1, FIST), (15, FIST), (16, OPEN)], 'L': [(1, FIST), (15, FIST), (16, OPEN)]}, events={'release': 16},
                     target='bench top (facility HandTarget), equipment left as a work object')
    C['BenchPickup'] = dict(frames=28, loop=False, prop='repair_item', keys=[(1, IDLE), (12, BENCH), (15, BENCH), (29, CARRY_POSE)],
                     grip={'R': [(1, OPEN), (14, FIST)], 'L': [(1, OPEN), (14, FIST)]}, events={'grab': 14}, target='finished item on the bench')
    tong = P(upper_arm_L=(-.9, -.2, 0), forearm_L=(-.7, 0, -.35), hand_L=(.15, 0, 0))
    lift = merge(tong, spine(x=.25), look(x=.3), P(upper_arm_R=(-1.7, .15, 0), forearm_R=(-1.3, 0, 0), hand_R=(-.5, 0, 0)))
    strike = merge(tong, spine(x=.33), look(x=.35), crouch(.08), P(upper_arm_R=(-.8, .05, 0), forearm_R=(-.55, 0, 0), hand_R=(.35, 0, 0)))
    C['SmithRepair'] = dict(frames=24, loop=True, prop='Tool_Hammer', keys=[(1, strike), (5, merge(strike, P(upper_arm_R=(-.15, 0, 0)))), (15, lift), (20, strike), (25, strike)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'hit': 20}, target='workpiece fixed on the anvil (facility HandTarget)')
    sew = lambda t: merge(BENCH, spine(x=.08), look(x=.3), P(upper_arm_R=(-1.0 - .25 * max(0, S(TAU * t)), .15, 0), forearm_R=(-.75 - .45 * max(0, S(TAU * t)), 0, .25),
                                                             hand_R=(-.2 * S(TAU * t), 0, 0)))
    C['SewRepair'] = dict(frames=48, loop=True, prop=None, keys=K.cyc(48, sew, 8), grip={'R': [(1, .55)], 'L': [(1, .4)]},
                     events={'stitch': 12, 'stitch_2': 36}, target='cloth/leather item lying on the sewing table')

    # ---------------- food work ----------------------------------------------------------------------------------------
    press_up = merge(spine(x=-.05), P(upper_arm_L=(-2.0, -.15, 0), upper_arm_R=(-2.0, .15, 0), forearm_L=(-.6, 0, 0), forearm_R=(-.6, 0, 0)))
    press_dn = merge(spine(x=.4), crouch(.3), P(upper_arm_L=(-1.1, -.1, 0), upper_arm_R=(-1.1, .1, 0), forearm_L=(-.2, 0, 0), forearm_R=(-.2, 0, 0)))
    C['PressLever'] = dict(frames=32, loop=True, prop=None, keys=[(1, press_up), (18, press_dn), (24, press_dn), (33, press_up)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'press': 18}, target='oil/juice press lever (facility HandTarget)')
    peel_in = merge(spine(x=.18), crouch(.15), P(upper_arm_L=(-1.2, -.05, 0), upper_arm_R=(-1.2, .05, 0), forearm_L=(-.1, 0, 0), forearm_R=(-.1, 0, 0)))
    C['OvenLoad'] = dict(frames=32, loop=False, prop='peel', keys=[(1, BENCH), (12, merge(BENCH, spine(x=-.05))), (18, peel_in), (22, peel_in), (33, BENCH)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'insert': 18, 'withdraw': 26}, target='oven mouth')
    ladle_dn = merge(BENCH, spine(x=.25), P(upper_arm_R=(-.75, .2, 0), forearm_R=(-.4, 0, 0), hand_R=(.4, 0, 0)))
    ladle_up = merge(BENCH, P(upper_arm_R=(-1.3, .1, 0), forearm_R=(-.9, 0, 0), hand_R=(-.3, 0, 0)))
    pour = merge(BENCH, spine(z=-.15), P(upper_arm_R=(-1.1, -.1, -.4), forearm_R=(-.7, 0, 0), hand_R=(.6, 0, .4)))
    C['Ladle'] = dict(frames=28, loop=True, prop='ladle', keys=[(1, ladle_up), (10, ladle_dn), (16, ladle_up), (22, pour), (29, ladle_up)],
                     grip={'R': [(1, FIST)], 'L': [(1, .4)]}, events={'scoop': 10, 'pour': 22}, target='pot → bowl')
    knead = lambda t: merge(BENCH, spine(x=.2 + .1 * max(0, S(TAU * t))), P(upper_arm_L=(-1.0 + .15 * S(TAU * t), -.1, 0), upper_arm_R=(-1.0 + .15 * S(TAU * t), .1, 0)))
    C['Knead'] = dict(frames=32, loop=True, prop=None, keys=K.cyc(32, knead, 8), grip={'R': [(1, .3)], 'L': [(1, .3)]}, events={'press': 8}, target='dough on the bench')

    # ---------------- brewing / distilling ------------------------------------------------------------------------------
    sack_up = merge(spine(x=-.1), P(upper_arm_L=(-1.7, -.25, 0), upper_arm_R=(-1.7, .25, 0), forearm_L=(-.9, 0, -.2), forearm_R=(-.9, 0, .2)))
    sack_tip = merge(spine(x=.3), look(x=.2), P(upper_arm_L=(-1.5, -.2, 0), upper_arm_R=(-1.5, .2, 0), forearm_L=(-.4, 0, -.1), forearm_R=(-.4, 0, .1), hand_L=(.6, 0, 0), hand_R=(.6, 0, 0)))
    C['PourSack'] = dict(frames=32, loop=False, prop='sack', keys=[(1, CARRY_POSE), (10, sack_up), (16, sack_tip), (26, sack_tip), (33, IDLE)],
                     grip={'R': [(1, FIST), (30, OPEN)], 'L': [(1, FIST), (30, OPEN)]}, events={'pour_start': 16, 'pour_end': 26}, target='brewing barrel opening')
    tap = merge(crouch(.85), spine(x=.45), P(upper_arm_L=(-.9, -.1, 0), forearm_L=(-.6, 0, -.2), upper_arm_R=(-1.0, .1, 0), forearm_R=(-.9, 0, .3)))
    C['TapFill'] = dict(frames=40, loop=False, prop='jug', keys=[(1, IDLE), (10, tap), (14, merge(tap, P(hand_R=(0, 0, .6)))), (28, merge(tap, P(hand_R=(0, 0, .6)))), (32, tap), (41, CARRY_POSE)],
                     grip={'R': [(1, OPEN), (10, FIST)], 'L': [(1, FIST)]}, events={'tap_open': 14, 'tap_close': 32}, target='barrel/still tap, jug under it')
    valve = lambda t: merge(BENCH, spine(x=.1), P(upper_arm_R=(-1.15, .2 + .2 * S(TAU * t), .3 * math.cos(TAU * t)), forearm_R=(-.7, 0, 0), upper_arm_L=(-1.15, -.2 + .2 * S(TAU * t), .3 * math.cos(TAU * t)), forearm_L=(-.7, 0, 0)))
    C['ValveTurn'] = dict(frames=24, loop=True, prop=None, keys=K.cyc(24, valve, 8), grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'turn': 12},
                     target='valve wheel (still, gas extractor, tank)')

    # ---------------- drinking and its after-effects ------------------------------------------------------------------
    hold = P(upper_arm_R=(-.55, .1, 0), forearm_R=(-1.2, 0, .3))
    sip = merge(look(x=-.35), spine(x=-.08), P(upper_arm_R=(-1.15, .35, .35), forearm_R=(-2.0, 0, .2), hand_R=(-.3, 0, 0)))
    C['Drink'] = dict(frames=48, loop=False, prop='cup', keys=[(1, hold), (8, hold), (18, sip), (30, merge(sip, look(x=-.1))), (38, hold), (49, hold)],
                     grip={'R': [(1, .6)]}, events={'lift': 8, 'sip': 20, 'lower': 36}, target='cup rim at the mouth')
    C['WalkTipsy'] = dict(frames=32, loop=True, prop=None, keys=K.cyc(32, lambda t: merge(K.walk(t, stride=.45, lift=.4, arm=.5), spine(y=.12 * S(TAU * t), z=.08 * S(TAU * t / 2 * 2)),
                     look(y=.15 * S(TAU * t + .7), x=-.05)), 8), events={}, target='tipsy (취기) walk')
    sway = lambda t: merge(spine(y=.25 * S(TAU * t), x=.12), look(y=-.2 * S(TAU * t), x=.2), crouch(.15 + .1 * max(0, S(TAU * t))),
                           P(upper_arm_L=(-.2, -.6 * max(0, S(TAU * t)), 0), upper_arm_R=(-.2, .6 * max(0, -S(TAU * t)), 0), thigh_L=(-.2 * S(TAU * t), 0, 0)))
    C['Stagger'] = dict(frames=40, loop=True, prop=None, keys=K.cyc(40, sway, 8), events={}, target='drunk (만취) stagger in place')
    C['PassOut'] = dict(frames=40, loop=False, prop=None, keys=[(1, sway(0)), (10, sway(.25)), (18, merge(crouch(.7), spine(x=.3))), (28, merge(LIE_SIDE, spine(x=.1))),
                     (41, merge(LIE_SIDE, spine(x=.12), look(x=.1)))], events={'down': 28}, target='drunk collapse; Downed idle afterwards')
    C['HangoverIdle'] = dict(frames=72, loop=True, prop=None, keys=K.cyc(72, lambda t: merge(K.breathe(t, .5), spine(x=.18), look(x=.3 + .05 * S(TAU * t)),
                     P(upper_arm_R=(-1.1, .25, .3), forearm_R=(-2.1, 0, .3), Ear_L=(.4, 0, 0), Ear_R=(.4, 0, 0))), 8), events={}, target='hangover: hand on forehead, ears down')
    C['Crave'] = dict(frames=48, loop=True, prop=None, keys=K.cyc(48, lambda t: merge(K.breathe(t, 1.3), look(z=.35 * S(TAU * t), x=.05), spine(z=.1 * S(TAU * t)),
                     P(upper_arm_L=(-.35, .1, 0), forearm_L=(-.9, 0, -.5), upper_arm_R=(-.35, -.1, 0), forearm_R=(-.9, 0, .5))), 8), events={}, target='alcohol craving: restless looking around')

    # ---------------- machines and containers -------------------------------------------------------------------------
    panel = lambda t: merge(BENCH, spine(x=.06), P(upper_arm_R=(-1.05 - .15 * max(0, S(TAU * t)), .15, 0), forearm_R=(-.55, 0, 0), hand_R=(.2 * max(0, S(TAU * t)), 0, 0)))
    C['ControlPanel'] = dict(frames=36, loop=True, prop=None, keys=K.cyc(36, panel, 6), grip={'R': [(1, .25)], 'L': [(1, .5)]}, events={'press': 9, 'lever': 27},
                     target='prospector / extractor control panel (facility HandTarget)')
    drive = lambda t: merge(SEAT, spine(x=.12), look(z=.12 * S(TAU * t)), P(upper_arm_L=(-.95 + .2 * S(TAU * t), -.1, 0), upper_arm_R=(-.95 - .2 * S(TAU * t), .1, 0),
                                                                           forearm_L=(-.6, 0, 0), forearm_R=(-.6, 0, 0)))
    C['DriveSeated'] = dict(frames=48, loop=True, prop=None, keys=K.cyc(48, drive, 8), grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={},
                     target='deep drill driver seat, both levers (facility SeatPoint + HandTarget)')
    can_dn = merge(crouch(.9), spine(x=.5), P(upper_arm_L=(-.75, -.1, 0), upper_arm_R=(-.75, .1, 0), forearm_L=(-.45, 0, 0), forearm_R=(-.45, 0, 0)))
    can_tw = merge(can_dn, P(hand_R=(0, 0, .8), forearm_R=(-.6, 0, .3)))
    C['CanisterConnect'] = dict(frames=40, loop=False, prop='canister', keys=[(1, CARRY_POSE), (12, can_dn), (18, can_dn), (26, can_tw), (30, can_tw), (41, IDLE)],
                     grip={'R': [(1, FIST), (13, OPEN), (20, FIST)], 'L': [(1, FIST), (13, OPEN)]}, events={'place': 13, 'connect': 26}, target='filling/discharge coupling at floor level')
    C['CanisterDisconnect'] = dict(frames=40, loop=False, prop='canister', keys=[(1, IDLE), (10, can_tw), (16, can_dn), (26, can_dn), (30, merge(CARRY_POSE, crouch(.4))), (41, CARRY_POSE)],
                     grip={'R': [(1, OPEN), (8, FIST), (16, OPEN), (24, FIST)], 'L': [(1, OPEN), (24, FIST)]}, events={'disconnect': 12, 'lift': 26}, target='coupling → carry')

    # ---------------- six tool jobs (existing chop/mine/hammer/groundwork poses, now with grips, events, left-hand IK) -----
    up = merge(spine(x=-.18, z=.30), look(x=-.12, z=.15), hips(-.02), P(pevis=(0, 0, .10)),
               P(upper_arm_R=(-2.9, .3, 0), forearm_R=(-.75, 0, 0), hand_R=(-.45, 0, 0), upper_arm_L=(-2.7, -.35, 0), forearm_L=(-.85, 0, 0), hand_L=(-.3, 0, 0)),
               P(thigh_L=(-.12, 0, 0), shin_L=(.25, 0, 0), thigh_R=(.12, 0, 0)))
    hit = merge(spine(x=.32, z=-.18), look(x=.15), crouch(.25), P(pevis=(0, 0, -.12)),
                P(upper_arm_R=(-.95, .05, 0), forearm_R=(-.35, 0, 0), hand_R=(.25, 0, 0), upper_arm_L=(-.8, -.05, 0), forearm_L=(-.45, 0, 0), hand_L=(.2, 0, 0)))
    stuck = merge(hit, spine(x=.06), P(upper_arm_R=(.08, 0, 0), upper_arm_L=(.08, 0, 0)))
    C['Tool_Chop'] = dict(frames=40, loop=True, prop='Tool_Axe', two_hand=True, keys=two_hand_tool(up, hit, stuck), grip={'R': [(1, FIST)], 'L': [(1, FIST)]},
                     events={'hit': 19}, target='tree trunk / stump at the contact point')
    C['Tool_Mine'] = dict(frames=40, loop=True, prop='Tool_Pickaxe', two_hand=True, keys=two_hand_tool(merge(up, spine(x=-.05)), merge(hit, spine(x=.12)), merge(stuck, crouch(.1))),
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'hit': 19}, target='ore vein / rock at the contact point')
    steady = P(upper_arm_L=(-.8, .15, 0), forearm_L=(-.85, 0, -.2), hand_L=(.2, 0, 0))
    hl = merge(steady, spine(x=.28), look(x=.25), P(upper_arm_R=(-1.65, .15, 0), forearm_R=(-1.35, 0, 0), hand_R=(-.45, 0, 0)))
    hs = merge(steady, spine(x=.36), look(x=.32), crouch(.08), P(upper_arm_R=(-.7, .05, 0), forearm_R=(-.6, 0, 0), hand_R=(.3, 0, 0)))
    C['Tool_Hammer'] = dict(frames=22, loop=True, prop='Tool_Hammer', keys=[(1, hs), (5, merge(hs, P(upper_arm_R=(-.15, 0, 0)))), (14, hl), (19, hs), (23, hs)],
                     grip={'R': [(1, FIST)], 'L': [(1, .5)]}, events={'hit': 19}, target='construction / repair / demolition surface')
    dig_in = merge(spine(x=.35), crouch(.3), P(upper_arm_R=(-.55, .1, 0), forearm_R=(-.5, 0, 0), upper_arm_L=(-1.0, -.1, 0), forearm_L=(-.3, 0, 0), thigh_R=(-.4, 0, 0), shin_R=(.3, 0, 0)))
    dig_lift = merge(spine(x=.1, z=.35), look(z=.2), P(upper_arm_R=(-1.0, .2, .3), forearm_R=(-.9, 0, 0), upper_arm_L=(-1.4, -.2, .3), forearm_L=(-.6, 0, 0)))
    C['Tool_Dig'] = dict(frames=40, loop=True, prop='Tool_Shovel', two_hand=True, keys=[(1, dig_lift), (10, merge(dig_in, spine(x=-.1))), (16, dig_in), (24, merge(dig_in, spine(x=-.15))), (32, dig_lift), (41, dig_lift)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'hit': 16, 'toss': 32}, target='sand / clay / soil at the contact point')
    till_up = merge(spine(x=-.05), P(upper_arm_R=(-1.9, .15, 0), forearm_R=(-.7, 0, 0), upper_arm_L=(-1.6, -.15, 0), forearm_L=(-.8, 0, 0)))
    till_hit = merge(spine(x=.4), crouch(.45), P(upper_arm_R=(-.6, .05, 0), forearm_R=(-.3, 0, 0), upper_arm_L=(-.7, -.05, 0), forearm_L=(-.4, 0, 0)))
    C['Tool_Till'] = dict(frames=36, loop=True, prop='Tool_Hoe', two_hand=True, keys=[(1, till_hit), (12, till_up), (17, till_hit), (24, merge(till_hit, spine(x=.06), P(upper_arm_R=(.1, 0, 0)))), (37, till_hit)],
                     grip={'R': [(1, FIST)], 'L': [(1, FIST)]}, events={'hit': 17}, target='field tile soil (밭 조성·파종)')
    reap0 = merge(crouch(.55), spine(x=.3, z=.35), P(upper_arm_R=(-.6, .4, .3), forearm_R=(-.4, 0, 0), upper_arm_L=(-.9, -.1, 0), forearm_L=(-.6, 0, -.3)))
    reap1 = merge(crouch(.55), spine(x=.35, z=-.3), P(upper_arm_R=(-.9, -.2, -.4), forearm_R=(-.3, 0, 0), upper_arm_L=(-.9, -.1, 0), forearm_L=(-.6, 0, -.3)))
    C['Tool_Harvest'] = dict(frames=28, loop=True, prop='Tool_Sickle', keys=[(1, reap0), (9, reap0), (15, reap1), (20, reap1), (29, reap0)],
                     grip={'R': [(1, FIST)], 'L': [(1, .7)]}, events={'hit': 15}, target='crop stalks at the contact point')

    # ---------------- 2026-10-24 새 기획(G11·G34·G54·G55·G56·G60·G61) 동작 ------------------------------------------------
    bow_low = P(upper_arm_L=(-.4, -.1, 0), forearm_L=(-.3, 0, 0), upper_arm_R=(-.2, .1, 0))
    bow_aim = merge(spine(z=.3), look(z=-.25), P(upper_arm_L=(-1.3, -.3, .5), forearm_L=(-.2, 0, 0), upper_arm_R=(-1.2, .3, .3), forearm_R=(-1.1, 0, .3)))
    bow_draw = merge(bow_aim, P(upper_arm_R=(-1.45, .55, .35), forearm_R=(-1.9, 0, .3)))
    bow_loose = merge(bow_aim, P(upper_arm_R=(-1.35, .75, .45), forearm_R=(-1.5, 0, .3), hand_R=(-.3, 0, 0)))
    C['BowShoot'] = dict(frames=36, loop=True, prop='Weapon_ShortBow', keys=[(1, bow_low), (8, bow_aim), (18, bow_draw), (21, bow_draw), (23, bow_loose), (30, bow_aim), (37, bow_low)],
                     grip={'L': [(1, FIST)], 'R': [(1, .5), (9, .7), (22, .7), (23, OPEN), (32, .5)]}, events={'nock': 9, 'release': 23},
                     target='단궁 Socket.Tool.L, 화살은 nock에서 Socket.Tool.R에 보이고 release에서 발사체로 교체(G34 사거리 12 m)')
    beat = lambda t: merge(crouch(.7), spine(x=.4), look(x=.3), P(upper_arm_R=(-1.6 + .9 * max(0, S(TAU * t)), .1, 0), forearm_R=(-.6, 0, 0),
                                                              upper_arm_L=(-1.6 + .9 * max(0, -S(TAU * t)), -.1, 0), forearm_L=(-.6, 0, 0)))
    C['BeatFire'] = dict(frames=24, loop=True, prop='cloth', keys=K.cyc(24, beat, 6), grip={'R': [(1, .6)], 'L': [(1, .6)]}, events={'hit': 6, 'hit_2': 18},
                     target='작은 불 1칸을 두드려 끔(G56, 물·도구 없이 5초)')
    treat = lambda t: merge(FLOOR_SIT, spine(x=.35), look(x=.45), P(upper_arm_L=(-.7, -.05, 0), forearm_L=(-.9, 0, -.4),
                                                            upper_arm_R=(-.8 + .15 * S(TAU * t), .15 + .15 * math.cos(TAU * t), 0), forearm_R=(-.9, 0, .4)))
    C['SelfTreat'] = dict(frames=48, loop=True, prop='bandage', keys=K.cyc(48, treat, 8), grip={'R': [(1, .5)], 'L': [(1, .3)]}, events={'wrap': 12, 'wrap_2': 36},
                     target='의식 있는 장수의 자가 처치·투약(G11·G55, 작업량 2배). 앉아 다리/팔에 붕대 감기')
    gaze = lambda t: merge(K.breathe(t, .6), look(x=-.55 + .05 * S(TAU * t), z=.15 * S(TAU * t / 2 * 2)), spine(x=-.1),
                           P(upper_arm_L=(.25, .1, 0), forearm_L=(-.6, 0, .5), upper_arm_R=(.25, -.1, 0), forearm_R=(-.6, 0, -.5)))
    C['SkyGaze'] = dict(frames=96, loop=True, prop=None, keys=K.cyc(96, gaze, 8), events={}, target='무시설 오락 하늘 보기(G54), 손은 등 뒤')
    squat = merge(SQUAT, spine(x=.25), look(x=.1), P(upper_arm_L=(-.6, -.2, 0), forearm_L=(-.8, 0, 0), upper_arm_R=(-.6, .2, 0), forearm_R=(-.8, 0, 0)))
    C['LatrineSquat'] = dict(frames=72, loop=False, prop=None, keys=[(1, IDLE), (12, squat), (36, merge(squat, look(z=.2))), (52, squat), (64, merge(crouch(.4), spine(x=.1))), (73, IDLE)],
                     events={'sit': 12, 'stand': 64}, target='구덩이 변소·임시 용변(G60), 시설 WorkPoint 위')
    lie = merge(LIE_SIDE, spine(x=.12), look(x=.1))
    sit_up = merge(FLOOR_SIT, spine(x=.25), look(x=.2), P(upper_arm_L=(-.2, -.3, 0), upper_arm_R=(-.2, .3, 0)))
    C['WakeUp'] = dict(frames=48, loop=False, prop=None, keys=[(1, lie), (10, merge(lie, look(x=-.1))), (22, sit_up), (34, merge(SQUAT, spine(x=.3))), (42, merge(crouch(.2), spine(x=.08))), (49, IDLE)],
                     events={'conscious': 10, 'up': 42}, target='쓰러짐 1일 뒤 의식 회복(G11, 개인 체력 20%). PassOut/Downed 다음에 재생')
    return C


def create(rig):
    """Build 'PK|<clip>' actions (body keys + finger curls). Returns {name: spec}."""
    import bpy
    from mathutils import Vector, Euler
    rig.animation_data_create(); out = {}
    fingers = {s: [b.name for b in rig.pose.bones if (b.name.startswith(('f_', 'thumb.')) and b.name.endswith('.' + s))] for s in 'LR'}
    for name, c in clips().items():
        act = bpy.data.actions.new('PK|' + name); act.use_fake_user = True
        rig.animation_data.action = act; prev = {}
        for frame, pose in c['keys']:
            pose = merge(K.RELAX, pose)
            for bn in K.DRIVEN:
                pb = rig.pose.bones[bn]; basis = pb.bone.matrix_local.to_3x3()
                e = (basis.inverted() @ Euler(pose.get(bn, (0, 0, 0))).to_matrix() @ basis).to_euler('XYZ', prev.get(bn, Euler()))
                prev[bn] = e; pb.rotation_mode = 'XYZ'; pb.rotation_euler = e
                pb.location = basis.inverted() @ Vector(pose.get(bn + '_loc', (0, 0, 0)))
                pb.keyframe_insert('rotation_euler', frame=frame, group=bn); pb.keyframe_insert('location', frame=frame, group=bn)
        for side, keys in c.get('grip', {}).items():
            for frame, curl in keys:
                for bn in fingers[side]:
                    pb = rig.pose.bones[bn]; pb.rotation_mode = 'XYZ'; th = bn.startswith('thumb.')
                    pb.rotation_euler = (curl * (THUMB if th else PINKY if 'pinky' in bn else 1), 0, -.18 * curl / FIST if th else 0)
                    pb.keyframe_insert('rotation_euler', frame=frame, group=bn)
            for bn in fingers[side]:  # hold the last grip to the end
                rig.pose.bones[bn].keyframe_insert('rotation_euler', frame=c['frames'] + 1, group=bn)
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        constant = fc.data_path.split('"')[1].startswith(('f_', 'thumb.')) if '"' in fc.data_path else False
                        for kp in fc.keyframe_points:
                            kp.interpolation = 'CONSTANT' if constant and len(fc.keyframe_points) > 2 else 'BEZIER'
                            kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
                        fc.update()
                        if c['loop']: fc.modifiers.new('CYCLES')
        act['loop'] = c['loop']; act['duration_frames'] = c['frames']; act['fps'] = 24
        act['events'] = c.get('events', {}); act['prop'] = c.get('prop') or ''; act['two_hand'] = bool(c.get('two_hand'))
        act['target'] = c.get('target', '')
        out[name] = c
    rig.animation_data.action = None
    for pb in rig.pose.bones: pb.matrix_basis.identity()
    return out
