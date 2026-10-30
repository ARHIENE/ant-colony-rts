using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void RocketActivation(AirshipYard yard)
        {
            if (yard.ModificationsLocked) { MenuTheme.Text(content, "내장 전원 가동 · 확장/이동/철거 잠금 · 준비 완료 후에도 공세 지속", 13, 40); return; }
            var preview = yard.ActivationPreview;
            MenuTheme.Text(content, yard.FuelLoaded ? "15일분 연료 전량 적재 완료" : $"잠정 연료 규격: 정제유 하루 {AirshipYard.FuelUnitsPerDay} · 15일분 전량 적재 필요", 13, 40);
            MenuTheme.Button(content, "15일분 연료 적재 (잠정: 정제유 15)", () => {
                if (!yard.TryLoadFuel(AirshipYard.FuelResource, AirshipYard.FuelUnitsPerDay)) ToastManager.Show("연료 재고와 로켓 상태를 확인하세요.");
                ScienceFacilities();
            }).interactable = !yard.FuelLoaded && yard.Phase == RocketPhase.Construction;
            MenuTheme.Text(content, preview == null ? "가동 불가: 전 지역 소유물·이동 화물 재산/인구 집계 연결 대기"
                : $"재산 현재 {preview.currentWealth:0.##} / 직전 6일 평균 {preview.averageWealth:0.##} → 고정 입력 {preview.Wealth:0.##}\n인구 현재 {preview.currentPopulation:0.##} / 평균 {preview.averagePopulation:0.##} → 고정 입력 {preview.Population:0.##}", 13, 55);
            if (!RocketCampaign.AssaultAvailable) MenuTheme.Text(content, "가동 불가: 예상 강도·최종 공세 생성기 연결 대기", 13, 35);
            else if (preview != null) MenuTheme.Text(content, $"예상 최종 공세 첫 파동 {AntColony.World.ColonyInvasion.PredictedFirstWave(preview)}마리 (잠정: 인구 10·재산 100당 +1, 파동마다 증가·상한 적용)", 13, 35);
            MenuTheme.Text(content, "전 지역 재산에는 장식·식량·생활 시설·외부 화물이 포함됩니다. 가동 후 좌석을 늘릴 수 없으므로 구할 인원(장수+병력+시민)을 먼저 정하세요. 준비 중 인구가 늘어도 공세 강도는 고정됩니다.", 12, 72);
            MenuTheme.Text(content, $"로켓 핵심물: {CampaignResearch.Instance?.BossProgress.Home(AntColony.Boss.BossId.D) ?? 0}개 · 가동 시1개 소비. 파괴 후 재건에는 D 재공략 또는 핵심물 복구 제작이 필요합니다. 부분 손상은 타이머를 보존하지만 실제 파괴는15일을 다시 시작합니다.", 12, 72);
            MenuTheme.Button(content, "로켓 가동 — 15일 준비 시작", () => {
                FinalBattleReserveWarning(() => {
                    var current = yard.ActivationPreview;
                    if (current == null || !yard.TryActivate(current.currentWealth, current.currentPopulation)) ToastManager.Show("연료·부품·경제 집계·공세 연결 상태를 확인하세요.");
                    ScienceFacilities();
                }, yard.ActivationReserves);
            }).interactable = yard.Ready && yard.FuelLoaded && preview != null && RocketCampaign.AssaultAvailable
                && CampaignResearch.Instance?.BossProgress.Home(AntColony.Boss.BossId.D) > 0;
        }
        private void ConfirmRocketBoarding(AirshipYard yard, CommanderAnt commander)
        {
            Screen("로켓 탑승", commander.CommanderName);
            MenuTheme.Text(content, CampaignOutcome.Preview(yard.Passengers, commander) + $"\n추가 탑승 예정: 장수 1명 + 병력 {commander.TroopCount}명 · 빈 좌석 {yard.Capacity - yard.OccupiedSeats}", 13, 70);
            MenuTheme.Button(content, "탑승 확정", () => { if (!yard.TryBoard(commander)) ToastManager.Show("15일 준비 완료·좌석·거리·장수 상태를 확인하세요."); ScienceFacilities(); });
            MenuTheme.Button(content, "돌아가기", ScienceFacilities);
        }
        private void ConfirmRocketLaunch(AirshipYard yard)
        {
            Screen("로켓 발사", "이번 게임을 종료합니다");
            MenuTheme.Text(content, CampaignOutcome.Preview(yard.Passengers), 13, 70);
            MenuTheme.Button(content, "발사 확정", () => { if (!yard.TryDepart()) ToastManager.Show("탑승자와 로켓 상태를 확인하세요."); Resume(); });
            MenuTheme.Button(content, "돌아가기", ScienceFacilities);
        }
    }
}
