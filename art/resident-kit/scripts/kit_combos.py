"""Manual combos that mix parts from different source residents (no random generator).

Each combo = its own rig object (shared armature data) + linked copies of part meshes.
Fur/Inner material slots are overridden per object with one palette index.
"""
import bpy, sys, json, importlib
from mathutils import Matrix
sys.path.insert(0, r"E:\Git\ant\art\resident-kit\scripts")
import kit_common as K
importlib.reload(K)

COMBOS = {
    'Combo_A': {'palette': 3, 'parts': ['Body_01', 'Tail_01', 'Ears_02', 'TopCrown_01', 'TopFlower_01', 'Face_02', 'OutfitBag_01']},
    'Combo_B': {'palette': 16, 'parts': ['Body_01', 'Tail_01', 'Ears_04', 'TopLeaf_01', 'Face_03', 'OutfitCape_02', 'Glasses_01', 'Hair_03_CURL', 'Injury_SCAR_CHEEK', 'Injury_BANDAGE_ARM_L']},
    'Combo_C': {'palette': 22, 'parts': ['Body_01', 'Tail_01', 'Ears_03', 'Hair_06_SOFT_CREST', 'Expr_SLEEPY', 'OutfitCollar_03', 'Mark_01', 'Injury_BRUISE_FOREHEAD']},
    'Combo_D': {'palette': 11, 'parts': ['Body_01', 'Tail_01', 'Ears_01', 'TopCheek_01', 'Face_01', 'OutfitBag_01', 'OutfitCape_03', 'Hair_02_ROUND_FRINGE']},
}
# State → face part (expressions and injuries stay separate parts).
STATE_FACE = {'Sleep': 'Expr_SLEEP', 'Death': 'Expr_DEAD', 'Hurt': 'Expr_PAIN', 'Eat': 'Expr_TASTY', 'Mourn': 'Expr_SAD', 'Rest': 'Expr_SLEEPY'}


def palette(i):
    return bpy.data.materials.get('Fur_%02d' % i), bpy.data.materials.get('Inner_%02d' % i)


def build_combo(name, spec, location=(0, 0, 0), collection=None, face=None):
    collection = collection or K.collection('Combos')
    master = bpy.data.objects['ResidentRig']
    rig = master.copy(); rig.name = name; rig.animation_data_clear()
    collection.objects.link(rig); rig.location = location
    fur, inner = palette(spec['palette'])
    parts = list(spec['parts'])
    if face:  # swap whichever face/expression part the combo has
        parts = [p for p in parts if not p.startswith(('Face_', 'Expr_'))] + [face]
    for p in parts:
        src = bpy.data.objects['Part_' + p]
        o = src.copy(); o.name = name + '__' + p
        collection.objects.link(o)
        o.parent = rig; o.matrix_parent_inverse = Matrix.Identity(4); o.matrix_basis = Matrix.Identity(4)
        o.hide_viewport = False; o.hide_render = False
        for m in o.modifiers:
            if m.type == 'ARMATURE': m.object = rig
        for slot in o.material_slots:
            data_mat = slot.material
            if data_mat and data_mat.name.startswith(('Fur_', 'Inner_')):
                slot.link = 'OBJECT'
                slot.material = fur if data_mat.name.startswith('Fur_') else (inner or fur)
    rig['combo'] = json.dumps(spec)
    return rig

