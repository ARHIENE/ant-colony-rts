using System.Linq;
using AntColony.Buildings;
using AntColony.Core;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void CivilianDestination(int id)
        { Roster(); }
        private void RocketReservations(AirshipYard yard)
        {
            if (yard.ReservationOverflow > 0) MenuTheme.Text(content, yard.ReservationWarning, 13, 65).color = MenuTheme.Danger;
            MenuTheme.Text(content, $"예약 {yard.ReservedSeats}석 · 미예약 {yard.UnreservedSeats}석 · 다른 발사대와 중복 예약 불가. 예약은 출발·탑승을 자동 실행하지 않습니다. 예약 취소는 이동 지시를 취소하지 않습니다.", 12, 65);
            foreach (var c in SortedCommanders().Where(c => yard.ReservedCommanders.Contains(c.PersonalState.id)))
                MenuTheme.Button(content, "장수 예약 취소: " + c.CommanderName, () => { yard.CancelReservation(c.PersonalState.id); ScienceFacilities(); });
            foreach (var c in SortedCommanders().Where(c => !c.IsEmbarked && !AirshipYard.IsReserved(c.PersonalState.id)))
                MenuTheme.Button(content, "장수 좌석 예약: " + c.CommanderName, () => { if (!yard.Reserve(c)) ToastManager.Show("장수 상태·좌석을 확인하세요."); ScienceFacilities(); });

        }
    }
}
