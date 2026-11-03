using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.World
{
    public partial class ExpeditionTransport
    {
        // 실제 출정과 UI가 같은 조건을 사용한다. 식량 부족은 경고이며 강제 금지가 아니다.
        public string DepartureBlock(ExpeditionSite destination)
        {
            if (ExpeditionRules.Paused) return ExpeditionRules.PauseReason;
            if (!isActiveAndEnabled) return "비활성 수송 수단";
            if (!CanOperate) return "운행 불가 · 수리/잔해 상태를 확인하세요";
            if (!HasPilot) return "활동 가능한 조종 장수 없음";
            if (IsWithdrawing) return "철수 작업 중";
            if (ReservedCargo > 0) return "화물 적재 대기 중";
            if (PendingCrewOrderCount > 0) return "승무원 지시 대기: " + CrewOrderStatus;
            if (State != ExpeditionState.Home) return "본거지에 있는 수송 수단만 출정 가능";
            var world = WorldMapManager.Instance;
            if (world == null || !world.Unlocked) return "월드맵 잠김";
            if (destination == null) return "목적지를 선택하세요";
            if (!world.Sites.Contains(destination) || !WorldMapManager.SiteRevealed(destination)) return "공개되지 않은 목적지";
            if (destination.Disposition == ConquestDisposition.Abandoned) return "포기한 거점은 재방문 불가";
            if (crew.Count == 0 && destination.Disposition != ConquestDisposition.Annexed
                || Walking && !crew.Any(c => c != null && !c.IsDead)) return "탑승 장수 없음";
            if (Load > Capacity) return $"병력 정원 초과 {Load}/{Capacity}";
            if (CommanderLoad > CommanderCapacity) return $"장수 정원 초과 {CommanderLoad}/{CommanderCapacity}";
            if (Route?.IsRunning == true && (destination != Route.Destination || HasCargo || Route.WaitSeconds > 0))
                return "자동 수송 작업 중 · 중지 후 직접 출정하세요";
            return "";
        }

        public float PlannedRoundTripSeconds(ExpeditionSite destination) => CanOperate
            ? 2 * TravelSecondsTo(destination) / TravelSpeedMultiplier + (Walking ? 0 : ExpeditionRules.DockSeconds) : float.PositiveInfinity;
        public float TravelRemainingSeconds => CanOperate ? Remaining / TravelSpeedMultiplier : float.PositiveInfinity;
        public string DepartureWarning(ExpeditionSite destination)
        {
            var food = RecommendedFood(destination);
            return (AvailableFood < food ? $"보급 {food - AvailableFood} 부족 (권장 {food}/보유 {AvailableFood})" : "")
                + (LeavesHomeUnattended ? (AvailableFood < food ? " · " : "") + "본거지 대응 장수 없음" : "");
        }
        public string TravelWaitReason
        {
            get
            {
                if (State != ExpeditionState.Outbound && State != ExpeditionState.Returning) return "";
                if (!CanOperate) return "이동 중단 · 수송 수단 수리 필요";
                if (Remaining > 0) return "";
                if (State == ExpeditionState.Returning) return HomeHasEnemies()
                    ? "귀환 착륙 대기 · 본거지의 적을 격퇴하세요" : "귀환 착륙 대기 · 착륙 공간/경로를 확보하세요";
                return "현지 접안 대기 · 먼저 도착한 수송 수단이 접안 중";
            }
        }
        public string CargoSummary => $"자원 {cargo.Values.Sum()} · 장비 {EquipmentCargo.Count} · 시신 {BodyLoad} · 핵심물 {BossRewardTransfer.CargoLoad(this)}"
            + (BlueprintCargo ? " · 설계도" : "") + $" · 적재 예약 {ReservedCargo}";
        // 확인 뒤 승무원·부대·보급·목적지가 바뀌면 경고를 다시 확인한다. 저장할 진행 상태는 아니다.
        public string DepartureWarningKey(ExpeditionSite destination) => (destination != null ? destination.GetEntityId().ToString() : "")
            + ":" + DepartureWarning(destination) + ":" + string.Join(",", crew.Where(c => c != null).Select(c => c.PersonalState.id + "/" + c.TroopCount));
    }
}
