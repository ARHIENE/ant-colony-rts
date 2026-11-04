"""Modular kemono assets: one origin, one posed-rest skeleton, separate slots."""
import bpy
import math
from pathlib import Path
from mathutils import Vector, Matrix

OUT = Path('E:/Git/ant/art/kemono-kit')
PALETTES = {'Ivory':(.82,.79,.76), 'Silver':(.47,.53,.62), 'Honey':(.62,.37,.15),
            'Rose':(.62,.29,.36), 'Lavender':(.43,.32,.59), 'Mint':(.28,.52,.43)}

def mat(name, rgb):
    m=bpy.data.materials.new(name);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*rgb,1);p.inputs['Roughness'].default_value=.8
    m.diffuse_color=(*rgb,1)
    return m

def copy_mesh(old, name, collection):
    source_scene=old.users_scene[0]
    with bpy.context.temp_override(scene=source_scene,view_layer=source_scene.view_layers[0]):
        dg=bpy.context.evaluated_depsgraph_get();evaluated=old.evaluated_get(dg)
        data=bpy.data.meshes.new_from_object(evaluated,preserve_all_data_layers=True,depsgraph=dg)
        data.transform(evaluated.matrix_world)
    o=bpy.data.objects.new(name,data);collection.objects.link(o)
    # new_from_object preserves deform indices; reconstruct their names.
    for g in old.vertex_groups:
        if g.name not in o.vertex_groups:o.vertex_groups.new(name=g.name)
    return o

def bind(o,rig,bone=None):
    if bone:
        o.vertex_groups.clear();g=o.vertex_groups.new(name=bone)
        g.add(list(range(len(o.data.vertices))),1,'REPLACE')
    mod=o.modifiers.new('KemonoSkin','ARMATURE');mod.object=rig
    o.parent=rig;o.matrix_parent_inverse=Matrix.Identity(4)

# Kiki bones that stick out of this chibi body: leaf tips, cloth physics chains, the unused accessory.
# Their weights move to MERGE targets (skirt → legs via the hem blend in parts.skirt_to_legs).
MERGE={'shoulder.L Dress':'shoulder.L','shoulder.R Dress':'shoulder.R','Accesstory':'spine_02'}
def dropped(name):return name.endswith('_end') or name.startswith(('Skirt','Accesstory')) or ' Dress' in name

