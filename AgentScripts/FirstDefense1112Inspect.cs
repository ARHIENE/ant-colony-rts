using System;
using System.IO;
using System.Linq;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
public static class FirstDefense1112Inspect {
 public static string Run(){
 var nodes=ResourceNode.Available.Where(n=>n!=null&&n.IsLooseCargo&&!n.IsDepleted).ToArray();
 var s="Loose cargo count="+nodes.Length+" subunit="+nodes.Count(n=>n.AmountRemaining<.999f)+"\n";
 s+=string.Join("\n",nodes.Select(n=>$"{n.name}: {n.AmountRemaining:R} at {n.transform.position}"));
 s+="\n"+string.Join("\n",CommanderRoster.Instance.Commanders.Select(c=>$"{c.name} carried={c.CarriedAmount:R} node={c.CurrentResourceNode?.AmountRemaining:R}"));
 File.WriteAllText("Temp/first-defense-1112/cargo-diagnosis.txt",s);return s;
 }
}
