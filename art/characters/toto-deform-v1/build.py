# 토토 데포르메 v1: 시안 A 토토(크림 토끼)를 스티커 데포르메 그림체로.
# 평면 색(그림자 없음) + 굵은 외곽선 + 몸보다 큰 머리 + 초대형 눈. Blender MCP에서 exec로 실행.
import bpy, math
from pathlib import Path
from mathutils import Vector

HERE = Path(r"E:\Git\ant\art\characters\toto-deform-v1")
scene = bpy.context.scene
for o in list(scene.objects):  # 이전 모찌/토토 데포르메만 정리
    if o.get("deform_part") or o.name.startswith(("Mochi", "TotoD")):
        bpy.data.objects.remove(o, do_unlink=True)
root = bpy.data.objects.new("TotoD", None)
scene.collection.objects.link(root)

def mat(name, rgb):
    """그림자 없는 평면 색."""
    m = bpy.data.materials.get("TotoD_" + name) or bpy.data.materials.new("TotoD_" + name)
    m.use_nodes = True
    nt = m.node_tree; nt.nodes.clear()
    e = nt.nodes.new("ShaderNodeEmission"); e.inputs["Color"].default_value = (*rgb, 1)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    nt.links.new(e.outputs[0], out.inputs["Surface"])
    return m

LINE = mat("Line", (.10, .06, .05)); LINE.use_backface_culling = True

def tag(o):
    o["deform_part"] = 1; o.parent = root
    return o

def ball(name, loc, scale, m, line=.022, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, location=loc)
    o = bpy.context.object
    o.name, o.scale, o.rotation_euler = "TotoD_" + name, scale, rot
    o.data.materials.append(m)
    bpy.ops.object.shade_smooth()
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)  # 외곽선 두께 균일
    o.modifiers.new("Sub", "SUBSURF").levels = 1
    if line:
        o.data.materials.append(LINE)
        s = o.modifiers.new("Outline", "SOLIDIFY")
        s.thickness, s.offset, s.use_flip_normals, s.material_offset = line, 1, True, 1
    return tag(o)

def stroke(name, pts, m, depth=.012):
    c = bpy.data.curves.new(name, "CURVE")
    c.dimensions, c.bevel_depth, c.bevel_resolution, c.use_fill_caps = "3D", depth, 3, True
    sp = c.splines.new("POLY"); sp.points.add(len(pts) - 1)
    for p, co in zip(sp.points, pts):
        p.co = (*co, 1)
    o = bpy.data.objects.new("TotoD_" + name, c); scene.collection.objects.link(o)
    c.materials.append(m)
    return tag(o)

cream = mat("Cream", (1.0, .95, .86))
ear_in = mat("EarPink", (1.0, .76, .74))
blush = mat("Blush", (1.0, .62, .64))
hatch = mat("BlushLine", (.92, .38, .42))
eye = mat("Eye", (.17, .09, .06))
iris = mat("Iris", (.47, .28, .16))
shine = mat("Shine", (1, 1, 1))
mouth = mat("Mouth", (.55, .16, .16))
tongue = mat("Tongue", (1.0, .52, .52))
bag = mat("Bag", (.58, .38, .22))

# 머리: 전체 높이의 2/3. 위아래로 살짝 눌린 찹쌀떡
HC, HR = Vector((0, 0, .80)), Vector((.62, .52, .50))
ball("Head", HC, HR, cream, .03)

def on_face(x, z, push=0.0):
    dx, dz = x / HR.x, (z - HC.z) / HR.z
    y = -HR.y * math.sqrt(max(0, 1 - dx * dx - dz * dz))
    n = Vector((x / HR.x**2, y / HR.y**2, (z - HC.z) / HR.z**2)).normalized()
    return Vector((x, y, z)) + n * push, n.to_track_quat("-Y", "Z").to_euler()

def decal(name, x, z, w, h, m, push, line=0):
    p, r = on_face(x, z, push)
    return ball(name, p, (w, .01, h), m, line, r)

# 눈: 얼굴의 거의 1/3, 아래쪽에 넓게. 큰 하이라이트 + 아래쪽 밝은 홍채
for sx in (-1, 1):
    s, x = "LR"[sx > 0], sx * .25
    decal("Eye" + s, x, .73, .14, .165, eye, .006)
    decal("Iris" + s, x, .675, .10, .075, iris, .014)
    decal("ShineBig" + s, x - .04, .795, .06, .065, shine, .022)
    decal("ShineSmall" + s, x + .055, .665, .024, .026, shine, .022)
    decal("Blush" + s, sx * .43, .58, .09, .05, blush, .008)
    for i in range(3):  # 볼 빗금 ///
        bx = sx * .43 + (i - 1) * .035
        stroke(f"Hatch{s}{i}", [on_face(bx - .012, .555, .02)[0], on_face(bx + .012, .605, .02)[0]], hatch, .006)

# 입: 활짝 벌린 'D' 입 + 혀
decal("Mouth", 0, .575, .055, .045, mouth, .01)
decal("Tongue", 0, .552, .034, .02, tongue, .018)

# 귀: 토토의 긴 토끼 귀, 바깥으로 살짝 벌어짐
for sx in (-1, 1):
    s = "LR"[sx > 0]
    ball("Ear" + s, (sx * .22, .04, 1.42), (.11, .07, .34), cream, .025, (0, sx * .28, 0))
    ball("EarIn" + s, (sx * .215, -.012, 1.42), (.06, .02, .25), ear_in, 0, (0, sx * .28, 0))

# 몸: 머리 밑 작은 콩. 팔은 만세(스티커 포즈), 발은 뭉툭
ball("Body", (0, .02, .22), (.26, .22, .22), cream)
for sx in (-1, 1):
    s = "LR"[sx > 0]
    ball("Arm" + s, (sx * .34, -.08, .32), (.09, .08, .13), cream, rot=(0, sx * -.9, 0))
    ball("Foot" + s, (sx * .13, -.06, .05), (.09, .11, .06), cream)
ball("Tail", (0, .26, .18), (.08, .08, .08), cream)
ball("Backpack", (0, .30, .40), (.20, .10, .20), bag)

# 촬영: 투명 배경 정면 스티커 컷
cd = bpy.data.cameras.new("TotoD_Cam"); cam = tag(bpy.data.objects.new("TotoD_Cam", cd))
scene.collection.objects.link(cam); scene.camera = cam
cam.location = (.9, -6, 1.2)
cam.rotation_euler = (Vector((0, 0, .85)) - cam.location).to_track_quat("-Z", "Y").to_euler()
cd.type, cd.ortho_scale = "ORTHO", 2.4
r = scene.render
r.resolution_x = r.resolution_y = 1000
r.film_transparent = True
r.image_settings.file_format, r.image_settings.color_mode = "PNG", "RGBA"
r.filepath = str(HERE / "preview.png")
scene.view_settings.view_transform = "Standard"
print("built", len(root.children), "parts")
