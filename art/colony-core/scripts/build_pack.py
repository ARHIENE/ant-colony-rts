"""Run in Blender: exec(compile(open(PATH).read(), PATH, 'exec'))."""
import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector

BASE=Path('E:/Git/ant/art/colony-core')
sys.path.insert(0,str(BASE/'scripts'))
import geometry as g, models

assert 'ColonyCore_Showcase' not in bpy.data.scenes, 'Pack already built; preserve it rather than overwrite.'
scene=bpy.data.scenes.new('ColonyCore_Showcase')
bpy.context.window.scene=scene
scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
g.materialize(); models.build()
roots=[o for o in scene.objects if o.parent is None and o.type=='EMPTY']
manifest=[]
for index,root in enumerate(roots):
    meshes=[o for o in root.children_recursive if o.type=='MESH']
    # Make every asset rest on the ground; root stays at zero for exported pivots.
    bpy.context.view_layer.update()
    bounds=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    low=min(v.z for v in bounds)
    for child in root.children: child.location.z-=low
    bpy.context.view_layer.update()
    bounds=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
    size=[round(max(v[i] for v in bounds)-min(v[i] for v in bounds),4) for i in range(3)]
    triangles=0
    for o in meshes:
        o.data.calc_loop_triangles(); triangles+=len(o.data.loop_triangles)
        assert all(math.isfinite(v) for vertex in o.data.vertices for v in vertex.co)
        assert len(o.data.materials)>0
    bpy.ops.object.select_all(action='DESELECT')
    for o in [root,*root.children_recursive]: o.select_set(True)
    bpy.context.view_layer.objects.active=root
    bpy.ops.export_scene.fbx(filepath=str(BASE/'exports'/f'{root.name}.fbx'),use_selection=True,
        object_types={'EMPTY','MESH'},apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',
        axis_forward='-Z',axis_up='Y',bake_anim=False,use_mesh_modifiers=True,
        add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='AUTO')
    manifest.append({'id':root.name,'fbx':f'exports/{root.name}.fbx','dimensions_blender_xyz':size,
        'dimensions_unity_xyz':[size[0],size[2],size[1]],'triangles':triangles,
        'mesh_count':len(meshes),'materials':sorted({m.name for o in meshes for m in o.data.materials}),
        'children':[o.name for o in root.children],'ground_offset_applied':round(low,5)})
    root.location=(index%5*5,index//5*4.5,0)

(BASE/'manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(BASE/'palette.json').write_text(json.dumps({f'CC_{k}':{'linear_rgba':[*v,1], 'roughness':.8,'metallic':.25 if k=='Iron' else 0} for k,v in g.PALETTE.items()},indent=2),encoding='utf-8')

# Studio objects are only in the .blend, never in individual FBXs.
g.ROOT=None
floor=g.box('Studio floor',(10,9,-.22),(28,25,.35),'Cream',.05)
for index,root in enumerate(roots):
    data=bpy.data.curves.new('Label','FONT'); data.body=root.name.removeprefix('CC_'); data.size=.23; data.align_x='CENTER'
    label=bpy.data.objects.new('Label_'+root.name,data); scene.collection.objects.link(label)
    label.location=(root.location.x,root.location.y-1.75,.005); data.materials.append(g.MATS['Dark'])

camera_data=bpy.data.cameras.new('ShowcaseCamera'); camera=bpy.data.objects.new('ShowcaseCamera',camera_data); scene.collection.objects.link(camera)
camera.location=(28,-31,37); target=Vector((10,8,0))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler(); camera_data.type='ORTHO'; camera_data.ortho_scale=31
scene.camera=camera
world=bpy.data.worlds.new('Colony studio'); scene.world=world; world.use_nodes=True
next(n for n in world.node_tree.nodes if n.type=='BACKGROUND').inputs['Color'].default_value=(.28,.32,.38,1)
for name,loc,power,size in [('Key',(3,-6,18),2400,12),('Fill',(20,12,15),1800,10)]:
    d=bpy.data.lights.new(name,'AREA'); o=bpy.data.objects.new(name,d); scene.collection.objects.link(o); o.location=loc; d.energy=power; d.shape='DISK'; d.size=size
    o.rotation_euler=(Vector((10,8,0))-o.location).to_track_quat('-Z','Y').to_euler()
scene.render.resolution_x=1800; scene.render.resolution_y=1400; scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(BASE/'previews'/'overview.png')
# Keep the existing Blender default engine; no guessed engine identifiers.
scene.render.film_transparent=False
bpy.ops.wm.save_as_mainfile(filepath=str(BASE/'colony-core.blend'))
print(json.dumps({'assets':len(manifest),'triangles':sum(x['triangles'] for x in manifest),'file':bpy.data.filepath}))

