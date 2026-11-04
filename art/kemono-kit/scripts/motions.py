"""Key-pose motions for the kemono biped (replaces the old resident recipe adaptation).

Poses are armature-space Euler angles per bone (same convention as the old animations.py):
the character faces -Y, +X is its left. X: +bends spine/head forward, +swings a hanging limb
back, +bends the knee (shin) back. Y: tilts toward +X; raises the left arm sideways with -Y,
the right arm with +Y. Z: +turns toward the character's left.
Keys are eased (Bezier, auto-clamped); loops repeat their first key at the end."""
import math
from mathutils import Vector, Euler

TAU = math.tau
ARMS = {'upper_arm', 'forearm', 'hand', 'shoulder'}
DRIVEN = ['Root', 'pevis', 'spine', 'spine_01', 'spine_02', 'neck', 'head', 'Tail', 'Ear.L', 'Ear.R'] + \
    [f'{n}.{s}' for s in 'LR' for n in ('shoulder', 'upper_arm', 'forearm', 'hand', 'thigh', 'shin', 'foot')]
# Arms hang naturally instead of the A-pose rest: bring each upper arm in toward the body.
RELAX = {'upper_arm.L': (0, .42, 0), 'upper_arm.R': (0, -.42, 0), 'forearm.L': (-.12, 0, 0), 'forearm.R': (-.12, 0, 0)}


def P(**kw):
    """Pose from keyword shorthands: dots become underscores in names (upper_arm_L → upper_arm.L)."""
    out = {}
    for k, v in kw.items():
        name = k.replace('_L', '.L').replace('_R', '.R') if k.endswith(('_L', '_R')) else k
        out[name] = tuple(v) if not isinstance(v, (int, float)) else (v, 0, 0)
    return out


def merge(*poses):
    out = {}
    for p in poses:
        for k, v in p.items():
            a = out.get(k, (0, 0, 0)); out[k] = tuple(a[i] + v[i] for i in range(len(v)))
    return out


def mirror(p):
    """Swap L/R and flip the Y/Z angles (and X location) so a right-handed pose becomes left-handed."""
    out = {}
    for k, v in p.items():
        n = k.replace('.L', '.T').replace('.R', '.L').replace('.T', '.R')
        out[n] = (-v[0], v[1], v[2]) if k.endswith('_loc') else (v[0], -v[1], -v[2])
    return out


def spine(x=0, y=0, z=0):
    """Spread a torso bend over the three spine bones (more toward the chest)."""
    return {'spine': (x * .25, y * .25, z * .2), 'spine_01': (x * .35, y * .35, z * .35), 'spine_02': (x * .4, y * .4, z * .45)}


def look(x=0, y=0, z=0):
    return {'neck': (x * .4, y * .4, z * .4), 'head': (x * .6, y * .6, z * .6)}


def hips(z=0, x=0, rx=0, rz=0):
    """Lower/shift the pelvis (location, armature units) and rotate it."""
    return {'pevis_loc': (x, 0, z), 'pevis': (rx, 0, rz)}


def crouch(k):
    """Bend both knees by k (0..1) keeping the feet on the floor: thighs forward, shins back, feet level."""
    return {**hips(-.13 * k), **{f'thigh.{s}': (-.95 * k, 0, 0) for s in 'LR'},
            **{f'shin.{s}': (1.55 * k, 0, 0) for s in 'LR'}, **{f'foot.{s}': (-.6 * k, 0, 0) for s in 'LR'}}


SEAT = merge({f'thigh.{s}': (-1.45, 0, 0) for s in 'LR'}, {f'shin.{s}': (1.45, 0, 0) for s in 'LR'})
ARMS_FWD = P(upper_arm_L=(-.75, -.05, 0), upper_arm_R=(-.75, .05, 0), forearm_L=(-.55, 0, 0), forearm_R=(-.55, 0, 0))


def breathe(t, amt=1.0):
    s = math.sin(TAU * t)
    return merge(spine(x=-.025 * amt * s), look(x=.02 * amt * s), P(shoulder_L=(0, 0, .02 * amt * s), shoulder_R=(0, 0, -.02 * amt * s)))


# ---------- clip definitions: name -> (frames, loop, [(frame, pose)]) ----------

def cyc(frames, fn, n=8):
    """Sample a periodic pose function at n evenly spaced keys (frame 1 .. frames+1)."""
    return [(1 + round(frames * i / n), fn(i / n)) for i in range(n + 1)]


