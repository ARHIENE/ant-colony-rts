import bpy, math, json, sys
import runpy
from mathutils import Vector, Quaternion
from pathlib import Path

CREATURE=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'WallRam'
NEEDLE=CREATURE=='NeedleStalker'
BLOOM=CREATURE=='BloomMender'
OUT=Path('F:/UnityProjects/ant-colony-rts/art/enemy-concepts-1114')/CREATURE
if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2:
    OUT=Path(sys.argv[sys.argv.index('--')+2])/CREATURE
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='F:/UnityProjects/ant-colony-rts/MESHY/'+CREATURE+'_1114_SmartRig.glb')
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
mesh=bpy.data.objects['output_unwrapped']
for o in list(bpy.context.scene.objects):
    if o.type=='MESH' and o!=mesh: bpy.data.objects.remove(o,do_unlink=True)
rig.name=CREATURE+'_Rig'; mesh.name=CREATURE+'_Mesh'
scene=bpy.context.scene; scene.render.fps=30
forward=Vector((0,-1,0) if NEEDLE else (-.56,-.83,0)).normalized(); right=forward.cross(Vector((0,0,1))).normalized(); up=Vector((0,0,1))
right=-right
lower_bones=['Bone_010','Bone_015'] if NEEDLE else ['Bone_010','Bone_014','Bone_031','Bone_035']
upper_bones=['Bone_012','Bone_017'] if NEEDLE else ['Bone_011','Bone_015','Bone_032','Bone_036']
head_bone='Bone_004' if NEEDLE else 'Bone_005'
if BLOOM:
    forward=Vector((0,-1,0)); right=Vector((1,0,0))
    lower_bones=['Bone_007','Bone_012','Bone_017','Bone_022','Bone_027']
    upper_bones=['Bone_009','Bone_014','Bone_019','Bone_024','Bone_029']
    head_bone='Bone_003'
root=rig.pose.bones['Bone_000']
profiles = runpy.run_path(str(Path(__file__).with_name('remaining_motion_profiles.py')))
profile = profiles['PROFILES'].get(CREATURE)
if not profile and CREATURE not in ('WallRam', 'NeedleStalker', 'BloomMender'):
    raise ValueError('No inspected rig profile: '+CREATURE)
if profile:
    bone_name=profiles['bone']
    lower_bones=list(map(bone_name,profile['feet']))
    upper_bones=list(map(bone_name,profile['hips']))
    head_bone=bone_name(profile['head'])
    forward=Vector((0,-1,0)); right=Vector((1,0,0))
    for n in lower_bones+upper_bones+[head_bone]+list(map(bone_name,profile['attack']+profile.get('tail',[]))):
        assert n in rig.pose.bones, (CREATURE,n)
def local_offset(b,v): return b.bone.matrix_local.to_3x3().inverted() @ v
def rotate(b,axis,angle):
    b.rotation_mode='QUATERNION'
    b.rotation_quaternion=Quaternion(local_offset(b,axis).normalized(),angle)
targets=[]
for foot_index,lower in enumerate(lower_bones):
    b=rig.pose.bones[lower]
    target=bpy.data.objects.new('FootTarget_'+lower,None); scene.collection.objects.link(target)
    target.location=rig.matrix_world @ b.bone.tail_local
    con=b.constraints.new('IK'); con.target=target; con.chain_count=3 if NEEDLE or BLOOM else 2; con.use_stretch=False
    if profile:
        chain=profile['chain']; con.chain_count=chain[foot_index] if isinstance(chain,list) else chain
    targets.append((target,target.location.copy()))
