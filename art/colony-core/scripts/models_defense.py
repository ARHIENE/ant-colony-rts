"""Fortifications and defensive devices. Moving parts have named hinge roots."""
import math,bpy
from mathutils import Vector
import geometry as g

def hinge(name,p,objects):
    o=bpy.data.objects.new(name,None); bpy.context.scene.collection.objects.link(o); o.parent=g.ROOT; o.location=p
    for child in objects: child.parent=o; child.location-=Vector(p)
    return o

def build():
    g.asset('CC_Watchtower')
    for x in [-.7,.7]:
        for y in [-.7,.7]: g.rod('Tower leg',(x,y,0),(x,y,3),.11)
    for y in [-.7,.7]:
        g.rod('Cross brace',(-.7,y,.2),(.7,y,2.5),.065)
        g.rod('Cross brace',(.7,y,.2),(-.7,y,2.5),.065)
    for i in range(6): g.box('Platform',((i-2.5)*.3,0,2.45),(.28,1.9,.14),'CutWood')
    for y in [-.82,.82]: g.rod('Guardrail',(-.9,y,3.05),(.9,y,3.05),.06)
    for x in [-.9,.9]: g.rod('Guardrail',(x,-.82,3.05),(x,.82,3.05),.06)
    for x in [-.32,.32]: g.rod('Ladder',(x,-1.2,0),(x,-.8,2.5),.045)
    for i in range(8): g.rod('Rung',(-.32,-1.2+i*.05,.18+i*.29),(.32,-1.2+i*.05,.18+i*.29),.035,'CutWood')
    for i in range(5): g.leaf('Canopy',((i-2)*.4,0,3.45),2.1,.7,0)
    for x in [-.7,.7]: g.rod('Roof post',(x,.7,2.45),(x,.7,3.5),.07)
    g.asset('CC_CastleWall')
    for row in range(4):
        for col in range(3): g.box('Stone block',((col-1)*.8,0,.24+row*.47),(.78,.9,.45),'Stone',.06)
    for x in [-.9,0,.9]: g.box('Merlon',(x,0,2.06),(.48,.92,.48),'Stone',.055)
    g.asset('CC_CastleGate')
    for x in [-1.2,1.2]:
        for row in range(5): g.box('Pier',(x,0,.25+row*.48),(.6,.9,.47),'Stone',.055)
    g.box('Lintel',(0,0,2.5),(3,.9,.32),'Stone',.07)
    for side in [-1,1]:
        children=[]
        for i in range(4): children.append(g.box('Gate plank',(side*(.12+i*.24),0,1.12),(.22,.16,2.16),'Wood',.03))
        for z in [.45,1.7]: children.append(g.box('Iron strap',(side*.48,-.12,z),(.92,.07,.13),'Iron',.015))
        hinge('GateLeft_Hinge' if side<0 else 'GateRight_Hinge',(side*.96,0,0),children)
    g.asset('CC_AcidTower')
    for i in range(6):
        a=i*math.tau/6; g.rock('Foundation',(.6*math.cos(a),.6*math.sin(a),.2),(.35,.3,.25),'Stone')
    g.rod('Reservoir',(0,0,.2),(0,0,1.55),.40,'Leaf',10)
    for z in [.45,1.30]: g.rod('Band',(0,0,z),(0,0,z+.12),.43,'Iron',10)
    parts=[g.rod('Nozzle body',(0,0,1.6),(0,-.55,1.6),.16,'Iron',8),g.rod('Tip',(0,-.55,1.6),(0,-.85,1.6),.10,'Amber',8)]
    hinge('Turret_Pivot',(0,0,1.6),parts)
    g.asset('CC_SpikeTrap')
    g.box('Plate',(0,0,.06),(1.5,1.5,.12),'Earth',.08)
    for x in [-.45,0,.45]:
        for y in [-.45,0,.45]:
            bpy.ops.mesh.primitive_cone_add(vertices=5,radius1=.08,radius2=0,depth=.48,location=(x,y,.34))
            g.finish(bpy.context.object,'Spike','CutWood')
    g.asset('CC_StickyTrap')
    g.box('Leaf tray',(0,0,.035),(1.5,1.5,.07),'Leaf',.12)
    g.rock('Resin pool',(0,0,.08),(.65,.65,.045),'Amber',2)
    for x,y in [(-.4,.2),(.2,.35),(.35,-.25)]: g.rock('Resin glob',(x,y,.12),(.12,.1,.07),'Amber')
