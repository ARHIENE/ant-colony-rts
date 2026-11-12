"""Read-only mesh/texture diagnostic for the exposed rear-neck patch."""
from inspect_parts import *
v, uv, tri = mesh('Part_Hair_BOB_00')
c = v[tri].mean(axis=1)
t = uv[tri].mean(axis=1)
rgb = tex[np.clip(((1-t[:,1])*h).astype(int),0,h-1),np.clip((t[:,0]*w).astype(int),0,w-1)].astype(int)
report=[]
for low, high in [(1.14,1.18),(1.18,1.22),(1.22,1.26),(1.26,1.30)]:
    selected=(c[:,1]>=low)&(c[:,1]<high)&(c[:,2]<0)&(abs(c[:,0])<.08)
    report.append(dict(y=[low,high],triangles=int(selected.sum()),
        medianRGB=np.median(rgb[selected],axis=0).tolist() if selected.any() else [],
        warmSkinLike=int((selected&(rgb[:,0]-rgb[:,2]>30)&(rgb[:,0]-rgb[:,1]>15)).sum())))
(root/'nape-classification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
