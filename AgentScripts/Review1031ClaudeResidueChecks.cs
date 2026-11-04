using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
// 2026-10-31 Claude: L1 실행 잔여(연구소 실물 승급 차감·정기 교환 중단 실제 월간 처리·노년 자연사 연간 정산) Play 검사.
public static class Review1031ClaudeResidueChecks
{
    static int count;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Review1031Residue " + count + ": " + label); count++; }
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Review1031Residue-" + Guid.NewGuid().ToString("N"));
        GameObject labObject = null;
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 311032, mapSize = MapSize.Small, startingCount = 2 }); await Wait(() => !SaveSystem.Busy);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var rm = ResourceManager.Instance;

            // 연구소 실물 승급: 선행 연구 없으면 거부, 있으면 표시 비용만 차감하고 1등급 상승.
            labObject = new GameObject("Review1031 Lab"); var lab = labObject.AddComponent<ScienceLab>();
            var cost = ScienceLabUpgrades.Cost(1);
            foreach (var c in cost) rm.AddCapacity(c.type, c.amount * 2);
            foreach (var c in cost) rm.Add(c.type, c.amount);
            Check(!lab.TryUpgrade() && lab.Tier == 1, "선행 연구 없이 승급 거부");
            var research = CampaignResearch.Instance;
            var state = typeof(CampaignResearch).GetField("state", Any).GetValue(research);
            var completed = (System.Collections.Generic.List<int>)state.GetType().GetField("completed").GetValue(state);
            completed.Add((int)ScienceLabUpgrades.RequiredResearch(1));
            var before = cost.Select(c => rm.GetAmount(c.type)).ToArray();
            Check(lab.TryUpgrade() && lab.Tier == 2, "선행 연구+재료로 2등급");
            Check(cost.Select((c, i) => before[i] - rm.GetAmount(c.type) == c.amount).All(x => x), "표시 재료만큼 실재고 차감");
            Check(!lab.TryUpgrade(), "다음 등급 재료 없으면 거부");

            // 정기 교환 중단: 협정 유지 중에도 월간 식량→흙 교환 없음, 재개 시 교환.
            var d = DiplomacyManager.Instance; var civ = d.Data.civilizations.First(c => !c.extinct);
            civ.contacted = true; civ.war = false; civ.affinity = 0; civ.nextOffer = float.MaxValue; civ.nextRaid = float.MaxValue;
            civ.treaties[(int)TreatyKind.Trade] = d.Data.elapsed + 1e6f; civ.resources[1] = 1000;
            rm.AddCapacity(AntColony.Data.ResourceType.Soil, 100); rm.AddCapacity(AntColony.Data.ResourceType.Food, 100);
            rm.Add(AntColony.Data.ResourceType.Food, 50);
            var monthly = typeof(DiplomacyManager).GetMethod("Monthly", Any);
            Check(d.SetPeriodicTradePaused(civ, true), "중단 설정");
            var soil = rm.GetAmount(AntColony.Data.ResourceType.Soil); var aff = civ.affinity;
            monthly.Invoke(d, new object[] { civ });
            Check(rm.GetAmount(AntColony.Data.ResourceType.Soil) == soil && civ.affinity == aff && civ.HasTreaty(TreatyKind.Trade, d.Data.elapsed), "중단 중 교환·호감 없음, 협정 유지");
            d.SetPeriodicTradePaused(civ, false);
            monthly.Invoke(d, new object[] { civ });
            Check(rm.GetAmount(AntColony.Data.ResourceType.Soil) == soil + DiplomacyRules.MonthlyTradeAmount, "재개 후 월간 교환");

            // 노년 자연사: 연간 정산에서 15%, 정산 대기 제외, 소수 이월.
            var pop = ColonyPopulation.Instance; var s = pop.S;
            s.ages[64] = 0; s.old = 100; s.agingDue = 20; s.oldDeathRemainder = 0;
            pop.AgeYear();
            Check(s.old == 88 && Math.Abs(s.oldDeathRemainder) < 1e-6, "대기20 제외 80명 중 12명 자연사");
            s.old = 30; s.agingDue = 0; s.ages[64] = 0;
            pop.AgeYear();
            Check(s.old == 26 && Math.Abs(s.oldDeathRemainder - .5) < 1e-6, "30명 4.5 → 4명 사망·0.5 이월");
            return "Review1031Residue passed " + count;
        }
        finally
        {
            if (labObject != null) UnityEngine.Object.Destroy(labObject);
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
