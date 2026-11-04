"""Resource / food / dish models (BLENDER-1019 §5). One recipe per base form, params give each item its identity.

Real-world sizes in kemono metres (apple Ø ≈ 8 cm next to a 1.2 m character) — nothing enlarged for visibility.
Every recipe builds into a pk.Mesh; build_item() finalises with origin at the bottom centre.
Variants made generically: stack (carry/floor pile: crate, sack row or heap), rotten (perishables), seed packet.
"""
import math, random
from mathutils import Vector, Matrix
import pk

TAU = math.tau


def hx(h): return tuple(int(h.lstrip('#')[i:i + 2], 16) for i in (0, 2, 4))
def rgb(c): return '#%02x%02x%02x' % tuple(max(0, min(255, int(v))) for v in c)
def mix(a, b, t): A, B = hx(a), hx(b); return rgb([A[i] + (B[i] - A[i]) * t for i in range(3)])
def dark(a, k=.7): return mix(a, '#000000', 1 - k)
def light(a, k=.3): return mix(a, '#ffffff', k)


R = random.Random(1019)
GREEN, STEM, WOOD, CREAM = '#4f8a2e', '#5a3a1e', '#8a5a30', '#f2ece0'


# ---------------------------------------------------------------- produce
def round_(m, p):
    r = p.get('size', .04); sq = p.get('squash', .92); c = p['color']
    prof = [(0, 0), (r * .55, r * .08), (r * .92, r * .5), (r, r * sq), (r * .9, r * sq * 1.45), (r * .45, r * sq * 1.85), (0, r * sq * 1.78)]
    m.lathe(prof, c, seg=12)
    if p.get('cleft'): m.cyl((0, -r * .98, r * .3), (0, -r * .98, r * 1.6), r * .06, dark(c, .8), seg=4)
    if p.get('blush'): m.ball((r * .5, -r * .6, r * 1.1), (r * .5, r * .3, r * .45), p['blush'], seg=8, rings=4)
    top = r * sq * 1.78
    if p.get('stem'): m.cyl((0, 0, top - .004), (0, 0, top + r * .35), r * .07, p['stem'], seg=5)
    if p.get('leaf'): m.leaf((0, 0, top + r * .2), (r * .7, 0, top + r * .45), r * .4, p['leaf'])
    if p.get('calyx'):
        for k in range(5): a = TAU * k / 5; m.leaf((0, 0, top), (r * .5 * math.cos(a), r * .5 * math.sin(a), top + r * .05), r * .25, p['calyx'])
    if p.get('crown'):
        for k in range(6): a = TAU * k / 6; m.cyl((0, 0, top - .002), (r * .2 * math.cos(a), r * .2 * math.sin(a), top + r * .3), r * .07, dark(c, .7), seg=4)


def pear(m, p):
    s = .7 if p.get('small') else 1.0; r = .035 * s; c = p['color']
    m.lathe([(0, 0), (r * .8, r * .2), (r, r * .8), (r * .75, r * 1.5), (r * .45, r * 2.1), (r * .2, r * 2.4), (0, r * 2.45)], c, seg=12)
    m.cyl((0, 0, r * 2.4), (r * .1, 0, r * 2.9), r * .07, STEM, seg=5)


def lemon(m, p):
    r = .03; m.lathe([(0, 0), (r * .35, r * .1), (r * .85, r * .6), (r, r * 1.25), (r * .85, r * 1.9), (r * .35, r * 2.4), (0, r * 2.5)], p['color'], seg=12, rot=(0, math.pi / 2, 0), c=(-r * 1.25, 0, r))


def mango(m, p):
    r = .04; m.lathe([(0, 0), (r * .6, r * .2), (r, r * 1.0), (r * .8, r * 1.8), (0, r * 2.3)], p['color'], seg=12, rot=(0, math.pi / 2, .3), c=(-r * 1.1, 0, r * .75), sy=.8)
    m.ball((.0, -r * .4, r * 1.2), (r * .7, r * .3, r * .5), p['blush'], seg=8, rings=4)


def oblong(m, p):
    L, r, c = p['length'], p['r'], p['color']
    prof = [(0, 0), (r * .7, L * .05), (r, L * .2), (r * (1.25 if p.get('flare') else 1), L * .7), (r * .7, L * .95), (0, L)]
    m.lathe(prof, c, seg=10, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, r))
    if p.get('stripe'):  # stripes run along the skin (straight rods stuck out past the tapered ends)
        for k in range(8):
            a = TAU * k / 8; pts = [(-L / 2 + z, rad * 1.02) for rad, z in prof[1:-1]]
            for (x0, r0), (x1, r1) in zip(pts, pts[1:]):
                m.cyl((x0, r0 * math.cos(a), r + r0 * math.sin(a)), (x1, r1 * math.cos(a), r + r1 * math.sin(a)), r * .07, p['stripe'], seg=4)
    if p.get('bumps'):
        for k in range(10): m.ball((R.uniform(-L * .4, L * .4), R.uniform(-r, r) * .9, r + R.choice((-1, 1)) * r * .85), (.003,) * 3, light(c, .2), seg=4, rings=3)
    if p.get('calyx'):
        for k in range(4): a = TAU * k / 4; m.leaf((L / 2, 0, r), (L / 2 + r * .3, r * .7 * math.cos(a), r + r * .7 * math.sin(a)), r * .5, p['calyx'])
    m.cyl((L / 2, 0, r), (L / 2 + r * .5, 0, r), r * .12, STEM, seg=5)


def ribbed(m, p):
    r, c = p.get('size', .1), p['color']; n = 12 if p.get('smooth') else 10
    m.lathe([(0, 0), (r * .6, r * .05), (r * .95, r * .35), (r, r * .7), (r * .85, r * 1.15), (r * .3, r * 1.4), (0, r * 1.38)], c, seg=20, sy=.95)
    if not p.get('smooth'):
        for k in range(n):  # rounded lobes (the old 4-sided lathes read as spikes)
            a = TAU * k / n; m.ball((r * .6 * math.cos(a), r * .6 * math.sin(a), r * .68), (r * .42, r * .42, r * .64), c if k % 2 else dark(c, .92), seg=8, rings=6, rot=(0, 0, a))
    if p.get('stripe'):  # stripes follow the skin profile (torus arcs stood off the non-spherical body)
        prof = [(r * .6, r * .05), (r * .95, r * .35), (r, r * .7), (r * .85, r * 1.15), (r * .3, r * 1.4)]
        for k in range(10):
            a = TAU * k / 10
            for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
                m.cyl((r0 * 1.005 * math.cos(a), r0 * 1.005 * math.sin(a) * .95, z0), (r1 * 1.005 * math.cos(a), r1 * 1.005 * math.sin(a) * .95, z1), r * .035, p['stripe'], seg=3)
    if p.get('net'):  # netting = thin meridians + two fine rings hugging the skin (thick rings read as stacked discs)
        prof = [(r * .6, r * .05), (r * .95, r * .35), (r, r * .7), (r * .85, r * 1.15), (r * .3, r * 1.4)]
        for k in range(14):
            a = TAU * k / 14
            for (r0, z0), (r1, z1) in zip(prof, prof[1:]):
                m.cyl((r0 * 1.01 * math.cos(a), r0 * 1.01 * math.sin(a) * .95, z0), (r1 * 1.01 * math.cos(a), r1 * 1.01 * math.sin(a) * .95, z1), r * .01, light(c, .35), seg=3)
        for z, rr in ((.55, .98), (.95, .93)): m.torus((0, 0, r * z), r * rr, r * .008, light(c, .35), seg=20, tseg=3)
    if p.get('stem'): m.cyl((0, 0, r * 1.33), (r * .1, 0, r * 1.6), r * .07, p['stem'], seg=6)


def tuber(m, p):
    c = p['color']; L = .1 if p.get('long') else .065
    m.ico((0, 0, .028), (L / 2, .028, .025), c, sub=2, smooth=True)
    for k in range(4): m.ball((R.uniform(-L * .4, L * .4), R.uniform(-.02, .02), .028 + R.choice((-1, 1)) * .022), (.003,) * 3, dark(c, .6), seg=4, rings=3)
    if p.get('rings'):
        for k in range(3): m.torus((-L * .3 + k * L * .3, 0, .028), .026, .002, dark(c, .7), seg=10, tseg=3, rot=(0, math.pi / 2, 0))


def taproot(m, p):
    c = p['color']; fat = p.get('fat'); r = .025 if fat else .016; L = .17 if fat else .13
    m.lathe([(0, 0), (r * .3, L * .3), (r * .8, L * .75), (r, L * .95), (r * .6, L)], c, seg=10, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, r))
    for k in range(5): a = (k - 2) * .35; m.leaf((L / 2, 0, r), (L / 2 + .08 * math.cos(a), .03 * math.sin(a), r + .06), .02, p['top'])


def bulbroot(m, p):
    c = p['color']; r = .035
    m.lathe([(0, 0), (r * .3, r * .1), (r * .9, r * .6), (r, r * 1.1), (r * .6, r * 1.6), (0, r * 1.7)], c, seg=12)
    m.lathe([(r * .9, r * .9), (r, r * 1.1), (r * .6, r * 1.6), (0, r * 1.72)], p['cap'], seg=12, close=False)
    m.cyl((0, 0, -r * .5), (0, 0, .002), .003, c, seg=4)
    for k in range(4): a = TAU * k / 4; m.leaf((0, 0, r * 1.6), (r * 1.2 * math.cos(a), r * 1.2 * math.sin(a), r * 3.2), r * .7, p['top'])


