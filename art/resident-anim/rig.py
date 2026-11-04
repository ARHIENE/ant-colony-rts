"""Shared short-limbed Generic rig. Mesh copies preserve original concept boards."""
import bpy,math
from mathutils import Vector
BONES={
 'Root':((0,0,0),(0,0,.20),None),
 'Body':((0,0,.30),(0,0,.78),'Root'),
 'Head':((0,0,.80),(0,0,1.70),'Body'),
 'Arm.L':((-.30,-.12,.72),(-.43,-.42,.65),'Body'),
 'Arm.R':((.30,-.12,.72),(.43,-.42,.65),'Body'),
 'Hand.L':((-.43,-.42,.65),(-.43,-.42,.48),'Arm.L'),
 'Hand.R':((.43,-.42,.65),(.43,-.42,.48),'Arm.R'),
 'Foot.L':((-.27,0,.26),(-.27,-.13,.10),'Root'),
 'Foot.R':((.27,0,.26),(.27,-.13,.10),'Root'),
 'Ear.L':((-.50,0,1.95),(-.78,0,1.56),'Head'),
 'Ear.R':((.50,0,1.95),(.78,0,1.56),'Head'),
 'Tail':((0,.32,.30),(0,.54,.36),'Body')}
def make(scene,source,name):
    arm=bpy.data.armatures.new(name+'_Skeleton');rig=bpy.data.objects.new(name,arm);scene.collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
    for n,(h,t,parent) in BONES.items():
        b=arm.edit_bones.new(n);b.head=h;b.tail=t
        if parent:b.parent=arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode='OBJECT')
    for pb in rig.pose.bones:pb.rotation_mode='XYZ'
    for original in source.children:
        if original.type!='MESH':continue
        # Equipment is separate from the common body rig and attached by the work scene.
        if original.name.startswith(('Staff','Spear','RoundShield','Shield','Basket','Berry','Book','Watering','Spout','Handle','FoldedCloth','Stem')):continue
        o=original.copy();o.data=original.data.copy();scene.collection.objects.link(o);o.parent=rig;o.name=name+'__'+original.name
        o.animation_data_clear()
        for group in list(o.vertex_groups):o.vertex_groups.remove(group)
        for n in BONES:o.vertex_groups.new(name=n)
        mat=o.matrix_local.copy()
        is_body=original.name.startswith('V4_Body')
        if original.name.startswith('V4_Paw'):bind='Hand.L' if original.location.x<0 else 'Hand.R'
        elif original.name.startswith('V4_Foot'):bind='Foot.L' if original.location.x<0 else 'Foot.R'
        elif original.name.startswith('Tail'):bind='Tail'
        elif original.name.startswith(('Satchel','V4_Strap','ShortCape','Collar')):bind='Body'
        elif original.name.startswith(('EarFlop','EarPoint','EarRound','EarInner')):
            cx=sum((mat@v.co).x for v in o.data.vertices)/len(o.data.vertices);bind='Ear.L' if cx<0 else 'Ear.R'
        else:bind='Head'
        for v in o.data.vertices:
            if is_body:
                z=(mat@v.co).z;t=max(0,min(1,(z-.66)/.32));t=t*t*(3-2*t)
                if t<1:o.vertex_groups['Body'].add([v.index],1-t,'REPLACE')
                if t>0:o.vertex_groups['Head'].add([v.index],t,'REPLACE')
            else:o.vertex_groups[bind].add([v.index],1,'REPLACE')
        modifier=o.modifiers.new('Resident deform','ARMATURE');modifier.object=rig
        o['source_part']=original.name
    # Constraint sockets use bone-head coordinates, avoiding Blender bone-tail parenting offsets.
    for n,bone,loc in [('ToolGrip.R','Hand.R',(.43,-.49,.63)),('ToolGrip.L','Hand.L',(-.43,-.49,.63)),('CarryAnchor','Body',(0,-.68,.56))]:
        e=bpy.data.objects.new(name+'__'+n,None);scene.collection.objects.link(e);e.parent=rig
        c=e.constraints.new('COPY_TRANSFORMS');c.target=rig;c.subtarget=bone
        e['socket']=n;e['rest_world_hint']=list(loc)
    rig['forward']='-Y';rig['rig_type']='Generic';rig['note']='Short mitten limbs; no fingers or humanoid retargeting'
    return rig
