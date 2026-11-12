from pathlib import Path
import os
p=Path('Assets/_Project/Scripts/Units/CommanderAnt.cs')
s=p.read_text(encoding='utf-8-sig')
s=s.replace('평시에는 자동 작업하며, 출전 시 배정된 일반개미가 개인 체력보다 먼저 피해를 받는다.', '평시에는 자동 작업하며, 전투는 장수 개인의 체력으로 판정한다.')
s=s.replace('base.GatherRate * Mathf.Max(1, troopCount) * WorkRate', 'base.GatherRate * WorkRate')
a=s.index('        // 대기 중인 일반개미를 이 장수에게 배정한다.')
b=s.index('        // 배정된 병력을 대기 풀로 돌려보낸다.',a)
s=s[:a]+'        // 구 UI·저장 도구의 호출 호환용. 시민 병력을 새로 배정하지 않는다.\n        public bool TryAssign(int count) => false;\n\n'+s[b:]
a=s.index('            var troopDamage = Mathf.Min(damage, TroopHealth);')
b=s.index('            // 떠난(적대) 장수',a)
s=s[:a]+s[b:]
t=p.with_suffix('.cs.takeover');t.write_text(s,encoding='utf-8');os.replace(t,p)
