"""미리보기 전용: ant-tools.blend의 도구를 leg.01.R 끝(본 tail)에 붙인다. 손잡이 +Z → 본 +Y(다리 연장 방향).
blender -b ant-work-clips.blend --python attach_preview.py --python render_poses.py -- out.png Clip:frame,... Tool_Name"""
import bpy, sys, math
args = sys.argv[sys.argv.index('--') + 1:]
name = args[2]; rot = [math.radians(float(v)) for v in (args[3].split("/") if len(args) > 3 else ("-90", "0", "0"))]
with bpy.data.libraries.load('E:/Git/ant/art/ant-anim/ant-tools.blend') as (src, dst):
    dst.objects = [n for n in src.objects]
for o in dst.objects: bpy.context.scene.collection.objects.link(o)
root = bpy.data.objects[name]; rig = bpy.data.objects['Rig']
for o in dst.objects:
    if o.parent is None and o is not root: o.hide_render = True
    elif o.parent is not None and o.parent is not root: o.hide_render = True
root.parent = rig; root.parent_type = 'BONE'; root.parent_bone = 'leg.01.R'
root.location = (0, .022, 0); root.rotation_euler = rot  # 본 tail보다 다리 메시가 길어 끝으로 이동
