"""Finish the silhouette and save the separate modeling study."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
HERE=Path(__file__).resolve().parent
scene=bpy.context.scene
root=next(o for o in scene.objects if o.name.startswith('Human_SD_V2'))
# Stagger the bob tips; a straight shell edge read as a helmet from behind.
shell=next(o for o in root.children if o.name.startswith('V2_HairShell'))
for v in shell.data.vertices:
    if v.co.y>-.10 and v.co.z<2.05:
        factor=min(1,(2.05-v.co.z)/.35)
        a=math.atan2(v.co.y-.15,v.co.x)
        v.co.z-=factor*(.13+.055*math.cos(a*9))
for o in root.children:
    if o.name.startswith('V2_Bob'):
        j=int(o.name.split('.')[0][-1]);s=-1 if 'BobL' in o.name else 1
        for v in o.data.vertices:
            t=max(0,min(1,(2.10-v.co.z)/.50))
            v.co.x+=s*.10*t*t
            v.co.z+=t*(j-1)*.045
scene.camera.location=(.30,-9,2.0)
scene.camera.rotation_euler=(Vector((0,0,1.65))-scene.camera.location).to_track_quat('-Z','Y').to_euler()
scene.render.filepath=str(HERE/'preview.png')
scene.render.resolution_x=1000;scene.render.resolution_y=1000
scene.render.film_transparent=False
# Preserve every pre-existing scene in this new file, including Claude's current work.
assert bpy.data.objects.get('TotoD') is not None
assert bpy.data.objects.get('Human_SD_Model') is not None
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.region_3d.view_perspective='CAMERA'
bpy.ops.wm.save_as_mainfile(filepath=str(HERE/'human-sd-v2.blend'))
meshes=[o for o in root.children_recursive if o.type=='MESH']
info={'scene':scene.name,'root':root.name,'mesh_objects':len(meshes),'rigged':False,'unity_imported':False,'preserved':['TotoD','Human_SD_Model']}
(HERE/'validation.json').write_text(json.dumps(info,ensure_ascii=False,indent=2),encoding='utf-8')
bpy.ops.render.render(write_still=True)
print(info)
