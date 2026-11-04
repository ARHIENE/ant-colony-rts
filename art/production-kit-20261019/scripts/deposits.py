"""Surface deposits (1 cell each) × states Normal / Mining / Depleted, and underground-range overlay markers.

Coal seam, metal veins (copper, tin, iron, silver, gold, titanium, starlight), oil seeping ground, gas venting cracks.
Deposit visuals are independent of the extraction machine on top (removing/re-placing a machine never restores them).
Mining = partly dug face + rubble; Depleted = hollowed grey face with a marker stake (no new extractor allowed).
Overlay markers (flat, unlit-style emissive, engine places them on discovered underground ranges): edge, corner, fill, pin.
Mineable surface cells: the whole 1×1 tile; extractors snap their footprint centre on a deposit cell (manifest 'snap').
"""
import math
from mathutils import Vector
import pk
from facilities import Fac, TAU, STONE, SOOT, OIL, GAS_C, WOOD_D

R = __import__('random').Random(1019)
GROUND = '#8a7a5a'
METALS = {'Copper': ('구리', '#3aa88a', '#c07a42'), 'Tin': ('주석', '#c8ccd0', '#9aa0a6'), 'Iron': ('철', '#a8553a', '#5a4a44'),
          'Silver': ('은', '#f4f8fa', '#4a4e56'), 'Gold': ('금', '#f2c830', '#6a5a48'), 'Titanium': ('티타늄', '#7a9ab8', '#5a6068'),
          'Starlight': ('성휘석(가칭)', '#b89aff', '#3a3450')}
STATES = {'Normal': '정상', 'Mining': '채굴 중', 'Depleted': '고갈'}


def rock_face(m, state, rock, speck, glow=False, seam=False):
    m.box((0, 0, .02), (1.0, 1.0, .04), GROUND)
    h = {'Normal': .55, 'Mining': .38, 'Depleted': .14}[state]
    n = {'Normal': 7, 'Mining': 5, 'Depleted': 3}[state]
    col = rock if state != 'Depleted' else '#7a7672'
    for k in range(n):
        a = TAU * k / n; d = Vector((math.cos(a), math.sin(a), 0)) * R.uniform(.12, .3)
        m.ico(d + Vector((0, .1, h * .45)), (R.uniform(.16, .24), R.uniform(.14, .2), h * R.uniform(.4, .55)), col, sub=1)
    if state != 'Depleted':
        for k in range(18 if state == 'Normal' else 8):
            p = Vector((R.uniform(-.32, .32), R.uniform(-.15, .32), R.uniform(.08, h * .9)))
            if seam: m.box(p, (.12, .04, .03), speck, rot=(0, R.uniform(-.3, .3), 0))
            else: m.ico(p, (.03,) * 3, pk.mat(speck, metal=.6, rough=.3, emit=1.5 if glow else 0), sub=0)
    if state == 'Mining':
        for k in range(6): m.ico((R.uniform(-.35, .35), R.uniform(-.45, -.25), .04), (.04, .035, .03), col, sub=0)
        m.box((0, -.18, .02), (.5, .25, .03), '#6a5a42')
    if state == 'Depleted':
        m.cyl((.35, -.35, 0), (.35, -.35, .35), .012, WOOD_D, seg=5); m.box((.35, -.35, .33), (.12, .01, .07), '#c8402a')


def oil_seep(m, state):
    m.box((0, 0, .02), (1.0, 1.0, .04), GROUND)
    k = {'Normal': 1.0, 'Mining': .7, 'Depleted': .25}[state]
    for i in range(5):
        a = TAU * i / 5; d = Vector((math.cos(a), math.sin(a), 0)) * .15 * R.uniform(.5, 1)
        m.cyl(d + Vector((0, 0, .04)), d + Vector((0, 0, .045)), .22 * k, pk.mat(OIL, rough=.05) if state != 'Depleted' else '#5a5048', seg=12)
    if state == 'Depleted': m.cyl((.35, -.35, 0), (.35, -.35, .35), .012, WOOD_D, seg=5); m.box((.35, -.35, .33), (.12, .01, .07), '#c8402a')


