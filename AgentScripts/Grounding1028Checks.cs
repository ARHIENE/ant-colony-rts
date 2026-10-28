using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Grounding1028Checks
{
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("Grounding FAIL " + count + " " + message); count++; }
    static void Pose(CommanderAnt c, bool idle)
    {
        var min = float.PositiveInfinity; var max = float.NegativeInfinity;
        foreach (var skin in c.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.enabled))
        {
            var mesh = new Mesh(); skin.BakeMesh(mesh, true);
            foreach (var v in mesh.vertices) { var y = skin.transform.TransformPoint(v).y; min = Mathf.Min(min, y); max = Mathf.Max(max, y); }
            Object.Destroy(mesh);
        }
        Check(Physics.Raycast(c.Position + Vector3.up * 4, Vector3.down, out var ground, 12, 1 << 8, QueryTriggerInteraction.Ignore), "ground found");
        Check(min - ground.point.y > -.12f && min - ground.point.y < .18f, "feet at ground " + (min - ground.point.y));
        Check(max - min > (idle ? 1.3f : 1.05f) && max - min < 1.85f, "actual skinned height " + (max - min));
        var bones = c.GetComponentsInChildren<Transform>();
        var head = bones.First(t => t.name == "head"); var pelvis = bones.First(t => t.name == "pevis");
        Check((head.position - pelvis.position).magnitude > .15f && Vector3.Dot((head.position - pelvis.position).normalized, Vector3.up) > .7f, "skeleton not collapsed");
        var collider = c.GetComponent<CapsuleCollider>();
        Check(collider.bounds.max.y >= max - .2f && collider.bounds.min.y <= min + .1f, "selection collider follows body");
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(60);
        Check(!SaveSystem.Busy, "scene ready"); await Task.Delay(250);
    }
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Grounding1028Checks-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready();
            var team = Enumerable.Range(0, 3).Select(i => { var c = StartingCandidate.Roll(i); c.traits.values.Clear(); return c; }).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 281028, startingCount = 3, startingTeam = team }); await Ready();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            await Task.Delay(300);
            foreach (var c in CommanderRoster.Instance.Commanders) Pose(c, true);
            var walker = CommanderRoster.Instance.Commanders[0]; var start = walker.Position;
            walker.CommandMove(start + Vector3.right * 8);
            for (var i = 0; i < 6; i++) { await Task.Delay(250); Pose(walker, false); }
            Check((walker.Position - start).sqrMagnitude > .25f, "actually walked");
            walker.CommandStop(); await Task.Delay(300); Pose(walker, true);
            Check(SaveSystem.TrySave(false, 0, out var error), "save " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error);
            await Ready(); Time.timeScale = 0; await Task.Delay(250);
            foreach (var c in CommanderRoster.Instance.Commanders) Pose(c, true);
            var result = "PASS " + count + " actual skinned size, feet/terrain/collider, walking and paused save-load grounding checks";
            File.WriteAllText("Temp/grounding1028-result.txt", result); return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/grounding1028-result.txt", e.ToString()); throw; }
        finally { SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = 0; }
    }
}
