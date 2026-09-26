// 전역 서드파티 타입(ResourceType, BossHealth 등)보다 프로젝트 타입이 먼저 해석되도록 네임스페이스 안에서 using 한다.
namespace AntColony.Regression
{
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 장수 채집 숙련도 + 보스 전리품 검사. Play 모드 새 세션에서 실행한다.
public static class WorkProficiencyLootChecks
{
    static int checks;
    static readonly BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;

    static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("FAIL: " + name);
        checks++;
    }

    static FieldInfo Field(object instance, string name)
    {
        for (var type = instance.GetType(); type != null; type = type.BaseType)
        {
            var info = type.GetField(name, Flags | BindingFlags.DeclaredOnly);
            if (info != null) return info;
        }
        throw new Exception("missing field: " + name);
    }
    static object Get(object instance, string field) => Field(instance, field).GetValue(instance);
    static void Set(object instance, string field, object value) => Field(instance, field).SetValue(instance, value);

    static async Task<bool> WaitFor(Func<bool> condition, int seconds = 5)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!condition() && DateTime.UtcNow < deadline) await Task.Delay(20);
        return condition();
    }

    static ResourceNode SpawnNode(ResourceType type, float amount, Vector3 position, Action<ResourceNode> configure = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.SetActive(false);
        go.name = "ProficiencyCheckNode";
        go.transform.position = position;
        var node = go.AddComponent<ResourceNode>();
        node.ConfigureLoot(type, amount);
        configure?.Invoke(node);
        go.SetActive(true);
        return node;
    }

    static BossHealth SpawnBoss(Vector3 position, int food, int special)
    {
        var go = new GameObject("LootCheckBoss");
        go.SetActive(false);
        go.transform.position = position;
        go.AddComponent<BoxCollider>().size = new Vector3(3f, 3f, 3f);
        var boss = go.AddComponent<BossHealth>();
        Set(boss, "maxHp", 100f);
        Set(boss, "foodReward", food);
        Set(boss, "specialReward", special);
        go.SetActive(true);
        return boss;
    }

    static List<ResourceNode> LootNodes() => Object.FindObjectsByType<ResourceNode>()
        .Where(n => n.name.StartsWith("BossLoot ")).ToList();

    static bool Overlaps(RectTransform a, RectTransform b)
    {
        var ca = new Vector3[4];
        var cb = new Vector3[4];
        a.GetWorldCorners(ca);
        b.GetWorldCorners(cb);
        return ca[0].x < cb[2].x && ca[2].x > cb[0].x && ca[0].y < cb[2].y && ca[2].y > cb[0].y;
    }

    // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
    static async System.Threading.Tasks.Task StartGame()
    {
        if (AntColony.Core.GameSession.Instance.GameStarted) return;
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions());
        while (AntColony.Save.SaveSystem.Busy) await System.Threading.Tasks.Task.Delay(50);
        AntColony.UI.GameMenuController.Instance.Resume(); UnityEngine.Time.timeScale = 1;
    }
    // 무기=역할 개편: 예전 보직 변경을 해당 무기(날개) 장착으로 대신한다.
    static bool Arm(AntColony.Units.CommanderAnt c, AntColony.Data.UnitRole role)
    {
        var inv = AntColony.Units.EquipmentInventory.Instance;
        var item = role == AntColony.Data.UnitRole.Flying
            ? new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Armor, armor = AntColony.Units.ArmorKind.Wings, quality = 1 }
            : new AntColony.Units.EquipmentItem { slot = AntColony.Units.EquipmentSlot.Weapon, quality = 1,
                weapon = role == AntColony.Data.UnitRole.Ranged ? AntColony.Units.WeaponKind.AcidSprayer : role == AntColony.Data.UnitRole.Defense ? AntColony.Units.WeaponKind.Shield
                    : role == AntColony.Data.UnitRole.Support ? AntColony.Units.WeaponKind.Pheromone : AntColony.Units.WeaponKind.Mandible };
        if (inv.Full) inv.Items.RemoveAt(0);
        if (!inv.Add(item) || !inv.Equip(c, item)) return false;
        inv.Items.RemoveAll(e => e.slot == item.slot && e.quality == 1 && e != item);
        return role == AntColony.Data.UnitRole.Flying ? c.IsFlying : c.Role == (role == AntColony.Data.UnitRole.Worker ? AntColony.Data.UnitRole.Melee : role);
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        await StartGame();
        checks = 0;

        var rm = ResourceManager.Instance;
        var gm = GameManager.Instance;
        var commander = Object.FindObjectsByType<CommanderAnt>()
            .First(c => c.isActiveAndEnabled && true && true);
        var otherRole = UnitRole.Defense;
        var startRole = commander.Role;
        var savedTroops = commander.TroopCount;
        var savedFree = AntPool.Instance.Free;
        var fishingWas = gm.FishingUnlocked;
        var fishingProp = typeof(GameManager).GetProperty("FishingUnlocked");
        var gatherRate = typeof(WorkerAnt).GetProperty("GatherRate", Flags);
        var carryCapacity = typeof(WorkerAnt).GetProperty("CarryCapacity", Flags);

        var upkeep = Object.FindAnyObjectByType<UpkeepManager>();
        var upkeepEnabled = upkeep.enabled;
        upkeep.enabled = false;
        var threats = Object.FindObjectsByType<MonoBehaviour>().Where(m => m.enabled
            && (m is WildMonster || m is ColonyInvasion)).ToArray();
        foreach (var threat in threats) threat.enabled = false;

        var amounts = (Dictionary<ResourceType, int>)Get(rm, "amounts");
        var savedAmounts = new Dictionary<ResourceType, int>(amounts);
        var selection = Object.FindAnyObjectByType<AntColony.Units.SelectionManager>();
        var spawned = new List<GameObject>();
        var preexistingLoot = LootNodes();
        try
        {
            commander.CommandStop();
            Check(Arm(commander, UnitRole.Worker), "commander takes worker role");
            if (!commander.HasTroops)
            {
                AntPool.Instance.Breed(2);
                Check(commander.TryAssign(1), "commander has a troop");
            }

            // 4) 씬 보스 전리품은 원정 거점의 고정 난이도를 따른다.
            var sceneBoss = Object.FindObjectsByType<BossHealth>().FirstOrDefault(b => b.name != "LootCheckBoss");
            if (sceneBoss != null)
            {
                var difficulty = sceneBoss.GetComponentInParent<ExpeditionSite>()?.Difficulty ?? 1;
                Check((int)Get(sceneBoss, "foodReward") == 100 * difficulty
                    && (int)Get(sceneBoss, "specialReward") == 20 * difficulty, "scene boss loot follows site difficulty");
            }

            // 5) 보스 전리품: 생존·비치명·비활성화에는 드롭 없음.
            var bossPos = commander.transform.position + Vector3.forward * 4f;
            var boss = SpawnBoss(bossPos, 100, 20);
            spawned.Add(boss.gameObject);
            Check(LootNodes().Count == preexistingLoot.Count, "live boss drops nothing");
            boss.TakeDamage(10f);
            Check(LootNodes().Count == preexistingLoot.Count, "nonfatal damage drops nothing");
            boss.gameObject.SetActive(false);
            boss.gameObject.SetActive(true);
            Object.Destroy(boss.gameObject); // 살아 있는 보스 파괴
            await Task.Delay(50);
            Check(LootNodes().Count == preexistingLoot.Count, "disable/destroy of live boss drops nothing");

            // 비치명 피격의 onHPChanged 안에서 치명타: 안쪽·바깥쪽 TakeDamage가 모두 Die에 도달해도 한 번만 처리.
            boss = SpawnBoss(bossPos, 100, 20);
            spawned.Add(boss.gameObject);
            var deaths = 0;
            var hooked = false;
            boss.onHPChanged = new UnityEngine.Events.UnityEvent<float, float>();
            boss.onDead = new UnityEngine.Events.UnityEvent();
            boss.onDead.AddListener(() => deaths++);
            var hpBoss = boss;
            boss.onHPChanged.AddListener((hp, max) => { if (hooked) return; hooked = true; hpBoss.TakeDamage(1000f); });
            boss.TakeDamage(10f);
            var hpDrops = LootNodes().Except(preexistingLoot).ToList();
            spawned.AddRange(hpDrops.Select(d => d.gameObject));
            Check(deaths == 1 && hpDrops.Count == 2, "lethal from nonfatal onHPChanged dies once: " + deaths + "/" + hpDrops.Count);
            foreach (var drop in hpDrops) drop.gameObject.SetActive(false); // 이후 드롭 집계에서 제외
            Object.Destroy(boss.gameObject);

            boss = SpawnBoss(bossPos, 100, 20);
            spawned.Add(boss.gameObject);
            var reentered = 0;
            boss.onHPChanged = new UnityEngine.Events.UnityEvent<float, float>();
            boss.onDead = new UnityEngine.Events.UnityEvent();
            boss.onDead.AddListener(() => { reentered++; boss.TakeDamage(1000f); });
            boss.TakeDamage(1000f);
            boss.TakeDamage(1000f);
            var drops = LootNodes().Except(preexistingLoot).ToList();
            spawned.AddRange(drops.Select(d => d.gameObject));
            Check(reentered == 1 && drops.Count == 2, "death gives exactly two drops once: " + drops.Count);
            var food = drops.Single(d => d.ResourceType == ResourceType.Food);
            var special = drops.Single(d => d.ResourceType == ResourceType.Special);
            Check(food.AmountRemaining == 100f && special.AmountRemaining == 20f, "drop amounts");
            foreach (var drop in drops)
                Check(drop.CanGather && drop.GetComponent<ResourceNodeStatus>() != null
                    && drop.GetComponent<MeshRenderer>().enabled && drop.GetComponent<Collider>().enabled
                    && drop.transform.parent == null, "drop status/render/collider/unparented");
            var bossCollider = boss.GetComponent<Collider>();
            Check(!drops.Any(d => bossCollider.bounds.Intersects(d.GetComponent<Collider>().bounds)), "drops outside boss body");
            Check(UnityEngine.AI.NavMesh.SamplePosition(special.transform.position, out _, 1.5f, UnityEngine.AI.NavMesh.AllAreas),
                "drop sits on NavMesh");
            Object.Destroy(boss.gameObject);
            await Task.Delay(50);
            Check(food != null && special != null && special.CanGather && LootNodes().Except(preexistingLoot).Count() == 2,
                "boss destruction preserves drops");

            var second = SpawnBoss(bossPos + Vector3.forward * 20f, 5, 0);
            spawned.Add(second.gameObject);
            second.TakeDamage(1000f);
            var secondDrops = LootNodes().Except(preexistingLoot).Except(drops).ToList();
            spawned.AddRange(secondDrops.Select(d => d.gameObject));
            Check(secondDrops.Count == 1 && secondDrops[0].ResourceType == ResourceType.Food, "zero reward skipped");

            // 드롭마다 머티리얼을 만들지 않고 프리미티브 기본 머티리얼 + PropertyBlock 색만 쓴다.
            var probe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(probe);
            var defaultMaterial = probe.GetComponent<MeshRenderer>().sharedMaterial;
            var tintId = defaultMaterial.HasProperty(BossLoot.BaseColorId) ? BossLoot.BaseColorId : BossLoot.ColorId;
            Check(defaultMaterial.HasProperty(tintId), "default primitive shader has tint property: " + defaultMaterial.shader.name);
            foreach (var drop in drops.Concat(secondDrops))
            {
                var renderer = drop.GetComponent<MeshRenderer>();
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                var expected = drop.ResourceType == ResourceType.Special ? BossLoot.SpecialColor : BossLoot.FoodColor;
                Check(renderer.sharedMaterial == defaultMaterial && renderer.HasPropertyBlock()
                    && block.GetColor(tintId) == expected, "drop uses shared material + tint: " + renderer.sharedMaterial.name);
            }

            // 6) 실제 장수 채집 + 창고 반납 + 소진.
            // 한 번에 20을 나르도록 병력을 맞춰 반납 반올림 오차 없이 정확히 20이 쌓이게 한다.
            await WaitFor(() => !commander.IsCarrying, 20);
            var needTroops = Mathf.CeilToInt(20f / commander.Data.carryCapacity);
            if (commander.TroopCount < needTroops)
            {
                AntPool.Instance.Breed(needTroops - commander.TroopCount);
                Check(commander.TryAssign(needTroops - commander.TroopCount), "troops to carry 20 in one trip");
            }
            Check((float)carryCapacity.GetValue(commander) >= 20f, "carry capacity >= 20");
            rm.AddCapacity(ResourceType.Special, 100); amounts[ResourceType.Special] = 0;
            Check(await WaitFor(() =>
            {
                if (special.gameObject.activeSelf && !commander.IsCarrying && Get(commander, "state").ToString() == "Idle")
                    commander.CommandGather(special);
                return !special.gameObject.activeSelf && !commander.IsCarrying;
            }, 90), "special loot harvested and deposited");
            Check(rm.GetAmount(ResourceType.Special) == 20, "special stored: " + rm.GetAmount(ResourceType.Special));


            return "PASS: " + checks + " boss loot / harvest checks";
        }
        finally
        {
            selection.ClearSelection();
            commander.CommandStop();
            foreach (var go in spawned) if (go != null) Object.Destroy(go);
            if (commander.TroopCount > savedTroops) commander.ReturnTroops(commander.TroopCount - savedTroops);
            typeof(AntPool).GetProperty("Free").SetValue(AntPool.Instance, savedFree);
            fishingProp.SetValue(gm, fishingWas);
            Arm(commander, startRole);
            foreach (var pair in savedAmounts) amounts[pair.Key] = pair.Value;
            if (!savedAmounts.ContainsKey(ResourceType.Special)) amounts.Remove(ResourceType.Special);
            Set(gm, "bossDefeated", false);
            upkeep.enabled = upkeepEnabled;
            foreach (var threat in threats) if (threat != null) threat.enabled = true;
        }
    }

    // 실제 씬 보스(MiniBird)를 처치해 기본 전리품, 지상 장수 도달 가능, 카메라 클릭 레이 적중을 확인한다.
    // 보스를 되살릴 수 없으므로 매번 새 Play 모드에서 실행한다(씬 파일은 건드리지 않음).
    public static async Task<string> SceneBoss()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        checks = 0;
        var boss = Object.FindObjectsByType<BossHealth>().Single();
        Check(!boss.IsDead && LootNodes().Count == 0, "fresh play mode with live scene boss");
        var bossPos = boss.transform.position;
        var commander = Object.FindObjectsByType<CommanderAnt>()
            .First(c => c.isActiveAndEnabled && !c.IsFlying);
        var cam = Camera.main;
        var mask = (LayerMask)Get(Object.FindAnyObjectByType<UnitSelectionController>(), "groundMask");
        var drops = new List<ResourceNode>();
        var camController = cam.GetComponent<AntColony.Camera.IsometricCameraController>();
        var camControllerEnabled = camController != null && camController.enabled;
        var camPos = cam.transform.position;
        var camRot = cam.transform.rotation;
        try
        {
            boss.TakeDamage(boss.MaxHp);
            await Task.Yield();
            drops = LootNodes();
            Check(boss.IsDead && drops.Count == 2, "scene boss drops two nodes: " + drops.Count);
            Check(drops.Single(d => d.ResourceType == ResourceType.Food).AmountRemaining == 100f
                && drops.Single(d => d.ResourceType == ResourceType.Special).AmountRemaining == 20f, "scene boss default rewards");
            // 기지에 있는 시작 카메라가 아니라, 플레이어가 보스 위치로 팬한 일반 RTS 시점(기존 회전·컨트롤러 거리)에서 클릭한다.
            if (camController != null) camController.enabled = false;
            var midpoint = (drops[0].transform.position + drops[1].transform.position) * 0.5f;
            var viewDistance = camController != null ? (float)Get(camController, "distance") : 30f;
            cam.transform.position = midpoint - cam.transform.forward * viewDistance;
            foreach (var drop in drops)
            {
                var where = $"{drop.name} at {drop.transform.position} (boss {bossPos})";
                Check(commander.CanReach(drop.transform.position), commander.name + " reaches " + where);
                var target = drop.GetComponent<Collider>().bounds.center;
                var viewport = cam.WorldToViewportPoint(target);
                Check(viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f,
                    "loot on screen " + where + ", viewport " + viewport);
                var hitLoot = Physics.Raycast(cam.ScreenPointToRay(cam.WorldToScreenPoint(target)), out var hit, 500f, mask)
                    && hit.collider.GetComponentInParent<ResourceNode>() == drop;
                Check(hitLoot, "camera click ray hits " + where + ", got " + (hit.collider != null ? hit.collider.name : "nothing"));
            }
            return "PASS: " + checks + " scene boss loot checks";
        }
        finally
        {
            cam.transform.SetPositionAndRotation(camPos, camRot);
            if (camController != null) camController.enabled = camControllerEnabled;
            foreach (var drop in drops) if (drop != null) Object.Destroy(drop.gameObject);
        }
    }
}
}
