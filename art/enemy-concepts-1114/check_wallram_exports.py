import bpy,json,math,sys
from pathlib import Path
creature=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'WallRam'
out=Path('F:/UnityProjects/ant-colony-rts/art/enemy-concepts-1114')/creature
only=sys.argv[sys.argv.index('--')+2] if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2 else None
if only=='all': only=None
if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+3:
    out=Path(sys.argv[sys.argv.index('--')+3])/creature
checks=json.loads((out/'export-checks.json').read_text()) if only else []
checks=[check for check in checks if check['clip']!=only] if only else checks
for spec in json.loads((out/'clips.json').read_text()):
    if only and spec['name']!=only: continue
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(out/(spec['name']+'.fbx')))
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    mesh=next(o for o in bpy.context.scene.objects if o.type=='MESH')
    action=rig.animation_data.action
    start,end=map(int,action.frame_range)
    samples=[]; min_z=1e9
    for f in range(start,end+1):
        bpy.context.scene.frame_set(f)
        ev=mesh.evaluated_get(bpy.context.evaluated_depsgraph_get())
        pts=[ev.matrix_world @ v.co for v in ev.data.vertices]
        assert all(math.isfinite(c) for p in pts for c in p)
        min_z=min(min_z,min(p.z for p in pts))
        samples.append(pts)
    loop_error=max((a-b).length for a,b in zip(samples[0],samples[-1]))
    motion=max((a-b).length for sample in samples for a,b in zip(samples[0],sample))
    assert len(samples)>1 and motion>.001,(spec['name'],'no motion',motion)
    assert min_z>-.005,(spec['name'],'floor penetration',min_z)
    if spec['loop']: assert loop_error<.003,(spec['name'],'loop seam',loop_error)
    checks.append({'clip':spec['name'],'frames':len(samples),'bones':len(rig.data.bones),'loopError':loop_error,'motion':motion,'minZ':min_z,'status':'PASS'})
(out/'export-checks.json').write_text(json.dumps(checks,indent=2))
print(json.dumps(checks))

