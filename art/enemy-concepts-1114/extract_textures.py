import json,struct,sys
from pathlib import Path
root=Path('F:/UnityProjects/ant-colony-rts')
names=sys.argv[1:] or ['WallRam','NeedleStalker','BloomMender']
for name in names:
    src=root/'MESHY'/f'{name}_1114_SmartRig.glb'
    if not src.exists(): continue
    p=src.read_bytes(); n=struct.unpack_from('<I',p,12)[0]; j=json.loads(p[20:20+n]); binary=p[28+n:]
    out=root/'art/enemy-concepts-1114'/name/'Textures'; out.mkdir(parents=True,exist_ok=True)
    material=j['materials'][0]
    pbr=material['pbrMetallicRoughness']
    slots={'BaseColor':pbr.get('baseColorTexture'),'Normal':material.get('normalTexture'),'MetallicRoughness':pbr.get('metallicRoughnessTexture')}
    for label,slot in slots.items():
        if slot is None: continue
        img=j['images'][j['textures'][slot['index']]['source']]
        view=j['bufferViews'][img['bufferView']]; start=view.get('byteOffset',0)
        assert img['mimeType']=='image/png',img['mimeType']
        filename=label+'.png'
        (out/filename).write_bytes(binary[start:start+view['byteLength']])
    print(name,'textures extracted')
