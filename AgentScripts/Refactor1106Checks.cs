using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using R = AntColony.Data.ResourceType;

// Edit-mode fixtures in a temporary additive scene; no player saves or scene saves.
public static class Refactor1106Checks
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }
    static object Call(Type type, string name, object target, params object[] args) => type.GetMethod(name, Hidden).Invoke(target, args);
    static GameObject Make(string name, Vector3 position)
    {
        var go = new GameObject(name); go.transform.position = position; return go;
    }

    public static string Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run stopped, in Edit mode.");
        count = 0;
        Food(); Materials();
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try { Targets(); Attribution(); ProcessorCancellation(); }
        finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
        return "Refactor1106: PASS " + count;
    }

    static void Food()
    {
        var random = new System.Random(1106);
        for (int sample = 0; sample < 200; sample++)
        {
            var food = new FoodLots();
            for (int i = 0; i < sample % 35; i++)
                food.lots.Add(new FoodLots.Lot { amount = random.Next(1, 20), shelf = random.Next(1, 8), cookSkillPlusOne = random.Next(0, 22) });
            var total = food.lots.Sum(l => l.amount);
            var oldest = food.lots.Count == 0 ? 0 : food.lots.Min(l => l.shelf);
            Check(food.StalestAmount == food.lots.Where(l => l.shelf == oldest).Sum(l => l.amount), "oldest quantity, ties/empty");
            int skill = total == 0 ? 5 : (int)Math.Round(food.lots.Sum(l => (double)l.amount * (l.cookSkillPlusOne == 0 ? 5 : l.cookSkillPlusOne - 1)) / total);
            Check(food.CookingSkill == skill, "weighted cooking skill/legacy default");
            var taken = food.Take(3);
            Check(food.Total + taken.Total == total, "taking preserves quantity");
        }
    }

    static void Materials()
    {
        void Valid(List<ProductionInputs.Stock> stocks, bool production, bool kitchen, string label)
        {
            bool accepted = true;
            try { Call(typeof(SavePreflight), "ValidateStaged", null, new object[] { stocks }); }
            catch (TargetInvocationException e) when (e.InnerException is FormatException) { accepted = false; }
            Check(accepted == production, "production " + label);
            Check(Kitchen.Valid(new Kitchen.State { ingredients = stocks }) == kitchen, "kitchen " + label);
        }
        Valid(null, true, true, "legacy null");
        Valid(new List<ProductionInputs.Stock>(), true, true, "empty");
        foreach (int amount in new[] { -1, 0, 1, 100000000, 100000001 })
            Valid(new List<ProductionInputs.Stock> { new ProductionInputs.Stock { type = R.Wood, amount = amount } },
                amount >= 0 && amount <= 100000000, amount > 0 && amount <= 100000000, "amount boundary " + amount);
        Valid(new List<ProductionInputs.Stock> { null }, false, false, "null stock");
        Valid(new List<ProductionInputs.Stock> { new ProductionInputs.Stock { type = (R)int.MaxValue, amount = 1 } }, false, false, "unknown resource");
        var item = new ProductionInputs.Stock { type = R.Food, amount = 1 };
        Valid(new List<ProductionInputs.Stock> { item, item }, false, false, "duplicate resource");
        item.lots = new FoodLots(); item.lots.Add(2, 10);
        Valid(new List<ProductionInputs.Stock> { item }, false, false, "lots exceed quantity");
        item.amount = 2; Valid(new List<ProductionInputs.Stock> { item }, true, true, "fresh lots");
        item.lots.lots[0] = new FoodLots.Lot { amount = 2, shelf = float.NaN };
        Valid(new List<ProductionInputs.Stock> { item }, false, false, "invalid freshness");
    }

    static void Targets()
    {
        var origin = new Vector3(20000, 20000, 20000);
        var boss = Make("refactor-boss", origin);
        var basic = boss.AddComponent<BossBasicPatternLoop>();
        var sequence = boss.AddComponent<BossPatternSequenceSimple>();
        foreach (var pattern in new MonoBehaviour[] { basic, sequence })
        {
            pattern.GetType().GetField("antLayerMask", Hidden).SetValue(pattern, (LayerMask)(1 << 30));
            pattern.GetType().GetField("searchRadius", Hidden).SetValue(pattern, 10f);
        }
        BossHealth Target(string name, Vector3 offset)
        {
            var go = Make(name, origin + offset); go.layer = 30;
            go.AddComponent<BoxCollider>();
            var health = go.AddComponent<BossHealth>();
            typeof(BossHealth).GetField("currentHp", Hidden).SetValue(health, 100f);
            return health;
        }
        void Expect(Transform expected, string label)
        {
            Physics.SyncTransforms();
            foreach (var pattern in new MonoBehaviour[] { basic, sequence })
                Check((Transform)Call(pattern.GetType(), "FindNearestAntTarget", pattern) == expected, label);
        }
        Expect(null, "no targets");
        var far = Target("far", Vector3.right * 5);
        var near = Target("near-horizontal", new Vector3(2, 6, 0));
        Expect(near.transform, "horizontal distance wins within 3D sphere");
        near.enabled = false; Expect(far.transform, "disabled target excluded");
        near.enabled = true;
        typeof(BossHealth).GetField("currentHp", Hidden).SetValue(near, 0f);
        Expect(far.transform, "dead target excluded");
        far.GetComponent<BoxCollider>().isTrigger = true; Expect(null, "trigger excluded");
        far.GetComponent<BoxCollider>().isTrigger = false;
        far.transform.position = origin + Vector3.right * 20; Expect(null, "outside radius");
    }

    static void Attribution()
    {
        var type = typeof(SaveSnapshot).Assembly.GetType("AntColony.Save.SaveAttribution");
        var site = Make("refactor-site", Vector3.zero).AddComponent<BuildingConstructionSite>();
        typeof(BuildingConstructionSite).GetProperty("Order").SetValue(site, long.MaxValue - 1106);
        SceneObjects.Register(site);
        try
        {
            var dto = new ConstructionAttributionDto { order = site.Order, contributions = new WorkContribution(), artist = new SoleWorker(), babelUsed = true };
            dto.contributions.Add("worker", 3, true);
            var list = new List<ConstructionAttributionDto> { dto, new ConstructionAttributionDto { order = -1106 } };
            Call(type, "Restore", null, new object[] { list });
            var contributions = (WorkContribution)typeof(BuildingConstructionSite).GetField("Contributions", Hidden).GetValue(site);
            Check(contributions.total == 3 && !ReferenceEquals(contributions, dto.contributions), "restored contributions deep copy, missing site skipped");
            dto.contributions.total = 9;
            Check(contributions.total == 3, "snapshot mutations cannot affect restored site");
            Call(type, "Restore", null, new object[] { null });
            Check(contributions.total == 3, "legacy null attribution");
        }
        finally { SceneObjects.Unregister(site); }
    }

    static void ProcessorCancellation()
    {
        var go = Make("refactor-processor", Vector3.zero); go.SetActive(false);
        var processor = go.AddComponent<Processor>();
        var state = new Processor.State { current = -1, progress = 4, makerId = "worker", gasPaid = true,
            fermenting = true, fermented = true, fermentation = 10, pending = 0, paused = true, ruined = true };
        state.orders.Add(new Processor.Order { recipe = 0, amount = 5 });
        typeof(Processor).GetField("state", Hidden).SetValue(processor, state);
        processor.CancelCurrent();
        Check(state.current == -1 && state.progress == 0 && state.makerId == "" && !state.gasPaid, "cancel clears active batch");
        Check(!state.fermenting && !state.fermented && state.fermentation == 0 && state.pending == -1, "cancel clears fermentation and pending");
        Check(state.paused && state.ruined && state.orders.Single().amount == 5, "cancel preserves facility state/orders");
        processor.CancelCurrent();
        Check(state.orders.Single().amount == 5, "repeat cancellation is safe");
    }

    public static string MeshFingerprints()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run stopped, in Edit mode.");
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        var results = new List<string>();
        var meshes = new List<Mesh>();
        try
        {
            var center = new Vector3(20000, 20000, 20000);
            var ground = Make("refactor-ground", center - Vector3.up * 2);
            ground.layer = 30; ground.transform.rotation = Quaternion.Euler(8, 0, 0);
            var box = ground.AddComponent<BoxCollider>(); box.size = new Vector3(40, 1, 40);
            Physics.SyncTransforms();
            foreach (var type in new[] { typeof(AntColony.Boss.Telegraph.GroundTelegraphCircle), typeof(AntColony.Boss.Telegraph.GroundTelegraphSector), typeof(AntColony.Boss.Telegraph.GroundTelegraphLine) })
            {
                var go = Make(type.Name, center); go.transform.rotation = Quaternion.Euler(0, 37, 0);
                var shape = go.AddComponent(type);
                // Edit mode does not call Awake: seed the same required component references.
                type.GetField("meshFilter", Hidden).SetValue(shape, go.GetComponent<MeshFilter>());
                foreach (int mask in new[] { 0, 1 << 30 })
                {
                    type.GetField("groundMask").SetValue(shape, (LayerMask)mask);
                    type.GetMethod("Build").Invoke(shape, null);
                    var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                    if (!meshes.Contains(mesh)) meshes.Add(mesh);
                    using (var stream = new System.IO.MemoryStream())
                    using (var writer = new System.IO.BinaryWriter(stream))
                    {
                        foreach (var p in mesh.vertices) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
                        foreach (var p in mesh.uv) { writer.Write(p.x); writer.Write(p.y); }
                        foreach (var i in mesh.triangles) writer.Write(i);
                        foreach (var p in mesh.normals) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
                        using (var hash = System.Security.Cryptography.SHA256.Create())
                            results.Add(type.Name + "/" + mask + ":" + BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", ""));
                    }
                }
            }
        }
        finally
        {
            foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh);
            SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true);
        }
        return string.Join("\n", results);
    }
}
