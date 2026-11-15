using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using UnityEngine;
public static class SoilOutput1115Watch
{
 public static string Main(){_=Watch();return "Watching actual quarry output without advancing or modifying gameplay";}
 static async Task Watch(){
  const string log="work/colony-polish-1115/soil-output.txt";
  var quarry=SceneObjects.All<Processor>().First(p=>p.isActiveAndEnabled&&!p.IsDead&&p.Data.kind==BuildingKind.Quarry);
  var known=ResourceNode.Available.Select(n=>n.GetEntityId()).ToHashSet();
  float old=quarry.Progress;var deadline=DateTime.UtcNow.AddSeconds(70);
  File.WriteAllText(log,$"RUNNING natural quarry at {GameCalendar.GameSeconds}, progress={old}, stock={ResourceManager.Instance.GetAmount(ResourceType.Soil)}\n");
  while(DateTime.UtcNow<deadline&&Application.isPlaying&&quarry&&!quarry.IsDead){
   var output=ResourceNode.Available.FirstOrDefault(n=>!known.Contains(n.GetEntityId())&&n.IsLooseCargo&&n.ResourceType==ResourceType.Soil&&(n.transform.position-quarry.Position).sqrMagnitude<25);
   if(output){File.AppendAllText(log,$"PASS actual new soil output amount={output.AmountRemaining} pos={output.transform.position} gameSeconds={GameCalendar.GameSeconds} stock={ResourceManager.Instance.GetAmount(ResourceType.Soil)}\n");return;}
   await Task.Delay(100);
  }
  File.AppendAllText(log,"UNVERIFIED no new output observed during bounded watch\n");
 }
}

