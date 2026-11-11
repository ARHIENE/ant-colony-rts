using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.World;

namespace AntColony.UI
{
    public static partial class GoalChain
    {
        private static int tier, food, bedShortage;
        private static bool wreck;
        private static AirshipYard yard;
        private static void Observe()
        {
            var r = CampaignResearch.Instance;
            tier = 0; yard = null;
            foreach (var lab in SceneObjects.All<ScienceLab>())
                if (lab.isActiveAndEnabled && !lab.IsDead && lab.Tier > tier) tier = lab.Tier;
            foreach (var y in SceneObjects.All<AirshipYard>())
                if (y.isActiveAndEnabled && !y.IsDead && (yard == null || y.Operating)) yard = y;
            wreck = SceneObjects.Any<CrashWreck>() != null;
            food = ResourceManager.Instance?.GetAmount(ResourceType.Food) ?? 0;
            bedShortage = HudOverview.BedShortage;
            Complete(0, !wreck);
            Complete(1, food >= 20 && bedShortage == 0 && HasHomeCommander());
            var researched = r.CompletedCount > 0;
            if (!researched)
                foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
                    if (ResourceStudyRules.RequiresResearch(type) && r.ResourceResearched(type)) { researched = true; break; }
            Complete(2, researched);
            Complete(3, tier >= 2);
            var world = WorldMapManager.Instance;
            if (world != null)
                foreach (var ship in world.Transports)
                    if (ship != null && ship.State != ExpeditionState.Home) { Complete(4, true); break; }
            for (var stage = 5; stage <= 12; stage++)
            {
                var target = TargetTier(stage);
                if (target > 0) Complete(stage, tier >= target);
                else
                {
                    var id = TargetBoss(stage);
                    Complete(stage, r.BossProgress.Delivered(id));
                    Complete(4, r.BossProgress.Defeated(id) || r.BossProgress.Obtained(id) || r.BossProgress.Delivered(id));
                }
            }
            Complete(13, yard != null && yard.Ready);
            Complete(14, yard != null && (yard.Phase == RocketPhase.Boarding || yard.Phase == RocketPhase.Launched));
            Complete(15, r.Departed);
            // 이미 상위 시대에 도달한 구저장에 초반 비축을 다시 강요하지 않는다.
            if (!state.initialized && tier >= 2) state.completed |= 15;
            if (r.Departed) state.completed = AllCompleted;
        }

        public static int TargetTier(int stage) => stage switch { 3 => 2, 6 => 3, 7 => 4, 10 => 5, 11 => 6, _ => 0 };
        private static bool IsBossStage(int stage) => stage == 5 || stage == 8 || stage == 9 || stage == 12;
        private static BossId TargetBoss(int stage) => stage switch { 5 => BossId.A, 8 => BossId.B, 9 => BossId.C, _ => BossId.D };

        private static bool HasHomeCommander()
        {
            foreach (var c in ColonyEvents.HomeCommanders()) return true;
            return false;
        }

