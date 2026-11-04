"""Render one asset category, then restore all showcase layout/visibility."""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
BASE=Path('E:/Git/ant/art/colony-core')
manifest=json.loads((BASE/'manifest.json').read_text())
categories={
 '01_nature':[r['id'] for r in manifest if r.get('recipe')=='models_nature'],
 '02_supplies':[r['id'] for r in manifest if r.get('recipe')=='models_supplies'],
 '03_construction':['CC_ConstructionPlan','CC_ConstructionFrame','CC_DemolitionRubble'],
 '04_crops':[r['id'] for r in manifest if r['id'].startswith('CC_CropFungus')]+['CC_FarmTile'],
 '05_living':[r['id'] for r in manifest if r.get('recipe')=='models_living'],
 '06_defense':[r['id'] for r in manifest if r.get('recipe')=='models_defense']}
scene=bpy.context.scene; camera=scene.camera
objects=list(scene.objects)
state={o:(o.location.copy(),o.rotation_euler.copy(),o.scale.copy(),o.hide_render) for o in objects}
render_state=(scene.render.resolution_x,scene.render.resolution_y,scene.render.filepath,camera.data.ortho_scale)
try:
    ids=categories[CATEGORY]; columns=min(4,len(ids)); rows=math.ceil(len(ids)/columns)
    sx,sy={'02_supplies':(1.6,2),'04_crops':(2.6,3.4),'05_living':(3.2,3.8)}.get(CATEGORY,(5,6))
    keep=set()
    for i,name in enumerate(ids):
        root=bpy.data.objects[name]; root.location=(i%columns*sx,i//columns*sy,0)
        keep.update([root,*root.children_recursive])
        label=bpy.data.objects['Label_'+name]; label.location=(root.location.x,root.location.y-sy*.34,.005); label.scale=(min(1,sx/3.5),)*3; keep.add(label)
    for o in objects:
        if o.type not in {'CAMERA','LIGHT'} and o.name!='Studio floor': o.hide_render=o not in keep
    center=Vector(((columns-1)*sx/2,(rows-1)*sy/2,0))
    floor=bpy.data.objects['Studio floor']; floor.location=(center.x,center.y,-.22); floor.dimensions=(columns*sx+3,rows*sy+3,.35)
    camera.location=center+Vector((10,-20,23)); camera.rotation_euler=(center-camera.location).to_track_quat('-Z','Y').to_euler(); camera.data.ortho_scale=max(columns*5+3,rows*6+4)
    for name,offset in [('Key',(-5,-7,15)),('Fill',(7,8,13))]:
        light=bpy.data.objects[name]; light.location=center+Vector(offset); light.rotation_euler=(center-light.location).to_track_quat('-Z','Y').to_euler()
    scene.render.resolution_x=1800; scene.render.resolution_y=1500; scene.render.filepath=str(BASE/'previews'/f'{CATEGORY}.png')
    bpy.context.view_layer.update()
    projected=[camera.matrix_world.inverted() @ (o.matrix_world @ Vector(v)) for o in keep if o.type in {'MESH','FONT'} for v in o.bound_box]
    xmin,xmax=min(p.x for p in projected),max(p.x for p in projected)
    ymin,ymax=min(p.y for p in projected),max(p.y for p in projected)
    camera.location+=camera.rotation_euler.to_matrix() @ Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
    camera.data.ortho_scale=max(xmax-xmin,(ymax-ymin)*1.2)*1.16
    bpy.ops.render.render(write_still=True)
    print('RENDERED',CATEGORY,len(ids))
finally:
    for o,(loc,rot,scale,hidden) in state.items(): o.location=loc; o.rotation_euler=rot; o.scale=scale; o.hide_render=hidden
    scene.render.resolution_x,scene.render.resolution_y,scene.render.filepath,camera.data.ortho_scale=render_state
