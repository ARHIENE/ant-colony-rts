"""Validate Blender animation asset structure, not Unity gameplay."""
import bpy,json,math
from pathlib import Path
HERE=Path(__file__).resolve().parent
mapping=json.loads((HERE/'facility-map.json').read_text(encoding='utf-8'))
spec=json.loads((HERE/'clips.json').read_text(encoding='utf-8'))
errors=[];curves=0;weighted=0
reference=set(bpy.data.objects['ResidentRig_01'].data.bones.keys())
for name,info in spec.items():
    act=bpy.data.actions.get('Resident|'+name)
    if not act:errors.append('Missing action '+name);continue
    for layer in act.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                for fc in bag.fcurves:
                    curves+=1
                    if any(not math.isfinite(float(k.co.y)) for k in fc.keyframe_points):errors.append('Nonfinite '+name)
                    if info['loop'] and abs(fc.evaluate(1)-fc.evaluate(info['frames']+1))>1e-4:errors.append('Loop seam '+name+' '+fc.data_path)
actors=[o for o in bpy.data.objects if o.type=='ARMATURE' and 'Hand.L' in o.data.bones and o.name.startswith(('ResidentRig_','CC_'))]
for a in actors:
    if set(a.data.bones.keys())!=reference:errors.append('Skeleton mismatch '+a.name)
    for o in a.children:
        if o.type!='MESH':continue
        deform=[m for m in o.modifiers if m.type=='ARMATURE' and m.object==a]
        if not deform:errors.append('No deformation '+o.name);continue
        weighted+=1
        for v in o.data.vertices:
            total=sum(g.weight for g in v.groups if o.vertex_groups[g.group].name in reference)
            if abs(total-1)>1e-4:errors.append('Weight '+o.name);break
for rid,info in mapping.items():
    s=bpy.data.scenes.get(info['scene'])
    if not s:errors.append('Missing scene '+rid);continue
    if info['actor']:
        a=s.objects.get(info['actor'])
        if not a or a.animation_data.action.name!='Resident|'+info['clip']:errors.append('Action mapping '+rid)
    if any(o.type=='ARMATURE' and 'Hand.L' not in o.data.bones for o in s.objects):errors.append('Unexpected rig '+rid)
report={'actions':len(spec),'curves':curves,'rigs':len(actors),'weighted_meshes':weighted,'facility_scenes':len(mapping),'errors':errors,'scope':'Structural validation only; not all contact poses or Unity import tested'}
(HERE/'validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(report,ensure_ascii=False))
