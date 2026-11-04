"""Export parts and shared actions without preview scenery."""
import bpy
import json
from pathlib import Path

root=Path('E:/Git/ant/art/kemono-kit')
# Kit files hold a collection/rig, not scenes (see build.py); link them into a fresh scene.
def load(path,attr):
    with bpy.data.libraries.load(str(path),link=False) as (a,b):setattr(b,attr,list(getattr(a,attr)))
    s=bpy.data.scenes.new(path.stem);bpy.context.window.scene=s
    for x in getattr(b,attr):
        (s.collection.children if attr=='collections' else s.collection.objects).link(x)
    return s
s=load(root/'kemono-character-kit.blend','collections')
rig=bpy.data.objects['KemonoRig'];s.collection.objects.link(rig)
for o in s.objects:
    o.hide_set(False);o.hide_viewport=False;o.hide_render=False;o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(root/'exports/Kemono_Parts.fbx'),use_selection=True,
    object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,
    axis_forward='-Z',axis_up='Y',path_mode='COPY',embed_textures=True)

s=load(root/'kemono-animations.blend','objects')
rig=next(o for o in s.objects if o.type=='ARMATURE')
acts={tr.strips[0].action.name.split('|',1)[-1].split('.')[0]:tr.strips[0].action for tr in rig.animation_data.nla_tracks}
for tr in list(rig.animation_data.nla_tracks):rig.animation_data.nla_tracks.remove(tr)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
exported=[]
for name,act in acts.items():
    rig.animation_data.action=act;rig.animation_data.action_slot=act.slots[0]
    s.frame_start=1;s.frame_end=int(act['duration_frames'])+1
    bpy.ops.export_scene.fbx(filepath=str(root/'exports'/('Kemono@'+name+'.fbx')),
        use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,
        bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,
        bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y')
    exported.append(name)
(root/'exports/manifest.json').write_text(json.dumps({'parts':'Kemono_Parts.fbx','animations':exported,'unity_validated':False},indent=2),encoding='utf-8')
print('Exported parts and',len(exported),'animations')
