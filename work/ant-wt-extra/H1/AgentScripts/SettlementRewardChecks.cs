using System;
using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Regression
{
    using ResourceType = AntColony.Data.ResourceType;
    // 새 Play 세션에서 실행. 검사 후 Play 종료로 런타임 상태를 복구한다.
    public static class SettlementRewardChecks
    {
        static int checks;
        static void Check(bool ok, string message)
        { if (!ok) throw new Exception("FAIL: " + message); checks++; }

        public static string Main()
        {
            checks = 0;
            Check(Application.isPlaying, "Play mode required");
            var template = (EnemyColony)typeof(WorldMapManager).GetField("colonyTemplate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(WorldMapManager.Instance);
            var templateNodes = template.GetComponentsInChildren<ResourceNode>(true);
            var resources = ResourceManager.Instance;
            var homeFood = resources.GetAmount(ResourceType.Food);
            foreach (var difficulty in new[] { 1, 2, 3 })
            {
                var site = WorldMapManager.Instance.Sites.First(s => s.Colony != null && s.Difficulty == difficulty);
                var colony = site.Colony;
                var nodes = colony.GetComponentsInChildren<ResourceNode>(true);
                foreach (var node in nodes)
                {
                    Check(node.AmountRemaining == templateNodes.First(n => n.name == node.name).AmountRemaining * difficulty,
                        $"difficulty {difficulty}: initial {node.ResourceType} stock uses template multiplier");
                    Check(node.IsRaidLocked, "scaled stock retains conquest lock");
                }
                foreach (var node in nodes) Check(node.TryConsumeStock(node.AmountRemaining), "empty stock for production check");
                typeof(ExpeditionSite).GetProperty("Disposition").SetValue(site, ConquestDisposition.Annexed);
                var settlement = site.gameObject.AddComponent<AnnexedSettlement>();
                settlement.Tick(59);
                Check(colony.GetStock(ResourceType.Food) == 0, "production waits for full period");
                settlement.Tick(1);
                Check(colony.GetStock(ResourceType.Food) == 10 * difficulty
                    && colony.GetStock(ResourceType.Soil) == 5 * difficulty, "production scales with difficulty");
                settlement.Tick(120);
                Check(colony.GetStock(ResourceType.Food) == 30 * difficulty
                    && colony.GetStock(ResourceType.Soil) == 15 * difficulty, "multiple periods scale once");
                settlement.Tick(float.MaxValue);
                Check(colony.GetStock(ResourceType.Food) == 300 * difficulty
                    && colony.GetStock(ResourceType.Soil) == 200 * difficulty, "large elapsed time respects scaled caps");
                colony.AddResources(int.MaxValue, int.MaxValue);
                Check(colony.GetStock(ResourceType.Food) == 300 * difficulty
                    && colony.GetStock(ResourceType.Soil) == 200 * difficulty, "shared stock addition respects scaled caps");
                colony.TrySpendResources(10, 5);
                typeof(ExpeditionSite).GetProperty("Disposition").SetValue(site, ConquestDisposition.Lost);
                settlement.Tick(60);
                Check(colony.GetStock(ResourceType.Food) == 300 * difficulty - 10, "lost settlement stops production");
                typeof(ExpeditionSite).GetProperty("Disposition").SetValue(site, ConquestDisposition.Annexed);
                settlement.Tick(60);
                Check(colony.GetStock(ResourceType.Food) == 300 * difficulty
                    && colony.GetStock(ResourceType.Soil) == 200 * difficulty, "reconquest resumes without multiplying caps again");
            }
            Check(resources.GetAmount(ResourceType.Food) == homeFood, "rewards stay local");
            return $"PASS: {checks} settlement reward checks";
        }
    }
}
