using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
using AntColony.Buildings;
public static class Motion1113BedInspect
{
 public static async Task<string> Run()
 {
  var f=BindingFlags.Instance|BindingFlags.NonPublic;
  var lines=CommanderRoster.Instance.Commanders.Where(c=>c&&c.IsColonyMember).Select(c=>c.name+" pos="+c.Position+" asleep="+c.IsAsleep+" fatigue="+c.PersonalState.sleep.fatigue+" dorm="+Dormitory.Of(c)+" arrived="+typeof(CommanderAnt).GetField("bedArrived",f).GetValue(c)+" approach="+typeof(CommanderAnt).GetField("bedApproach",f).GetValue(c)+" bed="+typeof(CommanderAnt).GetField("approachingBed",f).GetValue(c)+" agentTarget="+c.Agent.destination+" next="+c.Agent.nextPosition+" hasPath="+c.Agent.hasPath+" path="+c.Agent.pathStatus+" remaining="+c.Agent.remainingDistance+" stop="+c.Agent.stoppingDistance).ToArray();
  var beds=UnityEngine.Object.FindObjectsByType<Dormitory>(FindObjectsInactive.Exclude).Select(b=>b.name+" residents="+string.Join(",",b.Residents.Select(c=>c.name))+" anchor="+b.GetComponent<FacilityVisual>()?.Model?.Find("SleepingSurface")+" model="+b.GetComponent<FacilityVisual>()?.Model?.name);
  ScreenCapture.CaptureScreenshot("work/motion-actions-1113/screens/new-bed.png");await Task.Delay(300);
  var first=CommanderRoster.Instance.Commanders.First(c=>c&&c.IsColonyMember);
  var approach=(Vector3)typeof(CommanderAnt).GetField("bedApproach",f).GetValue(first);
  UnityEngine.AI.NavMesh.SamplePosition(approach,out var sampled,.75f,UnityEngine.AI.NavMesh.AllAreas);
  return string.Join("\n",lines.Concat(beds))+"\nresampled="+sampled.position;
 }
}

