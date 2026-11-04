"""FURNITURE-97 recipes, built directly in kit units (no later scaling).

Resident: ~2.24 tall, ~1.76 wide, work surface .80, seat .42, bed top .62. Front = -Y, Z up.
Same low-poly helpers and palette as art/colony-core (geometry.py, read only). No resident meshes.
Each recipe places its own point Empties (WorkPoint_0, SeatPoint, LiePoint, HandTarget, InputPoint).
"""
import bpy, math
import geometry as g

TAU = math.tau


def P(name, pos):
    e = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(e)
    e.parent = g.ROOT; e.location = pos; e.empty_display_size = .15
    return e


class pivot:
    """with pivot('Door_Hinge', (x, y, z)): parts made inside are parented to the pivot."""
    def __init__(self, name, pos): self.name, self.pos = name, pos
    def __enter__(self):
        self.e = P(self.name, self.pos); self.before = set(bpy.data.objects); return self.e
    def __exit__(self, *a):
        for o in set(bpy.data.objects) - self.before:
            if o.parent is g.ROOT:
                o.parent = self.e; o.matrix_parent_inverse = self.e.matrix_world.inverted()


def turn(o, x=0, y=0, z=0):
    o.rotation_euler = (x, y, z); return o


def disc(name, p, r, h, mat='Wood', axis='Z', v=16):
    d = {'X': (h, 0, 0), 'Y': (0, h, 0), 'Z': (0, 0, h)}[axis]
    return g.rod(name, p, (p[0] + d[0], p[1] + d[1], p[2] + d[2]), r, mat, v)


def table(w, d, h, top='CutWood', leg='Bark'):
    for x in (-w / 2 + .14, w / 2 - .14):
        for y in (-d / 2 + .14, d / 2 - .14): g.rod('Leg', (x, y, 0), (x, y, h - .1), .08, leg)
    g.box('Top', (0, 0, h - .05), (w, d, .1), top)


def stool(p, h=.42, mat='CutWood'):
    for a in range(3):
        t = a * TAU / 3; g.rod('Stool leg', (p[0] + .22 * math.cos(t), p[1] + .22 * math.sin(t), 0), (p[0], p[1], h - .08), .045, 'Bark')
    disc('Stool seat', (p[0], p[1], h - .08), .34, .08, mat)


def bed_frame(w, d, legs=.42, top=.62):
    for x in (-w / 2 + .1, w / 2 - .1):
        for y in (-d / 2 + .1, d / 2 - .1): g.rod('Bed leg', (x, y, 0), (x, y, legs), .08, 'Bark')
    g.box('Bed base', (0, 0, legs), (w, d, .14), 'Wood')
    g.box('Mattress', (0, -.05, top - .08), (w - .12, d - .2, .16), 'Cream', .05)
    g.box('Headboard', (0, d / 2 - .04, legs + .45), (w, .1, .9), 'Wood')


def door_frame(width=2.3, height=3.0):
    for x in (-width / 2 - .12, width / 2 + .12): g.box('Frame post', (x, 0, height / 2), (.24, .34, height), 'Bark')
    g.box('Lintel', (0, 0, height + .1), (width + .5, .36, .22), 'Bark')


def figure(p, s=1.0, mat='Stone'):
    """Stylised round resident silhouette for statues (not a rig, not the character model)."""
    x, y, z = p
    g.rock('Statue body', (x, y, z + .55 * s), (.6 * s, .5 * s, .6 * s), mat, 2)
    g.rock('Statue head', (x, y, z + 1.45 * s), (.62 * s, .55 * s, .52 * s), mat, 2)
    for side in (-1, 1):
        g.rock('Statue ear', (x + .42 * s * side, y, z + 2.0 * s), (.14 * s, .1 * s, .32 * s), mat, 1)
        g.rod('Statue arm', (x + .5 * s * side, y - .1 * s, z + .75 * s), (x + .62 * s * side, y - .35 * s, z + .5 * s), .12 * s, mat)


# ---------------- 타일 ----------------
def tile():
    g.asset('CC_Floor')
    for i in range(6): g.box('Floor plank', ((i - 2.5) * .4, 0, .04), (.38, 2.4, .08), 'Wood' if i % 2 else 'CutWood', .015)
    for y in (-1.0, 1.0): g.box('Cross batten', (0, y, .09), (2.4, .1, .03), 'Bark', .01)
    P('WorkPoint_0', (0, -1.75, 0))
    g.asset('CC_Pillar')
    g.box('Plinth', (0, 0, .15), (1.0, 1.0, .3), 'Stone', .06)
    g.rod('Shaft', (0, 0, .3), (0, 0, 3.4), .3, 'Bark', 9)
    for z in (1.0, 2.2): g.lash((0, -.31, z))
    g.box('Capital', (0, 0, 3.5), (1.0, 1.0, .25), 'Wood', .05)
    P('WorkPoint_0', (0, -1.35, 0))
    g.asset('CC_CapWall')
    g.box('Earth core', (0, 0, 1.25), (2.7, .5, 2.5), 'Earth', .06)
    cols = ('Iron', 'Teal', 'Amber')
    for r in range(3):
        for c in range(3):
            disc('Bottle cap', ((c - 1) * .85, -.27, .45 + r * .8), .36, .06, cols[(r + c) % 3], 'Y', 18)
    P('WorkPoint_0', (0, -1.3, 0))
    for rid, barred in (('CC_LockedDoor', False), ('CC_BarredDoor', True)):
        g.asset(rid)
        door_frame()
        with pivot('Door_Hinge', (-1.12, 0, 0)):
            if barred:
                for i in range(6): g.rod('Bar', (-1.0 + i * .4, 0, .05), (-1.0 + i * .4, 0, 2.85), .045, 'Iron')
                for z in (.6, 2.2): g.box('Cross bar', (0, 0, z), (2.2, .1, .1), 'Iron', .01)
            else:
                for i in range(5): g.box('Door board', (-.88 + i * .44, 0, 1.45), (.42, .14, 2.8), 'Wood' if i % 2 else 'CutWood', .02)
                for z in (.6, 2.3): g.box('Iron band', (0, -.08, z), (2.2, .03, .14), 'Iron', .01)
                g.box('Lock box', (.85, -.12, 1.35), (.28, .1, .36), 'Iron', .02)
                disc('Keyhole', (.85, -.18, 1.32), .05, .02, 'Dark', 'Y', 8)
        P('WorkPoint_0', (0, -1.2, 0))


