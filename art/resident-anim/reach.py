"""Forward reach and soft forearms for short-handed work poses."""
import bpy,sys
from mathutils import Vector
sys.path.insert(0,'E:/Git/ant/art/characters/toto-v1');import forms as f
reference=bpy.data.objects['ResidentRig_01']
for name in ['Hammer','Chop','Mine','Attack','Stamp','BenchWork','Treat','Operate','Stir','Grind','Saw','Wash']:
    a=bpy.data.actions['Resident|'+name]
    if a.get('reach_corrected'):continue
    for hand in ['Hand.L','Hand.R']:
        delta=reference.data.bones[hand].matrix_local.to_3x3().inverted()@Vector((0,-.30,0))
        for layer in a.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        if fc.data_path=='pose.bones["'+hand+'"].location':
                            for kp in fc.keyframe_points:kp.co.y+=delta[fc.array_index]
    a['reach_corrected']=True
for scene in list(bpy.data.scenes):
    actors=[o for o in scene.objects if o.type=='ARMATURE' and 'Hand.L' in o.data.bones]
    if not actors:continue
    bpy.context.window.scene=scene
    for actor in actors:
        if actor.get('forearms_added'):continue
        body=next(o for o in actor.children if o.type=='MESH' and 'V4_Body' in o.name);mat=body.data.materials[0]
        for side in ['L','R']:
            a=actor.data.bones['Arm.'+side].head_local;b=actor.data.bones['Hand.'+side].head_local
            points=[a.lerp(b,t/4) for t in range(5)]
            o=f.tube(actor.name+'_SoftArm.'+side,points,.085,mat,actor,12)
            upper=o.vertex_groups.new(name='Arm.'+side);hand=o.vertex_groups.new(name='Hand.'+side)
            for v in o.data.vertices:
                t=(v.index//12)/4
                if t<1:upper.add([v.index],1-t,'REPLACE')
                if t>0:hand.add([v.index],t,'REPLACE')
            m=o.modifiers.new('Arm stretch','ARMATURE');m.object=actor
        actor['forearms_added']=True
bpy.context.window.scene=bpy.data.scenes['Work__CC_Workshop'];bpy.context.scene.frame_set(7)
