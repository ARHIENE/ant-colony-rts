import pathlib,json,numpy as np
from inspect_parts import *
v,uv,tri=meshes[names[0]]
ids=json.loads((root/'face-hair-partition-candidate.json').read_text())['moveToHair']; mask=np.zeros(len(tri),bool);mask[ids]=True
meshes['Part_Hair_Front_00']=(v.copy(),uv,tri[mask])
allv=np.concatenate([mesh(p.stem)[0] for p in pathlib.Path('Assets/Resources/MeshyNative/SourceParts/Rig0').glob('*.asset')])
low=allv.min(0);high=allv.max(0);height=high[1]-low[1];center=(low+high)/2
q=v.copy();y=(q[:,1]-low[1])/height;x=(q[:,0]-center[0])/height;z=(q[:,2]-center[2])/height
eye=np.exp(-((abs(x)-.057)/.04)**4-((y-.805)/.045)**4);mouth=np.exp(-(x/.035)**4-((y-.735)/.025)**4)
for shape in ['Neutral','Blink','Smile','Surprise']:
 dy=np.zeros(len(q))
 if shape=='Blink':dy=-(y-.805)*eye*.85
 if shape=='Smile':dy=(abs(x)/.035*.008-.002)*mouth
 if shape=='Surprise':dy=(y-.805)*eye*.15+(y-.735)*mouth*.5
 dy[z<.01]=0;deformed=v.copy();deformed[:,1]+=dy*height;meshes[names[0]]=(deformed,uv,tri[~mask])
 png(root/('offline-expression-'+shape+'.png'),render(names+['Part_Hair_Front_00'],True))
 print(shape,flush=True)
