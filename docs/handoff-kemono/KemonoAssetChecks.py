"""정적 반입 검사. Unity Play/Animator 평가 검사를 대신하지 않는다."""
from pathlib import Path
import hashlib
import json
import re

ROOT = Path(__file__).resolve().parents[2]
EXPORT = ROOT / 'art/production-kit-20261019/exports'

def same(source, target):
    assert target.is_file(), target
    assert hashlib.sha256(source.read_bytes()).digest() == hashlib.sha256(target.read_bytes()).digest(), target

same(EXPORT/'character/Kemono_Character.fbx', ROOT/'Assets/Resources/Kemono/Kemono_Character.fbx')
clips = list((EXPORT/'character').glob('Kemono@*.fbx')) + list((EXPORT/'animations').glob('Kemono@*.fbx'))
assert len(clips) == 88
names = [p.stem.split('@')[1].removeprefix('PK_') for p in clips]
assert len(set(names)) == 88
for source in clips:
    same(source, ROOT/'Assets/_Project/Art/Kemono/Anim'/source.name)
handheld = list((EXPORT/'handheld').glob('*.fbx'))
assert len(handheld) == 71
for source in handheld:
    same(source, ROOT/'Assets/Resources/Kemono/Handheld'/source.name)
facilities = dict(OilDrill='OilDerrick', OilRefinery='Refinery', FluidTank='LiquidTank', FluidPipe='PipeL_Straight', OilDeposit='Deposit_Oil_Normal',
                  FuelCharger='FillingStation', CrashWreck='CrashWreck', School='School', CitizenClinic='CitizenClinic', CivicPublicService='CivicPublicService',
                  PlanterBed='PlanterBed', WaterSource='WaterSource', WaterPump='WaterPump', SteamGenerator='SteamGenerator')
patch = (ROOT/'docs/handoff-kemono/KEMONO-UNITY-1024.patch').read_text(encoding='utf-8')
for kind, model in facilities.items():
    same(EXPORT/'facilities'/f'{model}.fbx', ROOT/'Assets/Resources/Facilities'/f'CC_{model}.fbx')
    assert f'BuildingKind.{kind}, "CC_{model}"' in patch
same(ROOT/'Assets/ThirdParty/LowPoly Fantasy Village/Models/Props/Well.fbx', ROOT/'Assets/Resources/Facilities/CC_ManualWell.fbx')
assert 'BuildingKind.ManualWell, "CC_ManualWell"' in patch
assert 'BuildingKind.StarterMushroomBed, "CC_MushroomFarm"' in patch
assert (ROOT/'Assets/Resources/Facilities/CC_MushroomFarm.fbx').is_file()
importer = (ROOT/'Assets/_Project/Editor/KemonoImport.cs').read_text(encoding='utf-8-sig')
loop_block = importer.split('ProductionLoops = {', 1)[1].split('};', 1)[0]
loops = set(re.findall(r'"([^"]+)"', loop_block))
expected = {v['clip'] for v in json.loads((EXPORT/'animations/export-list.json').read_text()).values() if v['loop']}
assert loops == expected, (loops ^ expected)
assert 'State("Attack", clip)' in importer and 'name == "SwordAttack"' in importer
runtime = (ROOT/'Assets/_Project/Scripts/Units/KemonoModel.cs').read_text(encoding='utf-8-sig')
for tool in re.findall(r'"(Tool_[A-Za-z]+_Stone)"', runtime):
    assert (ROOT/'Assets/Resources/Kemono/Handheld'/f'{tool}.fbx').is_file(), tool
assert 'playedClip == clip' in patch
print('PASS static assets: 175 identical FBX copies, 88 unique clips, 25 production loops, 16 facility mappings, tool resources, Attack alias.')
print('Unity import/avatar/Play/screenshots/regression checks: NOT RUN.')

