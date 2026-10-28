using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.World;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FishingVisual1028Checks
{
    public static string Import()
    {
        if (Application.isPlaying) throw new Exception("Import in edit mode");
        AssetDatabase.Refresh();
        typeof(AntColony.EditorTools.KemonoImport).GetMethod("ImportProp", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { "Assets/Resources/ResourceModels/Fishing_Bank.fbx", true });
        var model = Resources.Load<GameObject>("ResourceModels/Fishing_Bank");
        if (model == null || !model.GetComponentsInChildren<Renderer>().Any()) throw new Exception("Missing fishing model");
        foreach (var r in model.GetComponentsInChildren<Renderer>())
            if (r.sharedMaterials.Any(m => m == null || m.shader.name != "Universal Render Pipeline/Lit")) throw new Exception("Invalid fishing material");
        AssetDatabase.SaveAssets();
        return "PASS fishing bank FBX/URP import";
    }

    static int checks;
    static void Check(bool ok, string label)
    {
        if (!ok) throw new Exception("Fishing visual FAIL " + checks + " " + label);
        checks++;
    }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80);
        Check(!SaveSystem.Busy, "scene ready");
        await Task.Delay(200);
    }
    static ResourceNode Spot() => Object.FindObjectsByType<ResourceNode>().First(n => n.RequiresFishing);
    static void Visual(ResourceNode n)
    {
        var model = n.transform.Find("ResourceModel");
        Check(model != null, "bank model attached");
        Check(n.GetComponent<Renderer>().forceRenderingOff, "cube hidden");
        Check(model.GetComponentsInChildren<Renderer>().All(r => r.enabled && !r.forceRenderingOff), "bank visible");
        Check(model.GetComponentsInChildren<Collider>().Length == 0, "display adds no navigation obstacle");
        var box = n.GetComponent<BoxCollider>();
        Check(box != null && box.isTrigger, "owner selection collider");
        Physics.SyncTransforms();
        Check(Mathf.Abs(Mathf.Max(box.bounds.size.x, box.bounds.size.z) - 2.2f) < .02f && box.bounds.size.y < 2, "human scale bank");
        Check(box.Raycast(new Ray(box.bounds.center + Vector3.up * 5, Vector3.down), out var hit, 10), "bank click ray");
        ResourceVisual.Attach(n);
        Check(n.transform.Cast<Transform>().Count(t => t.name == "ResourceModel") == 1, "repeat attach is idempotent");
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        checks = 0;
        var root = SaveStorage.RootOverride;
        var speed = Time.timeScale;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "FishingVisual1028-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready();
            var c = StartingCandidate.Roll(0); c.traits.values.Clear();
            SaveSystem.NewGame(new NewGameOptions { seed = 261024, startingCount = 1, startingTeam = new[] { c } });
            await Ready();
            Time.timeScale = 0;
            var spot = Spot();
            Visual(spot);
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, false);
            var stock = spot.AmountRemaining;
            Check(!spot.CanGather && spot.Extract(1) == 0 && spot.AmountRemaining == stock, "model keeps research lock");
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(GameManager.Instance, true);
            spot.RefreshFishingSeason();
            var catchStart = spot.AmountRemaining;
            Check(spot.Extract(1) == 1 && spot.AmountRemaining == catchStart - 1, "model keeps catch stock");
            spot.Extract(10000);
            Check(spot.FishedOut && !spot.CanGather, "depleted bank persists");
            Check(SaveSystem.TrySave(false, 0, out var error), "save " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "load " + error);
            await Ready(); Time.timeScale = 0;
            spot = Spot(); Visual(spot);
            Check(spot.FishedOut && !spot.CanGather, "load does not refill exhausted bank");
            typeof(ResourceNode).GetProperty("FishSeason", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(spot, GameCalendar.TotalSeasons - 1);
            spot.RefreshFishingSeason();
            Check(spot.AmountRemaining == GameBalance.FishingSeasonFood, "season refresh unchanged");
            var result = "PASS " + checks + " fishing model/click/stock/research/season/real save-load checks";
            File.WriteAllText("Temp/fishing-visual1028-result.txt", result);
            return result;
        }
        catch (Exception e) { File.WriteAllText("Temp/fishing-visual1028-result.txt", e.ToString()); throw; }
        finally { SaveStorage.RootOverride = root; Time.timeScale = speed; }
    }
}
