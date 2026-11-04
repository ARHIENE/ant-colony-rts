import bpy
import math
import numpy as np
from pathlib import Path
from mathutils import Vector

OUT=Path('E:/Git/ant/art/characters/kemono-kiki-v2')
OUT.mkdir(parents=True,exist_ok=True)
src=bpy.data.scenes['Kiki_Rabbit_Settler']
s=bpy.data.scenes.new('Rabbit_NewFace_V2')
mapping={}
materials={}
for old in src.objects:
    new=old.copy()
    if old.data:new.data=old.data.copy()
    s.collection.objects.link(new);mapping[old]=new
    if new.type=='MESH':
        for slot in new.material_slots:
            m=slot.material
            if m:
                if m not in materials:materials[m]=m.copy()
                slot.material=materials[m]
for old,new in mapping.items():
    world=old.matrix_world.copy()
    new.parent=mapping.get(old.parent)
    new.matrix_world=world
s.world=src.world.copy();s.camera=mapping[src.camera]
bpy.context.window.scene=s
face=next(o for o in s.objects if o.name.startswith('Settler_Face'))
hair=next(o for o in s.objects if o.name.startswith('Settler_Hair'))

# Transform eye socket, eyeball and lashes together to retain their fit.
for v in face.data.vertices:
    x,y,z=v.co
    ax=abs(x);side=1 if x>=0 else -1
    if y<-.035:
        weight=math.exp(-((ax-.079)/.061)**6-((z-.849)/.075)**6)
        v.co.x+=side*.006*weight
        v.co.z+=(z-.845)*.19*weight
        # Softer cheeks and a shorter, rounded lower face.
        cheek=math.exp(-((z-.785)/.047)**2)*math.exp(-((ax-.105)/.060)**2)
        v.co.x+=side*.010*cheek
        v.co.y-=.006*cheek
        if z<.785:
            w=min(1,max(0,(.785-z)/.076))
            v.co.z+=.019*w*w
            v.co.x*=1+.045*w
        # Raise the corners of the existing small mouth, retaining its cavity.
        if ax<.035 and .753<z<.797:
            w=math.exp(-((z-.774)/.015)**4)
            v.co.x*=1+.18*w
            v.co.z+=.008*(ax/.035)**1.4*w

# A new blue-green iris design replaces the recognizable gold source artwork.
n=512
yy,xx=np.mgrid[0:n,0:n].astype(float)
u=xx/(n-1);v=yy/(n-1)
r=np.sqrt(((u-.5)/.36)**2+((v-.5)/.44)**2)
pix=np.zeros((n,n,4),dtype=np.float32);pix[:,:,3]=1
base=np.clip((1-v)*1.3,0,1)[...,None]
pix[:,:,:3]=np.array((.10,.19,.25))*(1-base)+np.array((.25,.66,.68))*base
rim=np.clip((r-.85)/.13,0,1)[...,None]
pix[:,:,:3]=pix[:,:,:3]*(1-rim)+np.array((.07,.13,.20))*rim
def ellipse(cx,cy,rx,ry,color):
    q=((u-cx)/rx)**2+((v-cy)/ry)**2
    a=np.clip((1-q)*120,0,1)[...,None]
    pix[:,:,:3]=pix[:,:,:3]*(1-a)+np.array(color)*a
ellipse(.50,.52,.11,.22,(.035,.09,.14))
ellipse(.39,.72,.075,.085,(1,1,.96))
ellipse(.65,.31,.035,.04,(.75,1,.99))
im=bpy.data.images.new('RabbitV2_NewIris',width=n,height=n,alpha=True)
im.pixels.foreach_set(pix.ravel());im.filepath_raw=str(OUT/'iris.png');im.file_format='PNG';im.save();im.pack()
iris=face.data.materials[1]
for node in iris.node_tree.nodes:
    if node.type=='TEX_IMAGE':node.image=im

# Shift the fringe parting sideways, exposing more forehead and reducing the
# symmetrical drooping center fringe of the source hairstyle.
for vertex in hair.data.vertices:
    x,y,z=vertex.co
    if y<-.065 and z>.86:
        w=math.exp(-((x+.015)/.17)**4)*max(0,min(1,(z-.86)/.13))
        vertex.co.x+=.040*w
        if z<1.025:
            vertex.co.z+=.028*w
    # Open up the cheeks and keep a compact bob silhouette.
    if z<.86 and abs(x)>.12:
        vertex.co.z+=.018
        vertex.co.x*=1.025
for m in hair.data.materials:
    if not m or not m.use_nodes:continue
    nodes=m.node_tree.nodes;links=m.node_tree.links
    p=next(n for n in nodes if n.type=='BSDF_PRINCIPLED')
    tex=next((n for n in nodes if n.type=='TEX_IMAGE'),None)
    if tex:
        mix=nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=.55;mix.inputs[2].default_value=(.69,.68,.82,1)
        links.new(tex.outputs['Color'],mix.inputs[1]);links.new(mix.outputs[0],p.inputs['Base Color']);links.new(mix.outputs[0],p.inputs['Emission Color'])

s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True
s.render.resolution_x=900;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.render.image_settings.file_format='PNG';s.view_settings.exposure=.3
for name,loc in [('front',(0,-3,.88)),('three-quarter',(.95,-3,.98))]:
    s.camera.location=loc;s.camera.rotation_euler=(Vector((0,0,.75))-s.camera.location).to_track_quat('-Z','Y').to_euler()
    s.render.filepath=str(OUT/(name+'.png'));bpy.ops.render.render(write_still=True)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':area.spaces.active.region_3d.view_perspective='CAMERA';area.spaces.active.overlay.show_overlays=False
bpy.data.libraries.write(str(OUT/'rabbit-newface.blend'),{s},fake_user=True)
assert face.data is not next(o for o in src.objects if o.name.startswith('Settler_Face')).data
print('V2 copied independently: reshaped eyes/cheeks/chin/mouth, new iris and fringe, two renders saved.')
