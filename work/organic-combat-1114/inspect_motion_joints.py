import bpy,json,math
from pathlib import Path
root=Path('F:/UnityProjects/ant-colony-rts')
out=root/'work/organic-combat-1114/motion-audit'
results=[]
for path in out.glob('*/metrics.json'):
    name=path.parent.name
    candidates=[r for r in json.loads(path.read_text()) if r['peakJointDegrees']>20 or r['peakEdgeStretch999']>1.8]
    if not candidates: continue
    bpy.ops.wm.open_mainfile(filepath=str(root/'art/enemy-concepts-1114'/name/(name+'_5Motions.blend')))
    rig=bpy.data.objects[name+'_Rig']
    for r in candidates:
        rig.animation_data.action=bpy.data.actions[r['clip']]
        frame=max(r['frames'],key=lambda f:f['jointDegrees'])['frame']
        bpy.context.scene.frame_set(frame-1); before={b.name:b.rotation_quaternion.copy() for b in rig.pose.bones}
        bpy.context.scene.frame_set(frame)
        angles=sorted([(min(b.rotation_quaternion.rotation_difference(before[b.name]).angle,2*math.pi-b.rotation_quaternion.rotation_difference(before[b.name]).angle)*180/math.pi,b.name) for b in rig.pose.bones],reverse=True)
        results.append({'name':name,'clip':r['clip'],'frame':frame,'joints':angles[:4]})
(out/'joint-spikes.json').write_text(json.dumps(results,indent=2))
print(json.dumps(results))
