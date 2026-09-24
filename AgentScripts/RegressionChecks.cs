// Run in Play mode: unity command run_script --file AgentScripts/RegressionChecks.cs --timeout_ms 60000
namespace AntColony.Regression
{
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Boss;
using AntColony.Boss.AoE;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

using ResourceType = AntColony.Data.ResourceType;

public static class RegressionChecks
{
    static readonly List<Object> created = new List<Object>();
    static readonly List<string> passed = new List<string>();
    static Vector3 origin = new Vector3(1000, 0, 1000);

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play mode.");
        var rm = ResourceManager.Instance;
        var food = rm.GetAmount(ResourceType.Food);
        var soil = rm.GetAmount(ResourceType.Soil);
        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var upkeepEnabled = upkeep != null && upkeep.enabled;
        if (upkeep != null) upkeep.enabled = false;
        // 메인 메뉴가 시뮬레이션을 멈춘 상태로 시작하므로 검사 동안만 시간을 흐르게 한다.
        var timeScale = Time.timeScale;
        Time.timeScale = 1;
        NavMeshDataInstance nav = default;
        try
        {
            var ground = NewObject("RegressionGround");
            ground.layer = 8;
            var box = ground.AddComponent<BoxCollider>();
            box.size = new Vector3(80, .2f, 80);
            box.center = new Vector3(0, -.1f, 0);
            var sources = new List<NavMeshBuildSource> { new NavMeshBuildSource {
                shape = NavMeshBuildSourceShape.Box, size = box.size,
                transform = Matrix4x4.TRS(origin + box.center, Quaternion.identity, Vector3.one)
            }};
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0), sources,
                new Bounds(origin, new Vector3(80, 10, 80)), Vector3.zero, Quaternion.identity);
            created.Add(data);
            nav = NavMesh.AddNavMeshData(data);
            Physics.SyncTransforms();
            await Task.Delay(50);

            var worker = Unit<CommanderAnt>(UnitRole.Worker, origin);
            AntPool.Instance.Breed(1);
            Check(worker.TryAssign(1), "commander receives workforce for gameplay checks");
            var soldier = Unit<SoldierAnt>(UnitRole.Melee, origin + Vector3.right * 8);
            var flying = Unit<FlyingAnt>(UnitRole.Flying, origin + Vector3.left * 8);
            Check(flying.transform.position.y >= origin.y + 2.9f, "flying spawn starts above ground before combat");
            var enemy = NewObject("RegressionEnemy", origin + Vector3.right * 25).AddComponent<WildMonster>();
            Field(enemy, "detectionRadius").SetValue(enemy, 0f);
            Check(!soldier.CanAttackTarget(worker) && !soldier.CanAttackTarget(soldier), "friendly/self attack rejected");
            Check(soldier.CanAttackTarget(enemy), "enemy attack allowed");

            var ownBuilding = NewObject("RegressionBuilding").AddComponent<BuildingBase>();
            Check(!soldier.CanAttackTarget(ownBuilding), "friendly building attack rejected");
            var node = NewObject("RegressionFood").AddComponent<ResourceNode>();
            var other = NewObject("RegressionSoil").AddComponent<ResourceNode>();
            Field(other, "resourceType").SetValue(other, ResourceType.Soil);
            Field(worker, "carriedAmount").SetValue(worker, 5f);
            Field(worker, "carriedType").SetValue(worker, ResourceType.Food);
            worker.CommandGather(other);
            Check(Field(worker, "targetNode").GetValue(worker) == null
                && (ResourceType)Field(worker, "carriedType").GetValue(worker) == ResourceType.Food,
                "changing resource never converts carried food to soil");
            worker.Initialize(worker.Data, null, null);
            Field(worker, "targetNode").SetValue(worker, node);
            Field(worker, "carriedAmount").SetValue(worker, 9.99f);
            var before = (float)Field(node, "amountRemaining").GetValue(node);
            Invoke(worker, "TickGathering");
            Check(before - (float)Field(node, "amountRemaining").GetValue(node) <= .011f,
                "harvest cannot discard overflow beyond carry capacity");
            worker.Initialize(worker.Data, null, null);
            Check((float)Field(worker, "carriedAmount").GetValue(worker) == 0f
                && Field(worker, "state").GetValue(worker).ToString() == "Idle", "worker reinitialization resets task and cargo");

