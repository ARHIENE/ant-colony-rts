import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector
W=Path('E:/Git/ant/codex-work~'); R=W/'round3'; sys.path.insert(0,str(W/'blender/scripts'))
import review,render_common as RC
label=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'baseline'
rig=review.setup(str(R/'blender'/(label+'.blend'))); rig.data.pose_position='POSE'; rig.animation_data.action=None
for b in rig.pose.bones: b.matrix_basis.identity()
worn=review.dress_one(rig,hair='BOB',ears='RABBIT',tail='FOX',tier='Steel',helm=False)
keep={'Part_Body_BASE_ARMOR','Part_Outfit_LINEN_ARMOR','Part_Hair_BOB_00','Part_Ears_RABBIT_00','Part_Tail_FOX_00','Part_Face_NEUTRAL_00'}
for o in bpy.data.objects:
 if o.name.startswith('Part_'): o.hide_render=o.name not in keep; o.hide_set(o.hide_render)
for o in worn:
 o.hide_render=o.name.startswith('Belt'); o.hide_set(o.hide_render)
cam=RC.setup(720,transparent=True,res_y=900); cam.data.type='ORTHO'; cam.data.ortho_scale=1.38
sc=bpy.context.scene
try: sc.eevee.taa_render_samples=32
except: pass
sc.render.image_settings.color_mode='RGBA'
# Rest, plus samples with the largest original target overlap when the canonical baseline is ready.
poses=[('Rest',None,1)]
report=R/'reports/baseline-frame-pairs.json'
if report.exists():
 d=json.loads(report.read_text()); chosen=[]
 for item,part in [('Armor_Breastplate_Lower','Part_Outfit_LINEN_ARMOR'),('Armor_Fauld','Part_Body_BASE_ARMOR')]:
  best=max([(v,k) for k,v in d.items() if k.startswith(item+'|'+part+'|')],default=None)
  if best:
   fields=best[1].split('|'); clip=fields[-2]; f=int(fields[-1]); key=(clip,f)
   if key not in chosen: chosen.append(key); poses.append((item,clip,f))
info=[]
for pose,clip,frame in poses:
 if clip:
  a=next(a for a in bpy.data.actions if a.name.split('|',1)[-1]==clip); rig.animation_data.action=a; rig.animation_data.action_slot=a.slots[0]
 else:
  rig.animation_data.action=None
  for b in rig.pose.bones: b.matrix_basis.identity()
 sc.frame_set(frame); bpy.context.view_layer.update()
 for view,d in [('front',(0,-1,.10)),('side',(1,0,.10)),('back',(0,1,.10))]:
  v=Vector(d).normalized(); target=Vector((0,-.015,.56)); cam.location=target+v*3; cam.rotation_euler=(-v).to_track_quat('-Z','Y').to_euler()
  p=R/'renders'/f'{label}-{pose}-{view}.png'; RC.shot(p); info.append({'file':str(p),'pose':pose,'clip':clip,'frame':frame,'view':view})
(R/'reports'/('renders-'+label+'.json')).write_text(json.dumps(info,indent=2),encoding='utf-8')
print('RENDERED',label,len(info),flush=True)
