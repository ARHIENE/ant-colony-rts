"""Human SD concept. Separate scene, no edits to older character assets."""
import bpy,sys,math,importlib
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(HERE.parent/'toto-v1'))
import forms as f
importlib.reload(f)
scene=bpy.data.scenes.new('Human_SD_Studio');bpy.context.window.scene=scene
root=bpy.data.objects.new('Human_SD_Model',None);scene.collection.objects.link(root)
skin=f.material('SD_Skin',(.96,.71,.54));hair=f.material('SD_TealHair',(.13,.25,.26));hairlight=f.material('SD_HairHighlight',(.20,.34,.34))
cloth=f.material('SD_CreamLinen',(.87,.76,.57));cape=f.material('SD_Terracotta',(.48,.20,.12));trim=f.material('SD_CapeTrim',(.72,.39,.21))
leather=f.material('SD_Leather',(.20,.11,.065));gold=f.material('SD_Brass',(.66,.43,.17));dark=f.material('SD_Ink',(.035,.025,.024));white=f.material('SD_EyeWhite',(1,.95,.85));iris=f.material('SD_Iris',(.06,.18,.20));aqua=f.material('SD_IrisLight',(.27,.49,.46));blush=f.material('SD_Blush',(.97,.47,.41))
# Rounded cheek silhouette, jaw taper, front -Y. Local mesh coordinates used for face projection.
head=f.rings('Head',[(0,0,1.48,.10,.10),(0,0,1.51,.38,.28),(0,0,1.58,.63,.41),(0,0,1.73,.83,.51),(0,0,1.94,.94,.57),(0,0,2.16,.96,.59),(0,0,2.38,.90,.58),(0,0,2.58,.75,.51),(0,0,2.74,.49,.37),(0,0,2.82,.15,.15),(0,0,2.83,.02,.02)],skin,root,48)
f.oval('Neck',(0,0,1.45),(.19,.19,.20),skin,root,20,10)
f.rings('Tunic',[(0,0,.45,.57,.33),(0,0,.51,.59,.34),(0,0,.72,.51,.32),(0,0,.94,.42,.28),(0,0,1.15,.40,.27),(0,0,1.33,.46,.29),(0,0,1.43,.27,.21)],cloth,root,32)
f.rings('Belt',[(0,0,.95,.435,.293),(0,0,1.015,.425,.29)],leather,root,32)
f.oval('Buckle',(0,-.30,.985),(.085,.025,.065),gold,root,16,8)
for sign,label in [(-1,'L'),(1,'R')]:
    f.oval('Ear_'+label,(sign*.87,.0,1.94),(.15,.13,.22),skin,root,20,10)
    f.oval('Leg_'+label,(sign*.255,0,.41),(.16,.17,.29),skin,root,20,10)
    f.oval('Boot_'+label,(sign*.255,-.09,.16),(.215,.29,.16),leather,root,24,12)
    f.rings('BootCuff_'+label,[(sign*.255,0,.20,.19,.20),(sign*.255,0,.34,.19,.20)],leather,root,20)
    sleeve=f.oval('Sleeve_'+label,(sign*.51,-.015,1.10),(.24,.25,.34),cloth,root,24,12);sleeve.rotation_euler[1]=sign*.58
    palm=f.oval('Hand_'+label,(sign*.71,-.035,.91),(.13,.12,.15),skin,root,20,10)
    f.oval('Thumb_'+label,(sign*.64,-.135,.93),(.06,.065,.085),skin,root,16,8)
    for j in range(3):
        x=sign*.255+(j-1)*.055
        f.tube('BootStitch_'+label+str(j),[(x,-.29,.13),(x+.015,-.28,.20)],.008,trim,root)
# Short mantle, open in front, solid cloth thickness.
verts=[];N=32
for z,rx,ry in [(1.48,.24,.23),(1.39,.49,.32),(1.20,.67,.40),(1.05,.63,.39)]:
    for j in range(N+1):
        t=-math.pi/2+.35+j*(math.tau-.70)/N
        verts.append((rx*math.cos(t),ry*math.sin(t),z))
faces=[]
for i in range(3):
    for j in range(N):a=i*(N+1)+j;faces.append((a,a+1,a+N+2,a+N+1))
mantle=f.mesh('Capelet',verts,faces,cape,root,True)
mod=mantle.modifiers.new('Cloth thickness','SOLIDIFY');mod.thickness=.025
bpy.context.view_layer.objects.active=mantle;bpy.ops.object.modifier_apply(modifier=mod.name)
for sign in [-1,1]:
    f.tube('CapeEdge',[(sign*.08,-.225,1.48),(sign*.17,-.32,1.39),(sign*.23,-.40,1.20),(sign*.22,-.39,1.05)],.015,trim,root)
f.oval('CapeBrooch',(0,-.265,1.40),(.085,.035,.085),gold,root,20,10)
# All facial shapes are projected onto the same head surface, not spheres pasted on the face.
def patch(name,x,z,rx,rz,mat,offset=0):
    o=f.face_patch(name,head,x,z,rx,rz,mat,root);o.location.y=-offset;return o
