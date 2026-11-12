"""Diagnostic rear projection, not a game texture or asset modification."""
from inspect_parts import *
v,u,t=mesh('Part_Hair_BOB_00'); c=v[t].mean(1)
mask=(c[:,1]<1.19)&(c[:,2]>-.065)&(c[:,2]<.025)&(abs(c[:,0])<.10)
keys=np.round(u*100000).astype(int)
(root/'nape-body-uv-candidate.txt').write_text('\n'.join(sorted('|'.join(sorted(','.join(map(str,keys[j])) for j in face)) for face in t[mask])))
meshes.clear()
for n in ['Part_Face_NEUTRAL_00','Part_Body_Meadow_00','Part_Outfit_CITIZEN_00','Part_Ears_ANTENNA_00']:
    a,b,d=mesh(n);a[:,[0,2]]*=-1;meshes[n]=(a,b,d)
v[:,[0,2]]*=-1
meshes['Remaining']=(v,u,t[~mask]);meshes['Hair_Candidate']=(v,u,t[mask])
png(root/'nape-candidate-diagnostic.png',render(list(meshes),True))
print('Diagnostic candidate triangles',int(mask.sum()),'; not an approved partition')
