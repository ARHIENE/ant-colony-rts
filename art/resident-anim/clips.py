"""Authored biped pose curves. Root motion belongs to gameplay, not these clips."""
import bpy,math
from mathutils import Vector,Euler
TAU=math.tau
SPECS={
 'Idle':(48,True),'Walk':(24,True),'Run':(16,True),'StandEnter':(10,False),'StandExit':(10,False),'StandIdle':(48,True),
 'Pickup':(24,False),'PutDown':(24,False),'Carry':(24,True),'GroundWork':(32,True),'BenchWork':(32,True),
 'Hammer':(24,True),'Chop':(28,True),'Mine':(28,True),'Treat':(40,True),'Operate':(48,True),
 'Stir':(40,True),'Grind':(32,True),'Saw':(32,True),'Stamp':(32,True),'Wash':(32,True),
 'Eat':(48,True),'Sit':(48,True),'Rest':(48,True),'Sleep':(64,True),'Mourn':(48,False),'Inspect':(64,True),
 'Attack':(24,False),'Hurt':(16,False),'Death':(40,False)}
CONTACT={'Hammer':[13],'Chop':[15],'Mine':[15],'Stamp':[17],'Attack':[13],'Pickup':[15],'PutDown':[15]}
def pose(name,t):
    d={};s=math.sin(TAU*t);c=math.cos(TAU*t);pulse=(1-c)/2
    def rot(n,x=0,y=0,z=0):d[n]=(x,y,z)
    def loc(n,x=0,y=0,z=0):d[n+'_loc']=(x,y,z)
    rot('Head',.025*s);rot('Ear.L',0,0,.035*s);rot('Ear.R',0,0,-.035*s);rot('Tail',.06*s)
    loc('Body',z=.008*s)
    if name in ('Walk','Run','Carry'):
        amp=.60 if name=='Run' else .34
        rot('Foot.L',amp*s);rot('Foot.R',-amp*s)
        loc('Foot.L',z=max(0,s)*.065);loc('Foot.R',z=max(0,-s)*.065)
        loc('Body',z=.025*(1-math.cos(2*TAU*t)))
        rot('Body',.03 if name!='Run' else .14,0,.035*s)
        if name=='Carry':loc('Hand.L',y=.18,z=.06);loc('Hand.R',y=.18,z=.06)
        else:rot('Arm.L',-.24*s);rot('Arm.R',.24*s)
    elif name in ('Pickup','PutDown','GroundWork'):
        a=math.sin(math.pi*t)**2 if name!='GroundWork' else .65+.15*s
        rot('Body',.30*a);rot('Head',-.15*a);loc('Body',z=-.14*a)
        loc('Hand.L',y=.12*a,z=.10*a);loc('Hand.R',y=.12*a,z=.10*a)
        rot('Arm.L',.42*a+.07*s);rot('Arm.R',.42*a-.07*s)
    elif name in ('Hammer','Chop','Mine','Attack','Stamp'):
        loc('Hand.L',z=.20);loc('Hand.R',z=.20)
        # Windup, rapid downstroke at mid-cycle, then recovery.
        a=math.sin(math.pi*min(1,t*2)) if t<.5 else -.30*math.sin(math.pi*(t-.5)*2)
        rot('Arm.R',-.85*a);rot('Hand.R',-.25*a);rot('Body',.08*(1-a));rot('Head',.04*(1-a))
        if name in ('Mine','Chop'):rot('Arm.L',-.65*a);loc('Hand.L',x=.12,z=.20)
        if name=='Chop':rot('Body',.05,0,.12*a)
        if name=='Stamp':rot('Arm.R',-.40*a)
    elif name in ('BenchWork','Treat','Operate','Stir','Grind','Saw','Wash'):
        rot('Body',.08);rot('Head',.08);loc('Hand.L',y=.15,z=.20);loc('Hand.R',y=.15,z=.20)
        if name=='Stir':rot('Arm.R',.13*s,0,.15*c);rot('Hand.R',0,0,.1*s)
        elif name=='Saw':loc('Hand.R',y=.16+.08*s,z=.20);loc('Hand.L',y=.16+.08*s,z=.20)
        elif name=='Grind':rot('Arm.R',.30*pulse);loc('Hand.R',y=.15,z=.20+.08*pulse)
        elif name=='Wash':rot('Hand.L',.12*s,0,.12*c);rot('Hand.R',-.12*s,0,-.12*c)
        else:
            a=.035 if name=='Operate' else .10
            rot('Hand.R',a*s,0,a*c);rot('Arm.L',.04*s);rot('Head',.10 if name=='Treat' else .04)
    elif name in ('Sit','Rest','Eat'):
        loc('Body',z=-.10);rot('Foot.L',-.35);rot('Foot.R',-.35)
        if name=='Eat':rot('Arm.R',-.75*pulse);rot('Hand.R',-.15*pulse);rot('Head',.05*pulse)
        if name=='Rest':rot('Head',0,.04*s,.06*s);rot('Arm.L',-.2*pulse)
    elif name=='Sleep':
        rot('Root',-math.pi/2);loc('Root',z=.58);rot('Arm.L',.25);rot('Arm.R',.25);loc('Body',z=.006*s)
    elif name=='Mourn':
        a=math.sin(math.pi*t)**2;rot('Head',.26*a);rot('Body',.08*a);rot('Arm.L',.1*a);rot('Arm.R',.1*a)
    elif name=='Inspect':rot('Head',.08,0,.25*s);rot('Arm.R',-.20*pulse)
    elif name=='Hurt':
        a=math.sin(math.pi*t);rot('Body',-.18*a);rot('Head',.12*a);rot('Arm.L',-.18*a);rot('Arm.R',-.18*a)
    elif name=='Death':
        a=min(1,t*1.5);a=a*a*(3-2*a);rot('Root',0,-math.pi/2*a,0);loc('Root',z=.65*a);rot('Head',.12*a)
    elif name in ('StandEnter','StandExit'):
        a=t if name=='StandEnter' else 1-t;rot('Body',.06*a);rot('Arm.L',.12*a);rot('Arm.R',.12*a)
    return d
def build(rig):
    rig.animation_data_create();actions={}
    for name,(frames,loop) in SPECS.items():
        act=bpy.data.actions.new('Resident|'+name);rig.animation_data.action=act;act.use_fake_user=True
        for frame in range(1,frames+2):
            values=pose(name,(frame-1)/frames)
            for bone in rig.pose.bones:
                basis=bone.bone.matrix_local.to_3x3()
                world_rotation=Euler(values.get(bone.name,(0,0,0))).to_matrix()
                bone.rotation_euler=(basis.inverted()@world_rotation@basis).to_euler()
                move=Vector(values.get(bone.name+'_loc',(0,0,0)))
                if bone.name.startswith('Hand'):move.y=-move.y
                bone.location=basis.inverted()@move
                bone.keyframe_insert('rotation_euler',frame=frame,group=bone.name);bone.keyframe_insert('location',frame=frame,group=bone.name)
        for layer in act.layers:
            for strip in layer.strips:
                for bag in strip.channelbags:
                    for fc in bag.fcurves:
                        for kp in fc.keyframe_points:kp.interpolation='LINEAR'
                        if loop:fc.modifiers.new('CYCLES')
        for f in CONTACT.get(name,[]):act.pose_markers.new('VisualContact').frame=f
        act['loop']=loop;act['fps']=24;act['duration_frames']=frames;act['events_are_visual_only']=True;actions[name]=act
    rig.animation_data.action=actions['Idle'];return actions
