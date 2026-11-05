"""Four small hand-built woodland facilities; preserve production work-point empties."""
import bpy,math,random,json,shutil
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent;ROOT=OUT.parents[1]
rng=random.Random(1105)
def mat(name,col):
    m=bpy.data.materials.new('Story_'+name);m.use_nodes=True;m.diffuse_color=(*col,1)
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.9
    return m
clay=mat('OchrePlaster',(.56,.37,.22));plaster=mat('CreamPlaster',(.72,.61,.42))
wood=mat('Oak',(.22,.115,.055));endgrain=mat('CutWood',(.37,.23,.11));dark=mat('Recess',(.058,.043,.025))
stone=mat('Fieldstone',(.31,.32,.25));sage=mat('LeafShutter',(.25,.35,.17));linen=mat('GrainSack',(.58,.49,.31))
thatch=[mat('Straw'+str(i),(.38+i*.025,.245+i*.02,.10+i*.011)) for i in range(6)]
leaf=[mat('Leaf'+str(i),(.17+i*.018,.26+i*.022,.105+i*.009)) for i in range(5)]
made=[]
def finish(o,name,material):
    o.name=name;o.data.materials.append(material);o.parent=root;made.append(o);return o
def box(name,pos,scale,material,bevel=.035):
    bpy.ops.mesh.primitive_cube_add(size=1,location=pos);o=bpy.context.object;o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    if bevel:
        m=o.modifiers.new('Soft hand-cut edge','BEVEL');m.width=bevel;m.segments=2
        bpy.ops.object.modifier_apply(modifier=m.name)
    return finish(o,name,material)
def oval(name,pos,scale,material):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,location=pos);o=bpy.context.object;o.scale=scale
    for p in o.data.polygons:p.use_smooth=True
    return finish(o,name,material)
def beam(name,a,b,r,material):
    a,b=Vector(a),Vector(b);bpy.ops.mesh.primitive_cylinder_add(vertices=10,radius=r,depth=(b-a).length,location=(a+b)*.5)
    o=bpy.context.object;o.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler();return finish(o,name,material)
def straw_roof(width,depth,eave,rise,palette):
    # Overlapping slightly bowed shingles with varied lengths and warm local tones.
    n=18;pitch=depth/n
    for side in [-1,1]:
        for tier in range(3):
            t0=tier/3;t1=(tier+1)/3+.07
            for j in range(n):
                y=-depth/2+(j+.5)*pitch+rng.uniform(-.016,.016)
                a=(side*width/2*t0,y,eave+rise*(1-t0)+.045)
                b=(side*width/2*t1,y,eave+rise*(1-t1)+rng.uniform(-.025,.025))
                d=Vector(b)-Vector(a);o=box('Overlapping straw', (Vector(a)+Vector(b))*.5,(d.length+.08,pitch*1.13,.13),palette[(j+tier*2)%len(palette)],.055)
                o.rotation_euler[1]=-math.atan2(d.z,d.x) if side==1 else math.atan2(d.z,-d.x)
    beam('Ridge timber',(0,-depth/2-.06,eave+rise+.10),(0,depth/2+.06,eave+rise+.10),.105,wood)
    for y in [-depth/2,depth/2]:
        for s in [-1,1]:beam('Barge board',(0,y,eave+rise),(s*width*.5,y,eave),.09,wood)
def foundation(w,d):
    for side in [-1,1]:
        for j in range(8):oval('Rounded fieldstone',(side*w/2,-d/2+(j+.5)*d/8,.12),(.16,.23,.13),stone)
    for side in [-1,1]:
        for j in range(8):oval('Rounded fieldstone',(-w/2+(j+.5)*w/8,side*d/2,.12),(.24,.16,.13),stone)
def doorway(y):
    box('Door recess',(0,y,.92),(.85,.05,1.6),dark,.18)
    for j in range(5):box('Door oak plank',(-.32+j*.16,y-.04,.91),(.15,.065,1.43),endgrain,.035)
    for s in [-1,1]:beam('Door jamb',(s*.46,y-.08,.12),(s*.46,y-.08,1.72),.075,wood)
    beam('Lintel',(-.46,y-.08,1.72),(.46,y-.08,1.72),.09,wood)
    beam('Door crossbar',(-.34,y-.085,.55),(.34,y-.085,.55),.035,wood)
    oval('Latch',(.24,y-.13,.96),(.055,.035,.045),wood)
def window(x,y,z):
    box('Window recess',(x,y,z),(.63,.06,.65),dark,.16)
    for s in [-1,1]:box('Sage shutter',(x+s*.33,y-.07,z),(.23,.1,.7),sage,.04)
    beam('Window mullion',(x,y-.07,z-.28),(x,y-.07,z+.28),.025,endgrain)
    box('Window sill',(x,y-.16,z-.34),(.87,.31,.09),wood)
    box('Herb windowbox',(x,y-.19,z-.48),(.65,.29,.22),endgrain)
    for j in range(5):oval('Herbs',(x-.24+j*.12,y-.19,z-.29),(.09,.09,.17),sage)