# ---------------- 생활 ----------------
def living():
    g.asset('CC_SingleBed'); bed_frame(1.7, 3.0)
    g.rock('Pillow', (0, 1.05, .7), (.5, .26, .14), 'LeafLight', 1)
    P('LiePoint', (0, 0, .62)); P('WorkPoint_0', (0, -2.1, 0))
    g.asset('CC_DoubleBed'); bed_frame(3.2, 3.0)
    for x in (-.75, .75): g.rock('Pillow', (x, 1.05, .7), (.5, .26, .14), 'LeafLight', 1)
    g.box('Blanket', (0, -.5, .63), (3.0, 1.6, .06), 'Teal', .02)
    P('LiePoint', (-.75, 0, .62)); P('WorkPoint_0', (-.75, -2.1, 0))
    g.asset('CC_BunkBed')
    for x in (-.95, .95):
        for y in (-1.45, 1.45): g.rod('Bunk post', (x, y, 0), (x, y, 3.5), .09, 'Bark')
    g.box('Lower base', (0, 0, .42), (1.9, 2.9, .14), 'Wood'); g.box('Lower mattress', (0, 0, .54), (1.75, 2.7, .16), 'Cream', .05)
    g.box('Upper base', (0, 0, 2.3), (1.9, 2.9, .14), 'Wood'); g.box('Upper mattress', (0, 0, 2.42), (1.75, 2.7, .16), 'Cream', .05)
    g.box('Upper rail', (0, -1.45, 2.85), (1.9, .08, .1), 'Wood', .01)
    for z in (.9, 1.4, 1.9, 2.4): g.rod('Ladder rung', (1.05, -1.0, z), (1.05, -.4, z), .04, 'CutWood')
    g.rock('Pillow', (0, 1.0, .7), (.5, .26, .14), 'LeafLight', 1)
    P('LiePoint', (0, 0, .62)); P('WorkPoint_0', (0, -2.1, 0))
    g.asset('CC_Hammock')
    for y in (-2.0, 2.0):
        g.rod('Hammock post', (-.5, y, 0), (0, y, 2.3), .1, 'Bark'); g.rod('Hammock post', (.5, y, 0), (0, y, 2.3), .1, 'Bark')
    # Sagging cloth: chain of slats along Y, lowest in the middle.
    n = 9
    for i in range(n):
        y = -1.6 + i * 3.2 / (n - 1); z = .62 + .55 * (y / 1.6) ** 2
        slope = math.atan(2 * .55 * y / 1.6 ** 2)
        turn(g.box('Hammock cloth', (0, y, z), (1.3, .4, .05), 'Cream', .01), slope, 0, 0)
    for y, z in ((-1.6, 1.17), (1.6, 1.17)): g.rod('Hammock rope', (0, y, z), (0, y * 1.25, 2.2), .03, 'Cream')
    P('LiePoint', (0, 0, .64)); P('WorkPoint_0', (0, -2.4, 0))
    g.asset('CC_Nursery')
    for x in (-.55, .55):
        for y in (-.4, .4): g.rod('Cradle stand', (x, y, 0), (x, y * .4, .35), .05, 'Bark')
    with pivot('Cradle_Pivot', (0, 0, .35)):
        for x in (-.55, .55): turn(g.rock('Rocker', (x, 0, .38), (.08, .55, .08), 'Bark', 1))
        g.bowl('Cradle basket', (0, 0, .42), .62, .42, 'CutWood')
        g.rock('Swaddle', (0, .05, .62), (.32, .4, .16), 'Cream', 1)
        g.leaf('Canopy leaf', (0, .35, 1.0), 1.2, .9, math.pi / 2, 'LeafLight')
    P('HandTarget', (0, -.45, .8)); P('WorkPoint_0', (0, -1.4, 0))
    g.asset('CC_BanquetTable'); table(5.2, 1.5, .8)
    for y in (-1.25, 1.25):
        for x in (-2.2, 0, 2.2): g.rod('Bench leg', (x, y, 0), (x, y, .36), .07, 'Bark')
        g.box('Long bench', (0, y, .38), (5.0, .55, .08), 'Wood')
    for x in (-1.8, -.6, .6, 1.8): g.bowl('Dish', (x, -.35, .8), .22, .08, 'Cream')
    g.rock('Roast', (0, .15, .92), (.45, .3, .2), 'Fungus', 1)
    P('SeatPoint', (0, -1.25, .42)); P('WorkPoint_0', (0, -2.2, 0))
    g.asset('CC_Bathtub')
    g.bowl('Tub', (0, 0, 0), 1.15, .85, 'Wood')
    g.lash((0, -1.05, .65))
    disc('Bath water', (0, 0, .58), 1.0, .04, 'Teal', 'Z', 18)
    P('SeatPoint', (0, 0, .4)); P('WorkPoint_0', (0, -1.9, 0))
    g.asset('CC_Hearth')
    g.box('Hearth base', (0, 0, .28), (1.8, 1.3, .56), 'Stone', .08)
    g.box('Fire mouth', (0, -.64, .25), (.7, .04, .3), 'Dark', 0)
    g.rock('Ember', (0, -.5, .22), (.22, .12, .08), 'Amber', 1)
    g.bowl('Pot', (0, 0, .56), .5, .34, 'Iron')
    g.rod('Chimney', (.6, .45, .56), (.6, .45, 2.2), .16, 'Stone', 8)
    P('HandTarget', (0, -.2, .85)); P('WorkPoint_0', (0, -1.3, 0))