def gas_vent(m, state):
    m.box((0, 0, .02), (1.0, 1.0, .04), GROUND)
    for i in range(4):
        a = R.uniform(0, TAU); L = R.uniform(.25, .4)
        m.box((0, 0, .041), (L, .025, .004), SOOT, rot=(0, 0, a))
    if state != 'Depleted':
        k = 1.0 if state == 'Normal' else .5
        for i in range(3): m.cyl((0, 0, .06 + i * .12), (0, 0, .062 + i * .12), (.12 + i * .06) * k, pk.mat(GAS_C, alpha=.25, rough=.1, name='PK_GasHaze'), seg=12)
    else: m.cyl((.35, -.35, 0), (.35, -.35, .35), .012, WOOD_D, seg=5); m.box((.35, -.35, .33), (.12, .01, .07), '#c8402a')


def build_deposits(col):
    out = []
    kinds = [('Coal', '석탄 광맥', lambda m, s: rock_face(m, s, '#4a4440', '#141210', seam=True), 'Fossil_Coal')]
    kinds += [(k, f'{v[0]} 광맥', (lambda vv: (lambda m, s: rock_face(m, s, vv[2], vv[1], glow=vv is METALS['Starlight'])))(v), f'Ore_{k}') for k, v in METALS.items()]
    kinds += [('Oil', '원유가 배어 나오는 땅', oil_seep, 'Fossil_CrudeOil'), ('Gas', '가스가 새는 지면 틈', gas_vent, 'Fossil_NaturalGas')]
    for kid, ko, fn, res in kinds:
        for st, sko in STATES.items():
            f = Fac(f'Deposit_{kid}_{st}', f'{ko} ({sko})', (1, 1), col, '매장 지점', f'자원 {res}. 상태={sko}. 시설과 별개 데이터')
            fn(f.body, st); f.root['resource'] = res; f.root['state'] = st
            f.root['snap'] = 'extractor footprint centre on this cell; depleted = no new extractor'
            if kid not in ('Oil', 'Gas') and st != 'Depleted': f.point('WorkPoint_0', (0, -.75, 0)); f.point('HandTarget', (0, -.15, .3))
            out.append(f.finish())
    return out


def build_overlay(col):
    """Discovered underground range: flat emissive marks the engine lays on cells (no underground level is modelled)."""
    out = []; glow = pk.mat('#4ac8ff', emit=1.5, alpha=.55, name='PK_OverlayGlow')
    f = Fac('Overlay_Edge', '지하 매장 범위 경계', (1, 0), col, '매장 지점', '범위 경계 한 칸 변'); f.body.box((0, 0, .01), (1.0, .06, .01), glow); out.append(f.finish())
    f = Fac('Overlay_Corner', '지하 매장 범위 모서리', (0, 0), col, '매장 지점', '경계 모서리'); f.body.box((0, 0, .01), (.06, .06, .012), glow); out.append(f.finish())
    f = Fac('Overlay_Fill', '지하 매장 범위 칸', (1, 1), col, '매장 지점', '범위 내부 칸(빗금)')
    for k in range(5): f.body.box((-.4 + k * .2, 0, .006), (.03, .7, .004), glow, rot=(0, 0, .785))
    out.append(f.finish())
    f = Fac('Overlay_Pin', '발견 매장층 표시', (0, 0), col, '매장 지점', '발견 지점 핀(자원 아이콘은 엔진 UI)')
    f.body.cyl((0, 0, 0), (0, 0, .5), .015, glow, seg=6); f.body.ball((0, 0, .55), (.06,) * 3, glow, seg=10, rings=6); out.append(f.finish())
    return out


ALL = [build_deposits, build_overlay]
