using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

public static class ImplB6Checks
{
    static int count;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label)
    { if (!value) throw new Exception("B6 FAIL " + count + ": " + label); count++; }

    public static async Task<string> Main()
    {
        count = 0;
        Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var scale = Time.timeScale;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB6-" + Guid.NewGuid().ToString("N"));
        CampaignResearch research = null; CampaignResearch.State previous = null;
        try
        {
            var end = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
            Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;
            var s = new BossProgress.State(); var p = new BossProgress(s);
            Check(BossProgress.Validate(s), "initial state valid");
            Check(p.Revealed(BossId.A) && !p.Revealed(BossId.B) && !p.Recipe(BossId.B), "only A visible");
            Check(!p.RecordKill(BossId.A, float.NaN) && !p.RecordKill(BossId.D, 0), "invalid kill blocked");
            Check(p.RecordKill(BossId.A, 100) && !p.RecordKill(BossId.A, 100), "one kill per cycle");
            Check(!p.Obtained(BossId.A) && !p.Delivered(BossId.A), "death is neither acquisition nor delivery");
            Check(p.TryAcquire(BossId.A, "commander:test", out var a) && a.count == 2, "A gives two cores on pickup");
            Check(p.Obtained(BossId.A) && !p.Delivered(BossId.A) && !p.Recipe(BossId.B), "G19 recovery before recipe");
            Check(!p.TryAcquire(BossId.A, "transport:0", out _), "one reward per cycle");
            Check(p.TryDeliver(a.id) && !p.TryDeliver(a.id) && p.Home(BossId.A) == 2, "delivery once");
            Check(p.Recipe(BossId.B) && p.Recipe(BossId.C) && !p.Recipe(BossId.D), "A opens B/C only");
            foreach (var id in new[] { BossId.B, BossId.C })
            {
                var cost = BossProgress.CraftCost(id, false);
                Check(cost[AntColony.Data.ResourceType.Wood] == 5 && cost[AntColony.Data.ResourceType.Stone] == 5, "detector base cost " + id);
                s.tools[(int)id] = 1;
                Check(p.UseDetector(id) && !p.UseDetector(id) && p.Revealed(id), "detector single use " + id);
                Check(p.RecordKill(id, 200) && p.TryAcquire(id, "transport:0", out _), "kill and acquire " + id);
            }
            Check(!p.Recipe(BossId.D), "B/C acquisition alone does not open D");
            Check(p.TryDeliver(s.cargo[0].id) && !p.Recipe(BossId.D), "one branch delivery insufficient");
            Check(p.TryDeliver(s.cargo[0].id) && p.Recipe(BossId.D), "both branch deliveries open D");
            Check(BossProgress.CraftCost(BossId.D, false).Count == 0, "D no invented wood cost");
            Check(!BossProgress.Tradable && !BossProgress.Recyclable, "core/tool trade and recycling disabled");
            foreach (BossId id in Enum.GetValues(typeof(BossId)))
            {
                var cost = BossProgress.CraftCost(id, true);
                Check(cost[BossProgress.EraMaterial(id)] == 100 && cost[AntColony.Data.ResourceType.Component] == 10, "per-item recovery cost " + id);
            }
            s.craftBoss = 0; s.craftRecovery = true; s.craftRemaining = 60;
            p.TickCraft(59.99f); Check(p.Home(BossId.A) == 2, "recovery not early");
            p.TickCraft(float.PositiveInfinity); Check(p.CraftRemaining > 0, "invalid craft delta ignored");
            p.TickCraft(.02f); Check(p.Home(BossId.A) == 3 && s.cargo.Count == 0, "recovery gives exactly one core, no other reward");
            var due = 100 + BossProgress.RespawnSeconds;
            Check(BossProgress.RespawnSeconds == 1800, "12 G08 days");
            Check(!p.TryRespawn(BossId.A, due - .01f, false, false, false), "respawn not early");
            Check(!p.TryRespawn(BossId.A, due, true, false, false), "ally blocks respawn");
            Check(!p.TryRespawn(BossId.A, due, false, true, false), "transport blocks respawn");
            Check(!p.TryRespawn(BossId.A, due, false, false, true), "combat blocks respawn");
            Check(p.TryRespawn(BossId.A, due, false, false, false) && p.Cycle(BossId.A) == 1, "withdrawal permits next cycle");
            Check(p.RecordKill(BossId.A, due) && p.TryRespawn(BossId.A, due + 1800, false, false, false), "unclaimed reward survives another respawn");
            Check(p.RecordKill(BossId.A, due + 1800), "third cycle death");
            Check(p.TryAcquire(BossId.A, "transport:0", out var old) && old.cycle == 1, "old reward retained");
            Check(p.TryAcquire(BossId.A, "transport:0", out var current) && current.cycle == 2, "new reward separate");
            Check(p.LoseCargo(old.id) && p.Obtained(BossId.A) && p.Delivered(BossId.A), "loss does not undo permanent records");
            var clone = JsonUtility.FromJson<BossProgress.State>(JsonUtility.ToJson(s));
            Check(BossProgress.Validate(clone) && clone.cargo.Count == 1 && clone.cargo[0].id == current.id, "JSON cargo/cycle roundtrip");
            clone.craftRemaining = float.NaN; Check(!BossProgress.Validate(clone), "reject invalid state");

            research = CampaignResearch.Instance; Check(research != null, "campaign exists");
            previous = research.CaptureState(); research.RestoreState(new CampaignResearch.State());
            research.AcquireBlueprint(); Check(!research.HasBlueprint, "old blueprint path cannot skip D");
            research.RecordSampleDelivery(AntColony.Data.ResourceType.IronOre, 0);
            Check(!research.HasSample(AntColony.Data.ResourceType.IronOre), "no zero-quantity sample");
            research.RecordSampleDelivery(AntColony.Data.ResourceType.IronOre, 1);
            Check(research.HasSample(AntColony.Data.ResourceType.IronOre), "one delivered sample recorded");
            Check(research.TryCompleteResourceResearch(AntColony.Data.ResourceType.IronOre), "sample unlocks resource research");
            Check(research.CanMine(AntColony.Data.ResourceType.IronOre, 2, 2)
                && !research.CanMine(AntColony.Data.ResourceType.IronOre, 1, 2), "bulk mining needs research and correct tool");
            Check(research.CanRecoverSample(AntColony.Data.ResourceType.Coal, 1, 2)
                && !research.CanRecoverSample(AntColony.Data.ResourceType.Coal, 0, 2), "previous tool tier can recover sample");
            var rs = research.CaptureState();
            rs.bosses = s;
            Check(CampaignResearch.Validate(rs, out _), "nested campaign state validates");
            research.RestoreState(JsonUtility.FromJson<CampaignResearch.State>(JsonUtility.ToJson(rs)));
            Check(research.HasSample(AntColony.Data.ResourceType.IronOre) && research.BossProgress.Cargo.Count == 1, "campaign JSON roundtrip");
            Check(BossProgress.Hazard(BossId.B).Contains("열") && BossProgress.Reward(BossId.D).Contains("로켓"), "hazard/reward guide");
            CheckConsumption(research);
            CheckCrafting();
            CheckCarriers(research);
            CheckDeathAndBeta(research);
            return "PASS " + count;
        }
        finally
        {
            if (research != null && previous != null) research.RestoreState(previous);
            SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); Time.timeScale = scale;
        }
    }

    static void CheckConsumption(CampaignResearch research)
    {
        research.RestoreState(new CampaignResearch.State());
        Check(!research.TryCompleteResourceResearch(AntColony.Data.ResourceType.Coal), "no research before sample delivery");
        Check(!research.CanMine(AntColony.Data.ResourceType.Coal, 6, 2), "high tool alone cannot unlock bulk mining");
        Check(!research.CanRecoverSample((AntColony.Data.ResourceType)(-1), 1, 2)
            && !research.CanMine((AntColony.Data.ResourceType)(-1), 6, 2), "undefined resource rejected");
        Check(research.CanConsumeProgressionItem(null, ProgressionItemRole.Ordinary), "ordinary items remain consumable");
        foreach (var role in new[] { ProgressionItemRole.UnimportedProgress, ProgressionItemRole.EssentialReward,
            ProgressionItemRole.SowingSeed, ProgressionItemRole.EssentialReward | ProgressionItemRole.SowingSeed })
        {
            Check(!research.CanConsumeProgressionItem("protected", role), "protected by default " + role);
            Check(!research.SetConsumptionOverride("protected", true, false), "warning confirmation required " + role);
            Check(research.SetConsumptionOverride("protected", true, true)
                && research.CanConsumeProgressionItem("protected", role), "explicit opt-out " + role);
            research.RestoreState(JsonUtility.FromJson<CampaignResearch.State>(JsonUtility.ToJson(research.CaptureState())));
            Check(research.CanConsumeProgressionItem("protected", role), "override survives JSON " + role);
            Check(research.SetConsumptionOverride("protected", false, false)
                && !research.CanConsumeProgressionItem("protected", role), "protection restored " + role);
        }
        Check(!research.SetConsumptionOverride("", true, true), "stable item ID required");
        research.RecordSampleDelivery(AntColony.Data.ResourceType.Coal, 1);
        research.RecordSampleDelivery(AntColony.Data.ResourceType.Coal, 1);
        Check(research.CaptureState().samples.Count == 1 && !research.CanRecoverSample(AntColony.Data.ResourceType.Coal, 1, 2), "sample recorded once permanently");
        Check(research.TryCompleteResourceResearch(AntColony.Data.ResourceType.Coal)
            && !research.TryCompleteResourceResearch(AntColony.Data.ResourceType.Coal), "research completes once");
        var invalid = research.CaptureState(); invalid.samples.Clear();
        Check(!CampaignResearch.Validate(invalid, out _), "research without sample rejected on load");
        invalid = research.CaptureState(); invalid.samples.Add((int)AntColony.Data.ResourceType.Coal);
        Check(!CampaignResearch.Validate(invalid, out _), "duplicate samples rejected on load");
        var legacy = JsonUtility.FromJson<CampaignResearch.State>("{}");
        research.RestoreState(legacy);
        Check(research.BossProgress.Revealed(BossId.A) && !research.HasSample(AntColony.Data.ResourceType.Coal), "missing new fields default safely");
    }

    static void CheckCrafting()
    {
        var resources = ResourceManager.Instance;
        GameObject created = null;
        if (resources == null) { created = new GameObject("B6 resources fixture"); resources = created.AddComponent<ResourceManager>(); }
        var bins = resources.Bins.Select(b => (bin: b, state: b.Capture())).ToArray();
        var history = CampaignHistory.Instance?.Capture();
        var amounts = typeof(StoreBin).GetField("amounts", Hidden);
        void Fund(AntColony.Data.ResourceType type, int value)
        {
            foreach (var bin in resources.Bins) ((int[])amounts.GetValue(bin))[(int)type] = 0;
            ((int[])amounts.GetValue(resources.BaseBin))[(int)type] = value;
        }
        try
        {
            // 직접 만든 칸 재고를 사용한다. 불시착 시작에는 Stockpile이 없다.
            var s = new BossProgress.State(); var p = new BossProgress(s);
            Check(!p.TryStartCraft(BossId.A, true, resources), "no recovery before first acquisition");
            p.RecordKill(BossId.A, 0); p.TryAcquire(BossId.A, "fixture", out var a);
            Fund(AntColony.Data.ResourceType.Plank, 100); Fund(AntColony.Data.ResourceType.Component, 9);
            Check(!p.TryStartCraft(BossId.A, true, resources) && resources.GetAmount(AntColony.Data.ResourceType.Plank) == 100,
                "missing component does not spend processing material");
            Fund(AntColony.Data.ResourceType.Component, 10);
            Check(p.TryStartCraft(BossId.A, true, resources), "acquisition permits recovery before delivery");
            Check(resources.GetAmount(AntColony.Data.ResourceType.Plank) == 0
                && resources.GetAmount(AntColony.Data.ResourceType.Component) == 0, "recovery charges both exact costs");
            Check(!p.TryStartCraft(BossId.A, true, resources), "one craft at a time");
            var resumed = JsonUtility.FromJson<BossProgress.State>(JsonUtility.ToJson(s));
            Check(BossProgress.Validate(resumed), "in-progress craft JSON valid");
            p = new BossProgress(resumed); s = resumed;
            p.TickCraft(59); Check(p.Home(BossId.A) == 0, "recovery not paid out early");
            p.TickCraft(1); p.TickCraft(60);
            Check(p.Home(BossId.A) == 1 && !p.Delivered(BossId.A) && !p.Recipe(BossId.B), "recovery one core only, not false import");
            p.TryDeliver(a.id);
            Fund(AntColony.Data.ResourceType.Wood, 5); Fund(AntColony.Data.ResourceType.Stone, 4);
            Check(!p.TryStartCraft(BossId.B, false, resources) && p.Home(BossId.A) == 3
                && resources.GetAmount(AntColony.Data.ResourceType.Wood) == 5, "insufficient detector resources spend nothing");
            Fund(AntColony.Data.ResourceType.Stone, 5);
            var reentered = false;
            Action nested = () => reentered |= p.TryStartCraft(BossId.B, false, resources);
            resources.OnResourcesChanged += nested;
            try { Check(p.TryStartCraft(BossId.B, false, resources), "B detector craft starts"); }
            finally { resources.OnResourcesChanged -= nested; }
            Check(!reentered && p.Home(BossId.A) == 2 && resources.GetAmount(AntColony.Data.ResourceType.Wood) == 0
                && resources.GetAmount(AntColony.Data.ResourceType.Stone) == 0, "detector exact core/resource cost and reentrancy guard");
            p.TickCraft(60);
            Check(p.Tools(BossId.B) == 1 && !p.TryStartCraft(BossId.B, false, resources), "no duplicate detector stock");
            Check(p.UseDetector(BossId.B) && !p.TryStartCraft(BossId.B, false, resources), "revealed target cannot craft detector");
            Fund(AntColony.Data.ResourceType.Wood, 5); Fund(AntColony.Data.ResourceType.Stone, 5);
            Check(p.TryStartCraft(BossId.C, false, resources), "C consumes second branch core");
            p.TickCraft(60); Check(p.UseDetector(BossId.C), "C revealed through crafted tool");
            foreach (var branch in new[] { BossId.B, BossId.C })
            {
                p.RecordKill(branch, 0); p.TryAcquire(branch, "fixture", out var core); p.TryDeliver(core.id);
            }
            Check(p.TryStartCraft(BossId.D, false, resources) && p.Home(BossId.B) == 0 && p.Home(BossId.C) == 0,
                "D detector consumes both imported unique cores");
            p.TickCraft(60); Check(p.UseDetector(BossId.D), "full crafted A-B/C-D chain");
            p.RecordKill(BossId.D, 0); p.TryAcquire(BossId.D, "fixture", out var d); p.TryDeliver(d.id);
            foreach (BossId id in Enum.GetValues(typeof(BossId)))
            {
                Fund(BossProgress.EraMaterial(id), 100); Fund(AntColony.Data.ResourceType.Component, 10);
                var before = p.Home(id);
                Check(p.TryStartCraft(id, true, resources), "repeat core recovery starts " + id);
                p.TickCraft(60);
                Check(p.Home(id) == before + 1 && resources.GetAmount(BossProgress.EraMaterial(id)) == 0
                    && resources.GetAmount(AntColony.Data.ResourceType.Component) == 0, "exact one paid recovery " + id);
            }
            Check(BossProgress.Validate(s), "completed crafting state valid");
            var invalid = JsonUtility.FromJson<BossProgress.State>(JsonUtility.ToJson(s)); invalid.tools[1] = 1;
            Check(!BossProgress.Validate(invalid), "revealed target tool rejected on load");
        }
        finally
        {
            foreach (var bin in bins) typeof(StoreBin).GetMethod("Apply", Hidden).Invoke(bin.bin, new object[] { bin.state });
            if (history != null) CampaignHistory.Instance?.Restore(history);
            if (created != null) UnityEngine.Object.DestroyImmediate(created);
        }
    }

    static void CheckCarriers(CampaignResearch research)
    {
        var first = new GameObject("B6 carrier fixture"); first.SetActive(false);
        var second = new GameObject("B6 other carrier fixture"); second.SetActive(false);
        try
        {
            var ship = first.AddComponent<ExpeditionTransport>();
            var other = second.AddComponent<ExpeditionTransport>();
            var key = BossRewardTransfer.Carrier(ship);
            Check(key == BossRewardTransfer.Carrier(ship) && key != BossRewardTransfer.Carrier(other), "stable distinct carrier IDs without world-list assumption");
            var dto = JsonUtility.FromJson<TransportDto>(JsonUtility.ToJson(new TransportDto { bossCarrierId = key }));
            BossRewardTransfer.RestoreCarrier(other, dto.bossCarrierId, 99);
            Check(BossRewardTransfer.Carrier(other) == key, "carrier ID survives JSON and index changes");
            BossRewardTransfer.RestoreCarrier(other, null, 3);
            Check(BossRewardTransfer.Carrier(other) == "transport:3", "previous B6 cargo index restored compatibly");
            research.RestoreState(new CampaignResearch.State());
            var p = research.BossProgress;
            p.RecordKill(BossId.A, 0); p.TryAcquire(BossId.A, key, out _);
            Check(BossRewardTransfer.CargoLoad(ship) == 2 && BossRewardTransfer.CargoLoad(other) == 0, "core load belongs to one carrier");
            BossRewardTransfer.Deliver(other);
            Check(!p.Delivered(BossId.A), "unrelated carrier cannot deliver reward");
            BossRewardTransfer.Deliver(ship); BossRewardTransfer.Deliver(ship);
            Check(p.Delivered(BossId.A) && p.Home(BossId.A) == 2 && BossRewardTransfer.CargoLoad(ship) == 0, "home transfer records import once");
            p.TryRespawn(BossId.A, BossProgress.RespawnSeconds, false, false, false);
            p.RecordKill(BossId.A, BossProgress.RespawnSeconds); p.TryAcquire(BossId.A, key, out _);
            BossRewardTransfer.Lose(ship);
            Check(p.Cargo.Count == 0 && p.Obtained(BossId.A) && p.Home(BossId.A) == 2, "carrier loss preserves prior imports and recovery qualification");
            var s = research.CaptureState(); s.bosses.revealed[3] = true; research.RestoreState(s);
            p = research.BossProgress; p.RecordKill(BossId.D, 0); p.TryAcquire(BossId.D, key, out _);
            Check(!research.HasBlueprint, "D acquisition does not unlock rocket");
            BossRewardTransfer.Deliver(ship);
            Check(research.HasBlueprint && p.Delivered(BossId.D), "D home delivery unlocks rocket");
        }
        finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); }
    }

    static void CheckDeathAndBeta(CampaignResearch research)
    {
        var go = new GameObject("B6 boss fixture");
        var betaObject = new GameObject("B6 beta fixture"); betaObject.SetActive(false);
        var game = GameManager.Instance;
        var history = CampaignHistory.Instance?.Capture();
        var flags = game == null ? null : new[] { "loopCompleted", "bossDefeated", "defeated" }
            .Select(n => (bool)typeof(GameManager).GetField(n, Hidden).GetValue(game)).ToArray();
        try
        {
            research.RestoreState(new CampaignResearch.State());
            var beta = betaObject.AddComponent<BetaProgress>();
            var screen = GameMenuController.Instance?.ScreenName;
            Check(!beta.BetaScenario, "official campaign is default");
            beta.BetaScenario = true;
            research.RestoreState(JsonUtility.FromJson<CampaignResearch.State>(JsonUtility.ToJson(research.CaptureState())));
            Check(beta.BetaScenario, "beta scenario selection survives campaign JSON");
            beta.BetaScenario = false;
            typeof(BetaProgress).GetMethod("Victory", Hidden).Invoke(beta, null);
            Check(GameMenuController.Instance?.ScreenName == screen, "official first boss does not open beta victory");
            // 활성 씬의 베타 구독자·튜토리얼까지 실행하지 않고 이 검사에서 만든 보스만 검증한다.
            if (game != null) typeof(GameManager).GetField("bossDefeated", Hidden).SetValue(game, true);
            var boss = go.AddComponent<AntColony.Boss.BossHealth>();
            var loop = go.AddComponent<AntColony.Boss.BossBasicPatternLoop>();
            var sequence = go.AddComponent<AntColony.Boss.BossPatternSequenceSimple>();
            var campaignId = boss.GetType().GetProperty("CampaignId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Check(campaignId != null, "loaded AntColony.Boss.BossHealth has CampaignId; recompile B6 runtime sources if missing: " + boss.GetType().AssemblyQualifiedName);
            var campaignSetter = campaignId.GetSetMethod(true);
            Check(campaignSetter != null, "CampaignId has a setter");
            campaignSetter.Invoke(boss, new object[] { (BossId?)BossId.A });
            typeof(AntColony.Boss.BossHealth).GetField("foodReward", Hidden).SetValue(boss, 0);
            typeof(AntColony.Boss.BossHealth).GetField("specialReward", Hidden).SetValue(boss, 0);
            typeof(AntColony.Boss.BossHealth).GetMethod("RestoreDefeated", Hidden).Invoke(boss, null);
            Check(!research.BossProgress.Defeated(BossId.A) && research.CaptureState().bosses.unclaimed.Count == 0,
                "restoring dead boss does not create kill/reward");
            typeof(AntColony.Boss.BossHealth).GetMethod("Respawn", Hidden).Invoke(boss, null);
            Check(loop.enabled && sequence.enabled
                && (float)typeof(AntColony.Boss.BossBasicPatternLoop).GetField("castTimer", Hidden).GetValue(loop)
                    == (float)typeof(AntColony.Boss.BossBasicPatternLoop).GetField("firstCastDelay", Hidden).GetValue(loop)
                && (int)typeof(AntColony.Boss.BossPatternSequenceSimple).GetField("nextPatternIndex", Hidden).GetValue(sequence) == 0,
                "respawn reenables and resets boss patterns");
            boss.TakeDamage(boss.MaxHp); boss.TakeDamage(boss.MaxHp);
            Check(research.BossProgress.Defeated(BossId.A) && research.CaptureState().bosses.unclaimed.Count == 1,
                "actual boss death records exactly one reward");
            Check(!research.BossProgress.Obtained(BossId.A), "actual death still requires pickup");
        }
        finally
        {
            if (flags != null) typeof(GameManager).GetMethod("RestoreFlags", Hidden).Invoke(game, new object[] { flags[0], flags[1], flags[2] });
            if (history != null) CampaignHistory.Instance?.Restore(history);
            UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(betaObject);
        }
    }
}
