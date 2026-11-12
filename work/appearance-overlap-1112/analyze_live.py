import json,pathlib,numpy as np
root=pathlib.Path('work/appearance-overlap-1112')
report=[]
for path in root.glob('*-live.json'):
 d=json.loads(path.read_text());v=np.array([[p[k] for k in 'xyz'] for p in d['source']]);b=np.array([[p[k] for k in 'xyz'] for p in d['baked']]);tri=np.array(d['triangles']).reshape(-1,3)
 print(path.name,{k:len(value) for k,value in d.items()},'triangleMax',int(tri.max()))
 if tri.max()>=len(v) or tri.max()>=len(b):
  report.append({'part':path.name,'invalidTriangleIndices':int((tri>=min(len(v),len(b))).sum()),'maxIndex':int(tri.max()),'sourceVertexCount':len(v),'bakedVertexCount':len(b)})
  continue
 a=np.column_stack((v,np.ones(len(v))));fit=np.linalg.lstsq(a,b,rcond=None)[0];res=np.linalg.norm(a@fit-b,axis=1)
 lengths=np.linalg.norm(v[tri[:,0]]-v[tri[:,1]],axis=1);bl=np.linalg.norm(b[tri[:,0]]-b[tri[:,1]],axis=1)
 report.append({'part':path.name,'vertices':len(v),'triangles':len(tri),'sourceBounds':[v.min(0).tolist(),v.max(0).tolist()],'bakedBounds':[b.min(0).tolist(),b.max(0).tolist()],'rigidResidualMax':float(res.max()),'rigidResidualMean':float(res.mean()),'edgeRatioQuantiles':np.quantile(bl/np.maximum(lengths,1e-10),[0,.5,.99,1]).tolist(),'boneIndices':sorted(set(w['m_BoneIndex0'] for w in d['weights'])),'weight0':sorted(set(w['m_Weight0'] for w in d['weights']))[:10]})
(root/'live-mesh-analysis.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
