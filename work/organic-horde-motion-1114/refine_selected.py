import runpy,sys,json
from pathlib import Path
root=Path('F:/UnityProjects/ant-colony-rts/work/organic-horde-motion-1114')
sys.argv=['refine_contacts.py','--','WallRam','AcidSpitter','BroodAlien','SiegeBrooder','VeilWeaver','BloomMender','BroodCarrier']
runpy.run_path(str(root/'refine_contacts.py'),run_name='__main__')
for p in (root/'refined').glob('*/contact-profile.json'):
 data=json.loads(p.read_text());rig=json.loads((root.parents[1]/'art/enemy-concepts-1114'/(p.parent.name+'-rig.json')).read_text());dims=next(o['dimensions'] for o in rig if o['type']=='MESH');data['horizontalExtent']=max(dims[0],dims[1]);p.write_text(json.dumps(data,indent=2))
runpy.run_path(str(root/'audit_and_render.py'),run_name='__main__')
