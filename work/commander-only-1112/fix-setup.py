from pathlib import Path
p=Path('AgentScripts/KiteUser1112.cs');s=p.read_text(encoding='utf-8-sig');s=s.replace(' public static async Task<string> Setup(){',' public static async Task<string> Setup(){\n  var deadline=DateTime.UtcNow.AddSeconds(90);\n  while(SaveSystem.Busy){if(DateTime.UtcNow>deadline)throw new Exception("initial load timeout");await Task.Delay(50);}')
s=s.replace('  while(SaveSystem.Busy||WaveDefense.Instance==null)await Task.Delay(50);','  while(SaveSystem.Busy||WaveDefense.Instance==null||CommanderRoster.Instance==null||CommanderRoster.Instance.Commanders.Count(c=>c!=null&&c.IsColonyMember)<3){if(DateTime.UtcNow>deadline)throw new Exception("new game timeout");await Task.Delay(50);}')
p.write_text(s,encoding='utf-8')
