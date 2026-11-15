import bpy, sys, json, math
import numpy as np
from pathlib import Path

name=sys.argv[sys.argv.index('--')+1]
root=Path('F:/UnityProjects/ant-colony-rts')
source=root/'art/enemy-concepts-1114'/name
out=root/'work/organic-combat-1114/motion-audit'/name
if len(sys.argv)>sys.argv.index('--')+2:
    source=Path(sys.argv[sys.argv.index('--')+2])/name
    out=root/'work/organic-combat-1114/polished-audit'/name
out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(source/(name+'_5Motions.blend')))
rig=bpy.data.objects[name+'_Rig']; mesh=bpy.data.objects[name+'_Mesh']; scene=bpy.context.scene
def points():
    ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
    a=np.empty(len(ev.data.vertices)*3,dtype=np.float32)
    ev.data.vertices.foreach_get('co',a)
    return a.reshape(-1,3)
rig.animation_data.action=bpy.data.actions['Idle']; scene.frame_set(1)
base=points(); extent=float(np.linalg.norm(np.ptp(base,axis=0)))
edges=np.array([e.vertices[:] for e in mesh.data.edges],dtype=np.int32)
lengths=np.linalg.norm(base[edges[:,0]]-base[edges[:,1]],axis=1)
valid=lengths>extent*.0005; edges=edges[valid]; lengths=lengths[valid]
report=[]
for spec in json.loads((source/'clips.json').read_text()):
    action=bpy.data.actions[spec['name']]; rig.animation_data.action=action
    previous=None; oldq=None; peak_step=0.; peak_angle=0.; peak_ratio=1.; worst_frame=1
    frames=[]; start=None; end=None
    for f in range(1,spec['frames']+1):
        scene.frame_set(f); p=points()
        q=np.array([tuple(b.rotation_quaternion.normalized()) for b in rig.pose.bones])
        ratio=np.linalg.norm(p[edges[:,0]]-p[edges[:,1]],axis=1)/lengths
        stretch=float(np.quantile(ratio,.999))
        if stretch>peak_ratio: peak_ratio=stretch; worst_frame=f
        step=0. if previous is None else float(np.max(np.linalg.norm(p-previous,axis=1)))/extent
        angle=0. if oldq is None else float(np.max(2*np.arccos(np.clip(np.abs(np.sum(q*oldq,axis=1)),0,1))))*180/math.pi
        peak_step=max(peak_step,step); peak_angle=max(peak_angle,angle)
        frames.append({'frame':f,'step':step,'jointDegrees':angle,'stretch999':stretch,'minZ':float(p[:,2].min())})
        if start is None: start=p.copy()
        end=p.copy(); previous=p; oldq=q
    report.append({'clip':spec['name'],'peakStepFraction':peak_step,'peakJointDegrees':peak_angle,
        'peakEdgeStretch999':peak_ratio,'worstStretchFrame':worst_frame,
        'loopGapFraction':float(np.max(np.linalg.norm(end-start,axis=1)))/extent,
        'idleReturnGapFraction':float(np.max(np.linalg.norm(end-base,axis=1)))/extent,
        'frames':frames})
(out/'metrics.json').write_text(json.dumps(report,indent=2))
print(json.dumps({'name':name,'clips':[{k:v for k,v in r.items() if k!='frames'} for r in report]}))
