from pathlib import Path
import json,urllib.request,uuid
root=Path('E:/Git/ant')
results=[]
for job in json.loads((root/'work/save-20261028-followup/upload-jobs.json').read_text(encoding='utf-8')):
 boundary='save27'+uuid.uuid4().hex
 prefix=('Content-Disposition: form-data; name="'+job.get('upload_form_field','file')+'"; filename="'+job['filename']+'"\r\nContent-Type: image/png\r\n\r\n').encode()
 data=b'--'+boundary.encode()+b'\r\n'+prefix+(root/('Temp/save-20261028-followup/'+job['filename'])).read_bytes()+b'\r\n--'+boundary.encode()+b'--\r\n'
 headers=dict(job['upload_headers']);headers['Content-Type']='multipart/form-data; boundary='+boundary
 request=urllib.request.Request(job['upload_url'],data=data,headers=headers,method='POST')
 with urllib.request.urlopen(request,timeout=60) as response:
  payload=json.load(response)
  status=response.status
 results.append({'name':job['name'],'response':payload})
 print(job['name'],status,flush=True)
(root/'work/save-20261028-followup/upload-results.json').write_text(json.dumps(results,ensure_ascii=False,indent=2),encoding='utf-8')