        private static string Describe(int stage)
        {
            var context = CampaignNextSteps.Context();
            if (ExpeditionRules.Paused && stage >= 4 && stage < StageCount)
                return "<b>정착지 운영 · 웨이브 방어</b>\n" + ExpeditionRules.PauseReason
                    + "\n" + (context == "" ? Action(2) : context);
            if (stage == StageCount) return "<b>탈출 완료</b>\n결과 화면에서 구출·잔류 기록 확인";
            return "<b>현재 목표 · " + Title(stage) + "</b>\n" + Progress(stage)
                + "\n" + (context == "" ? Action(stage) : context) + "\n<color=#d6d7cb>이후 · " + Reward(stage) + "</color>";
        }
        private static string Progress(int stage)
        {
            var r = CampaignResearch.Instance;
            if (stage == 0) return wreck ? "잔해 회수 0/1" : "잔해 회수 1/1";
            if (stage == 1) return $"창고 식량 {food}/20 · 잠자리 {bedShortage}개 부족";
            if (stage == 2) return ResearchProgress();
            var target = TargetTier(stage);
            if (target > 0)
            {
                var promotion = ScienceLabUpgrades.RequiredResearch(target - 1);
                return $"연구소 {tier}/{target}등급 · " + (r.Has(promotion) ? "승급 연구 완료" : ResearchProgress());
            }
            if (stage == 4) return "원정 출발 0/1 · 보급·장비·잔류 인원 확인";
            if (IsBossStage(stage))
            {
                var id = TargetBoss(stage); var b = r.BossProgress;
                return b.Obtained(id) ? "핵심물 확보 · 본거지 반입 0/1" : b.Defeated(id) ? "보스 격파 · 핵심물 회수 0/1" : "핵심물 반입 0/1";
            }
            if (stage == 13) return $"선체 {(yard != null && yard.Hull ? 1 : 0)}/1 · 엔진 {(yard != null && yard.Engine ? 1 : 0)}/1";
            if (stage == 14) return yard != null && yard.Operating
                ? $"방어 준비 {(AirshipYard.PreparationSeconds - yard.PreparationRemaining) / GameCalendar.SecondsPerDay:0.0}/15일"
                : "로켓 가동 0/1";
            return "발사 0/1 · 탑승자와 남길 인원을 직접 확인";
        }
        private static string ResearchProgress()
        {
            var r = CampaignResearch.Instance;
            if (r.Active != null) return $"{r.Active.Name} {r.Progress / r.Active.Work:0%}";
            if (r.ActiveResource.HasValue) return $"{r.ActiveResource.Value.DisplayName()} 표본 {r.ResourceProgress / CampaignResearch.ResourceResearchWork:0%}";
            return "진행 중인 연구 없음";
        }
        private static string Action(int stage)
        {
            if (stage <= 1) return CampaignNextSteps.EarlyGoal(stage);
            if (stage == 4) return "세계 지도에서 목적지 선택 → 보급 후 직접 출발";
            if (IsBossStage(stage))
            {
                var id = TargetBoss(stage); var b = CampaignResearch.Instance.BossProgress;
                if (b.Obtained(id) || b.Defeated(id)) return "현장에서 핵심물 회수 → 귀환 후 본거지 하역";
                var block = b.DetectionBlock(id);
                return b.Revealed(id) ? BossProgress.Hazard(id) + " · 출정은 직접 결정"
                    : (block == "" ? "탐지기 제작·사용 필요" : block) + " · 과학에서 탐지기 확인";
            }
            if (stage >= 13)
            {
                if (yard == null) return "발사대 없음·파괴 · 과학 시설에서 로켓 건설·복구 확인";
                if (!yard.Ready) return "선체·엔진 미완성 · 과학 시설에서 연구·조립 조건 확인";
                if (!yard.Operating)
                    return !yard.FuelLoaded ? "가동 연료 미적재 · 로켓 화면에서 연료 적재"
                        : "가동 대기 · 로켓 화면에서 핵심물·가동 조건 확인 후 직접 가동";
                if (yard.Phase == RocketPhase.Preparing) return "방어 준비 중 · 비축·방어 인원 확인 후 탑승 대기";
                return "로켓 화면에서 시민·장수 탑승 후 직접 발사";
            }
            var r = CampaignResearch.Instance;
            if (tier == 0) return "B 건설 → 작업 → 정착지 연구대 건설";
            if (r.Paused) return "연구 정지 · 과학에서 재개";
            if (r.Active != null || r.ActiveResource.HasValue)
            {
                foreach (var lab in SceneObjects.All<ScienceLab>()) if (r.LabRate(lab) > 0) return CampaignNextSteps.ParallelGoal(tier);
                foreach (var lab in SceneObjects.All<ScienceLab>())
                    if (lab.isActiveAndEnabled && !lab.IsDead) return r.LabWaitReason(lab) + " · 과학에서 담당·작업표 확인";
            }
            var target = TargetTier(stage);
            if (target > 0 && tier < target)
            {
                var promotion = ScienceLabUpgrades.RequiredResearch(tier);
                if (r.Has(promotion)) return "과학 시설에서 승급 · " + ScienceLabUpgrades.StockStatus(tier, Stock);
                var block = r.BlockReason(promotion);
                if (block == "") block = r.ResearchCostShortage(promotion);
                return (block == "" ? CampaignResearch.Technologies[(int)promotion].Name + " 선택 필요" : block) + " · 과학에서 직접 선택";
            }
            return "다음 선택 · " + CampaignNextSteps.ParallelGoal(tier);
        }
        private static int Stock(ResourceType type) => ResourceManager.Instance?.GetAmount(type) ?? 0;
    }
}
