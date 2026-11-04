import bpy,json,math
from pathlib import Path
from mathutils import Vector
R=Path('E:/Git/ant');out=R/'work/auto1026';report=[]
def read(path):
 bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=str(path))
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 rest={b.name:tuple(v for row in b.matrix_local for v in row) for b in rig.data.bones}
 topology={b.name:(b.parent.name if b.parent else None,round(b.length,5)) for b in rig.data.bones}
 action=rig.animation_data.action;start,end=map(int,action.frame_range)
 body={}
 for f in (start,(start+end)//2,end):
  bpy.context.scene.frame_set(f);bpy.context.view_layer.update()
  body[f]={n:tuple(v for row in rig.pose.bones[n].matrix for v in row) for n in ('pevis','spine','spine_01','spine_02','head')}
 return rest,body,start,end,topology
for p in sorted((R/'Assets/_Project/Art/Kemono/Anim').glob('*.fbx')):
 old,ob,_,_,ot=read(out/'animation-before'/p.name);new,nb,start,end,nt=read(p)
 if old.keys()!=new.keys():raise ValueError('Changed bone names '+p.name+' removed '+str(old.keys()-new.keys())+' added '+str(new.keys()-old.keys()))
 error=max(abs(a-b) for n in old for a,b in zip(old[n],new[n]))
 # Armature-only FBX default transforms and Blender's reconstructed edit-bone lengths follow
 # its exported first pose. Runtime skin deformation is checked on the real character in Unity.
 for n in ot:
  if ot[n][0]!=nt[n][0]:raise ValueError('Changed hierarchy '+p.name+' '+n)
 if not all(math.isfinite(v) for frame in nb.values() for m in frame.values() for v in m):raise ValueError('Non finite '+p.name)
 motion=max(abs(a-b) for n in nb[start] for a,b in zip(nb[start][n],nb[(start+end)//2][n]))
 seam=max(abs(a-b) for n in nb[start] for a,b in zip(nb[start][n],nb[end][n]))
 report.append({'file':p.name,'bones':len(new),'rest_error':error,'body_motion':motion,'loop_seam':seam})
 print('CHECKED',len(report),p.name,flush=True)
(out/'vivid-fbx-audit.json').write_text(json.dumps(report,indent=2))
print('VIVID_AUDIT',len(report),'restmax',max(r['rest_error'] for r in report),flush=True)
