using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;

namespace AntColony.UI
{
    // 현재 상태만 읽는 안내. 연구·출정·발사 선택과 저장 상태는 변경하지 않는다.
    public static class CampaignNextSteps
    {
        public static string Text()
        {
            var r = CampaignResearch.Instance;
            if (r == null) return "과학연구소 건설 · 장수 배정";
            if (r.Departed) return "로켓 발사 완료 · 결과 화면에서 구출·잔류 기록 확인";
            var yard = SceneObjects.All<AirshipYard>().FirstOrDefault(y => !y.IsDead && y.Operating);
            if (yard != null) return $"최종 방어 · 준비 {yard.PreparationRemaining / GameCalendar.SecondsPerDay:0.0}일 남음\n공공서비스에서 비축 확인 · 탑승·발사는 직접 결정";
            var patients = AntColony.World.ColonyEvents.HomeCommanders().Where(c => c.Health.LifeThreatening).ToArray();
            if (patients.Length > 0) return $"회복 우선 · 위독한 장수 {patients.Length}명\n의료 화면에서 출혈·약품·의료 작업 허용 확인";
            var travel = TravelGoal();
            if (travel != "") return travel;
            if (r.Active != null || r.ActiveResource != null)
                return $"{(r.Paused ? "연구 정지" : "연구")} · {r.Active?.Name ?? (r.ActiveResource.Value.DisplayName() + " 표본")}\n"
                    + (r.Paused ? "과학 화면에서 재개 · 진행은 보존됩니다" : r.ResearchRate > 0 ? "진행 중 · " + ParallelGoal(HighestLab()) : "과학 화면에서 담당·작업표·접근 상태 확인");
            var tier = HighestLab();
            if (tier == 0) return "과학연구소 건설 · 인구 제한 없음\n과학에서 직접 연구 선택 · 연구 장수 배정";
            if (tier >= 6) return LateGoal(r);
            var promotion = ScienceLabUpgrades.RequiredResearch(tier);
            return r.Has(promotion) ? $"연구소 {tier}→{tier + 1}등급 승급 준비\n과학 시설에서 " + ScienceLabUpgrades.StockStatus(tier, t => ResourceManager.Instance?.GetAmount(t) ?? 0)
                : $"{CampaignResearch.EraNames[tier - 1]} 시대 · {CampaignResearch.Technologies[(int)promotion].Name}\n과학에서 직접 선택 · " + ParallelGoal(tier);
        }

        private static int HighestLab() => SceneObjects.All<ScienceLab>().Where(l => l.isActiveAndEnabled && !l.IsDead).Select(l => l.Tier).DefaultIfEmpty(0).Max();

        private static string TravelGoal()
        {
            var world = AntColony.World.WorldMapManager.Instance;
            if (world == null) return "";
            foreach (var ship in world.Transports)
            {
                if (ship == null || ship.State == AntColony.World.ExpeditionState.Home) continue;
                if (ship.State == AntColony.World.ExpeditionState.Outbound) return $"이동 중 · {ship.Remaining:0}초\n{SiteNames.Name(ship.Site)} · 도착 후 전장 보기";
                if (ship.State == AntColony.World.ExpeditionState.Returning) return $"귀환 중 · {ship.Remaining:0}초\n도착 후 본거지에 화물 하역";
                return $"현장 · {SiteNames.Name(ship.Site)}\n전투·회수 후 장수를 모아 귀환을 직접 지시";
            }
            return "";
        }

        public static string ParallelGoal(int tier) => tier switch
        {
            0 or 1 => "침상·식탁·변소와 지정 채집으로 생활 안정",
            2 => "학당·진료소 운영비와 차량 원정 준비",
            3 => "고급 작물·치수 공사로 계절 대비",
            4 => "냉장 물류·항공 수송과 B/C 장비 준비",
            5 => "보온·부위 재생으로 부상자 복귀 준비",
            _ => "구출할 시민·장수와 로켓 좌석·방어 비축 준비"
        };

        private static string LateGoal(CampaignResearch r)
        {
            var b = r.BossProgress;
            foreach (var id in new[] { BossId.A, BossId.B, BossId.C, BossId.D })
            {
                if (b.Delivered(id)) continue;
                if (b.Obtained(id) || b.Defeated(id)) return $"{id} 핵심물 회수·본거지 반입\n과학 시설에서 화물과 탐지기 조건 확인";
                return $"{id} 준비 · {(b.Revealed(id) ? BossProgress.Hazard(id) : b.DetectionBlock(id))}\n과학 시설에서 탐지기 확인 · 출정은 직접 결정";
            }
            return "탈출 준비 · 선체·엔진·좌석·방어 비축\n과학 시설에서 로켓 확인 · 시민 대피와 탑승은 직접 결정";
        }
    }
}
