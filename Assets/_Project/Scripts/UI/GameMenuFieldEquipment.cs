using System.Linq;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;

namespace AntColony.UI
{
    public sealed partial class GameMenuController
    {
        private void FieldEquipment(CommanderAnt c)
        {
            MenuTheme.Text(content, "현지 직접 전달: 같은 원정대의 화물함 3m 안, 두 장수 간 3m 안에서만 가능합니다. 전투·작업·운반·철수 중에는 불가. 추가 작업시간 없이 즉시 교환하며 이동 예약은 만들지 않습니다.", 13, 100);
            var ship = WorldMapManager.Instance?.Transports.FirstOrDefault(s => s != null && s.Crew.Contains(c));
            if (ship == null) return;
            foreach (var item in ship.EquipmentCargo.ToArray())
                MenuTheme.Button(content, "현지 화물 장착/교환: " + item.Label, () => {
                    if (!ship.EquipFromCargo(c, item)) ToastManager.Show("화물함 거리·상태·착륙 가능 여부를 확인하세요."); ShowEquipment(c);
                }).interactable = ship.FieldEquipmentReady(c);
            foreach (var other in ship.Crew.Where(o => o != null && o != c))
                foreach (var item in c.PersonalState.equipment.ToArray())
                    MenuTheme.Button(content, other.CommanderName + "에게 전달/동일 슬롯 교환: " + item.Label, () => {
                        if (!ship.ExchangeEquipment(c, other, item)) ToastManager.Show("양쪽 장수와 화물함의 거리·상태를 확인하세요."); ShowEquipment(c);
                    }).interactable = ship.FieldEquipmentReady(c) && ship.FieldEquipmentReady(other);
        }
    }
}
