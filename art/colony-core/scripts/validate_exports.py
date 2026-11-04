"""FBX roundtrip validation in an isolated temporary scene; preserve the showcase."""
import bpy,json
from pathlib import Path
from mathutils import Vector
BASE=Path('E:/Git/ant/art/colony-core')
original=bpy.context.window.scene
temp=bpy.data.scenes.new('CC_Validation')
bpy.context.window.scene=temp
results=[]
try:
    for row in json.loads((BASE/'manifest.json').read_text()):
        bpy.ops.import_scene.fbx(filepath=str(BASE/row['fbx']))
        meshes=[o for o in temp.objects if o.type=='MESH']
        bpy.context.view_layer.update()
        pts=[o.matrix_world@Vector(v) for o in meshes for v in o.bound_box]
        size=[max(v[i] for v in pts)-min(v[i] for v in pts) for i in range(3)]
        assert len(meshes)==row['mesh_count'],row['id']+' meshes'
        assert all(abs(size[i]-row['dimensions_blender_xyz'][i])<.005 for i in range(3)),(row['id'],size)
        assert abs(min(v.z for v in pts))<.005,row['id']+' ground'
        assert all(len(o.data.materials)>0 for o in meshes),row['id']+' materials'
        assert all(not p.use_smooth for o in meshes for p in o.data.polygons),row['id']+' flat shading'
        assert not any(o.type in {'CAMERA','LIGHT'} for o in temp.objects)
        triangles=0
        for o in meshes: o.data.calc_loop_triangles(); triangles+=len(o.data.loop_triangles)
        assert triangles==row['triangles'],row['id']+' triangles'
        results.append({'id':row['id'],'passed':True,'triangles':triangles})
        for o in list(temp.objects): bpy.data.objects.remove(o,do_unlink=True)
finally:
    bpy.context.window.scene=original; bpy.data.scenes.remove(temp)
(BASE/'validation.json').write_text(json.dumps({'checks':'FBX roundtrip: dimensions, ground pivot, mesh count, materials, flat shading, triangles, no camera/light','unity_import_tested':False,'assets':results},indent=2),encoding='utf-8')
print('ROUNDTRIP_PASS',len(results))
