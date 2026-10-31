using System.Linq;
using AntColony.Buildings;
using AntColony.Core;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void CivilianDestination(int id)
        {
            var pop = ColonyPopulation.Instance; if (pop == null) return;
            Screen("시민 거처·승객 예약", "그룹 #" + id);
            MenuTheme.Text(content, "임시 거처는 재개발 대상·추가 주거 정원이 아닙니다. 도착 뒤 보호되지만 주거 민심 불이익이 절반 남습니다. 빈 주거로 실제 입주하면 해당 불이익이 사라지고 기존 속도로 민심이 회복됩니다.", 13, 85);
            MenuTheme.Button(content, "본거지 임시 거처로 대피 (최대50명)", () => {
                if (pop.BeginTemporaryShelter(id) == 0) ToastManager.Show("이동 중·예약·경로를 확인하세요."); CivicServices();
            });
            foreach (var home in Housing.All.Where(h => h.isActiveAndEnabled && pop.HousingRoom(h) > 0))
                MenuTheme.Button(content, $"입주: {home.name} · 빈 자리 {pop.HousingRoom(home)}", () => {
                    if (pop.BeginRehousing(id, home) == 0) ToastManager.Show("빈 자리·예약·이동 경로를 확인하세요."); CivicServices();
                });
            foreach (var yard in SceneObjects.All<AirshipYard>().Where(y => !y.IsDead))
            {
                if (yard.ReservationOverflow > 0) MenuTheme.Text(content, yard.ReservationWarning, 13, 65).color = MenuTheme.Danger;
                MenuTheme.Button(content, $"{yard.name} ({yard.Position.x:0},{yard.Position.z:0}) 예약·이동 · 빈 자리 {yard.UnreservedSeats}", () => {
                    if (yard.EvacuateAndReserve(id) == 0) ToastManager.Show("이동 중·다른 예약·빈 자리·경로를 확인하세요."); CivicServices();
                });
                if (yard.ReservedGroups.Contains(id)) MenuTheme.Button(content, "이 로켓의 시민 예약 취소 (이동은 유지)", () => { yard.CancelGroupReservation(id); CivicServices(); });
            }
            MenuTheme.Button(content, "돌아가기", CivicServices);
        }
        private void RocketReservations(AirshipYard yard)
        {
            if (yard.ReservationOverflow > 0) MenuTheme.Text(content, yard.ReservationWarning, 13, 65).color = MenuTheme.Danger;
            MenuTheme.Text(content, $"예약 {yard.ReservedSeats}석 · 미예약 {yard.UnreservedSeats}석 · 다른 발사대와 중복 예약 불가. 예약은 출발·탑승을 자동 실행하지 않습니다. 예약 취소는 이동 지시를 취소하지 않습니다.", 12, 65);
            foreach (var c in SortedCommanders().Where(c => yard.ReservedCommanders.Contains(c.PersonalState.id)))
                MenuTheme.Button(content, "장수 예약 취소: " + c.CommanderName, () => { yard.CancelReservation(c.PersonalState.id); ScienceFacilities(); });
            foreach (var c in SortedCommanders().Where(c => !c.IsEmbarked && !AirshipYard.IsReserved(c.PersonalState.id)))
                MenuTheme.Button(content, "장수 좌석 예약: " + c.CommanderName, () => { if (!yard.Reserve(c)) ToastManager.Show("장수 상태·좌석을 확인하세요."); ScienceFacilities(); });
            foreach (var id in yard.ReservedGroups.ToArray())
                MenuTheme.Button(content, "시민 그룹 예약 취소: #" + id, () => { yard.CancelGroupReservation(id); ScienceFacilities(); });
        }
    }
}
