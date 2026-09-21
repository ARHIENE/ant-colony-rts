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

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Play mode required");
        checks = 0;

        var rm = ResourceManager.Instance;
        var gm = GameManager.Instance;
        var commander = Object.FindObjectsByType<CommanderAnt>()
            .First(c => c.isActiveAndEnabled && c.CanTakeRole(UnitRole.Worker) && c.AllowedRoles.Count > 1);
        var otherRole = commander.AllowedRoles.First(r => r != UnitRole.Worker);
        var startRole = commander.Role;
        var work = commander.WorkProficiency;
        var savedLevel = work.Level;
        var savedProgress = work.Progress;
        var startRank = commander.Rank;
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
            Check(commander.TrySetRole(UnitRole.Worker), "commander takes worker role");
            if (!commander.HasTroops)
            {
                AntPool.Instance.Breed(2);
                Check(commander.TryAssign(1), "commander has a troop");
            }
            Set(work, "level", 0);
            Set(work, "progress", 0f);

            // 1) 순수 누적 규칙: 무효 입력 무시, 이월, 5레벨 상한.
            work.AddGathered(0f);
            work.AddGathered(-5f);
            work.AddGathered(float.NaN);
            Check(work.Level == 0 && work.Progress == 0f, "zero/negative/NaN earns nothing");
            work.AddGathered(250.5f);
            Check(work.Level == 2 && Mathf.Approximately(work.Progress, 50.5f), "carry fractional progress: " + work.Level + "/" + work.Progress);

            var troops = commander.TroopCount;
            var attack = commander.AttackDamage;
            var armor = commander.Armor;
            var limit = commander.CommandLimit;
            work.AddGathered(10000f);
            Check(work.Level == CommanderWorkProficiency.MaxLevel && work.Progress == 0f
                && Mathf.Approximately(work.GatherMultiplier, 1.5f), "level 5 cap = +50%");
            Check(Mathf.Approximately((float)gatherRate.GetValue(commander), commander.Data.gatherRate * troops * 1.5f),
                "gather rate +50%");
            Check(Mathf.Approximately((float)carryCapacity.GetValue(commander), commander.Data.carryCapacity * troops)
                && commander.AttackDamage == attack && commander.Armor == armor && commander.CommandLimit == limit,
                "carry/combat/limit unchanged");

            // 2) 보직·병력·비활성화에도 유지.
            Check(commander.TrySetRole(otherRole) && work.Level == 5, "role change keeps level");
            Check(commander.TrySetRole(UnitRole.Worker), "back to worker");
            var otherRank = Enum.GetValues(typeof(CommanderRank)).Cast<CommanderRank>()
                .First(r => r != startRank && CommanderRanks.CommandLimit(r) >= commander.TroopCount);
            Check(commander.TrySetRank(otherRank) && work.Level == 5, "rank change keeps level");
            Check(commander.TrySetRank(startRank) && work.Level == 5, "rank restored keeps level");
            Set(commander, "pendingDamage", 0f);
            Check(commander.ReturnTroops(troops) == troops && work.Level == 5, "troop return keeps level");
            Check(commander.TryAssign(troops), "troops restored");
            commander.gameObject.SetActive(false);
            commander.gameObject.SetActive(true);
            if (commander.TroopCount < troops) commander.TryAssign(troops - commander.TroopCount);
            Check(work.Level == 5 && commander.HasTroops, "disable/enable keeps level");

            // 3) 실제 채집만 진행도를 준다.
            Set(work, "level", 0);
            Set(work, "progress", 0f);
            await Task.Delay(300);
            Check(work.Progress == 0f, "idle earns nothing");

            var carry = (float)carryCapacity.GetValue(commander);
            var partial = Mathf.Min(carry * 0.5f, 7.5f); // 한 짐 안에 끝나는 부분 채집
            var node = SpawnNode(ResourceType.Food, partial, commander.transform.position + Vector3.right);
            spawned.Add(node.gameObject);

            Set(commander, "pendingDamage", 0f);
            Check(commander.ReturnTroops(troops) == troops, "troops removed");
            commander.CommandGather(node);
            Check(Get(commander, "targetNode") == null, "troop-zero gather rejected");
            Check(commander.TryAssign(troops), "troops back");

            commander.CommandGather(node);
            commander.CommandStop();
            await Task.Delay(200);
            Check(work.Progress == 0f && Mathf.Approximately(node.AmountRemaining, partial), "canceled gather earns nothing");

            commander.CommandGather(node);
            Check(await WaitFor(() => !node.gameObject.activeSelf, 20), "partial node depleted by real gathering: "
                + "state=" + Get(commander, "state") + " stock=" + node.AmountRemaining
                + " troops=" + commander.TroopCount + " reachable=" + commander.CanReach(node.transform.position)
                + " position=" + commander.Position + " node=" + node.transform.position);
            Check(Mathf.Abs(work.Progress - partial) < 0.01f && work.Level == 0, "progress equals extracted amount: " + work.Progress);
            var afterDepleted = work.Progress;
            commander.CommandGather(node);
            await Task.Delay(200);
            Check(work.Progress == afterDepleted, "depleted node earns nothing");
            await WaitFor(() => !commander.IsCarrying, 20);
            commander.CommandStop();

            // 3-1) 잠긴 낚시터: 명령 거부·추출 0·진행도 없음. 해금 후 실제 낚시량만큼만 오른다.
            var small = Mathf.Min(carry * 0.5f, 5f);
            fishingProp.SetValue(gm, false);
            var fish = SpawnNode(ResourceType.Food, small, commander.transform.position + Vector3.left,
                n => Set(n, "requiresFishing", true));
            spawned.Add(fish.gameObject);
            var before = work.Progress;
            commander.CommandGather(fish);
            await Task.Delay(200);
            Check(Get(commander, "targetNode") == null && fish.Extract(1f) == 0f && work.Progress == before
                && fish.AmountRemaining == small, "locked fishing gives no progress");
            fishingProp.SetValue(gm, true);
            commander.CommandGather(fish);
            Check(await WaitFor(() => !fish.gameObject.activeSelf, 20), "fishing node depleted");
            Check(Mathf.Abs(work.Progress - before - small) < 0.001f, "fishing progress equals extracted: " + (work.Progress - before));
            await WaitFor(() => !commander.IsCarrying, 20);
            commander.CommandStop();

            // 3-2) 재성장 밭: 수확량만큼 오르고, 재성장 중(소진)에는 진행도 없음.
            var farm = SpawnNode(ResourceType.Food, small, commander.transform.position + Vector3.back,
                n => { Set(n, "regrowSeconds", 1000f); Set(n, "regrowAmount", small); });
            spawned.Add(farm.gameObject);
            before = work.Progress;
            commander.CommandGather(farm);
            Check(await WaitFor(() => farm.IsRegrowing, 20), "farm harvested to regrow");
            Check(Mathf.Abs(work.Progress - before - small) < 0.001f, "farm progress equals extracted: " + (work.Progress - before));
            await WaitFor(() => !commander.IsCarrying, 20);
            before = work.Progress;
            commander.CommandGather(farm);
            await Task.Delay(200);
            Check(work.Progress == before && farm.gameObject.activeSelf && farm.Extract(1f) == 0f, "regrowing farm earns nothing");
            commander.CommandStop();

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
            amounts[ResourceType.Special] = 0;
            var progressBefore = work.Progress;
            Check(await WaitFor(() =>
            {
                if (special.gameObject.activeSelf && !commander.IsCarrying && Get(commander, "state").ToString() == "Idle")
                    commander.CommandGather(special);
                return !special.gameObject.activeSelf && !commander.IsCarrying;
            }, 90), "special loot harvested and deposited");
            Check(rm.GetAmount(ResourceType.Special) == 20, "special stored: " + rm.GetAmount(ResourceType.Special));
            Check(Mathf.Abs(work.Progress - progressBefore - 20f) < 0.01f, "loot harvest earns proficiency");

            // 7) UI 전용 줄.
            var panel = Object.FindAnyObjectByType<AntColony.UI.SelectedUnitPanel>();
            var workText = (UnityEngine.UI.Text)typeof(AntColony.UI.SelectedUnitPanel).GetField("workText", Flags).GetValue(panel);
            selection.ClearSelection();
            typeof(AntColony.Units.SelectionManager).GetMethod("AddToSelection", Flags)
                .Invoke(selection, new object[] { commander.GetComponent<AntColony.Units.SelectableObject>() });
            Set(work, "level", 2);
            Set(work, "progress", 37.6f);
            Check(await WaitFor(() => workText.text == "Gather Lv 2 (37/100) +20% speed"), "work text: " + workText.text);
            // 레벨마다 가장 긴 표시값(진행도 99)과 MAX 모두 줄 폭 안에 들어가야 한다.
            for (var lv = 0; lv <= CommanderWorkProficiency.MaxLevel; lv++)
            {
                Set(work, "level", lv);
                Set(work, "progress", lv < CommanderWorkProficiency.MaxLevel ? 99.9f : 0f);
                var expected = lv < CommanderWorkProficiency.MaxLevel
                    ? $"Gather Lv {lv} (99/100) +{lv * 10}% speed" : "Gather Lv 5 (MAX) +50% speed";
                Check(await WaitFor(() => workText.text == expected), "work text: " + workText.text);
                Check(workText.preferredWidth <= workText.rectTransform.rect.width,
                    "work text fits: " + expected + " " + workText.preferredWidth);
            }
            foreach (var button in workText.transform.parent.GetComponentsInChildren<UnityEngine.UI.Button>())
                Check(!Overlaps(workText.rectTransform, (RectTransform)button.transform), "work text clear of " + button.name);
            var combat = (UnityEngine.UI.Text)typeof(AntColony.UI.SelectedUnitPanel).GetField("combatStatsText", Flags).GetValue(panel);
            Check(!Overlaps(workText.rectTransform, combat.rectTransform), "work text clear of combat stats");

            return "PASS: " + checks + " work proficiency / boss loot / ui checks";
        }
        finally
        {
            selection.ClearSelection();
            commander.CommandStop();
            foreach (var go in spawned) if (go != null) Object.Destroy(go);
            Set(work, "level", savedLevel);
            Set(work, "progress", savedProgress);
            if (commander.TroopCount > savedTroops) commander.ReturnTroops(commander.TroopCount - savedTroops);
            commander.TrySetRank(startRank);
            typeof(AntPool).GetProperty("Free").SetValue(AntPool.Instance, savedFree);
            fishingProp.SetValue(gm, fishingWas);
            commander.TrySetRole(startRole);
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