            soldier.CommandAttack(enemy);
            soldier.Initialize(soldier.Data, null, null);
            Check(Field(soldier, "currentTarget").GetValue(soldier) == null
                && Field(soldier, "state").GetValue(soldier).ToString() == "Idle", "soldier reinitialization clears old target");

            var pool = NewObject("RegressionPool").AddComponent<ObjectPool>();
            var template = Unit<WorkerAnt>(UnitRole.Worker, origin + Vector3.forward * 15);
            template.gameObject.SetActive(false);
            var pooled = pool.Get(template.gameObject, origin + Vector3.forward * 10, Quaternion.identity);
            created.Add(pooled);
            var reused = pooled.GetComponent<WorkerAnt>();
            reused.Initialize(template.Data, pool, template.gameObject);
            Check(pooled.activeInHierarchy, "first pooled spawn activates inactive template");
            Field(reused, "carriedAmount").SetValue(reused, 4f);
            reused.TakeDamage(float.MaxValue);
            var next = pool.Get(template.gameObject, origin + Vector3.forward * 10, Quaternion.identity);
            next.GetComponent<WorkerAnt>().Initialize(template.Data, pool, template.gameObject);
            Check(next == pooled && (float)Field(reused, "carriedAmount").GetValue(reused) == 0f,
                "death and pooled production reuse without old cargo");

            var depotGo = NewObject("RegressionStorage", origin + Vector3.forward * 3);
            depotGo.SetActive(false);
            var storage = depotGo.AddComponent<Storage>();
            var buildingData = ScriptableObject.CreateInstance<BuildingData>();
            created.Add(buildingData);
            buildingData.foodCapacityBonus = 50;
            buildingData.soilCapacityBonus = 50;
            Field(storage, "data").SetValue(storage, buildingData);
            var capacity = rm.GetCapacity(ResourceType.Food);
            depotGo.SetActive(true);
            await Task.Delay(50);
            Check(rm.GetCapacity(ResourceType.Food) == capacity + 50, "storage capacity added exactly once");
            depotGo.SetActive(false);
            Check(rm.GetCapacity(ResourceType.Food) == capacity, "disabled storage removes capacity");
            depotGo.SetActive(true);
            Check(rm.GetCapacity(ResourceType.Food) == capacity + 50, "reenabled storage restores capacity once");
            var foodBeforeOverflow = rm.GetAmount(ResourceType.Food);
            rm.Add(ResourceType.Food, capacity + 50);
            var fullFood = rm.GetAmount(ResourceType.Food);
            depotGo.SetActive(false);
            rm.Add(ResourceType.Food, 1);
            Check(rm.GetAmount(ResourceType.Food) == fullFood, "capacity reduction and further deposits preserve existing stock");
            depotGo.SetActive(true);
            rm.TrySpend(fullFood - foodBeforeOverflow, 0);
            Field(worker, "carriedAmount").SetValue(worker, 5f);
            Field(worker, "targetDeposit").SetValue(worker, storage);
            depotGo.SetActive(false);
            Invoke(worker, "TickReturning");
            Check((BuildingBase)Field(worker, "targetDeposit").GetValue(worker) != storage,
                "worker redirects when its deposit is disabled");
            depotGo.SetActive(true);
            worker.Initialize(worker.Data, null, null);

            node.transform.position = origin + Vector3.forward;
            worker.CommandGather(node);
            var expectedFood = rm.GetAmount(ResourceType.Food) + worker.Data.carryCapacity;
            await Until(() => rm.GetAmount(ResourceType.Food) >= expectedFood, "real worker gather, return and deposit", 10000);
            Check(!worker.CanReach(origin + Vector3.right * 100), "off-navmesh construction rejected");

