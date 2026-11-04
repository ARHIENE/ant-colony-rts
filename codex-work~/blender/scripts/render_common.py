"""Shared review-render setup: EEVEE, soft sun + sky fill, neutral transparent background, 3/4 camera fit to bounds."""
import bpy, math
from mathutils import Vector

VIEW = Vector((.55, -1.0, .62)).normalized()  # 3/4 front view used by every item PNG


def setup(res=256, transparent=True, res_y=None):
    s = bpy.context.scene
    try: s.render.engine = 'BLENDER_EEVEE'
    except TypeError: pass
    s.render.resolution_x = res; s.render.resolution_y = res_y or res; s.render.resolution_percentage = 100
    s.render.film_transparent = transparent; s.render.image_settings.file_format = 'PNG'; s.render.image_settings.color_mode = 'RGBA'
    s.view_settings.view_transform = 'Standard'
    w = s.world or bpy.data.worlds.new('W'); s.world = w; w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND'); bg.inputs[0].default_value = (.62, .64, .68, 1); bg.inputs[1].default_value = .9
    sun = bpy.data.objects.get('ReviewSun')
    if not sun:
        sun = bpy.data.objects.new('ReviewSun', bpy.data.lights.new('ReviewSun', 'SUN')); s.collection.objects.link(sun)
    sun.data.energy = 3.2; sun.data.angle = .3; sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(35))
    cam = bpy.data.objects.get('ReviewCam')
    if not cam:
        cam = bpy.data.objects.new('ReviewCam', bpy.data.cameras.new('ReviewCam')); s.collection.objects.link(cam)
    cam.data.lens = 50; cam.data.clip_start = .01; cam.data.clip_end = 200; s.camera = cam
    return cam


def bounds(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == 'MESH' for c in o.bound_box]
    if not pts: return Vector(), Vector((.1, .1, .1))
    return Vector([min(p[i] for p in pts) for i in range(3)]), Vector([max(p[i] for p in pts) for i in range(3)])


def fit(cam, objs, view=VIEW, margin=1.15, wide=1.0):
    lo, hi = bounds(objs); c = (lo + hi) / 2; r = max((hi - lo).length / 2, .03) * margin / wide  # wide = image aspect for strips
    d = r / math.tan(cam.data.angle / 2)
    cam.location = c + view * d; cam.rotation_euler = (-view).to_track_quat('-Z', 'Y').to_euler()


def only(objs):
    """Render only these objects (and their children)."""
    keep = set()
    for o in objs: keep.add(o); keep.update(o.children_recursive)
    for o in bpy.data.objects:
        if o.type in ('MESH', 'CURVE', 'FONT'): o.hide_render = o not in keep


def shot(path):
    bpy.context.scene.render.filepath = str(path); bpy.ops.render.render(write_still=True)
