using System.Linq;
using AntColony.Units;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 장비 화면(2026-10-15): 장수의 실제 슬롯(무기·갑옷·장신구)과 본거지 보관함 인스턴스. 장착·교체·해제는 EquipmentInventory가 처리한다.
    public sealed partial class GameMenuController
    {
        private static readonly string[] SlotNames = { "무기", "갑옷", "장신구" };

        private static string EquipmentEffect(EquipmentItem e) => e.slot switch
        {
            EquipmentSlot.Weapon => e.weapon switch
            {
                WeaponKind.Mandible => "근접 공격(근접 무기)", WeaponKind.AcidSprayer => "원거리 산 분사",
                WeaponKind.WoodenSword => "나무칼 · 근접 공격", WeaponKind.ShortBow => "단궁 · 사거리 12m · 공격 간격 2초 · 탄약 없음",
                WeaponKind.Shield => "방어 위주 전투", _ => "지휘 신호기 지원(지휘 범위 +" + e.quality + ")"
            },
            EquipmentSlot.Armor => e.armor == ArmorKind.Wings ? "비행 가능(비행 장비)" : "피해 감소(보호 갑옷)",
            _ => e.effect switch
            {
                TrinketEffect.Move => "이동 속도", TrinketEffect.Gather => "채집 속도",
                TrinketEffect.Mood => "기분", _ => "지휘 한도"
            } + " 보너스"
        };

        public void ShowEquipment(CommanderAnt c)
        {
            var inventory = EquipmentInventory.Instance;
            if (c == null || c.IsDead || inventory == null) { Resume(); return; }
            var window = V5Window($"{c.CommanderName} / 장비", "현재 장착 → 가능한 교체 → 불가 이유 · 반환 장비는 보관함에 들어온 뒤 직접 장착");
            L.Button(window, "Back To Commander", "장수 상세", 20, 677, 160, 30, () => Details(c));

            content = V5Section(window, "장착 슬롯", 20, 104, 420, 544);
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var item = c.PersonalState.equipment.Find(e => e.slot == slot);
                MenuTheme.Text(content, $"{SlotNames[(int)slot]}: " + (item == null ? "비어 있음" : $"{item.Label} — {EquipmentEffect(item)}"), 16, 48);
                if (item == null) continue;
                var block = inventory.ChangeBlock(c, null, item);
                MenuTheme.Button(content, "해제(보관함으로)", () => { inventory.Unequip(c, item); ShowEquipment(c); }, block).interactable = block == "";
                if (block != "") MenuTheme.Text(content, "<color=#f2c94c>" + block + "</color>", 14, 26);
            }

            content = V5Section(window, $"본거지 보관함 {inventory.Items.Count}/{EquipmentInventory.Capacity}", 454, 104, 626, 544);
            if (inventory.Items.Count == 0) MenuTheme.Text(content, "보관 중인 장비가 없습니다.", 15, 30);
            foreach (var item in inventory.Items.OrderBy(e => e.slot).ThenByDescending(e => e.quality).ToArray())
            {
                var old = c.PersonalState.equipment.Find(e => e.slot == item.slot);
                var block = inventory.ChangeBlock(c, item, old);
                var label = $"{SlotNames[(int)item.slot]} · {item.Label} — {EquipmentEffect(item)}" + (old != null ? $" (교체: {old.Label}은 보관함으로)" : "");
                MenuTheme.Button(content, label, () => { inventory.Equip(c, item); ShowEquipment(c); }, block == "" ? "장착" : block).interactable = block == "";
                if (block != "") MenuTheme.Text(content, "<color=#f2c94c>" + block + "</color>", 14, 24);
            }
            refreshDutyScreen = null;
            V5Fit(window);
        }
    }
}
