from pathlib import Path
import zipfile,shutil,json
root=Path('F:/UnityProjects/ant-colony-rts/art/life-assets-1112')
src=Path('C:/Users/gusdu/Downloads/74345b2b-30ac-46a3-bc00-4e92647d3681.tmp')
with zipfile.ZipFile(src) as z:
 assert z.testzip() is None
 dest=root/'models'/'MedicineBench';dest.mkdir(parents=True,exist_ok=True)
 for entry in z.infolist():
  if entry.is_dir():continue
  name=Path(entry.filename).name
  if name.endswith('.fbx'):name='MedicineBench.fbx'
  elif 'basecolor' in name.lower() or 'base_color' in name.lower():name='BaseColor.png'
  elif 'normal' in name.lower():name='Normal.png'
  (dest/name).write_bytes(z.read(entry))
 shutil.copy2(src,root/'MedicineBench.zip')
 print([(p.name,p.stat().st_size) for p in dest.iterdir()])
