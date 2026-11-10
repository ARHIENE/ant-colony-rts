using System.Linq;
using UnityEngine;
using AntColony.Units; using AntColony.Core;
public static class Animation1110Inspect {
 public static string Main(){var c=Object.FindObjectsByType<CommanderAnt>(FindObjectsInactive.Exclude).Where(x=>x.IsColonyMember).OrderBy(x=>Vector3.Distance(x.Position,Camera.main.transform.position)).First();return string.Join("\n",Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude).Where(r=>r.enabled&&Vector3.Distance(r.bounds.center,c.Position)<2).Select(r=>r.name+" parent="+r.transform.parent?.name+" bounds="+r.bounds+" material="+r.sharedMaterial?.name));}
}



