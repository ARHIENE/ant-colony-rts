import bpy,json
from pathlib import Path
W=Path('E:/Git/ant/codex-work~'); R=W/'round2'
def plain(v):
 if hasattr(v,'to_dict'): return plain(v.to_dict())
 if hasattr(v,'to_list'): return plain(v.to_list())
 if isinstance(v,dict): return {k:plain(x) for k,x in v.items()}
 if isinstance(v,(tuple,list)): return [plain(x) for x in v]
 return v
result={}
for label,path in [('before',R/'blender/baseline.blend'),('after',R/'blender/pk-motions-round2.blend')]:
 bpy.ops.wm.open_mainfile(filepath=str(path)); result[label]={n:{'range':list(bpy.data.actions['PK|'+n].frame_range),'properties':{k:plain(v) for k,v in bpy.data.actions['PK|'+n].items()}} for n in ('Tool_Dig','Tool_Mine')}
result['passed']=result['before']==result['after']
(R/'reports/timing-events.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
assert result['passed'],result
print('TIMING EVENTS UNCHANGED',flush=True)
