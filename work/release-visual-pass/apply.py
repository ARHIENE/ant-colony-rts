from pathlib import Path
p=Path('work/release-visual-pass/draft/MapGenerator.cs');s=p.read_text(encoding='utf-8-sig').replace('if (!spawnObjects.Any(s => s.prefab != null)) SpawnMissingDressing();','if (!styled) SpawnMissingDressing();');p.write_text(s,encoding='utf-8')
root=Path('Assets/_Project/Scripts'); work=Path('work/release-visual-pass')
for name in ['MapGenerator.cs','CommanderOverhead.cs','MenuViewport.cs','BuildScreen.cs']:
 f=next(root.rglob(name)); before=(work/'before'/name).read_bytes()
 if f.read_bytes()!=before: raise Exception('Concurrent edit '+name)
for name in ['MapGenerator.cs','CommanderOverhead.cs','MenuViewport.cs','BuildScreen.cs']:
 f=next(root.rglob(name));f.write_bytes((work/'draft'/name).read_bytes())
print('Applied 4 guarded files')
