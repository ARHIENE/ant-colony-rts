from inspect_parts import *
v,uv,tri=mesh('Part_Face_NEUTRAL_00')
other=np.concatenate([mesh(n)[0] for n in ['Part_Body_Meadow_00','Part_Outfit_CITIZEN_00','Part_Hair_BOB_00','Part_Ears_ANTENNA_00']]);others={tuple(p) for p in other}; shared=np.array([tuple(p) in others for p in v]);seam=v[shared];distance=np.full(len(v),.045)
for start in range(0,len(v),256):
 chunk=v[start:start+256];distance[start:start+len(chunk)]=np.minimum(.045,np.sqrt(((chunk[:,None,:]-seam[None,:,:])**2).sum(2).min(1)))
t=distance/.045;edge=t*t*(3-2*t);allv=np.concatenate([v,other]);low=allv.min(0);high=allv.max(0);height=high[1]-low[1];center=(low+high)/2
x=(v[:,0]-center[0])/height;y=(v[:,1]-low[1])/height;z=(v[:,2]-center[2])/height;eye=np.exp(-((abs(x)-.057)/.04)**4-((y-.805)/.045)**4);dy=-(y-.805)*eye*.85*height;dy[z<.01]=0
report=dict(sharedVertices=int(shared.sum()),oldMaxSeamDisplacement=float(abs(dy[shared]).max()),newMaxSeamDisplacement=float(abs((dy*edge)[shared]).max()));print(report,flush=True);(root/'face-seam-analysis.json').write_text(json.dumps(report,indent=2))
for version,factor in [('old',np.ones(len(v))),('seam-fixed',edge)]:
 q=v.copy();q[:,1]+=dy*factor;meshes[names[0]]=(q,uv,tri);png(root/('blink-'+version+'-offline.png'),render(names,True));print(version,flush=True)