# ---------------- 저장·환경·전력 ----------------
def utility():
    g.asset('CC_Jar')
    g.rock('Jar belly', (0, 0, .75), (.75, .75, .78), 'Fungus', 2)
    g.rod('Jar neck', (0, 0, 1.35), (0, 0, 1.6), .38, 'Fungus', 12)
    disc('Jar lid', (0, 0, 1.6), .44, .08, 'CutWood', 'Z', 12)
    P('InputPoint', (0, -.2, 1.65)); P('WorkPoint_0', (0, -1.45, 0))
    g.asset('CC_FoodStore')
    g.box('Cellar walls', (0, 0, .4), (2.4, 1.8, .8), 'Stone', .06)
    g.box('Cellar dark', (0, 0, .79), (2.0, 1.4, .02), 'Dark', 0)
    with pivot('Lid_Hinge', (0, .8, .82)):
        for i in range(5): g.box('Lid plank', ((i - 2) * .44, 0, .86), (.42, 1.6, .08), 'Wood', .015)
        g.rod('Lid handle', (-.3, -.7, .95), (.3, -.7, .95), .04, 'Iron')
    P('InputPoint', (0, 0, .8)); P('WorkPoint_0', (0, -1.6, 0))
    g.asset('CC_Brazier')
    for a in range(3):
        t = a * TAU / 3; g.rod('Brazier leg', (.55 * math.cos(t), .55 * math.sin(t), 0), (.3 * math.cos(t), .3 * math.sin(t), .7), .05, 'Iron')
    g.bowl('Fire bowl', (0, 0, .7), .62, .32, 'Iron')
    for i in range(5): g.rock('Coal', (.25 * math.cos(i * 1.3), .25 * math.sin(i * 1.3), .85), (.14, .12, .1), 'Dark', 1)
    g.rock('Glow', (0, 0, .95), (.28, .28, .2), 'Amber', 1)
    P('InputPoint', (0, -.2, .95)); P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_Lantern')
    g.box('Lantern foot', (0, 0, .1), (.6, .6, .2), 'Stone', .04)
    g.rod('Lantern post', (0, 0, .2), (0, 0, 2.5), .07, 'Bark')
    g.rod('Lantern arm', (0, 0, 2.4), (0, -.5, 2.4), .05, 'Iron')
    g.box('Lantern glow', (0, -.5, 2.0), (.3, .3, .4), 'Amber', .03)
    g.box('Lantern cap', (0, -.5, 2.25), (.4, .4, .08), 'Iron', .02)
    P('InputPoint', (0, -.5, 2.0)); P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_Torch')
    g.rock('Torch base', (0, 0, .1), (.35, .35, .15), 'Stone', 1)
    g.rod('Torch stick', (0, 0, 0), (0, 0, 2.0), .07, 'Bark')
    g.bowl('Torch cup', (0, 0, 1.9), .2, .18, 'Iron')
    g.rock('Flame', (0, 0, 2.2), (.16, .16, .3), 'Amber', 1)
    P('InputPoint', (0, 0, 2.0)); P('WorkPoint_0', (0, -1.1, 0))
    g.asset('CC_FireflyLamp')
    g.rod('Lamp post', (0, 0, 0), (0, 0, 1.2), .08, 'Bark')
    g.rock('Glass jar', (0, 0, 1.55), (.32, .32, .4), 'Teal', 2)
    disc('Jar cork', (0, 0, 1.9), .16, .1, 'CutWood', 'Z', 8)
    for i in range(5): g.rock('Firefly', (.12 * math.cos(i * 2.1), .12 * math.sin(i * 2.1), 1.4 + i * .06), (.05, .05, .05), 'Amber', 1)
    P('InputPoint', (0, 0, 1.9)); P('WorkPoint_0', (0, -1.1, 0))
    g.asset('CC_Treadmill')
    for x in (-1.15, 1.15):
        g.rod('Wheel stand', (x, -1.2, 0), (x, 0, 1.85), .1, 'Bark'); g.rod('Wheel stand', (x, 1.2, 0), (x, 0, 1.85), .1, 'Bark')
    with pivot('Wheel_Pivot', (0, 0, 1.85)):
        n = 18
        for i in range(n):
            a = i * TAU / n; y, z = 1.75 * math.cos(a), 1.85 + 1.75 * math.sin(a)
            turn(g.box('Wheel plank', (0, y, z), (2.0, .55, .1), 'CutWood' if i % 2 else 'Wood', .01), a + math.pi / 2, 0, 0)
        for x in (-1.02, 1.02):
            for i in range(4):
                a = i * math.pi / 4; g.rod('Spoke', (x, -1.7 * math.cos(a), 1.85 - 1.7 * math.sin(a)), (x, 1.7 * math.cos(a), 1.85 + 1.7 * math.sin(a)), .05, 'Bark')
        g.rod('Axle', (-1.2, 0, 1.85), (1.2, 0, 1.85), .09, 'Iron')
    P('WorkPoint_0', (0, 0, .15))
    g.asset('CC_PowerWire')
    for x in (-1.2, 1.2):
        g.rod('Wire pole', (x, 0, 0), (x, 0, 1.7), .07, 'Bark'); g.rock('Insulator', (x, 0, 1.6), (.09, .09, .12), 'Cream', 1)
    g.rod('Wire', (-1.2, 0, 1.6), (1.2, 0, 1.6), .025, 'Iron')
    P('WorkPoint_0', (0, -1.0, 0))


