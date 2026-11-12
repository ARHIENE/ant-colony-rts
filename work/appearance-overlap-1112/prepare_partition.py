from inspect_parts import *
from collections import defaultdict,deque
v,uv,tri=meshes[names[0]]
centers=v[tri].mean(1);tuv=uv[tri].mean(1)
rgb=tex[np.clip(((1-tuv[:,1])*h).astype(int),0,h-1),np.clip((tuv[:,0]*w).astype(int),0,w-1)].astype(int)
eligible=(rgb[:,0]-rgb[:,2]<30)&(rgb[:,0]-rgb[:,1]<25)&(rgb.min(1)>20)&(centers[:,1]>1.14)
keys=[tuple(x) for x in np.round(v*100000).astype(int)]
neighbors=defaultdict(list)
for i,t in enumerate(tri):
 for j in t:neighbors[keys[j]].append(i)
hairkeys={tuple(x) for x in np.round(meshes[names[1]][0]*100000).astype(int)}
selected=set(i for i,t in enumerate(tri) if eligible[i] and any(keys[j] in hairkeys for j in t))
queue=deque(selected)
while queue:
 i=queue.popleft()
 for j in tri[i]:
  for other in neighbors[keys[j]]:
   if eligible[other] and other not in selected:selected.add(other);queue.append(other)
mask=np.array([i in selected for i in range(len(tri))])
out={'sourcePart':'Part_Face_NEUTRAL_00','sourceTriangles':len(tri),'moveToHair':sorted(selected),'status':'candidate; requires visual acceptance and Unity validation'}
(root/'hair-triangle-keys.txt').write_text('\n'.join(sorted('|'.join(sorted(','.join(map(str,keys[j])) for j in tri[i])) for i in selected)))
(root/'face-hair-partition-candidate.json').write_text(json.dumps(out))
meshes[names[0]]=(v,uv,tri[~mask]);meshes['Part_Hair_Front_00']=(v,uv,tri[mask])
png(root/'candidate-face.png',render([names[0]]))
png(root/'candidate-head.png',render(names+['Part_Hair_Front_00'],True))
print('Candidate face triangles moved to hair:',len(selected),'remaining:',int((~mask).sum()))
visited=set();components=[]
for seed in range(len(tri)):
 if seed in visited:continue
 comp=[];todo=[seed];visited.add(seed)
 while todo:
  i=todo.pop();comp.append(i)
  for j in tri[i]:
   for other in neighbors[keys[j]]:
    if other not in visited:visited.add(other);todo.append(other)
 components.append(comp)
print('Connected components (size, candidate hair count):',[(len(c),sum(i in selected for i in c)) for c in sorted(components,key=len,reverse=True)[:25]])
(root/'face-components.json').write_text(json.dumps(components))
