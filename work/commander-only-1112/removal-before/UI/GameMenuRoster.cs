using System;
using System.Linq;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V6 인재·편성: 왼쪽 장수 목록(필터·열 머리·합계) + 오른쪽 선택 장수 상세.
    public sealed partial class GameMenuController
    {
        private static readonly string[] SkillNames = { "채집", "건설", "농사", "낚시", "제작", "연구", "근접", "원거리", "지휘", "의료", "요리", "근력", "예술", "정치", "사육", "조사" };
        private static readonly string[] FilterNames = { "전체", "대기", "작업 중", "원정 중", "기분 경고" };
        private int rosterFilter;
        private string rosterSearch = "";

        public void Roster() => RosterScreen(null);
        public void Details(CommanderAnt c) => RosterScreen(c);

        private static int Category(CommanderAnt c) => c.IsAwayFromHome || c.IsDeparting ? 3 : c.CanStartConstruction ? 1 : 2;
        private static string Status(CommanderAnt c) => c.IsDeparting ? "이탈 중" : c.IsCaptive ? "포로" : c.Garrison != null ? "주둔"
            : c.Transport != null ? "원정 중" : c.ScoutMission != null ? "정찰 중" : c.PersonalHealth <= 0 ? "쓰러짐" : c.IsReturning ? "귀환 중" : c.IsDeployed ? "출전 중"
            : c.ScienceAssignment != null || c.LabUpgradeBusy ? "연구" : c.CraftingWorkshop != null ? "제작"
            : CommanderOverhead.Activity(c) is var activity && activity.Length > 0 ? activity : "대기";
        // 4 = 기분 경고: 기분 위험 구간이거나 붕괴 중인 장수.
        private static bool InFilter(CommanderAnt c, int filter) => filter == 0 || (filter == 4 ? CommanderOverhead.MoodAlert(c) : Category(c) == filter);
        private static Color MoodColor(float mood) => mood < 35 ? MenuTheme.Danger : mood < 50 ? MenuTheme.HpMid : MenuTheme.Hp;

        private bool rosterDetail;
        private void RosterScreen(CommanderAnt selected)
        {
            var all = SortedCommanders();
            var selection = AntColony.Core.SceneObjects.Any<SelectionManager>();
            var window = PrototypeWindow("장수", $"{all.Length}명 · 현재 선택 {HudOverview.Selected(selection).Length}명", () => RosterScreen(selected));
            float width = window.sizeDelta.x, height = window.sizeDelta.y;
            bool compact = width < 1000;
            var shown = all.Where(c => InFilter(c, rosterFilter) && (rosterSearch.Length == 0 || c.CommanderName.IndexOf(rosterSearch, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
            selected ??= shown.FirstOrDefault();
            for (int i = 0; i < FilterNames.Length; i++)
            {
                var filter = i;
                L.Button(window, "Filter " + FilterNames[i], $"{FilterNames[i]} {all.Count(c => InFilter(c, filter))}", 20 + i * 94, 82, 90, 30,
                    () => { rosterFilter = filter; rosterDetail = false; RosterScreen(selected); }, primary: filter == rosterFilter);
            }
            var search = MenuTheme.Input(window, rosterSearch); search.name = "Commander Search";
            L.Place((RectTransform)search.transform, 20, 122, Mathf.Min(250, width - 380), 32);
            search.contentType = InputField.ContentType.Standard; search.characterLimit = 20;
            search.onEndEdit.AddListener(v => { rosterSearch = v; rosterDetail = false; RosterScreen(selected); });
            L.Button(window, "Work Schedule", "작업표", width - 320, 122, 90, 32, WorkSchedule);
            L.Button(window, "Conscription", "출전 편성", width - 220, 122, 90, 32, OpenConscription);
            L.Button(window, "Carried Supplies", "소지품", width - 120, 122, 100, 32, CarriedSupplies);
            float top = compact ? 204 : 170, listWidth = compact ? width - 40 : width - 576;
            if (compact)
            {
                L.Button(window, "Roster List Tab", $"목록 {shown.Length}", 20, 164, 130, 30, () => { rosterDetail = false; RosterScreen(selected); }, primary: !rosterDetail);
                L.Button(window, "Roster Detail Tab", selected == null ? "상세" : selected.CommanderName + " 상세", 160, 164, 200, 30,
                    () => { rosterDetail = true; RosterScreen(selected); }, primary: rosterDetail);
            }
            if (!compact || !rosterDetail)
            {
                var list = L.List(window, 20, top, listWidth, height - top - 48, 5);
                foreach (var c in shown)
                {
                    var captured = c;
                    var row = L.Button(list, "Commander " + c.CommanderName, "", 0, 0, 0, 0,
                        () => { rosterDetail = true; RosterScreen(captured); }, primary: c == selected);
                    row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
                    var face = CharacterPortraits.Get(c);
                    if (face != null)
                    {
                        var portrait = L.Place(MenuTheme.Rect("Portrait", row.transform), 8, 8, 44, 44);
                        var image = portrait.gameObject.AddComponent<RawImage>(); image.texture = face; image.raycastTarget = false;
                    }
                    L.Label(row.transform, c.CommanderName, 15, 64, 4, listWidth - 180, 28, bold: true);
                    L.Label(row.transform, Status(c) + " · " + c.WeaponName, 12, 64, 32, listWidth - 190, 24, MenuTheme.Muted);
                    L.Label(row.transform, $"기분 {c.Mood:0}\n체력 {c.PersonalHealth:0}", 12, listWidth - 120, 8, 94, 48, MoodColor(c.Mood), TextAnchor.MiddleRight);
                }
                L.Label(window, $"{shown.Length}명 표시 · 전체 장수 {all.Length}명", 12, 20, height - 38, listWidth, 24, MenuTheme.Muted);
            }
            if (!compact || rosterDetail)
            {
                float x = compact ? 20 : width - 540;
                content = L.List(window, x, top, compact ? width - 40 : 520, height - top - 58, 10);
                if (selected == null) MenuTheme.Text(content, "조건에 맞는 장수가 없습니다.", 14, 40);
                else CommanderDetail(selected);
                if (selected != null)
                {
                    var target = selected;
                    L.Button(window, "Roster Select", "게임에서 선택", x, height - 46, 142, 32, () => { selection?.SelectOnly(target.GetComponent<SelectableObject>()); Resume(); });
                    L.Button(window, "Roster Add Selection", "선택에 추가", x + 152, height - 46, 142, 32, () => { selection?.Add(target.GetComponent<SelectableObject>()); RosterScreen(target); });
                    L.Button(window, "Roster Equipment", "장비", x + 304, height - 46, 88, 32, () => ShowEquipment(target));
                    L.Button(window, "Roster Health", "건강", x + 402, height - 46, 88, 32, () => ShowHealth(target));
                }
            }
        }

        private static string BreakName(AntColony.Units.MentalBreak b) => b switch
        {
            AntColony.Units.MentalBreak.None => "정상", AntColony.Units.MentalBreak.Idle => "무기력", AntColony.Units.MentalBreak.Flee => "도주",
            AntColony.Units.MentalBreak.AttackBuilding => "기물 파손", AntColony.Units.MentalBreak.AttackCommander => "동료 공격", AntColony.Units.MentalBreak.SelfHarm => "자해",
            AntColony.Units.MentalBreak.Binge => "폭식", AntColony.Units.MentalBreak.Wander => "배회", _ => b.ToString()
        };

        private void CommanderDetail(CommanderAnt c)
        {
            var head = L.Cell(content, "DetailHead", 108);
            var portrait = L.Well(head, "Portrait", 0, 0, 96, 96);
            // 얼굴 초상화(HUD 명부와 같은 CharacterPortraits). 없을 때만 역할 글자(2026-11-01).
            var face = CharacterPortraits.Get(c);
            if (face != null) { var img = MenuTheme.Rect("Face", portrait).gameObject.AddComponent<UnityEngine.UI.RawImage>(); img.texture = face; MenuTheme.Stretch(img.rectTransform); img.raycastTarget = false; }
            else L.Label(portrait, CommandCard.RoleName(c.Role), 20, 0, 0, 96, 96, MenuTheme.Dim, TextAnchor.MiddleCenter);
            L.Label(head, c.CommanderName, 20, 108, 0, 388, 30, MenuTheme.Accent, bold: true);
            L.Label(head, $"{c.WeaponName} · {CommandCard.RoleName(c.Role)} · {Status(c)}", 13, 108, 30, 388, 20, MenuTheme.Muted);
            TraitChip.Grid(head, c.Traits.values, 108, 50, 388, 18, 4, 3);
            L.Label(head, $"개인 전투    공격 <b>{c.AttackDamage:0.#}</b>    방어 <b>{c.Armor:0.#}</b>    연구 공격 {c.LabAttackLevel} · 방어 {c.LabArmorLevel}", 12, 108, 70, 388, 20);
            L.Meter(head, 108, 94, 388, 8, c.PersonalHealth / AntColony.Core.GameBalance.CommanderHealth, MenuTheme.Hp);

            var factors = c.PersonalState.moodFactors;
            var mood = L.Cell(content, "Mood", 70 + factors.Count * 18);
            L.Label(mood, $"<b>기분</b>   <size=15><b>{c.Mood:0}</b></size> / 100   <color=#b1b3a7>경고선 35/20/5 · {BreakName(c.PersonalState.mentalBreak)}</color>", 13, 0, 0, 496, 26);
            L.Meter(mood, 0, 30, 496, 8, c.Mood / 100f, MoodColor(c.Mood));
            L.Label(mood, $"기본 60 · 기대 수준 +{AntColony.Core.GameBalanceNeeds.ExpectationMood(c.PersonalState.expectationTier)}", 12, 0, 46, 496, 18, MenuTheme.Muted);
            for (var i = 0; i < factors.Count; i++)
                L.Label(mood, $"{PersonalDisplayNames.Mood(factors[i].reason)}  <b>{factors[i].value:+0;-0;0}</b>  <color=#b1b3a7>{factors[i].remaining:0}초</color>", 12, 0, 64 + i * 18, 496, 18, MenuTheme.Muted);

            // Phase 3: 충성심 대신 탈주 위험 — 기분이 기준 이하로 한 달 이어지면 떠난다.
            var leave = L.Cell(content, "Departure", 48);
            L.Label(leave, $"<b>탈주 위험</b>   기분 {c.Traits.DepartureMood} 이하 지속 시 이탈까지 <b>{Mathf.Max(0, SocialRules.Month - c.PersonalState.lowMoodSeconds):0}초</b>", 13, 0, 0, 496, 26);
            L.Meter(leave, 0, 30, 496, 8, c.PersonalState.lowMoodSeconds / SocialRules.Month, MenuTheme.DangerInk);

            var skills = L.Cell(content, "Skills", 26 + ((CommanderTalents.Count + 2) / 3) * 46);
            c.Talents.EnsurePotential();
            var evaluation = L.Cell(content, "TalentAssessment", 78);
            L.Label(evaluation, TalentSummary(c.PersonalState.id), 13, 0, 0, 496, 36);
            L.Button(evaluation, "Open Assessment", "인재 평가 · 분야별 별점", 0, 40, 496, 30, () => ShowAssessment(c.PersonalState.id));
            L.Label(skills, "<b>기술</b>  <color=#b1b3a7>0~20 · ★ 열정 · 최근 활용 비중</color>", 13, 0, 0, 496, 22);
            foreach (CommanderActivity skill in Enum.GetValues(typeof(CommanderActivity)))
            {
                var i = (int)skill; var level = c.Talents.Level(skill);
                var x = i % 3 * 166f; var y = 26 + i / 3 * 46f;
                var cell = L.Box(skills, "Skill " + skill, x, y, 160, 42, MenuTheme.Plate2);
                cell.gameObject.AddComponent<MenuTooltip>().Message = AntColony.Core.SpecialtyAcquisition.Description(c, skill);
                L.Label(cell, $"{SkillNames[i]} <color=#f2a93b>{new string('★', c.Traits.Flame(skill))}</color> <color=#b1b3a7>{c.Talents.UsageShare(skill):P0}</color>", 12, 8, 2, 100, 20, MenuTheme.Muted);
                L.Label(cell, level.ToString(), 16, 112, 2, 40, 20, align: TextAnchor.MiddleRight, bold: true);
                var progress = level >= CommanderTalents.MaxLevel ? 1f : c.Talents.Xp(skill) / CommanderTalents.Required(level);
                L.Meter(cell, 8, 28, 144, 5, progress, MenuTheme.Accent);
            }

            var lines = HealthLines(c.Health).Split('\n');
            var body = L.Cell(content, "Injuries", 24 + lines.Length * 18);
            L.Label(body, $"<b>건강 · {c.Health.Summary}</b>", 13, 0, 0, 496, 22);
            for (var i = 0; i < lines.Length; i++) L.Label(body, lines[i], 12, 0, 22 + i * 18, 496, 18, c.Health.Injured ? MenuTheme.DangerInk : MenuTheme.Muted);

            var equipment = L.Cell(content, "Equipment", 70);
            L.Label(equipment, "<b>장비</b>", 13, 0, 0, 496, 22);
            string[] slotNames = { "무기", "갑옷", "장신구" };
            for (var i = 0; i < 3; i++)
            {
                var item = c.PersonalState.equipment.Find(e => (int)e.slot == i);
                var box = L.Box(equipment, "Slot " + slotNames[i], i * 166f, 24, 160, 44, MenuTheme.Plate2);
                L.Label(box, slotNames[i], 11, 8, 2, 144, 18, MenuTheme.Dim);
                L.Label(box, item != null ? item.Label : "비어 있음", 13, 8, 20, 144, 22, item != null ? MenuTheme.TextColor : MenuTheme.Dim);
            }

            var relations = c.PersonalState.relations;
            var faction = c.Faction();
            var social = L.Cell(content, "Relations", 44 + relations.Count * 18);
            L.Label(social, "<b>관계</b>", 13, 0, 0, 496, 22);
            for (var i = 0; i < relations.Count; i++)
            {
                var relation = relations[i];
                var other = SortedCommanders().FirstOrDefault(o => o.PersonalState.id == relation.otherId);
                var label = relation.spouse ? "배우자" : !relation.family && relation.value >= 70 && other != null && !c.HasOtherSpouse(other) && !other.HasOtherSpouse(c) ? "연인" : relation.value >= 40 ? "친구(쓰러지면 복수 돌진)" : relation.value <= -40 ? "라이벌" : "지인";
                L.Label(social, $"{label}  <b>{other?.CommanderName ?? "떠난 장수"}</b>  <color=#b1b3a7>{relation.value:0}</color>", 12, 0, 22 + i * 18, 496, 18, MenuTheme.Muted);
            }
            L.Label(social, "파벌: " + (faction.Count == 0 ? "없음" : string.Join(", ", faction.Select(m => m.CommanderName))), 12, 0, 22 + relations.Count * 18, 496, 20, MenuTheme.Muted);

            CommanderActions(c);
        }
    }
}