# ---------------- 식량·작업·방어 ----------------
def production():
    g.asset('CC_MushroomFarm')
    for x in (-1.1, 1.1): g.rod('Bed log', (x, -1.0, .15), (x, 1.0, .15), .16, 'Bark')
    for y in (-1.05, 1.05): g.rod('Bed log', (-1.2, y, .15), (1.2, y, .15), .16, 'Bark')
    g.box('Compost bed', (0, 0, .15), (2.1, 1.9, .26), 'Soil', .03)
    for i, (x, y) in enumerate([(-.6, -.5), (0, -.4), (.6, -.55), (-.5, .35), (.2, .3), (.7, .45)]):
        h = .3 + .1 * (i % 3); g.rod('Stem', (x, y, .28), (x, y, .28 + h), .07, 'Cream')
        g.rock('Cap', (x, y, .28 + h), (.22, .22, .12), 'Fungus', 2)
    P('WorkPoint_0', (0, -1.75, 0))
    g.asset('CC_AphidPen')
    for x in (-1.5, 1.5):
        for y in (-1.2, 1.2): g.rod('Pen post', (x, y, 0), (x, y, 1.9 if y > 0 else 1.6), .09, 'Bark')
    for z in (.4, .8):
        g.rod('Pen rail', (-1.5, 1.2, z), (1.5, 1.2, z), .05); g.rod('Pen rail', (-1.5, -1.2, z), (-1.5, 1.2, z), .05); g.rod('Pen rail', (1.5, -1.2, z), (1.5, 1.2, z), .05)
    for i in range(6): g.leaf('Shed roof', ((i - 2.5) * .55, 0, 1.85), 2.8, .6, 0, 'Leaf' if i % 2 else 'LeafLight')
    g.box('Straw floor', (0, 0, .03), (3.0, 2.4, .06), 'Cream', .01)
    g.box('Trough', (0, -1.0, .3), (1.4, .4, .3), 'Wood', .03)
    g.box('Feed', (0, -1.0, .43), (1.25, .3, .05), 'LeafLight', 0)
    P('InputPoint', (0, -1.0, .45)); P('WorkPoint_0', (0, -1.85, 0))
    g.asset('CC_CareStation'); table(2.0, 1.2, .8)
    g.box('Soft pad', (0, 0, .83), (1.6, .9, .08), 'LeafLight', .03)
    g.bowl('Feed bowl', (.75, .35, .8), .2, .1, 'CutWood'); g.bottle((-.75, .35, .8))
    P('HandTarget', (0, -.2, .9)); P('WorkPoint_0', (0, -1.25, 0))
    g.asset('CC_Carpentry'); table(2.4, 1.2, .8)
    g.rod('Plank on bench', (-.9, -.1, .86), (.9, -.1, .86), .09, 'CutWood', 4)
    g.box('Vice', (1.05, -.5, .9), (.2, .2, .2), 'Iron', .02)
    for i in range(3): g.rock('Shavings', (-.4 + i * .4, .3, .82), (.12, .08, .03), 'CutWood', 1)
    P('HandTarget', (0, -.25, .86)); P('WorkPoint_0', (0, -1.25, 0))
    g.asset('CC_Stonecutter'); table(2.2, 1.2, .8, 'Wood', 'Stone')
    g.box('Stone block', (0, -.05, 1.0), (.8, .55, .4), 'Stone', .05)
    for i in range(4): g.rock('Chip', (-.7 + i * .45, .35, .83), (.08, .07, .04), 'Stone', 1)
    P('HandTarget', (0, -.3, 1.0)); P('WorkPoint_0', (0, -1.25, 0))
    g.asset('CC_SpinningWheel')
    g.box('Wheel bench', (0, .5, .3), (1.6, .5, .12), 'Wood')
    for x in (-.6, .6): g.rod('Bench leg', (x, .5, 0), (x, .5, .3), .06, 'Bark')
    for y in (.15, .85): g.rod('Wheel upright', (0, y, .3), (0, .5, 1.25), .05, 'Bark')
    with pivot('Wheel_Pivot', (0, .5, 1.1)):
        n = 12
        for i in range(n):
            a = i * TAU / n; turn(g.box('Rim', (0, .5 + .6 * math.cos(a), 1.1 + .6 * math.sin(a)), (.08, .34, .07), 'CutWood', .005), a + math.pi / 2, 0, 0)
        for i in range(3):
            a = i * math.pi / 3; g.rod('Spoke', (0, .5 - .58 * math.cos(a), 1.1 - .58 * math.sin(a)), (0, .5 + .58 * math.cos(a), 1.1 + .58 * math.sin(a)), .025, 'Bark')
    g.rod('Spindle', (-.6, .5, .45), (-.6, .5, .8), .04, 'CutWood'); g.rock('Yarn', (-.6, .5, .62), (.12, .12, .1), 'Cream', 1)
    stool((0, -.75, 0))
    P('SeatPoint', (0, -.75, .42)); P('HandTarget', (0, -.05, .9)); P('WorkPoint_0', (0, -1.6, 0))
    g.asset('CC_Smelter')
    g.rod('Furnace body', (0, 0, 0), (0, 0, 1.6), .85, 'Stone', 10)
    g.rod('Furnace top', (0, 0, 1.6), (0, 0, 2.6), .5, 'Stone', 10)
    g.box('Fire door', (0, -.82, .45), (.55, .06, .45), 'Amber', .02)
    g.box('Pour lip', (0, -.9, .95), (.25, .3, .08), 'Iron', .02)
    g.box('Bellows', (.95, -.1, .35), (.4, .7, .25), 'Bark', .05)
    P('InputPoint', (0, -.85, .5)); P('WorkPoint_0', (0, -1.75, 0))
    g.asset('CC_Kiln')
    g.rock('Kiln dome', (0, 0, .7), (1.15, 1.0, 1.0), 'Earth', 2)
    g.box('Kiln mouth', (0, -.98, .55), (.6, .06, .5), 'Dark', 0)
    g.rod('Kiln vent', (0, .1, 1.6), (0, .1, 1.95), .14, 'Stone', 8)
    g.box('Glass shelf', (1.45, -.2, .5), (.5, .8, .08), 'Wood', .01)
    for i in range(3): g.rod('Glass vessel', (1.45, -.45 + i * .25, .54), (1.45, -.45 + i * .25, .74), .07, 'Teal', 8)
    P('InputPoint', (0, -1.0, .55)); P('WorkPoint_0', (0, -1.75, 0))
    g.asset('CC_MineField')
    g.box('Turned soil', (0, 0, .03), (2.6, 2.2, .06), 'Soil', .02)
    for x, y in ((-.8, -.5), (.1, -.6), (.85, -.3), (-.4, .45), (.6, .55)):
        g.rock('Pod', (x, y, .05), (.2, .2, .1), 'Iron', 1); g.rock('Trigger', (x, y, .14), (.06, .06, .04), 'Amber', 1)
    g.rod('Warning stick', (-1.15, -1.0, 0), (-1.15, -1.0, 1.1), .03, 'Bark'); g.leaf('Warning leaf', (-1.15, -1.0, 1.15), .4, .3, 0, 'Amber')
    P('WorkPoint_0', (0, -1.6, 0))


# ---------------- 휴게 ----------------
def board_table(rid, kind):
    g.asset(rid)
    table(1.3, 1.3, .62)
    if kind == 'checker':
        for i in range(4):
            for j in range(4): g.box('Square', ((i - 1.5) * .25, (j - 1.5) * .25, .63), (.24, .24, .02), 'Cream' if (i + j) % 2 else 'Wood', 0)
        for x, y, m in ((-.37, -.37, 'Teal'), (.12, -.37, 'Teal'), (-.12, .37, 'Amber'), (.37, .12, 'Amber')): disc('Piece', (x, y, .64), .08, .06, m, 'Z', 8)
    elif kind == 'janggi':
        g.box('Janggi board', (0, 0, .63), (1.0, 1.0, .03), 'CutWood', 0)
        for i in range(5):
            g.box('Line', ((i - 2) * .22, 0, .655), (.012, .9, .005), 'Dark', 0); g.box('Line', (0, (i - 2) * .22, .655), (.9, .012, .005), 'Dark', 0)
        for k, (x, y) in enumerate(((-.44, -.44), (0, -.44), (.44, -.44), (-.22, .44), (.22, .44), (0, .22))):
            disc('Janggi piece', (x, y, .66), .09, .05, 'Teal' if y < 0 else 'Amber', 'Z', 8)
    else:
        g.box('Baduk block', (0, 0, .7), (1.05, 1.05, .16), 'CutWood', .02)
        for i in range(5):
            g.box('Line', ((i - 2) * .2, 0, .785), (.01, .85, .004), 'Dark', 0); g.box('Line', (0, (i - 2) * .2, .785), (.85, .01, .004), 'Dark', 0)
        for k, (x, y) in enumerate(((-.2, -.2), (0, .2), (.2, 0), (-.4, .2), (.2, .4))):
            g.rock('Go stone', (x, y, .8), (.07, .07, .03), 'White' if k % 2 else 'Dark', 1)
    for y in (-1.0, 1.0): stool((0, y, 0))
    P('SeatPoint', (0, -1.0, .42)); P('HandTarget', (0, -.2, .66)); P('WorkPoint_0', (0, -1.9, 0))


