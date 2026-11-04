"""Headless: blender -b --python scripts/render_equipment.py
One still per equipment item (10, EquipmentRecipe order) worn/held by a manual combo → previews/equipment/<id>.png.
Scenes are built in memory only; resident-preview.blend is not saved."""
import bpy, sys, math
from mathutils import Vector, Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K, kit_combos as C, kit_render as R

# The saved preview file keeps only linked data that its scenes use; link the rig, parts, palette and hand items again.
with bpy.data.libraries.load(str(K.KIT / 'resident-character-kit.blend'), link=True) as (a, b):
    b.objects = [n for n in a.objects if n == 'ResidentRig' or n.startswith('Part_')]
    b.materials = [n for n in a.materials if n.startswith(('Fur_', 'Inner_'))]
with bpy.data.libraries.load(str(K.KIT / 'resident-tools.blend'), link=True) as (a, b):
    b.objects = [n for n in a.objects if n.startswith(('Tool_', 'Prop_', 'Equip_'))]

# attach / camera / lights: same as kit_preview.py (that script builds the whole preview file on import).
def attach(obj, rig, bone, world_rest):
    """Parent to a socket bone so obj sits at world_rest (armature space) in the rest pose."""
    b = rig.data.bones[bone]
    obj.parent = rig; obj.parent_type = 'BONE'; obj.parent_bone = bone
    obj.matrix_parent_inverse = Matrix.Translation((0, -b.length, 0))
    obj.matrix_basis = b.matrix_local.inverted() @ world_rest


def camera(scene, objs, direction=(-2.3, -.9, 1.5)):
    scene.view_layers[0].update()
    pts = [o.matrix_world @ Vector(v) for o in objs if o.type == 'MESH' for v in o.bound_box]
    lo = Vector([min(p[i] for p in pts) for i in range(3)]); hi = Vector([max(p[i] for p in pts) for i in range(3)])
    cam = bpy.data.objects.new(scene.name + '_Cam', bpy.data.cameras.new(scene.name + '_Cam'))
    scene.collection.objects.link(cam); scene.camera = cam
    d = Vector(direction).normalized(); cam.location = (lo + hi) / 2 + d * 30
    cam.rotation_euler = (-d).to_track_quat('-Z', 'Y').to_euler()
    cam.data.type = 'ORTHO'; cam.data.ortho_scale = max(hi - lo) * 1.25 + .3; cam.data.clip_end = 100


def lights(scene):
    w = bpy.data.worlds.get('KitWorld') or bpy.data.worlds.new('KitWorld')
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == 'BACKGROUND')
    bg.inputs['Color'].default_value = (.93, .90, .84, 1); bg.inputs['Strength'].default_value = .55
    scene.world = w
    sun = bpy.data.objects.get('KitSun')
    if not sun:
        sun = bpy.data.objects.new('KitSun', bpy.data.lights.new('KitSun', 'SUN'))
        sun.data.energy = 2.2; sun.rotation_euler = (math.radians(50), 0, math.radians(-40))
    scene.collection.objects.link(sun)


ITEMS = [  # (EquipmentRecipe, kit object, worn part?, combo)
    ('Mandible', 'Prop_Spear', False, 'Combo_B'), ('AcidSprayer', 'Equip_AcidSprayer', False, 'Combo_C'),
    ('Shield', 'Prop_Shield', False, 'Combo_D'), ('Pheromone', 'Equip_Signaller', False, 'Combo_A'),
    ('Coating', 'Equip_Armor', True, 'Combo_B'), ('Wings', 'Equip_FlightGear', True, 'Combo_D'),
    ('Anklet', 'Trinket_Anklet', True, 'Combo_A'), ('Antenna', 'Trinket_SensorGoggles', True, 'Combo_C'),
    ('Scent', 'Trinket_ScentPouch', True, 'Combo_B'), ('Insignia', 'Trinket_Insignia', True, 'Combo_D')]
out = R.OUT / 'equipment'
for rid, obj, worn, combo in ITEMS:
    sc = bpy.data.scenes.new('EquipStill__' + rid); sc.render.fps = 24; lights(sc)
    spec = dict(C.COMBOS[combo]); spec['parts'] = spec['parts'] + ([obj] if worn else [])
    rig = C.build_combo(sc.name + '__' + combo, spec, (0, 0, 0), sc.collection)
    vis = [rig, *rig.children]
    if not worn:
        t = bpy.data.objects[obj].copy(); sc.collection.objects.link(t)
        sock = t.get('socket', 'Socket.Tool.R'); attach(t, rig, sock, rig.data.bones[sock].matrix_local); vis.append(t)
    camera(sc, vis, (-1.2, -2.0, 1.0)); sc.camera.data.ortho_scale = 3.4
    sc.render.resolution_x = sc.render.resolution_y = 360; sc.view_settings.view_transform = 'Standard'
    R.render([sc.name], frames=[1], out=out)
    (out / f'{sc.name}_f001.png').replace(out / f'{rid}.png')
print('equipment stills', len(ITEMS))
