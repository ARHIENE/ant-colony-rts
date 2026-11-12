from pathlib import Path
import zipfile,shutil,hashlib
root=Path('F:/UnityProjects/ant-colony-rts')
source=root/'MESHY/Meshy_AI_One_isolated_single_patient_bed_for_a_20261112_texture_remesh_tri_fbx.zip'
with zipfile.ZipFile(source) as z:
 bad=z.testzip()
 assert bad is None,bad
 dest=root/'art/life-assets-1112/models/Infirmary';dest.mkdir(parents=True,exist_ok=True)
 for e in z.infolist():
  if e.is_dir():continue
  name=Path(e.filename).name
  if name.endswith('.fbx'):name='Infirmary.fbx'
  elif name.endswith('_tri.png'):name='BaseColor.png'
  elif 'normal' in name.lower():name='Normal.png'
  (dest/name).write_bytes(z.read(e))
 shutil.copy2(source,root/'art/life-assets-1112/Infirmary.zip')
 print('CRC PASS; ZIP bytes=',source.stat().st_size)
 print('SHA256=',hashlib.sha256(source.read_bytes()).hexdigest())
 for p in dest.iterdir():print(p.name,p.stat().st_size)
