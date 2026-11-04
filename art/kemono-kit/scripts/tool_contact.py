"""Preview-only wrist IK, fixed grip orientation and compact safe wind-up."""
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

SETTINGS={'Tool_Axe':(20,15,Vector((0,.43,-.25))),
          'Tool_Pickaxe':(19,14,Vector((0,.38,-.29))),
          'Tool_Hammer':(19,14,Vector((0,.41,-.13)))}

def apply_contact(scene,tool):
    if tool.get('contact_ik'):return
    rig=tool.parent;name=tool.name.split('.')[0]
    window=bpy.context.window_manager.windows[0];previous=window.scene;window.scene=scene
    for bone in rig.pose.bones:
        if bone.name.startswith('f_') and bone.name.endswith('.R'):
            bone.rotation_mode='XYZ';bone.rotation_euler=(.8,0,0)
        elif bone.name.startswith('thumb.') and bone.name.endswith('.R'):
            bone.rotation_mode='XYZ';bone.rotation_euler.z=-.35
    if name not in SETTINGS:window.scene=previous;return
    hit,wind,edge=SETTINGS[name];scene.frame_set(hit);scene.view_layers[0].update()
    hand=rig.pose.bones['hand.R'];hand_world=rig.matrix_world@hand.matrix
    grip=hand_world.inverted()@tool.matrix_world
    base=tool.matrix_world.copy();point=base@edge
    # Long hair and low fixtures need the stroke beside the face, not under it.
    side_clearance={'Preview__CC_Lamp', 'Preview__CC_LeafWall', 'Preview__CC_OreOutcrop',
                    'Preview__CC_SpikeTrap', 'Swap__CC_Workshop__D'}
    if scene.name in side_clearance:
        side=rig.matrix_world.to_3x3()@Vector((-.25,0,0));point+=side
    deps=bpy.context.evaluated_depsgraph_get();nearest=None
    for obj in scene.objects:
        if obj.type!='MESH' or not obj.library or 'facility' not in obj.library.filepath:continue
        ev=obj.evaluated_get(deps);mesh=ev.to_mesh()
        tree=BVHTree.FromPolygons([ev.matrix_world@v.co for v in mesh.vertices],[list(p.vertices) for p in mesh.polygons])
        candidate=tree.find_nearest(point+Vector((0,0,.16)))
        if candidate and candidate[0] is not None and (nearest is None or candidate[3]<nearest[3]):nearest=candidate
        ev.to_mesh_clear()
    if nearest is None:window.scene=previous;return
    base.translation+=nearest[0]-(base@edge)
    target=bpy.data.objects.new(scene.name+'_GripTarget',None);scene.collection.objects.link(target)
    target.empty_display_size=.025;target.rotation_mode='QUATERNION'
    ik=rig.pose.bones['forearm.R'].constraints.new('IK');ik.name='ToolContact';ik.target=target;ik.chain_count=2;ik.use_stretch=False
    rotation=hand.constraints.new('COPY_ROTATION');rotation.name='ToolGrip';rotation.target=target
    right=rig.matrix_world.to_3x3()@Vector((-.16,0,0));right.normalize();right*=.16
    forward=rig.matrix_world.to_3x3()@Vector((0,-1,0));forward.normalize()
    height=.12 if scene.name in side_clearance else .28
    poses=[(1,Vector((0,0,.10))-forward*.08),(wind,Vector((0,0,height))+right-forward*.08),
           (hit,Vector()),(min(hit+3,scene.frame_end-2),Vector()),(scene.frame_end-7,Vector((0,0,.10))-forward*.08),(scene.frame_end,Vector((0,0,.10))-forward*.08)]
    for frame,offset in poses:
        desired=base.copy();desired.translation+=offset
        target.matrix_world=desired@grip.inverted()
        target.keyframe_insert('location',frame=frame);target.keyframe_insert('rotation_quaternion',frame=frame)
    tool['contact_ik']=True;tool['contact_point']=list(nearest[0]);tool['contact_frame']=hit;tool['contact_edge']=list(edge)
    scene.frame_set(hit);scene.view_layers[0].update()
    error=(target.matrix_world.translation-(rig.matrix_world@hand.matrix).translation).length
    if error>.01:
        # Keep both ankles planted while lowering the pelvis for low work surfaces.
        for side in 'LR':
            foot=rig.pose.bones['foot.'+side]
            ankle=bpy.data.objects.new(scene.name+'_Foot_'+side,None);scene.collection.objects.link(ankle)
            ankle.matrix_world=rig.matrix_world@foot.matrix
            leg=rig.pose.bones['shin.'+side].constraints.new('IK');leg.target=ankle;leg.chain_count=2;leg.use_stretch=False
            fc=foot.constraints.new('COPY_ROTATION');fc.target=ankle
        lower=bpy.data.objects.new(scene.name+'_CrouchOffset',None);scene.collection.objects.link(lower)
        pc=rig.pose.bones['pevis'].constraints.new('COPY_LOCATION');pc.target=lower;pc.use_x=False;pc.use_y=False;pc.use_z=True;pc.use_offset=True
        best=(error,0)
        for step in range(1,16):
            lower.location.z=-step*.025;scene.view_layers[0].update()
            gap=(target.matrix_world.translation-(rig.matrix_world@hand.matrix).translation).length
            if gap<best[0]:best=(gap,lower.location.z)
        lower.location.z=best[1];scene.view_layers[0].update()
        for attempt in range(12):
            delta=target.matrix_world.translation-(rig.matrix_world@hand.matrix).translation
            if delta.length<.008:break
            delta.z=0
            if delta.length<.001:break
            if delta.length>.08:delta.normalize();delta*=.08
            rig.location+=delta
            for side in 'LR':bpy.data.objects[scene.name+'_Foot_'+side].location+=delta
            scene.view_layers[0].update()
    scene.frame_set(1);window.scene=previous
