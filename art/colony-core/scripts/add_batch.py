"""Execute with MODULES set to recipe module names; append only new assets."""
import bpy,sys,json,math,importlib
from pathlib import Path
from mathutils import Vector
BASE=Path('E:/Git/ant/art/colony-core')
sys.path.insert(0,str(BASE/'scripts'))
import geometry as g
g.MATS={name:bpy.data.materials['CC_'+name] for name in g.PALETTE}
scene=bpy.context.scene
assert scene.name=='ColonyCore_Showcase'
manifest=json.loads((BASE/'manifest.json').read_text())
before=set(scene.objects)
for module in MODULES:
    assert not any(r.get('recipe')==module for r in manifest), 'Recipe already built: '+module
    start=set(scene.objects)
    importlib.import_module(module).build()
    for root in set(scene.objects)-start:
        if root.parent is None and root.type=='EMPTY': root['recipe']=module
roots=sorted([o for o in set(scene.objects)-before if o.parent is None and o.type=='EMPTY'],key=lambda o:o.name)
for root in roots:
    assert root.name not in {r['id'] for r in manifest}
    meshes=[o for o in root.children_recursive if o.type=='MESH']
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    low=min(v.z for v in pts)
    for o in root.children: o.location.z-=low
    bpy.context.view_layer.update()
    pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    size=[round(max(v[i] for v in pts)-min(v[i] for v in pts),4) for i in range(3)]
    triangles=0
    for o in meshes:
        for poly in o.data.polygons: poly.use_smooth=False
        o.data.calc_loop_triangles(); triangles+=len(o.data.loop_triangles)
        assert all(math.isfinite(c) for v in o.data.vertices for c in v.co)
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*root.children_recursive]: o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(BASE/'exports'/f'{root.name}.fbx'),use_selection=True,object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',axis_forward='-Z',axis_up='Y',bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE')
    manifest.append({'id':root.name,'recipe':root['recipe'],'fbx':f'exports/{root.name}.fbx','dimensions_blender_xyz':size,'dimensions_unity_xyz':[size[0],size[2],size[1]],'triangles':triangles,'mesh_count':len(meshes),'materials':sorted({m.name for o in meshes for m in o.data.materials}),'children':[o.name for o in root.children]})
    data=bpy.data.curves.new('Label','FONT'); data.body=root.name.removeprefix('CC_'); data.size=.23; data.align_x='CENTER'; data.materials.append(g.MATS['Dark'])
    label=bpy.data.objects.new('Label_'+root.name,data); scene.collection.objects.link(label)
for i,row in enumerate(manifest):
    root=bpy.data.objects[row['id']]; root.location=(i%9*5,i//9*5.5,0)
    bpy.data.objects['Label_'+row['id']].location=(root.location.x,root.location.y-1.9,.005)
rows=math.ceil(len(manifest)/9); center=Vector((20,(rows-1)*2.75,0))
floor=bpy.data.objects['Studio floor']; floor.location=(*center[:2],-.22); floor.dimensions=(47,rows*5.5+4,.35)
camera=scene.camera; camera.location=center+Vector((28,-55,63)); camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.ortho_scale=max(56,rows*6)
scene.render.resolution_x=2600; scene.render.resolution_y=2200
(BASE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'colony-core.blend'))
print('ADDED',len(roots),'TOTAL',len(manifest))
