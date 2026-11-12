import re,pathlib,numpy as np
root=pathlib.Path('Assets/Resources/MeshyNative/SourceParts');data=[]
names=['Part_Face_NEUTRAL_00','Part_Hair_BOB_00']
for i in range(2):
 for name in names:
  s=(root/('Rig'+str(i))/(name+'.asset')).read_text()
  count=int(re.search(r'm_VertexCount: (\d+)',s)[1]);b=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',s)[1])
  v=np.ndarray((count,3),'<f4',b,0,(40,4)).copy();t=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',s)[1]),'<u4').reshape(-1,3)
  uv=np.ndarray((count,2),'<f4',b,(count*40+15)//16*16,(8,4)).copy()
  keys=np.round(uv*100000).astype(int)
  data.append(set('|'.join(sorted(','.join(map(str,keys[j])) for j in tri)) for tri in t))
import sys,json
sys.path.insert(0,str(pathlib.Path(__file__).parent));import inspect_parts as a
v,uv,tri=a.meshes[a.names[0]];keys=np.round(uv*100000).astype(int)
ids=json.loads((a.root/'face-hair-partition-candidate.json').read_text())['moveToHair']
expected=set('|'.join(sorted(','.join(map(str,keys[j])) for j in tri[i])) for i in ids)
(a.root/'hair-uv-triangle-keys.txt').write_text('\n'.join(sorted(expected)))
for i in range(2):print(names[i],'triangle sets',len(data[i]),len(data[i+2]),'shared',len(data[i]&data[i+2]))
for i in [0,2]:print('Rig',i//2,'candidate keys match',len(expected&data[i]),'of',len(expected))
