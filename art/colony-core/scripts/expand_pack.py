import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector
BASE=Path('E:/Git/ant/art/colony-core')
sys.path.insert(0,str(BASE/'scripts'))
import geometry as g, models_expansion
scene=bpy.context.scene
assert scene.name=='ColonyCore_Showcase'
assert not bpy.data.objects.get('CC_Dormitory'), 'Expansion exists already.'
before=set(scene.objects)
models_expansion.build()
roots=[o for o in scene.objects if o not in before and o.parent is None and o.type=='EMPTY']
manifest=json.loads((BASE/'manifest.json').read_text())
for root in roots:
    meshes=[o for o in root.children_recursive if o.type=='MESH']
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    low=min(v.z for v in pts)
    for o in root.children: o.location.z-=low
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    size=[round(max(v[i] for v in pts)-min(v[i] for v in pts),4) for i in range(3)]
    triangles=0
    for o in meshes: o.data.calc_loop_triangles(); triangles+=len(o.data.loop_triangles)
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*root.children_recursive]: o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(BASE/'exports'/f'{root.name}.fbx'),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE')
    index=len(manifest)
    manifest.append({'id':root.name,'fbx':f'exports/{root.name}.fbx','dimensions_blender_xyz':size,'dimensions_unity_xyz':[size[0],size[2],size[1]],'triangles':triangles,'mesh_count':len(meshes),'materials':sorted({m.name for o in meshes for m in o.data.materials}),'children':[o.name for o in root.children]})
    root.location=(index%6*5,index//6*4.5,0)
    data=bpy.data.curves.new('Label','FONT'); data.body=root.name.removeprefix('CC_'); data.size=.23; data.align_x='CENTER'
    label=bpy.data.objects.new('Label_'+root.name,data); scene.collection.objects.link(label); label.location=(root.location.x,root.location.y-1.75,.005); data.materials.append(g.MATS['Dark'])
for i,row in enumerate(manifest):
    root=bpy.data.objects[row['id']]; root.location=(i%6*5,i//6*4.5,0)
    bpy.data.objects['Label_'+row['id']].location=(root.location.x,root.location.y-1.75,.005)
floor=bpy.data.objects['Studio floor']; floor.location=(12.5,11.25,-.22); floor.scale=(1.2,1.25,1)
camera=scene.camera; camera.location=(32,-38,48); camera.rotation_euler=(Vector((12.5,11,0))-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.ortho_scale=39
scene.render.resolution_x=2100; scene.render.resolution_y=1700
(BASE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
bpy.data.libraries.write(str(BASE/'materials.blend'),set(g.MATS.values()),fake_user=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'colony-core.blend'))
print('TOTAL_ASSETS',len(manifest),'TRIANGLES',sum(x['triangles'] for x in manifest))

