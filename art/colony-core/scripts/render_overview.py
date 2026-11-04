import bpy,json,math
from pathlib import Path
from mathutils import Vector
base=Path('E:/Git/ant/art/colony-core')
scene=bpy.context.scene; camera=scene.camera
manifest=json.loads((base/'manifest.json').read_text())
subjects=[]
for row in manifest:
    root=bpy.data.objects[row['id']]
    subjects.extend(o for o in root.children_recursive if o.type=='MESH')
    subjects.append(bpy.data.objects['Label_'+row['id']])
bpy.context.view_layer.update()
projected=[camera.matrix_world.inverted()@(o.matrix_world@Vector(v)) for o in subjects for v in o.bound_box]
xmin,xmax=min(p.x for p in projected),max(p.x for p in projected)
ymin,ymax=min(p.y for p in projected),max(p.y for p in projected)
camera.location+=camera.rotation_euler.to_matrix()@Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
camera.data.ortho_scale=max(xmax-xmin,(ymax-ymin)*scene.render.resolution_x/scene.render.resolution_y)*1.12
if not bpy.data.objects.get('ShowcaseSun'):
    d=bpy.data.lights.new('ShowcaseSun','SUN'); o=bpy.data.objects.new('ShowcaseSun',d); scene.collection.objects.link(o)
    o.rotation_euler=(math.radians(25),math.radians(-20),math.radians(-25)); d.energy=1.5; d.angle=.35
scene.render.filepath=str(base/'previews/overview.png')
bpy.ops.render.render(write_still=True)
bpy.ops.wm.save_as_mainfile(filepath=str(base/'colony-core.blend'))
