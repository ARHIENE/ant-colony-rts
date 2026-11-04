import bpy
import bmesh
from pathlib import Path
from mathutils import Vector

OUT=Path('E:/Git/ant/art/characters/kemono-kiki-v1')
s=bpy.context.scene
o=s.objects['Settler_Body'];me=o.data
adj={v.index:set() for v in me.vertices}
for e in me.edges:
    a,b=e.vertices;adj[a].add(b);adj[b].add(a)
seen=set();remove=set();garment=set();legs=set()
for start in adj:
    if start in seen:continue
    stack=[start];seen.add(start);ids=[]
    while stack:
        n=stack.pop();ids.append(n)
        for j in adj[n]:
            if j not in seen:seen.add(j);stack.append(j)
    vs=[me.vertices[j].co for j in ids]
    lo=min(v.z for v in vs);hi=max(v.z for v in vs)
    # Only the copied seasonal collar and dangling decorations are removed.
    if len(ids)==387 or (.56<lo and hi<.64):remove.update(ids)
    elif len(ids)==854:garment.update(ids)
    elif lo<.0:legs.update(ids)

for name in ['Settler_Blue','Settler_Linen','Settler_Leather']:
    me.materials.append(bpy.data.materials[name])
blue,linen,leather=range(len(me.materials)-3,len(me.materials))
for p in me.polygons:
    ids=set(p.vertices)
    center=sum((me.vertices[i].co for i in ids),Vector())/len(ids)
    if ids.issubset(garment):p.material_index=blue
    elif ids.issubset(legs) and center.z<.205:p.material_index=leather
for i in legs:
    v=me.vertices[i]
    if v.co.z<.09:
        footcenter=.065 if v.co.x>0 else -.065
        v.co.x=footcenter+(v.co.x-footcenter)*1.18
bm=bmesh.new();bm.from_mesh(me);bm.verts.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[bm.verts[i] for i in remove],context='VERTS')
bm.to_mesh(me);bm.free();me.update()
for im in bpy.data.images:
    if im.packed_file:continue
    if im.filepath and 'Xmas Girl' in im.filepath:
        try:im.pack()
        except RuntimeError:pass
cam=s.camera
for name,loc in [('front',(0,-3,.88)),('three-quarter',(.95,-3,.98)),('game-angle',(1.8,-3,2.5))]:
    cam.location=loc
    cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(OUT/(name+'.png'))
    bpy.ops.render.render(write_still=True)
cam.location=(.95,-3,.98);cam.rotation_euler=(Vector((0,0,.75))-cam.location).to_track_quat('-Z','Y').to_euler()
bpy.data.libraries.write(str(OUT/'rabbit-settler.blend'),{s},fake_user=True)
assert all(x not in [ob.name for ob in s.objects] for x in ['Hat_mesh.001'])
print('Static study saved, seasonal collar removed, three camera angles rendered; no game rig exported.')
