from pathlib import Path
p=Path('Assets/_Project/Scripts/Units/CommanderAnt.cs');s=p.read_text(encoding='utf-8-sig')
s=s.replace('public int TroopCount => troopCount;', 'public int TroopCount => 0; // Legacy save/UI accessor; commanders have no troops.')
s=s.replace('public bool HasTroops => troopCount > 0;', 'public bool HasTroops => false;')
s=s.replace('public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)\n        {','public override void Initialize(UnitData data, ObjectPool sourcePool, GameObject prefab)\n        {\n            troopCount = 0; pendingDamage = 0; // Ignore obsolete serialized troop allocations.')
s=s.replace('        // 병력 수만큼 부대 전체의 전투력/채집량이 늘어난다.\n        // 레벨 공격 보너스는 기존 공격력과 같이 1마리분에 더해진 뒤 병력 수(병력 0이면 장수 본인 1인분)만큼 곱해진다.\n        // 신중형 성격은 1마리분 공격력을 깎으므로 0 밑으로 내려가지 않게 막는다(병력 수를 곱하면 부호가 증폭된다).', '        // 장수 자신의 무기·기술·특성과 부상으로 전투력을 계산한다.')
s=s.replace('        // 피해는 부대 전체가 나눠 받고, HP 1이 쌓일 때마다 병력이 실제로 줄어든다(환급 없음).','        // 방어와 장비 보정 후 장수 개인 체력에 피해를 적용한다.')
q=Path('work/commander-only-1112/pending/Units/CommanderAnt.cs');q.parent.mkdir(parents=True,exist_ok=True);q.write_text(s,encoding='utf-8')
