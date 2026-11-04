"""Field growth stages for every seed crop (BLENDER-1019 §5).

The game's FarmPlot grows continuously (0→100 %) and, after a harvest, the plant stays and regrows. Stages:
  S1 새싹 0–25 %, S2 생장 25–60 %, S3 성숙(꽃·풋열매) 60–99 %, S4 수확 가능 100 %,
  S5 수확 직후: the plant that remains after picking (stubble / bush without fruit / tree without fruit) — shown from
  harvest until 25 % of the regrow, then S2→S4 again (no bare soil, no sprout restart).
Five shared plant forms; species identity comes from the produce colour/shape and the form parameters.
Plants stand on the field tile (no soil in the mesh). Origin bottom centre, kit metres (character 1.2 m).
"""
import math, random
from mathutils import Vector
import pk

TAU = math.tau
R = random.Random(20261020)
LEAF, LEAF_D, STEM, BARK, STRAW = '#5f9a34', '#467a28', '#6a8a3a', '#6b4a2e', '#c9b06a'
STAGES = {1: ('새싹', '0–25%'), 2: ('생장', '25–60%'), 3: ('성숙', '60–99%'), 4: ('수확 가능', '100%'),
          5: ('수확 직후(재성장 시작)', '수확 후 0–25%, 이후 S2→S4')}
GROW = {1: .25, 2: .55, 3: .85, 4: 1.0, 5: .7}


def _blades(m, n, h, r, color, lean=.25, w=.006):
    for k in range(n):
        a = TAU * k / n + R.uniform(-.2, .2); d = Vector((math.cos(a), math.sin(a), 0))
        base = d * r * R.uniform(0, 1); tip = base + d * h * lean * R.uniform(.5, 1) + Vector((0, 0, h * R.uniform(.8, 1)))
        m.leaf(base, tip, w, color, normal=(-d.y, d.x, 0), thick=.0012)


def grass(m, s, p):
    """Cereals, flax, rice. S4 heads in the grain colour, S5 cut stubble + new blades."""
    h = p.get('h', .32)
    if s == 5:
        for k in range(14):
            a = TAU * k / 14; d = Vector((math.cos(a), math.sin(a), 0)) * R.uniform(.005, .04)
            m.cyl(d, d + Vector((0, 0, h * .14)), .0025, STRAW, seg=4)
        _blades(m, 8, h * .3, .03, LEAF); return
    g = GROW[s]; _blades(m, 6 + 6 * s, h * g, .045 * g + .01, LEAF if s < 4 else mix_straw(p))
    if s >= 3:
        for k in range(10):
            a = TAU * k / 10; d = Vector((math.cos(a), math.sin(a), 0)) * R.uniform(.005, .04)
            top = d + Vector((d.x * .4, d.y * .4, h * g)); m.cyl(d, top, .0018, STEM if s == 3 else STRAW, seg=3)
            head = p['color'] if s == 4 else '#9ab860'
            m.ball(top + Vector((0, 0, .02)), (.006, .006, .022), head, seg=6, rings=4, rot=(.3 if p.get('droop') else 0, 0, a))


def mix_straw(p): return '#a8a050'


def stalk(m, s, p):
    """Corn, sorghum, sunflower, sugarcane: one tall stalk with alternating leaves."""
    h = p.get('h', .55) * (GROW[s] if s != 5 else .45); kind = p.get('kind')
    for k in range(3 if kind == 'cane' else 1):
        off = Vector(((k - 1) * .03, 0, 0)) if kind == 'cane' else Vector()
        m.cyl(off, off + Vector((0, 0, h)), .008 if kind != 'cane' else .01, STEM if s != 5 else STRAW, seg=6)
        if s == 5 and kind != 'cane': continue
        for j in range(max(2, int(6 * h / .55))):
            z = h * (.2 + .7 * j / 6); a = j * 2.4
            d = Vector((math.cos(a), math.sin(a), 0)); m.leaf(off + Vector((0, 0, z)), off + d * .1 + Vector((0, 0, z + .05)), .025, LEAF, normal=(-d.y, d.x, .3))
    if s == 5:
        _blades(m, 5, .08, .02, LEAF); return
    if s >= 3 and kind == 'corn':
        for a in (0, math.pi):
            d = Vector((math.cos(a), math.sin(a), 0)); c = d * .025 + Vector((0, 0, h * .55))
            m.ball(c, (.012, .012, .035), p['color'] if s == 4 else '#a8b860', seg=8, rings=5, rot=(.4 * d.y, -.4 * d.x, 0))
            m.leaf(c - Vector((0, 0, .03)), c + Vector((0, 0, .04)) + d * .01, .02, '#a8b860', normal=(d.x, d.y, 0))
    if s >= 3 and kind == 'sunflower':
        top = Vector((0, -.02, h)); m.cyl(top, top + Vector((0, -.006, 0)), .05 if s == 4 else .025, '#f2c020', seg=14)
        m.cyl(top + Vector((0, -.006, 0)), top + Vector((0, -.012, 0)), .028 if s == 4 else .012, '#5a3a1e', seg=12)
    if s >= 3 and kind == 'sorghum':
        m.ball(Vector((0, 0, h + .03)), (.02, .02, .045), p['color'] if s == 4 else '#9ab860', seg=8, rings=5)


