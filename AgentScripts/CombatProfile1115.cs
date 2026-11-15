using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditorInternal;
using AntColony.UI;
public static class CombatProfile1115
{
 public static async Task<string> Main(){
  if(Time.timeScale!=0)throw new Exception("Paused checkpoint required");
  bool enabled=ProfilerDriver.enabled, editor=ProfilerDriver.profileEditor;int first=ProfilerDriver.lastFrameIndex+1;
  try{ProfilerDriver.profileEditor=false;ProfilerDriver.enabled=true;SimulationPause.UserSpeed(1);await Task.Delay(10000);}
  finally{SimulationPause.UserSpeed(0);ProfilerDriver.enabled=enabled;ProfilerDriver.profileEditor=editor;}
  var rows=new Dictionary<string,(double total,double max,int count)>();var frames=new List<float>();
  int last=ProfilerDriver.lastFrameIndex;
  for(int frame=Math.Max(first,last-120);frame<=last;frame++){
   using(var f=ProfilerDriver.GetRawFrameDataView(frame,0)){
    if(!f.valid)continue;frames.Add(f.frameTimeMs);
    for(int i=0;i<f.sampleCount;i++){
     var name=f.GetSampleName(i);double ms=f.GetSampleTimeMs(i);if(ms<1)continue;
     var prior=rows.TryGetValue(name,out var v)?v:(0d,0d,0);
     rows[name]=(prior.Item1+ms,Math.Max(prior.Item2,ms),prior.Item3+1);
    }
   }
  }
  var report="10 seconds runtime observation; restore natural checkpoint after this probe. Inclusive samples, not additive.\nframes="+frames.Count+" mean="+(frames.Count>0?frames.Average():0)+" max="+(frames.Count>0?frames.Max():0)+"\n"+
   string.Join("\n",rows.OrderByDescending(p=>p.Value.total).Take(45).Select(p=>$"{p.Key}: total={p.Value.total:0.0}ms max={p.Value.max:0.0}ms count={p.Value.count}"));
  File.WriteAllText("work/colony-polish-1115/combat-profile.txt",report);return report;
 }
}
