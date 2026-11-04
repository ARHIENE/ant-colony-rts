"""Small low-poly forms, Z up, front -Y. Independent of the facility pack."""
import bpy, math
from mathutils import Vector

def material(name, color):
    m=bpy.data.materials.new(name); m.diffuse_color=(*color,1); m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*color,1)
    p.inputs['Roughness'].default_value=.82
    return m

def mesh(name, verts, faces, mat, parent=None, smooth=False):
    data=bpy.data.meshes.new(name); data.from_pydata(verts,[],faces); data.update()
    o=bpy.data.objects.new(name,data); bpy.context.collection.objects.link(o)
    o.parent=parent; data.materials.append(mat)
    for p in data.polygons: p.use_smooth=smooth
    return o

def rings(name, sections, mat, parent, sides=24, smooth=True):
    # Sections are centre and ellipse radii in the XY plane.
    verts=[]
    for x,y,z,rx,ry in sections:
        verts.extend((x+rx*math.cos(2*math.pi*j/sides),y+ry*math.sin(2*math.pi*j/sides),z) for j in range(sides))
    faces=[tuple(reversed(range(sides)))]
    for i in range(len(sections)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides
            faces.append((a,b,b+sides,a+sides))
    faces.append(tuple((len(sections)-1)*sides+j for j in range(sides)))
    return mesh(name,verts,faces,mat,parent,smooth)

def oval(name, loc, scale, mat, parent, segments=20, rings_count=10):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments,ring_count=rings_count,location=loc)
    o=bpy.context.object; o.name=name; o.scale=scale; o.parent=parent
    o.data.materials.append(mat)
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    for p in o.data.polygons:p.use_smooth=True
    return o

def tube(name, points, radius, mat, parent, sides=8):
    verts=[]
    for i,p in enumerate(points):
        tangent=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])
        tangent.normalize(); side=tangent.cross(Vector((0,1,0)))
        if side.length<.01:side=tangent.cross(Vector((1,0,0)))
        side.normalize(); other=tangent.cross(side).normalized()
        verts.extend(tuple(Vector(p)+radius*(side*math.cos(j*2*math.pi/sides)+other*math.sin(j*2*math.pi/sides))) for j in range(sides))
    faces=[tuple(reversed(range(sides)))]
    for i in range(len(points)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.append(tuple((len(points)-1)*sides+j for j in range(sides)))
    return mesh(name,verts,faces,mat,parent,True)

def bent_ear(name, sections, mat, parent, sides=16):
    verts=[]
    for i,(x,y,z,width,depth) in enumerate(sections):
        a=Vector(sections[max(0,i-1)][:3]); b=Vector(sections[min(len(sections)-1,i+1)][:3])
        t=(b-a).normalized(); side=Vector((t.z,0,-t.x)).normalized()
        forward=t.cross(side).normalized()
        for j in range(sides):
            angle=2*math.pi*j/sides
            verts.append(tuple(Vector((x,y,z))+side*width*math.cos(angle)+forward*depth*math.sin(angle)))
    faces=[tuple(reversed(range(sides)))]
    for i in range(len(sections)-1):
        for j in range(sides):
            a=i*sides+j;b=i*sides+(j+1)%sides;faces.append((a,b,b+sides,a+sides))
    faces.append(tuple((len(sections)-1)*sides+j for j in range(sides)))
    return mesh(name,verts,faces,mat,parent,True)

def face_patch(name, body, x,z,rx,rz,mat,parent):
    points=[(x,z)]
    for ring in [.33,.67,1]:
        points.extend((x+rx*ring*math.cos(j*math.tau/24),z+rz*ring*math.sin(j*math.tau/24)) for j in range(24))
    verts=[]
    for px,pz in points:
        ok,hit,normal,index=body.ray_cast(Vector((px,-2,pz)),Vector((0,1,0)))
        assert ok, 'Face patch outside body'
        verts.append((px,hit.y-.008,pz))
    faces=[(0,1+j,1+(j+1)%24) for j in range(24)]
    for ring in range(2):
        for j in range(24):
            a=1+ring*24+j;b=1+ring*24+(j+1)%24;faces.append((a,a+24,b+24,b))
    return mesh(name,verts,faces,mat,parent,True)
