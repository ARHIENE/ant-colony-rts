"""Headless entry: blender -b --factory-startup --python run.py, then (fresh process) --python export.py
Rebuilds the Kiki reference scene (same import/pose as kemono-kiki-v1/build.py), then runs build.py."""
import bpy
from pathlib import Path
from mathutils import Vector, Quaternion

HERE=Path(__file__).resolve().parent
s=bpy.data.scenes.new('Kiki_Reference_Study');bpy.context.window.scene=s
bpy.ops.import_scene.fbx(filepath='E:/Git/ant/Assets/Kiki/Xmas Girl/Model/Kiki-v2.fbx')
rig=next(o for o in s.objects if o.type=='ARMATURE')
rig.data.pose_position='POSE'
for bone in rig.pose.bones:bone.matrix_basis.identity()
for side,angle in [('L',.40),('R',-.40)]:
    bone=rig.pose.bones['upper_arm.'+side]
    axis=bone.bone.matrix_local.to_3x3().inverted()@Vector((0,1,0))
    bone.rotation_mode='QUATERNION';bone.rotation_quaternion=Quaternion(axis,angle)
face=next(o for o in s.objects if o.type=='MESH' and o.name.startswith('Face.'))
for key in face.data.shape_keys.key_blocks:
    if key.name!='Basis':key.value=0
bpy.context.view_layer.update()
exec(compile((HERE/'build.py').read_text(encoding='utf-8'),'build.py','exec'))
