"""Appearance slots and expression/injury layers fitted to the current human SD face."""
import bpy
import bmesh
import math
from mathutils import Vector
from common import copy_mesh, bind, mat, sphere, stroke, face_transform, MERGE

EXPRS={
 'NEUTRAL':{},'HAPPY':{'Blink':1,'eyeBrow Height':.12},
 'SURPRISE':{'eyeBrow Height':.7,'O':.8},'ANGRY':{'Sad ':-.65},
 'PAIN':{'><':1,'Ahh':.45},'SLEEPY':{'Blink':.65},'SLEEP':{'Blink':1},
 'SAD':{'Sad ':.7,'Blink':.18},'PROUD':{'Blink':.8,'eyeBrow Height':.2},
 'CURIOUS':{'eyeBrow Height':.5},'EMBARRASSED':{'Blink':.25,'Sad ':.3},
 'TASTY':{'Blink':1,'Ahh':.35},'EMPTY':{'Blink':.45},
 'TEARY':{'Sad ':.8},'SOBBING':{'Blink':1,'Sad ':.8,'Big Ahh':.8},
 'NERVOUS':{'Sad ':.45,'O':.2},'DEAD':{'Blink':1}}

def skirt_to_legs(o,rig,hip=.42,hem=.28):
    # Kiki skirt bones are cloth physics nothing animates: blend their weight from pelvis (waist) to thighs (hem)
    # so the hem follows each step. Other dropped bones merge into their MERGE target.
    gone=[g for g in o.vertex_groups if g.name not in rig.data.bones]
    if not gone:return
    get=lambda n:o.vertex_groups.get(n) or o.vertex_groups.new(name=n)
    skirt={g.index for g in gone if g.name.startswith('Skirt')}
    other={g.index:get(MERGE[g.name]) for g in gone if g.index not in skirt}
    gl,gr,gp=get('thigh.L'),get('thigh.R'),get('pevis')
    for v in o.data.vertices:
        w=0
        for g in v.groups:
            if g.group in skirt:w+=g.weight
            elif g.group in other and g.weight>0:other[g.group].add([v.index],g.weight,'ADD')
        if w<=0:continue
        a=min(1,max(0,(hip-v.co.z)/(hip-hem)));a=a*a*(3-2*a)
        left=min(1,max(0,.5+v.co.x/.08))
        for g,share in ((gl,a*left),(gr,a*(1-left)),(gp,1-a)):
            if share>0:g.add([v.index],w*share,'ADD')
    for g in gone:o.vertex_groups.remove(g)

