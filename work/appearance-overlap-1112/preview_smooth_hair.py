from inspect_parts import *
base,uv,tri=mesh('Part_Hair_BOB_00')
other=np.concatenate([mesh(n)[0] for n in ['Part_Body_Meadow_00','Part_Outfit_CITIZEN_00','Part_Face_NEUTRAL_00','Part_Ears_ANTENNA_00']])
others={tuple(p) for p in other};seam=np.array([p for p in base if tuple(p) in others]);dist=np.full(len(base),.045)
for first in range(0,len(base),256):
 chunk=base[first:first+256];dist[first:first+len(chunk)]=np.minimum(.045,np.sqrt(((chunk[:,None,:]-seam[None,:,:])**2).sum(2).min(1)))
allv=np.concatenate([base,other]);low=allv.min(0);high=allv.max(0);height=high[1]-low[1];center=(high+low)/2
x=(base[:,0]-center[0])/height;y=(base[:,1]-low[1])/height;z=(base[:,2]-center[2])/height
lower=np.clip((.87-y)/.15,0,1)*dist/.045;amount=height*lower
smooth=lambda t:np.clip(t,0,1)**2*(3-2*np.clip(t,0,1))
report=[]
for kind in ['PONY','TWIN','UPDO']:
 for version in ['old','smooth']:
  b=(z<0).astype(float) if version=='old' else smooth((.025-z)/.08)
  side=(abs(x)>.07).astype(float) if version=='old' else smooth((abs(x)-.04)/.06)
  q=base.copy()
  if kind=='PONY':q[:,1]-=amount*.19*b;q[:,0]+=(center[0]-q[:,0])*.35*lower*b;q[:,2]-=amount*.04*b
  if kind=='TWIN':q[:,1]-=amount*.15*side;q[:,0]+=np.sign(x)*amount*.035*side
  if kind=='UPDO':q[:,1]+=amount*.08*b;q[:,2]-=amount*.015*b
  lengths=[]
  for a,c in [(0,1),(1,2),(2,0)]:
   length=np.linalg.norm(base[tri[:,a]]-base[tri[:,c]],axis=1);deformed=np.linalg.norm(q[tri[:,a]]-q[tri[:,c]],axis=1);lengths.extend((deformed[length>1e-5]/length[length>1e-5]).tolist())
  report.append(dict(hair=kind,version=version,maxEdgeStretch=max(lengths),p99=float(np.quantile(lengths,.99))))
  meshes[names[1]]=(q,uv,tri);png(root/('hair-'+kind+'-'+version+'-offline.png'),render(names,True))
  print(report[-1],flush=True)
(root/'hair-smooth-edge-analysis.json').write_text(json.dumps(report,indent=2))