            var blockedBuilding = NewObject("RegressionBlockedBuilding");
            blockedBuilding.SetActive(false);
            var blockedSite = NewObject("RegressionBlockedSite", origin + Vector3.right * 100).AddComponent<BuildingConstructionSite>();
            blockedSite.Initialize(blockedBuilding, 1f);
            worker.CommandBuild(blockedSite);
            await Task.Delay(200);
            Check(blockedSite != null && blockedBuilding != null && !blockedBuilding.activeSelf,
                "blocked construction preserves prepaid site for retry");
            blockedSite.Cancel();
            await Until(() => worker.CanStartConstruction, "canceled site releases worker", 2000);

            var placement = NewObject("RegressionPlacement").AddComponent<BuildingPlacementController>();
            placement.enabled = false;
            Field(placement, "pendingKind").SetValue(placement, BuildingKind.Farm);
            Field(placement, "builder").SetValue(placement, worker);
            Field(placement, "placementValid").SetValue(placement, true);
            var buildPoint = origin + Vector3.right * 5;
            var soilBeforeBuild = rm.GetAmount(ResourceType.Soil);
            Invoke(placement, "TryPlace", buildPoint + Vector3.up * .2f, buildPoint);
            BuildingConstructionSite placedSite = null;
            foreach (var candidate in Object.FindObjectsByType<BuildingConstructionSite>())
                if (Vector3.Distance(candidate.Position, buildPoint) < 1f) placedSite = candidate;
            Check(placedSite != null && rm.GetAmount(ResourceType.Soil) == soilBeforeBuild - 30,
                "actual placement creates farm site and charges once");
            created.Add(placedSite.gameObject);
            var placedFarm = (GameObject)Field(placedSite, "completedBuilding").GetValue(placedSite);
            created.Add(placedFarm);
            Physics.SyncTransforms();
            Check((bool)Invoke(placement, "HasObstruction", buildPoint + Vector3.up * .2f),
                "construction footprint prevents overlapping placements");
            await Until(() => placedFarm.activeInHierarchy, "actual scene farm template completes through worker", 8000);

            var farmGo = NewObject("RegressionFarm", origin + Vector3.forward * 2);
            farmGo.SetActive(false);
            farmGo.AddComponent<BuildingBase>();
            var farm = farmGo.AddComponent<ResourceNode>();
            Field(farm, "amountRemaining").SetValue(farm, 0f);
            Field(farm, "regrowSeconds").SetValue(farm, .15f);
            Field(farm, "regrowAmount").SetValue(farm, 20f);
            var site = NewObject("RegressionSite", farmGo.transform.position).AddComponent<BuildingConstructionSite>();
            site.Initialize(farmGo, .1f);
            worker.CommandBuild(site);
            await Until(() => farmGo.activeInHierarchy && !farm.IsDepleted, "walk, complete farm and first growth", 8000);
            var farmStatus = farm.GetComponent<ResourceNodeStatus>();
            Check(farmStatus != null && farmStatus.StatusText == "Ready · 20 Food", "grown farm displays harvest amount");
            Check(node.GetComponent<ResourceNodeStatus>() == null, "ordinary resource does not acquire regrowth display");
            expectedFood = rm.GetAmount(ResourceType.Food) + worker.Data.carryCapacity;
            worker.CommandGather(farm);
            await Until(() => rm.GetAmount(ResourceType.Food) >= expectedFood, "farm harvest and real deposit", 8000);
            farm.Extract(1000);
            Check(farmStatus.StatusText.StartsWith("Growing · "), "depleted farm displays growth countdown");
            var timer = (float)Field(farm, "regrowTimer").GetValue(farm);
            Check(farm.Extract(1) == 0 && (float)Field(farm, "regrowTimer").GetValue(farm) == timer,
                "empty farm extraction does not restart growth");
            await Until(() => !farm.IsDepleted, "farm regrows after depletion", 2000);

