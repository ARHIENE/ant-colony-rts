import bpy, math, json
from mathutils import Vector
from mathutils.kdtree import KDTree
from pathlib import Path
P=Path(__file__).parent
s=bpy.data.scenes['Textured_Modular_Settler'];bpy.context.window.scene=s
rig=next(o for o in s.objects if o.type=='ARMATURE')
palette={}
def mat(name,color):
 m=bpy.data.materials.new(name);m.use_nodes=True;m.diffuse_color=(*color,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Roughness'].default_value=.8
 palette[name]=list(color)
 return m
linen=mat('Meadow_ApronLinen',(.71,.58,.36));leather=mat('Meadow_ArtisanLeather',(.24,.105,.055));guard=mat('Meadow_GuardLeather',(.17,.21,.25));trim=mat('Meadow_BrassTrim',(.65,.43,.16))
def panel(variant,label,rows,material):
 # Tailored curved cloth panels; weights inherited from the adjacent garment.
 src=s.objects['Part_Outfit_'+variant+'_00'];tree=KDTree(len(src.data.vertices))
 for v in src.data.vertices:tree.insert(v.co,v.index)
 tree.balance();verts=[];faces=[];steps=12
 for z,width,y in rows:
  for j in range(steps+1):
   t=2*j/steps-1;verts.append((t*width,y+abs(t)**2*.032,z-.008*math.cos(t*math.pi/2)))
 for i in range(len(rows)-1):
  for j in range(steps):
   a=i*(steps+1)+j;faces.append((a,a+1,a+steps+2,a+steps+1))
 me=bpy.data.meshes.new(label);me.from_pydata(verts,[],faces);me.materials.append(material);me.update()
 o=bpy.data.objects.new('Part_Outfit_'+variant+'_'+label+'_00',me);s.collection.objects.link(o);o.parent=rig;o.matrix_parent_inverse=rig.matrix_world.inverted()
 for g in src.vertex_groups:o.vertex_groups.new(name=g.name)
 for v in me.vertices:
  _,idx,_=tree.find(v.co)
  for g in src.data.vertices[idx].groups:o.vertex_groups[g.group].add([v.index],g.weight,'REPLACE')
 sol=o.modifiers.new('Cloth thickness','SOLIDIFY');sol.thickness=.003
 bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.modifier_apply(modifier=sol.name)
 arm=o.modifiers.new('ProductionSkin','ARMATURE');arm.object=rig
 for f in me.polygons:f.use_smooth=True
 return o
panel('FARMER','Apron',[(.65,.065,-.126),(.57,.08,-.154),(.50,.11,-.175),(.40,.14,-.175)],linen)
panel('ARTISAN','WorkApron',[(.66,.068,-.14),(.57,.09,-.17),(.49,.12,-.19),(.37,.135,-.175)],leather)
panel('ARTISAN','Pocket',[(.49,.072,-.198),(.43,.072,-.197)],linen)
panel('GUARD','Vest',[(.70,.075,-.105),(.65,.13,-.13),(.58,.12,-.17),(.53,.11,-.18)],guard)
panel('GUARD','HemTrim',[(.54,.112,-.186),(.525,.112,-.186)],trim)
# Normalize retained influences after pruning very small weights.
for o in s.objects:
 if o.type!='MESH':continue
 for v in o.data.vertices:
  gs=[(g.group,g.weight) for g in v.groups];total=sum(w for _,w in gs)
  if total:
   for gi,w in gs:o.vertex_groups[gi].add([v.index],w/total,'REPLACE')
 o.hide_render=False;o.hide_set(False)
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(P/'Kemono_Character.fbx'),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False)
(P/'material-colors.json').write_text(json.dumps(palette,indent=2))
for o in s.objects:
 if o.type=='MESH':
  visible=not(o.name.startswith('Part_Hair_') and not o.name.startswith('Part_Hair_BOB_')) and not(o.name.startswith('Part_Outfit_') and not o.name.startswith('Part_Outfit_CITIZEN_'))
  o.hide_render=not visible;o.hide_set(not visible)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'game-character.blend'))
print('Exported skinned character with six hair variants, four outfit groups and eight facial shapes.')
