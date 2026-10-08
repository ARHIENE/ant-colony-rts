using System.Linq;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 하단 콘솔 가운데(HUD v3): 초상·장비 칸 / 이름·무기·특성·하는 일 / 체력 막대 / 기분·나이·운반량·공격방어 / 욕구 3종 / 주요 기술 3개.
    // 초상 클릭 = 상세 탭 창(전체 기술·포상·연구), 무기 칸 클릭 = 무기 교체.
    public class SelectedUnitPanel : MonoBehaviour
    {
        public static readonly string[] ActivityNames = { "채집", "건설", "농사", "낚시", "제작", "연구", "근접", "원거리", "지휘", "의료", "요리", "근력", "예술" };

        private SelectionManager selection;
        private GameObject panel;
        private Text portrait, header, job, healthText, topSkills;
        private RawImage portraitAnt;
        private RectTransform troopFill, healthFill;
        private readonly Text[] statValues = new Text[3], statNotes = new Text[3], needValues = new Text[4];
        private readonly RectTransform[] needFills = new RectTransform[4]; // 욕구 4종: 배고픔·피로·오락·위생
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
            var rect = MenuTheme.Rect("SelectedUnitPanel", HudConsole.Center);
            MenuTheme.Stretch(rect); rect.offsetMin = new Vector2(12, 8); rect.offsetMax = new Vector2(-12, -14);
            panel = rect.gameObject;
            var por = Well(rect, "Portrait", Vector2.zero, new Vector2(110, 126));
            MenuTheme.InsetScreen(por.gameObject).raycastTarget = true;
            por.GetComponent<Image>().color = MenuTheme.Hex(0x1c261c);
            por.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                FindFirstObjectByType<DetailTabs>()?.Toggle(FindSingleSelectedCommander(selection)));
            por.gameObject.AddComponent<MenuTooltip>().Message = "초상 클릭: 상세·나이·특성·전체 기술·포상·연구";
            portrait = Label(por, "", 21, Vector2.zero, new Vector2(110, 126), MenuTheme.Selection);
            portrait.alignment = TextAnchor.MiddleCenter;
            var ant = MenuTheme.Rect("PortraitAnt", por);
            ant.anchorMin = ant.anchorMax = ant.pivot = new Vector2(.5f, 1);
            ant.sizeDelta = new Vector2(80, 80); ant.anchoredPosition = new Vector2(0, -8);
            portraitAnt = ant.gameObject.AddComponent<RawImage>(); portraitAnt.texture = RosterBar.AntFace; portraitAnt.raycastTarget = false;
            string[] slotNames = { "무기", "방어구", "장신구" };
            string[] objectNames = { "Weapon", "Armor Slot", "Trinket Slot" };
            for (var i = 0; i < 3; i++)
            {
                var slot = Well(rect, objectNames[i], new Vector2(i * 38, -134), new Vector2(34, 38));
                slotTexts[i] = Label(slot, slotNames[i].Substring(0, 1), 12, Vector2.zero, new Vector2(34, 38), MenuTheme.Dim);
                slotTexts[i].alignment = TextAnchor.MiddleCenter;
                slotTips[i] = slot.gameObject.AddComponent<MenuTooltip>();
                slot.GetComponent<Image>().raycastTarget = true;
            }
            rect.Find("Weapon").gameObject.AddComponent<Button>().onClick.AddListener(() => FindSingleSelectedCommander(selection)?.CycleWeapon());
            var screen = MenuTheme.Rect("CommanderInfoScreen", rect); MenuTheme.Stretch(screen); screen.offsetMin = new Vector2(122, 0);
            MenuTheme.InsetScreen(screen.gameObject).raycastTarget = false;
            var info = MenuTheme.Rect("CommanderInfo", screen); MenuTheme.Stretch(info); info.offsetMin = new Vector2(10, 2); info.offsetMax = new Vector2(-10, -2);
            header = RowLabel(info, "", 21, 0, 28);
            job = RowLabel(info, "", 12, 29, 20); job.color = MenuTheme.Selection;
            healthText = RowLabel(info, "", 12, 52, 18);
            var healthTrack = Row(info, "PersonalHealthTrack", 74, 7);
            healthTrack.gameObject.AddComponent<Image>().color = MenuTheme.Well;
            healthFill = Fill(healthTrack, "PersonalHealthFill", MenuTheme.Hp);
            var track = Row(info, "TroopTrack", 84, 4); track.gameObject.AddComponent<Image>().color = MenuTheme.Well;
            troopFill = Fill(track, "TroopFill", MenuTheme.Accent);
            string[] statNames = { "기분", "운반량", "공격 / 방어" };
            for (var i = 0; i < 3; i++)
            {
                var cell = Row(info, "Stat " + i, 94, 48, i / 3f, (i + 1) / 3f);
                RowLabel(cell, statNames[i], 12, 0, 16).color = MenuTheme.Muted;
                statValues[i] = RowLabel(cell, "", 20, 16, 25); statValues[i].font = MenuTheme.NumberFont;
                statNotes[i] = RowLabel(cell, "", 10, 40, 14); statNotes[i].color = MenuTheme.Dim;
            }
            string[] needNames = { "배고픔", "피로", "오락", "위생" };
            Color[] needColors = { MenuTheme.Warning, MenuTheme.Loyal, MenuTheme.Hex(0xb9a2f2), MenuTheme.Hex(0x7cc4d6) };
            for (var i = 0; i < 4; i++)
            {
                var cell = Row(info, "Need " + needNames[i], 152, 28, i / 4f, (i + 1) / 4f);
                RowLabel(cell, needNames[i], 12, 0, 18).color = MenuTheme.Muted;
                needValues[i] = RowLabel(cell, "", 12, 0, 18); needValues[i].alignment = TextAnchor.MiddleRight;
                needValues[i].font = MenuTheme.NumberFont;
                var meter = Row(cell, "Track", 22, 5); meter.gameObject.AddComponent<Image>().color = MenuTheme.Well;
                needFills[i] = Fill(meter, "Fill", needColors[i]);
            }
            topSkills = RowLabel(info, "", 11, 186, 14); topSkills.color = MenuTheme.Muted;
            panel.SetActive(false);
        }

        private static RectTransform Row(Transform parent, string name, float y, float height, float start = 0, float end = 1)
        {
            var r = MenuTheme.Rect(name, parent);
            r.anchorMin = new Vector2(start, 1); r.anchorMax = new Vector2(end, 1); r.pivot = new Vector2(0, 1);
            r.offsetMin = new Vector2(0, -y - height); r.offsetMax = new Vector2(end < 1 ? -8 : 0, -y);
            return r;
        }
        private static Text RowLabel(Transform parent, string text, int size, float y, float height)
        {
            var r = Row(parent, text.Length > 0 ? text : "Text", y, height);
            var t = r.gameObject.AddComponent<Text>(); t.font = MenuTheme.Font; t.fontSize = size;
            t.color = MenuTheme.TextColor; t.text = text; t.raycastTarget = false; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true; t.resizeTextMinSize = 10; t.resizeTextMaxSize = size;
            return t;
        }

        private void LateUpdate()
        {
            if (panel == null) return;
            int count = 0;
            float current = 0f, maximum = 0f;
            float troops = 0f, troopMaximum = 0f;
            bool deployed = false;
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
                    if (unit is CommanderAnt commander)
                    {
                        current += commander.PersonalHealth; maximum += GameBalance.CommanderHealth;
                        if (commander.IsDeployed) { deployed = true; troops += commander.TroopHealth; troopMaximum += commander.CommandLimit; }
                    }
                    else { current += unit.CurrentHealth; maximum += unit.Data.maxHealth; }
                }
            }
            var visible = count == 1 && !BuildScreen.Picking;
            panel.SetActive(visible);
            if (!visible) return;
            healthFill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f, 1f);
            troopFill.parent.gameObject.SetActive(deployed);
            troopFill.anchorMax = new Vector2(troopMaximum > 0f ? Mathf.Clamp01(troops / troopMaximum) : 0f, 1f);
            healthText.text = $"체력 <b>{Mathf.CeilToInt(current)}</b> <color=#968976>/ {Mathf.CeilToInt(maximum)}</color>"
                + (deployed ? $"    병력 <b>{Mathf.CeilToInt(troops)}</b> <color=#968976>/ {Mathf.CeilToInt(troopMaximum)}</color>" : "");

            var c = count == 1 ? first as CommanderAnt : null;
            if (c == null)
            {
                portrait.text = count.ToString(); portraitAnt.gameObject.SetActive(false);
                header.text = count > 1 ? $"<size=17><b><color=#efe7da>선택된 부대 {count}</color></b></size>" : $"<size=17><b><color=#efe7da>{first.name}</color></b></size>";
                job.text = topSkills.text = "";
                for (var i = 0; i < 3; i++) statValues[i].text = statNotes[i].text = "";
                if (count == 1) statValues[2].text = $"{first.AttackDamage:0.#} / {first.Armor:0.#}";
                for (var i = 0; i < 3; i++) { needValues[i].text = ""; needFills[i].anchorMax = new Vector2(0, 1); slotTexts[i].color = MenuTheme.Dim; slotTips[i].Message = ""; }
                return;
            }

            portraitAnt.gameObject.SetActive(true); portraitAnt.color = RosterBar.ColorOf(c);
            portrait.text = "\n\n\n<size=12>" + CommandCard.RoleName(c.Role) + " · 상세 ↗</size>";
            header.text = $"<size=17><b><color=#efe7da>{c.CommanderName}</color></b></size>   {c.WeaponLabel}";
            var activity = CommanderOverhead.Activity(c);
            var skill = c.Talents.Level(c.CurrentActivity);
            job.text = $"하는 일 <color=#efe7da>{(activity.Length > 0 ? activity : "대기")}</color>" + (activity.Length > 0 && !c.IsDeployed ? $" {skill}" : "");

            statValues[0].text = $"{c.Mood:0}";
            statValues[0].color = c.Mood <= 20 ? MenuTheme.DangerInk : c.Mood <= CommanderOverhead.MoodWarning ? MenuTheme.Accent : MenuTheme.TextColor;
            var factor = c.PersonalState.moodFactors.OrderByDescending(f => Mathf.Abs(f.value)).FirstOrDefault();
            statNotes[0].text = factor != null ? $"{factor.reason} {factor.value:+0;-0}" : "";
            statValues[1].text = $"{c.LoadCapacity:0.#}";
            statNotes[1].text = $"근력 {c.Talents.Level(CommanderActivity.Strength)}";
            statValues[2].text = $"{c.AttackDamage:0.#} / {c.Armor:0.#}";
            statNotes[2].text = c.IsDeployed ? "출전 중" : "출전 시";

            float[] needs = { 100f - c.Satiety, c.Fatigue, c.Joy, c.Hygiene };
            for (var i = 0; i < 4; i++)
            {
                needValues[i].text = $"{needs[i]:0}";
                needFills[i].anchorMax = new Vector2(Mathf.Clamp01(needs[i] / 100f), 1f);
            }

            var best = Enumerable.Range(0, CommanderTalents.Count).OrderByDescending(i => c.Talents.Level((CommanderActivity)i)).Take(3)
                .Select(i => {
                    var passion = c.Traits.passions.Exists(p => p.activity == (CommanderActivity)i && p.flame > 0);
                    return $"<color=#{(passion ? "f2a93b" : "efe7da")}>{ActivityNames[i]} <b>{c.Talents.Level((CommanderActivity)i)}</b></color>";
                });
            topSkills.text = "주요 기술   " + string.Join("    ", best);

            var items = new[] { c.Weapon, c.EquippedArmor, c.PersonalState.equipment.Find(e => e.slot == EquipmentSlot.Trinket) };
            for (var i = 0; i < 3; i++)
            {
                slotTexts[i].color = items[i] != null ? MenuTheme.TextColor : MenuTheme.Dim;
                slotTips[i].Message = (items[i] != null ? items[i].Label : "비어 있음") + (i == 0 ? "\n클릭: 가진 무기로 교체(병력 유지)" : "");
            }
        }

        private static RectTransform Fill(RectTransform track, string name, Color color)
        {
            var fill = MenuTheme.Rect(name, track); MenuTheme.Stretch(fill);
            var image = fill.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
            return fill;
        }

        private static RectTransform Well(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = MenuTheme.Rect(name, parent);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>(); image.color = MenuTheme.FrameButton; image.raycastTarget = false;
            if (name != "Portrait") rect.gameObject.AddComponent<Outline>().effectColor = MenuTheme.ScreenRim;
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
