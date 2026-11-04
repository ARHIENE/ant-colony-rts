"""Bounded ±3 cm path slide plus wrist roll, reusing kit two-bone solve and clearance gates."""
import ast,bpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
from mathutils.bvhtree import BVHTree
root=Path('E:/Git/ant/codex-work~'); kit=root/'blender'
bpy.ops.wm.open_mainfile(filepath=str(kit/'pk-motions.blend'))
rig=bpy.data.objects['KemonoRig']; rig.data.pose_position='POSE'
vl=bpy.context.view_layer; scene=bpy.context.scene; E=bpy.data.objects
for node in ast.parse((kit/'scripts/build_motions.py').read_text(encoding='utf-8-sig')).body:
    if isinstance(node,ast.FunctionDef) and node.name in {'arm_cost','solve_arm','attach','hand_verts','clearance','wrist_bend','tool_length'}: exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_solver','exec'))
    if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id in {'CLEAR_PARTS','ARMOUR_PARTS'} for t in node.targets): exec(compile(ast.Module(body=[node],type_ignores=[]),'kit_constants','exec'))
original=bpy.data.actions['PK|Tool_Till']; rig.animation_data.action=original; rig.animation_data.action_slot=original.slots[0]
frames=list(range(1,int(original.get('duration_frames',original.frame_range[1]))+2))
prop=attach('Tool_Hoe_Steel','Socket.Tool.R'); before=clearance(prop,frames,'RL')
hand_in=rig.data.bones['Socket.Tool.R'].matrix_local.inverted()@rig.data.bones['hand.R'].matrix_local
poses={}; rolls=[]; offsets=[]; bends0={'R':0,'L':0}; tool0={}; previous=Vector((0,0,0)); prevroll=0
for frame in frames:
    scene.frame_set(frame); vl.update()
    for side in 'RL': bends0[side]=max(bends0[side],wrist_bend(side))
    socket=rig.pose.bones['Socket.Tool.R'].matrix.copy(); tool0[frame]=prop.matrix_world.copy()
    left=rig.pose.bones['hand.L'].matrix.copy(); sh={s:rig.pose.bones['upper_arm.'+s].head.copy() for s in 'RL'}
    # Reject new torso plate contact before choosing a pose; bracers are tested after baking.
    trees={}
    for n in ('Armor_Breastplate','Armor_Breastplate_Lower','Armor_Fauld','Armor_Tasset_L','Armor_Tasset_R'):
        obj=E[n]; M=rig.matrix_world@rig.pose.bones[obj['bone']].matrix@rig.data.bones[obj['bone']].matrix_local.inverted()
        trees[n]=BVHTree.FromPolygons([M@v.co for v in obj.data.vertices],[p.vertices[:] for p in obj.data.polygons])
    tv=[v.co.copy() for v in prop.data.vertices]; tp=[p.vertices[:] for p in prop.data.polygons]
    initial=BVHTree.FromPolygons([prop.matrix_world@v for v in tv],tp)
    allowed={n for n,t in trees.items() if initial.overlap(t)}
    best=None; candidates=[]
    def score(off,angle):
        P=Matrix.Translation(off)@socket@Matrix.Rotation(math.radians(angle),4,'Y')
        HR=P@hand_in; HL=P@socket.inverted()@left
        cost=max(arm_cost('R',HR,sh['R']),arm_cost('L',HL,sh['L']))+math.radians(.15)*off.length*100+math.radians(.1)*(off-previous).length*100+math.radians(.002)*abs(angle-prevroll)
        return cost,HR,HL
    for x in (-.03,0,.03):
      for y in (-.03,0,.03):
       for z in (-.03,0,.03):
        off=Vector((x,y,z))
        if off.length>.030001: continue
        for angle in range(-150,151,10):
            c,hr,hl=score(off,angle)
            candidates.append((c,off,angle,hr,hl))
    for candidate_pose in sorted(candidates,key=lambda c:c[0]):
        _,off,angle,hr,hl=candidate_pose
        P=Matrix.Translation(off)@socket@Matrix.Rotation(math.radians(angle),4,'Y')
        delta=rig.matrix_world@P@socket.inverted()@rig.matrix_world.inverted()
        tt=BVHTree.FromPolygons([delta@tool0[frame]@v for v in tv],tp)
        if all(n in allowed or not tt.overlap(t) for n,t in trees.items()):
            best=candidate_pose; break
    if best is None: raise RuntimeError('No collision-safe candidate at '+str(frame))
    _,off,angle,hr,hl=best
    if frame==frames[-1] and original.get('loop'): off=Vector(offsets[0]); angle=rolls[0]; _,hr,hl=score(off,angle)
    solve_arm('R',hr); solve_arm('L',hl)
    poses[frame]={n:rig.pose.bones[n].matrix.copy() for s in 'RL' for n in ('upper_arm.'+s,'forearm.'+s,'hand.'+s)}
    rolls.append(angle); offsets.append(list(off)); previous=off; prevroll=angle
candidate=original.copy(); rig.animation_data.action=candidate; rig.animation_data.action_slot=candidate.slots[0]
for frame in frames:
    scene.frame_set(frame)
    for name,m in poses[frame].items():
        pb=rig.pose.bones[name]; pb.matrix=m; vl.update(); pb.keyframe_insert('rotation_euler',frame=frame,group=name)
after=clearance(prop,frames,'RL'); bends={'R':0,'L':0}; left_error=0; lengths=[]; shift=0
for frame in frames:
    scene.frame_set(frame); vl.update()
    for s in 'RL': bends[s]=max(bends[s],wrist_bend(s))
    left_error=max(left_error,(rig.matrix_world@rig.pose.bones['Socket.Tool.L'].head-prop.matrix_world@Vector(E['Tool_Hoe_Steel']['grip2'])).length)
    lengths.append(tool_length(prop)); shift=max(shift,(prop.matrix_world.translation-tool0[frame].translation).length)
keep=bends['R']<60 and bends['R']<bends0['R']-3 and bends['L']<=max(60,bends0['L']) and all(v['frames']<=before.get(n,{}).get('frames',0) for n,v in after.items()) and left_error<.001 and max(lengths)-min(lengths)<.00001 and shift<=.03001
report={'accepted':keep,'wrist_before_deg':bends0,'wrist_after_deg':bends,'clearance_before':before,'clearance_after':after,'left_grip_error_m':left_error,'rigid_length_drift_m':max(lengths)-min(lengths),'tool_path_shift_max_m':shift,'roll_deg':rolls,'offset_m':offsets}
if keep:
    bpy.data.actions.remove(original); candidate.name='PK|Tool_Till'; candidate.use_fake_user=True
else:
    rig.animation_data.action=original; bpy.data.actions.remove(candidate)
bpy.data.objects.remove(prop)
rig.animation_data.action=None
for pb in rig.pose.bones: pb.matrix_basis.identity()
if keep: bpy.ops.wm.save_as_mainfile(filepath=str(kit/'pk-motions.blend'))
(root/'reports/wrist-slide-clearance.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SLIDE',keep,bends0,bends,left_error,shift,flush=True)
