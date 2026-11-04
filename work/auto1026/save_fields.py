from pathlib import Path
r=Path('E:/Git/ant/Assets/_Project/Scripts/Save')
for filename in ('SaveSnapshot.cs','SaveBuildings.cs'):
 p=r/filename;s=p.read_text(encoding='utf-8')
 if filename=='SaveSnapshot.cs':
  s=s.replace('gatheringForbidden = n != null && n.GatheringForbidden,','gatheringForbidden = n != null && n.GatheringForbidden, workRequested = n != null && n.WorkRequested, workPaused = n != null && n.WorkPaused,')
  s=s.replace('node.GatheringForbidden = n.gatheringForbidden;','node.GatheringForbidden = n.gatheringForbidden; node.WorkRequested = n.workRequested; node.WorkPaused = n.workPaused;')
  s=s.replace('node.GatheringForbidden = d.gatheringForbidden;','node.GatheringForbidden = d.gatheringForbidden; node.WorkRequested = d.workRequested; node.WorkPaused = d.workPaused;')
 else:
  s=s.replace('gatheringForbidden = nodes[i].GatheringForbidden,','gatheringForbidden = nodes[i].GatheringForbidden, workRequested = nodes[i].WorkRequested, workPaused = nodes[i].WorkPaused,')
  s=s.replace('nodes[node.index].GatheringForbidden = node.gatheringForbidden;','nodes[node.index].GatheringForbidden = node.gatheringForbidden; nodes[node.index].WorkRequested = node.workRequested; nodes[node.index].WorkPaused = node.workPaused;')
 p.write_text(s,encoding='utf-8')
