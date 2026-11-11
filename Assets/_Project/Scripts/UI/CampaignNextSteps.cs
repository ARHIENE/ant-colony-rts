using AntColony.Core;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.World;
using System.Linq;


namespace AntColony.UI
{
    // 현재 상태만 읽는 안내. 연구·출정·발사 선택과 저장 상태는 변경하지 않는다.
    public static class CampaignNextSteps
    {
        public static string Text() => GoalChain.CurrentText;

        // 긴급 회복·이동은 목표 아래 행동 한 줄에 표시한다.
        public static string Context()
        {
            foreach (var c in AntColony.World.ColonyEvents.HomeCommanders())
                if (c.Health.LifeThreatening) return "위독한 장수 · 의료에서 출혈·약품·작업 허용 먼저 확인";
            var world = AntColony.World.WorldMapManager.Instance;
            if (world != null)
                foreach (var ship in world.Transports)
                {
                    if (ship == null || ship.State == AntColony.World.ExpeditionState.Home) continue;
                    var wait = ship.TravelWaitReason;
                    if (wait != "") return wait;
                    if (ship.State == AntColony.World.ExpeditionState.Outbound) return $"원정 이동 {ship.TravelRemainingSeconds:0}초 · 도착 후 전장 보기";
                    if (ship.State == AntColony.World.ExpeditionState.Returning) return $"귀환 {ship.TravelRemainingSeconds:0}초 · 본거지 도착 후 하역";
                    return "현장 · " + SiteNames.Name(ship.Site) + " · 회수·귀환은 직접 지시";
                }
            return WaveDefense.Instance?.InWave == true ? "" : RecoverySummary(false);
        }
        public static string RecoverySummary(bool includeHealthy)
        {
            int injured = ColonyEvents.HomeCommanders().Count(c => c.PersonalHealth < GameBalance.CommanderHealth
                || c.Health.wounds.Any(w => !w.Permanent && w.damage > 0));
            int damaged = SceneObjects.All<BuildingBase>().Count(b => b.isActiveAndEnabled && !b.IsDead
                && b.CountsTowardPlayerDefeat && b.CurrentHealth < b.MaxHealth);
            if (!includeHealthy && injured == 0 && damaged == 0) return "";
            return $"부상 {injured}명 · 파손 {damaged}곳" + (injured + damaged > 0 ? " · 치료·수리 작업표 확인" : " · 다음 방어 준비");
        }

        public static string EarlyGoal(int stage)
        {
            bool storage = SceneObjects.All<BuildingBase>().Any(b => b.isActiveAndEnabled && !b.IsDead && b.Data?.kind == BuildingKind.Storage);
            bool planned = SceneObjects.All<BuildingConstructionSite>().Any(s => s.isActiveAndEnabled && s.BuildingKind == BuildingKind.Storage);
            if (!storage && !planned) return "B 건설 → 저장 → 창고 · 채집물을 입고할 곳 마련";
            var block = ConstructionBlock();
            if (block != "") return block;
            if (!ResourceNode.Available.Any(n => n.NeedsDesignation && n.WorkRequested && !n.WorkPaused && !n.IsDepleted))
                return "작업 지정 → 벌목·채굴·야생 채집 대상 선택";
            if (stage == 0) return "잔해 선택 → 철거 지정으로 건설 재료 회수";
            return HudOverview.BedShortage > 0 ? "B 건설 → 생활 → 잠자리 마련"
                : "채집한 식량을 창고로 운반 · 장수 작업표의 운반 허용 확인";
        }

        private static string ConstructionBlock()
        {
            // 기존 예정지의 대기 판정을 사용한다. 이 안내는 작업 순서를 바꾸지 않는다.
            AntColony.Buildings.BuildingConstructionSite first = null;
            foreach (var site in SceneObjects.All<AntColony.Buildings.BuildingConstructionSite>())
                if (site.isActiveAndEnabled && !site.Paused && (first == null || site.Order < first.Order)) first = site;
            if (first != null)
            {
                var reason = first.WaitReason;
                switch (reason)
                {
                    case AntColony.Buildings.BuildingConstructionSite.Wait.MaterialShortage:
                        foreach (var need in first.Needs)
                            if (need.delivered + need.reserved + first.InTransit(need.type) < need.need)
                                return "건설 재료 부족 · 예정지 선택 → 부족 재료 확인·채집 지정";
                        break;
                    case AntColony.Buildings.BuildingConstructionSite.Wait.AwaitingHaul:
                        return "건설 운반 대기 · 장수 작업표의 운반 허용·다른 작업 확인";
                    case AntColony.Buildings.BuildingConstructionSite.Wait.NoWorker:
                    case AntColony.Buildings.BuildingConstructionSite.Wait.Forbidden:
                        return "건설 담당 없음 · 장수 작업표에서 건설·운반 허용 확인";
                    case AntColony.Buildings.BuildingConstructionSite.Wait.Unreachable:
                        return "건설 접근 대기 · 장수의 작업 가능 상태·예정지까지 통로 확인";
                }
            }
            return "";
        }

        public static string ParallelGoal(int tier)
        {
            var block = ConstructionBlock();
            if (block != "") return block;
            if (HudOverview.BedShortage > 0)
                return $"잠자리 {HudOverview.BedShortage}개 부족 · B 건설에서 숙소 확인";
            if (WaveDefense.Instance is WaveDefense wave && !wave.InWave)
            {
                var research = CampaignResearch.Instance;
                var choice = research != null && research.Active == null && !research.ActiveResource.HasValue
                    ? "연구 메뉴에서 연구 선택 / B 방어 → 산성탑" : "B 방어 → 산성탑 준비";
                if (WaveDefense.CanStart) choice += " / 우상단 앞당기기";
                int idle = CommanderRoster.Instance == null ? 0 : CommanderRoster.Instance.Commanders.Count(ControlGroups.IsIdle);
                return choice + (idle > 0 ? $"\n대기 장수 {idle}명 · . 키로 찾기" : "");
            }
            return DevelopmentGoal(tier);
        }

        private static string DevelopmentGoal(int tier) => tier switch
        {
            0 or 1 => "침상·식탁·변소와 지정 채집으로 생활 안정",
            2 => AntColony.World.ExpeditionRules.Paused ? "학당·진료소 운영비와 방어 준비" : "학당·진료소 운영비와 차량 원정 준비",
            3 => "고급 작물·치수 공사로 계절 대비",
            4 => AntColony.World.ExpeditionRules.Paused ? "냉장 물류와 방어 비축 준비" : "냉장 물류·항공 수송과 B/C 장비 준비",
            5 => "보온·부위 재생으로 부상자 복귀 준비",
            _ => "구출할 시민·장수와 로켓 좌석·방어 비축 준비"
        };

    }
}
