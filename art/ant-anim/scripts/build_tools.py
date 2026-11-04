"""개미 손(leg.01) 부착용 도구 소품 → ant-tools.blend / exports/Ant_Tools.fbx.
colony-core/scripts/geometry.py 재사용. 각 도구 원점 = 손잡이 끝(쥐는 곳), 손잡이 +Z 방향.
QuirkyAnt 리그 스케일(몸길이 약 .17)에 맞춰 길이 약 .1로 축소한다. 실행: blender -b --python build_tools.py"""
import bpy, sys
from pathlib import Path
sys.path.insert(0, 'E:/Git/ant/art/colony-core/scripts')
import geometry as g  # noqa: E402

BASE = Path('E:/Git/ant/art/ant-anim')
SCALE = .1  # 손잡이 1 → 개미 리그 단위(앞다리 길이와 비슷)

bpy.ops.wm.read_factory_settings(use_empty=True)
g.materialize()

def tool(name, build):
    root = g.asset(name); build(); root.scale = (SCALE,) * 3
    return root

TOOLS = {
    'Tool_Hammer': lambda: (g.rod('Handle', (0, 0, 0), (0, 0, 1), .05, 'Wood'), g.box('Head', (0, 0, 1), (.42, .2, .2), 'Iron')),
    'Tool_Axe': lambda: (g.rod('Handle', (0, 0, 0), (0, 0, 1.05), .05, 'Wood'), g.box('Blade', (0, -.17, .9), (.06, .32, .3), 'Iron')),
    'Tool_Pickaxe': lambda: (g.rod('Handle', (0, 0, 0), (0, 0, 1.05), .05, 'Wood'),
                             g.rod('PickA', (0, 0, 1), (0, -.45, .88), .045, 'Iron', 5), g.rod('PickB', (0, 0, 1), (0, .45, .88), .045, 'Iron', 5)),
    'Tool_Saw': lambda: (g.box('Grip', (0, 0, .12), (.1, .14, .26), 'Wood'), g.box('Blade', (0, 0, .62), (.03, .2, .78), 'Stone', .01)),
    'Tool_Pestle': lambda: (g.rod('Pestle', (0, 0, 0), (0, 0, .8), .07, 'Cream'), g.rock('Tip', (0, 0, .82), (.12, .12, .1), 'Cream', 1)),
    'Tool_Scalpel': lambda: (g.rod('Grip', (0, 0, 0), (0, 0, .55), .035, 'Iron'), g.box('Blade', (0, -.02, .66), (.015, .07, .2), 'White', .005)),
    'Tool_Spoon': lambda: (g.rod('Handle', (0, 0, 0), (0, 0, .85), .035, 'Wood'), g.bowl('Bowl', (0, 0, .85), .14, .08, 'Wood')),
    'Tool_Stamp': lambda: (g.rod('Grip', (0, 0, 0), (0, 0, .35), .07, 'Wood'), g.box('Face', (0, 0, .4), (.22, .22, .08), 'Amber')),
}
for i, (name, build) in enumerate(TOOLS.items()):
    tool(name, build).location = (i * .03, 0, 0)  # 미리보기용 나열(내보낼 때 원점 0으로)

bpy.ops.wm.save_as_mainfile(filepath=str(BASE / 'ant-tools.blend'))
for o in bpy.data.objects:
    if o.parent is None: o.location = (0, 0, 0)
bpy.ops.export_scene.fbx(filepath=str(BASE / 'exports' / 'Ant_Tools.fbx'), object_types={'EMPTY', 'MESH'}, add_leaf_bones=False, bake_anim=False)
print('TOOLS', list(TOOLS))
