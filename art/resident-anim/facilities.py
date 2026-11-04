"""Replace ant previews with resident rigs in independent facility scenes."""
import bpy,sys,json,math
from pathlib import Path
from mathutils import Vector,Matrix
HERE=Path(__file__).resolve().parent;sys.path.insert(0,str(HERE));import rig,clips
BASE=Path('E:/Git/ant/art/colony-core')
spec=json.loads((BASE/'interaction_points.json').read_text(encoding='utf-8'))
library=bpy.data.scenes['Resident_Animation_Library'];world=library.world
def assign(actor,clip):
    actor.animation_data_create();act=bpy.data.actions['Resident|'+clip];actor.animation_data.action=act
    if act.slots:actor.animation_data.action_slot=act.slots[0]
def clone_actor(scene,source,name):
    actor=source.copy();actor.data=source.data;scene.collection.objects.link(actor);actor.name=name;actor.animation_data_clear();actor.location=(0,0,0)
    for orig in source.children:
        o=orig.copy();scene.collection.objects.link(o);o.parent=actor
        for m in o.modifiers:
            if m.type=='ARMATURE':m.object=actor
        for c in o.constraints:
            if hasattr(c,'target') and c.target==source:c.target=actor
    return actor
def bounds(root):
    inv=root.matrix_world.inverted();pts=[inv@(o.matrix_world@Vector(c)) for o in root.children_recursive if o.type=='MESH' for c in o.bound_box]
    return Vector([min(v[i] for v in pts) for i in range(3)]),Vector([max(v[i] for v in pts) for i in range(3)])
def camera(scene,objs):
    bpy.context.view_layer.update();pts=[o.matrix_world@Vector(v) for o in objs if o.type=='MESH' for v in o.bound_box]
    lo=Vector([min(p[i] for p in pts) for i in range(3)]);hi=Vector([max(p[i] for p in pts) for i in range(3)]);center=(lo+hi)/2
    cam=bpy.data.objects.new('FacilityCamera',bpy.data.cameras.new('FacilityCamera'));scene.collection.objects.link(cam);scene.camera=cam
    d=Vector((1.2,-1.8,1.2)).normalized();cam.location=center+d*20;cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=max(hi.x-lo.x,hi.y-lo.y,hi.z-lo.z)*1.55+.3
def attach_tool(scene,actor,source):
    meshes=[o for o in source.children_recursive if o.type=='MESH']
    if not meshes:return []
    pts=[source.matrix_world.inverted()@o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    length=max(p.z for p in pts)-min(p.z for p in pts);scale=.62/max(length,.001)
    result=[]
    # Geometry in rig-local rest space, weighted to the hand. Never ant bone parenting.
    orient=Matrix.Rotation(math.radians(35),4,'X')
    xf=Matrix.Translation(Vector((.43,-.50,.63)))@orient@Matrix.Scale(scale,4)
    for original in meshes:
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=actor;o.matrix_local=Matrix.Identity(4);o.animation_data_clear()
        trans=xf@source.matrix_world.inverted()@original.matrix_world
        for v in o.data.vertices:v.co=trans@v.co
        for g in list(o.vertex_groups):o.vertex_groups.remove(g)
        g=o.vertex_groups.new(name='Hand.R');g.add(list(range(len(o.data.vertices))),1,'REPLACE')
        m=o.modifiers.new('Hand tool deform','ARMATURE');m.object=actor;o['resident_tool']=source.name;result.append(o)
    return result
# Load actual tool geometry, not only its empty roots.
with bpy.data.libraries.load('E:/Git/ant/art/ant-anim/ant-tools.blend') as (src,dst):dst.objects=list(src.objects)
tools={o.name.split('.')[0]:o for o in dst.objects if o and o.name.startswith('Tool_')}
# 24 appearance rigs share exactly the same bone layout and action set.
gallery=bpy.data.scenes.new('Resident_24_Rigged');bpy.context.window.scene=gallery;gallery.world=world
actors=[]
for n in range(1,25):
    actor=rig.make(gallery,bpy.data.objects['Resident_%02d'%n],'ResidentRig_%02d_Library'%n)
    assign(actor,'Idle');actor.location=((n-1)%6*2.6,0,(3-(n-1)//6)*2.8);actors.append(actor)
gallery.frame_end=49;gallery.render.fps=24
mapping={};overrides={'CC_Kitchen':'Stir','CC_Apothecary':'Grind','CC_ButcherTable':'Saw','CC_AdminDesk':'Stamp','CC_Washbasin':'Wash'}
for index,(rid,info) in enumerate(spec.items()):
    root=bpy.data.objects[rid]
    scene=bpy.data.scenes.new('Work__'+rid);bpy.context.window.scene=scene;scene.world=world;scene.render.fps=24;scene.frame_end=49
    for o in [root,*root.children_recursive]:scene.collection.objects.link(o)
    root.location=(0,0,0);bpy.context.view_layer.update();lo,hi=bounds(root)
    clip=overrides.get(rid,info['clip']);actor=None
    if clip:
        actor=clone_actor(scene,actors[index%24],rid+'__Resident');assign(actor,clip)
        size=.65
        # Standing hand height tracks the surface for tabletop tasks.
        if clip in ('Hammer','BenchWork','Stir','Grind','Saw','Stamp','Wash','Operate','Treat'):size=max(.55,min(1.25,hi.z/.80))
        actor.scale=(size,)*3;actor.rotation_euler=(0,0,math.pi)
        wp=info['points'].get('WorkPoint_0',{}).get('pos',[0,float(lo.y-.5),0])
        actor.location=(wp[0],min(wp[1],lo.y-size*.45),0)
        if rid=='CC_Shower':actor.location=(0,0,0)
        if clip in ('Sleep','Sit','Rest') and 'SeatPoint' in info['points']:
            pos=info['points']['SeatPoint']['pos'];actor.location=(pos[0],pos[1],pos[2]-.20*size);actor.rotation_euler.z=0
        if clip=='Carry':
            actor.location=(0,0,0);actor.rotation_euler.z=0
            span=max(hi-lo);sc=min(1,.45/max(span,.01));root.scale=(sc,)*3
            # Follow body carry socket with a maintained two-hand forward offset.
            root.parent=actor;root.parent_type='BONE';root.parent_bone='Body';root.matrix_world=actor.matrix_world@Matrix.Translation((0,-.68,.58))@Matrix.Scale(sc,4)
        if info['tool']:attach_tool(scene,actor,tools[info['tool']])
        scene.frame_end=clips.SPECS[clip][0]+1
        actor['work_clip']=clip;actor['facility']=rid
    scene.frame_set(1);bpy.context.view_layer.update()
    visible=[root,*root.children_recursive]+([actor,*actor.children_recursive] if actor else [])
    camera(scene,visible)
    scene.render.resolution_x=480;scene.render.resolution_y=480;scene.render.resolution_percentage=100
    mapping[rid]={'scene':scene.name,'clip':clip,'tool':info['tool'],'actor':actor.name if actor else None,'scale':float(actor.scale.x) if actor else None,'work_position':list(actor.location) if actor else None,'original_points':info['points'],'hinges_preserved':info['hinges'],'status':'Blender preview; gameplay not connected'}
    if index%15==0:print('facility',index+1,rid)
(HERE/'facility-map.json').write_text(json.dumps(mapping,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.context.window.scene=bpy.data.scenes['Work__CC_Workshop']
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'resident-interactions.blend'))
print('24 rigged residents;',len(mapping),'facility scenes')
