import bpy, json, math, runpy
import numpy as np
from pathlib import Path
from mathutils import Vector
ROOT=Path('F:/UnityProjects/ant-colony-rts/work/organic-horde-motion-1114')
profiles=runpy.run_path(str(ROOT.parents[1]/'art/enemy-concepts-1114/remaining_motion_profiles.py'))['PROFILES']
profiles.update({'WallRam':{'feet':[10,14,31,35]},'NeedleStalker':{'feet':[10,15]},'BloomMender':{'feet':[7,12,17,22,27]}})
reports=[]
for profile_path in sorted((ROOT/'refined').glob('*/contact-profile.json')):
 name=profile_path.parent.name; source=profile_path.parent
 bpy.ops.wm.open_mainfile(filepath=str(source/(name+'_5Motions.blend')))
 rig=bpy.data.objects[name+'_Rig'];mesh=bpy.data.objects[name+'_Mesh'];scene=bpy.context.scene
 def points():
  ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get());p=np.empty(len(ev.data.vertices)*3,dtype=np.float32);ev.data.vertices.foreach_get('co',p)
  # Authoring mesh transforms are identity; report in the same source-space units as existing audits.
  return p.reshape(-1,3)
 rig.animation_data.action=bpy.data.actions['Idle'];scene.frame_set(1);base=points();extent=float(np.linalg.norm(np.ptp(base,axis=0)))
 edges=np.array([e.vertices[:] for e in mesh.data.edges]);lengths=np.linalg.norm(base[edges[:,0]]-base[edges[:,1]],axis=1)
 valid=lengths>extent*.0005;edges=edges[valid];lengths=lengths[valid]
 data=json.loads(profile_path.read_text());results=[]
 for spec in json.loads((source/'clips.json').read_text()):
  if spec['name'] not in data['changed']:continue
  rig.animation_data.action=bpy.data.actions[spec['name']]
  previous=None;oldq=None;peakstep=0;peakangle=0;stretch=1;ground=0;first=None;last=None
  rider_first=None;rider_delta=0;previous_feet=None;slip=[]
  feet=profiles[name]['feet'];forward=Vector((-.56,-.83,0) if name=='WallRam' else (0,-1,0)).normalized()
  for f in range(1,spec['frames']+1):
   scene.frame_set(f);p=points();q=np.array([tuple(b.rotation_quaternion.normalized()) for b in rig.pose.bones])
   stretch=max(stretch,float(np.quantile(np.linalg.norm(p[edges[:,0]]-p[edges[:,1]],axis=1)/lengths,.999)))
   ground=max(ground,abs(float(p[:,2].min())))
   if previous is not None:
    peakstep=max(peakstep,float(np.max(np.linalg.norm(p-previous,axis=1)))/extent)
    peakangle=max(peakangle,float(np.max(2*np.arccos(np.clip(np.abs(np.sum(q*oldq,axis=1)),0,1))))*180/math.pi)
   if first is None:first=p.copy()
   if data['riderBones']:
    rider=rig.pose.bones['Bone_%03d'%data['riderBones'][0]].rotation_quaternion.copy()
    if rider_first is None:rider_first=rider.copy()
    rider_delta=max(rider_delta,rider.rotation_difference(rider_first).angle*180/math.pi)
   tails=[rig.matrix_world@rig.pose.bones['Bone_%03d'%i].tail for i in feet]
   if spec['name']=='Move' and previous_feet is not None:
    for i,point in enumerate(tails):
     phase=((f-1)/30+(i%2)*.5)%1;oldphase=((f-2)/30+(i%2)*.5)%1
     if 0<oldphase<phase<.6:
      residual=(point-previous_feet[i])*30+forward*data['strideSourceUnitsPerSecond'];residual.z=0;slip.append(residual.length)
   previous_feet=tails
   previous=p;oldq=q;last=p
  results.append(dict(clip=spec['name'],stepFraction=peakstep,jointStepDegrees=peakangle,edgeStretch999=stretch,groundAbsolute=ground,loopGapFraction=float(np.max(np.linalg.norm(last-first,axis=1)))/extent,riderDegrees=rider_delta,stanceSlipSpeedMean=float(np.mean(slip)) if slip else None,stanceSlipSpeedMax=max(slip) if slip else None))
 report=dict(name=name,clips=results);reports.append(report);(ROOT/'contact-audit.json').write_text(json.dumps(reports,indent=2));print(json.dumps(report),flush=True)
