"""New industry / food / fluid facilities, pipe modules, deposits and overlay markers (BLENDER-1019 §6).

Kit metres, 1 grid cell = 1 m (character 1.2 m stands in one cell). Front = -Y (the side the worker stands on),
origin = footprint centre on the floor. Each facility = root Empty 'FAC_<id>' with:
  Body (static mesh), moving parts as separate children (pivot at the joint) with looping '<id>|Run' actions,
  point Empties (names below, +Y of the Empty = facing), and 'Select' (cube Empty = engine click volume).
Points: WorkPoint_0 (stand, faces +Y into the machine), HandTarget (hand contact), InputPoint (item goes in),
  OutputPoint (finished solid placed on the floor beside it), FluidIn_L/G / FluidOut_L/G (pipe ports, L=liquid G=gas),
  PowerIn / PowerOut, SeatPoint, ControlPoint (panel/lever), ChargePoint (drum/canister on the floor), CarryPoint.
States for tanks are separate children Level_Empty/Level_Partial/Level_Full (engine shows one); machines are 'stopped'
in rest pose and 'running' while <id>|Run plays. No fluid simulation, no explosion/pollution (not decided in the spec).
"""
import math
from mathutils import Vector, Matrix
import bpy
import pk

TAU = math.tau
WOOD, WOOD_D, IRON, STEEL, COPPER, BRICK, STONE, SOOT = '#9a6a3c', '#6a4628', '#4a4e56', '#9aa2ac', '#c07a42', '#a8553a', '#8a8580', '#2a2626'
GLASS = pk.mat('#cfe8f0', alpha=.35, rough=.05, name='PK_FacGlass')
FIRE = pk.mat('#ff8a2a', emit=3.0, name='PK_FacFire')
LAMP_ON = pk.mat('#6aff7a', emit=2.5, name='PK_FacLampOn')
LIQ_PIPE, LIQ_BAND = '#2f5f8a', '#d8d8d0'   # liquid pipes: thick blue with white flange bands
GAS_PIPE, GAS_BAND = '#e0b830', '#c8402a'   # gas pipes: thinner yellow with red bands
OIL, FUEL, GAS_C = '#1a1612', '#d8a030', '#e8e070'
BENCH_Z = .40  # work top height that the kemono BENCH pose reaches


class Fac:
    def __init__(self, fid, ko, cells, col, group, note=''):
        self.id, self.col = fid, col
        self.root = bpy.data.objects.new('FAC_' + fid, None); col.objects.link(self.root)
        self.root.empty_display_type = 'ARROWS'; self.root.empty_display_size = .2
        r = self.root; r['id'], r['ko'], r['cells'], r['group'], r['note'] = fid, ko, list(cells), group, note
        r['units'] = 'kit m, 1 cell = 1 m, origin = footprint centre on floor, front -Y'
        self.body = pk.Mesh(fid + '_Body'); self.parts = []; self.points = {}

    def part(self, name, mesh, pivot):
        """mesh authored in facility space; becomes a child whose origin is the pivot."""
        lo = Vector(pivot)
        for v in mesh.bm.verts: v.co -= lo
        o = mesh.to_object(self.col, origin='keep'); o.name = f'{self.id}_{name}'; o.parent = self.root; o.location = lo
        self.parts.append(o); return o

    def point(self, name, pos, face=(0, 1, 0), size=.06):
        e = pk.empty(f'{self.id}.{name}', pos, self.col, parent=self.root, size=size, kind='ARROWS')
        d = Vector(face).normalized(); e.rotation_euler = Vector((0, 1, 0)).rotation_difference(d).to_euler()  # Empty +Y = facing
        self.points[name] = {'pos': [round(x, 3) for x in pos], 'face': [round(x, 3) for x in d]}
        return e

    def finish(self, select=None):
        b = self.body.to_object(self.col, origin='keep'); b.parent = self.root
        mn = Vector((9, 9, 9)); mx = -mn
        for o in [b, *self.parts]:
            for c in o.bound_box:
                w = o.matrix_parent_inverse @ o.matrix_basis @ Vector(c) if o != b else Vector(c)
                mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
        if select is None: select = (mn, mx)
        s = pk.empty(f'{self.id}.Select', (Vector(select[0]) + Vector(select[1])) / 2, self.col, parent=self.root, size=.5, kind='CUBE')
        s.scale = (Vector(select[1]) - Vector(select[0])) / 2 + Vector((.03, .03, .03))
        self.root['bounds'] = [[round(x, 3) for x in mn], [round(x, 3) for x in mx]]
        self.root['select_box'] = [[round(x, 3) for x in select[0]], [round(x, 3) for x in select[1]]]
        self.root['points'] = self.points
        return self.root


def spin(obj, axis, frames=48, turns=1, name=None):
    act = bpy.data.actions.new(name or f'{obj.name}|Run'); obj.animation_data_create(); obj.animation_data.action = act
    obj.rotation_mode = 'XYZ'; i = 'XYZ'.index(axis)
    for f, a in ((1, 0), (frames + 1, TAU * turns)):
        e = [0, 0, 0]; e[i] = a; obj.rotation_euler = e; obj.keyframe_insert('rotation_euler', frame=f)
    _linear_cycle(act); act.use_fake_user = True; act['part'] = obj.name; obj.rotation_euler = (0, 0, 0); return act


