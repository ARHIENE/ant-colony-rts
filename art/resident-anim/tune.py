"""Furniture proportion and preview visibility corrections for the new biped."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent
mapping=json.loads((HERE/'facility-map.json').read_text(encoding='utf-8'))
tables={'CC_Workshop','CC_Kitchen','CC_Apothecary','CC_ButcherTable','CC_ResearchDesk','CC_AdminDesk','CC_DiningTable','CC_Washbasin'}
beds={'CC_CommanderBed','CC_AnimalTreatment','CC_SleepingMat'}
for rid,info in mapping.items():
    s=bpy.data.scenes[info['scene']];bpy.context.window.scene=s;root=bpy.data.objects[rid];actor=bpy.data.objects.get(info['actor']) if info['actor'] else None
    if rid in tables:root.scale=(1.30,1.25,.80)
    elif rid in beds:root.scale=(1.55,1.45,1)
    elif rid in ('CC_Chair','CC_RestBench','CC_Toilet'):root.scale=(1.40,1.40,1)
    if actor and info['clip']!='Carry':
        size=.85 if rid in tables or rid in beds else .65;actor.scale=(size,)*3
        pts=[o.matrix_world@Vector(v) for o in root.children_recursive if o.type=='MESH' for v in o.bound_box];bpy.context.view_layer.update()
        if rid in tables:
            front=min(p.y for p in pts);actor.location.y=front-.36
        # Retain a visible margin around beds instead of covering the entire mattress.
        if info['clip']=='Sleep':actor.location.y=-.75;actor.location.z=.10
        for o in actor.children:
            if 'resident_tool' in o:
                for v in o.data.vertices:v.co=Vector((.43,-.50,.63))+Matrix.Rotation(math.radians(45),3,'X')@(v.co-Vector((.43,-.50,.63)))
    s.frame_set(1);bpy.context.view_layer.update()
    obs=[root,*root.children_recursive]+([actor,*actor.children_recursive] if actor else [])
    pts=[o.matrix_world@Vector(v) for o in obs if o.type=='MESH' for v in o.bound_box]
    lo=Vector([min(v[i] for v in pts) for i in range(3)]);hi=Vector([max(v[i] for v in pts) for i in range(3)]);center=(lo+hi)/2
    d=Vector((-2.3,.7,1.5)).normalized();s.camera.location=center+d*20;s.camera.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=max(hi-lo)*1.5+.2
    info['facility_scale']=list(root.scale);info['scale']=float(actor.scale.x) if actor else None;info['work_position']=list(actor.location) if actor else None
(HERE/'facility-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.context.window.scene=bpy.data.scenes['Work__CC_Workshop']
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'resident-interactions.blend'))