def rosette(m, s, p):
    """Root crops, leafy greens, alliums, herbs. S4 shows the produce shoulder/head; S5 = new rosette after picking."""
    g = GROW[s] if s != 5 else .35; L = p.get('leaf_len', .12) * g; n = p.get('leaves', 7)
    upright = p.get('upright', False); leaf = p.get('leaf', LEAF)
    for k in range(n + (2 if s >= 3 and n else 0)):
        a = TAU * k / n + R.uniform(-.15, .15); d = Vector((math.cos(a), math.sin(a), 0))
        tip = d * L * (.25 if upright else .7) + Vector((0, 0, L * (1.2 if upright else .7)))
        m.leaf(Vector((0, 0, .004)), tip, L * (.18 if upright else .5), leaf if k % 2 else LEAF_D, normal=(-d.y, d.x, .2 if upright else .6))
    if s in (3, 4) and p.get('show'):
        r = p.get('r', .025) * (1 if s == 4 else .7); c = p['color']
        if p['show'] == 'shoulder': m.ball((0, 0, r * .3), (r, r, r * .8), c, seg=10, rings=6)
        elif p['show'] == 'head': m.ball((0, 0, r * .9), (r, r, r * .9), c, seg=10, rings=6)
        elif p['show'] == 'spears':
            for k in range(5): a = TAU * k / 5; m.cyl((math.cos(a) * .015, math.sin(a) * .015, 0), (math.cos(a) * .018, math.sin(a) * .018, .09 * g), .005, c, seg=5)
        elif p['show'] == 'pads':
            for k in range(3): a = TAU * k / 3; m.cyl((math.cos(a) * .05, math.sin(a) * .05, .002), (math.cos(a) * .05, math.sin(a) * .05, .004), .04, LEAF, seg=10)
            if s == 4: m.ball((0, 0, .08), (.02, .02, .025), '#f2b8c8', seg=8, rings=5)
        elif p['show'] == 'flowers':
            for k in range(5): a = TAU * k / 5; m.ball((math.cos(a) * L * .4, math.sin(a) * L * .4, L * .9), (.006,) * 3, c, seg=5, rings=3)