def ear_axis(src,left):
    # Root = lowest tenth of the rabbit ear on that side, tip = highest tenth. Character left is +X (like upper_arm.L).
    vs=sorted((o.matrix_world@v.co for o in src.objects if o.name.startswith('Settler_Ear') and o.type=='MESH'
              for v in o.data.vertices if (v.co.x>0)==left),key=lambda v:v.z)
    n=max(1,len(vs)//10);return sum(vs[:n],Vector())/n,sum(vs[-n:],Vector())/n

def tail_root(src):
    # Where the bob tail meets the back: the front-most (lowest Y) tenth of its vertices.
    o=next(o for o in src.objects if o.name.startswith('Settler_Tail'))
    vs=sorted((o.matrix_world@v.co for v in o.data.vertices),key=lambda v:v.y)
    n=max(1,len(vs)//10);return sum(vs[:n],Vector())/n

def make_rig(ref,collection,src):
    data=bpy.data.armatures.new('KemonoSkeleton')
    rig=bpy.data.objects.new('KemonoRig',data);collection.objects.link(rig)
    bpy.context.view_layer.objects.active=rig;rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    root=data.edit_bones.new('Root');root.head=(0,0,0);root.tail=(0,0,.1)
    def pos(v):return Vector((v.x*1.10,v.y*1.06,v.z*.73))
    keep=[pb for pb in ref.pose.bones if not dropped(pb.name)]
    for pb in keep:
        b=data.edit_bones.new(pb.name);b.head=pos(ref.matrix_world@pb.head);b.tail=pos(ref.matrix_world@pb.tail)
        b.align_roll(ref.matrix_world.to_3x3()@pb.matrix.to_3x3().col[2])
    for pb in keep:
        data.edit_bones[pb.name].parent=data.edit_bones[pb.parent.name if pb.parent else 'Root']
    extras={
        'Ear.L':(*ear_axis(src,True),'head'),
        'Ear.R':(*ear_axis(src,False),'head'),
        'Tail':(tail_root(src),tail_root(src)+Vector((0,.16,0)),'pevis'),
        'Socket.Carry':((0,-.17,.52),(0,-.17,.64),'spine'),  # just in front of the chest
        'Socket.Hat':((0,0,1.17),(0,0,1.30),'head')}  # top of the hair
    for side in ['L','R']:
        hand=data.edit_bones['hand.'+side]
        palm=(hand.head+hand.tail)/2  # grip at the palm, not the wrist
        extras['Socket.Tool.'+side]=(palm,palm+Vector((0,-.12,0)),'hand.'+side)
    for name,(h,t,parent) in extras.items():
        b=data.edit_bones.new(name);b.head=h;b.tail=t;b.parent=data.edit_bones[parent]
        b.use_deform=not name.startswith('Socket.')
    bpy.ops.object.mode_set(mode='OBJECT')
    rig.select_set(False)
    for pb in rig.pose.bones:pb.rotation_mode='XYZ'
    rig['forward']='-Y';rig['fps']=24;rig['rig_type']='Generic'
    return rig

def face_transform(p):
    # Exact previous V1 → V2 → emoticon-space deformation, also applied to morph targets.
    p=Vector((p.x*1.23,p.y*1.23,(p.z-.97)*1.23+.7081))
    x,y,z=p;ax=abs(x);side=1 if x>=0 else -1
    if y<-.035:
        w=math.exp(-((ax-.079)/.061)**6-((z-.849)/.075)**6)
        p.x+=side*.006*w;p.z+=(z-.845)*.19*w
        cheek=math.exp(-((z-.785)/.047)**2-((ax-.105)/.060)**2)
        p.x+=side*.010*cheek;p.y-=.006*cheek
        if z<.785:
            q=min(1,max(0,(.785-z)/.076));p.z+=.019*q*q;p.x*=1+.045*q
        if ax<.035 and .753<z<.797:
            q=math.exp(-((z-.774)/.015)**4);p.x*=1+.18*q;p.z+=.008*(ax/.035)**1.4*q
    x,y,z=p;ax=abs(x);side=1 if x>=0 else -1
    if y<-.035:
        w=math.exp(-((ax-.085)/.061)**6-((z-.852)/.072)**6)
        p.x+=side*((ax-.085)*.06-.012)*w;p.z+=((z-.852)*.12-.006)*w
        cheek=math.exp(-((z-.787)/.039)**2-((ax-.111)/.045)**2)
        p.x+=side*.003*cheek;p.y-=.004*cheek
        if z<.792:
            q=min(1,max(0,(.792-z)/.064));p.z+=.008*q*q;p.x*=1+.02*q
    return p

def sphere(name,loc,scale,material,collection):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=loc)
    o=bpy.context.object;o.name=name;o.scale=scale
    bpy.ops.object.transform_apply(location=True,rotation=False,scale=True)
    for c in list(o.users_collection):c.objects.unlink(o)
    collection.objects.link(o);o.data.materials.append(material)
    for p in o.data.polygons:p.use_smooth=True
    return o

def stroke(name,points,radius,material,collection):
    curve=bpy.data.curves.new(name,'CURVE');curve.dimensions='3D';curve.bevel_depth=radius;curve.bevel_resolution=2;curve.use_fill_caps=True
    spl=curve.splines.new('POLY');spl.points.add(len(points)-1)
    for p,co in zip(spl.points,points):p.co=(*co,1)
    temp=bpy.data.objects.new(name,curve);collection.objects.link(temp)
    bpy.context.view_layer.update()
    o=copy_mesh(temp,name+'_mesh',collection);bpy.data.objects.remove(temp,do_unlink=True)
    o.data.materials.append(material)
    return o