def recreation():
    board_table('CC_BoardGame', 'checker'); board_table('CC_Janggi', 'janggi'); board_table('CC_Baduk', 'baduk')
    g.asset('CC_GamblingDen')
    disc('Round table top', (0, 0, .54), .75, .08, 'Teal', 'Z', 14); g.rod('Table foot', (0, 0, 0), (0, 0, .54), .12, 'Bark')
    for i, (x, y) in enumerate(((-.2, -.1), (.15, .1))): turn(g.box('Die', (x, y, .69), (.14, .14, .14), 'White', .02), 0, 0, .5 * i)
    g.bowl('Dice cup', (.4, .25, .62), .14, .22, 'Bark')
    for i in range(4): disc('Coin', (-.35 + i * .07, .35, .62 + i * .02), .07, .02, 'Amber', 'Z', 10)
    for y in (-1.0, 1.0): stool((0, y, 0))
    P('SeatPoint', (0, -1.0, .42)); P('HandTarget', (0, -.2, .62)); P('WorkPoint_0', (0, -1.9, 0))
    g.asset('CC_WrestlingRing')
    disc('Sand ring', (0, 0, 0), 2.6, .12, 'Cream', 'Z', 20)
    for i in range(16):
        a = i * TAU / 16; g.rod('Ring rope', (2.6 * math.cos(a), 2.6 * math.sin(a), .14), (2.6 * math.cos(a + TAU / 16), 2.6 * math.sin(a + TAU / 16), .14), .06, 'Bark')
    for i in range(4):
        a = i * TAU / 4 + TAU / 8; g.rod('Corner stake', (2.8 * math.cos(a), 2.8 * math.sin(a), 0), (2.8 * math.cos(a), 2.8 * math.sin(a), .7), .08, 'Bark')
    P('WorkPoint_0', (0, -.75, .12))
    g.asset('CC_DartBoard')
    for x in (-.4, .4): g.rod('Board leg', (x, .3, 0), (x * .3, 0, 1.9), .06, 'Bark')
    for r, m in ((.6, 'Cream'), (.42, 'Teal'), (.24, 'Amber'), (.08, 'Dark')): disc('Target ring', (0, -.06 - (.6 - r) * .05, 1.55), r, .06, m, 'Y', 18)
    for x, z in ((.18, 1.62), (-.1, 1.45)): g.rod('Thorn dart', (x, -.12, z), (x, -.4, z + .03), .02, 'Bark', 5)
    P('WorkPoint_0', (0, -3.2, 0))
    g.asset('CC_Instrument')
    disc('Big drum', (-.55, 0, 0), .5, .75, 'Wood', 'Z', 14); disc('Drum skin', (-.55, 0, .75), .48, .03, 'Cream', 'Z', 14)
    disc('Small drum', (.55, .05, 0), .34, .6, 'Fungus', 'Z', 12); disc('Drum skin', (.55, .05, .6), .32, .03, 'Cream', 'Z', 12)
    for i in range(5): g.box('Xylophone bar', (-.45 + i * .22, .55, .8), (.18, .5 - i * .05, .05), 'CutWood', .01)
    for x in (-.6, .5): g.rod('Xylo stand', (x, .55, 0), (x, .55, .78), .04, 'Bark')
    P('HandTarget', (0, -.2, .78)); P('WorkPoint_0', (0, -1.2, 0))
    g.asset('CC_ExerciseRig')
    for x in (-1.1, 1.1): g.rod('Rack post', (x, .4, 0), (x, .4, 2.6), .08, 'Bark')
    g.rod('Pull-up bar', (-1.1, .4, 2.55), (1.1, .4, 2.55), .05, 'Iron')
    g.rod('Barbell', (-1.0, -.1, 1.2), (1.0, -.1, 1.2), .04, 'Bark')
    for x in (-.85, .85): g.rock('Stone weight', (x, -.1, 1.2), (.14, .3, .3), 'Stone', 1)
    for x in (-1.1, 1.1): g.box('Bar hook', (x, .1, 1.15), (.12, .5, .06), 'Iron', .01)
    P('WorkPoint_0', (0, -.6, 0))
    g.asset('CC_WebSwing')
    for x in (-1.3, 1.3):
        g.rod('Swing leg', (x, -.9, 0), (x, 0, 3.2), .09, 'Bark'); g.rod('Swing leg', (x, .9, 0), (x, 0, 3.2), .09, 'Bark')
    g.rod('Top bar', (-1.4, 0, 3.2), (1.4, 0, 3.2), .08, 'Bark')
    with pivot('Swing_Pivot', (0, 0, 3.2)):
        for x in (-.6, .6): g.rod('Silk strand', (x, 0, 3.2), (x, 0, .58), .025, 'White')
        seat = g.box('Swing_Seat', (0, 0, .52), (1.4, .6, .1), 'CutWood', .02)
    P('SeatPoint', (0, 0, .57)); P('WorkPoint_0', (0, -1.4, 0))
    g.asset('CC_HotSpring')
    for i in range(14):
        a = i * TAU / 14; g.rock('Spring rock', (2.3 * math.cos(a), 2.3 * math.sin(a), .25), (.45, .4, .35), 'Stone', 1)
    disc('Spring water', (0, 0, 0), 2.1, .32, 'Teal', 'Z', 18)
    for i in range(3): g.rock('Steam', (-.6 + i * .6, .4, .55 + .15 * i), (.22, .22, .16), 'White', 1)
    P('SeatPoint', (0, -.6, .35)); P('WorkPoint_0', (0, -3.1, 0))


# ---------------- 장식 ----------------
def plinth(h=.5, w=1.1):
    g.box('Plinth', (0, 0, h / 2), (w, w, h), 'Stone', .05)