def rock(obj, axis, amp, frames=48, name=None, loc=None):
    """Back-and-forth rotation (or translation if loc=vector) as a seamless loop."""
    act = bpy.data.actions.new(name or f'{obj.name}|Run'); obj.animation_data_create(); obj.animation_data.action = act
    obj.rotation_mode = 'XYZ'; i = 'XYZ'.index(axis)
    for f in range(1, frames + 2, max(1, frames // 8)):
        t = math.sin(TAU * (f - 1) / frames)
        if loc is not None: obj.location = Vector(obj['rest']) + Vector(loc) * t; obj.keyframe_insert('location', frame=f)
        else:
            e = [0, 0, 0]; e[i] = amp * t; obj.rotation_euler = e; obj.keyframe_insert('rotation_euler', frame=f)
    _cycle(act); act.use_fake_user = True; act['part'] = obj.name
    obj.rotation_euler = (0, 0, 0)
    if loc is not None: obj.location = Vector(obj['rest'])
    return act


def swing(obj, axis, angle, name):
    """Open → hold → close (48 f), like the resident-kit doors."""
    act = bpy.data.actions.new(name); obj.animation_data_create(); obj.animation_data.action = act
    obj.rotation_mode = 'XYZ'; i = 'XYZ'.index(axis)
    for f, a in ((1, 0), (12, angle), (36, angle), (48, 0)):
        e = [0, 0, 0]; e[i] = a; obj.rotation_euler = e; obj.keyframe_insert('rotation_euler', frame=f)
    act.use_fake_user = True; act['part'] = obj.name; obj.rotation_euler = (0, 0, 0); obj.animation_data.action = None; return act


def _fcurves(act):
    for l in act.layers:
        for st in l.strips:
            for cb in st.channelbags: yield from cb.fcurves


def _linear_cycle(act):
    for fc in _fcurves(act):
        for k in fc.keyframe_points: k.interpolation = 'LINEAR'
        fc.modifiers.new('CYCLES')


def _cycle(act):
    for fc in _fcurves(act): fc.modifiers.new('CYCLES')


# ------------------------------------------------------------------ shared bits
def bench(m, w, d, h=BENCH_Z, top=WOOD, leg=WOOD_D):
    m.box((0, 0, h - .02), (w, d, .04), top)
    for sx in (-1, 1):
        for sy in (-1, 1): m.box((sx * (w / 2 - .03), sy * (d / 2 - .03), (h - .04) / 2), (.04, .04, h - .04), leg)


def gauge(m, c, r=.05, face=(0, -1, 0)):
    d = Vector(face); m.cyl(c, Vector(c) + d * .015, r, '#e8e8e0', seg=12); m.cyl(Vector(c) - d * .005, c, r * 1.12, IRON, seg=12)


def wheel(m, c, r=.06, axis=(0, -1, 0), color=GAS_BAND):
    a = Vector(axis); rot = Vector((0, 0, 1)).rotation_difference(a).to_euler()
    m.torus(c, r, .008, color, seg=12, tseg=4, rot=rot)
    for k in range(3):
        q = Matrix.Rotation(TAU * k / 3, 3, a); m.cyl(c, Vector(c) + q @ (a.orthogonal().normalized() * r), .005, color, seg=4)


def chimney(m, base, h, r=.07, color=BRICK):
    m.cyl(base, Vector(base) + Vector((0, 0, h)), r, color, seg=8); m.cyl(Vector(base) + Vector((0, 0, h)), Vector(base) + Vector((0, 0, h + .03)), r * 1.2, SOOT, seg=8)


def lamp(m, c, on=True): m.ball(c, (.015,) * 3, LAMP_ON if on else '#3a4a3a', seg=6, rings=4)


def panel(m, c, w=.18, h=.24):
    """Control panel on a post: buttons + lever. Hand contact is the panel face."""
    m.cyl((c[0], c[1], 0), (c[0], c[1], c[2] - h / 2), .02, IRON, seg=6)
    m.box(c, (w, .05, h), '#5a6068', rot=(-.35, 0, 0))
    for k, col in enumerate(('#e04a3a', '#3ac04a', '#e0c03a')): m.ball((c[0] - w * .3 + k * w * .3, c[1] - .03, c[2] + .03), (.012,) * 3, col, seg=6, rings=4)


def port(f, m, name, pos, face, kind):
    """Fluid/power port: a flange stub on the body + a point Empty for the pipe/wire snap."""
    p, d = Vector(pos), Vector(face)
    if kind in ('L', 'G'):
        col, r = (LIQ_PIPE, .055) if kind == 'L' else (GAS_PIPE, .04)
        m.cyl(p - d * .06, p, r, col, seg=10); m.cyl(p - d * .012, p, r * 1.35, LIQ_BAND if kind == 'L' else GAS_BAND, seg=10)
    else:
        m.box(p - d * .02, (.06, .06, .06), '#3a3a40'); m.cyl(p - d * .02, p + d * .01, .015, '#e0c03a', seg=6)
    f.point(name, pos, face)
