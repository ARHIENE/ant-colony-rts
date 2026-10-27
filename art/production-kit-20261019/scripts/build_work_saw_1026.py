import bpy,sys,shutil
from pathlib import Path
root=Path('E:/Git/ant');kit=root/'art/production-kit-20261019'
sys.path.insert(0,str(kit/'scripts'));import pk
bpy.ops.wm.read_factory_settings(use_empty=True)
col=pk.collection('Handheld');m=pk.Mesh('Tool_Saw')
m.box((0,-.025,0),(.032,.11,.055),'#7a5a34')
m.box((0,.018,0),(.038,.016,.07),'#3a2414')
profile=[(.04,-.035),(.35,-.035),(.35,.025),(.04,.025)]
# Blade is in Y/Z, thickness X; authored grip remains at zero.
from mathutils import Matrix
frame=Matrix(((0,0,1,0),(1,0,0,0),(0,1,0,0),(0,0,0,1)))
m.prism(profile,-.003,.003,'#b8c2ca',M=frame)
for i in range(15):
 y=.05+i*.019
 m.prism([(y,-.034),(y+.008,-.049),(y+.016,-.034)],-.003,.003,'#b8c2ca',M=frame)
ob=m.to_object(col,origin='keep');bpy.context.view_layer.objects.active=ob;ob.select_set(True)
bpy.ops.wm.save_as_mainfile(filepath=str(kit/'work-saw-1026.blend'))
out=kit/'exports/handheld/Tool_Saw.fbx'
bpy.ops.export_scene.fbx(filepath=str(out),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y')
shutil.copy2(out,root/'Assets/Resources/Kemono/Handheld/Tool_Saw.fbx')
print('BUILT_WORK_SAW',flush=True)
