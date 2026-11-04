"""Compact can with an actual grip at the shared hand socket."""
import bpy, sys, math
from mathutils import Matrix, Vector
import kit_common as K
sys.path.insert(0, 'E:/Git/ant/art/characters/toto-v1')
import forms as F

def fit_can(obj, rig):
    mat = obj.data.materials[0]
    body = F.oval('CanBody', (0, -.18, -.18), (.18, .17, .19), mat, None, 12, 8)
    spout = F.tube('CanSpout', [(0, -.28, -.20), (0, -.46, -.08), (0, -.51, -.05)], .035, mat, None, 8)
    handle = F.tube('CanHandle', [(0, -.12 + .12 * math.cos(i*math.pi/12), -.10 + .13*math.sin(i*math.pi/12)) for i in range(25)], .025, mat, None, 8)
    parts = [body, spout, handle]
    merged = K.join(parts)
    # Handle top is the grip; retain the kit socket's orientation convention.
    bone = rig.data.bones['Socket.Tool.R']
    transform = bone.matrix_local.inverted() @ Matrix.Translation(bone.head_local) @ Matrix.Translation((0, .12, -.03))
    merged.data.transform(transform)
    previous = obj.data
    obj.data = merged.data.copy()
    if previous.users == 0:
        bpy.data.meshes.remove(previous)
    obj.data.name = obj.name
    bpy.data.objects.remove(merged, do_unlink=True)
    obj['grip_centered'] = True
