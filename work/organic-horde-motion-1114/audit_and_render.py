import runpy,sys
from pathlib import Path
r=Path('F:/UnityProjects/ant-colony-rts/work/organic-horde-motion-1114')
runpy.run_path(str(r/'audit_contacts.py'),run_name='__main__')
for name in ['HuntRider','SiegeRider','ClawHunter']:
 sys.argv=['render_review.py','--',name,str(r/'refined')]
 runpy.run_path(str(r/'render_review.py'),run_name='__main__')
