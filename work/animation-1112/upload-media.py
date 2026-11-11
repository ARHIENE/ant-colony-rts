from pathlib import Path
import json,urllib.request,uuid,concurrent.futures
root=Path.cwd()
items=json.loads((root/'Temp/notion-1112-uploads.json').read_text())
out=root/'work/animation-1112/notion-uploads';out.mkdir(exist_ok=True)
def send(item):
 p=Path(item['path']);dest=out/(p.name+'.json')
 if dest.exists():return p.name+' already sent'
 boundary='----Codex'+uuid.uuid4().hex
 body=(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="{item["filename"]}"\r\nContent-Type: {item["content_type"]}\r\n\r\n').encode()+p.read_bytes()+f'\r\n--{boundary}--\r\n'.encode()
 headers=dict(item['upload_headers']);headers['Content-Type']='multipart/form-data; boundary='+boundary
 req=urllib.request.Request(item['upload_url'],data=body,headers=headers,method='POST')
 with urllib.request.urlopen(req,timeout=120) as response:data=json.load(response)
 dest.write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf8')
 return p.name+' uploaded'
with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
 for result in pool.map(send,items):print(result,flush=True)
