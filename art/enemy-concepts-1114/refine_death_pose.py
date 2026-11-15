import bpy,sys,math
from pathlib import Path
from mathutils import Vector,Quaternion

name=sys.argv[sys.argv.index('--')+1]
out=Path(__file__).resolve().parent/name
if len(sys.argv)>sys.argv.index('--')+2:
    out=Path(sys.argv[sys.argv.index('--')+2])/name
bpy.ops.wm.open_mainfile(filepath=str(out/(name+'_5Motions.blend')))
rig=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']
root=rig.pose.bones['Bone_000']; s=bpy.context.scene
rig.animation_data.action=bpy.data.actions['Death']
inv=root.bone.matrix_local.to_3x3().inverted()
axis=(inv@Vector((-.56,-.83,0) if name=='WallRam' else (0,-1,0))).normalized()
s.frame_set(61)
def height_at(angle):
    root.rotation_quaternion=Quaternion(axis,angle)
    bpy.context.view_layer.update()
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    heights=[(ev.matrix_world@v.co).z for v in ev.data.vertices]
    return max(heights)-min(heights)
# Upright creatures settle on their side; broad carapaces can settle on their back.
# Select using the actual skinned silhouette instead of one angle for every body.
final_angle=min([math.pi/2+i*math.pi/32 for i in range(17)],key=height_at)
for f in range(1,62):
    s.frame_set(f)
    t=(f-1)/60; fall=min(1,max(0,(t-.12)/.68)); fall=fall*fall*(3-2*fall)
    root.rotation_mode='QUATERNION'; root.rotation_quaternion=Quaternion(axis,final_angle*fall)
    root.location=inv@Vector((0,0,-.38*fall))
    root.keyframe_insert('rotation_quaternion',frame=f); root.keyframe_insert('location',frame=f)
    bpy.context.view_layer.update()
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    min_z=min((ev.matrix_world@v.co).z for v in ev.data.vertices)
    root.location+=inv@Vector((0,0,max(0,-min_z)-max(0,min_z)*fall))
    root.keyframe_insert('location',frame=f)
bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True); mesh.select_set(True)
bpy.context.view_layer.objects.active=rig; s.frame_start=1; s.frame_end=61
bpy.ops.export_scene.fbx(filepath=str(out/'Death.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
rig.animation_data.action=bpy.data.actions['Idle']; s.frame_set(1); s.frame_end=91
bpy.ops.wm.save_as_mainfile(filepath=str(out/(name+'_5Motions.blend')))
(out/'death-refined.txt').write_text(f'Final roll {final_angle:.6f} radians selected for low resting silhouette; grounded per frame. See export-checks.json for validation.')