def bush(m, s, p):
    """Fruiting vegetables, beans, berries, vines on the ground. S3 flowers, S4 fruit, S5 bush without fruit."""
    g = GROW[s]; h = p.get('h', .2) * g; ground = p.get('ground')
    if ground:  # pumpkin/melon/cucumber family: spreading vine with big leaves
        for k in range(5 + s):
            a = TAU * k / 7; d = Vector((math.cos(a), math.sin(a), 0)) * .12 * g
            m.cyl(Vector((0, 0, .006)), d + Vector((0, 0, .006)), .003, STEM, seg=4)
            m.ball(d + Vector((0, 0, .02 * g + .005)), (.035 * g + .01, .035 * g + .01, .012), LEAF if k % 2 else LEAF_D, seg=8, rings=4)
    else:
        for k in range(3 + s):
            a = TAU * k / 6 + R.uniform(-.3, .3); d = Vector((math.cos(a), math.sin(a), 0)) * .035 * g
            leaf = ('#b8a848' if k % 2 else '#9a9040') if (s == 4 and not p.get('n', 5)) else (LEAF if k % 2 else LEAF_D)  # tuber crops: yellowing tops = ready
            m.ico(d + Vector((0, 0, h * R.uniform(.5, .85))), (h * .35, h * .35, h * .3), leaf, sub=1)
        m.cyl((0, 0, 0), (0, 0, h * .5), .005, STEM, seg=5)
        if p.get('stake'): m.cyl((.03, 0, 0), (.03, 0, h * 1.25), .003, BARK, seg=4)
    if s == 3:
        for k in range(5): m.ball(_spot(h, ground, g), (.005,) * 3, p.get('flower', '#f8f0c0'), seg=5, rings=3)
    if s == 4:
        fr, sh = p.get('fruit_r', .015), p.get('fruit', 'ball')
        for k in range(p.get('n', 5)):
            c = _spot(h, ground, g)
            if sh == 'pod': m.ball(c, (.005, .005, fr * 1.6), p['color'], seg=6, rings=4, rot=(R.uniform(-.5, .5), R.uniform(-.5, .5), 0))
            elif sh == 'long': m.ball(c, (fr * .5, fr * .5, fr * 1.5), p['color'], seg=8, rings=5, rot=(R.uniform(0, 1.4), 0, R.uniform(0, 3)))
            else: m.ball(c, (fr, fr, fr * .95), p['color'], seg=8, rings=5)


def _spot(h, ground, g):
    a = R.uniform(0, TAU); d = Vector((math.cos(a), math.sin(a), 0))
    return d * R.uniform(.03, .1) * g + Vector((0, 0, .02)) if ground else d * h * .32 + Vector((0, 0, h * R.uniform(.35, .85)))


def tree(m, s, p):
    """Orchard trees/palms/tea bushes. S3 blossom, S4 fruit, S5 the same tree without fruit (regrows fruit)."""
    g = GROW[s] if s != 5 else 1.0; H = p.get('h', .8) * (.3 + .7 * g); palm = p.get('palm'); bushy = p.get('bushy')
    trunk = H * (.35 if bushy else .55)
    m.cyl((0, 0, 0), (0, 0, trunk), .012 + .02 * g, BARK, seg=7, r2=.01 + .012 * g)
    if palm:
        for k in range(7):
            a = TAU * k / 7; d = Vector((math.cos(a), math.sin(a), 0))
            m.leaf(Vector((0, 0, trunk)), Vector((0, 0, trunk + .05)) + d * H * .45, .06 * g + .02, LEAF if k % 2 else LEAF_D, normal=(0, 0, 1))
        top = Vector((0, 0, trunk - .02))
    else:
        cr = H * (.32 if not bushy else .4)
        for k in range(4):
            a = TAU * k / 4; d = Vector((math.cos(a), math.sin(a), 0)) * cr * .45
            m.ico(d + Vector((0, 0, trunk + cr * .55)), (cr * .7, cr * .7, cr * .6), LEAF if k % 2 else LEAF_D, sub=1)
        m.ico((0, 0, trunk + cr * 1.0), (cr * .75,) * 3, LEAF, sub=1)
        top = Vector((0, 0, trunk + cr * .6))
    if s == 3 and not palm:
        for k in range(10): m.ball(_canopy(top, H, palm), (.008,) * 3, p.get('blossom', '#f6dde6'), seg=5, rings=3)
    if s == 4:
        fr, sh = p.get('fruit_r', .018), p.get('fruit', 'ball')
        if sh == 'bunch':  # banana / dates hang under the crown
            for k in range(6): m.ball(top + Vector((math.cos(k) * .03, math.sin(k) * .03, -.03 - k * .008)), (fr * .5, fr * .5, fr * 1.3), p['color'], seg=6, rings=4)
        elif sh == 'trunkpod':  # cacao pods grow on the trunk
            for k in range(4): a = TAU * k / 4; m.ball((math.cos(a) * .025, math.sin(a) * .025, trunk * (.4 + .12 * k)), (fr * .6, fr * .6, fr * 1.2), p['color'], seg=8, rings=5)
        else:
            for k in range(p.get('n', 9)): m.ball(_canopy(top, H, palm), (fr, fr, fr * .95), p['color'], seg=8, rings=5)


def _canopy(top, H, palm):
    a = R.uniform(0, TAU); e = R.uniform(-.4, .9)
    return top + Vector((math.cos(a) * math.cos(e), math.sin(a) * math.cos(e), math.sin(e))) * H * .3