def create(scene,src,ref_face,rig,collection):
    slots={};manifest=[]
    def register(slot,key,objects,bone=None):
        for i,o in enumerate(objects):
            o.name=f'Part_{slot}_{key}_{i:02}'
            if not any(m.type=='ARMATURE' for m in o.modifiers):bind(o,rig,bone)
            o['slot']=slot;o['choice']=key
        slots.setdefault(slot,{})[key]=objects
        manifest.append({'slot':slot,'choice':key,'objects':[o.name for o in objects],
                         'rule':'additive' if slot in ('Injury','Overlay') else 'one'})
    def find(prefix):return next(o for o in src.objects if o.name.startswith(prefix))
    body=copy_mesh(find('Settler_Body'),'Body',collection)
    skirt_to_legs(body,rig)
    def subset(original,keep,name):
        o=original.copy();o.data=original.data.copy();o.name=name;collection.objects.link(o)
        bm=bmesh.new();bm.from_mesh(o.data)
        bmesh.ops.delete(bm,geom=[f for f in bm.faces if not keep(f.material_index)],context='FACES')
        bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
        bm.to_mesh(o.data);bm.free();o.data.update();return o
    skin=subset(body,lambda i:i!=1,'BodySkin')
    garment=subset(body,lambda i:i==1,'Dress')
    register('Body','BASE',[skin])
    dress=garment.copy();dress.data=garment.data.copy();collection.objects.link(dress)
    dress.data.materials[1]=mat('PlainLinen',(.62,.59,.48))
    register('Outfit','LINEN',[dress])
    bpy.data.objects.remove(body,do_unlink=True);bpy.data.objects.remove(garment,do_unlink=True)

    baseface=find('Settler_Face')
    basis=ref_face.data.shape_keys.key_blocks[0]
    transformed=[face_transform(ref_face.matrix_world@v.co) for v in basis.data]
    # Source and current mesh must retain the same topology for safe morph transfer.
    assert len(transformed)==len(baseface.data.vertices)
    error=max((a-v.co).length for a,v in zip(transformed,baseface.data.vertices))
    assert error<.0001, f'Face source mismatch {error}'
    ink=mat('FaceInk',(.10,.035,.035));pink=mat('BlushInk',(.84,.28,.28))
    def project(o,x,z,offset=.002):
        hit,p,n,i=o.ray_cast(Vector((x,-1,z)),Vector((0,1,0)))
        return (x,p.y-offset if hit else -.16,z)
    for key,recipe in EXPRS.items():
        o=copy_mesh(baseface,'Face_'+key,collection)
        for i,v in enumerate(o.data.vertices):
            p=basis.data[i].co.copy()
            for shape,value in recipe.items():p+=(ref_face.data.shape_keys.key_blocks[shape].data[i].co-basis.data[i].co)*value
            v.co=face_transform(ref_face.matrix_world@p)
        o.data.update();bpy.context.view_layer.update()
        objs=[o]
        if key in ('NEUTRAL','HAPPY','PROUD','CURIOUS','EMBARRASSED'):
            pts=[project(o,x,.757+.004*(x/.012)**2) for x in [-.012+i*.024/20 for i in range(21)]]
            objs.append(stroke('Smile',pts,.0015,ink,collection))
        if key=='DEAD':
            for side in [-1,1]:
                for slope in [-1,1]:
                    pts=[project(o,side*.073+dx,.817+slope*dx) for dx in [-.014,.014]]
                    objs.append(stroke('DeadCross',pts,.0025,ink,collection))
        register('Face',key,objs,'head')
    face=slots['Face']['NEUTRAL'][0]
    for key in ['BLUSH','TEARS','SWEAT']:
        objs=[]
        if key=='BLUSH':
            for side in [-1,1]:
                for i in range(2):
                    x=side*(.133+i*.007)
                    objs.append(stroke('Blush',[project(face,x-.002,.785),project(face,x+.001,.791)],.0014,pink,collection))
        else:
            water=mat('Tears' if key=='TEARS' else 'Sweat',(.26,.65,.85))
            for side in ([-1,1] if key=='TEARS' else [1]):
                x=side*(.091 if key=='TEARS' else .14)
                z=.789 if key=='TEARS' else .887
                objs.append(sphere(key,project(face,x,z,.005),(.006,.004,.018),water,collection))
        register('Overlay',key,objs,'head')

    fur=mat('Fur_Ivory',(.82,.79,.76));inner=mat('InnerRose',(.70,.34,.37))
    hair=find('Settler_Hair')
    for key in ['BOB','SHORT','LONG','SIDE','BUNS','PONY']:
        h=copy_mesh(hair,'Hair_'+key,collection)
        if key=='SHORT':
            for v in h.data.vertices:
                if v.co.z<.91:v.co.z=.91+(v.co.z-.91)*.62
        elif key=='LONG':
            for v in h.data.vertices:
                if v.co.z<.91:v.co.z=.91+(v.co.z-.91)*1.37
        elif key=='SIDE':
            for v in h.data.vertices:v.co.x=-v.co.x
            # Reflecting the geometry reverses winding.
            bm=bmesh.new();bm.from_mesh(h.data);bmesh.ops.reverse_faces(bm,faces=list(bm.faces));bm.to_mesh(h.data);bm.free()
        objs=[h]
        if key=='BUNS':
            for side in [-1,1]:objs.append(sphere('HairBun',(side*.20,.035,1.06),(.075,.066,.075),fur,collection))
        if key=='PONY':objs.append(sphere('HairPony',(0,.17,.94),(.072,.08,.20),fur,collection))
        register('Hair',key,objs,'head')
    ears=[]
    for old in src.objects:
        if old.name.startswith('Settler_Ear'):
            o=copy_mesh(old,'RabbitEar',collection)
            x=sum(v.co.x for v in o.data.vertices)
            bind(o,rig,'Ear.L' if x>0 else 'Ear.R');ears.append(o)
    register('Ears','RABBIT',ears)
    for key in ['UPRIGHT','CAT','ROUND','DROOP']:
        objs=[]
        for side,bone in [(1,'Ear.L'),(-1,'Ear.R')]:
            if key=='CAT':
                verts=[(side*x,y,z) for x,y,z in [(.11,-.02,1.08),(.25,-.02,1.08),(.22,-.005,1.29),(.18,.07,1.11)]]
                me=bpy.data.meshes.new('CatEar');me.from_pydata(verts,[],[(0,2,1),(0,1,3),(1,2,3),(2,0,3)]);me.materials.append(fur)
                outer=bpy.data.objects.new('CatEar',me);collection.objects.link(outer)
                bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
                insert=sphere('CatInner',(side*.195,-.024,1.15),(.035,.004,.055),inner,collection)
            else:
                loc,scale={'UPRIGHT':((side*.13,.02,1.30),(.045,.035,.20)),
                           'ROUND':((side*.17,.015,1.14),(.075,.042,.075)),
                           'DROOP':((side*.22,.025,.96),(.055,.045,.19))}[key]
                outer=sphere('Ear',loc,scale,fur,collection)
                insert=sphere('EarInner',(loc[0],loc[1]-.035,loc[2]),(scale[0]*.58,.009,scale[2]*.75),inner,collection)
            for o in [outer,insert]:bind(o,rig,bone);objs.append(o)
        register('Ears',key,objs)
    for key in ['BOB','LONG','FOX']:
        if key=='BOB':objs=[copy_mesh(find('Settler_Tail'),'BobTail',collection)]
        elif key=='FOX':objs=[sphere('FoxTail',(0,.23,.40),(.095,.20,.10),fur,collection)]
        else:
            control=[Vector(p) for p in [(0,.12,.36),(.08,.23,.36),(.17,.31,.47),(.19,.32,.61),(.14,.30,.67)]]
            smooth=[]
            for i in range(len(control)-1):
                p0,p1,p2,p3=control[max(0,i-1)],control[i],control[i+1],control[min(len(control)-1,i+2)]
                for j in range(8):
                    t=j/8
                    smooth.append(.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t))
            smooth.append(control[-1])
            objs=[stroke('LongTail',smooth,.022,fur,collection)]
        register('Tail',key,objs,'Tail')

    # Health overlays stay independent of every facial expression.
    sites={'FOREHEAD':(0,-.16,.95,'head'),'CHEEK':(-.13,-.14,.785,'head'),
           'TORSO':(0,-.12,.55,'spine_02'),'ARM_L':(.20,-.025,.47,'forearm.L'),
           'ARM_R':(-.20,-.025,.47,'forearm.R'),'LEG':(.05,-.055,.22,'shin.L')}
    scar=mat('Scar',(.35,.12,.10));bruise=mat('Bruise',(.36,.22,.40));band=mat('Bandage',(.78,.63,.43))
    for kind in ['SCAR','SCRATCH','BRUISE','BANDAGE']:
        for site,(x,y,z,bone) in sites.items():
            if bone=='head':x,y,z=project(face,x,z,.004)
            if kind=='SCAR':objs=[stroke('Scar',[(x-.009,y,z-.014),(x+.008,y,z+.015)],.0017,scar,collection)]
            elif kind=='SCRATCH':objs=[stroke('Scratch',[(x-.007+i*.006,y,z-.012),(x-.003+i*.006,y,z+.012)],.0012,scar,collection) for i in range(3)]
            else:objs=[sphere(kind,(x,y-.001,z),(.014,.0025,.009),bruise if kind=='BRUISE' else band,collection)]
            register('Injury',kind+'_'+site,objs,bone)
    return slots,manifest,error
