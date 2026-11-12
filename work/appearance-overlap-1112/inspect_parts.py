import pathlib,re,struct,zlib,json
import numpy as np
root=pathlib.Path('work/appearance-overlap-1112')
def mesh(name):
 s=pathlib.Path('Assets/Resources/MeshyNative/SourceParts/Rig0/'+name+'.asset').read_text()
 count=int(re.search(r'm_VertexCount: (\d+)',s)[1]); blob=bytes.fromhex(re.search(r'_typelessdata: ([0-9a-f]+)',s)[1])
 v=np.ndarray((count,3),'<f4',blob,0,(40,4)).copy()
 uv=np.ndarray((count,2),'<f4',blob,(count*40+15)//16*16,(8,4)).copy()
 tri=np.frombuffer(bytes.fromhex(re.search(r'm_IndexBuffer: ([0-9a-f]+)',s)[1]),'<u4').reshape(-1,3)
 return v,uv,tri
def png(path,img):
 h,w=img.shape[:2]
 def chunk(t,b):return struct.pack('!I',len(b))+t+b+struct.pack('!I',zlib.crc32(t+b)&0xffffffff)
 path.write_bytes(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',w,h,8,2,0,0,0))+chunk(b'IDAT',zlib.compress(b''.join(b'\0'+row.tobytes() for row in img)))+chunk(b'IEND',b''))
w,h,stride=map(int,(root/'texture-size.txt').read_text(encoding='utf-8-sig').split()); tex=np.fromfile(root/'texture.bgra',np.uint8).reshape(h,stride//4,4)[:,:w,:3][:,:,::-1]
names=['Part_Face_NEUTRAL_00','Part_Hair_BOB_00','Part_Ears_ANTENNA_00']
meshes={n:mesh(n) for n in names}
def render(selected,colored=False):
 img=np.full((640,640,3),35,np.uint8); depth=np.full((640,640),-np.inf)
 for n in selected:
  v,uv,tri=meshes[n]; p=np.column_stack((320+v[:,0]*1000,610-(v[:,1]-1.05)*1000))
  for ids in tri:
   q=p[ids]; lo=np.maximum(np.floor(q.min(0)).astype(int),0);hi=np.minimum(np.ceil(q.max(0)).astype(int),639)
   if np.any(hi<lo):continue
   xx,yy=np.meshgrid(np.arange(lo[0],hi[0]+1)+.5,np.arange(lo[1],hi[1]+1)+.5)
   a,b,c=q; den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1])
   if abs(den)<1e-9:continue
   u=((b[1]-c[1])*(xx-c[0])+(c[0]-b[0])*(yy-c[1]))/den; vv=((c[1]-a[1])*(xx-c[0])+(a[0]-c[0])*(yy-c[1]))/den; z=1-u-vv
   zz=u*v[ids[0],2]+vv*v[ids[1],2]+z*v[ids[2],2]; d=depth[lo[1]:hi[1]+1,lo[0]:hi[0]+1]; mask=(u>=0)&(vv>=0)&(z>=0)&(zz>d)
   tuv=u[...,None]*uv[ids[0]]+vv[...,None]*uv[ids[1]]+z[...,None]*uv[ids[2]]
   color=tex[np.clip(((1-tuv[:,:,1])*h).astype(int),0,h-1),np.clip((tuv[:,:,0]*w).astype(int),0,w-1)].copy()
   if colored and 'Hair' in n:color=(color.astype(float)*np.array([.9,.35,.65])).astype(np.uint8)
   patch=img[lo[1]:hi[1]+1,lo[0]:hi[0]+1];patch[mask]=color[mask];d[mask]=zz[mask]
 return img
if __name__=='__main__':
 png(root/'face-only.png',render([names[0]]))
 png(root/'head-colored-hair.png',render(names,True))
 print(json.dumps({n:{'vertices':len(v),'triangles':len(t),'bounds':[v.min(0).tolist(),v.max(0).tolist()]} for n,(v,u,t) in meshes.items()}))