def decoration():
    g.asset('CC_Statue'); plinth(); figure((0, 0, .5), .7)
    P('WorkPoint_0', (0, -1.5, 0))
    g.asset('CC_BronzeStatue'); plinth(.7, 1.3); figure((0, 0, .7), .85, 'Amber')
    g.box('Name plate', (0, -.66, .4), (.6, .03, .2), 'Iron', .01)
    P('WorkPoint_0', (0, -1.6, 0))
    g.asset('CC_FlowerPot')
    g.bowl('Pot', (0, 0, 0), .5, .55, 'Fungus'); disc('Pot soil', (0, 0, .48), .42, .03, 'Soil', 'Z', 10)
    for i, (x, y, h, m) in enumerate(((-.15, 0, .9, 'Amber'), (.12, .1, 1.1, 'Cream'), (0, -.15, .75, 'Amber'))):
        g.rod('Flower stem', (x, y, .5), (x, y, h), .025, 'Leaf'); g.rock('Blossom', (x, y, h), (.14, .14, .1), m, 1)
        g.leaf('Pot leaf', (x * 2, y * 2, .7), .35, .2, i, 'LeafLight')
    P('HandTarget', (0, -.1, .7)); P('WorkPoint_0', (0, -1.2, 0))
    g.asset('CC_Flag')
    g.rock('Flag base', (0, 0, .1), (.45, .45, .2), 'Stone', 1)
    g.rod('Flag pole', (0, 0, 0), (0, 0, 3.4), .05, 'Bark')
    g.box('Banner', (.6, 0, 2.95), (1.1, .03, .7), 'Teal', .01); g.box('Banner stripe', (.6, -.02, 2.95), (1.1, .02, .15), 'Amber', 0)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_Painting')
    g.rod('Easel leg', (-.5, -.1, 0), (-.2, 0, 2.0), .04, 'Bark'); g.rod('Easel leg', (.5, -.1, 0), (.2, 0, 2.0), .04, 'Bark'); g.rod('Easel leg', (0, .5, 0), (0, .05, 1.9), .04, 'Bark')
    g.box('Canvas', (0, -.08, 1.35), (1.1, .04, .85), 'Cream', .01)
    for x, z, m in ((-.25, 1.45, 'Teal'), (.2, 1.3, 'Amber'), (0, 1.15, 'LeafLight')): g.rock('Paint blob', (x, -.11, z), (.16, .02, .12), m, 1)
    g.box('Easel ledge', (0, -.1, .9), (1.1, .12, .05), 'Wood', .01)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_ShellDecoration')
    g.rock('Sand mound', (0, 0, .05), (.8, .7, .12), 'Cream', 1)
    for i in range(5):
        r = .5 - i * .09; g.rod('Shell whorl', (.05 * i, 0, .1 + i * .2), (.05 * i, 0, .3 + i * .2), r, 'White' if i % 2 else 'Cream', 9)
    g.rock('Shell lip', (-.35, -.15, .3), (.25, .12, .2), 'Fungus', 1)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_MarbleMosaic')
    turn(g.box('Mosaic slab', (0, 0, .75), (1.6, .14, 1.3), 'Stone', .03), -.15, 0, 0)
    cols = ('Teal', 'Amber', 'White', 'Fungus')
    for i in range(4):
        for j in range(3): g.rock('Marble', ((i - 1.5) * .34, -.1 + (j - 1) * .05, .4 + j * .34), (.1, .06, .1), cols[(i + j) % 4], 1)
    for x in (-.6, .6): g.box('Slab foot', (x, .1, .06), (.3, .5, .12), 'Stone', .02)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_BottleMobile')
    g.box('Mobile base', (0, 0, .08), (.6, .6, .16), 'Stone', .03)
    g.rod('Mobile post', (0, 0, 0), (0, 0, 2.6), .05, 'Bark')
    with pivot('Mobile_Pivot', (0, 0, 2.6)):
        g.rod('Mobile arm', (-.8, 0, 2.6), (.8, 0, 2.6), .03, 'Iron'); g.rod('Mobile arm', (0, -.8, 2.55), (0, .8, 2.55), .03, 'Iron')
        for x, y, m in ((-.8, 0, 'Teal'), (.8, 0, 'Amber'), (0, -.8, 'Iron'), (0, .8, 'Teal')):
            g.rod('String', (x, y, 2.58), (x, y, 2.05), .01, 'Cream', 4)
            if m == 'Teal': g.rod('Little bottle', (x, y, 1.75), (x, y, 2.05), .08, m, 8)
            else: disc('Cap', (x, y, 1.98), .14, .05, m, 'Y', 12)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_Carpet')
    g.box('Rug', (0, 0, .02), (2.4, 3.0, .04), 'Fungus', .01)
    g.box('Rug field', (0, 0, .042), (1.9, 2.5, .01), 'Cream', 0)
    for y in (-.8, 0, .8): g.box('Rug band', (0, y, .05), (1.9, .18, .01), 'Teal', 0)
    P('SeatPoint', (0, 0, .34)); P('WorkPoint_0', (0, -2.0, 0))
    g.asset('CC_Tapestry')
    for x in (-.9, .9): g.rod('Tapestry post', (x, 0, 0), (x, 0, 2.7), .06, 'Bark')
    g.rod('Hanging rod', (-1.0, 0, 2.55), (1.0, 0, 2.55), .04, 'Bark')
    g.box('Woven cloth', (0, -.03, 1.55), (1.6, .03, 1.9), 'Teal', .01)
    for z in (1.0, 1.6, 2.2): g.box('Woven band', (0, -.05, z), (1.6, .02, .16), 'Amber', 0)
    g.box('Woven center', (0, -.06, 1.6), (.5, .02, .5), 'Cream', 0)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_CuriosDisplay')
    for x in (-.9, .9): g.box('Cabinet side', (x, 0, .9), (.1, .6, 1.8), 'Wood')
    for z in (.1, .7, 1.3, 1.85): g.box('Cabinet shelf', (0, 0, z), (1.9, .6, .08), 'Wood', .01)
    disc('Button', (-.5, -.05, .75), .16, .05, 'Teal', 'Z', 10); disc('Coin', (0, -.05, .75), .14, .03, 'Amber', 'Z', 12)
    g.rod('Screw', (.5, -.05, .74), (.5, -.05, 1.05), .04, 'Iron', 6); g.rod('Pencil stub', (-.5, 0, 1.38), (.3, 0, 1.38), .05, 'Amber', 6)
    g.rock('Marble', (.55, 0, 1.45), (.1, .1, .1), 'Teal', 2)
    P('WorkPoint_0', (0, -1.3, 0))
    g.asset('CC_Monument')
    for i, w in enumerate((1.8, 1.4)): g.box('Monument step', (0, 0, .12 + i * .24), (w, w, .24), 'Stone', .03)
    g.rod('Obelisk', (0, 0, .48), (0, 0, 3.6), .4, 'Stone', 4)
    g.box('Inscription', (0, -.3, 1.4), (.35, .03, .6), 'Dark', 0)
    P('WorkPoint_0', (0, -1.5, 0))
    g.asset('CC_TrophyCase')
    for x in (-.9, .9): g.box('Case side', (x, 0, .8), (.1, .55, 1.6), 'Bark')
    for z in (.1, .8, 1.6): g.box('Case shelf', (0, 0, z), (1.9, .55, .08), 'Bark', .01)
    g.bowl('Trophy cup', (-.45, 0, .85), .16, .3, 'Amber'); g.rod('Cup foot', (-.45, 0, .14), (-.45, 0, .85), .05, 'Amber')
    g.rod('Enemy horn', (.3, 0, .9), (.55, 0, 1.4), .07, 'Cream', 6); g.rock('Wing scrap', (.4, .05, .3), (.3, .04, .15), 'Teal', 1)
    P('WorkPoint_0', (0, -1.3, 0))


