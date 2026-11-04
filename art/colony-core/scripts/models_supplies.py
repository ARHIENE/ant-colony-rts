"""Dropped and carried goods; individual stock objects, not baked inventory."""
import math
import geometry as g

def build():
    g.asset('CC_ResourcePlanks')
    for i in range(4): g.box('Plank',((i%2-.5)*.3,0,.06+(i//2)*.12),(.27,1,.1),'CutWood',.018)
    for y in [-.28,.28]: g.box('Binding',(0,y,.15),(.65,.055,.32),'Cream',.01)
    g.asset('CC_ResourceOre')
    for x,y,z in [(-.18,0,.15),(.17,.1,.17),(0,-.15,.18)]:
        g.rock('Ore',(x,y,z),(.23,.17,.2),'Iron')
        g.rock('Facet',(x,y-.12,z+.05),(.07,.04,.09),'Amber')
    g.asset('CC_ResourceSilk')
    g.rod('Spindle',(0,0,0),(0,0,.55),.07,'Wood')
    for z in [.05,.5]: g.rod('Spool rim',(0,0,z),(0,0,z+.04),.27,'CutWood',10)
    for i in range(8): g.rod('Silk wrap',(0,0,.1+i*.047),(0,0,.14+i*.047),.22,'White',10)
    for name,mat in [('CC_FeedPlant','LeafLight'),('CC_FeedMeat','Fungus')]:
        g.asset(name); g.leaf('Wrapping',(0,0,.03),.8,.65,0)
        for i in range(5):
            a=i*2.4; g.rock('Feed piece',(.20*math.cos(a),.2*math.sin(a),.18),(.14,.1,.12),mat)
    for name,mat in [('CC_MealSimple','Cream'),('CC_MealGood','Amber'),('CC_FoodRotten','Soil')]:
        g.asset(name); g.bowl('Serving dish',(0,0,0),.38,.13,'Wood')
        for i in range(6):
            a=i*2.4; g.rock('Food',(.2*math.cos(a),.2*math.sin(a),.16),(.15,.11,.1),mat)
        if name=='CC_MealGood':
            g.leaf('Garnish',(0,0,.26),.3,.15,.8,'LeafLight')
            g.rock('Protein portion',(0,0,.23),(.16,.13,.09),'Fungus')
        if name=='CC_FoodRotten':
            for x in [-.1,.1]: g.rock('Mold',(x,-.1,.24),(.09,.07,.04),'Leaf')
    for name,mat,count in [('CC_MedicineHerbal','Leaf',1),('CC_MedicineStandard','Teal',2),('CC_MedicineAdvanced','Amber',3),('CC_MedicineAnimal','Fungus',1)]:
        g.asset(name)
        for i in range(count): g.bottle(((i-(count-1)/2)*.23,0,0),mat)
        if name=='CC_MedicineHerbal': g.leaf('Herb',(0,-.2,.04),.45,.2,.8)
        if name=='CC_MedicineAnimal': g.box('Animal label',(0,-.1,.15),(.12,.025,.11),'Cream',.008)
    g.asset('CC_SurgeryKit')
    g.box('Case',(0,0,.12),(.65,.45,.24),'Teal',.045)
    for x in [-.2,.2]: g.box('Latch',(x,-.235,.13),(.08,.04,.1),'Iron',.01)
    g.rod('Handle',(-.12,0,.3),(.12,0,.3),.03,'Iron')
    g.asset('CC_Manure')
    for x,y,z,s in [(-.16,0,.09,.15),(.15,0,.10,.16),(0,.1,.20,.13)]: g.rock('Dropping',(x,y,z),(s,s*.8,s*.65),'Earth',1)
