"""Core colony facility and resource recipes; no ants or live animals."""
import math
import geometry as g

def nest():
    g.asset('CC_NestEntrance')
    # An actual open funnel, not a black disc placed on a solid mound.
    import bpy
    n=14; v=[]
    for r,z in [(2.15,0),(1.7,.48),(1.1,1.0),(.67,.93),(.53,.05)]:
        for i in range(n):
            a=i*math.tau/n; rr=r*(1+.035*math.sin(i*5)); v.append((rr*math.cos(a),rr*math.sin(a),z))
    f=[]
    for j in range(4):
        for i in range(n): f.append((j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i))
    me=bpy.data.meshes.new('Earth funnel'); me.from_pydata(v,[],f); me.update()
    o=bpy.data.objects.new('Open mound',me); bpy.context.scene.collection.objects.link(o); g.finish(o,'Open mound','Earth')
    for i in range(9):
        a=i*math.tau/9; g.rock('Rim pebble',(1.8*math.cos(a),1.8*math.sin(a),.13),(.28,.2,.18))
    for x in [-.85,.85]: g.rod('Entry post',(x,-1.3,0),(x,-1.3,1.55),.1); g.lash((x,-1.3,1.25))
    g.rod('Lintel',(-1,-1.3,1.45),(1,-1.3,1.45),.12)
    g.leaf('Leaf banner',(0,-1.35,1.6),1.1,.65,math.pi/2)

def storage():
    g.asset('CC_Storage')
    for i in range(7): g.box('Floor plank',((i-3)*.39,0,.12),(.37,2.2,.18))
    for x in [-1.2,1.2]:
        for y in [-.9,.9]: g.rod('Post',(x,y,0),(x,y,2.15),.10); g.lash((x,y,1.85))
    for z in [.55,1.1]:
        g.box('Back slat',(0,.95,z),(2.55,.1,.18))
        for x in [-1.22,1.22]: g.box('Side slat',(x,0,z),(.1,1.9,.18))
    for i in range(7):
        o=g.leaf('Roof leaf',((i-3)*.4,0,2.05),2.7,.68,0,'Leaf' if i%2 else 'LeafLight')
    g.rod('Ridge',(-1.5,0,2.23),(1.5,0,2.23),.08)
    g.box('Contents_RootMarker',(0,.85,.2),(.01,.01,.01),'Dark',0)
    # Storage exports empty; runtime stock is not baked into the building.

def farm():
    g.asset('CC_FarmTile')
    g.box('Soil',(0,0,.10),(1.9,1.9,.20),'Soil',.06)
    for x in [-.95,.95]: g.rod('Edge',(x,-1,.12),(x,1,.12),.06)
    for y in [-.95,.95]: g.rod('Edge',(-1,y,.12),(1,y,.12),.06)
    for x in [-.55,0,.55]: g.box('Furrow',(x,0,.22),(.23,1.75,.07),'Earth',.02)
    for stage,scale in [('Young',.42),('Mature',1)]:
        g.asset('CC_CropFungus_'+stage)
        for x,y,s in [(-.35,.15,.7),(.3,.3,.9),(.1,-.3,1)]:
            h=.55*s*scale; g.rod('Stem',(x,y,0),(x,y,h),.10*s*scale,'Cream')
            g.rock('Cap',(x,y,h),(.33*s*scale,.33*s*scale,.19*s*scale),'Fungus',2)

def fence():
    g.asset('CC_RanchFence')
    for x in [-1,1]:
        g.rod('Post',(x,0,0),(x,0,1.25),.09); g.lash((x,0,.9))
    for z in [.4,.9]: g.rod('Rail',(-1,0,z),(1,0,z),.055)
    for x in [-.5,0,.5]: g.rod('Twig',(x,0,.18),(x,0,1.05),.035)
    g.asset('CC_RanchGate')
    for x in [-1,1]: g.rod('Post',(x,0,0),(x,0,1.25),.11)
    import bpy
    hinge=bpy.data.objects.new('Gate_Hinge',None); bpy.context.scene.collection.objects.link(hinge); hinge.parent=g.ROOT; hinge.location=(-.86,0,0)
    before=set(g.ROOT.children)
    for z in [.35,.85]: g.rod('Gate rail',(-.86,0,z),(.86,0,z),.065)
    for x in [-.75,-.25,.25,.75]: g.rod('Gate twig',(x,0,.15),(x,0,1.02),.04)
    g.rod('Diagonal',(-.8,0,.25),(.8,0,.95),.04,'CutWood')
    for o in set(g.ROOT.children)-before:
        o.parent=hinge; o.location.x+=.86
    g.asset('CC_Feeder')
    g.box('Bottom',(0,0,.2),(1.7,.7,.15))
    for y in [-.4,.4]: g.box('Side',(0,y,.4),(1.85,.12,.5))
    for x in [-.85,.85]: g.box('End',(x,0,.4),(.12,.8,.5))
    for x in [-.6,.6]: g.box('Foot',(x,0,.08),(.22,1,.16),'Bark')

