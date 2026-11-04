"""Construction stages and crop state meshes. State switching is not gameplay code."""
import math
import geometry as g

def build():
    g.asset('CC_ConstructionPlan')
    for x in [-1.4,1.4]:
        for y in [-1.4,1.4]: g.rod('Survey stake',(x,y,0),(x,y,.55),.055,'CutWood'); g.lash((x,y,.4))
    for x in [-1.4,1.4]: g.rod('String',(x,-1.4,.3),(x,1.4,.3),.012,'Cream',4)
    for y in [-1.4,1.4]: g.rod('String',(-1.4,y,.3),(1.4,y,.3),.012,'Cream',4)
    g.asset('CC_ConstructionFrame')
    for x in [-1.2,1.2]:
        for y in [-1.1,1.1]: g.rod('Frame post',(x,y,0),(x,y,1.9),.09)
    for y in [-1.1,1.1]: g.rod('Header',(-1.2,y,1.9),(1.2,y,1.9),.085)
    g.rod('Diagonal brace',(-1.2,1.1,.2),(1.2,1.1,1.9),.065)
    for x in [-.4,0,.4]: g.box('Staged plank',(x,0,.08),(.3,1.8,.12))
    for x in [-.4,.4]: g.rod('Ladder side',(x,-1.5,0),(x,-1.1,2.1),.05)
    for i in range(6): g.rod('Rung',(-.4,-1.5+i*.065,.15+i*.33),(.4,-1.5+i*.065,.15+i*.33),.04,'CutWood')
    g.asset('CC_DemolitionRubble')
    for i in range(7):
        a=i*2.4; g.rock('Broken block',(.7*math.cos(a),.6*math.sin(a),.18),(.3,.23,.2),'Earth')
    for i in range(3): g.rod('Broken timber',(-.6,i*.2-.3,.25),(.6,.3-i*.3,.15),.07)
    for name,phase in [('CC_CropFungus_Planted',0),('CC_CropFungus_Growing',1),('CC_CropFungus_Harvested',2),('CC_CropFungus_Pest',3)]:
        g.asset(name)
        for x,y,h in [(-.35,.15,.40),(.3,.3,.52),(.1,-.3,.6)]:
            if phase in [0,2]:
                g.rock('Living root',(x,y,.055),(.14,.12,.07),'Cream')
                if phase==2: g.rod('Cut stalk',(x,y,0),(x,y,.18),.07,'Cream')
            else:
                g.rod('Stem',(x,y,0),(x,y,h),.08,'Cream')
                g.rock('Cap',(x,y,h),(.24,.24,.13),'Earth' if phase==3 else 'Fungus',1)
                if phase==3:
                    for dx,dy in [(-.08,-.07),(.06,.05)]: g.rock('Disease spot',(x+dx,y+dy,h+.11),(.06,.05,.025),'Dark')
