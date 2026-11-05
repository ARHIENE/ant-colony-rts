"""Meshy-derived modular prototype. Production export follows visual/animation validation."""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).parent
source=bpy.data.scenes['Anime_Modular_Work'].objects['Meadow_Working']
rig=next(o for o in bpy.data.scenes['Anime_Modular_Work'].objects if o.type=='ARMATURE')
s=bpy.data.scenes.new('Modular_Settler');bpy.context.window.scene=s
s.collection.objects.link(rig);rig.name='KemonoRig';rig.animation_data_clear()
from mathutils import Matrix
for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
def material(name,c,texture=False):
 m=bpy.data.materials.new('Meadow_'+name);m.diffuse_color=(*c,1);m.use_nodes=True
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*c,1);p.inputs['Roughness'].default_value=.82
 if texture:
  im=bpy.data.images.load(str(P/'front-a-pose.png'),check_existing=True);im.pack();n=m.node_tree.nodes.new('ShaderNodeTexImage');n.image=im;m.node_tree.links.new(n.outputs['Color'],p.inputs['Base Color'])
 return m
skin=material('Skin',(.83,.57,.43));silver=material('Hair',(.69,.63,.67));cloth=material('Sage',(.24,.29,.17));cream=material('Linen',(.73,.64,.48));leather=material('Leather',(.25,.12,.065));face=material('ReferenceFace',(.8,.6,.5),True);frontcloth=material('ReferenceCloth',(.3,.3,.2),True);gold=material('Brass',(.64,.38,.09));steel=material('Guard',(.23,.27,.29))
# Source landmarks are measured on Meshy A pose; targets are the existing animation rig.
segments={
 'pevis':((0,0,-.30),(0,0,-.13)), 'spine':((0,0,-.20),(0,0,-.05)),
 'spine_01':((0,0,-.10),(0,0,.06)), 'spine_02':((0,0,.04),(0,0,.23)),
 'neck':((0,0,.23),(0,0,.30)), 'head':((0,0,.30),(0,0,.77))}
for side,sg in [('L',1),('R',-1)]:
 for name,a,b in [('upper_arm',(.19,0,.16),(.31,0,.035)),('forearm',(.31,0,.035),(.42,0,-.07)),('hand',(.42,0,-.07),(.46,0,-.13)),('thigh',(.12,0,-.30),(.13,0,-.54)),('shin',(.13,0,-.54),(.14,0,-.78)),('foot',(.14,0,-.78),(.14,-.10,-.93))]:
  segments[name+'.'+side]=((sg*a[0],a[1],a[2]),(sg*b[0],b[1],b[2]))
S={}
for name,(a,b) in segments.items():
 a,b=Vector(a),Vector(b);bone=rig.data.bones[name];ta=rig.matrix_world@bone.head_local;tb=rig.matrix_world@bone.tail_local
 u=(b-a).normalized();v=(tb-ta).normalized();S[name]=(a,b,ta,tb,u,u.rotation_difference(v))
def weights(p,kind):
 x,y,z=p
 if z>.30:return [('head',1.)]
 side='L' if x>0 else 'R'
 if kind=='Body' and z<-.32: names=['thigh.'+side,'shin.'+side,'foot.'+side]
 elif abs(x)>.245 and z<.19 and z>-.24:names=['upper_arm.'+side,'forearm.'+side,'hand.'+side]
 elif z>.18:names=['neck','spine_02']
 elif z<-.10:names=['pevis','spine']
 else:names=['spine','spine_01','spine_02']
 ds=[]
 for n in names:
  a,b,*_=S[n];t=max(0,min(1,(p-a).dot(b-a)/(b-a).length_squared));d=(p-a-t*(b-a)).length
  ds.append((n,math.exp(-d*d/.008)))
 total=sum(w for n,w in ds)
 if total<1e-12:return [(min(names,key=lambda n:(p-S[n][0]).length),1)]
 return [(n,w/total) for n,w in ds if w/total>.015]
def warp(p,ws):
 if len(ws)==1 and ws[0][0]=='head':return Vector((p.x*.80,p.y*.80,.722+(p.z-.30)*.89))
 if all(n in ['pevis','spine','spine_01','spine_02','neck'] for n,w in ws):
  return Vector((p.x*.65,p.y*.72-.02,.54+p.z*.65))
 if all(n.startswith(('thigh.','shin.','foot.')) for n,w in ws):
  return Vector(((1 if p.x>0 else -1)*(.05+(abs(p.x)-.13)*.72),p.y*.72,.65+p.z*.72))
 q=Vector()
 for n,w in ws:
  a,b,ta,tb,u,rot=S[n];along=(p-a).dot(u);radial=p-a-u*along
  q+=w*(ta+(tb-ta)*(along/(b-a).length)+rot@radial*.72)
 return q
def category(p):
 x,y,z=p
 if z>.80:return 'Ears'
 if z>.31:
  if z>.65 or y>.025 or (abs(x)>.185 and z>.37) or (z>.575 and y>-.15):return 'Hair'
  return 'Face'
 if z<-.33 or (abs(x)>.31 and z<.065):return 'Body'
 return 'Outfit'
groups={k:[] for k in ['Body','Hair','Face','Outfit','Ears']}
for f in source.data.polygons:
 p=sum((source.data.vertices[i].co for i in f.vertices),Vector())/len(f.vertices);groups[category(p)].append(f)
