"""Second pass: housing, production, cold storage and infrastructure."""
import math
import geometry as g

def house():
    g.asset('CC_Dormitory')
    g.box('Raised floor',(0,0,.14),(3.2,2.6,.28),'Earth',.12)
    for x in [-1.4,1.4]:
        for y in [-1,1]: g.rod('Corner',(x,y,.1),(x,y,1.8),.1)
    g.box('Back wall',(0,1.1,.85),(2.9,.18,1.4),'Earth',.1)
    for x in [-1.4,1.4]: g.box('Side wall',(x,0,.85),(.18,2.2,1.4),'Earth',.08)
    for x in [-1,1]: g.box('Front wall',(x,-1.1,.85),(.8,.18,1.4),'Earth',.08)
    for side in [-1,1]:
        for i in range(7):
            o=g.leaf('Thatched leaf',((i-3)*.47,side*.66,1.95),1.8,.72,0,'Leaf' if i%2 else 'LeafLight')
            o.rotation_euler.x=side*-.38
    g.rod('Roof ridge',(-1.7,0,2.28),(1.7,0,2.28),.1)
    g.box('Doorstep',(0,-1.35,.13),(1.1,.5,.25),'Stone',.06)
    g.leaf('Door awning',(0,-1.3,1.55),1,.9,math.pi/2)

def workshop():
    g.asset('CC_Workshop'); g.bench(2.4,1.1,.85)
    g.box('Backboard',(0,.48,1.3),(2.2,.1,.75),'Bark',.03)
    for x in [-.7,-.25,.3,.8]:
        g.rod('Tool shaft',(x,.36,1.1),(x,.36,1.6),.035,'CutWood')
        g.box('Tool head',(x,.36,1.55),(.23,.10,.13),'Iron',.02)
    g.box('Vise base',(.7,-.28,.98),(.4,.35,.13),'Iron')
    for x in [.57,.84]: g.box('Vise jaw',(x,-.28,1.08),(.06,.3,.22),'Iron',.01)
    g.rod('Uncut twig',(-.9,-.15,.97),(.1,-.15,.97),.08)
    g.rod('Saw handle',(-.4,-.35,.97),(-.1,-.35,.97),.05,'CutWood')
    g.box('Saw blade',(-.65,-.35,.98),(.45,.12,.025),'Iron',.005)

def research():
    g.asset('CC_ResearchDesk'); g.bench(2.1,1.15,.82)
    g.box('Open journal',(-.42,-.18,.92),(.7,.63,.08),'Cream',.02)
    g.rod('Journal spine',(-.42,-.5,.98),(-.42,.12,.98),.018,'Bark',5)
    for x in [-.62,-.25]:
        for y in [-.35,-.22,-.09]: g.box('Writing',(x,y,.965),(.20,.018,.01),'Wood',0)
    g.rod('Lens stand',(.60,.25,.88),(.6,.25,1.52),.045,'Iron')
    # Wide insect-scale lens represented by an opaque polished blue face.
    g.rod('Lens rim',(.6,.18,1.5),(.6,.3,1.5),.32,'Iron',12)
    g.rod('Lens face',(.6,.165,1.5),(.6,.18,1.5),.27,'Teal',12)
    g.rock('Specimen',(.55,-.18,1.0),(.17,.13,.16),'Amber')
    g.asset('CC_AdminDesk'); g.bench(1.8,1.0,.8)
    for i in range(3): g.box('Ledger',( -.48,.1,.91+i*.07),(.55,.55,.07),'Cream')
    g.box('Map',( .32,-.03,.88),(.7,.7,.025),'Cream',.01)
    for x,y in [(.2,0),(.5,.15),(.4,-.2)]: g.rock('Map weight',(x,y,.93),(.055,.055,.06),'Amber')
    g.bottle((-.5,-.3,.9),'Iron')

def cold():
    import bpy
    for name,w,h in [('CC_Refrigerator',1.2,1.7),('CC_Freezer',1.9,1.0)]:
        g.asset(name)
        g.box('Cabinet bottom',(0,0,.17),(w,1.05,.18),'Cream',.04)
        for x in [-w/2+.08,w/2-.08]: g.box('Cabinet side',(x,0,h/2+.08),(.16,1.05,h),'Cream',.04)
        g.box('Cabinet back',(0,.45,h/2+.08),(w,.16,h),'Cream',.04)
        if 'Refrigerator' in name:
            g.box('Cabinet top',(0,0,h+.02),(w,1.05,.16),'Cream',.04)
            for z in [.62,1.10]: g.box('Shelf',(0,.02,z),(w-.3,.78,.055),'Iron',.015)
        else:
            g.box('Cabinet front',(0,-.45,h/2+.08),(w,.16,h),'Cream',.04)
        for x in [-w/2+.12,w/2-.12]:
            for y in [-.4,.4]: g.box('Foot',(x,y,.08),(.17,.17,.16),'Iron',.02)
        if 'Refrigerator' in name:
            hinge=bpy.data.objects.new('Door_Hinge',None); bpy.context.scene.collection.objects.link(hinge); hinge.parent=g.ROOT; hinge.location=(-w/2,-.58,.08)
            o=g.box('Door',(0,-.58,h/2+.08),(w-.08,.12,h-.08),'Teal',.06); o.parent=hinge; o.location-=hinge.location
            o=g.rod('Handle',(.35,-.71,.8),(.35,-.71,1.15),.04,'Iron'); o.parent=hinge; o.location-=hinge.location
        else:
            hinge=bpy.data.objects.new('Lid_Hinge',None); bpy.context.scene.collection.objects.link(hinge); hinge.parent=g.ROOT; hinge.location=(0,.53,h+.08)
            o=g.box('Lid',(0,0,h+.1),(w+.05,1.1,.16),'Teal',.06); o.parent=hinge; o.location-=hinge.location
            o=g.rod('Handle',(-.25,-.62,h+.12),(.25,-.62,h+.12),.035,'Iron'); o.parent=hinge; o.location-=hinge.location
        for z in [.25,.4,.55]: g.box('Vent',(0,.54,z),(w*.6,.035,.05),'Iron',.005)
        g.rock('Cold badge',(0,-.66,.5),(.11,.035,.14),'White')

