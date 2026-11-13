using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Units;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.World;
public static class Motion1113StartCheck
{
 public static async Task<string> Run()
 {
  AntColony.UI.SimulationPause.UserSpeed(0);
  int wrecks=UnityEngine.Object.FindObjectsByType<CrashWreck>(FindObjectsInactive.Exclude).Length;
  if(wrecks!=0)throw new Exception("Starting wreck remains: "+wrecks);
  var corpse=Corpse.Spawn(new Corpse.State{kind=CorpseKind.Ant,name="Paused ant corpse",position=new Vector3(0,0,0),count=1});
  await Task.Delay(300);
  var visible=corpse.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r=>r.enabled&&!r.forceRenderingOff).Select(r=>r.name).ToArray();
  string result="new game wrecks="+wrecks+" commanders="+CommanderRoster.Instance.Commanders.Count+"\nAnt corpse visible skins="+string.Join(",",visible)+" children="+string.Join(",",corpse.GetComponentsInChildren<Transform>().Where(t=>t.parent==corpse.transform).Select(t=>t.name));
  UnityEngine.Object.Destroy(corpse.gameObject);
  System.IO.File.WriteAllText("work/motion-actions-1113/start-check.txt",result);return result;
 }
}
