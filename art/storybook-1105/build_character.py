"""New storybook settler meshes, retaining the production skeleton and sockets.
Run in Blender. The original FBX is archived once before any export.
"""
import bpy, bmesh, math, json, shutil
from pathlib import Path
from mathutils import Vector, Matrix

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
OUT.mkdir(exist_ok=True)
SOURCE=ROOT/'Assets/Resources/Kemono/Kemono_Character.fbx'
BACKUP=OUT/'original-character.fbx'
if not BACKUP.exists(): shutil.copy2(SOURCE,BACKUP)
scene=bpy.data.scenes.new('Storybook_Settler')
bpy.context.window.scene=scene
bpy.ops.import_scene.fbx(filepath=str(BACKUP))
rig=next(o for o in scene.objects if o.type=='ARMATURE')
rig.name='KemonoRig'
bone_snapshot={b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}
oldparts=[o for o in scene.objects if o.type=='MESH']
options={slot:sorted(set(o.name.split('_')[2] for o in oldparts if o.name.startswith('Part_'+slot+'_'))) for slot in ['Hair','Ears','Tail','Face']}
for o in oldparts: bpy.data.objects.remove(o,do_unlink=True)
rig.animation_data_clear()
for p in rig.pose.bones: p.matrix_basis=Matrix.Identity(4)

def material(name,col):
    m=bpy.data.materials.new('Story_'+name);m.diffuse_color=(*col,1);m.use_nodes=True
    p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
    p.inputs['Base Color'].default_value=(*col,1);p.inputs['Roughness'].default_value=.86
    return m
skin=material('WarmClay',(.88,.73,.57)); cream=material('OatLinen',(.73,.64,.46))
hair=material('SilverHair',(.47,.54,.61)); ink=material('Cocoa',(.025,.019,.019))
rose=material('InnerTerracotta',(.72,.39,.34)); green=material('Sage',(.23,.34,.20))
leather=material('Leather',(.17,.085,.04)); white=material('Milk',(.92,.86,.68))
metal=material('Pewter',(.25,.29,.27))

def finish(o,name,mat,bone):
    o.name=name;o.data.materials.clear();o.data.materials.append(mat)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True)
    bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
    if bone:
        g=o.vertex_groups.new(name=bone);g.add(list(range(len(o.data.vertices))),1,'REPLACE')
        o.parent=rig;o.matrix_parent_inverse=rig.matrix_world.inverted()
        mod=o.modifiers.new('Skin','ARMATURE');mod.object=rig
    for f in o.data.polygons:f.use_smooth=True
    return o

def oval(name,pos,scale,mat,bone='head',rot=None):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=pos)
    o=bpy.context.object;o.scale=scale
    if rot:o.rotation_euler=rot
    return finish(o,name,mat,bone)

def tube(name,points,radius,mat,bone='head'):
    cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=radius;cu.bevel_resolution=2;cu.use_fill_caps=True
    sp=cu.splines.new('BEZIER');sp.bezier_points.add(len(points)-1)
    for b,p in zip(sp.bezier_points,points):b.co=p;b.handle_left_type=b.handle_right_type='AUTO'
    o=bpy.data.objects.new(name,cu);scene.collection.objects.link(o)
    bpy.ops.object.select_all(action='DESELECT');o.select_set(True);bpy.context.view_layer.objects.active=o;bpy.ops.object.convert(target='MESH')
    return finish(o,name,mat,bone)

def limb(name,bone,r1,r2,mat):
    b=rig.data.bones[bone];a=rig.matrix_world@b.head_local;c=rig.matrix_world@b.tail_local
    mid=(a+c)*.5;length=(c-a).length
    bpy.ops.mesh.primitive_uv_sphere_add(segments=16,ring_count=10,location=mid)
    o=bpy.context.object;o.scale=(r1,r2,length*.64);o.rotation_euler=(c-a).to_track_quat('Z','Y').to_euler()
    return finish(o,name,mat,bone)

# Plump cloth silhouette and mittens replace the source dress and fingers.
# One continuous tunic avoids intersecting spheres and the jagged colour seam.
sections=[(.335,.105,.078),(.35,.145,.103),(.42,.155,.112),(.51,.144,.108),(.59,.125,.096),(.65,.096,.074),(.675,.057,.05)]
vertices=[];faces=[];n=24
for z,rx,ry in sections:
    for i in range(n):
        a=i*2*math.pi/n;vertices.append((rx*math.cos(a),ry*math.sin(a),z))
for j in range(len(sections)-1):
    for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,b,b+n,a+n))
