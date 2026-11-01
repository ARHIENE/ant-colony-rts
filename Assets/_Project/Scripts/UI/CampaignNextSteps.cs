using AntColony.Core;


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

    }
}
