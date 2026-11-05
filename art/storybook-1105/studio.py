import bpy,math
from pathlib import Path
from mathutils import Vector
OUT=Path(__file__).resolve().parent
def studio(name,target,campos,scale):
    sc=bpy.data.scenes.new(name);bpy.context.window.scene=sc
    sc.render.engine='BLENDER_EEVEE';sc.render.resolution_x=1500;sc.render.resolution_y=1000;sc.render.resolution_percentage=100
    sc.world=bpy.data.worlds.new(name+'World');sc.world.use_nodes=True
    bg=next(n for n in sc.world.node_tree.nodes if n.type=='BACKGROUND');bg.inputs[0].default_value=(.65,.7,.74,1);bg.inputs[1].default_value=.35
    bpy.ops.object.camera_add(location=campos);cam=bpy.context.object;cam.rotation_euler=(Vector(target)-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=scale;sc.camera=cam
    for pos,power,size in [((-4,-5,8),1100,5),((4,-1,6),650,4),((0,5,7),900,4)]:
        bpy.ops.object.light_add(type='AREA',location=pos);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler()
    m=bpy.data.materials.new('Studio warm paper');m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(.72,.68,.57,1);p.inputs['Roughness'].default_value=1
    bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(m)
    sc.render.image_settings.file_format='PNG';sc.render.filepath=str(OUT/(name+'.png'))
    return sc
def copy_static(source,dest,offset,scale=1,angle=0):
    # Render evaluated, currently visible meshes only; production files keep their rig.
    bpy.context.window.scene=source;deps=bpy.context.evaluated_depsgraph_get()
    for o in source.objects:
        if o.type!='MESH' or o.hide_render:continue
        mesh=bpy.data.meshes.new_from_object(o.evaluated_get(deps));copy=bpy.data.objects.new(o.name+'_Preview',mesh);dest.collection.objects.link(copy)
        copy.matrix_world=o.matrix_world
        for v in mesh.vertices:v.co=o.matrix_world@v.co
        copy.matrix_world.identity();copy.scale=(scale,)*3;copy.rotation_euler[2]=angle;copy.location=offset
    bpy.context.window.scene=dest
sc=studio('Character_Preview',(0,0,.66),(1.8,-6,2.4),3.1)
copy_static(bpy.data.scenes['Storybook_Settler'],sc,(-.62,0,0))
copy_static(bpy.data.scenes['Storybook_Settler'],sc,(.62,0,0),angle=-.65)
sc=studio('Buildings_Preview',(0,0,.8),(8,-12,10),12.4)
for name,offset in [('CC_Hut',(-2,-1.7,0)),('CC_House',(1.9,-1.7,0)),('CC_Storage',(-2,2,0)),('CC_Watchtower',(1.9,2,0))]:
    copy_static(bpy.data.scenes['Story_'+name],sc,offset,scale=.73)
copy_static(bpy.data.scenes['Storybook_Settler'],sc,(-.1,-2.2,0),scale=.8)
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'storybook-source.blend'))
print('Saved character and buildings studio scenes.')
