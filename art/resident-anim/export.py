"""Generic rig and individual animation FBX files; no Unity scene changes."""
import bpy,json
from pathlib import Path
HERE=Path(__file__).resolve().parent
out=HERE/'exports';out.mkdir(exist_ok=True)
scene=bpy.data.scenes['Resident_Animation_Library'];bpy.context.window.scene=scene
actor=bpy.data.objects['ResidentRig_01'];spec=json.loads((HERE/'clips.json').read_text())
def select(objects):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=actor
def export(path,animation):
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=animation,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
previous=actor.animation_data.action
actor.animation_data.action=None
for p in actor.pose.bones:p.location=(0,0,0);p.rotation_euler=(0,0,0);p.scale=(1,1,1)
select([actor]+[o for o in actor.children if o.type=='MESH'])
export(out/'Resident_Generic.fbx',False)
select([actor])
for name,info in spec.items():
    act=bpy.data.actions['Resident|'+name];actor.animation_data.action=act
    actor.animation_data.action_slot=act.slots[0]
    scene.frame_start=1;scene.frame_end=info['frames']+1;scene.frame_set(1)
    export(out/(name+'.fbx'),True)
actor.animation_data.action=previous;actor.animation_data.action_slot=previous.slots[0]
scene.frame_end=49;scene.frame_set(1)
print('Exported rig and',len(spec),'animation files')