validation=[]
for name in ['CC_Hut','CC_House','CC_Storage','CC_Watchtower']:
    scene=bpy.data.scenes.new('Story_'+name);bpy.context.window.scene=scene;made=[]
    source=ROOT/'Assets/Resources/Facilities'/ (name+'.fbx');backup=OUT/('original-'+name+'.fbx')
    if not backup.exists():shutil.copy2(source,backup)
    bpy.ops.import_scene.fbx(filepath=str(backup))
    root=next(o for o in scene.objects if o.type=='EMPTY' and '__' not in o.name)
    workpoints={o.name:list(o.matrix_world.translation) for o in scene.objects if o.type=='EMPTY' and '__' in o.name}
    for o in list(scene.objects):
        if o.type=='MESH':bpy.data.objects.remove(o,do_unlink=True)
    if name in ['CC_Hut','CC_House']:
        house=name=='CC_House';w=3.4;d=2.9;h=2.35 if house else 2.05
        foundation(w,d);box('Hand-trowelled walls',(0,0,h/2),(w,d,h),plaster if house else clay,.16)
        for x in [-w/2,w/2]:
            for y in [-d/2,d/2]:beam('Oak corner post',(x,y,.12),(x,y,h+.05),.11,wood)
        for y in [-d/2-.02,d/2+.02]:beam('Wall plate',(-w/2,y,h-.06),(w/2,y,h-.06),.08,wood)
        doorway(-d/2-.02);window(1.08,-d/2-.04,1.4)
        rise=1.18 if house else 1.1
        vertices=[(x,y,z) for y in [-d/2,d/2] for x,z in [(-w/2,h),(w/2,h),(0,h+rise)]]
        me=bpy.data.meshes.new('Gable');me.from_pydata(vertices,[],[(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)]);me.update()
        o=bpy.data.objects.new('Plaster gable',me);scene.collection.objects.link(o);finish(o,o.name,plaster if house else clay)
        straw_roof(4.3,3.75,h,1.18 if house else 1.1,leaf if house else thatch)
        if house:
            box('Clay chimney',(-1,.6,3.05),(.53,.55,1.42),clay,.08)
            box('Chimney rim',(-1,.6,3.77),(.66,.68,.18),stone,.04)
            box('Chimney opening',(-1,.6,3.87),(.39,.41,.012),dark,.03)
        for j in range(3):box('Doorstep',(0,-1.68-j*.17,.07),(.9-j*.1,.27,.14),stone,.045)
        for j in range(3):beam('Stacked firewood',(-1.4+j*.23,-1.65,.20),(-1.4+j*.23,-1.97,.20),.1,endgrain)
    elif name=='CC_Storage':
        foundation(3.4,2.8)
        for j in range(12):box('Raised floor plank',(-1.61+j*.293,0,.2),(.28,2.9,.17),endgrain,.025)
        for x in [-1.65,1.65]:
            for y in [-1.33,1.33]:beam('Warehouse post',(x,y,.15),(x,y,2.18),.11,wood)
        for x in [-1.65,1.65]:
            for j in range(7):box('Slatted side',(x,0,.45+j*.23),(.13,2.8,.19),endgrain,.025)
        for j in range(12):box('Back board',(-1.6+j*.29,1.33,1.1),(.27,.13,1.9),wood,.02)
        straw_roof(4.15,3.55,2.16,.95,thatch)
        for x,y,z in [(-1,-.65,.62),(-.4,-.6,.6),(-.7,-.4,1.08),(1,.5,.6)]:
            oval('Grain sack',(x,y,z),(.32,.29,.43),linen);beam('Sack tied neck',(x-.08,y,z+.38),(x+.08,y,z+.38),.035,wood)
        for x in [.55,1.1]:
            box('Storage crate',(x,-.65,.53),(.48,.55,.56),endgrain,.03)
            for z in [.31,.7]:box('Crate band',(x,-.94,z),(.51,.05,.06),wood,.01)
        box('Carved stock sign',(0,-1.47,1.96),(.95,.14,.32),sage,.05)
        for j in [-1,0,1]:beam('Wheat sign',(j*.12,-1.56,1.87),(j*.12,-1.56,2.06),.017,linen)
    else:
        for x in [-.98,.98]:
            for y in [-.98,.98]:
                beam('Tower pillar',(x*1.13,y*1.13,.05),(x,y,3.8),.14,wood)
                oval('Foot stone',(x*1.13,y*1.13,.13),(.28,.26,.15),stone)
        for y in [-1,1]:
            beam('Diagonal brace',(-1.06,y,1.0),(1,y,2.8),.075,endgrain)
        for j in range(9):box('Lookout plank',(-1.23+j*.307,0,3.05),(.3,2.65,.18),endgrain,.025)
        for y in [-1.2,1.2]:
            for z in [3.28,3.65]:beam('Safety rail',(-1.2,y,z),(1.2,y,z),.055,wood)
        for x in [-1.2,1.2]:beam('Side rail',(x,-1.2,3.65),(x,1.2,3.65),.055,wood)
        for x in [-.38,.38]:beam('Ladder stile',(x,-1.49,.05),(x,-1.12,3.06),.055,wood)
        for j in range(11):beam('Ladder rung',(-.38,-1.49+j*.034,.22+j*.255),(.38,-1.49+j*.034,.22+j*.255),.035,endgrain)
        straw_roof(3.12,3.1,4.15,.98,leaf)
        for x in [-.98,.98]:
            for y in [-.98,.98]:beam('Roof post',(x,y,3),(x,y,4.25),.08,wood)
    bpy.ops.object.select_all(action='DESELECT')
    for o in made:o.select_set(True)
    bpy.context.view_layer.objects.active=made[0];bpy.ops.object.join();made=[bpy.context.object];made[0].name=name+'_StoryMesh'
    bpy.ops.object.select_all(action='DESELECT')
    for o in scene.objects:o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'MESH','EMPTY'},bake_anim=False,axis_forward='-Z',axis_up='Y')
    assert workpoints=={o.name:list(o.matrix_world.translation) for o in scene.objects if o.type=='EMPTY' and '__' in o.name}
    validation.append({'name':name,'meshes':len(made),'workpoints_unchanged':True,'vertices':sum(len(o.data.vertices) for o in made)})
(OUT/'building-validation.json').write_text(json.dumps(validation,indent=2))
print(json.dumps(validation))
