import sys
sys.path.insert(0,'work/appearance-overlap-1112')
from inspect_parts import *
v,uv,t=mesh('Part_Face_NEUTRAL_00');other=np.concatenate([mesh(n)[0] for n in ['Part_Body_Meadow_00','Part_Outfit_CITIZEN_00','Part_Hair_BOB_00','Part_Ears_ANTENNA_00']]);av=np.concatenate([v,other]);lo=av.min(0);hi=av.max(0);p=(v-(lo+hi)/2)/(hi[1]-lo[1]);p[:,1]=(v[:,1]-lo[1])/(hi[1]-lo[1]);colors=tex[np.clip(((1-uv[:,1])*h).astype(int),0,h-1),np.clip((uv[:,0]*w).astype(int),0,w-1)];dark=(colors.max(1)<100)&(p[:,2]>.01)&(abs(p[:,0])>.025)&(abs(p[:,0])<.10)&(p[:,1]>.77)&(p[:,1]<.85)
print('bounds',lo,hi,'dark percentiles',np.percentile(p[dark],[0,10,50,90,100],axis=0))
others={tuple(q) for q in other};seam=v[[tuple(q) in others for q in v]];distance=np.full(len(v),.045)
for i in range(0,len(v),256):distance[i:i+256]=np.minimum(.045,np.sqrt(((v[i:i+256,None,:]-seam[None,:,:])**2).sum(2).min(1)))
print('iris edge',np.percentile(distance[dark]/.045,[0,10,50,90,100]))