def head(m, p):
    c, ci = p['color'], p['inner']; r = .065 if not p.get('loose') else .055
    m.ball((0, 0, r * .85), (r * .7, r * .7, r * .75), ci, seg=10, rings=6)
    loose = p.get('loose')
    for k in range(8):  # wrapper leaves = flattened shells round the core (flat diamonds read as an artichoke)
        a = TAU * k / 8 + (.4 if k % 2 else 0); d = Vector((math.cos(a), math.sin(a), 0)); h = .55 if k % 2 else .75
        m.ball((d.x * r * .62, d.y * r * .62, r * h), (r * .72, r * .22, r * (.85 if loose else .7)), c if k % 2 else dark(c, .9), seg=8, rings=5,
               rot=(0, -.35 if loose else -.15, a + math.pi / 2))


def napa(m, p):
    c, ci = p['color'], p['inner']; L = .2
    m.lathe([(0, 0), (.035, .02), (.045, .1), (.04, .17), (0, L)], ci, seg=10, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, .045))
    # green leafy top half wraps the white ribs (thin tip leaves alone read as a radish)
    for k in range(6):
        a = TAU * k / 6
        m.ball((L * .18, .03 * math.cos(a), .045 + .03 * math.sin(a)), (L * .3, .022, .03), c if k % 2 else dark(c, .88), seg=8, rings=5, rot=(a, 0, 0))


def bunch(m, p):
    c = p['color']
    for k in range(7): a = (k - 3) * .18; m.cyl((0, 0, .006), (.12 * math.cos(a), .12 * math.sin(a) * .3, .01), .0025, light(c, .4), seg=4)
    for k in range(7): a = (k - 3) * .18; m.leaf((.07 * math.cos(a), .07 * math.sin(a) * .3, .01), (.16 * math.cos(a), .16 * math.sin(a) * .4, .02), .05, c)
    m.torus((.02, 0, .008), .012, .003, '#c8a870', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def bulb(m, p):
    c = p['color']; r = .032
    if p.get('cloves'):
        for k in range(6): a = TAU * k / 6; m.lathe([(0, 0), (r * .45, r * .3), (r * .4, r * 1.1), (0, r * 1.6)], c, seg=6, c=(r * .3 * math.cos(a), r * .3 * math.sin(a), 0))
        m.lathe([(0, 0), (r * .5, r * .3), (r * .5, r * 1.1), (0, r * 1.8)], c, seg=8)
    else:
        m.lathe([(0, 0), (r * .5, r * .1), (r, r * .7), (r * .85, r * 1.3), (r * .25, r * 1.8), (.003, r * 2.1)], c, seg=12)
        for k in range(6): a = TAU * k / 6; m.cyl((r * .9 * math.cos(a), r * .9 * math.sin(a), r * .4), (r * .5 * math.cos(a), r * .5 * math.sin(a), r * 1.6), .0015, dark(c, .75), seg=3)


def leek(m, p):
    L = .3
    m.cyl((-L / 2, 0, .015), (0, 0, .015), .015, p['color'], seg=8)
    for k in range(3): a = (k - 1) * .25; m.leaf((-.01, 0, .015), (L / 2, .05 * math.sin(a), .03 + .015 * k), .03, p['top'])
    for k in range(5): m.cyl((-L / 2, 0, .015), (-L / 2 - .015, (k - 2) * .006, .006), .0012, CREAM, seg=3)


def spears(m, p):
    for k in range(5):
        a = (k - 2) * .07; s = Vector((math.cos(a), math.sin(a), 0)); b = Vector((0, (k - 2) * .008, .006 + (k % 2) * .008))
        m.cyl(b - s * .09, b + s * .08, .005, p['color'], seg=6); m.ball(b + s * .085, (.008, .006, .006), p['tip'], seg=6, rings=4)
    m.torus((-.02, 0, .01), .02, .003, '#c84a2a', seg=10, tseg=3, rot=(0, math.pi / 2, 0))


def pod(m, p):
    c = p['color']
    for k in range(3):
        y = (k - 1) * .02
        m.lathe([(0, 0), (.007, .01), (.008, .08), (0, .1)], c, seg=6, rot=(0, math.pi / 2, k * .2), c=(-.05, y, .008), sy=.6)
        for j in range(4): m.ball((-.035 + j * .022, y, .013), (.006,) * 3, p['bean'], seg=6, rings=4)


def chili(m, p):
    for k in range(3):
        a = k * .5
        m.lathe([(0, 0), (.004, .02), (.009, .07), (.007, .09)], p['color'], seg=8, rot=(0, math.pi / 2, a), c=(-.045, (k - 1) * .02, .008))
        m.cyl((.045 * math.cos(a), (k - 1) * .02 + .045 * math.sin(a), .008), (.06 * math.cos(a), (k - 1) * .02 + .06 * math.sin(a), .014), .003, GREEN, seg=4)


def bell(m, p):
    r = .038; c = p['color']
    for k in range(4):
        a = TAU * k / 4; m.lathe([(0, 0), (r * .5, r * .1), (r * .55, r * 1.0), (r * .4, r * 1.6), (0, r * 1.7)], c, seg=8, c=(r * .45 * math.cos(a), r * .45 * math.sin(a), 0))
    m.cyl((0, 0, r * 1.6), (0, 0, r * 2.1), r * .1, GREEN, seg=6)


def lotus(m, p):
    L, r = .14, .028
    for k in range(2): m.lathe([(0, 0), (r, .005), (r * 1.1, L * .4), (r, L * .5)], p['color'], seg=12, rot=(0, math.pi / 2, 0), c=(-L / 2 + k * L * .5, 0, r))
    for k in range(6): a = TAU * k / 6; m.cyl((-L / 2 - .001, r * .5 * math.cos(a), r + r * .5 * math.sin(a)), (-L / 2 - .001 + .002, r * .5 * math.cos(a), r + r * .5 * math.sin(a)), r * .18, dark(p['color'], .6), seg=6)


def beans(m, p):
    c = p['color']; n = 48
    m.lathe([(0, 0), (.04, 0), (.026, .014), (0, .022)], dark(c, .85), seg=12)  # the pile body under the top beans
    for k in range(n):
        x, y = R.uniform(-.036, .036), R.uniform(-.036, .036)
        if math.hypot(x, y) > .038: continue
        z = .004 + max(0, .022 - math.hypot(x, y) * .55)
        if p.get('round'): m.ball((x, y, z), (.006,) * 3, c, seg=6, rings=4)
        elif p.get('flat'): m.ball((x, y, z), (.006, .006, .003), c, seg=6, rings=3)
        else: m.ball((x, y, z), ((.009 if p.get('big') else .006), .004, .004), c, seg=6, rings=3, rot=(0, 0, R.uniform(0, 3)))


def corn(m, p):
    L = .16
    m.lathe([(0, 0), (.02, .015), (.024, .08), (.018, .14), (0, L)], p['kernel'], seg=12, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, .024))
    for k in range(3): a = (k - 1) * .9; m.leaf((-L / 2, 0, .024), (L / 2 + .02, .03 * math.sin(a), .024 + .03 * math.cos(a)), .035, p['husk'], normal=(0, math.sin(a), math.cos(a)))


