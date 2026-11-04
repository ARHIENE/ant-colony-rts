from pathlib import Path
w=Path('E:/Git/ant/codex-work~'); s=(w/'blender/scripts/repair_wrist_slide.py').read_text()
s=s.replace('from mathutils import Matrix,Vector','from mathutils import Matrix,Vector\nfrom mathutils.bvhtree import BVHTree')
s=s.replace('    best=None\n', '''    # Reject new torso plate contact before choosing a pose; bracers are tested after baking.
    trees={}
    for n in ('Armor_Breastplate','Armor_Breastplate_Lower','Armor_Fauld','Armor_Tasset_L','Armor_Tasset_R'):
        obj=E[n]; M=rig.matrix_world@rig.pose.bones[obj['bone']].matrix@rig.data.bones[obj['bone']].matrix_local.inverted()
        trees[n]=BVHTree.FromPolygons([M@v.co for v in obj.data.vertices],[p.vertices[:] for p in obj.data.polygons])
    tv=[v.co.copy() for v in prop.data.vertices]; tp=[p.vertices[:] for p in prop.data.polygons]
    initial=BVHTree.FromPolygons([prop.matrix_world@v for v in tv],tp)
    allowed={n for n,t in trees.items() if initial.overlap(t)}
    best=None; candidates=[]
''')
s=s.replace("            if best is None or c<best[0]: best=(c,off,angle,hr,hl)","            candidates.append((c,off,angle,hr,hl))")
s=s.replace('    _,off,angle,hr,hl=best', '''    for candidate_pose in sorted(candidates,key=lambda c:c[0]):
        _,off,angle,hr,hl=candidate_pose
        P=Matrix.Translation(off)@socket@Matrix.Rotation(math.radians(angle),4,'Y')
        delta=rig.matrix_world@P@socket.inverted()@rig.matrix_world.inverted()
        tt=BVHTree.FromPolygons([delta@tool0[frame]@v for v in tv],tp)
        if all(n in allowed or not tt.overlap(t) for n,t in trees.items()):
            best=candidate_pose; break
    if best is None: raise RuntimeError('No collision-safe candidate at '+str(frame))
    _,off,angle,hr,hl=best''')
s=s.replace("root/'reports/wrist-slide.json'","root/'reports/wrist-slide-clearance.json'")
(w/'blender/scripts/repair_wrist_slide_clearance.py').write_text(s)