specs={'Idle':(90,True),'Move':(30,True),'Attack':(42,False),'Hit':(18,False),'Death':(60,False)}
if BLOOM: specs['Heal']=(90,False)
tracks=[]
for name,(length,loop) in specs.items():
    rig.animation_data_clear()
    for b in rig.pose.bones:
        for c in b.constraints: c.mute=(name=='Death')
    for target,_ in targets: target.animation_data_clear()
    for b in rig.pose.bones:
        b.location=(0,0,0); b.rotation_mode='QUATERNION'; b.rotation_quaternion=(1,0,0,0); b.scale=(1,1,1)
    for frame in range(1,length+2):
        t=(frame-1)/length; phase=2*math.pi*t
        displacement=Vector((0,0,0)); pitch=0; head=0; roll=0
        if name=='Idle': displacement=up*(.012*math.sin(phase)); head=.012*math.sin(phase)
        if name=='Move': displacement=up*(.018*(1-math.cos(2*phase))); pitch=.025*math.sin(phase)
        if name=='Attack':
            wind=math.sin(math.pi*min(t/.42,1))**2 if t<.42 else 0
            strike=math.sin(math.pi*max(0,min(1,(t-.38)/.30)))**2 if .38<t<.68 else 0
            displacement=forward*(-.06*wind+.12*strike)+up*(-.025*wind)
            pitch=.07*wind-.13*strike; head=.10*wind-.18*strike
            if NEEDLE:
                displacement=-forward*(.07*wind+.14*strike)
                pitch=.03*wind-.07*strike; head=-.05*wind+.12*strike
        if name=='Hit':
            impact=math.sin(math.pi*min(t/.42,1))**2 if t<.42 else .15*math.sin(math.pi*(t-.42)/.58)**2
            displacement=-forward*.065*impact; pitch=-.055*impact; head=.06*impact
        if name=='Death':
            fall=min(1,max(0,(t-.12)/.68)); fall=fall*fall*(3-2*fall)
            displacement=-up*.38*fall; roll=1.25*fall; head=-.15*fall
        if BLOOM:
            if name=='Attack':
                displacement=up*(.035*math.sin(math.pi*t)**2); pitch=0; head=.035*math.sin(math.pi*t)**2
            for n in ['Bone_042','Bone_047','Bone_065','Bone_070','Bone_075']:
                bend=.035*math.sin(phase+int(n[-2:]))
                if name=='Attack': bend+=.22*math.sin(math.pi*t)**4
                if name=='Heal': bend+=.18*math.sin(math.pi*t)
                rotate(rig.pose.bones[n],right,bend)
            if name=='Heal':
                displacement=up*(.045*math.sin(math.pi*t)); head=-.08*math.sin(math.pi*t)
                fan=rig.pose.bones['Bone_057']; pulse=1+.12*math.sin(math.pi*t)
                fan.scale=(pulse,pulse,pulse); fan.keyframe_insert('scale',frame=frame)
        if profile:
            kind=profile['kind']
            if name=='Death': roll=2.8*fall
            strike=math.sin(math.pi*max(0,min(1,(t-.35)/.36)))**2 if name=='Attack' and .35<t<.71 else 0
            wind=math.sin(math.pi*min(t/.42,1))**2 if name=='Attack' and t<.42 else 0
            if name=='Attack' and kind=='spit':
                displacement=-forward*(.07*wind+.14*strike)
                pitch=.035*wind-.09*strike; head=-.12*wind+.22*strike
            if name=='Attack' and kind in ('pulse','climb'):
                displacement=up*(.065*math.sin(math.pi*t)**2)
                pitch=0; head=.05*math.sin(math.pi*t)**2
            if name=='Attack' and kind=='bash':
                displacement=forward*(-.05*wind+.10*strike)
                head=.06*wind-.12*strike
            for i,number in enumerate(profile['attack']):
                bend=.012*math.sin(phase+i) if name=='Idle' else 0
                if name=='Move': bend=.055*math.sin(phase+i*math.pi)
                if name=='Attack':
                    amplitude=.45 if kind in ('slash','slam') else .25
                    bend=amplitude*(.45*wind-strike)*(1 if i%2==0 or kind!='slash' else -.7)
                if name=='Death': bend=.22*fall
                rotate(rig.pose.bones[bone_name(number)],right,bend)
            if kind=='slither':
                for i,number in enumerate(profile['tail']):
                    bend=(.10 if name=='Move' else .015)*math.sin(phase-i*.65)
                    if name=='Death': bend=.05*fall
                    rotate(rig.pose.bones[bone_name(number)],up,bend)
            if kind=='climb' and name=='Move':
                for i,number in enumerate(profile['hips']):
                    rotate(rig.pose.bones[bone_name(number)],up,.12*math.sin(phase+i*math.pi))
        root.location=local_offset(root,displacement)
        rotate(root,right,pitch)
        if roll: root.rotation_quaternion=Quaternion(local_offset(root,forward).normalized(),roll) @ root.rotation_quaternion
        rotate(rig.pose.bones[head_bone],right,head)
        if name=='Death':
            for bone in upper_bones:
                rotate(rig.pose.bones[bone],right,.18*fall)
            for bone in lower_bones:
                rotate(rig.pose.bones[bone],right,-.35*fall)
        for i,(target,base) in enumerate(targets):
            delta=Vector((0,0,0))
            if name=='Move':
                p=phase+(math.pi if i in (1,2) else 0)
                delta=forward*((.07 if BLOOM else .11)*math.cos(p))+up*((.045 if BLOOM else .07)*max(0,math.sin(p)))
                if profile:
                    p=phase+math.pi*(i%2)
                    stride=profile['stride']*(.65 if CREATURE=='AcidSpitter' else 1)
                    delta=forward*(stride*math.cos(p))+up*(min(.08,stride*.55)*max(0,math.sin(p)))
            if name=='Death': delta=right*((-.10 if i in (0,2) else .10)*min(1,t*2))
            target.location=base+delta; target.keyframe_insert('location',frame=frame)
        for b in rig.pose.bones:
            b.keyframe_insert('location',frame=frame)
            b.keyframe_insert('rotation_quaternion',frame=frame)
        scene.frame_set(frame)
    bpy.context.view_layer.objects.active=rig
    bpy.ops.object.select_all(action='DESELECT'); rig.select_set(True)
    bpy.ops.nla.bake(frame_start=1,frame_end=length+1,step=1,only_selected=False,visual_keying=True,clear_constraints=False,use_current_action=True,bake_types={'POSE'})
    action=rig.animation_data.action; action.name=name; action.use_fake_user=True
    action['loop']=loop
    tracks.append({'name':name,'frames':length+1,'seconds':length/30,'loop':loop})
    # Export one action per FBX: no ambiguity about clip selection in Unity.
    mesh.select_set(True); scene.frame_start=1; scene.frame_end=length+1
    for b in rig.pose.bones:
        for c in b.constraints: c.mute=True
    # Smooth solver branch jitter in baked joint rotations, keeping endpoints and loop seams.
    samples=[]
    for f in range(1,length+2):
        scene.frame_set(f)
        samples.append({b.name:b.rotation_quaternion.copy() for b in rig.pose.bones})
    for f in range(1,length+2):
        scene.frame_set(f)
        for b in rig.pose.bones:
            if not loop and f in (1,length+1): continue
            ref=samples[f-1][b.name]; values=[]
            for offset,weight in [(-2,1),(-1,2),(0,3),(1,2),(2,1)]:
                index=(f-1+offset)%length if loop else min(length,max(0,f-1+offset))
                q=samples[index][b.name].copy()
                if q.dot(ref)<0: q.negate()
                values.append((q,weight))
            b.rotation_quaternion=Quaternion(tuple(sum(q[i]*w for q,w in values)/9 for i in range(4))).normalized()
            b.keyframe_insert('rotation_quaternion',frame=f)
    # Keep the skinned silhouette above the ground after visual baking.
    for f in range(1,length+2):
        scene.frame_set(f)
        ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        min_z=min((ev.matrix_world @ v.co).z for v in ev.data.vertices)
        root.location+=local_offset(root,up*max(0,-min_z))
        root.keyframe_insert('location',frame=f)
    bpy.ops.export_scene.fbx(filepath=str(OUT/(name+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0.0,path_mode='COPY',embed_textures=True,axis_forward='-Z',axis_up='Y')
    for b in rig.pose.bones:
        for c in b.constraints: c.mute=False
for b in rig.pose.bones:
    for c in list(b.constraints): b.constraints.remove(c)
for target,_ in targets: bpy.data.objects.remove(target,do_unlink=True)
rig.animation_data.action=bpy.data.actions['Idle']; scene.frame_set(1)
scene.frame_start=1; scene.frame_end=91
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/(CREATURE+'_5Motions.blend')))
(OUT/'clips.json').write_text(json.dumps(tracks,indent=2))
print('EXPORTED',json.dumps(tracks))



