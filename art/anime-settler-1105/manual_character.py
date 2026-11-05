"""Directly authored mesh study. Not a production-rigged character yet."""
import bpy, bmesh, math
from mathutils import Vector
from pathlib import Path
P=Path(__file__).parent
s=bpy.data.scenes.new('Direct_Anime_Study');bpy.context.window.scene=s
def mat(name,c,rough=.75):
 m=bpy.data.materials.new('Anime_'+name);m.diffuse_color=(*c,1);m.use_nodes=True
 n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(*c,1);n.inputs['Roughness'].default_value=rough
 return m
skin=mat('Skin',(.80,.53,.40));hair=mat('Silver',(.68,.62,.65));hl=mat('SilverHighlight',(.81,.76,.78));ink=mat('Lashes',(.065,.032,.044));white=mat('Sclera',(.94,.88,.81));iris=mat('Iris',(.26,.10,.16));gold=mat('Amber',(.66,.33,.055),.3);green=mat('Sage',(.24,.29,.16));linen=mat('Linen',(.72,.63,.44));leather=mat('Leather',(.22,.105,.055));rose=mat('Lip',(.58,.26,.23))
def mesh(name,vs,fs,ma,sub=0):
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],fs);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(ma)
 bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 for p in me.polygons:p.use_smooth=True
 if sub:mod=o.modifiers.new('Surface flow','SUBSURF');mod.levels=sub
 return o
def loft(name,rings,ma,n=32,sub=1):
 # (center x,y,z, radius x, radius y); continuously connected quad strips.
 vs=[(x+rx*math.cos(2*math.pi*i/n),y+ry*math.sin(2*math.pi*i/n),z) for x,y,z,rx,ry in rings for i in range(n)]
 fs=[(j*n+i,j*n+(i+1)%n,(j+1)*n+(i+1)%n,(j+1)*n+i) for j in range(len(rings)-1) for i in range(n)]
 fs.extend([tuple(reversed(range(n))),tuple((len(rings)-1)*n+i for i in range(n))]);return mesh(name,vs,fs,ma,sub)
def curve(name,pts,r,ma):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=3
 sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(pts)-1)
 for b,p in zip(sp.bezier_points,pts):b.co=p;b.handle_left_type=b.handle_right_type='AUTO'
 o=bpy.data.objects.new(name,cu);s.collection.objects.link(o);cu.materials.append(ma);return o
# Head: narrow chin, broad cheek, gently flattened face; not an egg/sphere.
loft('Head_continuous',[(0,0,1.08,.025,.025),(0,-.018,1.105,.075,.065),(0,-.015,1.15,.15,.105),(0,0,1.23,.215,.142),(0,.012,1.32,.228,.164),(0,.025,1.42,.222,.183),(0,.025,1.51,.17,.157),(0,.025,1.565,.08,.087),(0,.025,1.58,.008,.01)],skin,48,2)
loft('Neck',[(0,0,1.00,.051,.05),(0,0,1.11,.055,.052),(0,0,1.15,.072,.06)],skin)
def lens(name,cx,cz,rx,rz,depth,ma):
 vs=[(cx,depth-.012,cz)];fs=[];n=48
 for ring in range(1,5):
  r=ring/4
  for i in range(n):
   a=2*math.pi*i/n;vs.append((cx+rx*r*math.cos(a),depth-.012*(1-r*r),cz+rz*r*math.sin(a)))
 for i in range(n):fs.append((0,1+i,1+(i+1)%n))
 for j in range(3):
  for i in range(n):a=1+j*n+i;b=1+j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
 return mesh(name,vs,fs,ma)
for sign in [-1,1]:
 x=sign*.092
 lens('Eye_white',x,1.295,.058,.067,-.153,white)
 lens('Iris_plum',x,1.292,.034,.053,-.171,iris)
 lens('Pupil',x,1.30,.017,.035,-.184,ink)
 lens('Eye_glint',x-.012,1.32,.010,.014,-.198,white)
 lens('Eye_glint_small',x+.013,1.272,.004,.006,-.197,white)
 curve('Upper_eyelid',[(x-.059,-.164,1.30),(x-.035,-.174,1.35),(x,-.176,1.361),(x+.038,-.17,1.35),(x+.061,-.159,1.314)],.006,ink)
 curve('Lower_lid',[(x-.05,-.159,1.27),(x,-.168,1.228),(x+.05,-.159,1.27)],.0025,rose)
 curve('Brow',[(x-.04,-.147,1.385),(x,-.154,1.395),(x+.04,-.146,1.385)],.004,hair)
 # Small integrated ear shape under the hair.
 loft('Ear',[(sign*.211,0,1.22,.018,.024),(sign*.222,0,1.25,.026,.025),(sign*.217,0,1.285,.016,.017)],skin,16,1)