FORMS = {'grass': grass, 'stalk': stalk, 'rosette': rosette, 'bush': bush, 'tree': tree}

# crop id → (form, params). Produce colour comes from the catalog when not given here.
PLANTS = {
    'Grain_Rice': ('grass', dict(h=.3, droop=True)), 'Grain_Wheat': ('grass', {}), 'Grain_Barley': ('grass', {}), 'Grain_Oat': ('grass', dict(droop=True)),
    'Grain_Rye': ('grass', dict(h=.38)), 'Grain_Millet': ('grass', dict(h=.3)), 'Grain_Buckwheat': ('rosette', dict(upright=True, leaf_len=.2, show='flowers', color='#f4f0e8')),
    'Crop_Flax': ('grass', dict(h=.34, color='#7aa0d8')),
    'Grain_Corn': ('stalk', dict(kind='corn', h=.6)), 'Grain_Sorghum': ('stalk', dict(kind='sorghum', h=.55)), 'Nut_Sunflower': ('stalk', dict(kind='sunflower', h=.6)),
    'Crop_Sugarcane': ('stalk', dict(kind='cane', h=.6)),
    'Veg_Potato': ('bush', dict(h=.16, fruit='ball', fruit_r=.012, n=0, flower='#e8e0f8')), 'Veg_SweetPotato': ('bush', dict(ground=True, n=0, flower='#e8c8f0')),
    'Veg_Carrot': ('rosette', dict(upright=True, show='shoulder', r=.012, leaf='#5aa83a')), 'Veg_Radish': ('rosette', dict(show='shoulder', r=.02)),
    'Veg_Turnip': ('rosette', dict(show='shoulder', r=.022)), 'Veg_Beet': ('rosette', dict(show='shoulder', r=.022, leaf='#7a3a4a')),
    'Crop_SugarBeet': ('rosette', dict(show='shoulder', r=.025)), 'Veg_Taro': ('rosette', dict(upright=True, leaf_len=.2, leaves=4)),
    'Veg_LotusRoot': ('rosette', dict(leaves=3, leaf_len=.08, show='pads')), 'Veg_Onion': ('rosette', dict(upright=True, show='shoulder', r=.02, leaves=6)),
    'Veg_Garlic': ('rosette', dict(upright=True, show='shoulder', r=.014, leaves=6)), 'Veg_Leek': ('rosette', dict(upright=True, leaf_len=.2, leaves=6, show='shoulder', r=.01)),
    'Veg_Cabbage': ('rosette', dict(show='head', r=.045, leaf_len=.1)), 'Veg_NapaCabbage': ('rosette', dict(upright=True, show='head', r=.035, leaf_len=.14)),
    'Veg_Lettuce': ('rosette', dict(show='head', r=.035, leaf_len=.09)), 'Veg_Spinach': ('rosette', dict(leaf_len=.09, leaves=9)),
    'Veg_Asparagus': ('rosette', dict(leaves=0, show='spears', leaf_len=.1)), 'Crop_Ginger': ('rosette', dict(upright=True, leaf_len=.18)),
    'Crop_Herb': ('rosette', dict(leaf_len=.07, leaves=10, show='flowers')), 'Crop_Basil': ('rosette', dict(leaf_len=.07, leaves=10)),
    'Crop_Mint': ('rosette', dict(leaf_len=.06, leaves=12, leaf='#6ab84a')), 'Nut_Peanut': ('bush', dict(h=.12, n=0, flower='#f2d040')),
    'Veg_Pumpkin': ('bush', dict(ground=True, fruit_r=.05, n=1, flower='#f2c020')), 'Fruit_Watermelon': ('bush', dict(ground=True, fruit_r=.055, n=1, flower='#f2e040')),
    'Fruit_Melon': ('bush', dict(ground=True, fruit_r=.04, n=2, flower='#f2e040')), 'Fruit_KoreanMelon': ('bush', dict(ground=True, fruit_r=.03, n=2, fruit='long', flower='#f2e040')),
    'Veg_Cucumber': ('bush', dict(stake=True, h=.28, fruit='long', fruit_r=.012, flower='#f2e040')), 'Veg_Zucchini': ('bush', dict(ground=True, fruit='long', fruit_r=.02, n=2, flower='#f2c020')),
    'Veg_Tomato': ('bush', dict(stake=True, h=.28, fruit_r=.014)), 'Veg_ChiliPepper': ('bush', dict(h=.2, fruit='pod', fruit_r=.012, n=7)),
    'Veg_BellPepper': ('bush', dict(h=.2, fruit_r=.016, n=4)), 'Veg_Eggplant': ('bush', dict(h=.24, fruit='long', fruit_r=.018, n=3, flower='#b890d8')),
    'Veg_Soybean': ('bush', dict(h=.18, fruit='pod', n=8)), 'Veg_Pea': ('bush', dict(stake=True, h=.26, fruit='pod', n=8)), 'Veg_RedBean': ('bush', dict(h=.16, fruit='pod', n=8, color='#a8b860')),
    'Veg_KidneyBean': ('bush', dict(stake=True, h=.26, fruit='pod', n=8, color='#a8b860')), 'Veg_Chickpea': ('bush', dict(h=.16, fruit='pod', n=8, color='#b8b870')),
    'Veg_Lentil': ('bush', dict(h=.14, fruit='pod', n=8, color='#b8b870')), 'Nut_Sesame': ('rosette', dict(upright=True, leaf_len=.22, show='flowers', color='#f4ecf0')),
    'Crop_Pepper': ('bush', dict(stake=True, h=.3, fruit_r=.005, n=14)), 'Crop_Cotton': ('bush', dict(h=.24, fruit_r=.014, n=6, flower='#f4ecc8')),
    'Fruit_Strawberry': ('bush', dict(h=.07, fruit_r=.01, n=5, flower='#ffffff')), 'Fruit_Blueberry': ('bush', dict(h=.22, fruit_r=.005, n=16)),
    'Fruit_Raspberry': ('bush', dict(h=.24, fruit_r=.006, n=14)), 'Fruit_Blackberry': ('bush', dict(h=.24, fruit_r=.006, n=14)),
    'Fruit_Cranberry': ('bush', dict(h=.08, fruit_r=.005, n=14)), 'Fruit_Grape': ('bush', dict(stake=True, h=.3, fruit_r=.006, n=18)),
    'Fruit_Pineapple': ('rosette', dict(upright=True, leaf_len=.16, leaves=10, show='head', r=.03, leaf='#5a8a4a')),
    'Fruit_Apple': ('tree', {}), 'Fruit_Pear': ('tree', {}), 'Fruit_Peach': ('tree', dict(blossom='#f4b8c8')), 'Fruit_Plum': ('tree', dict(blossom='#ffffff')),
    'Fruit_Apricot': ('tree', dict(blossom='#f8d8e0')), 'Fruit_Cherry': ('tree', dict(fruit_r=.008, n=16, blossom='#f8c8d8')), 'Fruit_Persimmon': ('tree', {}),
    'Fruit_Pomegranate': ('tree', dict(bushy=True, blossom='#e04a2a')), 'Fruit_Fig': ('tree', dict(bushy=True)), 'Fruit_Tangerine': ('tree', dict(bushy=True)),
    'Fruit_Orange': ('tree', {}), 'Fruit_Lemon': ('tree', dict(bushy=True)), 'Fruit_Mango': ('tree', dict(h=.95)), 'Fruit_Mulberry': ('tree', dict(fruit_r=.007, n=16)),
    'Fruit_Banana': ('tree', dict(palm=True, fruit='bunch', fruit_r=.022, h=.85)), 'Fruit_Date': ('tree', dict(palm=True, fruit='bunch', fruit_r=.01, h=.95)),
    'Nut_Chestnut': ('tree', dict(h=.95, fruit_r=.014)), 'Nut_Walnut': ('tree', dict(h=.95)), 'Nut_Almond': ('tree', dict(blossom='#fbe8ee')),
    'Crop_TeaLeaf': ('tree', dict(bushy=True, h=.4, n=0)), 'Crop_CoffeeCherry': ('tree', dict(bushy=True, h=.55, fruit_r=.007, n=16)),
    'Crop_CacaoPod': ('tree', dict(fruit='trunkpod', fruit_r=.022, h=.8)),
}


def build(m, crop_id, stage, produce_color):
    form, params = PLANTS[crop_id]
    p = dict(params); p.setdefault('color', produce_color)
    FORMS[form](m, stage, p)
    return form