def power():
    g.asset('CC_WoodGenerator')
    g.box('Base',(0,0,.12),(2,1.4,.24),'Stone',.06)
    g.box('Firebox',(-.4,0,.64),(1.0,1.0,.9),'Iron',.08)
    g.box('Door',(-.4,-.53,.63),(.7,.1,.55),'Bark',.04)
    for x in [-.62,-.4,-.18]: g.box('Grate',(x,-.60,.62),(.06,.04,.38),'Iron',.005)
    g.rod('Chimney',(-.65,.25,1),(-.65,.25,2.05),.16,'Iron',8)
    g.rod('Chimney rim',(-.65,.25,1.98),(-.65,.25,2.12),.20,'Iron',8)
    g.rod('Dynamo',(.2,0,.65),(.8,0,.65),.35,'Teal',10)
    g.rod('Axle',(-.1,0,.65),(.9,0,.65),.09,'Iron')
    for x in [.35,.6]: g.rod('Fuel twig',(x,-.42,.26),(x,.4,.26),.07)
    g.asset('CC_Battery')
    g.box('Housing',(0,0,.57),(.9,.7,1.05),'Teal',.08)
    for z in [.18,.94]: g.box('Band',(0,0,z),(.96,.76,.10),'Iron',.02)
    for x in [-.22,.22]: g.rod('Terminal',(x,0,1.1),(x,0,1.27),.075,'Amber' if x<0 else 'Iron')
    for z in [.42,.58,.74]: g.box('Charge segment',(0,-.365,z),(.32,.035,.08),'Amber',.01)
    g.asset('CC_Lamp')
    g.rock('Base',(0,0,.10),(.35,.3,.12),'Stone')
    g.rod('Stem',(0,0,0),(0,0,1.8),.065)
    g.rod('Arm',(0,0,1.8),(.4,0,1.8),.06)
    g.rod('Lantern',(.4,0,1.27),(.4,0,1.65),.20,'Amber',8)
    for z in [1.27,1.65]: g.rod('Rim',(.4,0,z),(.4,0,z+.05),.23,'Iron',8)
    for x in [.2,.6]: g.rod('Guard',(x,0,1.27),(x,0,1.7),.025,'Iron',5)
    g.leaf('Shade',(.4,0,1.73),.7,.6,0)

def architecture():
    g.asset('CC_SoilWall')
    for row in range(3):
        for col in range(3):
            g.box('Earth block',((col-1)*.66,0,.26+row*.49),(.64,.5,.48),'Earth',.065)
    g.asset('CC_LeafWall')
    for x in [-1,0,1]: g.rod('Frame',(x,0,0),(x,0,1.6),.06)
    for x in [-.65,0,.65]:
        o=g.leaf('Panel',(x,-.04,.82),1.7,.85,0,'Leaf' if x else 'LeafLight'); o.rotation_euler.x=math.pi/2
    for z in [.2,1.4]: g.rod('Binding rail',(-1,-.1,z),(1,-.1,z),.035,'Cream')
    g.asset('CC_DoorFrame')
    for x in [-.6,.6]: g.rod('Post',(x,0,0),(x,0,1.8),.10)
    g.rod('Lintel',(-.7,0,1.8),(.7,0,1.8),.12)
    import bpy
    hinge=bpy.data.objects.new('Door_Hinge',None); bpy.context.scene.collection.objects.link(hinge); hinge.parent=g.ROOT; hinge.location=(-.5,0,0)
    before=set(g.ROOT.children)
    for i in range(5): g.box('Door plank',((i-2)*.19,0,.85),(.18,.09,1.65),'Wood',.025)
    for z in [.35,1.35]: g.box('Strap',(0,-.065,z),(1,.05,.07),'Iron',.005)
    g.rock('Knob',(.3,-.12,.85),(.065,.065,.065),'Amber')
    for o in set(g.ROOT.children)-before: o.parent=hinge; o.location.x+=.5

def build():
    house(); workshop(); research(); cold(); power(); architecture()