faces.extend([tuple(reversed(range(n))),tuple((len(sections)-1)*n+i for i in range(n))])
me=bpy.data.meshes.new('Tunic');me.from_pydata(vertices,[],faces);me.update()
o=bpy.data.objects.new('Part_Body_Core_00',me);scene.collection.objects.link(o);finish(o,o.name,green,'spine_01')
g=o.vertex_groups.new(name='pevis')
for v in o.data.vertices:
    t=max(0,min(1,(v.co.z-.42)/.16));o.vertex_groups['spine_01'].add([v.index],t,'REPLACE');g.add([v.index],1-t,'REPLACE')
oval('Part_Body_Head_00',(0,-.018,1.0),(.28,.215,.315),skin)
oval('Part_Body_Neck_00',(0,0,.69),(.043,.04,.038),skin,'neck')
oval('Part_Outfit_LINEN_01',(0,-.105,.49),(.093,.018,.11),cream,'pevis')
for z in [.535,.49]:oval('Part_Outfit_LINEN_Button',(0,-.126,z),(.012,.008,.012),leather,'pevis')
tube('Part_Outfit_LINEN_Collar',[(-.1,-.065,.654),(0,-.105,.62),(.1,-.065,.654)],.017,cream,'spine_02')
for side in ['L','R']:
    s=1 if side=='L' else -1
    limb('Part_Body_Sleeve_'+side,'upper_arm.'+side,.067,.06,green)
    limb('Part_Body_Cuff_'+side,'forearm.'+side,.049,.047,cream)
    oval('Part_Body_Mitten_'+side,(s*.249,.008,.402),(.04,.035,.047),skin,'hand.'+side)
    limb('Part_Body_Trouser_'+side,'thigh.'+side,.057,.054,cream)
    limb('Part_Body_Gaiter_'+side,'shin.'+side,.048,.043,green)
    oval('Part_Body_Boot_'+side,(s*.052,-.029,.047),(.053,.084,.052),leather,'foot.'+side)

# A smooth cropped cap and a few deliberate swept locks; no reused source hair.
for hi,opt in enumerate(options['Hair']):
    prefix='Part_Hair_'+opt+'_'
    # Open lower hemisphere prevents a second ball behind the face.
    verts=[];faces=[];rings=9;n=24
    for j in range(rings+1):
        for i in range(n):
            a=2*math.pi*i/n
            edge=1.88-.98*max(0,-math.sin(a))**2
            theta=.02+(edge-.02)*j/rings
            z=1.0+.325*math.cos(theta)
            verts.append((.294*math.sin(theta)*math.cos(a),-.018+.23*math.sin(theta)*math.sin(a),z))
    for j in range(rings):
        for i in range(n):a=j*n+i;b=j*n+(i+1)%n;faces.append((a,a+n,b+n,b))
    me=bpy.data.meshes.new(prefix);me.from_pydata(verts,[],faces);me.update()
    o=bpy.data.objects.new(prefix+'00',me);scene.collection.objects.link(o);finish(o,o.name,hair,'head')
    for k in range(5):
        x=-.18+k*.09
        y=-.018-.20*math.sqrt(max(.1,1-(x/.3)**2))
        verts=[];faces=[];slices=[(.12,.003),(.09,.038),(.04,.055),(-.04,.047),(-.09,.024),(-.112,.002)]
        for z,radius in slices:
            for i in range(16):
                a=2*math.pi*i/16
                verts.append((x+radius*math.cos(a)+z*.14,y+radius*.65*math.sin(a),1.19+abs(x)*.10+z))
        for j in range(len(slices)-1):
            for i in range(16):a=j*16+i;b=j*16+(i+1)%16;faces.append((a,b,b+16,a+16))
        me=bpy.data.meshes.new('Swept bang');me.from_pydata(verts,[],faces);me.update()
        o=bpy.data.objects.new(prefix+str(k+1).zfill(2),me);scene.collection.objects.link(o);finish(o,o.name,hair,'head')
    for s in [-1,1]:
        oval(prefix+'Side'+str(s),(s*.265,-.055,1.008),(.054,.13,.16 if hi%3==0 else .20),hair,'head',(0,s*-.12,0))
    if hi%3==1:
        for s in [-1,1]:oval(prefix+'Bun'+str(s),(s*.27,.08,1.18),(.09,.086,.082),hair)
    if hi%3==2:oval(prefix+'Back',(0,.19,1.1),(.12,.09,.16),hair)

# User-selected HUD reference: curved ant antennae, no mammal ears.
for ei,opt in enumerate(options['Ears']):
    for side in ['L','R']:
        s=1 if side=='L' else -1
        height=1.50+ei%3*.035
        points=[(s*.115,.015,1.28),(s*.14,.018,height-.07),(s*.21,.005,height),(s*.255,-.02,height-.035)]
        tube('Part_Ears_'+opt+'_'+side,points,.013,hair,'Ear.'+side)
        oval('Part_Ears_'+opt+'_Tip'+side,points[-1],(.022,.019,.026),hair,'Ear.'+side)
