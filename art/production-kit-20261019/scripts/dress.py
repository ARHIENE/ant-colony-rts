"""Assemble a review character: show one part combination and attach equipment to the rig (preview only).

Rigid parts → bone parent (no deformation, so plates/tools never bend). Soft parts → Armature modifier.
Tier = material swap of PK_Equip_Shell / PK_Equip_Plate + the matching trim objects.
"""
import bpy
import pk, rig_adapter as R
from equip_spec import TIERS, LEATHER


def tier_mat(tid):
    t = next(x for x in [*TIERS, LEATHER] if x[0] == tid)
    return pk.mat(t[2], metal=t[3], rough=.45 if t[3] else .85, name=f'PK_Tier_{tid}')


def attach_rigid(o, rig, bone):
    mw = o.matrix_world.copy()
    o.parent = rig; o.parent_type = 'BONE'; o.parent_bone = bone
    o.matrix_world = mw


def attach_soft(o, rig):
    o.parent = rig; o.parent_type = 'OBJECT'
    if not any(m.type == 'ARMATURE' for m in o.modifiers):
        m = o.modifiers.new('Skin', 'ARMATURE'); m.object = rig


def wear(rig, src, tier, col, suffix):
    """Copy an equipment object for this review character and attach it."""
    o = src.copy(); o.data = src.data.copy(); col.objects.link(o); o.name = f'{src.name}__{suffix}'
    for i, m in enumerate(o.data.materials):
        if m and m.name in ('PK_Equip_Shell', 'PK_Equip_Plate'): o.data.materials[i] = tier_mat(tier)
    if src.get('soft'): attach_soft(o, rig)
    else: attach_rigid(o, rig, src['bone'])
    return o


def hold(rig, tool, socket, col, suffix):
    o = tool.copy(); col.objects.link(o); o.name = f'{tool.name}__{suffix}'
    o.parent = rig; o.parent_type = 'BONE'; o.parent_bone = socket
    o.matrix_parent_inverse.identity()
    # bone-parented children sit at the bone TAIL; move back to the head = socket point
    o.location = (0, -rig.data.bones[socket].length, 0); o.rotation_euler = (0, 0, 0)
    return o
