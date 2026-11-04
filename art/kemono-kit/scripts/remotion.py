"""Rebuild only the shared actions (kemono-animations.blend + clips.json) from motions.py.
Headless: blender -b --factory-startup --python remotion.py   (parts/rig are untouched)"""
import bpy, sys, json
from pathlib import Path
ROOT = Path('E:/Git/ant/art/kemono-kit')
sys.path.insert(0, str(ROOT / 'scripts'))
import animations
with bpy.data.libraries.load(str(ROOT / 'kemono-animations.blend'), link=False) as (a, b):
    b.objects = list(a.objects)
animrig = next(o for o in b.objects if o.type == 'ARMATURE')
bpy.context.scene.collection.objects.link(animrig)
for tr in list(animrig.animation_data.nla_tracks): animrig.animation_data.nla_tracks.remove(tr)
for act in [a for a in bpy.data.actions if a.name.startswith('Kemono|')]: bpy.data.actions.remove(act)
animrig.data.pose_position = 'POSE'
actions, specs = animations.create(animrig)
animrig.animation_data.action = actions['Idle']
for act in actions.values():
    track = animrig.animation_data.nla_tracks.new(); track.name = act.name; track.mute = True
    track.strips.new(act.name, 1, act)
bpy.data.libraries.write(str(ROOT / 'kemono-animations.blend'), {animrig, *actions.values()}, fake_user=True)
(ROOT / 'clips.json').write_text(json.dumps({n: {'frames': f, 'loop': l, 'fps': 24} for n, (f, l) in specs.items()}, indent=2), encoding='utf-8')
print('actions', len(actions))
