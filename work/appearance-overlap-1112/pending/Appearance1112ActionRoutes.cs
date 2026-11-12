using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using AntColony.Core;
using AntColony.Units;
public static class Appearance1112ActionRoutes
{
    public static async Task<string> Run()
    {
        var c = CommanderRoster.Instance.Commanders.First(x => x && x.IsColonyMember);
        var p = c.GetComponent<MeshyNativeMotionPlayer>(); var v = c.GetComponent<AntVisual>();
        var lib = Resources.Load<MeshyNativeLibrary>("MeshyNative/Runtime");
        var context = new[] { "WalkLimp", "WalkLimpSevere", "LeisureWalk", "IdleTalk", "IdleRest", "IdleLook" };
        var duty = c.WorkState.duty; bool hold = c.WorkState.holdDeployment;
        var report = new System.Collections.Generic.List<string>();
        try
        {
            c.CommandStop(); c.WorkState.duty = CommanderDuty.Deployed; c.WorkState.holdDeployment = true;
            p.PreviewMotion = null; p.PreviewPhase = -1;
            AntColony.UI.SimulationPause.UserSpeed(1);
            foreach (var action in lib.actions.Where(a => !context.Contains(a.action)))
            {
                var expected = ((ValueTuple<string,string>)typeof(AntVisual).GetMethod("Clip", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(v, new object[] { action.action })).Item1;
                v.Action(action.action, 60); await Task.Delay(100);
                if (v.MotionName != expected) throw new Exception(action.action + " rejected: expected " + expected + ", remains " + v.MotionName);
                var binding = lib.actions.FirstOrDefault(a => a.action == expected);
                if (binding == null || p.PlayingMotion != binding.motion) throw new Exception(action.action + " native route mismatch: " + p.PlayingMotion);
                report.Add(action.action + " -> " + expected + " -> " + p.PlayingMotion);
            }
            File.WriteAllLines("work/appearance-overlap-1112/direct-action-routes.txt", report);
            return report.Count + " direct visual routes passed; six contextual routes require separate gameplay checks";
        }
        finally
        {
            AntColony.UI.SimulationPause.UserSpeed(0); c.WorkState.duty = duty; c.WorkState.holdDeployment = hold;
            v.Action("Idle_A", 0);
        }
    }
}
