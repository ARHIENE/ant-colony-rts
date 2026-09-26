using System;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // 디자인 「장수 관리」: 왼쪽 740 목록(필터·열 머리·합계) + 오른쪽 선택 장수 상세.
    public sealed partial class GameMenuController
    {
        private static readonly string[] SkillNames = { "채집", "건설", "농사", "낚시", "제작", "연구", "근접", "원거리", "지휘" };
        private static readonly string[] FilterNames = { "전체", "대기", "작업 중", "원정 중" };
        private int rosterFilter;
        private string rosterSearch = "";

        public void Roster() => RosterScreen(null);
        public void Details(CommanderAnt c) => RosterScreen(c);

        private static int Category(CommanderAnt c) => c.IsAwayFromHome || c.IsDeparting ? 3 : c.CanStartConstruction ? 1 : 2;
        private static string Status(CommanderAnt c) => c.IsDeparting ? "이탈 중" : c.IsCaptive ? "포로" : c.Garrison != null ? "주둔"
            : c.Transport != null ? "원정 중" : !c.HasTroops ? "쓰러짐" : c.CanStartConstruction ? "대기" : "작업 중";
        private static Color MoodColor(float mood) => mood < 35 ? MenuTheme.Danger : mood < 50 ? MenuTheme.HpMid : MenuTheme.Hp;

        private void RosterScreen(CommanderAnt selected)
        {
            var all = SortedCommanders();
            var f = Frame(selected != null ? selected.CommanderName : "Commanders");
            var tab = L.Plate(f, "RosterTab", 48, 47, 200, 34);
            L.Label(tab, $"<b>장수 관리</b>   <color=#968976>{all.Length}명</color>", 14, 16, 0, 180, 34);
            var p = L.Plate(f, "RosterPanel", 48, 80, 1344, 676);

            for (var i = 0; i < FilterNames.Length; i++)
            {
                var index = i;
                var count = i == 0 ? all.Length : all.Count(c => Category(c) == i);
                var button = L.Button(p, "Filter " + FilterNames[i], $"{FilterNames[i]}  <color=#968976>{count}</color>", 12 + i * 100, 11, 94, 30,
                    () => { rosterFilter = index; RosterScreen(selected); }, null, false, 13);
                if (i == rosterFilter) button.GetComponent<Outline>().effectColor = MenuTheme.Accent;
            }
            var search = MenuTheme.Input(p, rosterSearch);
            L.Place((RectTransform)search.transform, 1344 - 12 - 240, 11, 240, 30);
            search.contentType = InputField.ContentType.Standard; search.characterLimit = 20; search.textComponent.fontSize = 13;
            search.onEndEdit.AddListener(v => { rosterSearch = v; RosterScreen(selected); });
            search.gameObject.AddComponent<MenuTooltip>().Message = "이름 검색: 입력 후 Enter";
            L.Line(p, 0, 52, 1344);

            var shown = all.Where(c => (rosterFilter == 0 || Category(c) == rosterFilter)
                && (rosterSearch.Length == 0 || c.CommanderName.IndexOf(rosterSearch, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            if (selected == null) selected = shown.FirstOrDefault();
            string[] heads = { "이름", "무기 (역할)", "병력", "기분", "충성심", "상태" };
            float[] cols = { 12, 162, 312, 472, 552, 632 };
            for (var i = 0; i < heads.Length; i++) L.Label(p, heads[i], 12, cols[i], 58, 140, 22, MenuTheme.Dim);
            var list = L.List(p, 6, 82, 728, 560, 4);
            foreach (var c in shown)
            {
                var captured = c;
                var row = L.Button(list, "Commander " + c.CommanderName, "", 0, 0, 0, 0, () => RosterScreen(captured));
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
                if (c == selected) row.GetComponent<Outline>().effectColor = MenuTheme.Accent;
                L.Label(row.transform, c.CommanderName, 14, cols[0] - 6, 0, 150, 40, LoyaltyColor(c.Traits.Loyalty), bold: true);
                L.Label(row.transform, $"{c.WeaponLabel} <color=#968976>· {CommandCard.RoleName(c.Role)}{(c.IsFlying ? " · 비행" : "")}</color>", 12, cols[1] - 6, 0, 150, 40, MenuTheme.Muted);
                L.Label(row.transform, $"<b>{c.TroopCount}</b><color=#968976>/{c.CommandLimit}</color>", 13, cols[2] - 6, 0, 60, 40);
                L.Meter(row.transform, cols[2] + 54, 17, 90, 6, c.CommandLimit > 0 ? (float)c.TroopCount / c.CommandLimit : 0, MenuTheme.Hp);
                L.Label(row.transform, $"{c.Mood:0}", 14, cols[3] - 6, 0, 70, 40, MoodColor(c.Mood));
                L.Label(row.transform, c.Traits.Loyalty.ToString(), 14, cols[4] - 6, 0, 70, 40, LoyaltyColor(c.Traits.Loyalty));
                L.Label(row.transform, Status(c), 12, cols[5] - 6, 0, 100, 40, MenuTheme.Muted);
            }
            L.Line(p, 0, 646, 740);
            if (all.Length > 0)
                L.Label(p, $"총 병력 <b>{all.Sum(c => c.TroopCount)}</b> / {all.Sum(c => c.CommandLimit)}     평균 기분 <b>{all.Average(c => c.Mood):0}</b>     평균 충성심 <b>{all.Average(c => c.Traits.Loyalty):0}</b>",
                    12, 12, 650, 716, 22, MenuTheme.Muted);
            L.Box(p, "Divider", 740, 53, 1, 623, MenuTheme.Line);

            content = L.List(p, 754, 62, 580, 604, 10);
            if (selected == null) MenuTheme.Text(content, "조건에 맞는 장수가 없습니다.", 14, 40).color = MenuTheme.Dim;
            else CommanderDetail(selected);
            L.Button(f, "Back", "닫기   Esc", 1392 - 110, 764, 110, 34, Back);
        }

        private void CommanderDetail(CommanderAnt c)
        {
            var head = L.Cell(content, "DetailHead", 108);
            var portrait = L.Well(head, "Portrait", 0, 0, 96, 96);
            L.Label(portrait, CommandCard.RoleName(c.Role), 20, 0, 0, 96, 96, MenuTheme.Dim, TextAnchor.MiddleCenter);
            L.Label(head, c.CommanderName, 20, 108, 0, 460, 30, MenuTheme.Accent, bold: true);
            L.Label(head, $"{c.WeaponLabel} · {CommandCard.RoleName(c.Role)} · {Status(c)}", 13, 108, 30, 460, 20, MenuTheme.Muted);
            L.Label(head, string.Join(" · ", c.Traits.values.Select(t => t.ToString())), 12, 108, 50, 460, 20, MenuTheme.Dim);
            L.Label(head, $"병력 <b>{c.TroopCount}</b> / {c.CommandLimit} 지휘한도    공격 <b>{c.AttackDamage:0.#}</b>    방어 <b>{c.Armor:0.#}</b>    연구 공격 {c.LabAttackLevel} · 방어 {c.LabArmorLevel}", 12, 108, 70, 460, 20);
            L.Meter(head, 108, 94, 460, 8, c.CommandLimit > 0 ? (float)c.TroopCount / c.CommandLimit : 0, MenuTheme.Hp);

            var factors = c.PersonalState.moodFactors;
            var mood = L.Cell(content, "Mood", 52 + factors.Count * 18);
            L.Label(mood, $"<b>기분</b>   <size=18><b>{c.Mood:0}</b></size> / 100   <color=#968976>경고선 35 · {c.PersonalState.mentalBreak}</color>", 13, 0, 0, 570, 26);
            L.Meter(mood, 0, 30, 570, 8, c.Mood / 100f, MoodColor(c.Mood));
            for (var i = 0; i < factors.Count; i++)
                L.Label(mood, $"{factors[i].reason}  <b>{factors[i].value:+0;-0;0}</b>  <color=#968976>{factors[i].remaining:0}초</color>", 12, 0, 46 + i * 18, 570, 18, MenuTheme.Muted);

            var reasons = c.Traits.loyaltyReasons.Skip(Math.Max(0, c.Traits.loyaltyReasons.Count - 3)).ToArray();
            var loyal = L.Cell(content, "Loyalty", 52 + reasons.Length * 18);
            L.Label(loyal, $"<b>충성심</b>   <size=18><b>{c.Traits.Loyalty}</b></size> / 100   <color=#968976>경고선 30 · 위험 15</color>", 13, 0, 0, 570, 26);
            L.Meter(loyal, 0, 30, 570, 8, c.Traits.Loyalty / 100f, LoyaltyColor(c.Traits.Loyalty));
            for (var i = 0; i < reasons.Length; i++) L.Label(loyal, reasons[i], 12, 0, 46 + i * 18, 570, 18, MenuTheme.Muted);

            var skills = L.Cell(content, "Skills", 26 + 3 * 46);
            L.Label(skills, "<b>기술</b>  <color=#968976>0~20 · ★ 열정</color>", 13, 0, 0, 570, 22);
            foreach (CommanderActivity skill in Enum.GetValues(typeof(CommanderActivity)))
            {
                var i = (int)skill; var level = c.Talents.Level(skill);
                var x = i % 3 * 192f; var y = 26 + i / 3 * 46f;
                var cell = L.Box(skills, "Skill " + skill, x, y, 186, 42, MenuTheme.Plate2);
                L.Label(cell, $"{SkillNames[i]} <color=#f2a93b>{new string('★', c.Traits.Flame(skill))}</color>", 12, 8, 2, 120, 20, MenuTheme.Muted);
                L.Label(cell, level.ToString(), 16, 130, 2, 48, 20, align: TextAnchor.MiddleRight, bold: true);
                var progress = level >= CommanderTalents.MaxLevel ? 1f : c.Talents.Xp(skill) / CommanderTalents.Required(level);
                L.Meter(cell, 8, 28, 170, 5, progress, MenuTheme.Accent);
            }

            var injuries = c.PersonalState.injuries;
            var body = L.Cell(content, "Injuries", 24 + Math.Max(1, injuries.Count) * 18);
            L.Label(body, "<b>부상 부위</b>", 13, 0, 0, 570, 22);
            if (injuries.Count == 0) L.Label(body, "이상 없음", 12, 0, 22, 570, 18, MenuTheme.Muted);
            for (var i = 0; i < injuries.Count; i++)
                L.Label(body, $"{injuries[i].part} · {injuries[i].severity}  <color=#968976>{injuries[i].remaining:0}초 뒤 회복</color>", 12, 0, 22 + i * 18, 570, 18, MenuTheme.DangerInk);
            if (c.PersonalState.infected)
                MenuTheme.Text(content, $"곰팡이 감염 · 치료 진행 {c.PersonalState.moldTreatment:0}/60 · 20초마다 병력 -1", 13, 22).color = MenuTheme.DangerInk;

            var equipment = L.Cell(content, "Equipment", 70);
            L.Label(equipment, "<b>장비</b>", 13, 0, 0, 570, 22);
            string[] slotNames = { "무기", "갑옷", "장신구" };
            for (var i = 0; i < 3; i++)
            {
                var item = c.PersonalState.equipment.Find(e => (int)e.slot == i);
                var box = L.Box(equipment, "Slot " + slotNames[i], i * 192f, 24, 186, 44, MenuTheme.Plate2);
                L.Label(box, slotNames[i], 11, 8, 2, 170, 18, MenuTheme.Dim);
                L.Label(box, item != null ? item.Label : "비어 있음", 13, 8, 20, 170, 22, item != null ? MenuTheme.TextColor : MenuTheme.Dim);
            }

            var relations = c.PersonalState.relations;
            var faction = c.Faction();
            var social = L.Cell(content, "Relations", 44 + relations.Count * 18);
            L.Label(social, "<b>관계</b>", 13, 0, 0, 570, 22);
            for (var i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                var other = SortedCommanders().FirstOrDefault(o => o.PersonalState.id == relation.otherId);
                var label = relation.spouse ? "배우자" : !relation.family && relation.value >= 70 ? "연인" : relation.value >= 40 ? "친구" : relation.value <= -40 ? "라이벌" : "지인";
                L.Label(social, $"{label}  <b>{other?.CommanderName ?? "떠난 장수"}</b>  <color=#968976>{relation.value:0}</color>", 12, 0, 22 + i * 18, 570, 18, MenuTheme.Muted);
            }
            L.Label(social, "파벌: " + (faction.Count == 0 ? "없음" : string.Join(", ", faction.Select(m => m.CommanderName))), 12, 0, 22 + relations.Count * 18, 570, 20, MenuTheme.Muted);

            CommanderActions(c);
        }
    }
}
