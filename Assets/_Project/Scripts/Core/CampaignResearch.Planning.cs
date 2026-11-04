using System;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;

namespace AntColony.Core
{
    public sealed partial class CampaignResearch
    {
        public static float LabMultiplier(int tier) => 1f + .25f * (tier - 1);
        // 실행과 표시가 같은 참여 조건을 사용한다. 담당 연결이 끊긴 장수는 중복 기여하지 않는다.
        public float LabRate(ScienceLab lab) => LabWaitReason(lab) == ""
            ? lab.Target.WorkRate(CommanderActivity.Research) * LabMultiplier(lab.Tier) : 0;
        public float ResearchRate => Paused ? 0 : SceneObjects.All<ScienceLab>().Sum(LabRate);
        public string TimeEstimate
        {
            get
            {
                if (Active == null && ActiveResource == null) return "연구를 직접 선택하세요";
                if (Paused) return "정지 · 진행 보존";
                var rate = ResearchRate;
                var left = Active != null ? Active.Work - Progress : ResourceResearchWork - ResourceProgress;
                return rate > 0 ? $"현재 {rate:0.00} 연구량/초 · 연속 작업 {Math.Max(0, left) / rate:0}초 남음(게임 시간)"
                    : "기여 0 · 아래 연구소의 담당·작업표·복귀 상태를 확인하세요";
            }
        }

        public string ResearchCostShortage(ScienceTechnology technology)
        {
            if (Has(technology) || Active?.Technology == technology || SavedProgress(technology) >= 0) return "";
            var d = Technologies[(int)technology];
            var rm = ResourceManager.Instance;
            var missing = new[] { (ResourceType.Food, d.Food), (ResourceType.Soil, d.Soil), (ResourceType.Special, d.Special) }
                .Where(c => (rm?.GetAmount(c.Item1) ?? 0) < c.Item2)
                .Select(c => $"{c.Item1.DisplayName()} {rm?.GetAmount(c.Item1) ?? 0}/{c.Item2}");
            var text = string.Join(" / ", missing);
            return text.Length == 0 ? "" : "입고 재고 부족: " + text;
        }
    }
}
