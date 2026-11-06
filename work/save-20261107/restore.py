from pathlib import Path
import base64,hashlib,io,json,tarfile
root=Path(__file__).resolve().parents[2]; here=Path(__file__).parent
raw=base64.b64decode(''.join(p.read_text() for p in sorted(here.glob('archive.b64.part*'))))
manifest={r['path']:r for r in json.loads((here/'manifest.json').read_text())}
with tarfile.open(fileobj=io.BytesIO(raw),mode='r:xz') as tar:
 for member in tar.getmembers():
  if member.name not in manifest or not member.isfile():raise ValueError('Unexpected archive entry')
  target=(root/member.name).resolve()
  if not target.is_relative_to(root):raise ValueError('Unsafe path')
  data=tar.extractfile(member).read()
  if hashlib.sha256(data).hexdigest()!=manifest[member.name]['sha256']:raise ValueError('Checksum mismatch')
  target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
print('Restored and verified archived originals')
