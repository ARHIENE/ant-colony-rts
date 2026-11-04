import json, subprocess, pathlib, collections, datetime
root=pathlib.Path('E:/Git/ant'); out=root/'work/save-20261026'
def git(*args): return subprocess.check_output(['git',*args],cwd=root).decode('utf-8',errors='replace')
changed=git('diff','--name-only').splitlines(); new=git('ls-files','--others','--exclude-standard').splitlines()
relevant=[p for p in new if p.startswith(('Assets/_Project/','Assets/Resources/','AgentScripts/','docs/'))]
assets=[root/p for p in relevant if p.startswith('Assets/Resources/') and (root/p).is_file()]
inv={'tracked':changed,'untracked_relevant':relevant,'groups':dict(collections.Counter('/'.join(p.split('/')[:2]) for p in new)), 'asset_bytes':sum(p.stat().st_size for p in assets)}
(out/'inventory.json').write_text(json.dumps(inv,ensure_ascii=False,indent=2),encoding='utf-8')
print('TRACKED',len(changed),'UNTRACKED RELEVANT',len(relevant),'RESOURCE MB',round(inv['asset_bytes']/2**20,1));print('UNTRACKED GROUPS',json.dumps(inv['groups'],ensure_ascii=False));print('NEW SOURCE',*[p for p in relevant if p.endswith('.cs')],sep='\n')
for path in sorted(pathlib.Path('C:/Users/Shim Hyeonyeop/.claude/projects/E--Git').glob('*.jsonl'),key=lambda p:p.stat().st_mtime,reverse=True)[:3]:
    messages=[]; calls=collections.Counter()
    for line in path.open(encoding='utf-8'):
        try:d=json.loads(line)
        except:continue
        if d.get('type')=='assistant':
            content=d.get('message',{}).get('content',[])
            texts=[x.get('text','') for x in content if x.get('type')=='text']
            if texts:messages.append({'timestamp':d.get('timestamp'), 'text':'\n'.join(texts)})
            for x in content:
                if x.get('type')=='tool_use':calls[x.get('name')]+=1
    (out/(path.stem+'-assistant.json')).write_text(json.dumps(messages,ensure_ascii=False,indent=2),encoding='utf-8')
    print('\nCLAUDE',path.name,'MESSAGES',len(messages),'TOOLS',calls.most_common(5))
    for m in messages[-4:]:print(m['timestamp'],m['text'][:1900])
print('LARGE RESOURCES',[(str(p.relative_to(root)),round(p.stat().st_size/2**20,2)) for p in sorted(assets,key=lambda p:p.stat().st_size,reverse=True)[:5]])
