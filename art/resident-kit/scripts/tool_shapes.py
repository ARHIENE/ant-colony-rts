"""Socket-local tools: grip at zero, shaft along +Y; preserve object identities."""
import bpy
import math

def repair_tools():
    repaired=[]
    for name in ('Tool_Axe','Tool_Hammer','Tool_Pickaxe','Prop_Spear'):
        obj=bpy.data.objects.get(name)
        if obj is None: continue
        verts=[];faces=[];materials=[]
        def prism(points, thickness, material):
            start=len(verts);n=len(points)
            verts.extend((x,y,z) for x in (-thickness/2,thickness/2) for y,z in points)
            faces.extend([tuple(start+i for i in reversed(range(n))),tuple(start+n+i for i in range(n))])
            materials.extend([material,material])
            for i in range(n):
                j=(i+1)%n;faces.append((start+i,start+j,start+n+j,start+n+i));materials.append(material)
        def shaft(low,high,r):
            start=len(verts);n=10
            verts.extend((r*math.cos(i*2*math.pi/n),y,r*math.sin(i*2*math.pi/n)) for y in (low,high) for i in range(n))
            faces.extend([tuple(start+i for i in reversed(range(n))),tuple(start+n+i for i in range(n))]);materials.extend([0,0])
            for i in range(n):
                j=(i+1)%n;faces.append((start+i,start+j,start+n+j,start+n+i));materials.append(0)
        if name=='Tool_Axe':
            shaft(-.20,.53,.025)
            prism([(.37,-.015),(.50,-.015),(.57,-.25),(.30,-.25)],.035,1)
        elif name=='Tool_Hammer':
            shaft(-.17,.43,.026)
            prism([(.35,-.13),(.47,-.13),(.47,.13),(.35,.13)],.12,1)
        elif name=='Tool_Pickaxe':
            shaft(-.20,.53,.025)
            prism([(.38,-.29),(.52,-.11),(.55,0),(.52,.11),(.38,.29),(.45,.08),(.47,0),(.45,-.08)],.035,1)
        else:
            shaft(-.43,.83,.025)
            prism([(.79,-.015),(.86,-.075),(1.10,0),(.86,.075),(.79,.015)],.028,1)
        mesh=bpy.data.meshes.new(name+'_GripAligned');mesh.from_pydata(verts,[],faces);mesh.update()
        for matname,color in [('Tool_Wood',(.36,.20,.09,1)),('Tool_Metal',(.36,.44,.46,1))]:
            mat=bpy.data.materials.get(matname) or bpy.data.materials.new(matname);mat.diffuse_color=color;mat.use_nodes=True
            bsdf=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bsdf.inputs['Base Color'].default_value=color;bsdf.inputs['Roughness'].default_value=.7
            mesh.materials.append(mat)
        for p,index in zip(mesh.polygons,materials):p.material_index=index
        old=obj.data
        obj.data=mesh
        if old.users==0:bpy.data.meshes.remove(old)
        existing=bpy.data.meshes.get(name)
        if existing and existing.users==0:bpy.data.meshes.remove(existing)
        mesh.name=name  # Linked preview copies reference the mesh datablock by this name.
        obj['grip_origin']='shaft center at local (0,0,0)';repaired.append(obj)
    return repaired
