"""Calibrate a constant grip rotation at the strike; tools remain bone-parented."""
from mathutils import Vector

def fit_tool(scene, tool):
    name=tool.name.split('.')[0]
    settings={'Tool_Axe':(20,(0,-1,-.25)), 'Tool_Pickaxe':(19,(0,-1,-.25)),
              'Tool_Hammer':(19,(0,-1,-.35)), 'Prop_Spear':(13,(0,-1,0))}
    if name not in settings or tool.get('grip_calibrated') == 2:return
    window=__import__('bpy').context.window_manager.windows[0]
    previous_scene=window.scene;window.scene=scene
    frame,direction=settings[name];old=scene.frame_current
    scene.frame_set(frame);scene.view_layers[0].update()
    world=tool.parent.matrix_world.to_3x3()@Vector(direction)
    local=tool.matrix_world.to_3x3().inverted()@world
    rotation=Vector((0,1,0)).rotation_difference(local.normalized())
    tool.matrix_basis=tool.matrix_basis@rotation.to_matrix().to_4x4()
    tool['grip_calibrated']=2
    scene.frame_set(old)
    window.scene=previous_scene