def medical():
    for name,animal in [('CC_CommanderBed',False),('CC_AnimalTreatment',True)]:
        g.asset(name); g.bench(1.6 if animal else 1.3,1.7,.55)
        g.box('Leaf mattress',(0,0,.68),(1.45 if animal else 1.15,1.55,.18),'Teal',.08)
        g.box('Head cushion',(0,.52,.81),(.9,.37,.13),'Cream',.06)
        if animal:
            for y in [-.35,.2]: g.box('Restraint strap',(0,y,.80),(1.48,.10,.035),'Cream',.01)
        else:
            g.box('Folded blanket',(0,-.35,.80),(1.17,.65,.06),'Leaf',.025)
        g.rod('Equipment pole',(.95,.5,0),(.95,.5,1.55),.045)
        g.leaf('Medical marker',(.95,.5,1.55),.45,.35,0,'Teal')
        g.bottle((.95,.5,1.08),'Amber')
    g.asset('CC_Apothecary'); g.bench(2,.95,.82)
    for x in [-.86,.86]: g.rod('Shelf post',(x,.35,.85),(x,.35,1.65),.05)
    g.box('Medicine shelf',(0,.35,1.38),(1.9,.35,.09))
    for i in range(5): g.bottle((-.65+i*.32,.35,1.44),'Teal' if i%2 else 'Amber')
    g.bowl('Mortar',(-.45,-.12,.89),.23,.20,'Stone')
    g.rod('Pestle',(-.46,-.12,.96),(-.26,-.12,1.25),.045,'Stone')
    g.leaf('Herb',( .4,-.18,.9),.6,.3,.9,'LeafLight')

def stations():
    g.asset('CC_ButcherTable'); g.bench(2.1,1.1,.8)
    g.box('Cutting slab',(0,0,.91),(1.65,.85,.12),'Stone',.07)
    g.box('Blade',(.5,.1,1.08),(.35,.055,.25),'Iron',.01)
    g.rod('Handle',(.5,.1,1.21),(.5,.1,1.48),.045,'Bark')
    g.bowl('Catch basin',(-.55,-.15,.06),.32,.23,'Wood')
    g.asset('CC_Kitchen')
    for i in range(10):
        a=math.tau*i/10; g.rock('Hearth stone',(-.55+.5*math.cos(a),.5*math.sin(a),.20),(.23,.20,.23))
    for y in [-.12,.12]: g.rod('Firewood',(-.9,y,.13),(-.2,y,.13),.07)
    g.bowl('Cooking pot',(-.55,0,.42),.47,.42,'Iron')
    for x in [.25,1.15]:
        for y in [-.4,.4]: g.rod('Leg',(x,y,0),(x,y,.8),.07)
    g.box('Prep surface',(.7,0,.83),(1.1,1,.12))
    g.leaf('Serving leaf',(.7,0,.91),.7,.5)
    g.bowl('Ingredient bowl',(.65,.25,.93),.15,.14,'Cream')
    g.asset('CC_CompostBin')
    for z in [.25,.55,.85]:
        for y in [-.65,.65]: g.box('Slat',(0,y,z),(1.5,.12,.22))
        for x in [-.7,.7]: g.box('Slat',(x,0,z),(.12,1.4,.22))
    for x in [-.7,.7]:
        for y in [-.65,.65]: g.rod('Post',(x,y,0),(x,y,1.05),.07)
    # Empty bin; compost content is a separate reusable resource mesh.
    g.asset('CC_Tombstone')
    g.rock('Foundation',(0,0,.12),(.6,.45,.17))
    g.box('Memorial stone',(0,.10,.65),(.7,.28,1.1),'Stone',.13)
    g.leaf('Carved leaf',(0,-.06,.67),.40,.22,0,'Cream').rotation_euler.x=math.pi/2
    g.leaf('Offering',(.24,-.32,.16),.45,.22,.8)

def resources():
    g.asset('CC_ResourceWood')
    for x,z in [(-.15,.12),(.15,.12),(0,.33)]:
        g.rod('Branch',(x,-.48,z),(x,.48,z),.13,'Bark')
        g.rod('Cut end',(x,-.49,z),(x,-.47,z),.105,'CutWood')
    g.asset('CC_ResourceStone')
    for x,y,z,s in [(-.2,0,.18,.28),(.2,.05,.15,.24),(0,.05,.38,.22)]: g.rock('Stone',(x,y,z),(s,s*.8,s*.65))
    g.asset('CC_ResourceLeaf')
    for i in range(4): g.leaf('Leaf',(0,0,.03+i*.06),.95,.5,i*.4,'Leaf' if i%2 else 'LeafLight')
    g.asset('CC_ResourceFiber')
    for i in range(6): g.rod('Fiber',(-.35,(i-2.5)*.07,.08),(.35,(i-2.5)*.07,.08),.035,'Cream',5)
    g.box('Tie',(0,0,.1),(.09,.47,.12),'Bark',.01)
    g.asset('CC_ResourceGrain')
    for i in range(7):
        a=i*2.4; g.rock('Seed',(.22*math.cos(a),.22*math.sin(a),.12+(.1 if i==6 else 0)),(.13,.09,.12),'Amber')
    g.asset('CC_ResourceCompost')
    g.rock('Compost',(0,0,.18),(.46,.38,.23),'Soil',2)
    g.leaf('Scrap',(.1,0,.35),.3,.17,.8,'Leaf')

def build():
    nest(); storage(); farm(); fence(); medical(); stations(); resources()