curve('Mouth', [(-.029,-.132,1.176),(0,-.144,1.17),(.029,-.132,1.179)],.0028,rose)
mesh('Nose_plane',[(-.012,-.145,1.245),(0,-.176,1.226),(.012,-.145,1.245),(0,-.145,1.214)],[(0,1,2),(0,3,1),(1,3,2)],skin,2)
# Hair cap and curved tapered ribbons with a lenticular cross section.
vs=[];fs=[];n=64
for j in range(17):
 for i in range(n):
  a=2*math.pi*i/n;edge=2.04-.98*max(0,-math.sin(a))**4;t=.01+(edge-.01)*j/16
  vs.append((.244*math.sin(t)*math.cos(a),.024+.208*math.sin(t)*math.sin(a),1.34+.27*math.cos(t)))
for j in range(16):
 for i in range(n):a=j*n+i;b=j*n+(i+1)%n;fs.append((a,b,b+n,a+n))
mesh('Coherent_hair_cap',vs,fs,hair,1)
def lock(name,pts,width,ma):
 vs=[];fs=[]
 for j,p in enumerate(pts):
  t=j/(len(pts)-1);w=width*(math.sin(math.pi*(.15+.85*t))**.7 if j<len(pts)-1 else .015)
  for i in range(12):
   a=2*math.pi*i/12;vs.append((p[0]+w*math.cos(a),p[1]+w*.22*math.sin(a),p[2]))
 for j in range(len(pts)-1):
  for i in range(12):a=j*12+i;b=j*12+(i+1)%12;fs.append((a,b,b+12,a+12))
 mesh(name,vs,fs,ma,2)
for i in range(7):
 x=(i-3)*.058;tip=x+(.027 if i<4 else -.02);z=1.36+abs(i-3)*.022
 lock('Swept_bang',[(x*.35,-.09,1.588),(x*.7,-.155,1.54),(x,-.20,1.47),(tip,-.204,z+.045),(tip-.025,-.185,z)],.047,hl if i%3==0 else hair)
for sign in [-1,1]:
 for k in range(4):
  y=-.10+k*.07
  lock('Layered_side_lock',[(sign*.15,y,1.55),(sign*.221,y-.035,1.45),(sign*(.25+k*.004),y-.04,1.32),(sign*.25,y-.03,1.19),(sign*(.20+k*.025),y-.06,1.13+k*.016)],.054,hair if k%2 else hl)
 curve('Antenna',[(sign*.071,.025,1.59),(sign*.10,.02,1.75),(sign*.19,.025,1.82),(sign*.25,.025,1.77)],.008,hair)
