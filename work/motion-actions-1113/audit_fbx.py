"""Read-only FBX structural audit. No conversions or animation rewriting."""
from pathlib import Path
import struct, zlib, hashlib, json

ROOT = Path('F:/UnityProjects/ant-colony-rts')

def read(path):
    data=Path("\\\\?\\"+str(path)).read_bytes()
    if not data.startswith(b'Kaydara FBX Binary'): raise ValueError('Not binary FBX: '+str(path))
    version=struct.unpack_from('<I',data,23)[0]
    wide=version>=7500
    header='<QQQB' if wide else '<IIIB'
    size=struct.calcsize(header)
    def node(pos):
        end,count,plen,nlen=struct.unpack_from(header,data,pos)
        if end==0:return None,pos+size
        pos+=size;name=data[pos:pos+nlen].decode();pos+=nlen;props=[]
        for _ in range(count):
            kind=chr(data[pos]);pos+=1
            formats={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
            if kind in formats:
                fmt='<'+formats[kind];val=struct.unpack_from(fmt,data,pos)[0];pos+=struct.calcsize(fmt)
            elif kind in 'SR':
                length=struct.unpack_from('<I',data,pos)[0];pos+=4;val=data[pos:pos+length];pos+=length
                if kind=='S':val=val.decode('utf-8',errors='replace')
            elif kind in 'fdlibc':
                length,encoding,nbytes=struct.unpack_from('<III',data,pos);pos+=12
                raw=data[pos:pos+nbytes];pos+=nbytes
                if encoding:raw=zlib.decompress(raw)
                val={'count':length,'sha256':hashlib.sha256(raw).hexdigest()}
            else:raise ValueError('Unknown property '+kind)
            props.append(val)
        children=[]
        while pos<end:
            child,pos=node(pos)
            if child is None:break
            children.append(child)
        return {'name':name,'props':props,'children':children},end
    nodes=[];pos=27
    while pos<len(data):
        n,pos=node(pos)
        if n is None:break
        nodes.append(n)
    return version,nodes

def audit(path):
    version,nodes=read(path)
    objects=next(n for n in nodes if n['name']=='Objects')['children']
    models=[n for n in objects if n['name']=='Model']
    bones=[n for n in models if n['props'][2]=='LimbNode']
    geometry=[n for n in objects if n['name']=='Geometry']
    clusters=[n for n in objects if n['name']=='Deformer' and n['props'][2]=='Cluster']
    def clean(n):return n.split('\x00')[0]
    rest=[]
    for n in bones:
        p=next((x['children'] for x in n['children'] if x['name']=='Properties70'),[])
        rest.append({'name':clean(n['props'][1]),'transforms':[x['props'] for x in p if x['props'][0] in ['Lcl Translation','Lcl Rotation','Lcl Scaling','PreRotation','PostRotation']]})
    return {'file':str(path),'version':version,'bones':rest,'geometry':[{'name':clean(n['props'][1]),'streams':{c['name']:c['props'] for c in n['children'] if c['name'] in ['Vertices','PolygonVertexIndex']}} for n in geometry], 'cluster_count':len(clusters),'animations':sum(n['name']=='AnimationStack' for n in objects)}

paths=list((ROOT/'art/meshy-1113').glob('Meshy_AI_Meadow*/*/*.fbx'))
paths += [ROOT/'Assets/_Project/Art/MeshyAnimation1106'/n for n in ['Heavy_Hammer_Swing.fbx','Rerig_All.fbx']]
reports=[audit(p) for p in paths]
(ROOT/'work/motion-actions-1113/fbx-structure.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
for r in reports:print(Path(r['file']).name,len(r['bones']),'bones',r['cluster_count'],'clusters',r['animations'],'animations')
for new in reports[:2]:
    a={b['name']:b['transforms'] for b in new['bones']}
    for old in reports[2:]:
        b={x['name']:x['transforms'] for x in old['bones']}
        print(Path(new['file']).stem[-20:],Path(old['file']).stem,'names_equal',a.keys()==b.keys(),'rest_equal',a==b)