for ti,opt in enumerate(options['Tail']):
    oval('Part_Tail_'+opt+'_00',(0,.13,.4),(.067,.082,.069+ti%2*.02),hair,'Tail')

# Facial landmarks are purpose-built geometry; expression names remain compatible.
for opt in options['Face']:
    prefix='Part_Face_'+opt+'_';closed=opt in ['SLEEP','SLEEPY','DEAD','HAPPY','TASTY']
    for s in [-1,1]:
        x=s*.094;y=-.225;z=1.018
        if closed:
            tube(prefix+'Eye'+str(s),[(x-.031,y,z),(x,y-.004,z+(.012 if opt in ['HAPPY','TASTY'] else -.01)),(x+.031,y,z)],.006,ink)
        else:
            oval(prefix+'Eye'+str(s),(x,y,z),(.032,.012,.045 if opt!='PAIN' else .024),ink)
            oval(prefix+'Glint'+str(s),(x-.009,y-.012,z+.017),(.008,.004,.01),white)
            oval(prefix+'SmallGlint'+str(s),(x+.009,y-.012,z-.016),(.0035,.003,.0045),white)
        oval(prefix+'Cheek'+str(s),(s*.18,-.187,.951),(.034,.008,.017),rose)
        if opt in ['ANGRY','PAIN','SAD','NERVOUS','CURIOUS','PROUD']:
            slope=(-s if opt in ['ANGRY','PROUD'] else s)*.009
            tube(prefix+'Brow'+str(s),[(x-.027,-.213,1.085-slope),(x+.027,-.213,1.085+slope)],.0045,hair)
        if opt in ['TEARY','SOBBING']:
            oval(prefix+'Tear'+str(s),(x,-.225,.958),(.009,.006,.032),white)
    oval(prefix+'Nose',(0,-.239,.977),(.011,.009,.008),skin)
    mouth=.928
    if opt in ['SURPRISE','SOBBING']:oval(prefix+'Mouth',(0,-.235,mouth),(.013,.006,.019),ink)
    else:tube(prefix+'Mouth',[(-.024,-.228,mouth+.006),(0,-.235,mouth+(-.003 if opt not in ['SAD','PAIN','DEAD'] else .014)),(.024,-.228,mouth+.006)],.004,ink)

# Existing armor switch still has real plates; stats and hand-held equipment are untouched.
oval('Part_Equipment_Armor_00',(0,-.095,.565),(.118,.029,.095),metal,'spine_01')
for s,side in [(1,'L'),(-1,'R')]:oval('Part_Equipment_Armor_'+side,(s*.117,.003,.65),(.071,.065,.035),metal,'upper_arm.'+side)

groups={}
for o in list(scene.objects):
    if o.type!='MESH':continue
    bits=o.name.split('_');key='Part_Body_Core' if bits[1]=='Body' else '_'.join(bits[:3])
    groups.setdefault(key,[]).append(o)
for key,objects in groups.items():
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0]
    if len(objects)>1:bpy.ops.object.join()
    objects[0].name=key+'_00'
allparts=[o for o in scene.objects if o.type=='MESH']
for o in allparts:
    if o.name.startswith('Part_Hair_'):
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(o.data);bm.free()
for o in allparts:o.hide_render=False;o.hide_set(False);o.select_set(True)
rig.select_set(True);bpy.context.view_layer.objects.active=rig
export=OUT/'Kemono_Character.fbx'
bpy.ops.export_scene.fbx(filepath=str(export),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',path_mode='AUTO',apply_scale_options='FBX_SCALE_ALL')
assert bone_snapshot=={b.name:[list(b.head_local),list(b.tail_local)] for b in rig.data.bones}
checks={'new_meshes':len(allparts),'vertices':sum(len(o.data.vertices) for o in allparts),'bone_count':len(rig.data.bones),'bone_rest_unchanged':True,'unweighted':sum(not v.groups for o in allparts for v in o.data.vertices),'source_meshes_reused':0,'expressions':options['Face'],'unity_verified':False}
(OUT/'character-validation.json').write_text(json.dumps(checks,indent=2))
# A single readable combination for the studio preview.
for o in allparts:
    name=o.name
    visible= name.startswith(('Part_Body_','Part_Outfit_','Part_Face_NEUTRAL_','Part_Hair_'+options['Hair'][0]+'_','Part_Ears_'+options['Ears'][0]+'_','Part_Tail_'+options['Tail'][0]+'_'))
    o.hide_render=not visible;o.hide_set(not visible)
print(json.dumps(checks))
