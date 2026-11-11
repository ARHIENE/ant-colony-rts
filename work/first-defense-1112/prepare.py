from pathlib import Path
p=Path('AgentScripts/Pacing1111Probe.cs');s=p.read_text(encoding='utf-8-sig').replace('public static class Pacing1111Probe','public static class FirstDefense1112Probe').replace('Temp/pacing-1111-','Temp/first-defense-1112/')
s=s.replace('static string label="before";', 'public static Task<string> Run() => Main("baseline",3,true);\n static string label="before";')
s=s.replace('log="";\n var settings=', 'log=""; Directory.CreateDirectory("Temp/first-defense-1112");\n var settings=')
s=s.replace('var end=DateTime.UtcNow.AddMinutes(5);Time.timeScale=5;', 'var end=DateTime.UtcNow.AddMinutes(7); float captureAt=30; Time.timeScale=5;')
s=s.replace('if(GameCalendar.GameSeconds>=next){next+=60;', '''if(GameCalendar.GameSeconds>=captureAt){ScreenCapture.CaptureScreenshot("Temp/first-defense-1112/"+label+"-prepare-"+(int)captureAt+".png");captureAt+=180;}
if(GameCalendar.GameSeconds>=next){next+=60;''')
needle='Note($"END team={count} wave={w.Wave} remaining={w.RemainingEnemies} screen={GameMenuController.Instance.ScreenName}");Time.timeScale=0;'
rep='''Note($"PREPARATION END team={count} wave={w.Wave} remaining={w.RemainingEnemies} screen={GameMenuController.Instance.ScreenName}");
 if(!w.InWave) throw new Exception("Preparation did not reach natural wave");
 var fighters=CommanderRoster.Instance.Commanders.Where(c=>c!=null&&!c.IsDead).ToArray();
 var local=SceneObjects.Any<LocalIncursions>();
 var camera=SceneObjects.Any<AntColony.Camera.IsometricCameraController>();camera.FocusOn(home);camera.enabled=false;
 SceneObjects.Any<SelectionManager>()?.SelectOnly(fighters[0].GetComponent<SelectableObject>());
 foreach(var c in fighters)c.CommandStop();
 float fightStart=GameCalendar.GameSeconds;float nextFight=0;int orders=0;float firstContact=-1;
 var battleEnd=DateTime.UtcNow.AddMinutes(6);SimulationPause.UserSpeed(1);
 while(w.InWave && GameMenuController.Instance.ScreenName!="COLONY LOST" && GameCalendar.GameSeconds-fightStart<240 && DateTime.UtcNow<battleEnd)
 {
  var enemies=local.Visitors.Where(v=>v!=null&&!v.IsDead&&!v.Retreating&&v.isActiveAndEnabled).ToArray();
  var close=enemies.Where(v=>Vector3.Distance(v.Position,home)<28).ToArray();
  foreach(var c in fighters.Where(c=>c!=null&&!c.IsDead&&!c.Health.Downed))
  {
   if(c.PersonalHealth<30){c.CommandMove(home-new Vector3(0,0,8));continue;}
   var enemy=close.OrderBy(v=>(v.Position-c.Position).sqrMagnitude).FirstOrDefault();
   if(enemy!=null){c.CommandAttack(enemy);orders++;if(firstContact<0)firstContact=GameCalendar.GameSeconds-fightStart;}
  }
  if(GameCalendar.GameSeconds-fightStart>=nextFight){
   nextFight+=10;Note($"FIGHT elapsed={GameCalendar.GameSeconds-fightStart:0} remaining={w.RemainingEnemies} orders={orders} hp="+string.Join(",",fighters.Select(c=>$"{c.CommanderName}:{c.PersonalHealth:0}/down={c.Health.Downed}/sleep={c.IsAsleep}/work={c.IsWorking}"))+" enemyDistances="+string.Join(",",enemies.Select(v=>Vector3.Distance(v.Position,home).ToString("0"))));
   ScreenCapture.CaptureScreenshot("Temp/first-defense-1112/"+label+"-fight-"+(int)nextFight+".png");
  }
  await Task.Delay(1000);
 }
 Note($"BATTLE RESULT cleared={!w.InWave} firstContact={firstContact:0} duration={GameCalendar.GameSeconds-fightStart:0} remaining={w.RemainingEnemies} screen={GameMenuController.Instance.ScreenName}");
 ScreenCapture.CaptureScreenshot("Temp/first-defense-1112/"+label+"-result.png");
 if(!w.InWave && GameMenuController.Instance.ScreenName!="COLONY LOST"){
  foreach(var c in fighters.Where(c=>c!=null&&!c.IsDead))c.CommandStop();
  var recoverStart=GameCalendar.GameSeconds;Time.timeScale=3;
  while(GameCalendar.GameSeconds-recoverStart<60 && DateTime.UtcNow<battleEnd)await Task.Delay(250);
  Note("RECOVERY "+CampaignNextSteps.RecoverySummary(true));ScreenCapture.CaptureScreenshot("Temp/first-defense-1112/"+label+"-recovery.png");
 }
 Time.timeScale=0;'''
assert needle in s;s=s.replace(needle,rep)
Path('AgentScripts/FirstDefense1112Probe.cs').write_text(s,encoding='utf-8')
p=Path('work/first-defense-1112/before/FacilityVisual.cs');s=p.read_text(encoding='utf-8-sig');needle='''            var improved = kind switch''';idx=s.index(needle)
s=s[:idx]+'''            var release = kind switch {
                BuildingKind.Campfire => "Campfire", BuildingKind.Carpentry => "Carpentry",
                BuildingKind.Kitchen => "Kitchen", BuildingKind.ScienceLab => "ResearchDesk",
                BuildingKind.SpikeTrap => "SpikeTrap", BuildingKind.QueenChamber => "Stockpile",
                BuildingKind.Workshop => "Workshop", _ => null };
            if (release != null && material == ResourceType.Soil)
            {
                var ready = Resources.Load<GameObject>($"Release1112/Models/{release}/{release}");
                if (ready != null) return ready;
            }
'''+s[idx:];Path('work/first-defense-1112/draft/FacilityVisual.cs').write_text(s,encoding='utf-8')
print('Natural preparation/combat/recovery probe and facility draft prepared')