            enemy.transform.position = flying.transform.position + Vector3.right;
            enemy.GetComponent<NavMeshAgent>().enabled = false;
            var flyingPosition = flying.transform.position;
            flying.CommandAttack(enemy);
            await Task.Delay(250);
            Check(Vector3.Distance(flyingPosition, flying.transform.position) < .05f, "flying attack stops in range");
            Check(enemy.CurrentHealth < 150f, "flying unit deals damage while stopped");
            Object.Destroy(enemy.gameObject);
            await Task.Delay(50);
            Check(!CombatTargeting.IsAlive(enemy), "destroyed interface target is invalid");

            flying.Rebel();
            await Task.Delay(50);
            WildMonster airborneEnemy = null;
            foreach (var obj in created)
                if (obj is GameObject go && go != null && go.TryGetComponent<WildMonster>(out var candidate) && candidate.IsFlying)
                    airborneEnemy = candidate;
            Check(airborneEnemy != null && CombatTargeting.IsAirborne(airborneEnemy)
                && !airborneEnemy.GetComponent<NavMeshAgent>().enabled, "flying rebellion retains air movement");
            Check(!CombatTargeting.CanAttack(UnitRole.Melee, airborneEnemy)
                && CombatTargeting.CanAttack(UnitRole.Ranged, airborneEnemy), "air target restrictions after rebellion");
            Check(!airborneEnemy.GetComponent<SelectableObject>().enabled, "rebels leave player selection");

            var selection = Object.FindAnyObjectByType<SelectionManager>();
            var selectionBox = (UnityEngine.UI.Image)Field(selection, "selectionBoxImage").GetValue(selection);
            Check(!selectionBox.raycastTarget, "drag selection visual cannot intercept pointer events");
            Invoke(selection, "AddToSelection", template.GetComponent<SelectableObject>());
            Check(!template.GetComponent<SelectableObject>().IsSelected, "ordinary ants cannot be selected");
            var selectedCommander = Unit<CommanderAnt>(UnitRole.Worker, origin + Vector3.left * 2);
            Invoke(selection, "AddToSelection", selectedCommander.GetComponent<SelectableObject>());
            var attackInput = Object.FindAnyObjectByType<AttackMoveController>();
            Field(attackInput, "consumedFrame").SetValue(attackInput, Time.frameCount);
            Invoke(selection, "Update");
            Check(selectedCommander.GetComponent<SelectableObject>().IsSelected && attackInput.ConsumesPointerInput,
                "attack command frame preserves selection");
            selection.ClearSelection();

            var bossGo = NewObject("RegressionBoss", origin + Vector3.right * 20);
            var boss = bossGo.AddComponent<BossHealth>();
            var circle = bossGo.AddComponent<BossCircleAoE>();
            var cone = bossGo.AddComponent<BossConeAoE>();
            var line = bossGo.AddComponent<BossLineAoE>();
            circle.CastAt(bossGo.transform.position);
            cone.CastFrom(bossGo.transform.position, Vector3.forward);
            line.CastFrom(bossGo.transform.position, Vector3.forward);
            boss.TakeDamage(float.MaxValue);
            Check(!circle.IsCasting && !cone.IsCasting && !line.IsCasting, "boss death cancels all queued attacks");
            circle.CastAt(origin);
            Check(!circle.IsCasting, "disabled boss attack cannot restart");

