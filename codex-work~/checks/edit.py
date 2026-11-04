from pathlib import Path
import hashlib,json
r=Path('E:/Git/ant'); w=r/'codex-work~'; names=['WorldMapChecks','SettlementDefenseChecks','TransportRouteChecks']; protected=['AgentScripts/'+n+'.cs' for n in names+['ImplG06Checks','FishingChecks']]
(w/'checks/reports/original-hashes.json').write_text(json.dumps({p:hashlib.sha256((r/p).read_bytes()).hexdigest() for p in protected},indent=2),encoding='utf-8')
def edit(name,fn):
 p='AgentScripts/'+name+'.cs'; raw=(r/p).read_bytes(); bom=raw.startswith(b'\xef\xbb\xbf'); s=raw.decode('utf-8-sig'); nl='\r\n' if '\r\n' in s else '\n'; s=s.replace('\r\n','\n'); s=fn(s); (w/'src'/p).write_bytes((b'\xef\xbb\xbf' if bom else b'')+s.replace('\n',nl).encode('utf-8'))
def world(s):
 old='        homeCommander.SetJobEnabled(CommanderJobs.Hauling, true);'
 assert s.count(old)==1
 return s.replace(old,'        // 건설 작업이 자재를 직접 배송한다. 일반 운반을 켜면 G02 바닥 돌 20개도 재고에 합쳐진다.')
def defense(s):
 anchor='        Set(captive, "carriedType", ResourceType.Food); Set(captive, "carriedAmount", 4.5f);'
 assert s.count(anchor)==1
 s=s.replace(anchor,'''        // 포획/탈출 난수 사이에 OnDowned의 부상 난수가 끼지 않도록 쓰러진 수비자를 먼저 준비한다.
        var poolBefore = AntPool.Instance.Total;
        var captiveTroops = captive.TroopCount;
        var deathMode = GameSession.Instance.Options.commanderDeath;
        try
        {
            GameSession.Instance.Options.commanderDeath = CommanderDeathMode.Gentle;
            captive.TakeDamage(float.MaxValue);
        }
        finally { GameSession.Instance.Options.commanderDeath = deathMode; }
        Check(!captive.IsDead && captive.PersonalHealth == 0 && !captive.HasTroops
            && AntPool.Instance.Total == poolBefore - captiveTroops, "downed fixture loses troops exactly once");
        // 의료의 무작위 출혈/감염은 별도 검사 범위. 부상 상태는 유지해 CaptureInjury의 추가 난수도 막는다.
        captive.Health.wounds.Clear(); captive.Health.illnesses.Clear(); captive.Health.bloodLoss = 0;
        MedicalRules.AddWound(captive.Health, BodyPart.Thorax, WoundKind.Bruise, 1).tended = true;
''' + anchor)
 old='''        var poolBefore = AntPool.Instance.Total;
        var captiveTroops = captive.TroopCount;
        // RNG를 한 번만 고정해 같은 상실 사건에서 포로/탈출 두 갈래를 모두 검증한다.'''
 assert s.count(old)==1
 s=s.replace(old,'''        Check(site.Settlement.Garrison.SequenceEqual(new[] { captive, escapee }), "capture fixture preserves defender order");
        // 이미 쓰러지고 부상 중인 첫 장수의 포획은 추가 부상 난수를 소비하지 않는다.''')
 s=s.replace('''            "one commander captured and hidden");''','''            "one commander captured and hidden: prisoners=" + defense.Prisoners.Count
            + " captive=" + captive.IsCaptive + " dead=" + captive.IsDead + " health=" + captive.PersonalHealth
            + " captor=" + (captive.Captor == site) + " active=" + captive.gameObject.activeSelf);''')
 s=s.replace('// 쓰러진 채 구출된 장수는 회복(60초)한 뒤에 탑승할 수 있다.','// G11: 출혈·질병 없는 장수는 하루 후 의식 회복. 60초 회복 전제는 폐기됐다.')
 s=s.replace('captive.TickDuty(Core.GameBalance.CommanderRecoverySeconds + 1);','captive.PersonalState.meal.satiety = 100;\n        captive.TickDuty(GameCalendar.SecondsPerDay + 1);')
 return s
def transport(s):
 # Keep the two-resource conservation assertions exact; expedition food remains enabled.
 s=s.replace('ResourceType.Food','ResourceType.Wood')
 s=s.replace('var food = nodes.First(n => n.ResourceType == ResourceType.Wood);','var wood = nodes.First(n => n.ResourceType == ResourceType.Food);')
 s=s.replace('food.Extract(food.AmountRemaining); soil.Extract(soil.AmountRemaining);','wood.Extract(wood.AmountRemaining); soil.Extract(soil.AmountRemaining);')
 s=s.replace('food.AddStock(12); soil.AddStock(7);','// G05 보급은 식량 화물을 정상 소비한다. 보존량 검사는 소비되지 않는 목재·흙으로 격리한다.\n        wood.ConfigureLoot(ResourceType.Wood, 12); soil.AddStock(7);')
 s=s.replace('food.AddStock(', 'wood.AddStock(').replace('beforeFood','beforeWood')
 s=s.replace('rm.TrySpend(100, 100);','Check(rm.TrySpend(ResourceType.Wood, 100) && rm.TrySpend(ResourceType.Soil, 100), "free room for second delivery");')
 s=s.replace('''&& rm.GetAmount(ResourceType.Wood) == beforeWood, "both resources remain cargo until arrival");''','''&& rm.GetAmount(ResourceType.Wood) == beforeWood, "both resources remain cargo until arrival: wood="
            + ship.GetCargo(ResourceType.Wood) + "/12 soil=" + ship.GetCargo(ResourceType.Soil) + "/7 homeWood="
            + rm.GetAmount(ResourceType.Wood) + "/" + beforeWood + " foodDebt=" + ship.FoodDebt);''')
 return s
edit('WorldMapChecks',world); edit('SettlementDefenseChecks',defense); edit('TransportRouteChecks',transport)
with (w/'IMPLEMENTATION_BOARD.md').open('a',encoding='utf-8') as f: f.write('\n## CHECKS-1025\nCodex: WorldMapChecks / SettlementDefenseChecks / TransportRouteChecks 사본만 수정. 게임 코드 수정 없음. ImplG06Checks / FishingChecks / UI 제외.\n')
print('Prepared three check copies with original newline/BOM.')