# ---------------- 군사·마을·이동 ----------------
def military():
    g.asset('CC_Barracks')
    g.rock('Dummy base', (0, 0, .1), (.5, .5, .15), 'Stone', 1)
    g.rod('Dummy post', (0, 0, 0), (0, 0, 2.3), .07, 'Bark')
    g.rock('Straw body', (0, 0, 1.3), (.45, .38, .55), 'Cream', 1)
    g.rock('Straw head', (0, 0, 2.1), (.32, .3, .3), 'Cream', 1)
    g.rod('Dummy arms', (-.75, 0, 1.55), (.75, 0, 1.55), .05, 'Bark')
    g.box('Target patch', (0, -.36, 1.35), (.25, .04, .25), 'Amber', .01)
    P('WorkPoint_0', (0, -1.55, 0))
    g.asset('CC_ConscriptionPost'); table(2.0, 1.1, .8)
    g.box('Roster book', (-.3, -.1, .84), (.6, .45, .06), 'Cream', .01); g.box('Ink pad', (.4, -.1, .83), (.2, .2, .04), 'Dark', .01)
    g.rod('Notice pole', (.85, .5, 0), (.85, .5, 2.2), .05, 'Bark'); g.box('Notice board', (.85, .45, 1.9), (.7, .05, .5), 'CutWood', .01)
    P('HandTarget', (0, -.2, .84)); P('WorkPoint_0', (0, -1.2, 0))
    g.asset('CC_Armory')
    for x in (-1.1, 1.1): g.rod('Rack post', (x, 0, 0), (x, 0, 2.2), .08, 'Bark')
    for z in (.5, 1.7): g.rod('Rack bar', (-1.1, 0, z), (1.1, 0, z), .05, 'Bark')
    for i in range(4): g.rod('Spear', (-.75 + i * .5, -.05, .1), (-.75 + i * .5, -.05, 2.3), .03, 'CutWood', 6); g.rock('Spear tip', (-.75 + i * .5, -.05, 2.35), (.05, .05, .12), 'Iron', 1)
    for x in (-.5, .5): disc('Hung shield', (x, -.15, 1.05), .32, .06, 'Wood', 'Y', 12)
    P('WorkPoint_0', (0, -1.1, 0))
    g.asset('CC_PrisonerCamp')
    g.box('Cage floor', (0, 0, .05), (2.4, 2.4, .1), 'Wood', .02)
    for i in range(8):
        t = -1.1 + i * 2.2 / 7
        for a, b in (((t, -1.15), (t, 1.15)), ((-1.15, t), (1.15, t))):
            for p in (a, b): g.rod('Cage bar', (p[0], p[1], .1), (p[0], p[1], 2.5), .035, 'Bark')
    g.box('Cage roof', (0, 0, 2.55), (2.5, 2.5, .1), 'Bark', .02)
    g.rock('Stump seat', (0, .2, .25), (.35, .35, .2), 'Bark', 1)
    P('SeatPoint', (0, .2, .45)); P('WorkPoint_0', (0, -1.7, 0))
    g.asset('CC_ScoutPost')
    for x in (-.7, .7):
        for y in (-.5, .5): g.rod('Post leg', (x, y, 0), (x, y, 1.8), .07, 'Bark')
    g.box('Lookout deck', (0, 0, 1.8), (1.7, 1.3, .1), 'Wood', .02)
    for z in (.4, .9, 1.4): g.rod('Ladder rung', (-.4, -.7, z), (.4, -.7, z), .035, 'CutWood')
    for x in (-.4, .4): g.rod('Ladder rail', (x, -.7, 0), (x, -.62, 1.85), .04, 'Bark')
    for i in range(3):
        t = i * TAU / 3; g.rod('Tripod leg', (1.3 + .3 * math.cos(t), -.6 + .3 * math.sin(t), 0), (1.3, -.6, 1.2), .03, 'Bark')
    g.rod('Spyglass', (1.3, -.5, 1.25), (1.3, -1.0, 1.35), .07, 'Iron', 8)
    P('WorkPoint_0', (1.3, -1.6, 0))
    # 마을
    g.asset('CC_Hut')
    g.rod('Hut wall', (0, 0, 0), (0, 0, 1.9), 1.9, 'Earth', 12)
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=2.4, radius2=.1, depth=1.9, location=(0, 0, 2.8)); g.finish(bpy.context.object, 'Thatch roof', 'Cream')
    g.box('Hut door', (0, -1.85, .9), (.9, .1, 1.7), 'Dark', 0)
    P('WorkPoint_0', (0, -2.6, 0))
    g.asset('CC_House')
    g.box('House walls', (0, 0, 1.4), (4.4, 3.6, 2.8), 'Earth', .08)
    for s in (-1, 1): turn(g.box('Roof slab', (0, s * 1.0, 3.35), (4.8, 2.4, .18), 'Bark', .03), -s * .55, 0, 0)
    g.box('House door', (0, -1.82, .95), (1.0, .06, 1.9), 'Wood', .02)
    for x in (-1.4, 1.4): g.box('Window', (x, -1.82, 1.7), (.6, .05, .5), 'Teal', .01)
    P('WorkPoint_0', (0, -2.6, 0))
    g.asset('CC_Apartment')
    for f in range(3):
        g.box('Storey', (0, 0, 1.25 + f * 2.5), (4.8, 3.8, 2.5), 'Earth' if f % 2 == 0 else 'CutWood', .05)
        for x in (-1.5, 0, 1.5):
            if f == 0 and x == 0: continue
            g.box('Window', (x, -1.92, 1.4 + f * 2.5), (.6, .05, .6), 'Teal', .01)
        if f: g.box('Balcony', (0, -2.1, .2 + f * 2.5), (1.4, .4, .1), 'Wood', .02)
    g.box('Apartment door', (0, -1.92, .95), (1.0, .06, 1.9), 'Wood', .02)
    for i in range(7): g.leaf('Roof leaf', ((i - 3) * .7, 0, 7.6), 4.2, .8, 0, 'Leaf' if i % 2 else 'LeafLight')
    P('WorkPoint_0', (0, -2.7, 0))
    g.asset('CC_AirshipYard')
    disc('Launch pad', (0, 0, 0), 2.6, .25, 'Stone', 'Z', 16)
    for x in (-1.4, 1.4): g.rod('Gantry post', (x, .9, .25), (x, .9, 5.0), .08, 'Iron')
    for z in (1.5, 3.0, 4.5): g.rod('Gantry beam', (-1.4, .9, z), (1.4, .9, z), .05, 'Iron')
    g.rod('Rocket hull', (0, 0, .5), (0, 0, 4.0), .55, 'Cream', 12)
    bpy.ops.mesh.primitive_cone_add(vertices=12, radius1=.55, radius2=0, depth=1.0, location=(0, 0, 4.5)); g.finish(bpy.context.object, 'Rocket nose', 'Amber')
    for i in range(3):
        t = i * TAU / 3; turn(g.box('Fin', (.6 * math.cos(t), .6 * math.sin(t), .9), (.5, .08, .8), 'Teal', .02), 0, 0, t)
    P('WorkPoint_0', (0, -1.4, .25))  # on the pad, next to the hull