# Sleeved tunic, underskirt and legs built as surface strips with tapered sections.
loft('Linen_hem',[(0,0,.48,.20,.12),(0,0,.51,.20,.12),(0,0,.73,.14,.10),(0,0,.81,.12,.085)],linen,48,1)
loft('Sage_wrap',[(0,0,.55,.20,.122),(0,0,.58,.19,.12),(0,0,.75,.128,.096),(0,0,.90,.145,.104),(0,0,1.0,.162,.095),(0,0,1.04,.075,.062)],green,48,1)
for sign in [-1,1]:
 loft('Leg',[(sign*.085,0,.13,.039,.043),(sign*.087,0,.22,.043,.048),(sign*.089,0,.34,.046,.05),(sign*.09,0,.40,.041,.045),(sign*.085,0,.53,.063,.065),(sign*.08,0,.65,.072,.074)],skin)
 loft('Boot',[(sign*.087,-.026,.015,.060,.095),(sign*.087,-.031,.04,.064,.10),(sign*.087,-.031,.09,.06,.10),(sign*.087,0,.14,.045,.048),(sign*.087,0,.22,.049,.052),(sign*.087,0,.235,.054,.057)],leather)
 loft('Sleeve',[(sign*.24,0,.84,.075,.076),(sign*.245,0,.86,.078,.079),(sign*.19,0,.96,.08,.084),(sign*.14,0,1.015,.06,.068)],green)
 loft('Arm',[(sign*.36,-.008,.65,.027,.030),(sign*.34,0,.70,.034,.036),(sign*.29,0,.79,.037,.039),(sign*.24,0,.87,.048,.048),(sign*.20,0,.94,.051,.05)],skin)
 loft('Hand',[(sign*.382,-.008,.59,.019,.014),(sign*.375,-.008,.61,.027,.020),(sign*.36,-.008,.66,.028,.023)],skin,24,1)
 for i in range(4):
  x=sign*(.36+i*.012)
  curve('Finger',[(x,-.012,.618),(x+sign*.008,-.015,.585),(x+sign*.01,-.008,.57+abs(i-1)*.008)],.006,skin)
 curve('Thumb',[(sign*.35,-.01,.64),(sign*.332,-.018,.618),(sign*.326,-.015,.60)],.009,skin)
 curve('Boot_top_stitch',[(sign*.087+.05*math.cos(a),.055*math.sin(a),.226) for a in [i*2*math.pi/32 for i in range(33)]],.002,linen)
 # Collar is a draping surface, not a spherical shoulder pad.
 mesh('Collar',[(0,-.067,1.043),(sign*.065,-.061,1.043),(sign*.17,-.046,1.003),(sign*.16,-.10,.977),(sign*.075,-.122,.993),(0,-.09,1.023)],[(0,1,2,3,4,5)],linen,2)
curve('Belt',[(.135*math.cos(a),.101*math.sin(a),.76) for a in [i*2*math.pi/48 for i in range(49)]],.014,leather)
curve('Satchel_strap',[(.10,-.075,1.027),(.06,-.116,.92),(-.04,-.12,.80),(-.15,-.145,.67)],.017,leather)
loft('Satchel', [(-.16,-.15,.55,.07,.032),(-.16,-.15,.57,.077,.038),(-.16,-.15,.70,.07,.031),(-.16,-.15,.725,.062,.026)],leather,24,2)
curve('Satchel_flap',[(-.225,-.188,.69),(-.16,-.196,.655),(-.095,-.188,.69)],.004,linen)
lens('Amber_clasp',0,1.024,.022,.012,-.102,gold)
# Neutral studio; isolated scene, no changes to production models.
floor=mat('Backdrop',(.65,.61,.55))
mesh('Ground',[(-20,-20,0),(20,-20,0),(20,20,0),(-20,20,0)],[(0,1,2,3)],floor)
world=bpy.data.worlds.new('Anime_studio');world.use_nodes=True;world.node_tree.nodes.get('Background').inputs[0].default_value=(.75,.72,.70,1);world.node_tree.nodes.get('Background').inputs[1].default_value=.7;s.world=world
for name,loc,power,size in [('Key',(-3,-4,5),450,4),('Fill',(3,-2,3),250,3),('Rim',(0,3,4),400,3)]:
 d=bpy.data.lights.new(name,'AREA');d.energy=power;d.shape='DISK';d.size=size;o=bpy.data.objects.new(name,d);s.collection.objects.link(o);o.location=loc;o.rotation_euler=(Vector((0,0,1))-o.location).to_track_quat('-Z','Y').to_euler()
d=bpy.data.cameras.new('Study_camera');o=bpy.data.objects.new('Study_camera',d);s.collection.objects.link(o);o.location=(2,-6,2.1);o.rotation_euler=(Vector((0,0,.93))-o.location).to_track_quat('-Z','Y').to_euler();d.type='ORTHO';d.ortho_scale=2.15;s.camera=o
s.render.engine='CYCLES';s.cycles.samples=32;s.render.resolution_x=900;s.render.resolution_y=1100;s.render.resolution_percentage=100;s.render.filepath=str(P/'direct-model-preview.png')
bpy.ops.wm.save_as_mainfile(filepath=str(P/'direct-model-study.blend'))
print('Direct mesh study saved; render separately; not production rigged.')
