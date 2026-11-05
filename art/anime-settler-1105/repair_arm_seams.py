"""Rebuild continuous arm/torso skin boundaries from the original Meshy topology."""
import bpy, ast, math, json
from pathlib import Path
from mathutils import Vector
P=Path('F:/UnityProjects/ant-colony-rts/art/anime-settler-1105')
source=bpy.data.scenes['Meadow_Textured_Source'].objects['Meadow_Textured_Low']
scene=bpy.data.scenes['Textured_Modular_Settler'];bpy.context.window.scene=scene
rig=scene.objects['KemonoRig']
baseline=P/'before-arm-geometry.json'
if not baseline.exists():
 with bpy.data.libraries.load(str(P/'game-character.blend'),link=False) as (available,loaded):
  requested=[n for n in available.objects if n.startswith(('Part_Body_','Part_Outfit_'))]
  loaded.objects=requested.copy()
 data={}
 for name,obj in zip(requested,loaded.objects):
  data[name]={'vertices':[list(v.co) for v in obj.data.vertices],
   'weights':[[(obj.vertex_groups[g.group].name,g.weight) for g in v.groups] for v in obj.data.vertices]}
  bpy.data.objects.remove(obj,do_unlink=True)
 baseline.write_text(json.dumps(data))
for name,data in json.loads(baseline.read_text()).items():
 obj=scene.objects.get(name)
 if obj is None:continue
 for v,p in zip(obj.data.vertices,data['vertices']):v.co=p
 if name.startswith('Part_Outfit_'):
  for v,ws in zip(obj.data.vertices,data['weights']):
   for g in list(v.groups):obj.vertex_groups[g.group].remove([v.index])
   for n,w in ws:obj.vertex_groups[n].add([v.index],w,'REPLACE')
tree=ast.parse((P/'build_textured_modular.py').read_text())
# Recover the original deterministic source-face partition, without rebuilding the scene.
exec(compile(ast.Module(body=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='category'],type_ignores=[]),'category','exec'))
def smooth(a,b,x):
 t=max(0,min(1,(x-a)/(b-a)));return t*t*(3-2*t)
def assign(o,i,ws):
 for g in list(o.data.vertices[i].groups):o.vertex_groups[g.group].remove([i])
 for n,w in ws.items():
  if n not in o.vertex_groups:o.vertex_groups.new(name=n)
  if w>0:o.vertex_groups[n].add([i],w,'REPLACE')
for kind in ['Body','Outfit']:
 faces=[f for f in source.data.polygons if category(sum((source.data.vertices[i].co for i in f.vertices),Vector())/len(f.vertices))==kind]
 ids=sorted({i for f in faces for i in f.vertices})
 for o in scene.objects:
  if not o.name.startswith('Part_'+kind+'_') or len(o.data.vertices)!=len(ids):continue
  changes=0
  for i,src in enumerate(ids):
   p=source.data.vertices[src].co;x,y,z=p
   if z<-.25 or z>.25:continue
   arm=smooth(.19,.255,abs(x))*(1-smooth(.10,.15,-y))
   if kind=='Outfit':arm*=smooth(-.015,.035,z)
   if arm==0:
    # Remove garment vertices accidentally attached to the wrist (satchel and skirt).
    if any(o.vertex_groups[g.group].name.startswith(('hand.','forearm.','upper_arm.')) for g in o.data.vertices[i].groups):
     assign(o,i,{'spine':1} if z<0 else {'spine_02':1});changes+=1
    continue
   # Preserve fitted finger weights; this pass repairs the forearm and sleeve seam.
   if kind=='Body' and o.data.vertices[i].co.z<.47 and abs(o.data.vertices[i].co.x)>.19:continue
   side='L' if x>0 else 'R'
   if abs(x)>.265 and y>-.10 and z>.015:
    # The previous per-bone warp had a discontinuity on the inside of the elbow.
    # Use one continuous mapping for both skin and sleeve boundary vertices.
    q=Vector((x*.56,y*.72+.012,.551+(z-.035)*(1.158 if z<.035 else .816)))
    blend=smooth(-.035,.005,z)*smooth(.245,.28,abs(x))
    o.data.vertices[i].co=o.data.vertices[i].co.lerp(q,blend)
   elbow=1-smooth(.005,.065,z);wrist=1-smooth(-.085,-.035,z)
   ws={'upper_arm.'+side:arm*(1-elbow),'forearm.'+side:arm*elbow*(1-wrist),'hand.'+side:arm*elbow*wrist,'spine_02':1-arm}
   assign(o,i,ws);changes+=1
  print(o.name,changes)
# Smooth vertex weights spatially across duplicated UV seams and adjacent triangles.
for o in scene.objects:
 if not o.name.startswith(('Part_Body_','Part_Outfit_')):continue
 neighbors=[set() for v in o.data.vertices]
 for e in o.data.edges:
  a,b=e.vertices;neighbors[a].add(b);neighbors[b].add(a)
 for iteration in range(5):
  weights=[{o.vertex_groups[g.group].name:g.weight for g in v.groups} for v in o.data.vertices]
  for v in o.data.vertices:
   if not (.47<v.co.z<.68 and abs(v.co.x)>.12):continue
   near=[j for j in neighbors[v.index] if (o.data.vertices[j].co-v.co).length<.045]
   if not near:continue
   ws={n:w*.5 for n,w in weights[v.index].items()}
   for j in near:
    for n,w in weights[j].items():ws[n]=ws.get(n,0)+w*.5/len(near)
   total=sum(ws.values());assign(o,v.index,{n:w/total for n,w in ws.items()})
bpy.ops.object.select_all(action='DESELECT')
for o in scene.objects:
 if o==rig or o.name.startswith('Part_'):o.hide_set(False);o.select_set(True)
bpy.context.view_layer.objects.active=rig
body=scene.objects['Part_Body_Meadow_00'];body.name='SourceBodyForSubdivision'
polished=body.copy();polished.data=body.data.copy();polished.name='Part_Body_Meadow_00';scene.collection.objects.link(polished)
polished.modifiers.clear();sub=polished.modifiers.new('Hand deformation topology','SUBSURF');sub.levels=1
bpy.context.view_layer.objects.active=polished
bpy.ops.object.modifier_apply(modifier=sub.name)
skin=polished.modifiers.new('ProductionSkin','ARMATURE');skin.object=rig
body.select_set(False);polished.select_set(True)
exec(compile((P/'arm_reach_export.py').read_text(),'arm_reach_export.py','exec'))
restore_reach=extend_arm_reach(scene,rig)
try:
 bpy.ops.export_scene.fbx(filepath=str(P/'Kemono_Character_HandFix.fbx'),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False)
finally:
 restore_reach()
 bpy.data.objects.remove(polished,do_unlink=True);body.name='Part_Body_Meadow_00'