# ---------------- 기반 시설(2026-10-19: BuildingKind 중 모델 없던 5종) ----------------
def structures():
    g.asset('CC_Stockpile')  # 비축더미(QueenChamber): 시작 자원 반납 지점
    g.rock('Dirt mound', (0, 0, .05), (1.7, 1.5, .25), 'Earth', 2)
    for x, y, r in ((-.75, -.2, 0), (.55, .45, .4), (.2, -.65, -.3)):
        turn(g.box('Crate', (x, y, .55), (.8, .8, .7), 'CutWood', .04), 0, 0, r)
        turn(g.box('Crate band', (x, y, .55), (.84, .84, .1), 'Bark', .01), 0, 0, r)
    for x, y in ((.85, -.5), (1.1, -.1)): g.rock('Grain sack', (x, y, .45), (.32, .28, .4), 'Cream', 2)
    for i in range(3): g.rod('Log', (-1.3, .5 + i * .28, .3 + (i % 2) * .22), (-.3, .6 + i * .28, .3 + (i % 2) * .22), .14, 'Bark', 8)
    for x, y in ((.1, .9), (-.25, 1.05), (.4, 1.1)): g.rock('Stone pile', (x, y, .3), (.22, .2, .16), 'Stone', 1)
    g.rod('Marker pole', (1.35, .9, 0), (1.35, .9, 2.6), .05, 'Bark')
    g.box('Marker cloth', (1.65, .9, 2.35), (.6, .03, .4), 'Amber', .01)
    P('InputPoint', (0, -.9, .2)); P('WorkPoint_0', (0, -1.9, 0))

    g.asset('CC_ResearchLab')  # 무기 연구소(근접·원거리·방패·지휘·비행 공용)
    table(2.4, 1.2, .82, 'CutWood', 'Bark')
    g.box('Vise', (-.55, -.15, .95), (.3, .25, .22), 'Iron', .02)
    g.rod('Blade on vise', (-.55, -.15, 1.08), (.35, -.25, 1.08), .035, 'Iron', 6)
    g.box('Blueprint', (.6, -.1, .83), (.7, .5, .02), 'Teal', .005)
    for x in (-1.45, 1.45): g.rod('Rack post', (x, .9, 0), (x, .9, 2.2), .07, 'Bark')
    g.rod('Rack bar', (-1.45, .9, 1.9), (1.45, .9, 1.9), .05, 'Bark')
    g.rod('Test spear', (-1.0, .82, .1), (-1.0, .82, 2.1), .03, 'CutWood', 6)
    disc('Test shield', (-.3, .82, 1.15), .35, .07, 'Wood', 'Y', 12)
    g.rod('Signal staff', (.4, .82, .1), (.4, .82, 1.95), .03, 'Bark', 6); g.rock('Signal gem', (.4, .82, 2.0), (.1, .1, .12), 'Amber', 1)
    g.box('Glider wing', (1.05, .8, 1.3), (.6, .04, .9), 'Cream', .01)
    P('HandTarget', (-.55, -.15, 1.08)); P('WorkPoint_0', (-.4, -1.25, 0))

    g.asset('CC_DefenseLab')  # 방어시설 연구소: 분사기·함정 시험대
    table(2.4, 1.2, .82, 'Wood', 'Bark')
    g.rod('Test tank', (-.6, 0, .82), (-.6, 0, 1.45), .26, 'Leaf', 10); g.rod('Tank band', (-.6, 0, 1.1), (-.6, 0, 1.18), .28, 'Iron', 10)
    g.rod('Test nozzle', (-.6, 0, 1.3), (-.6, -.55, 1.3), .07, 'Iron', 8)
    g.box('Spike sample', (.45, -.1, .87), (.6, .6, .08), 'Earth', .02)
    for x in (.3, .6):
        bpy.ops.mesh.primitive_cone_add(vertices=5, radius1=.06, radius2=0, depth=.3, location=(x, -.1, 1.05)); g.finish(bpy.context.object, 'Spike', 'CutWood')
    g.rock('Resin sample', (.5, .3, .88), (.18, .14, .06), 'Amber', 1)
    g.box('Target board', (0, 1.0, 1.4), (2.0, .1, 1.6), 'CutWood', .02)
    for r, m in ((.55, 'Cream'), (.35, 'Amber'), (.15, 'Dark')): disc('Target ring', (0, .94, 1.4), r, .03, m, 'Y', 16)
    P('HandTarget', (-.6, -.3, 1.3)); P('WorkPoint_0', (-.4, -1.25, 0))

    g.asset('CC_RestRoom')  # 휴게실(옛 시설, 오락 가구로 대체 예정)
    g.box('Rest rug', (0, 0, .03), (3.0, 2.6, .06), 'Teal', .02)
    g.box('Low bench', (0, .7, .25), (2.2, .7, .5), 'CutWood', .05); g.box('Bench back', (0, 1.05, .7), (2.2, .12, .5), 'Wood', .03)
    for x in (-.6, .6): g.rock('Cushion', (x, .65, .58), (.4, .3, .1), 'Cream', 2)
    g.rod('Tea table', (0, -.4, 0), (0, -.4, .4), .45, 'Wood', 10)
    g.rod('Teapot', (-.1, -.4, .4), (-.1, -.4, .62), .14, 'Amber', 8); g.rod('Spout', (.02, -.4, .5), (.2, -.4, .62), .03, 'Amber', 6)
    g.bowl('Cup', (.2, -.25, .4), .08, .08, 'Cream')
    g.rod('Pot', (1.25, -.95, 0), (1.25, -.95, .4), .22, 'Earth', 8)
    for a in range(4): g.leaf('Plant leaf', (1.25, -.95, .65), .6, .25, a * TAU / 4, 'LeafLight')
    P('SeatPoint', (0, .6, .5)); P('WorkPoint_0', (0, -.1, 0))

    g.asset('CC_AreaAcidTower')  # 범위형 산성액 분사탑: 낮고 넓은 통 + 세 갈래 분사구
    for i in range(8):
        a = i * TAU / 8; g.rock('Foundation', (.9 * math.cos(a), .9 * math.sin(a), .2), (.38, .32, .25), 'Stone')
    g.rod('Wide reservoir', (0, 0, .2), (0, 0, 1.15), .68, 'Leaf', 12)
    for z in (.4, .95): g.rod('Band', (0, 0, z), (0, 0, z + .12), .71, 'Iron', 12)
    with pivot('Turret_Pivot', (0, 0, 1.3)):
        disc('Turret cap', (0, 0, 1.15), .45, .25, 'Iron', 'Z', 10)
        for i in range(3):
            a = i * TAU / 3 - TAU / 4
            g.rod('Spray arm', (0, 0, 1.3), (.6 * math.cos(a), .6 * math.sin(a), 1.4), .09, 'Iron', 8)
            g.rock('Spray head', (.65 * math.cos(a), .65 * math.sin(a), 1.42), (.13, .13, .1), 'Amber', 1)
    P('WorkPoint_0', (0, -1.7, 0))


def build():
    for f in (tile, living, utility, production, recreation, decoration, military, structures): f()
