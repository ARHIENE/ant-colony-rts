import bpy, math, json
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path('F:/UnityProjects/ant-colony-rts')
OUT=ROOT/'art/meshy-equipment-1105/export'
OUT.mkdir(parents=True,exist_ok=True)
scene=bpy.data.scenes.new('Meshy_Equipment_Exports')
bpy.context.window.scene=scene
sources={}
for p in (ROOT/'art/meshy-equipment-1105/downloads').rglob('*.glb'):
    bits=p.parent.name.split('_')
    if len(bits)>2 and bits[2].isdigit():sources[int(bits[2])]=p
for p in (ROOT/'MESHY').glob('*texture.glb'):
    bits=p.name.split('_')
    if len(bits)>2 and bits[2].isdigit():sources[int(bits[2])]=p

# Socket convention inherited from shipped handhelds: blade/tool axis +Y,
# blade flat across X; bow lies in YZ, shield lies in XY facing +Z.
spec={
1:('Weapon_Sword_Copper',.54,'sword'),2:('Weapon_Spear_Copper',.95,'spear'),
3:('Weapon_ShortBow',.62,'bow'),4:('Weapon_Shield_Copper',.36,'shield'),
5:('Weapon_AcidSprayer',.43,'gun'),6:('Weapon_CommandStaff',.65,'staff'),
7:('Weapon_SingleRifle',.76,'gun'),8:('Weapon_BurstCarbine',.63,'gun'),9:('Weapon_Scattergun',.62,'gun'),
10:('Wear_LeafHelmet',.40,'wear'),11:('Wear_PlateArmor',.40,'wear'),12:('Wear_FlightPack',.50,'wear'),
13:('Wear_Anklet',.11,'wear'),14:('Wear_Goggles',.29,'wear'),15:('Wear_ScentPouch',.15,'wear'),
16:('Wear_Insignia',.08,'wear'),17:('Tool_Axe_Stone',.48,'tool'),18:('Tool_Pickaxe_Stone',.50,'tool'),
19:('Tool_Shovel_Stone',.65,'tool'),20:('Tool_Hammer_Stone',.34,'tool'),21:('Tool_Hoe_Stone',.58,'tool'),22:('Tool_Sickle_Stone',.36,'tool'),
23:('Wear_BossA',.43,'wear'),24:('Wear_BossB',.40,'wear'),25:('Weapon_BossC_Quakehammer',.75,'tool'),
26:('Weapon_Katana',.72,'sword'),27:('Weapon_GrandBow',.82,'bow'),28:('Weapon_SniperRifle',.92,'gun')}
report=[]
for ident,p in sorted(sources.items()):
    if ident not in spec:continue
    name,length,kind=spec[ident]
    bpy.ops.object.select_all(action='DESELECT')
    bpy.ops.import_scene.gltf(filepath=str(p))
    meshes=[o for o in bpy.context.selected_objects if o.type=='MESH']
    if not meshes:raise RuntimeError('No mesh '+str(p))
    for o in bpy.context.selected_objects:o.select_set(o.type=='MESH')
    bpy.context.view_layer.objects.active=meshes[0]
    if len(meshes)>1:bpy.ops.object.join()
    o=bpy.context.object;o.name=name;o.data.name=name
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    # This source was generated with its hilt above the blade. Normalize before grip placement.
    if ident==26:
        for v in o.data.vertices:v.co.y=-v.co.y;v.co.z=-v.co.z
    lo=Vector(tuple(min(v.co[i] for v in o.data.vertices) for i in range(3)))
    hi=Vector(tuple(max(v.co[i] for v in o.data.vertices) for i in range(3)))
    size=hi-lo;center=(lo+hi)*.5
    if kind=='gun':
        # Meshy reference presents muzzle to image-left (-X), grip below rear.
        grip=Vector((lo.x+size.x*.76,center.y,lo.z+size.z*.26))
        rot=Matrix(((0,1,0),(0,0,1),(-1,0,0)))
        scale=length/size.x
    elif kind=='bow':
        grip=center.copy()
        rot=Matrix(((0,1,0),(0,0,1),(1,0,0)))
        scale=length/size.z
    elif kind=='shield':
        grip=Vector((center.x,hi.y,center.z));rot=Matrix(((1,0,0),(0,0,1),(0,-1,0)))
        scale=length/max(size.x,size.z)
    elif kind=='wear':
        grip=center;rot=Matrix.Identity(3);scale=length/max(size)
    else:
        grip=Vector((center.x,center.y,lo.z+size.z*({'sword':.18,'spear':.46,'staff':.28,'tool':.26}[kind])))
        rot=Matrix.Rotation(-math.pi/2,3,'X');scale=length/size.z
    for v in o.data.vertices:v.co=(rot@(v.co-grip))*scale
    for f in o.data.polygons:f.use_smooth=True
    materials=[]
    for mi,mat in enumerate(o.data.materials):
        if mat is None:continue
        mat=mat.copy();o.data.materials[mi]=mat;mat.name='MeadowEQ_'+str(ident).zfill(2)+'_'+str(mi)
        bsdf=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
        if bsdf:bsdf.inputs['Roughness'].default_value=.82;bsdf.inputs['Metallic'].default_value=0
        images=[n.image for n in mat.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
        texture=''
        if images:
            image=images[0];texture=mat.name+'.png';image.filepath_raw=str(OUT/texture);image.file_format='PNG';image.save()
        materials.append({'name':mat.name,'texture':texture})
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,apply_scale_options='FBX_SCALE_ALL',axis_forward='-Z',axis_up='Y',add_leaf_bones=False,bake_anim=False)
    report.append({'id':ident,'name':name,'kind':kind,'source':str(p),'materials':materials,'triangles':sum(len(f.vertices)-2 for f in o.data.polygons),'size':list(o.dimensions)})
    o.location.x=(ident-1)%7*1.2;o.location.z=2-(ident-1)//7*1.2
(OUT/'manifest.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT.parent/'equipment.blend'))
print('Exported',len(report),'Meshy assets',[(r['name'],r['triangles']) for r in report])