def line(name,points,mat=dark,r=.013,offset=.035):
    out=[]
    for x,z in points:
        ok,p,n,index=head.ray_cast(Vector((x,-2,z)),Vector((0,1,0)))
        assert ok
        out.append((x,p.y-offset,z))
    return f.tube(name,out,r,mat,root)
for sign,label in [(-1,'L'),(1,'R')]:
    x=sign*.36
    patch('EyeOutline_'+label,x,2.02,.215,.265,dark)
    patch('EyeWhite_'+label,x,2.015,.192,.237,white,.016)
    patch('Iris_'+label,x+.01,1.99,.145,.20,iris,.032)
    patch('IrisGlow_'+label,x+.01,1.88,.11,.067,aqua,.048)
    patch('Pupil_'+label,x+.01,2.025,.065,.105,dark,.05)
    patch('EyeGlint_'+label,x-.044,2.10,.051,.064,white,.065)
    patch('EyeSpark_'+label,x+.075,1.955,.018,.025,white,.065)
    patch('Blush_'+label,sign*.66,1.79,.115,.064,blush,.01)
    line('Lash_'+label,[(x-.20,2.16),(x-.15,2.25),(x-.05,2.28),(x+.06,2.27),(x+.16,2.21)],r=.022)
    line('Brow_'+label,[(x-.12,2.37),(x,2.40),(x+.12,2.37)],hair,.018)
patch('Smile',0,1.725,.085,.064,cape)
patch('SmileInner',0,1.71,.059,.036,blush,.018)
# Hair cap with open face, connected curved fringe clumps and softly tapered bob silhouette.
verts=[];faces=[];S=48;R=14
for i in range(R+1):
    for j in range(S):
        a=math.tau*j/S
        front=max(0,-math.sin(a));end=2.10-1.12*front**3
        t=.015+(end-.015)*i/R
        verts.append((1.035*math.sin(t)*math.cos(a),.12+.72*math.sin(t)*math.sin(a),2.24+.77*math.cos(t)))
for i in range(R):
    for j in range(S):a=i*S+j;b=i*S+(j+1)%S;faces.append((a,b,b+S,a+S))
f.mesh('HairCap',verts,faces,hair,root,True)
locks=[('Center',[(.12,-.12,2.96,.14,.10),(.10,-.40,2.81,.23,.14),(.03,-.56,2.61,.22,.13),(-.08,-.61,2.43,.15,.10),(-.18,-.60,2.36,.02,.025)]),('Left',[(-.26,-.10,2.91,.12,.10),(-.40,-.40,2.75,.22,.14),(-.51,-.55,2.52,.20,.12),(-.59,-.57,2.32,.13,.09),(-.68,-.52,2.24,.02,.02)]),('Right',[(.36,-.08,2.90,.12,.10),(.48,-.38,2.75,.21,.14),(.58,-.52,2.55,.19,.13),(.64,-.56,2.37,.12,.09),(.75,-.49,2.30,.02,.02)])]
for name,sections in locks:f.bent_ear('Fringe_'+name,sections,hair,root,24)
for sign,label in [(-1,'L'),(1,'R')]:
    for j in range(3):
        y=.04+j*.23
        f.bent_ear('BobLock_'+label+str(j),[(sign*.78,y,2.64,.17,.16),(sign*.98,y-.08,2.36,.21,.17),(sign*1.02,y-.1,2.05,.20,.16),(sign*.99,y-.12,1.79,.15,.13),(sign*(1.12 if j==0 else .94),y-.15,1.67,.025,.025)],hair,root,20)
f.bent_ear('Ahoge',[(.08,.12,2.91,.09,.07),(.22,.12,3.11,.08,.06),(.18,.10,3.25,.07,.05),(.02,.06,3.31,.035,.03),(-.08,.03,3.26,.008,.008)],hair,root,16)
# Studio
floor=f.material('StudioFloor',(.77,.72,.64))
bpy.ops.mesh.primitive_plane_add(size=200,location=(0,0,-.008));bpy.context.object.name='StudioFloor';bpy.context.object.data.materials.append(floor)
world=bpy.data.worlds.new('HumanSDWorld');world.use_nodes=True;scene.world=world
bg=next(n for n in world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs['Color'].default_value=(.8,.85,.9,1);bg.inputs['Strength'].default_value=.5
for name,loc,power,size in [('Key',(-3,-4,6),500,4),('Fill',(4,-2,3),300,4),('Rim',(1,4,5),500,3)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.size=size
    o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1.5))-o.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('HumanSDCamera');cam=bpy.data.objects.new('HumanSDCamera',data);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(1,-8,3.1);cam.rotation_euler=(Vector((0,0,1.65))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=3.9
scene.render.resolution_x=1000;scene.render.resolution_y=1000;scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG';scene.render.filepath=str(HERE/'preview.png')
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.shading.type='MATERIAL'
bpy.ops.object.select_all(action='DESELECT');root.select_set(True);bpy.context.view_layer.objects.active=root
print('Human SD created',len(root.children_recursive))
