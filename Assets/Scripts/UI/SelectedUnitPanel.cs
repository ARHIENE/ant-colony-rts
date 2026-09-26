using System.Linq;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 하단 콘솔 가운데: 선택 장수 카드(초상·장비 칸 / 이름·보직·특성 / 병력 막대 / 능력치 4칸 / 기술 9종).
    public class SelectedUnitPanel : MonoBehaviour
    {
        private static readonly string[] ActivityNames = { "채집", "건설", "농사", "낚시", "제작", "연구", "근접", "원거리", "지휘" };

        private SelectionManager selection;
        private GameObject panel;
        private Text portrait, header, troopsText, emptyText;
        private RectTransform troopFill;
        private readonly Text[] statValues = new Text[4], statNotes = new Text[4], skillTexts = new Text[9];
        private readonly MenuTooltip[] slotTips = new MenuTooltip[3];
        private readonly Text[] slotTexts = new Text[3];

        // 현재 선택에서 유효한 유닛이 정확히 한 명이고 장수이면 반환한다. 행동 시점에 캐시 없이 조회한다.
        public static CommanderAnt FindSingleSelectedCommander(SelectionManager selection)
        {
            if (selection == null) return null;
            AntUnitBase found = null;
            foreach (var selectable in selection.GetSelectedObjects())
            {
                if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsSelected) continue;
                var unit = selectable.GetComponent<AntUnitBase>();
                if (unit == null || !unit.isActiveAndEnabled || unit.IsDead || unit.Data == null) continue;
                if (found != null) return null;
                found = unit;
            }
            return found as CommanderAnt;
        }

        private void Start()
        {
            selection = FindFirstObjectByType<SelectionManager>();
            emptyText = Label(HudConsole.Center, "장수를 선택하면 정보와 명령이 여기에 표시됩니다.", 14, new Vector2(24, -24), new Vector2(600, 24), MenuTheme.Dim);

            // 가운데 판의 좌우 6px 겹침을 피해 안쪽에 놓는다(검사: 커맨드 카드와 겹치지 않음).
            var rect = MenuTheme.Rect("SelectedUnitPanel", HudConsole.Center);
            MenuTheme.Stretch(rect); rect.offsetMin = new Vector2(10, 10); rect.offsetMax = new Vector2(-12, -10);
            panel = rect.gameObject;

            var por = Well(rect, "Portrait", new Vector2(0, 0), new Vector2(104, 136));
            portrait = Label(por, "", 28, Vector2.zero, new Vector2(104, 136), MenuTheme.Dim);
            portrait.alignment = TextAnchor.MiddleCenter;
            string[] slotNames = { "무기", "방어구", "장신구" };
            for (var i = 0; i < 3; i++)
            {
                var slot = Well(rect, "Slot " + slotNames[i], new Vector2(i * 36, -142), new Vector2(32, 32));
                slotTexts[i] = Label(slot, slotNames[i].Substring(0, 1), 12, Vector2.zero, new Vector2(32, 32), MenuTheme.Dim);
                slotTexts[i].alignment = TextAnchor.MiddleCenter;
                slotTips[i] = slot.gameObject.AddComponent<MenuTooltip>();
                slot.GetComponent<Image>().raycastTarget = true;
            }

            const float x = 116f;
            header = Label(rect, "", 14, new Vector2(x, 0), new Vector2(690, 24), MenuTheme.Muted);
            header.supportRichText = true;
            troopsText = Label(rect, "", 13, new Vector2(x, -28), new Vector2(690, 18), MenuTheme.Dim);
            var track = Well(rect, "TroopTrack", new Vector2(x, -48), new Vector2(690, 12));
            troopFill = MenuTheme.Rect("TroopFill", track);
            MenuTheme.Stretch(troopFill);
            var fill = troopFill.gameObject.AddComponent<Image>(); fill.color = MenuTheme.Hp; fill.raycastTarget = false;

            string[] statNames = { "공격", "방어", "기분", "충성심" };
            for (var i = 0; i < 4; i++)
            {
                var sx = x + i * 174f;
                Label(rect, statNames[i], 12, new Vector2(sx, -68), new Vector2(170, 16), MenuTheme.Dim);
                statValues[i] = Label(rect, "", 22, new Vector2(sx, -84), new Vector2(170, 28), MenuTheme.TextColor);
                statValues[i].font = MenuTheme.NumberFont;
                statNotes[i] = Label(rect, "", 11, new Vector2(sx, -112), new Vector2(170, 16), MenuTheme.Dim);
            }
            for (var i = 0; i < 9; i++)
            {
                var cell = Well(rect, "Skill " + ActivityNames[i], new Vector2(x + i * 77f, -146), new Vector2(73, 30));
                skillTexts[i] = Label(cell, "", 12, new Vector2(6, 0), new Vector2(64, 30), MenuTheme.Muted);
                skillTexts[i].alignment = TextAnchor.MiddleLeft; skillTexts[i].supportRichText = true;
            }
            panel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (panel == null) return;
            int count = 0;
            float current = 0f, maximum = 0f;
            AntUnitBase first = null;
            if (selection != null)
            {
                foreach (var selectable in selection.GetSelectedObjects())
                {
                    if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsSelected) continue;
                    var unit = selectable.GetComponent<AntUnitBase>();
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDead || unit.Data == null) continue;
                    if (first == null) first = unit;
                    count++;
                    current += unit.CurrentHealth;
                    maximum += unit is CommanderAnt commander ? commander.CommandLimit : unit.Data.maxHealth;
                }
            }
            var visible = count > 0 && !BuildScreen.Picking;
            panel.SetActive(visible);
            emptyText.gameObject.SetActive(count == 0 && !BuildScreen.Picking);
            if (!visible) return;
            troopFill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f, 1f);
            troopsText.text = $"병력  <b>{Mathf.CeilToInt(current)}</b> / {Mathf.CeilToInt(maximum)}{(count == 1 && first is CommanderAnt ? " 지휘한도" : "")}";
            troopsText.supportRichText = true;

            var c = count == 1 ? first as CommanderAnt : null;
            if (c == null)
            {
                portrait.text = count.ToString();
                header.text = count > 1 ? $"<b><color=#f2a93b>선택된 부대 {count}</color></b>" : $"<b><color=#f2a93b>{first.name}</color></b>";
                for (var i = 0; i < 4; i++) statValues[i].text = statNotes[i].text = "";
                if (count == 1) { statValues[0].text = $"{first.AttackDamage:0.#}"; statValues[1].text = $"{first.Armor:0.#}"; }
                for (var i = 0; i < 9; i++) skillTexts[i].text = "";
                for (var i = 0; i < 3; i++) { slotTexts[i].color = MenuTheme.Dim; slotTips[i].Message = ""; }
                return;
            }

            portrait.text = CommandCard.RoleName(c.Role);
            var traits = string.Join(" · ", c.Traits.values.Select(t => t.ToString()));
            header.text = $"<b><color=#f2a93b>{c.CommanderName}</color></b>   {c.WeaponLabel} · {CommandCard.RoleName(c.Role)}"
                + (traits.Length > 0 ? $"   <color=#968976>|</color>  {traits}" : "");
            statValues[0].text = $"{c.AttackDamage:0.#}";
            statNotes[0].text = c.Weapon != null ? c.Weapon.Label : "맨 큰턱";
            statValues[1].text = $"{c.Armor:0.#}";
            statNotes[1].text = c.EquippedArmor != null ? c.EquippedArmor.Label : "방어구 없음";
            statValues[2].text = $"{c.Mood:0}";
            var factor = c.PersonalState.moodFactors.OrderByDescending(f => Mathf.Abs(f.value)).FirstOrDefault();
            statNotes[2].text = factor != null ? $"{factor.reason} {factor.value:+0;-0}" : "";
            statValues[3].text = c.Traits.Loyalty.ToString();
            statValues[3].color = GameMenuController.LoyaltyColor(c.Traits.Loyalty);
            statNotes[3].text = c.Traits.loyaltyReasons.Count > 0 ? c.Traits.loyaltyReasons[c.Traits.loyaltyReasons.Count - 1] : "";

            for (var i = 0; i < 9; i++)
            {
                var activity = (CommanderActivity)i;
                var passion = c.Traits.passions.Exists(p => p.activity == activity && p.flame > 0);
                skillTexts[i].text = $"{ActivityNames[i]} <b>{c.Talents.Level(activity)}</b>";
                skillTexts[i].color = passion ? MenuTheme.Accent : MenuTheme.Muted;
            }
            var items = new[] { c.Weapon, c.EquippedArmor, c.PersonalState.equipment.Find(e => e.slot == EquipmentSlot.Trinket) };
            for (var i = 0; i < 3; i++)
            {
                slotTexts[i].color = items[i] != null ? MenuTheme.TextColor : MenuTheme.Dim;
                slotTips[i].Message = items[i] != null ? items[i].Label : "비어 있음";
            }
        }

        private static RectTransform Well(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = MenuTheme.Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>(); image.color = MenuTheme.Well; image.raycastTarget = false;
            return rect;
        }

        private static Text Label(Transform parent, string value, int size, Vector2 position, Vector2 box, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = MenuTheme.Font; text.fontSize = size; text.color = color; text.text = value;
            text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow; text.verticalOverflow = VerticalWrapMode.Overflow;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = box;
            return text;
        }
    }
}
