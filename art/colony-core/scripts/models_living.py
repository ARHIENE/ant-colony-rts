"""Colony-scale hygiene, meals and recreation furniture."""
import math
import geometry as g

def build():
    g.asset('CC_Toilet')
    for x in [-.42,.42]:
        for y in [-.4,.4]: g.rod('Leg',(x,y,0),(x,y,.55),.075)
    g.bowl('Hollow seat',(0,0,.3),.46,.3,'CutWood')
    g.box('Back panel',(0,.48,.82),(1,.12,1.2),'Wood',.06)
    g.box('Paper holder',(.59,.1,.5),(.13,.3,.12),'Bark',.02)
    g.leaf('Leaf paper',(.6,.02,.62),.4,.22,0,'Cream')
    g.asset('CC_Washbasin')
    for x in [-.35,.35]:
        for y in [-.25,.25]: g.rod('Leg',(x,y,0),(x,y,.75),.055)
    g.box('Counter',(0,0,.72),(1,.8,.1),'Wood')
    g.bowl('Basin',(0,0,.78),.38,.23,'Stone')
    g.rod('Tap riser',(0,.36,.72),(0,.36,1.18),.035,'Iron')
    g.rod('Tap spout',(0,.36,1.18),(0,.07,1.18),.035,'Iron')
    g.rod('Tap outlet',(0,.07,1.18),(0,.07,1.1),.035,'Iron')
    g.asset('CC_Shower')
    for i in range(6): g.box('Drain slat',((i-2.5)*.19,0,.06),(.16,1.1,.12),'Wood',.015)
    g.rod('Supply',(0,.44,0),(0,.44,2.0),.045,'Iron')
    g.rod('Arm',(0,.44,2),(0,-.15,2),.045,'Iron')
    g.rod('Shower head',(0,-.15,1.88),(0,-.15,1.97),.19,'Iron',10)
    for x in [-.08,0,.08]: g.rock('Nozzle',(x,-.15,1.87),(.025,.025,.018),'Dark')
    g.rod('Valve',(-.1,.44,1),(.1,.44,1),.035,'Teal')
    g.asset('CC_DiningTable'); g.bench(2.2,1.15,.8)
    # Empty tabletop is intentional: dishes are separate inventory props.
    g.asset('CC_Chair')
    for x in [-.24,.24]:
        for y in [-.24,.24]: g.rod('Leg',(x,y,0),(x,y,.5 if y<0 else 1.0),.05)
    g.box('Seat',(0,0,.48),(.65,.65,.1),'CutWood')
    for z in [.75,.95]: g.box('Backrest',(0,.26,z),(.65,.08,.13),'Wood',.02)
    g.asset('CC_RestBench'); g.bench(1.8,.6,.48)
    for x in [-.75,.75]: g.rod('Back post',(x,.22,.25),(x,.22,1.1),.06)
    for z in [.8,1.0]: g.box('Back slat',(0,.24,z),(1.9,.12,.15),'Wood')
    g.asset('CC_SleepingMat')
    for i in range(7): g.box('Woven strip',((i-3)*.14,0,.04),(.13,1.65,.08),'Cream',.01)
    for y in [-.7,0,.7]: g.box('Binding',(0,y,.09),(1,.035,.02),'Bark',.005)
    g.rock('Pillow',(0,.52,.15),(.36,.2,.12),'LeafLight',1)
    g.asset('CC_Campfire')
    for i in range(9):
        a=i*math.tau/9; g.rock('Stone ring',(.6*math.cos(a),.6*math.sin(a),.13),(.2,.17,.15),'Stone')
    for i in range(3):
        a=i*math.pi/3; g.rod('Firewood',(-.4*math.cos(a),-.4*math.sin(a),.13),(.4*math.cos(a),.4*math.sin(a),.17),.10)
    # No baked flame: the gameplay fire effect can be enabled independently.
    g.asset('CC_Bookshelf')
    for x in [-.65,.65]: g.box('Side',(x,0,.85),(.12,.5,1.7),'Wood')
    for z in [.1,.6,1.1,1.65]: g.box('Shelf',(0,0,z),(1.4,.5,.1),'Wood')
    for row in range(3):
        for i in range(5): g.box('Bound volume',(-.45+i*.21,0,.32+row*.5),(.15,.32,.35),'Cream' if i%2 else 'Teal',.015)