def build(name,faces,kind,variant=0):
 ids=sorted({i for f in faces for i in f.vertices});remap={v:i for i,v in enumerate(ids)};vs=[];orig=[];ws=[]
 for i in ids:
  p=source.data.vertices[i].co.copy();orig.append(p.copy());w=weights(p,kind);q=p.copy()
  if kind=='Hair':
   lower=max(0,.59-p.z)
   if variant==1:q.z+=lower*.65;q.x*=.92 # cropped bob
   if variant==2:q.z-=lower*1.7;q.x*=1.02 # long side/back hair
   if variant==3 and p.y>.025:q.z-=lower*2.6;q.x*=.45;q.y+=lower*.8 # ponytail
   if variant==4 and abs(p.x)>.17:q.z-=lower*1.8;q.x*=1.20 # twin tails
   if variant==5 and p.y>.02:q.z+=lower*.7;q.x*=.80;q.y+=.055 # swept up
  if kind=='Outfit':
   if variant==1 and p.z<-.05:q.x*=1.05;q.z-=max(0,-.05-p.z)*.16
   if variant==2 and abs(p.x)>.20:q.x*=.93
   if variant==3 and p.z<-.07:q.z+=(-.07-p.z)*.27
  vs.append(warp(q,w));ws.append(w)
 me=bpy.data.meshes.new(name);me.from_pydata(vs,[],[tuple(remap[i] for i in f.vertices) for f in faces]);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o)
 mats=[skin,silver,cloth,cream,leather,face,frontcloth,gold,steel]
 for m in mats:me.materials.append(m)
 uv=me.uv_layers.new(name='ReferenceProjection')
 for poly,srcf in zip(me.polygons,faces):
  p=sum((orig[i] for i in poly.vertices),Vector())/len(poly.vertices)
  mi=0
  if kind in ['Hair','Ears']:mi=1
  elif kind=='Face':mi=5 if p.y<-.075 else 0
  elif kind=='Body':mi=4 if p.z<-.68 else 0
  elif kind=='Outfit':
   mi=6 if variant==0 and p.y<-.06 else (8 if variant==3 else 2)
   if p.z>.18:mi=3
   if abs(p.x)>.14 and p.y<-.17 and p.z<0:mi=4
  poly.material_index=mi;poly.use_smooth=True
  for li in poly.loop_indices:
   p=orig[me.loops[li].vertex_index];uv.data[li].uv=(.5+p.x*.754,.03+(p.z+.952)/1.903*.957)
 for n in segments:
  g=o.vertex_groups.new(name=n)
  for i,w in enumerate(ws):
   for bn,bw in w:
    if bn==n:g.add([i],bw,'REPLACE')
 o.parent=rig;o.matrix_parent_inverse=rig.matrix_world.inverted();mod=o.modifiers.new('ProductionSkin','ARMATURE');mod.object=rig
 if kind=='Face':
  o.shape_key_add(name='Basis',from_mix=False).value=0
  for key in ['Blink','Smile','Laugh','Surprise','Determined','Sad','Sleep','Pain']:
   sk=o.shape_key_add(name=key,from_mix=False);sk.value=0
   for i,p in enumerate(orig):
    if p.y>-.075:continue
    eye=math.exp(-((abs(p.x)-.115)/.065)**4-((p.z-.53)/.075)**4)
    mouth=math.exp(-(p.x/.055)**4-((p.z-.39)/.04)**4)
    dz=0;dx=0
    if key in ['Blink','Laugh','Sleep']:dz=-(p.z-.53)*eye*.94
    if key in ['Smile','Laugh']:dz+=(abs(p.x)/.055*.014-.003)*mouth
    if key=='Surprise':dz+=(p.z-.53)*eye*.15+(p.z-.39)*mouth*.65;dx=-p.x*mouth*.18
    if key=='Determined':dz+=(abs(p.x)-.115)*eye*.15
    if key=='Sad':dz-=(abs(p.x)-.115)*eye*.16;dz-=abs(p.x)*mouth*.18
    if key=='Pain':dz=-(p.z-.53)*eye*.7
    sk.data[i].co.z+=dz*.89;sk.data[i].co.x+=dx*.8
 return o
build('Part_Body_Meadow_00',groups['Body'],'Body')
build('Part_Face_NEUTRAL_00',groups['Face'],'Face')
build('Part_Ears_ANTENNA_00',groups['Ears'],'Ears')
for i,n in enumerate(['BOB','CROP','LONG','PONY','TWIN','UPDO']):
 o=build('Part_Hair_'+n+'_00',groups['Hair'],'Hair',i);o.hide_render=i!=0;o.hide_set(i!=0)
for i,n in enumerate(['CITIZEN','FARMER','ARTISAN','GUARD']):
 o=build('Part_Outfit_'+n+'_00',groups['Outfit'],'Outfit',i);o.hide_render=i!=0;o.hide_set(i!=0)
report={'stage':'prototype - not approved for production','hair':6,'outfits':4,'shapes':8,'parts':len([o for o in s.objects if o.type=='MESH']),'source_faces':len(source.data.polygons),'texture_method':'front-reference projection pending Meshy texture'}
(P/'modular-prototype-report.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'modular-prototype.blend'));print(json.dumps(report))
