"""Production kit shared helpers (BLENDER-1019).

Kit space = kemono rig space: 1 unit = 1 m, character ~1.2 m (hair top 1.17, palm socket z .41).
Front = -Y, up = +Z, 24 fps. Floor items: origin at bottom centre. Hand-held items: origin at the
grip centre, +Y toward the working end (head/blade), striking side -Z (same as resident-kit tools).
Geometry is built with bmesh into one mesh per asset (one material slot per colour) — no bpy.ops.
"""
import bpy, bmesh, math, json
from pathlib import Path
from mathutils import Vector, Matrix

KIT = Path('E:/Git/ant/art/production-kit-20261019')
TAU = math.tau
_MATS = {}


def hexrgb(h):
    h = h.lstrip('#'); c = [int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)]
    return tuple((v / 12.92) if v <= .04045 else ((v + .055) / 1.055) ** 2.4 for v in c)  # sRGB → linear


def mat(color, metal=0.0, rough=.75, emit=0.0, alpha=1.0, name=None):
    """color: '#rrggbb'. Materials are shared by (colour, finish) so 600 assets stay ~200 materials."""
    key = name or f'PK_{color.lstrip("#")}_{int(metal * 10)}{int(emit * 10)}{int(alpha * 10)}'
    m = bpy.data.materials.get(key)
    if m: return m
    m = bpy.data.materials.new(key); m.use_nodes = True
    rgb = hexrgb(color)
    p = next(n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value = (*rgb, 1); p.inputs['Roughness'].default_value = rough
    p.inputs['Metallic'].default_value = metal
    if emit:
        p.inputs['Emission Color'].default_value = (*rgb, 1); p.inputs['Emission Strength'].default_value = emit
    if alpha < 1:
        p.inputs['Alpha'].default_value = alpha
        try: m.surface_render_method = 'BLENDED'
        except Exception: pass
    m.diffuse_color = (*rgb, alpha)
    return m


def _basis(a, b):
    """Matrix taking +Z (length 1) onto segment a→b."""
    a, b = Vector(a), Vector(b); d = b - a; L = d.length or 1e-6
    q = Vector((0, 0, 1)).rotation_difference(d / L)
    return Matrix.Translation(a) @ q.to_matrix().to_4x4(), L


class Mesh:
    """Accumulates primitives into one bmesh with per-face material indices."""

    def __init__(self, name):
        self.name, self.bm, self.mats = name, bmesh.new(), []

    def _mi(self, m):
        if isinstance(m, str): m = mat(m)
        if m not in self.mats: self.mats.append(m)
        return self.mats.index(m)

    def _tag(self, before, m, smooth=False):
        mi = self._mi(m)
        for f in self.bm.faces:
            if f.index == -1 or f not in before:
                f.material_index = mi; f.smooth = smooth
        self.bm.faces.index_update()

    def _add(self, fn, m, smooth=False):
        before = set(self.bm.faces); fn(); self._tag(before, m, smooth); return self

    # --- primitives -------------------------------------------------------------------------
    def box(self, c, size, m, rot=(0, 0, 0)):
        M = Matrix.Translation(c) @ Matrix.LocRotScale(None, _euler(rot), None) @ Matrix.Diagonal((*size, 1))
        return self._add(lambda: bmesh.ops.create_cube(self.bm, size=1, matrix=M), m)

    def cyl(self, a, b, r, m, seg=8, r2=None, cap=True, smooth=False):
        B, L = _basis(a, b)
        M = B @ Matrix.Translation((0, 0, L / 2))
        r2 = r if r2 is None else r2
        return self._add(lambda: bmesh.ops.create_cone(self.bm, cap_ends=cap, cap_tris=False, segments=seg,
                                                       radius1=r, radius2=r2, depth=L, matrix=M), m, smooth)

    def ball(self, c, radii, m, seg=10, rings=6, rot=(0, 0, 0), smooth=True):
        M = Matrix.Translation(c) @ Matrix.LocRotScale(None, _euler(rot), None) @ Matrix.Diagonal((*radii, 1))
        return self._add(lambda: bmesh.ops.create_uvsphere(self.bm, u_segments=seg, v_segments=rings, radius=1, matrix=M), m, smooth)

    def ico(self, c, radii, m, sub=1, rot=(0, 0, 0), smooth=False):
        M = Matrix.Translation(c) @ Matrix.LocRotScale(None, _euler(rot), None) @ Matrix.Diagonal((*radii, 1))
        return self._add(lambda: bmesh.ops.create_icosphere(self.bm, subdivisions=sub, radius=1, matrix=M), m, smooth)

    def lathe(self, prof, m, c=(0, 0, 0), seg=12, rot=(0, 0, 0), sx=1.0, sy=1.0, smooth=True, close=True):
        """prof: [(radius, z), ...] bottom→top, revolved around local Z. Radius 0 ends are poles."""
        M = Matrix.Translation(c) @ Matrix.LocRotScale(None, _euler(rot), None)

        def build():
            rings = []
            for r, z in prof:
                if r <= 1e-5: rings.append([self.bm.verts.new(M @ Vector((0, 0, z)))]); continue
                rings.append([self.bm.verts.new(M @ Vector((r * sx * math.cos(TAU * i / seg), r * sy * math.sin(TAU * i / seg), z)))
                              for i in range(seg)])
            for lo, hi in zip(rings, rings[1:]):
                if len(lo) == 1 and len(hi) == 1: continue
                if len(lo) == 1:
                    for i in range(seg): self.bm.faces.new((lo[0], hi[(i + 1) % seg], hi[i]))
                elif len(hi) == 1:
                    for i in range(seg): self.bm.faces.new((lo[i], lo[(i + 1) % seg], hi[0]))
                else:
                    for i in range(seg): self.bm.faces.new((lo[i], lo[(i + 1) % seg], hi[(i + 1) % seg], hi[i]))
            if close:
                for ring in (rings[0], rings[-1]):
                    if len(ring) > 2:
                        f = self.bm.faces.new(ring if ring is rings[-1] else ring[::-1])
        return self._add(build, m, smooth)

    def prism(self, pts2d, z0, z1, m, M=None, smooth=False):
        """Extrude a 2D polygon (x, y) from z0 to z1, then transform by M."""
        M = M or Matrix()

        def build():
            lo = [self.bm.verts.new(M @ Vector((x, y, z0))) for x, y in pts2d]
            hi = [self.bm.verts.new(M @ Vector((x, y, z1))) for x, y in pts2d]
            n = len(pts2d)
            self.bm.faces.new(lo[::-1]); self.bm.faces.new(hi)
            for i in range(n): self.bm.faces.new((lo[i], lo[(i + 1) % n], hi[(i + 1) % n], hi[i]))
        return self._add(build, m, smooth)

    def torus(self, c, R, r, m, seg=12, tseg=6, rot=(0, 0, 0), arc=1.0):
        M = Matrix.Translation(c) @ Matrix.LocRotScale(None, _euler(rot), None)

        def build():
            closed = arc >= .999; n = seg if closed else seg + 1
            rings = []
            for i in range(n):
                a = TAU * arc * i / seg; ca, sa = math.cos(a), math.sin(a)
                rings.append([self.bm.verts.new(M @ Vector(((R + r * math.cos(TAU * j / tseg)) * ca, (R + r * math.cos(TAU * j / tseg)) * sa,
                                                               r * math.sin(TAU * j / tseg)))) for j in range(tseg)])
            for i in range(n if closed else n - 1):
                A, B2 = rings[i], rings[(i + 1) % n]
                for j in range(tseg): self.bm.faces.new((A[j], B2[j], B2[(j + 1) % tseg], A[(j + 1) % tseg]))
            if not closed:
                self.bm.faces.new(rings[0][::-1]); self.bm.faces.new(rings[-1])
        return self._add(build, m, True)

    def leaf(self, base, tip, width, m, normal=(0, 0, 1), thick=.004):
        """Flat diamond leaf from base to tip."""
        a, b = Vector(base), Vector(tip); d = b - a; n = Vector(normal).normalized()
        side = d.cross(n).normalized() * width / 2

        def build():
            p = [a, a + d * .35 + side, b, a + d * .35 - side]
            lo = [self.bm.verts.new(v - n * thick) for v in p]; hi = [self.bm.verts.new(v + n * thick) for v in p]
            self.bm.faces.new(hi); self.bm.faces.new(lo[::-1])
            for i in range(4): self.bm.faces.new((lo[i], lo[(i + 1) % 4], hi[(i + 1) % 4], hi[i]))
        return self._add(build, m)

    def merge(self, other, M=None):
        """Append another Mesh (optionally transformed)."""
        tmp = bpy.data.meshes.new('tmp'); other.bm.to_mesh(tmp)
        if M: tmp.transform(M)
        remap = [self._mi(m) for m in other.mats]
        before = set(self.bm.faces); self.bm.from_mesh(tmp)
        for f in self.bm.faces:
            if f not in before: f.material_index = remap[f.material_index] if remap else 0
        bpy.data.meshes.remove(tmp)
        return self

    def bounds(self):
        vs = [v.co for v in self.bm.verts]
        return (Vector([min(v[i] for v in vs) for i in range(3)]), Vector([max(v[i] for v in vs) for i in range(3)])) if vs else (Vector(), Vector())

    def to_object(self, collection, origin='bottom'):
        """origin: 'bottom' = move so bounds bottom-centre sits at (0,0,0); 'keep' = as authored."""
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        if origin == 'bottom':
            lo, hi = self.bounds(); off = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
            bmesh.ops.translate(self.bm, verts=self.bm.verts, vec=-off)
        me = bpy.data.meshes.new(self.name); self.bm.to_mesh(me); self.bm.free()
        for m in self.mats: me.materials.append(m)
        o = bpy.data.objects.new(self.name, me); collection.objects.link(o)
        return o


def _euler(rot):
    from mathutils import Euler
    return Euler(rot).to_quaternion()


def empty(name, loc, collection, parent=None, size=.04, kind='PLAIN_AXES'):
    e = bpy.data.objects.new(name, None); collection.objects.link(e)
    e.empty_display_type = kind; e.empty_display_size = size; e.location = loc
    if parent: e.parent = parent
    return e


def new_file():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    s = bpy.context.scene; s.render.fps = 24; s.unit_settings.system = 'METRIC'
    return s


def collection(name, parent=None):
    c = bpy.data.collections.get(name) or bpy.data.collections.new(name)
    if c.name not in (parent or bpy.context.scene.collection).children:
        (parent or bpy.context.scene.collection).children.link(c)
    return c


def layout(objs, cols=12, gap=.35):
    """Spread roots on a grid so the .blend is browsable."""
    for i, o in enumerate(objs):
        o.location = ((i % cols) * gap, -(i // cols) * gap, 0)


def to_py(v):
    """IDProperty groups/arrays and mathutils values → plain JSON types."""
    if hasattr(v, 'to_dict'): return to_py(v.to_dict())
    if hasattr(v, 'to_list'): return to_py(v.to_list())
    if isinstance(v, dict): return {k: to_py(x) for k, x in v.items()}
    if isinstance(v, (list, tuple)) or type(v).__name__ in ('Vector', 'Color', 'Euler', 'IDPropertyArray'): return [to_py(x) for x in v]
    if isinstance(v, float): return round(v, 4)
    return v


def write_json(path, data):
    Path(path).write_text(json.dumps(to_py(data), ensure_ascii=False, indent=1), encoding='utf-8')
