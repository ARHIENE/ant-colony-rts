# 모찌 v1: 2.2등신 치비 사람형 캐릭터. Blender MCP에서 exec로 실행.
# 열려 있는 장면에 만든다. 다시 실행하면 이전 모찌만 지우고 새로 만든다.
import bpy, math
from pathlib import Path
from mathutils import Vector

HERE = Path(r"E:\Git\ant\art\characters\mochi-v1")

scene = bpy.context.scene
for o in list(scene.objects):
    if o.name.startswith(("Mochi", "Hair_", "Eye_", "Iris_", "Shine", "Blush_", "Mouth", "Head", "Body",
                          "Skirt", "Cape", "Arm_", "Hand_", "Leg_", "Boot_")):
        bpy.data.objects.remove(o, do_unlink=True)
root = bpy.data.objects.new("Mochi", None)
scene.collection.objects.link(root)

def mat(name, rgb, rough=.85, emit=.35):
    m = bpy.data.materials.get("Mochi_" + name) or bpy.data.materials.new("Mochi_" + name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    if emit:  # 살짝 자체 발광 → 그림자가 탁해지지 않는 툰 느낌
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = emit
    return m

# 그림체 외곽선: 뒤집힌 껍데기(inverted hull)
LINE = mat("Line", (.16, .09, .08), 1, 0)
LINE.use_backface_culling = True

def ball(name, loc, scale, m, line=.012, smooth=2, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=24, ring_count=12, location=loc)
    o = bpy.context.object
    o.name, o.scale, o.rotation_euler = name, scale, rot
    o.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    if smooth:
        o.modifiers.new("Sub", "SUBSURF").levels = smooth
    if line:
        o.data.materials.append(LINE)
        s = o.modifiers.new("Outline", "SOLIDIFY")
        s.thickness, s.offset, s.use_flip_normals, s.material_offset = line / max(scale), 1, True, 1
    o.parent = root
    return o

skin = mat("Skin", (1.0, .80, .68))
blush = mat("Blush", (1.0, .55, .58), 1, .3)
hair = mat("Hair", (.24, .36, .40))
dress = mat("Dress", (.95, .90, .80))
cape = mat("Cape", (.62, .22, .16))
boot = mat("Boot", (.36, .22, .14))
gold = mat("Gold", (.78, .58, .26), .5)
eye = mat("Eye", (.10, .08, .10), .4)
iris = mat("Iris", (.22, .48, .52), .4, .4)
white = mat("Shine", (1, 1, 1), .2, 3)
mouth = mat("Mouth", (.75, .30, .32), .7)

# 머리: 몸과 거의 같은 크기(2.2등신), 옆으로 살짝 넓은 찹쌀떡
HC, HR = Vector((0, 0, 1.08)), Vector((.56, .50, .48))
ball("Head", HC, HR, skin)

def on_face(x, z, push=0.0):
    """머리 앞면 (x, z) 위치의 표면 좌표와 법선 회전."""
    dx, dz = x / HR.x, (z - HC.z) / HR.z
    y = -HR.y * math.sqrt(max(0, 1 - dx * dx - dz * dz))
    n = Vector((x / HR.x**2, y / HR.y**2, (z - HC.z) / HR.z**2)).normalized()
    p = Vector((x, y, z)) + n * push
    return p, n.to_track_quat("-Y", "Z").to_euler()

def face(name, x, z, w, h, m, push=.0, line=0):
    p, r = on_face(x, z, push)
    return ball(name, p, (w, .012, h), m, line, 1, r)

# 눈: 얼굴 아래쪽·넓게, 크고 동그랗게 → 귀여움 핵심
for side, sx in (("L", -1), ("R", 1)):
    x = sx * .22
    face("Eye_" + side, sx * .21, .97, .11, .13, eye, .004)
    face("Iris_" + side, sx * .21, .95, .085, .095, iris, .012)
    face("ShineBig_" + side, sx * .21 - .035, 1.01, .042, .046, white, .02)
    face("ShineSmall_" + side, sx * .21 + .04, .915, .018, .02, white, .02)
    face("Blush_" + side, sx * .37, .875, .085, .045, blush, .006)
# 입: 그림체처럼 가는 선의 'ω' 미소
mc = bpy.data.curves.new("Mouth", "CURVE")
mc.dimensions, mc.bevel_depth, mc.bevel_resolution = "3D", .007, 3
sp = mc.splines.new("POLY"); pts = [(-.05, .86), (-.025, .835), (0, .852), (.025, .835), (.05, .86)]
sp.points.add(len(pts) - 1)
for pt, (x, z) in zip(sp.points, pts):
    pt.co = (*on_face(x, z, .008)[0], 1)
mo = bpy.data.objects.new("Mouth", mc); scene.collection.objects.link(mo)
mc.materials.append(LINE); mo.parent = root

# 머리카락: 뾰족한 면 대신 말랑한 덩어리
ball("Hair_Cap", (0, .07, 1.16), (.60, .54, .47), hair)
for i, (x, z, sx, sz, ry) in enumerate([(-.33, 1.17, .12, .19, .55), (-.12, 1.21, .15, .16, .3),
                                         (.08, 1.22, .14, .15, -.2), (.29, 1.18, .13, .18, -.5)]):
    p, r = on_face(x, z, .015)
    ball(f"Hair_Bang{i}", p, (sx, .06, sz), hair, rot=(r.x, ry, r.z))
for sx in (-1, 1):
    ball("Hair_Side" + ("L" if sx < 0 else "R"), (sx * .46, -.10, .88), (.09, .11, .25), hair,
         rot=(0, sx * .12, 0))
# 더듬이 대신 바보털 한 가닥(개미 느낌 살짝 남김)
cv = bpy.data.curves.new("Ahoge", "CURVE")
cv.dimensions, cv.bevel_depth, cv.bevel_resolution = "3D", .025, 4
sp = cv.splines.new("BEZIER"); sp.bezier_points.add(2)
for bp, co in zip(sp.bezier_points, [(0, 0, 1.6), (.06, -.02, 1.76), (.16, -.02, 1.72)]):
    bp.co, bp.handle_left_type, bp.handle_right_type = co, "AUTO", "AUTO"
a = bpy.data.objects.new("Hair_Ahoge", cv); scene.collection.objects.link(a)
cv.materials.append(hair); a.parent = root

# 몸: 작은 원피스 + 망토 + 짧은 팔다리
ball("Body", (0, 0, .42), (.25, .22, .24), dress)
ball("Skirt", (0, 0, .30), (.32, .28, .17), dress)
ball("Cape", (0, .02, .58), (.33, .29, .12), cape)
ball("CapeBrooch", (0, -.27, .58), (.04, .02, .04), gold, .006)
for sx in (-1, 1):
    s = "L" if sx < 0 else "R"
    ball("Arm_" + s, (sx * .25, -.02, .45), (.065, .065, .12), dress, rot=(0, sx * .45, 0))
    ball("Hand_" + s, (sx * .31, -.04, .34), (.055, .055, .055), skin)
    ball("Leg_" + s, (sx * .10, 0, .14), (.06, .06, .08), skin)
    ball("Boot_" + s, (sx * .11, -.03, .06), (.085, .11, .065), boot)

# 촬영용 카메라(조명·바닥은 열린 장면 것 사용)
cd = bpy.data.cameras.new("Mochi_Cam"); cam = bpy.data.objects.new("Mochi_Cam", cd)
scene.collection.objects.link(cam); scene.camera = cam
cam.location = (1.2, -6, 1.5)
cam.rotation_euler = (Vector((0, 0, .85)) - cam.location).to_track_quat("-Z", "Y").to_euler()
cd.type, cd.ortho_scale = "ORTHO", 2.1
r = scene.render
r.resolution_x = r.resolution_y = 1000
scene.view_settings.view_transform = "Standard"  # 파스텔이 탁해지지 않게
r.image_settings.file_format = "PNG"
r.filepath = str(HERE / "preview.png")
print("built", len([o for o in scene.objects if o.parent == root]), "parts")
