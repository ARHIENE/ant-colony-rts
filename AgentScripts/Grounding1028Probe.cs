using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Grounding1028Probe
{
    public static async Task<string> Main()
    {
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Grounding1028-" + Guid.NewGuid().ToString("N"));
        var output = new StringBuilder();
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            while (SaveSystem.Busy) await Task.Delay(60);
            var team = Enumerable.Range(0, 3).Select(i => { var c = StartingCandidate.Roll(i); c.traits.values.Clear(); return c; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, startingCount = 3, startingTeam = team });
            while (SaveSystem.Busy) await Task.Delay(60);
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            for (var step = 0; step < 4; step++)
            {
                await Task.Delay(600);
                foreach (var c in CommanderRoster.Instance.Commanders)
                {
                    var min = float.PositiveInfinity; var max = float.NegativeInfinity;
                    foreach (var skin in c.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled))
                    {
                        var mesh = new Mesh(); skin.BakeMesh(mesh, true);
                        foreach (var v in mesh.vertices) { var y = skin.transform.TransformPoint(v).y; min = Mathf.Min(min, y); max = Mathf.Max(max, y); }
                        Object.Destroy(mesh);
                    }
                    var ground = Physics.Raycast(c.Position + Vector3.up * 20, Vector3.down, out var hit, 40, 1 << 8, QueryTriggerInteraction.Ignore) ? hit.point.y : -999;
                    var upright = c.transform.Find("Kemono/Upright"); var a = RigAnimator.Find(c.gameObject);
                    output.AppendLine($"step={step} {c.name} pos={c.Position} agentOffset={c.Agent.baseOffset} ground={ground:F3} meshBottom={min:F3} meshTop={max:F3} footDelta={min-ground:F3} upright={upright.localPosition} scale={upright.localScale} animator={a.enabled}");
                }
            }
            File.WriteAllText("Temp/grounding1028-probe.txt", output.ToString()); return output.ToString();
        }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0; }
    }
}