def walk(t, stride=.55, lift=.55, arm=.38, bounce=.018, lean=.06):
    s, c = math.sin(TAU * t), math.cos(TAU * t)
    p = {}
    for side, sg in (('L', 1), ('R', -1)):
        ph = s * sg  # >0: this leg swings back
        fwd = max(0, -math.sin(TAU * t + .9) * sg)  # knee bends while the leg passes forward
        p[f'thigh.{side}'] = (stride * ph, 0, 0)
        p[f'shin.{side}'] = (lift * fwd + .08, 0, 0)
        p[f'foot.{side}'] = (-.25 * fwd + .12 * max(0, ph), 0, 0)
        p[f'upper_arm.{side}'] = (-arm * ph, 0, 0)
        p[f'forearm.{side}'] = (-.25 - .15 * max(0, -ph), 0, 0)
    return merge(p, hips(-bounce * abs(c)), spine(x=lean, z=-.10 * s), P(pevis=(0, 0, .08 * s)), look(x=-lean * .5, z=.06 * s),
                 P(Tail=(.12 * c, 0, .2 * s)), P(Ear_L=(.08 * abs(c), 0, 0), Ear_R=(.08 * abs(c), 0, 0)))


def clips():
    C = {}
    C['Idle'] = (72, True, cyc(72, lambda t: merge(breathe(t), P(Tail=(0, 0, .12 * math.sin(TAU * t))), look(z=.05 * math.sin(TAU * t)))))
    C['StandIdle'] = (72, True, cyc(72, lambda t: merge(breathe(t), P(pevis=(0, 0, .03 * math.sin(TAU * t))), look(x=.03, z=-.08 * math.sin(TAU * t)),
                                                        P(Tail=(0, 0, .1 * math.sin(TAU * t))))))
    C['Walk'] = (24, True, cyc(24, walk, 8))
    C['Run'] = (16, True, cyc(16, lambda t: walk(t, stride=.85, lift=1.25, arm=.75, bounce=.035, lean=.22), 8))
    C['Carry'] = (24, True, cyc(24, lambda t: merge({k: v for k, v in walk(t, arm=0, lean=-.03).items() if not k.startswith(('upper_arm', 'forearm'))},
                                                     P(upper_arm_L=(-.55, .2, 0), upper_arm_R=(-.55, -.2, 0), forearm_L=(-1.1, 0, -.25), forearm_R=(-1.1, 0, .25))), 8))

    # Chop: wind up over the right shoulder (slow), drive down hard (fast), stick, pull free, reset.
    up = merge(spine(x=-.18, z=.30), look(x=-.12, z=.15), hips(-.02), P(pevis=(0, 0, .10)),
               P(upper_arm_R=(-2.9, .3, 0), forearm_R=(-.75, 0, 0), hand_R=(-.45, 0, 0),
                 upper_arm_L=(-2.7, -.35, 0), forearm_L=(-.85, 0, 0), hand_L=(-.3, 0, 0)),
               P(thigh_L=(-.12, 0, 0), shin_L=(.25, 0, 0), thigh_R=(.12, 0, 0)))
    hit = merge(spine(x=.32, z=-.18), look(x=.15), crouch(.25), P(pevis=(0, 0, -.12)),
                P(upper_arm_R=(-.95, .05, 0), forearm_R=(-.35, 0, 0), hand_R=(.25, 0, 0),
                  upper_arm_L=(-.8, -.05, 0), forearm_L=(-.45, 0, 0), hand_L=(.2, 0, 0)))
    stuck = merge(hit, spine(x=.06), P(upper_arm_R=(.08, 0, 0), upper_arm_L=(.08, 0, 0)))
    pull = merge(spine(x=.25), look(x=.1), crouch(.15), P(upper_arm_R=(-1.15, .1, 0), forearm_R=(-.6, 0, 0), upper_arm_L=(-1.0, -.1, 0), forearm_L=(-.7, 0, 0)))
    C['Chop'] = (40, True, [(1, pull), (15, up), (20, hit), (24, stuck), (32, pull), (41, pull)])
    C['Mine'] = (40, True, [(1, pull), (14, merge(up, spine(x=-.05))), (19, merge(hit, spine(x=.12))), (23, merge(stuck, crouch(.1))), (32, pull), (41, pull)])

    # Hammer: one-handed, elbow lifts, wrist snaps on the strike; free hand steadies the work.
    steady = P(upper_arm_L=(-.8, .15, 0), forearm_L=(-.85, 0, -.2), hand_L=(.2, 0, 0))
    lift = merge(steady, spine(x=.28), look(x=.25), P(upper_arm_R=(-1.65, .15, 0), forearm_R=(-1.35, 0, 0), hand_R=(-.45, 0, 0)))
    strike = merge(steady, spine(x=.36), look(x=.32), crouch(.08), P(upper_arm_R=(-.7, .05, 0), forearm_R=(-.6, 0, 0), hand_R=(.3, 0, 0)))
    C['Hammer'] = (22, True, [(1, strike), (5, merge(strike, P(upper_arm_R=(-.15, 0, 0)))), (14, lift), (19, strike), (23, strike)])

    C['Sit'] = (72, True, cyc(72, lambda t: merge(SEAT, breathe(t), spine(x=.05), P(upper_arm_L=(-.35, .05, 0), upper_arm_R=(-.35, -.05, 0),
                                                                                forearm_L=(-.7, 0, 0), forearm_R=(-.7, 0, 0)), look(z=.08 * math.sin(TAU * t)),
                                                       P(foot_L=(.15 * max(0, math.sin(TAU * t)), 0, 0)))))

    S = math.sin
    def ease(x): return x * x * (3 - 2 * x)
    def pulse(t, up=.3):
        """0 → 1 quickly over the first `up` of the cycle, then eases back: a press or strike."""
        return ease(t / up) if t < up else ease(1 - (t - up) / (1 - up))
    lap = P(upper_arm_L=(-.35, .05, 0), upper_arm_R=(-.35, -.05, 0), forearm_L=(-.8, 0, -.1), forearm_R=(-.8, 0, .1))
    bench = P(upper_arm_L=(-.6, .1, 0), upper_arm_R=(-.6, -.1, 0), forearm_L=(-.95, 0, -.2), forearm_R=(-.95, 0, .2))
    SQUAT = merge(hips(-.24), {f'thigh.{s}': (-1.5, .12 if s == 'L' else -.12, 0) for s in 'LR'},
                  {f'shin.{s}': (2.3, 0, 0) for s in 'LR'}, {f'foot.{s}': (-.8, 0, 0) for s in 'LR'})
    tail = lambda t, a=.15: P(Tail=(.05 * S(TAU * t), 0, a * S(TAU * t)))

    # --- standing transitions and carrying ---
    C['StandEnter'] = (10, False, [(1, merge(spine(x=.08), P(upper_arm_L=(.25, 0, 0), upper_arm_R=(-.25, 0, 0)), P(thigh_L=(.2, 0, 0), thigh_R=(-.2, 0, 0)))),
                                   (11, breathe(0))])
    C['StandExit'] = (10, False, [(1, breathe(0)), (11, merge(spine(x=.08), P(upper_arm_L=(-.2, 0, 0), upper_arm_R=(.2, 0, 0)), P(thigh_L=(-.25, 0, 0), shin_L=(.3, 0, 0))))])
    hold = P(upper_arm_L=(-.6, .2, 0), upper_arm_R=(-.6, -.2, 0), forearm_L=(-1.1, 0, -.3), forearm_R=(-1.1, 0, .3))
    low = merge(SQUAT, spine(x=.45), look(x=-.15), P(upper_arm_L=(-.7, .1, 0), upper_arm_R=(-.7, -.1, 0), forearm_L=(-.35, 0, -.2), forearm_R=(-.35, 0, .2)))
    C['Pickup'] = (28, False, [(1, breathe(0)), (10, low), (14, merge(low, P(forearm_L=(-.15, 0, 0), forearm_R=(-.15, 0, 0)))), (22, merge(crouch(.2), spine(x=.1), hold)), (29, hold)])
    C['PutDown'] = (28, False, [(1, hold), (12, merge(low, P(forearm_L=(-.15, 0, 0), forearm_R=(-.15, 0, 0)))), (16, low), (24, merge(crouch(.15), spine(x=.08))), (29, breathe(0))])
    C['CarryPerson'] = (28, True, cyc(28, lambda t: merge({k: v for k, v in walk(t, stride=.35, arm=0, lean=-.12, bounce=.012).items() if not k.startswith(('upper_arm', 'forearm'))},
                                                           P(upper_arm_L=(-1.0, .25, 0), upper_arm_R=(-1.0, -.25, 0), forearm_L=(-1.0, 0, -.35), forearm_R=(-1.0, 0, .35)), look(x=.1)), 8))

    # --- crafts and chores ---
    C['GroundWork'] = (40, True, cyc(40, lambda t: merge(SQUAT, spine(x=.5), look(x=-.1, z=.1 * S(TAU * t)),
        P(upper_arm_R=(-.55 - .35 * max(0, S(TAU * t)), -.05, 0), forearm_R=(-.25 - .5 * max(0, S(TAU * t)), 0, .15),
          upper_arm_L=(-.55 - .35 * max(0, -S(TAU * t)), .05, 0), forearm_L=(-.25 - .5 * max(0, -S(TAU * t)), 0, -.15)), tail(t, .08))))
    C['BenchWork'] = (40, True, cyc(40, lambda t: merge(bench, spine(x=.22, z=.06 * S(TAU * t)), look(x=.3, z=.05 * S(TAU * t)), P(pevis=(0, 0, .04 * S(TAU * t))),
        P(upper_arm_R=(-.18 * max(0, S(TAU * t)), 0, 0), upper_arm_L=(-.18 * max(0, -S(TAU * t)), 0, 0)), tail(t, .06))))
    C['Treat'] = (48, True, cyc(48, lambda t: merge(bench, spine(x=.32, y=.04 * S(TAU * t)), look(x=.35, y=.08 * S(TAU * t)), crouch(.1),
        P(upper_arm_R=(-.1 + .12 * S(TAU * t), 0, .12 * math.cos(TAU * t)), hand_R=(.15 * S(2 * TAU * t), 0, 0), upper_arm_L=(-.05, 0, 0)))))
    C['Operate'] = (48, True, [(1, merge(bench, spine(x=.38), look(x=.45), P(upper_arm_R=(-.1, 0, 0), hand_R=(.2, 0, 0)))),
                               (14, merge(bench, spine(x=.4), look(x=.48), P(upper_arm_R=(.05, 0, -.08), hand_R=(.35, 0, 0)))),
                               (22, merge(bench, spine(x=.4), look(x=.48), P(upper_arm_R=(.05, 0, .08), hand_R=(.35, 0, 0)))),
                               (34, merge(bench, spine(x=.3), look(x=.35, z=-.12), P(upper_arm_R=(-.2, 0, 0), hand_R=(.1, 0, 0)))),
                               (49, merge(bench, spine(x=.38), look(x=.45), P(upper_arm_R=(-.1, 0, 0), hand_R=(.2, 0, 0))))])
    C['Stir'] = (48, True, cyc(48, lambda t: merge(bench, spine(x=.18, z=.07 * math.cos(TAU * t)), look(x=.3, z=.05 * math.cos(TAU * t)),
        P(upper_arm_R=(-.25 + .2 * S(TAU * t), 0, .25 * math.cos(TAU * t)), forearm_R=(-.1, 0, 0), hand_R=(-.2, 0, 0), upper_arm_L=(-.05, 0, 0)), tail(t, .06)), 12))
    C['Grind'] = (32, True, cyc(32, lambda t: merge(bench, spine(x=.2 + .1 * pulse(t, .35)), look(x=.3), crouch(.05 + .07 * pulse(t, .35)),
        P(upper_arm_R=(-.35 + .3 * pulse(t, .35), 0, .06 * S(TAU * t)), forearm_R=(-.35 + .25 * pulse(t, .35), 0, 0), upper_arm_L=(-.05, 0, 0))), 8))
    C['Saw'] = (28, True, cyc(28, lambda t: merge(bench, spine(x=.22 + .08 * S(TAU * t), z=-.06 * S(TAU * t)), look(x=.3), P(pevis_loc=(0, 0, -.01 * abs(S(TAU * t)))),
        P(upper_arm_R=(-.45 * S(TAU * t), 0, 0), forearm_R=(.55 * S(TAU * t), 0, 0), upper_arm_L=(.05, 0, 0)), P(thigh_R=(.12, 0, 0), thigh_L=(-.12, 0, 0), shin_L=(.15, 0, 0))), 8))
    press = merge(bench, spine(x=.32), look(x=.38), crouch(.06), P(upper_arm_R=(.1, 0, 0), forearm_R=(.2, 0, 0)))
    raise_ = merge(bench, spine(x=.18), look(x=.3), P(upper_arm_R=(-.55, 0, 0), forearm_R=(-.5, 0, 0)))
    C['Stamp'] = (36, True, [(1, merge(bench, spine(x=.2), look(x=.3))), (10, raise_), (14, press), (20, press),
                             (27, merge(raise_, look(x=.2, z=.12))), (37, merge(bench, spine(x=.2), look(x=.3)))])
    C['Wash'] = (40, True, cyc(40, lambda t: merge(bench, spine(x=.28), look(x=.32), crouch(.05),
        P(upper_arm_L=(.12 * S(TAU * 2 * t), 0, .1 * math.cos(TAU * 2 * t)), upper_arm_R=(-.12 * S(TAU * 2 * t), 0, -.1 * math.cos(TAU * 2 * t)),
          hand_L=(.2 * S(TAU * 2 * t), 0, 0), hand_R=(-.2 * S(TAU * 2 * t), 0, 0))), 12))
    C['WaterPlant'] = (60, True, [(1, merge(spine(x=.1), P(upper_arm_R=(-.75, -.1, 0), forearm_R=(-.6, 0, 0), hand_R=(.2, 0, 0)))),
                                  (14, merge(spine(x=.18), look(x=.25), P(upper_arm_R=(-.8, -.1, -.12), forearm_R=(-.5, 0, 0), hand_R=(1.15, 0, 0)))),
                                  (32, merge(spine(x=.2, z=-.08), look(x=.28, z=-.1), P(upper_arm_R=(-.8, -.1, .14), forearm_R=(-.5, 0, 0), hand_R=(1.2, 0, 0)))),
                                  (46, merge(spine(x=.1), look(x=.15), P(upper_arm_R=(-.75, -.1, 0), forearm_R=(-.6, 0, 0), hand_R=(.25, 0, 0)))),
                                  (61, merge(spine(x=.1), P(upper_arm_R=(-.75, -.1, 0), forearm_R=(-.6, 0, 0), hand_R=(.2, 0, 0))))])
    C['RockCradle'] = (48, True, cyc(48, lambda t: merge(spine(x=.42, z=.04 * S(TAU * t)), look(x=.2, y=.08 * S(TAU * t)), P(pevis_loc=(.015 * S(TAU * t), 0, 0)),
        P(upper_arm_L=(-.95, .1, .14 * S(TAU * t)), upper_arm_R=(-.95, -.1, .14 * S(TAU * t)), forearm_L=(-.3, 0, -.1), forearm_R=(-.3, 0, .1))), 8))
    C['Inspect'] = (72, True, [(1, merge(spine(x=.12), look(x=.15))),
                               (18, merge(spine(x=.22), look(x=.25, y=.14), P(upper_arm_R=(-.9, 0, 0), forearm_R=(-1.2, 0, .2)))),
                               (36, merge(spine(x=.25, z=.1), look(x=.2, y=-.12, z=.15), P(upper_arm_R=(-.9, 0, 0), forearm_R=(-1.25, 0, .2)))),
                               (54, merge(spine(x=.15, z=-.08), look(x=.12, z=-.18))),
                               (73, merge(spine(x=.12), look(x=.15)))])

    # --- force work (axe/pick shared above), spear, darts, drums, weights ---
    guard = merge(spine(x=.05), P(upper_arm_R=(-.75, 0, 0), forearm_R=(-.9, 0, 0), upper_arm_L=(-.6, 0, 0), forearm_L=(-.9, 0, -.2)))
    draw = merge(spine(x=-.05, z=.3), look(z=-.2), P(upper_arm_R=(.25, 0, 0), forearm_R=(-1.5, 0, 0), upper_arm_L=(-.9, 0, 0), forearm_L=(-.7, 0, -.2)))
    lunge = merge(spine(x=.25, z=-.25), look(x=-.1, z=.15), hips(-.06), P(upper_arm_R=(-1.45, 0, 0), forearm_R=(-.1, 0, 0), upper_arm_L=(-.4, 0, 0)),
                  P(thigh_L=(-.65, 0, 0), shin_L=(.75, 0, 0), foot_L=(-.1, 0, 0), thigh_R=(.4, 0, 0), shin_R=(.15, 0, 0), foot_R=(.2, 0, 0)))
    C['Attack'] = (26, False, [(1, guard), (9, draw), (13, lunge), (17, lunge), (27, guard)])
    aim = merge(spine(x=.02), look(x=-.05), P(upper_arm_R=(-1.65, 0, 0), forearm_R=(-1.55, 0, 0), upper_arm_L=(-.6, .1, 0), forearm_L=(-.4, 0, 0)))
    C['Throw'] = (30, True, [(1, aim), (10, merge(aim, spine(z=.18), P(forearm_R=(-.55, 0, 0), upper_arm_R=(-.15, 0, 0)))),
                             (13, merge(spine(x=.15, z=-.12), P(upper_arm_R=(-1.35, 0, 0), forearm_R=(-.25, 0, 0), upper_arm_L=(-.4, .1, 0)))),
                             (18, merge(spine(x=.18, z=-.15), P(upper_arm_R=(-.75, 0, 0), forearm_R=(-.15, 0, 0)))), (31, aim)])
    C['PlayMusic'] = (24, True, cyc(24, lambda t: merge(bench, spine(x=.12), look(x=.15 + .05 * abs(S(TAU * t))), P(pevis_loc=(0, 0, -.008 * abs(S(TAU * t)))),
        P(upper_arm_L=(-.35 * max(0, S(TAU * t)), 0, 0), forearm_L=(-.4 * max(0, S(TAU * t)), 0, 0), hand_L=(-.3 * max(0, S(TAU * t)), 0, 0),
          upper_arm_R=(-.35 * max(0, -S(TAU * t)), 0, 0), forearm_R=(-.4 * max(0, -S(TAU * t)), 0, 0), hand_R=(-.3 * max(0, -S(TAU * t)), 0, 0)), tail(t, .12)), 8))
    C['Exercise'] = (60, True, cyc(60, lambda t: merge(crouch(.85 * (1 - math.cos(TAU * t)) / 2), spine(x=.12 * (1 - math.cos(TAU * t)) / 2),
        P(upper_arm_L=(-2.9 + .7 * (1 - math.cos(TAU * t)) / 2, -.3, 0), upper_arm_R=(-2.9 + .7 * (1 - math.cos(TAU * t)) / 2, .3, 0),
          forearm_L=(-.2 - 1.9 * (1 - math.cos(TAU * t)) / 2, 0, 0), forearm_R=(-.2 - 1.9 * (1 - math.cos(TAU * t)) / 2, 0, 0))), 8))
    C['Wrestle'] = (32, True, cyc(32, lambda t: merge(crouch(.45), spine(x=.35, z=.15 * S(TAU * t)), look(x=-.2, z=-.1 * S(TAU * t)),
        P(pevis_loc=(.03 * S(TAU * t), 0, 0), pevis=(0, 0, .1 * S(TAU * t))),
        P(upper_arm_L=(-1.1 + .15 * S(TAU * t), .1, 0), upper_arm_R=(-1.1 - .15 * S(TAU * t), -.1, 0), forearm_L=(-.6, 0, -.2), forearm_R=(-.6, 0, .2)),
        P(thigh_L=(-.25, 0, 0), thigh_R=(.25, 0, 0)), tail(t, .2)), 8))
    C['Fly'] = (24, True, cyc(24, lambda t: merge(P(Root_loc=(0, 0, .25 + .03 * S(TAU * t))), spine(x=.3), look(x=-.25),
        P(thigh_L=(.3 + .1 * S(TAU * t), 0, 0), thigh_R=(.3 - .1 * S(TAU * t), 0, 0), shin_L=(.5, 0, 0), shin_R=(.6, 0, 0), foot_L=(.4, 0, 0), foot_R=(.4, 0, 0)),
        P(upper_arm_L=(.2, -.35 - .1 * S(TAU * t), 0), upper_arm_R=(.2, .35 + .1 * S(TAU * t), 0)),
        P(Ear_L=(.25 + .1 * S(TAU * t), 0, 0), Ear_R=(.25 + .1 * S(TAU * t), 0, 0)), tail(t, .25)), 8))

    # --- seated family ---
    seated = lambda t, *extra: merge(SEAT, breathe(t, .7), *extra)
    C['Rest'] = (96, True, cyc(96, lambda t: seated(t, spine(x=-.06), lap, look(x=.1 + .14 * pulse(t, .6), z=.08 * S(TAU * t)), tail(t, .05)), 8))
    reach = P(upper_arm_R=(-1.15, -.05, 0), forearm_R=(-.45, 0, .15))
    base_pb = merge(SEAT, spine(x=.18), look(x=.3), P(upper_arm_L=(-.6, .05, 0), forearm_L=(-.9, 0, -.25)))
    C['PlayBoard'] = (72, True, [(1, merge(base_pb, P(upper_arm_R=(-.9, 0, 0), forearm_R=(-1.9, 0, .3)), look(y=.08))),  # hand at the chin, thinking
                                 (24, merge(base_pb, P(upper_arm_R=(-.9, 0, 0), forearm_R=(-1.9, 0, .3)), look(y=-.06))),
                                 (34, merge(base_pb, reach, spine(x=.06))), (40, merge(base_pb, reach, spine(x=.08), P(hand_R=(.3, 0, 0)))),
                                 (52, merge(base_pb, lap)), (73, merge(base_pb, P(upper_arm_R=(-.9, 0, 0), forearm_R=(-1.9, 0, .3)), look(y=.08)))])
    spoon_low = P(upper_arm_R=(-.55, -.05, 0), forearm_R=(-.9, 0, .25))
    spoon_up = P(upper_arm_R=(-1.0, -.2, 0), forearm_R=(-2.1, 0, .45), hand_R=(-.4, 0, 0))
    bowl = P(upper_arm_L=(-.55, .1, 0), forearm_L=(-1.15, 0, -.4))
    C['Eat'] = (72, True, [(1, merge(SEAT, spine(x=.12), look(x=.2), bowl, spoon_low)), (14, merge(SEAT, spine(x=.16), look(x=.28), bowl, spoon_low, P(hand_R=(.3, 0, 0)))),
                           (26, merge(SEAT, spine(x=.06), look(x=.06), bowl, spoon_up)), (32, merge(SEAT, spine(x=.04), look(x=.02), bowl, spoon_up)),
                           (40, merge(SEAT, spine(x=.08), look(x=.1), bowl, spoon_low)), (48, merge(SEAT, spine(x=.08), look(x=.04), bowl, spoon_low)),
                           (56, merge(SEAT, spine(x=.08), look(x=.1), bowl, spoon_low)), (73, merge(SEAT, spine(x=.12), look(x=.2), bowl, spoon_low))])
    C['Bathe'] = (96, True, cyc(96, lambda t: seated(t, spine(x=-.18), look(x=-.1, y=.06 * S(TAU * t)),
        P(upper_arm_L=(.1, -.55, 0), upper_arm_R=(.1, .55, 0), forearm_L=(-.4, 0, 0), forearm_R=(-.4, 0, 0)),
        P(thigh_L=(.4, 0, 0), thigh_R=(.4, 0, 0), shin_L=(-.6, 0, 0), shin_R=(-.6, 0, 0)), tail(t, .04)), 8))  # legs stretched out in the water
    C['Captive'] = (72, True, cyc(72, lambda t: seated(t, spine(x=.22 + .03 * S(TAU * t)), look(x=.35, z=.08 * S(TAU * t / 2 * 2)),
        P(upper_arm_L=(-.4, .15, 0), upper_arm_R=(-.4, -.15, 0), forearm_L=(-.9, 0, -.55), forearm_R=(-.9, 0, .55))), 8))
    C['Swing'] = (48, True, cyc(48, lambda t: merge(SEAT, spine(x=-.2 * S(TAU * t)), look(x=.12 * S(TAU * t)),
        P(thigh_L=(.3 * S(TAU * t), 0, 0), thigh_R=(.3 * S(TAU * t), 0, 0), shin_L=(-.75 * max(0, -S(TAU * t)) + .4 * max(0, S(TAU * t)), 0, 0),
          shin_R=(-.75 * max(0, -S(TAU * t)) + .4 * max(0, S(TAU * t)), 0, 0)),
        P(upper_arm_L=(-1.9, -.1, 0), upper_arm_R=(-1.9, .1, 0), forearm_L=(-.9 + .2 * S(TAU * t), 0, 0), forearm_R=(-.9 + .2 * S(TAU * t), 0, 0)), tail(t, .1)), 8))
    C['Spin'] = (32, True, cyc(32, lambda t: merge(SEAT, spine(x=.15, z=.05 * math.cos(TAU * t)), look(x=.25),
        P(upper_arm_R=(-.85 + .25 * S(TAU * t), -.05, .25 * math.cos(TAU * t)), forearm_R=(-.6, 0, 0), upper_arm_L=(-.75, .1, 0), forearm_L=(-.7, 0, -.2)),
        P(thigh_R=(.12 * max(0, S(TAU * t)), 0, 0), foot_R=(.25 * max(0, S(TAU * t)), 0, 0))), 8))

    # --- lying, falling, hurt ---
    LIE_BACK = P(Root=(-math.pi / 2, 0, 0), Root_loc=(0, 0, .20))
    LIE_SIDE = P(Root=(0, -math.pi / 2, 0), Root_loc=(0, 0, .20))
    C['Sleep'] = (96, True, cyc(96, lambda t: merge(LIE_BACK, spine(x=-.03 * S(TAU * t)), look(z=.35, x=.02 * S(TAU * t)),
        P(upper_arm_L=(-.5, .1, 0), forearm_L=(-1.0, 0, -.5), upper_arm_R=(.1, -.1, 0), thigh_L=(-.25, 0, 0), shin_L=(.4, 0, 0))), 8))
    C['Downed'] = (72, True, cyc(72, lambda t: merge(LIE_SIDE, spine(x=.15 + .04 * S(TAU * t)), look(x=.25 + .03 * S(TAU * t)),
        P(upper_arm_L=(-.6, .1, 0), forearm_L=(-1.2, 0, 0), upper_arm_R=(-.3, 0, 0), forearm_R=(-.9, 0, 0)),
        P(thigh_L=(-.6, 0, 0), shin_L=(1.0, 0, 0), thigh_R=(-.35, 0, 0), shin_R=(.6, 0, 0))), 8))
    clasp = P(upper_arm_L=(-.3, .15, 0), upper_arm_R=(-.3, -.15, 0), forearm_L=(-.95, 0, -.6), forearm_R=(-.95, 0, .6))
    C['Mourn'] = (60, False, [(1, breathe(0)), (22, merge(spine(x=.14), look(x=.32), clasp, P(Ear_L=(.35, 0, 0), Ear_R=(.35, 0, 0)))),
                              (61, merge(spine(x=.16), look(x=.36), clasp, P(Ear_L=(.4, 0, 0), Ear_R=(.4, 0, 0))))])
    flinch = merge(spine(x=-.3), look(x=-.3), P(upper_arm_L=(-.7, -.3, 0), upper_arm_R=(-.7, .3, 0), forearm_L=(-1.2, 0, 0), forearm_R=(-1.2, 0, 0)), P(thigh_R=(.25, 0, 0)))
    C['Hurt'] = (18, False, [(1, breathe(0)), (4, flinch), (9, merge(flinch, spine(x=.15))), (19, merge(crouch(.12), spine(x=.12)))])
    C['Death'] = (48, False, [(1, breathe(0)), (8, merge(spine(x=.3), look(x=.25), crouch(.25), P(upper_arm_L=(-.4, 0, 0), upper_arm_R=(-.5, 0, 0)))),
                              (18, merge(spine(x=.4), look(x=.35), crouch(.7))),
                              (30, merge(LIE_SIDE, spine(x=.05), look(x=.1), P(upper_arm_L=(-.6, 0, 0), thigh_L=(-.3, 0, 0), shin_L=(.4, 0, 0)))),
                              (49, merge(LIE_SIDE, spine(x=.02), look(x=.05), P(upper_arm_L=(-.7, 0, 0), thigh_L=(-.35, 0, 0), shin_L=(.45, 0, 0))))])
    return C


