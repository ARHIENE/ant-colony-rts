"""Small reusable low-poly forms. Coordinates: metres, Z up, front -Y."""
import bpy, math
from mathutils import Vector

PALETTE = {
    'Bark':(.22,.105,.045), 'Wood':(.49,.285,.12), 'CutWood':(.72,.49,.23),
    'Leaf':(.24,.39,.105), 'LeafLight':(.43,.57,.19), 'Earth':(.36,.21,.105),
    'Soil':(.15,.095,.06), 'Stone':(.39,.43,.43), 'Cream':(.81,.75,.54),
    'Amber':(.9,.49,.10), 'Teal':(.16,.49,.43), 'Iron':(.22,.27,.29),
    'Fungus':(.74,.38,.20), 'White':(.89,.86,.72), 'Dark':(.045,.033,.023)
}
MATS={}
ROOT=None

def materialize():
    for name,color in PALETTE.items():
        m=bpy.data.materials.new('CC_'+name)
        m.diffuse_color=(*color,1)
        m.use_nodes=True
        p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        p.inputs['Base Color'].default_value=(*color,1)
        p.inputs['Roughness'].default_value=.8
        p.inputs['Metallic'].default_value=.25 if name=='Iron' else 0
        MATS[name]=m

def asset(name):
    global ROOT
    ROOT=bpy.data.objects.new(name,None)
    bpy.context.scene.collection.objects.link(ROOT)
    ROOT['asset_id']=name
    return ROOT

def finish(o,name,mat):
    o.name=name
    o.parent=ROOT
    o.data.materials.append(MATS[mat])
    bpy.context.view_layer.objects.active=o
    o.select_set(True)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    o.select_set(False)
    return o

def box(name,p,size,mat='Wood',bevel=.035):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cube_add(size=1,location=p)
    o=bpy.context.object
    o.scale=size
    finish(o,name,mat)
    if bevel:
        mod=o.modifiers.new('Edge cuts','BEVEL'); mod.width=bevel; mod.segments=1
        bpy.context.view_layer.objects.active=o
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def rod(name,a,b,r=.06,mat='Bark',vertices=7):
    a,b=Vector(a),Vector(b); d=b-a
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_cone_add(vertices=vertices,radius1=r,radius2=r*.88,depth=d.length,location=(a+b)/2)
    o=bpy.context.object; o.rotation_euler=d.to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat)

def rock(name,p,size,mat='Stone',sub=1):
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=sub,radius=1,location=p)
    o=bpy.context.object; o.scale=size
    return finish(o,name,mat)

def leaf(name,p,length=1,width=.45,angle=0,mat='Leaf'):
    # A folded, double-sided blade with a pronounced central rib.
    v=[(0,-length/2,0),(-width/2,0,.02),(0,0,.13),(width/2,0,.02),(0,length/2,0)]
    f=[(0,2,1),(0,3,2),(1,2,4),(2,3,4)]
    me=bpy.data.meshes.new(name); me.from_pydata(v,[],f); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    o.location=p; o.rotation_euler.z=angle
    finish(o,name,mat)
    mod=o.modifiers.new('Leaf thickness','SOLIDIFY'); mod.thickness=.025
    bpy.context.view_layer.objects.active=o; bpy.ops.object.modifier_apply(modifier=mod.name)
    return o

def bowl(name,p,r=.4,h=.3,mat='Wood'):
    # Closed wall cross-section: bottom, outside rim, inside rim, inner floor.
    n=10; verts=[]
    for radius,z in [(r*.68,0),(r,h),(r*.85,h),(r*.55,.07)]:
        verts.extend([(p[0]+radius*math.cos(i*math.tau/n),p[1]+radius*math.sin(i*math.tau/n),p[2]+z) for i in range(n)])
    faces=[]
    for k in range(3):
        for i in range(n):
            j=(i+1)%n; faces.append((k*n+i,k*n+j,(k+1)*n+j,(k+1)*n+i))
    faces.extend([tuple(reversed(range(n))),tuple(range(3*n,4*n))])
    me=bpy.data.meshes.new(name); me.from_pydata(verts,[],faces); me.update()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    return finish(o,name,mat)

def bench(width=1.8,depth=.9,height=.8):
    for x in [-width*.4,width*.4]:
        for y in [-depth*.35,depth*.35]: rod('Leg',(x,y,.02),(x,y,height),.09)
    for i in range(4): box('Plank',(0,(i-1.5)*depth/4,height),(width,depth/4-.02,.12))
    rod('Brace',(-width*.4,0,.25),(width*.4,0,.25),.06)

def lash(p):
    for z in [-.04,0,.04]: box('Fiber binding',(p[0],p[1],p[2]+z),(.19,.18,.025),'Cream',.005)

def bottle(p,mat='Teal'):
    rod('Medicine jar',p,(p[0],p[1],p[2]+.26),.10,mat,8)
    rod('Cork',(p[0],p[1],p[2]+.26),(p[0],p[1],p[2]+.32),.055,'CutWood',8)
