"""Harvestable environment silhouettes, at colony gameplay scale."""
import math
import geometry as g

def tree(name,tall=False):
    g.asset(name)
    h=3.7 if tall else 2.8
    g.rod('Trunk',(0,0,0),(.12,.05,h),.27)
    for i in range(5):
        a=i*math.tau/5
        g.rod('Root',(0,0,.3),(.62*math.cos(a),.62*math.sin(a),.045),.10)
    for x,y,z,s in [(-.75,0,h-.35,1),(.7,.2,h-.25,.95),(0,.45,h+.4,1.1),(0,-.6,h+.2,.85)]:
        g.rod('Branch',(0,0,h*.55),(x,y,z),.09)
        g.rock('Canopy',(x,y,z),(s,.9*s,.85*s),'Leaf' if x<0 else 'LeafLight',1)

def build():
    tree('CC_TreeBroadleaf'); tree('CC_TreeTall',True)
    g.asset('CC_Stump')
    g.rod('Stump',(0,0,0),(0,0,.48),.43)
    g.rod('Cut face',(0,0,.48),(0,0,.49),.36,'CutWood')
    g.rod('Heartwood',(0,0,.49),(0,0,.495),.16,'Wood')
    for i in range(5):
        a=i*math.tau/5; g.rod('Root',(0,0,.15),(.6*math.cos(a),.6*math.sin(a),.06),.1)
    for name,mat in [('CC_RockOutcrop','Stone'),('CC_OreOutcrop','Iron')]:
        g.asset(name)
        for x,y,z,s in [(-.45,0,.4,.7),(.45,.1,.4,.6),(0,.22,.8,.8)]:
            g.rock('Outcrop',(x,y,z),(s,s*.8,s*.85),'Stone',1)
        if mat=='Iron':
            for x,y,z in [(-.25,-.45,.7),(.35,-.38,.5),(.15,-.2,1.2)]:
                g.rock('Exposed ore',(x,y,z),(.23,.18,.27),'Iron',1)
                g.rock('Mineral facet',(x-.05,y-.08,z+.03),(.09,.06,.15),'Amber')
    g.asset('CC_PebbleScatter')
    for i in range(7):
        a=i*2.4; g.rock('Pebble',(.6*math.cos(a),.4*math.sin(a),.08),(.16,.12,.1),'Stone')
    g.asset('CC_GrassTuft')
    for i in range(8):
        a=i*2.4
        o=g.leaf('Blade',(.16*math.cos(a),.16*math.sin(a),.28),.65,.12,a,'LeafLight' if i%2 else 'Leaf')
        o.rotation_euler.x=.9+(i%3)*.2
    g.asset('CC_Bush')
    for x,y,z in [(-.35,0,.40),(.32,.15,.45),(0,-.25,.65)]:
        g.rod('Twig',(0,0,0),(x,y,z),.045)
        g.rock('Foliage',(x,y,z),(.48,.4,.45),'Leaf',1)
    g.asset('CC_WildFungus')
    for x,y,h,r in [(-.3,.1,.6,.37),(.3,.1,.85,.45),(0,-.3,.35,.24)]:
        g.rod('Stem',(x,y,0),(x,y,h),r*.25,'Cream')
        g.rock('Wild cap',(x,y,h),(r,r,r*.5),'Fungus',1)
    g.asset('CC_FallenBranch')
    g.rod('Branch',(-1,0,.15),(1,.12,.15),.17)
    g.rod('Fork',(.1,.06,.15),(.65,.6,.22),.09)
    g.rod('Cut',(-1.01,0,.15),(-.99,0,.15),.14,'CutWood')
    g.leaf('Attached leaf',(.8,.6,.26),.7,.4,.7)