def create(rig):
    """Build 'Kemono|<clip>' actions on rig. Returns (actions, {name: (frames, loop)})."""
    import bpy
    rig.animation_data_create(); actions = {}; specs = {}
    for name, (frames, loop, keys) in clips().items():
        act = bpy.data.actions.new('Kemono|' + name); act.use_fake_user = True
        rig.animation_data.action = act
        prev = {}
        for frame, pose in keys:
            pose = merge(RELAX, pose)
            for bn in DRIVEN:
                pb = rig.pose.bones[bn]; basis = pb.bone.matrix_local.to_3x3()
                e = (basis.inverted() @ Euler(pose.get(bn, (0, 0, 0))).to_matrix() @ basis).to_euler('XYZ', prev.get(bn, Euler()))
                prev[bn] = e
                pb.rotation_mode = 'XYZ'; pb.rotation_euler = e
                pb.location = basis.inverted() @ Vector(pose.get(bn + '_loc', (0, 0, 0)))
                pb.keyframe_insert('rotation_euler', frame=frame, group=bn)
                pb.keyframe_insert('location', frame=frame, group=bn)
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points:
                            kp.interpolation = 'BEZIER'; kp.handle_left_type = kp.handle_right_type = 'AUTO_CLAMPED'
                        fc.update()
                        if loop: fc.modifiers.new('CYCLES')
        act['loop'] = loop; act['duration_frames'] = frames; act['fps'] = 24
        actions[name] = act; specs[name] = (frames, loop)
    rig.animation_data.action = None
    for pb in rig.pose.bones: pb.matrix_basis.identity()
    return actions, specs
