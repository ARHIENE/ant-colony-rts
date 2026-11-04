from pathlib import Path
w=Path('codex-work~/src/AgentScripts')
for name in ('TransportRouteChecks','SettlementDefenseChecks'):
 p=w/(name+'.cs'); raw=p.read_bytes(); s=raw.decode('utf-8-sig'); nl='\r\n' if '\r\n' in s else '\n'; s=s.replace('\r\n','\n')
 if name=='TransportRouteChecks': s=s.replace('rm.AddCapacity(ResourceType.Wood, 10000); rm.AddCapacity(ResourceType.Soil, 10000);','rm.AddCapacity(ResourceType.Soil, 10000); // 木材・흙은 같은 재료 한도를 사용한다.'.replace('木材・','목재·'))
 else:
  s=s.replace('ship.DepositResources(ResourceType.Food, 17);','ship.DepositResources(ResourceType.Wood, 17); // G05 보급과 화물 보존 검사를 분리한다.')
  s=s.replace('ship.GetCargo(ResourceType.Food) == 17','ship.GetCargo(ResourceType.Wood) == 17')
  s=s.replace('var homeFood = resources.GetAmount(ResourceType.Food);','var homeWood = resources.GetAmount(ResourceType.Wood);')
  s=s.replace('resources.GetAmount(ResourceType.Food) == homeFood + 17','resources.GetAmount(ResourceType.Wood) == homeWood + 17')
 p.write_bytes((b'\xef\xbb\xbf' if raw.startswith(b'\xef\xbb\xbf') else b'')+s.replace('\n',nl).encode('utf-8'))
