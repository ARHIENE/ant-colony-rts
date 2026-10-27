"""Missing resource props, using the production kit's mesh/material/export conventions."""
import bpy,sys,math,json,shutil
from pathlib import Path
from mathutils import Matrix,Vector
root=Path('E:/Git/ant');kit=root/'art/production-kit-20261019'
sys.path.insert(0,str(kit/'scripts'));import pk
pk.new_file();col=pk.collection('ResourceFill1028');objects=[]
out=kit/'exports/resource-fill-1028';out.mkdir(parents=True,exist_ok=True)
runtime=root/'Assets/Resources/ResourceModels'
def shell():
 m=pk.Mesh('ShellPlate');m.ball((0,0,.07),(.22,.31,.09),'#806049',seg=12,rings=6,smooth=False)
 for y in [-.16,-.05,.07,.18]:
  points=[]
  for i in range(7):
   x=-.16+i*.32/6;z=.07+.092*math.sqrt(max(0,1-(x/.22)**2-(y/.31)**2));points.append((x,y,z))
  for a,b in zip(points,points[1:]):m.cyl(a,b,.007,'#4e3d31',seg=5)
 return m
def add_shell(m,c,angle,scale=1):m.merge(shell(),Matrix.Translation(c)@Matrix.Rotation(angle,4,'Z')@Matrix.Diagonal((scale,scale,scale,1)))
def export(m):
 ob=m.to_object(col);objects.append(ob)
 for o in bpy.context.selected_objects:o.select_set(False)
 ob.select_set(True);bpy.context.view_layer.objects.active=ob
 dest=out/(ob.name+'.fbx');bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y')
 shutil.copy2(dest,runtime/dest.name)
 return ob
m=pk.Mesh('Mat_Chitin')
for c,a in [((-.12,-.1,0),-.35),((.13,.02,.04),.4),((-.02,.16,.09),1.4)]:add_shell(m,c,a)
export(m)
m=pk.Mesh('Deposit_Chitin')
for c,s in [((-.25,0,.12),(.5,.35,.2)),((.25,.1,.1),(.4,.4,.2)),((.02,-.25,.09),(.4,.3,.18))]:m.ico(c,s,'#66615a',sub=1)
for c,a in [((-.27,-.05,.19),-.3),((.3,.06,.22),.7),((0,-.22,.25),1.8),((-.03,.23,.3),.4),((.14,-.03,.4),-.8)]:add_shell(m,c,a,1.3)
export(m)
m=pk.Mesh('Mat_Wax');poly=[(.36*math.cos(i*math.pi/3),.36*math.sin(i*math.pi/3)) for i in range(6)];m.prism(poly,0,.045,'#b88c3e')
for x,y in [(0,0)]+[(.2*math.cos(i*math.pi/3),.2*math.sin(i*math.pi/3)) for i in range(6)]:m.torus((x,y,.085),.096,.022,'#dfbb68',seg=6,tseg=4)
export(m)
m=pk.Mesh('Part_Component');m.box((0,0,.025),(.46,.29,.05),'#687681')
for x,y,z,r,n in [(-.1,0,.09,.12,12),(.14,.07,.13,.085,10)]:
 m.torus((x,y,z),r,.027,'#b79a62',seg=n,tseg=4)
 for i in range(n):
  a=i*2*math.pi/n;m.box((x+(r+.027)*math.cos(a),y+(r+.027)*math.sin(a),z),(.055,.032,.048),'#9ca6ac',rot=(0,0,a))
for x,y in [(-.18,-.1),(.18,-.1)]:m.cyl((x,y,.05),(x,y,.1),.025,'#c2c7c8',seg=6)
export(m)
m=pk.Mesh('Water_Jug');m.lathe([(.13,0),(.19,.035),(.2,.37),(.15,.45),(.07,.49),(.07,.53)],'#527b95',seg=12,sx=1,sy=.65,smooth=False)
m.cyl((0,0,.53),(0,0,.58),.085,'#9da7aa',seg=12);m.torus((.16,0,.43),.12,.024,'#78868d',seg=10,tseg=5,rot=(math.pi/2,0,0))
frame=Matrix(((1,0,0,0),(0,0,-1,-.132),(0,1,0,0),(0,0,0,1)))
m.prism([(0,.34),(-.047,.26),(-.032,.21),(.032,.21),(.047,.26)],0,.004,'#d8e6e9',M=frame)
export(m)
m=pk.Mesh('Resource_Soil')
for c,s,color in [((0,0,.1),(.38,.28,.16),'#79614b'),((-.19,-.05,.06),(.21,.18,.1),'#8c7257'),((.2,.04,.06),(.2,.2,.1),'#6a5745'),((-.05,.18,.06),(.23,.15,.1),'#8c7257')]:m.ico(c,s,color,sub=1)
for x,y in [(-.15,-.12),(.16,.11),(.05,-.2)]:m.ico((x,y,.08),(.045,.04,.03),'#a39a87',sub=1)
export(m)
audit=[]
for i,ob in enumerate(objects):
 assert len(ob.data.vertices)>20 and all(math.isfinite(v) for p in ob.data.vertices for v in p.co)
 ob.location=((i%3)*1.45,(i//3)*1.55,0)
 audit.append({'name':ob.name,'vertices':len(ob.data.vertices),'faces':len(ob.data.polygons)})
bpy.ops.wm.save_as_mainfile(filepath=str(kit/'resource-fill-1028.blend'))
(out/'audit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
scene=bpy.context.scene;scene.world=bpy.data.worlds.new('ResourceReviewWorld');scene.world.color=(.14,.14,.14)
for name,loc,power,size in [('Key',(1,-4,6),650,5),('Fill',(-3,1,4),400,4)]:
 data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;o=bpy.data.objects.new(name,data);scene.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((1.2,.6,0))-o.location).to_track_quat('-Z','Y').to_euler()
camdata=bpy.data.cameras.new('ReviewCamera');cam=bpy.data.objects.new('ReviewCamera',camdata);scene.collection.objects.link(cam);cam.location=(4,-6,5);cam.rotation_euler=(Vector((1.3,.7,.1))-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=5.1;scene.camera=cam
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.render.filepath=str(out/'review.png');bpy.ops.render.render(write_still=True)
print('RESOURCE_FILL_1028 '+json.dumps(audit),flush=True)
