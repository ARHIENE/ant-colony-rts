import bpy,sys
from pathlib import Path
from mathutils import Vector
name=sys.argv[sys.argv.index('--')+1]
out=Path('F:/UnityProjects/ant-colony-rts/work/organic-combat-1114/polished')/name
bpy.ops.wm.open_mainfile(filepath=str(out/(name+'_5Motions.blend')))
s=bpy.context.scene; rig=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
rig.animation_data.action=bpy.data.actions['Attack']; s.frame_set(1)
base={b.name:(b.location.copy(),b.rotation_quaternion.copy(),b.scale.copy()) for b in rig.pose.bones}
samples=[]
for f in range(1,44):
    s.frame_set(f); samples.append({b.name:(b.location.copy(),b.rotation_quaternion.copy(),b.scale.copy()) for b in rig.pose.bones})
for f,pose in enumerate(samples,1):
    s.frame_set(f)
    for b in rig.pose.bones:
        a,q,k=base[b.name]; p,r,z=pose[b.name]
        gain=1 if '--ground-only' in sys.argv else .65
        b.location=a.lerp(p,gain); b.rotation_quaternion=q.slerp(r,gain); b.scale=k.lerp(z,gain)
        b.keyframe_insert('location',frame=f); b.keyframe_insert('rotation_quaternion',frame=f); b.keyframe_insert('scale',frame=f)
    bpy.context.view_layer.update()
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    minimum=min((ev.matrix_world@v.co).z for v in ev.data.vertices)
    root=rig.pose.bones['Bone_000']
    root.location+=root.bone.matrix_local.to_3x3().inverted()@Vector((0,0,max(0,-minimum)))
    root.keyframe_insert('location',frame=f)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); mesh.select_set(True); bpy.context.view_layer.objects.active=rig
s.frame_start=1; s.frame_end=43
bpy.ops.export_scene.fbx(filepath=str(out/'Attack.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
rig.animation_data.action=bpy.data.actions['Idle']; s.frame_set(1); s.frame_end=91
bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'_5Motions.blend')))
