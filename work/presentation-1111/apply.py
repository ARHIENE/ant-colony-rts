from pathlib import Path
root=Path.cwd();work=root/'work/presentation-1111'
targets={
 'BuildScreen.cs':'Assets/_Project/Scripts/UI/BuildScreen.cs',
 'GameMenuController.cs':'Assets/_Project/Scripts/UI/GameMenuController.cs',
 'CharacterPortraits.cs':'Assets/_Project/Scripts/UI/CharacterPortraits.cs',
 'BetaProgress.cs':'Assets/_Project/Scripts/UI/BetaProgress.cs',
 'GoalChain.cs':'Assets/_Project/Scripts/UI/GoalChain.cs',
 'MapGenerator.cs':'Assets/_Project/Scripts/Map/MapGenerator.cs',
 'TerrainBlend.shader':'Assets/_Project/Shaders/TerrainBlend.shader',
}
lease=(root/'Temp/unity-lock.txt').read_text(encoding='utf-8-sig')
assert lease.startswith('PRESENTATION-1111'),lease
for name,target in targets.items():
 assert (root/target).read_bytes()==(work/'before'/name).read_bytes(),f'Concurrent edit: {target}'
for name,target in targets.items(): (root/target).write_bytes((work/'draft'/name).read_bytes())
print('Applied 7 scoped files after checking concurrent edits')