def grain(m, p):
    """Single = a tied bundle of stalks with heads (the stack variant is a sack)."""
    head, stalk = p['head'], p['stalk']
    for k in range(9):
        a = TAU * k / 9; o = Vector((.008 * math.cos(a), .008 * math.sin(a), 0))
        tip = Vector((.18, o.y * 3, .02 + o.z * 3 + (.012 if p.get('droop') else 0)))
        m.cyl(Vector((-.08, o.y, .01)), tip, .0018, stalk, seg=3)
        hl = .05 if not p.get('bushy') else .04; d = Vector((1, 0, -.4 if p.get('droop') else .05)).normalized()
        if p.get('flower'): m.ico(tip, (.008,) * 3, head, sub=0)
        elif p.get('loose'):
            for j in range(4): m.ball(tip + d * (j * .012) + Vector((0, (j % 2 - .5) * .01, 0)), (.004, .002, .002), head, seg=4, rings=3)
        else:
            m.lathe([(0, 0), (.006 if not p.get('bushy') else .012, hl * .3), (.005, hl * .8), (0, hl)], head, seg=5, c=tip,
                    rot=(0, math.pi / 2 - (.4 if p.get('droop') else 0), 0))
        if p.get('awn'): m.cyl(tip + d * hl, tip + d * (hl + .03), .0007, head, seg=3)
    m.torus((-.03, 0, .01), .012, .003, '#c8a870', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def cluster(m, p):
    r = p['r']; n = p['n']; c = p['color']
    for k in range(n):
        lvl = int((math.sqrt(8 * k + 1) - 1) / 2); i = k - lvl * (lvl + 1) // 2
        a = TAU * i / max(1, lvl + 1); z = .08 - lvl * r * 1.6
        m.ball((r * lvl * .9 * math.cos(a), r * lvl * .9 * math.sin(a), max(r, z)), (r,) * 3, c, seg=8, rings=5)
    m.cyl((0, 0, .08), (0, 0, .1), .002, STEM, seg=4)
    m.leaf((0, 0, .1), (.04, 0, .11), .03, GREEN)


def strawberry(m, p):
    r = .016
    for k in range(3):
        c = Vector(((k - 1) * .03, (k % 2) * .01, 0))
        m.lathe([(0, 0), (r * .5, r * .3), (r, r * 1.2), (r * .8, r * 1.6), (0, r * 1.7)], p['color'], seg=8, c=c, rot=(math.pi, 0, 0) if False else (0, 0, 0))
        for j in range(4): a = TAU * j / 4; m.leaf(c + Vector((0, 0, r * 1.65)), c + Vector((r * .7 * math.cos(a), r * .7 * math.sin(a), r * 1.75)), r * .45, GREEN)


def berries(m, p):
    c = p['color']; n = p.get('n', 7); r = .007 if not p.get('long') else .006
    for k in range(n):
        a = TAU * k / max(1, n - 1); d = 0 if k == 0 else .013
        pos = (d * math.cos(a), d * math.sin(a), r)
        if p.get('bumpy') or p.get('long'):
            for j in range(5): m.ball((pos[0] + (j % 2 - .5) * r * .7, pos[1], r + j * r * (.5 if p.get('long') else .3)), (r * .6,) * 3, c, seg=6, rings=4)
        else: m.ball(pos, (r,) * 3, c, seg=8, rings=5)


def cherries(m, p):
    c = p['color']
    if p.get('many'):
        for k in range(9): m.ball((R.uniform(-.015, .015), R.uniform(-.015, .015), .006 + (k % 3) * .006), (.006,) * 3, c, seg=6, rings=4)
        m.cyl((0, 0, .01), (0, 0, .04), .002, WOOD, seg=4); return
    for s in (-1, 1):
        m.ball((s * .012, 0, .011), (.011,) * 3, c, seg=8, rings=5)
        m.cyl((s * .012, 0, .02), (0, 0, .055), .0012, STEM, seg=4)


def banana(m, p):
    c = p['color']
    for k in range(4):
        y = (k - 1.5) * .022
        bm = pk.Mesh('b'); bm.torus((0, 0, 0), .09, .014, c, seg=8, tseg=6, arc=.22)
        m.merge(bm, Matrix.Translation((0, y, .1)) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Rotation(-math.pi * .7, 4, 'Z'))
    m.cyl((-.05, 0, .012), (-.08, 0, .03), .008, dark(c, .5), seg=6)


def pineapple(m, p):
    r = .045; c = p['color']
    m.lathe([(0, 0), (r * .8, r * .1), (r, r * .9), (r * .9, r * 1.9), (r * .4, r * 2.3), (0, r * 2.32)], c, seg=10)
    for z in range(5):
        for k in range(8):
            a = TAU * (k + z * .5) / 8; rr = r * (.92 if z in (1, 2, 3) else .8)
            m.ball((rr * math.cos(a), rr * math.sin(a), r * (.35 + z * .4)), (.006,) * 3, dark(c, .7), seg=4, rings=3)
    for k in range(8): a = TAU * k / 8; m.leaf((0, 0, r * 2.2), (r * .7 * math.cos(a), r * .7 * math.sin(a), r * 3.6), r * .35, p['crown'])


def dates(m, p):
    for k in range(5): m.ball(((k - 2) * .015, (k % 2) * .012, .008), (.012, .0075, .0075), p['color'], seg=8, rings=4, rot=(0, 0, k * .4))


def chestnut(m, p):
    c = p['color']
    for k in range(3):
        a = TAU * k / 3; m.lathe([(0, 0), (.014, .004), (.015, .012), (.008, .022), (0, .026)], c, seg=8, c=(.014 * math.cos(a), .014 * math.sin(a), 0), sy=.7)
        m.ball((.014 * math.cos(a), .014 * math.sin(a), .003), (.01, .007, .004), light(c, .4), seg=6, rings=3)


def walnut(m, p):
    for k in range(3): m.ico(((k - 1) * .028, 0, .014), (.015, .014, .014), p['color'], sub=2)


def acorn(m, p):
    for k in range(3):
        c = Vector(((k - 1) * .022, (k % 2) * .01, 0))
        m.lathe([(0, 0), (.008, .005), (.009, .016), (0, .022)], p['color'], seg=8, c=c, rot=(0, math.pi / 2, k), sy=1)
        m.ball(c + Vector((.016 * math.cos(k) if False else .0, 0, .009)), (.0, .0, .0), p['cap'], seg=4, rings=3) if False else None
        m.lathe([(.0, 0), (.0095, .002), (.009, .008), (.0, .009)], p['cap'], seg=8, c=c + Vector((.013, 0, .009)), rot=(0, math.pi / 2, 0))


def almond(m, p):
    for k in range(6): m.ball((R.uniform(-.02, .02), R.uniform(-.02, .02), .005 + (k % 2) * .006), (.011, .007, .004), p['color'], seg=6, rings=4, rot=(0, 0, R.uniform(0, 3)))


def peanut(m, p):
    for k in range(3):
        y = (k - 1) * .016
        for s in (-1, 1): m.ball((s * .008, y, .007), (.009, .007, .007), p['color'], seg=6, rings=4)


def seedpile(m, p):
    """Small heap of seeds/beans/grains on a leaf plate (tiny items are shown as a pile)."""
    c = p['color']; n = 30 if p.get('tiny') else 20
    m.lathe([(0, 0), (.045, .0), (.045, .003), (0, .003)], '#a8b870', seg=10)
    for k in range(n):
        x, y = R.uniform(-.03, .03), R.uniform(-.03, .03); z = .006 + max(0, .018 - math.hypot(x, y) * .5)
        if p.get('tiny'): s = (.003, .002, .0015)
        elif p.get('long'): s = (.007, .003, .003)
        elif p.get('round'): s = (.004,) * 3
        elif p.get('bean'): s = (.006, .0045, .004)
        else: s = (.006, .003, .002)
        m.ball((x, y, z), s, c if not (p.get('stripe') and k % 3 == 0) else '#d8d0c0', seg=5, rings=3, rot=(0, 0, R.uniform(0, 3)))


def mushroom(m, p):
    cap, stem, sh = p['cap'], p['stem'], p['shape']
    if sh in ('dome', 'button', 'tall', 'king', 'funnel'):
        H = {'dome': .04, 'button': .03, 'tall': .07, 'king': .08, 'funnel': .04}[sh]; Rr = {'dome': .03, 'button': .022, 'tall': .022, 'king': .022, 'funnel': .025}[sh]
        sr = Rr * (.55 if sh == 'king' else .3)
        m.cyl((0, 0, 0), (0, 0, H), sr, stem, seg=8, r2=sr * (1.0 if sh != 'funnel' else 1.6))
        if sh == 'funnel': m.lathe([(sr * 1.6, H * .9), (Rr, H * 1.1), (Rr * .95, H * 1.2), (0, H * 1.05)], cap, seg=12)
        else: m.lathe([(sr, H * .95), (Rr, H * .95), (Rr * .9, H * 1.2), (Rr * .5, H * 1.4), (0, H * (1.45 if sh != 'button' else 1.55))], cap, seg=12)
        if p.get('cracks'):
            for k in range(6): a = TAU * k / 6; m.cyl((Rr * .3 * math.cos(a), Rr * .3 * math.sin(a), H * 1.4), (Rr * .85 * math.cos(a), Rr * .85 * math.sin(a), H * 1.15), .0015, light(cap, .5), seg=3)
    elif sh == 'shelf':
        for k in range(3): m.ball((.0, (k - 1) * .02, .01 + k * .006), (.035, .022, .006), cap, seg=10, rings=4, rot=(0, -.2, (k - 1) * .3))
        m.cyl((-.03, 0, 0), (0, 0, .012), .007, stem, seg=6)
    elif sh == 'enoki':
        for k in range(14):
            a, d = R.uniform(0, TAU), R.uniform(0, .012); b = Vector((d * math.cos(a), d * math.sin(a), 0)); t = b * 1.6 + Vector((0, 0, .08))
            m.cyl(b, t, .0015, stem, seg=3); m.ball(t, (.004, .004, .003), cap, seg=5, rings=3)
        m.cyl((0, 0, 0), (0, 0, .02), .014, light(stem, .2), seg=8)
    elif sh == 'cluster':
        for k in range(7):
            a = TAU * k / 7; b = Vector((.006 * math.cos(a), .006 * math.sin(a), 0)); t = b * 3 + Vector((0, 0, .04 + (k % 3) * .008))
            m.cyl(b, t, .003, stem, seg=4); m.ball(t, (.009, .009, .006), cap, seg=6, rings=3)
    elif sh == 'ear':
        for k in range(3): m.ball(((k - 1) * .02, (k % 2) * .01, .006), (.022, .018, .005), cap, seg=8, rings=4, rot=(.4 * k, .2, k))
    elif sh == 'morel':
        m.cyl((0, 0, 0), (0, 0, .025), .009, stem, seg=8)
        m.lathe([(.011, .02), (.016, .035), (.013, .06), (0, .075)], cap, seg=8, smooth=False)
        for k in range(10): m.ball((.014 * math.cos(k), .014 * math.sin(k), .03 + (k % 4) * .01), (.004,) * 3, dark(cap, .6), seg=4, rings=3)
    elif sh == 'truffle':
        m.ico((0, 0, .018), (.022, .02, .018), cap, sub=2)
        for k in range(8): m.ico((.02 * math.cos(k), .018 * math.sin(k), .012 + (k % 3) * .006), (.005,) * 3, dark(cap, .8), sub=0)


# ---------------------------------------------------------------- animals
def fish(m, p):
    L, D, c, belly = p['L'], p['D'], p['color'], p['belly']; h = L * D / 2
    prof = [(0, 0), (h * .35, L * .05), (h * .85, L * .22), (h, L * .42), (h * .7, L * .68), (h * .22, L * .82), (.0, L * .84)]
    m.lathe(prof, c, seg=10, sx=.38, sy=1, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, h))
    m.lathe([(0, L * .1), (h * .6, L * .25), (h * .7, L * .45), (h * .3, L * .7), (0, L * .78)], belly, seg=10, sx=.36, sy=.6, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, h * .75))
    tail = [(0, 0), (L * .18, h * .8), (L * .14, 0), (L * .18, -h * .8)]
    m.prism([(L * .32 + x, y) for x, y in tail], -.0015, .0015, dark(c, .8), M=Matrix.Translation((0, 0, h)) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
    m.prism([(-L * .05, 0), (L * .12, 0), (L * .02, h * .6)], -.001, .001, dark(c, .8), M=Matrix.Translation((0, 0, h * 1.85)) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
    for s in (-1, 1): m.ball((-L * .4, s * h * .3, h * 1.2), (h * .12,) * 3, '#101010', seg=6, rings=4)
    if p.get('spots'):
        for k in range(8): m.ball((R.uniform(-L * .3, L * .25), R.choice((-1, 1)) * h * .37, h + R.uniform(-h * .4, h * .6)), (h * .08,) * 3, p['spots'], seg=4, rings=3)
    if p.get('stripe'): m.cyl((-L * .35, 0, h * .9), (L * .3, 0, h * .9), h * .4, p['stripe'], seg=6, r2=h * .3) if False else m.box((0, 0, h * .9), (L * .6, h * .78, h * .12), p['stripe'])
    if p.get('wavy'):
        for k in range(5): m.box((-L * .2 + k * L * .1, 0, h * 1.5), (L * .03, h * .76, h * .35), p['wavy'], rot=(0, .5, 0))
    if p.get('barbel'):
        for s in (-1, 1): m.cyl((-L * .48, s * h * .1, h * .8), (-L * .58, s * h * .5, h * .5), .0012, dark(c, .6), seg=3)
    if p.get('finlets'):
        for k in range(4): m.box((L * .15 + k * L * .04, 0, h * 1.75), (.004, .002, .006), p['finlets'])
    if p.get('smoked'): m.torus((L * .3, 0, h), h * .5, .0015, '#c8a870', seg=8, tseg=3, rot=(0, math.pi / 2, 0))
    if p.get('salted'):
        for k in range(14): m.ball((R.uniform(-L * .4, L * .3), R.uniform(-h * .35, h * .35), h + R.uniform(0, h * .9)), (.0016,) * 3, '#f8f8f5', seg=4, rings=3)


def eel(m, p):
    L, c = p['L'], p['color']; r = .009
    pts = [Vector((-L / 2 + L * i / 10, .02 * math.sin(i * .9), r)) for i in range(11)]
    for a, b in zip(pts, pts[1:]): m.cyl(a, b, r, c, seg=8)
    for s in (-1, 1): m.ball(pts[0] + Vector((-.004, s * .005, .004)), (.0025,) * 3, '#101010', seg=4, rings=3)


def flatfish(m, p):
    L, c = p['L'], p['color']
    m.ball((0, 0, .008), (L * .4, L * .28, .008), c, seg=12, rings=5)
    m.ball((0, 0, .004), (L * .4, L * .28, .004), p['belly'], seg=12, rings=4)
    m.prism([(L * .38, 0), (L * .55, L * .12), (L * .52, 0), (L * .55, -L * .12)], 0, .003, dark(c, .8))
    for s in (0, 1): m.ball((-L * .25, .01 + s * .012, .016), (.004,) * 3, '#101010', seg=4, rings=3)


def shrimp(m, p):
    c = p['color']; bm = pk.Mesh('s'); bm.torus((0, 0, 0), .025, .009, c, seg=8, tseg=6, arc=.6)
    m.merge(bm, Matrix.Translation((0, 0, .012)) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
    for k in range(6): m.cyl((-.02 + k * .006, 0, .005), (-.02 + k * .006, .012, -.004 + .006), .001, dark(c, .7), seg=3)
    m.cyl((.024, 0, .016), (.05, .0, .03), .0008, c, seg=3)


def crab(m, p):
    c = p['color']
    m.ball((0, 0, .016), (.035, .026, .012), c, seg=10, rings=5)
    for s in (-1, 1):
        for k in range(3): m.cyl((s * .025, -.01 + k * .012, .012), (s * .05, -.015 + k * .015, .002), .003, c, seg=4)
        m.cyl((s * .02, -.02, .015), (s * .03, -.045, .015), .004, c, seg=4); m.ball((s * .03, -.05, .016), (.008, .01, .006), c, seg=6, rings=4)
        m.ball((s * .008, -.024, .026), (.003,) * 3, '#101010', seg=4, rings=3)


def crayfish(m, p):
    c = p['color']
    for k in range(5): m.ball((k * .012 - .02, 0, .01), (.008, .01, .007), c, seg=8, rings=4)
    m.prism([(.04, 0), (.055, .015), (.052, 0), (.055, -.015)], 0, .003, c, M=Matrix.Translation((0, 0, .006)))
    for s in (-1, 1): m.cyl((-.025, s * .006, .01), (-.05, s * .02, .01), .003, c, seg=4); m.ball((-.055, s * .022, .01), (.009, .005, .004), c, seg=6, rings=3)


def squid(m, p):
    c = p['color']; L = .13
    m.lathe([(0, 0), (.016, .02), (.018, .07), (0, .1)], c, seg=10, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, .016))
    m.prism([(-L / 2 + .1, 0), (-L / 2 + .085, .02), (-L / 2 + .07, 0), (-L / 2 + .085, -.02)], -.002, .002, c, M=Matrix.Translation((0, 0, .016)))
    for k in range(8): a = (k - 3.5) * .08; m.cyl((-L / 2 + .002, 0, .016), (-L / 2 - .05, .03 * math.sin(a), .004), .0018, light(c, .2), seg=3)


def octopus(m, p):
    c = p['color']
    m.ball((0, 0, .035), (.025, .025, .028), c, seg=10, rings=6)
    for k in range(8):
        a = TAU * k / 8; pts = [Vector((math.cos(a) * (.015 + .012 * i), math.sin(a) * (.015 + .012 * i), .012 - .002 * i + .006 * math.sin(i))) for i in range(5)]
        for x, y in zip(pts, pts[1:]): m.cyl(x, y, .004 * (1 - .15 * pts.index(x)), c, seg=5)
    for s in (-1, 1): m.ball((s * .01, -.022, .04), (.004,) * 3, '#f2efe6', seg=4, rings=3)


def shell_pair(m, c, r, ribs=None, rot=0, n=2):
    for k in range(n):
        x = (k - (n - 1) / 2) * r * 2.1
        m.ball((x, 0, r * .3), (r, r * .8, r * .3), c, seg=10, rings=4, rot=(0, 0, rot))
        if ribs:
            for j in range(5): m.cyl((x, 0, r * .55), (x + r * .9 * math.cos(j * .6 + .3), r * .7 * math.sin(j * .6 + .3) - r * .3, r * .3), .0012, ribs, seg=3)


def clam(m, p): shell_pair(m, p['color'], .016, p.get('ribs'), n=3)
def oyster(m, p):
    for k in range(2): m.ico((k * .04 - .02, 0, .008), (.025, .018, .008), p['color'], sub=1)


def mussel(m, p):
    for k in range(3): m.ball((k * .02 - .02, (k % 2) * .01, .007), (.022, .01, .007), p['color'], seg=8, rings=4, rot=(0, 0, .3 * k))


def scallop(m, p):
    for k in range(2):
        x = (k - .5) * .05
        m.lathe([(0, 0), (.022, .002), (.022, .005), (0, .007)], p['color'], seg=12, c=(x, 0, 0))
        for j in range(7): a = math.pi * (j / 6); m.cyl((x, 0, .006), (x + .02 * math.cos(a), .02 * math.sin(a) - .005, .006), .0015, light(p['color'], .3), seg=3)
        m.box((x, -.022, .003), (.012, .008, .004), p['color'])


def abalone(m, p):
    m.ball((0, 0, .008), (.035, .025, .008), p['color'], seg=12, rings=4)
    m.ball((.0, 0, .012), (.028, .019, .004), p['inner'], seg=12, rings=3)
    for k in range(4): m.ball((-.025 + k * .008, .018, .014), (.0025,) * 3, '#2a2a2a', seg=4, rings=3)


def laver(m, p):
    for k in range(4): m.box((0, 0, .001 + k * .0012), (.09, .09, .001), p['color'], rot=(0, 0, k * .05))


def seaweed(m, p):
    c = p['color']; w = .05 if p.get('wide') else .025
    for k in range(3): m.leaf((-.06, (k - 1) * .02, .004 + k * .003), (.07, (k - 1) * .03, .006 + k * .003), w, c)
    if p.get('frilly'):
        for k in range(6): m.ball((-.04 + k * .02, (k % 2 - .5) * .02, .008), (.008, .004, .003), light(c, .2), seg=5, rings=3)


def meat(m, p):
    c, fat, s = p['color'], p['fat'], p['size']; w, d = .07 * s, .05 * s
    m.ico((0, 0, .015 * s), (w / 2, d / 2, .015 * s), c, sub=2)
    m.torus((0, 0, .015 * s), w * .45, .006 * s, fat, seg=12, tseg=4, rot=(0, 0, 0)) if not p.get('lean') else None
    if p.get('chop'): m.cyl((w * .3, 0, .015 * s), (w * .75, 0, .02 * s), .006 * s, CREAM, seg=6); m.ball((w * .78, 0, .02 * s), (.008 * s,) * 3, CREAM, seg=6, rings=4)
    if p.get('marbled'):
        for k in range(6): m.cyl((R.uniform(-w * .3, w * .3), R.uniform(-d * .3, d * .3), .029 * s), (R.uniform(-w * .3, w * .3), R.uniform(-d * .3, d * .3), .029 * s), .0018, fat, seg=3)
    if p.get('rind'): m.box((0, d * .45, .015 * s), (w * .8, .006, .024 * s), p['rind'])
    if p.get('smoked'):
        for k in range(2): m.torus(((k - .5) * w * .4, 0, .015 * s), d * .52, .0018, '#c8a870', seg=10, tseg=3, rot=(0, math.pi / 2, 0))
    if p.get('salted'):
        for k in range(16): m.ball((R.uniform(-w * .4, w * .4), R.uniform(-d * .4, d * .4), .028 * s), (.0016,) * 3, '#f8f8f5', seg=4, rings=3)


def drumstick(m, p):
    c, s = p['color'], p['size']
    m.lathe([(0, 0), (.022 * s, .01 * s), (.026 * s, .04 * s), (.012 * s, .07 * s), (.006 * s, .08 * s)], p.get('skin', c), seg=10, rot=(0, math.pi / 2, 0), c=(-.045 * s, 0, .024 * s))
    m.cyl((.035 * s, 0, .024 * s), (.065 * s, 0, .024 * s), .005 * s, CREAM, seg=6); m.ball((.068 * s, 0, .024 * s), (.008 * s,) * 3, CREAM, seg=6, rings=4)


def chunk(m, p):
    for k in range(3): m.ico((k * .022 - .022, (k % 2) * .01, .01), (.012, .01, .009), p['color'], sub=1)
    m.ico((.0, .0, .02), (.012, .01, .004), p['shell'], sub=1)


def insect(m, p):
    c, k = p['color'], p['kind']
    if k == 'larva':
        for i in range(6): m.ball((-.02 + i * .008, 0, .006), (.006, .006, .006 - i * .0004), c, seg=6, rings=4)
        m.ball((.03, 0, .006), (.004,) * 3, '#7a4a22', seg=5, rings=3); return
    L = .045 if k == 'grasshopper' else .03
    m.ball((0, 0, .008), (L / 2, .007, .007), c, seg=8, rings=4)
    m.ball((-L / 2, 0, .01), (.006,) * 3, dark(c, .8), seg=6, rings=4)
    for s in (-1, 1):
        m.cyl((L * .2, s * .005, .01), (L * .05, s * .016, .02), .0012, c, seg=3); m.cyl((L * .05, s * .016, .02), (L * .5, s * .02, .002), .0012, c, seg=3)
        m.cyl((-L / 2, s * .003, .013), (-L / 2 - .02, s * .012, .03), .0005, c, seg=3)


def egg(m, p):
    s = p.get('size', 1.0); n = 3
    for k in range(n):
        c = Vector(((k - 1) * .045 * s, (k % 2) * .01, 0))
        m.lathe([(0, 0), (.016 * s, .006 * s), (.02 * s, .02 * s), (.016 * s, .038 * s), (0, .048 * s)], p['color'], seg=10, c=c)
        if p.get('spots'):
            for j in range(6): a = R.uniform(0, TAU); z = R.uniform(.01, .04) * s; m.ball(c + Vector((.018 * s * math.cos(a), .018 * s * math.sin(a), z)), (.002 * s,) * 3, p['spots'], seg=4, rings=3)


# ---------------------------------------------------------------- containers
def bottle(m, p):
    shape = p.get('shape', 'oil'); liq = p['liquid']
    glass = pk.mat('#d8eef2', alpha=.45, rough=.1, name='PK_Glass')
    prof = {'milk': [(0, 0), (.028, 0), (.03, .09), (.016, .12), (.014, .14)], 'oil': [(0, 0), (.025, 0), (.026, .12), (.012, .15), (.009, .18)],
            'small': [(0, 0), (.02, 0), (.021, .07), (.009, .095), (.007, .11)], 'leaf': [(0, 0), (.03, 0), (.03, .05), (.012, .09), (.008, .11)]}[shape]
    m.lathe(prof, glass, seg=12, close=False)
    inner = [(r * .9, z) for r, z in prof[:3]] + [(0, prof[2][1] * .95)]
    m.lathe([(0, .004)] + inner[1:], liq, seg=12)
    top = prof[-1][1]; m.cyl((0, 0, top), (0, 0, top + .015), prof[-1][0] * 1.25, p.get('cap', '#c8302a'), seg=10)
    m.box((0, -prof[1][0] - .0005, prof[2][1] * .45), (prof[1][0] * 1.2, .001, prof[2][1] * .35), p.get('cap', '#e8dcc0'))  # label band


def jar(m, p):
    big = p.get('big'); r = .04 if big else .032; h = .085 if big else .07
    glass = pk.mat('#d8eef2', alpha=.45, rough=.1, name='PK_Glass')
    m.lathe([(0, 0), (r, 0), (r * 1.02, h * .8), (r * .85, h), (r * .85, h * 1.05)], glass, seg=14, close=False)
    m.lathe([(0, .004), (r * .92, .004), (r * .94, h * .75), (0, h * .78)], p['content'], seg=14)
    m.cyl((0, 0, h * 1.02), (0, 0, h * 1.17), r * .92, p['lid'], seg=14)
    if p.get('cloth'): m.lathe([(r * 1.1, h * .95), (r * .95, h * 1.18), (0, h * 1.22)], '#f2ece0', seg=10)
    if p.get('chunks'):
        for k in range(6): m.box((R.uniform(-r * .5, r * .5), R.uniform(-r * .5, r * .5), R.uniform(h * .2, h * .6)), (.012, .012, .012), light(p['content'], .3))
    if p.get('drop'): m.lathe([(0, 0), (.008, .006), (.004, .016), (0, .022)], p['content'], seg=8, c=(r * 1.4, 0, 0))
    if p.get('dipper'): m.cyl((0, 0, h * .5), (r * .4, 0, h * 1.6), .003, WOOD, seg=5)


def sack(m, p):
    c = p['color']
    m.lathe([(0, 0), (.05, .0), (.06, .03), (.058, .09), (.04, .12), (.018, .135), (.022, .16), (.015, .17)], c, seg=12, sy=.8)
    m.torus((0, 0, .135), .02, .004, '#8a6a3a', seg=10, tseg=4)
    m.box((0, -.047, .07), (.04, .002, .03), p.get('tag', '#d0cfc6'))


def drum(m, p):
    m.cyl((0, 0, 0), (0, 0, .22), .07, p['color'], seg=16)
    for z in (.06, .15): m.torus((0, 0, z), .071, .004, p['band'], seg=16, tseg=4)
    m.cyl((.03, 0, .22), (.03, 0, .228), .01, '#c8c8c8', seg=8)
    m.box((0, -.071, .11), (.05, .002, .04), '#f2c230')  # hazard label


def gascan(m, p):
    m.lathe([(0, 0), (.04, 0), (.042, .03), (.042, .2), (.03, .24), (.01, .25), (.01, .27)], p['color'], seg=14)
    m.torus((0, 0, .26), .02, .004, '#8a8a8a', seg=10, tseg=4)
    m.cyl((0, 0, .27), (0, 0, .285), .008, '#3a3a3a', seg=8)


def jerrycan(m, p):
    m.box((0, 0, .1), (.11, .05, .2), p['color'])
    m.box((-.02, 0, .21), (.05, .02, .02), dark(p['color'], .7)); m.cyl((.035, 0, .19), (.045, 0, .225), .01, '#c8c8c8', seg=8)
    m.box((0, -.026, .1), (.08, .002, .14), dark(p['color'], .85))


def bucket(m, p):
    m.lathe([(0, 0), (.045, 0), (.055, .09), (.052, .09), (.044, .005), (0, .005)], WOOD, seg=14, close=False)
    m.lathe([(0, .008), (.046, .008), (.05, .07), (0, .07)], p['color'], seg=14)
    for z in (.02, .07): m.torus((0, 0, z), .05 + z * .1, .003, '#5a5a5a', seg=14, tseg=3)
    m.torus((0, 0, .09), .055, .002, '#5a5a5a', seg=14, tseg=3, rot=(math.pi / 2, 0, 0), arc=.5)


def can(m, p):
    m.cyl((0, 0, 0), (0, 0, .07), .03, p['color'], seg=14)
    m.cyl((0, 0, .012), (0, 0, .058), .0305, p['label'], seg=14, cap=False)
    for z in (.003, .067): m.torus((0, 0, z), .03, .002, light(p['color'], .2), seg=14, tseg=3)


def crock(m, p):
    m.lathe([(0, 0), (.04, 0), (.055, .05), (.045, .1), (.035, .11)], '#6a4a32', seg=14, close=False)
    m.lathe([(0, .09), (.04, .09), (0, .095)], p['content'], seg=14)
    m.lathe([(.04, .11), (.044, .118), (.01, .125), (0, .13)], '#5a3a26', seg=14)


def cup(m, p):
    m.lathe([(0, 0), (.026, 0), (.03, .05), (.028, .05), (0, .046)], p['cup'], seg=14)
    m.cyl((0, 0, .05), (0, 0, .053), .031, p['lid'], seg=14)


def cheese(m, p):
    m.cyl((0, 0, 0), (0, 0, .045), .06, p['rind'], seg=16)
    m.prism([(0, 0), (.065, -.02), (.065, .02)], 0, .044, p['color'], M=Matrix.Translation((.05, -.06, 0)))
    for k in range(3): m.ball((.08 + k * .008, -.06 + (k - 1) * .008, .02 + k * .006), (.004,) * 3, dark(p['color'], .75), seg=4, rings=3)


def block(m, p):
    w, d, h = p.get('size', (.06, .04, .03))
    m.box((0, 0, h / 2), (w, d, h), p['color'])
    if p.get('wrap'): m.box((-w * .2, 0, h / 2), (w * .65, d * 1.03, h * 1.03), p['wrap'])


def powder(m, p):
    m.lathe([(0, 0), (.04, .0), (.045, .05), (.043, .05), (0, .046)], '#8a6a4a', seg=14)
    m.lathe([(0, .046), (.042, .046), (.03, .058), (0, .066)], p['color'], seg=14)


def pieces(m, p):
    for k in range(10): m.ico((R.uniform(-.03, .03), R.uniform(-.03, .03), .006 + (k % 3) * .005), (.009, .007, .004), p['color'] if k % 3 else p['alt'], sub=0)


def strips(m, p):
    for k in range(5): m.box((0, (k - 2) * .014, .003 + (k % 2) * .004), (.09, .012, .004), p['color'], rot=(0, 0, R.uniform(-.1, .1)))
    m.torus((0, 0, .006), .02, .002, '#c8a870', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def driedfish(m, p):
    for k in range(3):
        f = pk.Mesh('f'); fish(f, dict(color=p['color'], belly=light(p['color'], .2), L=.11, D=.18))
        m.merge(f, Matrix.Translation((0, (k - 1) * .022, 0)) @ Matrix.Diagonal((1, .6, 1, 1)))
    m.cyl((-.05, -.04, .03), (-.05, .04, .03), .002, '#c8a870', seg=3)


# ---------------------------------------------------------------- industrial materials
def log(m, p):
    for k, (y, z) in enumerate(((-.04, .035), (.04, .035), (0, .1))):
        m.cyl((-.14, y, z), (.14, y, z), .035, p['bark'], seg=9, cap=False)
        for s in (-1, 1): m.cyl((s * .14, y, z), (s * .141, y, z), .033, p['core'], seg=9)


def plank(m, p):
    for k in range(4): m.box((0, (k % 2 - .5) * .0, .006 + k * .013), (.3, .06, .012), p['color'] if k % 2 else dark(p['color'], .9), rot=(0, 0, (k % 2) * .06))


def stone(m, p):
    for k in range(3): m.ico((k * .05 - .05, (k % 2) * .03, .025), (.03, .026, .022 + k * .003), p['color'], sub=1)
    if p.get('speck'):
        for k in range(10): m.ball((R.uniform(-.07, .07), R.uniform(-.02, .04), R.uniform(.035, .045)), (.003,) * 3, p['speck'], seg=4, rings=3)


def block_stone(m, p):
    for k in range(4): m.box(((k % 2) * .085 - .042, 0, .02 + (k // 2) * .041), (.08, .05, .04), p['color'] if k != 2 else dark(p['color'], .92))
    if p.get('speck'):
        for k in range(10): m.ball((R.uniform(-.08, .08), -.026, R.uniform(.01, .08)), (.002,) * 3, p['speck'], seg=4, rings=3)


def ore(m, p):
    for k in range(3): m.ico((k * .045 - .045, (k % 2) * .025, .022), (.026, .022, .02), p['rock'], sub=1)
    for k in range(14):
        e = pk.mat(p['speck'], metal=.6, rough=.3, emit=1.5 if p.get('glow') else 0)
        m.ico((R.uniform(-.07, .07), R.uniform(-.015, .04), R.uniform(.025, .042)), (.0045,) * 3, e, sub=0)


def ingot(m, p):
    c = pk.mat(p['color'], metal=.7, rough=.35, emit=1.2 if p.get('glow') else 0)
    for k, pos in enumerate(((-.03, 0, 0), (.03, 0, 0), (0, 0, .02))):
        m.prism([(-.025, -.012), (.025, -.012), (.02, .012), (-.02, .012)], 0, .018, c, M=Matrix.Translation(pos) @ Matrix.Rotation(math.pi / 2, 4, 'X') @ Matrix.Translation((0, .012, -.009)) if False else Matrix.Translation(pos))


def fiber(m, p):
    # bundle the size of a stone pile (~16 cm) — thin loose strands vanished at game distance
    for k in range(22): a = R.uniform(-.2, .2); m.cyl((-.08, R.uniform(-.018, .018), .006 + R.uniform(0, .02)), (.08, R.uniform(-.025, .025) + a * .05, .006 + R.uniform(0, .022)), .0028, p['color'], seg=4)
    m.torus((0, 0, .016), .024, .004, '#8a6a3a', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def spool(m, p):
    m.cyl((0, 0, 0), (0, 0, .006), .025, WOOD, seg=12); m.cyl((0, 0, .044), (0, 0, .05), .025, WOOD, seg=12)
    m.cyl((0, 0, .006), (0, 0, .044), .02, p['color'], seg=12)


def fluff(m, p):
    for k in range(6): m.ico((R.uniform(-.03, .03), R.uniform(-.03, .03), .015 + (k % 2) * .01), (.02,) * 3, p['color'], sub=1, smooth=True)


def bolt(m, p):
    m.cyl((-.1, 0, .03), (.1, 0, .03), .03, p['color'], seg=12)
    m.box((.11, -.02, .002), (.04, .06, .003), p['color'])
    m.cyl((-.105, 0, .03), (.105, 0, .03), .008, WOOD, seg=6)


def leather(m, p):
    t = p['thick']
    m.prism([(-.08, -.05), (-.04, -.065), (.05, -.06), (.09, -.02), (.08, .05), (.0, .065), (-.07, .045)], 0, t, p['color'])
    m.cyl((-.04, -.03, t), (.06, -.03, t), .015, p['color'], seg=8)  # rolled edge


def fur(m, p):
    m.prism([(-.08, -.05), (.0, -.065), (.08, -.04), (.09, .04), (-.01, .06), (-.09, .03)], 0, .008, p['color'])
    for k in range(12): m.ico((R.uniform(-.07, .07), R.uniform(-.04, .04), .012), (.012, .01, .006), light(p['color'], .1), sub=0)


def lumps(m, p):
    # small pile ~10 cm across (same read size as the stone/ore piles)
    for k in range(p.get('n', 6) + 3):
        m.ico((R.uniform(-.04, .04), R.uniform(-.035, .035), .018 + (k % 3) * .014), (.022, .019, .017), p['color'], sub=0 if not p.get('porous') else 1)
    if p.get('stick'):
        for k in range(3): m.cyl((-.05, (k - 1) * .02, .012 + k * .01), (.05, (k - 1) * .02 + .01, .016 + k * .01), .009, p['color'], seg=5)


def heap(m, p):
    c = p['color']
    m.lathe([(0, 0), (.07, 0), (.05, .025), (.02, .045), (0, .05)], c, seg=14, smooth=True)
    if p.get('lumpy'):
        for k in range(8): m.ico((R.uniform(-.04, .04), R.uniform(-.04, .04), .02 + R.uniform(0, .02)), (.012,) * 3, dark(c, .85), sub=0)
    if p.get('grainy'):
        for k in range(10): m.ico((R.uniform(-.05, .05), R.uniform(-.05, .05), .005), (.004,) * 3, light(c, .1), sub=0)


def lump(m, p): m.ico((0, 0, .03), (.05, .04, .03), p['color'], sub=2, smooth=True)


def crystals(m, p):
    m.ico((0, 0, .015), (.045, .038, .018), '#8a8070', sub=1)  # host rock
    for k in range(7):
        a = TAU * k / 7
        m.lathe([(0, 0), (.012, .007), (.012, .045), (0, .062)], pk.mat(p['color'], rough=.3), seg=6, c=(math.cos(a) * .014, math.sin(a) * .014, .02),
                rot=(math.sin(a) * .4, -math.cos(a) * .4, 0), smooth=False)


def drops(m, p):
    m.lathe([(0, 0), (.05, 0), (.05, .004), (0, .004)], '#7a5a32', seg=10)  # bark slab the resin was scraped onto
    for k in range(7): m.lathe([(0, 0), (.016, .007), (.012, .022), (0, .034)], pk.mat(p['color'], rough=.2, alpha=.9), seg=8, c=(R.uniform(-.03, .03), R.uniform(-.03, .03), .003))


def bale(m, p):
    m.box((0, 0, .05), (.16, .1, .1), p['color'])
    for x in (-.04, .04): m.torus((x, 0, .05), .062, .003, '#8a6a3a', seg=4, tseg=3, rot=(0, math.pi / 2, math.pi / 4))
    for k in range(10): m.cyl((R.uniform(-.08, .08), -.05, R.uniform(.01, .09)), (R.uniform(-.09, .09), -.06, R.uniform(.0, .1)), .001, light(p['color'], .2), seg=3)


def pellets(m, p):
    sack(m, dict(color=p['bag'], tag=p['color']))
    for k in range(8): m.ball((R.uniform(-.02, .02) + .07, R.uniform(-.02, .02), .004), (.004,) * 3, p['color'], seg=5, rings=3)


def roll(m, p):
    m.cyl((-.06, 0, .04), (.06, 0, .04), .04, p['color'], seg=14)
    m.cyl((-.061, 0, .04), (.061, 0, .04), .014, '#8a6a3a', seg=8)
    m.box((0, -.05, .003), (.12, .03, .004), p['color'])


def pane(m, p):
    g = pk.mat(p['color'], alpha=.5, rough=.05, name='PK_PaneGlass')
    for k in range(3): m.box((0, k * .012, .07), (.12, .006, .14), g, rot=(-.08, 0, 0))
    m.box((0, .01, .005), (.13, .05, .01), WOOD)


def bricks(m, p):
    for k in range(6): m.box((((k % 3) - 1) * .065 + (k // 3) * .03, 0, .015 + (k // 3) * .031), (.06, .03, .03), p['color'] if k % 2 else dark(p['color'], .9))


def vase(m, p):
    m.lathe([(0, 0), (.03, 0), (.05, .05), (.045, .1), (.022, .13), (.028, .15), (.024, .15), (0, .145)], p['color'], seg=14)
    m.torus((0, 0, .09), .049, .006, p['glaze'], seg=14, tseg=4)


# ---------------------------------------------------------------- specialty crops
def cotton(m, p):
    for k in range(3):
        c = Vector(((k - 1) * .035, 0, .02))
        m.ico(c, (.018,) * 3, p['color'], sub=2, smooth=True)
        for j in range(4): a = TAU * j / 4; m.leaf(c + Vector((0, 0, -.012)), c + Vector((.02 * math.cos(a), .02 * math.sin(a), -.016)), .012, '#6a4a2a')


def stalks(m, p):
    for k in range(12): m.cyl((-.1, R.uniform(-.008, .008), .006), (.1, R.uniform(-.015, .015), .006 + R.uniform(0, .01)), .0015, p['color'], seg=3)
    for k in range(5): m.ball((.1, R.uniform(-.012, .012), .012), (.005,) * 3, p['flower'], seg=5, rings=3)
    m.torus((0, 0, .008), .012, .003, '#8a6a3a', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def sprig(m, p):
    c = p['color']; w = .022 if p.get('broad') else .014
    for k in range(4):
        a = (k - 1.5) * .25; base = Vector((-.04, 0, .004)); tip = base + Vector((.1 * math.cos(a), .1 * math.sin(a), .008))
        m.cyl(base, tip, .0015, dark(c, .7), seg=3)
        for j in range(3): t = base.lerp(tip, .35 + j * .25); m.leaf(t, t + Vector((.01, (j % 2 - .5) * .03, .004)), w, c)
        if p.get('flower'): m.ball(tip, (.004,) * 3, p['flower'], seg=5, rings=3)
    m.torus((-.03, 0, .006), .01, .0025, '#c8a870', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def cane(m, p):
    for k in range(3):
        y = (k - 1) * .022
        m.cyl((-.14, y, .012), (.14, y, .012), .011, p['color'], seg=8)
        for x in (-.07, 0, .07): m.cyl((x - .003, y, .012), (x + .003, y, .012), .012, p['node'], seg=8)


def ginger(m, p):
    c = p['color']
    for k, (x, y, a) in enumerate(((0, 0, 0), (.03, .012, .6), (-.028, .01, -.5), (.012, -.02, -1.2))):
        m.ico((x, y, .012), (.022, .012, .011), c, sub=1, rot=(0, 0, a), smooth=True)


def sticks(m, p):
    for k in range(5): m.cyl((-.05, (k - 2) * .009, .005 + (k % 2) * .006), (.05, (k - 2) * .009, .005 + (k % 2) * .006), .004, p['color'], seg=6)
    m.torus((0, 0, .008), .016, .002, '#c8302a', seg=8, tseg=3, rot=(0, math.pi / 2, 0))


def leafpile(m, p):
    m.lathe([(0, 0), (.045, .0), (.045, .003), (0, .003)], '#c8a870', seg=10)
    for k in range(16): a = R.uniform(0, TAU); d = R.uniform(0, .03); m.leaf((d * math.cos(a), d * math.sin(a), .006 + k * .0008), (d * math.cos(a) + .015, d * math.sin(a) + .005, .007 + k * .0008), .008, p['color'])


def cacao(m, p):
    c = p['color']; L = .13
    m.lathe([(0, 0), (.025, .02), (.035, .06), (.03, .1), (0, L)], c, seg=10, rot=(0, math.pi / 2, 0), c=(-L / 2, 0, .035))
    for k in range(5): a = TAU * k / 5; m.cyl((-L * .4, .035 * math.cos(a), .035 + .035 * math.sin(a)), (L * .4, .03 * math.cos(a), .035 + .03 * math.sin(a)), .004, dark(c, .8), seg=3)


# ---------------------------------------------------------------- dishes (shared ware, distinct contents)
WARE = {
    'bowl': ([(0, 0), (.035, 0), (.05, .035), (.047, .035), (.033, .006), (0, .006)], .03, .05),
    'deepbowl': ([(0, 0), (.035, 0), (.055, .05), (.052, .05), (.032, .006), (0, .006)], .045, .052),
    'plate': ([(0, 0), (.05, 0), (.07, .012), (.067, .013), (.048, .004), (0, .004)], .006, .062),
    'longplate': ([(0, 0), (.05, 0), (.07, .012), (.067, .013), (.048, .004), (0, .004)], .006, .062),
    'pot': ([(0, 0), (.045, 0), (.05, .05), (.046, .05), (.042, .006), (0, .006)], .042, .046),
    'pie': ([(0, 0), (.055, 0), (.062, .02), (.058, .02), (.05, .004), (0, .004)], .02, .058),
    'board': (None, .012, .07), 'tray': (None, .012, .07),
    'cup': ([(0, 0), (.02, 0), (.022, .03), (.019, .03), (.017, .006), (0, .006)], .03, .02),
    'teacup': ([(0, 0), (.02, 0), (.032, .035), (.029, .035), (.018, .005), (0, .005)], .03, .028),
    'mug': ([(0, 0), (.03, 0), (.03, .07), (.027, .07), (.027, .006), (0, .006)], .062, .027),
    'glass': ([(0, 0), (.022, 0), (.028, .09), (.025, .09), (.02, .006), (0, .006)], .08, .025),
    'tankard': ([(0, 0), (.034, 0), (.034, .09), (.031, .09), (.031, .008), (0, .008)], .08, .031),
    'saucer': ([(0, 0), (.02, 0), (.04, .018), (.037, .018), (.018, .004), (0, .004)], .016, .036),
}
WARE_COL = {'board': WOOD, 'tray': '#3a3a3a', 'mug': '#e8dcc8', 'tankard': '#8a6a3a', 'pot': '#5a4a3a', 'pie': '#d8c8a8', 'glass': None, 'saucer': '#f0ebe0'}


def dish(m, p):
    ware = p['ware']; prof, fill, rim = WARE[ware]; wc = WARE_COL.get(ware, '#f2efe8')
    sx = 1.5 if ware == 'longplate' else 1.0
    if ware in ('board', 'tray'): m.box((0, 0, .006), (.15, .1, .012), wc)
    elif ware == 'glass': m.lathe(prof, pk.mat('#d8eef2', alpha=.45, rough=.1, name='PK_Glass'), seg=14, close=False)
    else: m.lathe(prof, wc, seg=16, sx=sx, close=False)
    if ware in ('mug', 'tankard'): m.torus((rim + .012, 0, fill * .55), .016, .004, wc, seg=10, tseg=4, rot=(math.pi / 2, 0, 0), arc=.6)
    if ware == 'saucer' and p.get('jug'): m.lathe([(0, 0), (.025, 0), (.035, .05), (.015, .09), (.018, .1)], p['jug'], seg=12, c=(.08, 0, 0))
    base = fill
    for shape, col, amt in p['items']:
        food(m, shape, col, amt, base, rim * .85, sx)
        if shape in ('soup', 'drink', 'rice'): base = base + (.004 if shape != 'soup' else 0)


def food(m, shape, c, amt, z, r, sx=1.0):
    n = max(1, int(amt * 8))
    if shape == 'soup' or shape == 'drink': m.cyl((0, 0, z * .5), (0, 0, z * .92), r * (.95 if shape == 'drink' else .92), c, seg=14)
    elif shape == 'rice': m.lathe([(0, z * .7), (r * .9, z * .8), (r * .5, z + .018), (0, z + .024)], c, seg=12, smooth=True)
    elif shape == 'mound': m.lathe([(0, z), (r * .8, z), (r * .3, z + .025), (0, z + .028)], c, seg=12)
    elif shape == 'grains':
        for k in range(n * 3): m.ball((R.uniform(-r * .6, r * .6), R.uniform(-r * .6, r * .6), z + .012), (.003,) * 3, c, seg=4, rings=3)
    elif shape in ('cubes', 'cubes_big'):
        s = .012 if shape == 'cubes' else .02
        for k in range(n * 2): m.box((R.uniform(-r * .6, r * .6) * sx, R.uniform(-r * .6, r * .6), z + s * .7), (s,) * 3, c, rot=(R.uniform(0, 1), R.uniform(0, 1), R.uniform(0, 1)))
    elif shape in ('slices', 'leaves', 'leaf', 'specks'):
        s = {'slices': .02, 'leaves': .028, 'leaf': .014, 'specks': .005}[shape]
        for k in range(n * (2 if shape != 'specks' else 4)):
            p0 = Vector((R.uniform(-r * .65, r * .65) * sx, R.uniform(-r * .65, r * .65), z + .01 + k * .0008))
            m.leaf(p0, p0 + Vector((s * math.cos(k), s * math.sin(k), .003)), s * .7, c)
    elif shape == 'balls':
        for k in range(max(3, n)): m.ball((R.uniform(-r * .55, r * .55) * sx, R.uniform(-r * .55, r * .55), z + .011), (.011,) * 3, c, seg=8, rings=5)
    elif shape == 'noodles':
        for k in range(10): m.torus((R.uniform(-r * .3, r * .3), R.uniform(-r * .3, r * .3), z + .006 + k * .001), r * R.uniform(.3, .7), .002, c, seg=12, tseg=3, arc=.7)
    elif shape == 'onigiri':
        for k in range(2):
            x = (k - .5) * .06; m.prism([(-.025, 0), (.025, 0), (0, .04)], -.012, .012, c, M=Matrix.Translation((x, 0, z)) @ Matrix.Rotation(math.pi / 2, 4, 'X'))
            m.box((x, -.013, z + .008), (.03, .002, .016), '#1a2a1a')
    elif shape == 'flat': m.cyl((0, 0, z), (0, 0, z + .01), r * .85, c, seg=14)
    elif shape == 'loaf': m.lathe([(0, 0), (.04, 0), (.045, .05), (.03, .07), (0, .072)], c, seg=10, sx=1.6, c=(0, 0, z))
    elif shape == 'round': m.lathe([(0, 0), (.05, 0), (.052, .025), (.03, .055), (0, .06)], c, seg=12, c=(0, 0, z))
    elif shape == 'cake': m.box((0, 0, z + .02), (.09, .06, .04), c)
    elif shape in ('pie', 'lattice'):
        m.cyl((0, 0, z), (0, 0, z + .01), r * .97, c, seg=16)
        if shape == 'lattice':
            for k in range(-2, 3): m.box((k * .02, 0, z + .011), (.006, r * 1.8, .003), light(c, .2)); m.box((0, k * .02, z + .012), (r * 1.8, .006, .003), light(c, .2))
    elif shape == 'vent':
        for k in range(3): m.box(((k - 1) * .015, 0, z + .012), (.004, .02, .002), c)
    elif shape == 'fishpiece':
        for k in range(3): m.box(((k - 1) * .022, 0, z), (.018, .014, .012), c)
    elif shape == 'steak': m.ico((0, 0, z + .008), (.05, .035, .01), c, sub=2); m.cyl((-.03, 0, z + .018), (.03, 0, z + .018), .0015, dark(c, .5), seg=3)
    elif shape in ('skewer', 'skewer_insect'):
        for s in (-.02, .02):
            m.cyl((-.08, s, z + .012), (.08, s, z + .012), .002, WOOD, seg=4)
            for k in range(4): m.box((-.045 + k * .03, s, z + .012), (.016, .014, .014), c if shape == 'skewer' else '#6a9a3a')
    elif shape == 'wholefish':
        f = pk.Mesh('f'); fish(f, dict(color=c, belly=light(c, .3), L=.14, D=.3)); m.merge(f, Matrix.Translation((0, 0, z)))
    elif shape == 'fillets':
        for k in range(3): m.ico(((k - 1) * .035, 0, z + .008), (.018, .012, .008), c, sub=1)
    elif shape == 'rings':
        for k in range(4): m.torus((R.uniform(-.03, .03), R.uniform(-.03, .03), z + .006), .009, .003, c, seg=10, tseg=4)
    elif shape == 'shrimp':
        for k in range(3): s = pk.Mesh('s'); shrimp(s, dict(color=c)); m.merge(s, Matrix.Translation(((k - 1) * .03, .01, z)) @ Matrix.Diagonal((.6, .6, .6, 1)))
    elif shape == 'shells':
        for k in range(4): m.ball((R.uniform(-r * .5, r * .5), R.uniform(-r * .5, r * .5), z + .004), (.01, .008, .004), c, seg=8, rings=3)
    elif shape == 'egg': m.lathe([(0, 0), (.016, .006), (.02, .02), (.016, .038), (0, .048)], c, seg=10, c=(0, 0, z * .5))
    elif shape == 'friedegg': m.cyl((0, 0, z), (0, 0, z + .004), .04, c, seg=14); m.ball((.005, 0, z + .006), (.014, .014, .007), '#f2b020', seg=8, rings=4)
    elif shape == 'omelette': m.ball((0, 0, z + .006), (.055, .028, .016), c, seg=12, rings=5)
    elif shape in ('crickets', 'larvae'):
        for k in range(5):
            s = pk.Mesh('i'); insect(s, dict(color=c, kind='larva' if shape == 'larvae' else 'cricket'))
            m.merge(s, Matrix.Translation((R.uniform(-.03, .03), R.uniform(-.03, .03), z)) @ Matrix.Rotation(R.uniform(0, 6), 4, 'Z'))
    elif shape == 'cookies':
        for k in range(4): m.cyl(((k % 2 - .5) * .04, (k // 2 - .5) * .04, z + k * .002), ((k % 2 - .5) * .04, (k // 2 - .5) * .04, z + .006 + k * .002), .016, c, seg=10)
    elif shape == 'cake_slice':  # sponge / cream / sponge / cream top — a plain white wedge vanished on the white plate
        tri = [(0, 0), (.06, -.025), (.06, .025)]
        for k, (h, col) in enumerate(((.016, '#e3b45e'), (.006, c), (.016, '#e3b45e'), (.008, c))):
            z0 = z + sum(x for x, _ in ((.016, 0), (.006, 0), (.016, 0), (.008, 0))[:k]); m.prism(tri, z0, z0 + h, col)
    elif shape == 'berry_top': m.ball((.045, 0, z + .053), (.008,) * 3, c, seg=6, rings=4)
    elif shape == 'pudding': m.lathe([(0, 0), (.03, 0), (.024, .035), (0, .036)], c, seg=12, c=(0, 0, z)); m.lathe([(0, .033), (.023, .033), (0, .04)], '#6a3a14', seg=12, c=(0, 0, z))
    elif shape == 'pancakes':
        for k in range(3): m.cyl((0, 0, z + k * .008), (0, 0, z + k * .008 + .007), .04, c, seg=14)
    elif shape == 'pat': m.box((0, 0, z + .028), (.014, .014, .006), c)
    elif shape == 'bars':
        for k in range(3): m.box((0, (k - 1) * .028, .02), (.09, .022, .014), c)
    elif shape == 'foam': m.cyl((0, 0, z * .92), (0, 0, z * .92 + .008), r * .95, c, seg=14)
    elif shape == 'wedges':
        for k in range(n): m.prism([(0, 0), (.02, .006), (.02, -.006)], 0, .012, c, M=Matrix.Translation((R.uniform(-r * .5, r * .5) * sx, R.uniform(-r * .5, r * .5), z)) @ Matrix.Rotation(R.uniform(0, 6), 4, 'Z'))


def winebottle(m, p):
    shape = p.get('shape', 'wine'); glass = pk.mat('#3a5a3a' if shape == 'wine' else '#d8eef2', alpha=.55, rough=.1, name='PK_BottleGlass_' + shape)
    prof = {'wine': [(0, 0), (.028, 0), (.028, .14), (.012, .19), (.01, .23)], 'round': [(0, 0), (.04, .02), (.04, .08), (.014, .13), (.011, .16)],
            'tall': [(0, 0), (.024, 0), (.024, .17), (.01, .2), (.009, .24)]}[shape]
    m.lathe(prof, glass, seg=12, close=False)
    m.lathe([(0, .005), (prof[1][0] * .88, .006), (prof[2][0] * .88, prof[2][1] * .9), (0, prof[2][1] * .92)], p['liquid'], seg=12)
    top = prof[-1][1]; m.cyl((0, 0, top), (0, 0, top + .018), prof[-1][0] * 1.1, '#8a6a3a', seg=8)
    m.box((0, -prof[1][0] - .001, prof[2][1] * .5), (prof[1][0] * 1.3, .001, .04), p.get('mark', '#e8dcc0'))


def jug(m, p):
    m.lathe([(0, 0), (.04, 0), (.05, .05), (.03, .11), (.014, .13), (.016, .14)], p['color'], seg=14)
    m.torus((.03, 0, .09), .02, .005, p['color'], seg=10, tseg=4, rot=(math.pi / 2, 0, 0), arc=.6)
    m.cyl((0, 0, .14), (0, 0, .155), .013, '#8a6a3a', seg=8)


RECIPES = {k: v for k, v in globals().items() if callable(v) and not k.startswith('_')}
RECIPES['round'] = round_; RECIPES['block'] = None  # 'block' is ambiguous: stone vs food, resolved below


def recipe_fn(name, group):
    if name == 'block': return block_stone if group == 'materials' else block
    return RECIPES[name]


# ---------------------------------------------------------------- variants
def stack(src_mesh_fn, recipe, p, group):
    """Carry/floor stack: grain → sack, liquids/containers → 2×2 group, heaps → bigger heap, produce → crate of 6."""
    m = pk.Mesh('stack')
    if recipe == 'grain':
        sack(m, dict(color='#d8c8a0', tag=p['head']))
        for k in range(6): m.ball((R.uniform(-.015, .015), R.uniform(-.015, .015), .17 + k * .003), (.004, .003, .003), p['head'], seg=4, rings=3)
        return m
    if recipe in ('heap', 'seedpile', 'powder', 'lumps', 'pellets', 'sack', 'drops', 'crystals', 'pieces', 'leafpile', 'beans'):
        s = pk.Mesh('one'); src_mesh_fn(s); m.merge(s, Matrix.Diagonal((1.8, 1.8, 1.6, 1))); return m
    if group == 'materials':  # bundles/stacks of 3 copies
        for k, (x, z) in enumerate(((-.0, 0), (0, 0), (0, 0))):
            s = pk.Mesh('one'); src_mesh_fn(s); lo, hi = s.bounds()
            m.merge(s, Matrix.Translation((0, 0, k * (hi.z - lo.z) * .98)) @ Matrix.Rotation(k * .25, 4, 'Z'))
        return m
    # food: shallow crate with up to 6 copies
    s = pk.Mesh('one'); src_mesh_fn(s); lo, hi = s.bounds(); w, d = hi.x - lo.x, hi.y - lo.y
    nx, ny = (3, 2) if max(w, d) < .09 else (2, 1)
    cw, cd = nx * (w + .01) + .02, ny * (d + .01) + .02
    m.box((0, 0, .004), (cw, cd, .008), WOOD)
    for sx in (-1, 1): m.box((sx * cw / 2, 0, .02), (.008, cd, .04), WOOD)
    for sy in (-1, 1): m.box((0, sy * cd / 2, .02), (cw, .008, .04), dark(WOOD, .9))
    for i in range(nx):
        for j in range(ny):
            s = pk.Mesh('one'); src_mesh_fn(s); lo, hi = s.bounds()
            off = Vector(((i - (nx - 1) / 2) * (w + .01) - (lo.x + hi.x) / 2, (j - (ny - 1) / 2) * (d + .01) - (lo.y + hi.y) / 2, .008 - lo.z))
            m.merge(s, Matrix.Translation(off) @ Matrix.Rotation((i + j) * .3, 4, 'Z') if max(w, d) < .09 else Matrix.Translation(off))
    return m


ROT_BROWN, MOLD = '#4a3a22', '#c8d0b0'


def rot_materials(mats):
    """Rotten look: colours pulled toward brown-grey and darkened (shape kept), + mould spots added by caller."""
    out = []
    for m_ in mats:
        c = m_.diffuse_color; h = '#%02x%02x%02x' % tuple(int(255 * (min(1, v) ** (1 / 2.2))) for v in c[:3])
        out.append(pk.mat(mix(dark(h, .75), ROT_BROWN, .55), rough=.95, alpha=c[3]))
    return out


def seed_packet(m, crop_color, label):
    m.box((0, 0, .045), (.05, .006, .09), '#e8dcc0')
    m.box((0, -.0035, .055), (.036, .001, .036), crop_color)
    m.box((0, -.0035, .015), (.036, .001, .008), label)
    for k in range(5): m.ball((.035 + R.uniform(-.008, .008), R.uniform(-.008, .008), .003), (.003, .002, .002), dark(crop_color, .6), seg=4, rings=3)