            var mapGo = NewObject("RegressionMap", origin + Vector3.right * 100);
            var map = mapGo.AddComponent<MapGenerator>();
            map.enabled = false;
            Field(map, "xSize").SetValue(map, 2);
            Field(map, "zSize").SetValue(map, 2);
            var manual = NewObject("ManualChild");
            manual.transform.SetParent(mapGo.transform);
            var material = new Material(Shader.Find("AntColony/TerrainBlend"));
            created.Add(material);
            Field(map, "mat").SetValue(map, material);
            var layers = (IList)Field(map, "terrainLayers").GetValue(map);
            var smallTexture = new Texture2D(8, 4);
            created.Add(smallTexture);
            smallTexture.Apply(false, true);
            layers[0].GetType().GetField("texture").SetValue(layers[0], smallTexture);
            var decorations = (IList)Field(map, "spawnObjects").GetValue(map);
            decorations.Add(new MapGenerator.SpawnObject { prefab = NewObject("RegressionDecoration"), spawnChance = 1f });
            var rng = UnityEngine.Random.state;
            map.GenerateTerrain();
            var oldMesh = mapGo.GetComponent<MeshFilter>().sharedMesh;
            var oldTextures = Field(map, "terrainTextures").GetValue(map) as Object;
            map.GenerateTerrain();
            Check(UnityEngine.Random.state.Equals(rng), "map generation preserves global random state");
            await Task.Delay(50);
            Check(manual != null, "map regeneration preserves manual children");
            Check(oldMesh == null && oldTextures == null, "map regeneration releases old mesh and textures");
            layers.Clear();
            map.GenerateTerrain();
            Check(Field(map, "terrainTextures").GetValue(map) == null, "empty terrain texture list is safe");
            return string.Join("\n", passed);
        }
        catch (Exception error)
        {
            throw new Exception(string.Join("\n", passed) + "\n" + error);
        }
        finally
        {
            for (var i = created.Count - 1; i >= 0; i--)
                if (created[i] is GameObject go && go != null) Object.Destroy(go);
            await Task.Delay(50);
            nav.Remove();
            foreach (var asset in created)
                if (asset != null && asset is not GameObject) Object.Destroy(asset);
            if (upkeep != null) upkeep.enabled = upkeepEnabled;
            Time.timeScale = timeScale;
            if (rm != null)
            {
                rm.TrySpend(Math.Max(0, rm.GetAmount(ResourceType.Food) - food), Math.Max(0, rm.GetAmount(ResourceType.Soil) - soil));
                rm.Add(ResourceType.Food, food - rm.GetAmount(ResourceType.Food));
                rm.Add(ResourceType.Soil, soil - rm.GetAmount(ResourceType.Soil));
            }
        }
    }

    static GameObject NewObject(string name, Vector3? position = null)
    {
        var go = new GameObject(name);
        go.transform.position = position ?? origin;
        created.Add(go);
        return go;
    }

    static T Unit<T>(UnitRole role, Vector3 position) where T : AntUnitBase
    {
        var go = NewObject("Regression" + role, position);
        var unit = go.AddComponent<T>();
        unit.Agent.stoppingDistance = .15f;
        var data = ScriptableObject.CreateInstance<UnitData>();
        created.Add(data);
        data.role = role;
        data.moveSpeed = 10;
        data.gatherRate = 50;
        unit.Initialize(data, null, null);
        return unit;
    }

    static FieldInfo Field(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null) return field;
        }
        throw new MissingFieldException(name);
    }

    static object Invoke(object target, string name, params object[] args)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null) return method.Invoke(target, args);
        }
        throw new MissingMethodException(name);
    }
    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        passed.Add("PASS: " + name);
    }
    static async Task Until(Func<bool> condition, string name, int timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition() && (DateTime.UtcNow - start).TotalMilliseconds < timeout) await Task.Delay(20);
        if (!condition())
        {
            foreach (var worker in Object.FindObjectsByType<WorkerAnt>())
                if (worker.name.StartsWith("Regression")) name += $"\n{worker.name}: state={Field(worker, "state").GetValue(worker)} cargo={Field(worker, "carriedAmount").GetValue(worker)} position={worker.transform.position} path={worker.Agent.pathStatus} remaining={worker.Agent.remainingDistance}";
            name += $"\nFood={ResourceManager.Instance.GetAmount(ResourceType.Food)} capacity={ResourceManager.Instance.GetCapacity(ResourceType.Food)}";
        }
        Check(condition(), name);
    }
}
}
