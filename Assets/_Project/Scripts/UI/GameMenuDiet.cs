using System.Linq;
using AntColony.Units;
using L = AntColony.UI.MenuLayout;

namespace AntColony.UI
{
    // V5 식단 화면(2026-10-15): 공통 식단과 장수별 덮어쓰기, 복사·붙여넣기·모든 장수 적용·공통 복귀. 실제 식사는 CommanderMeal이 이 설정을 따른다.
    public sealed partial class GameMenuController
    {
        private CommanderAnt dietTarget; // null = 공통
        private int dietClipboard = -1;
        private static readonly (int bit, string name)[] DietItems = { (CommanderAnt.DietRaw, "날것"), (CommanderAnt.DietSimple, "간단한 요리"), (CommanderAnt.DietFine, "좋은 요리") };

        public void ShowDiet(CommanderAnt target = null)
        {
            dietTarget = target;
            var window = V5Window("식단", "허용한 정상 식품 중 좋은 요리 → 간단한 요리 → 날것 순 · 같은 종류는 먼저 썩을 것부터 · 썩은 식품은 최후 선택");
            ManagementTabs(window, "물류·식품", "식단");
            content = V5Section(window, "대상", 20, 104, 360, 544);
            MenuTheme.Button(content, (target == null ? "▶ " : "") + "공통 식단(모든 장수 기본)", () => ShowDiet(null));
            foreach (var c in SortedCommanders().Where(c => c.IsColonyMember && !c.IsDead))
            {
                var pick = c;
                MenuTheme.Button(content, (target == c ? "▶ " : "") + c.CommanderName + (c.PersonalState.meal.diet < 0 ? " · 공통" : " · 개별"), () => ShowDiet(pick));
            }

            var current = target == null ? CommanderAnt.CommonDiet : target.Diet;
            content = V5Section(window, target == null ? "공통 식단" : $"{target.CommanderName} 식단" + (target.PersonalState.meal.diet < 0 ? " (공통 식단 사용 중)" : " (개별 설정)"), 394, 104, 686, 544);
            void Set(int value)
            {
                if (target == null) CommanderAnt.CommonDiet = value; else target.PersonalState.meal.diet = value;
                ShowDiet(target);
            }
            foreach (var (bit, name) in DietItems)
                MenuTheme.Button(content, ((current & bit) != 0 ? "☑ " : "☐ ") + name, () => Set(current ^ bit));
            if (current == 0) MenuTheme.Text(content, "<color=#f2c94c>허용한 정상 식품이 없어 썩은 식품만 먹습니다(기분 하락·식중독 위험).</color>", 15, 40);
            var row = L.Cell(content, "DietTools", 44);
            L.Button(row, "Diet Copy", "복사", 0, 0, 150, 40, () => { dietClipboard = current; ShowDiet(target); });
            L.Button(row, "Diet Paste", "붙여넣기", 156, 0, 150, 40, () => { if (dietClipboard >= 0) Set(dietClipboard); }).interactable = dietClipboard >= 0;
            L.Button(row, "Diet Apply All", "모든 장수에게 적용", 312, 0, 200, 40, () =>
            {
                foreach (var c in SortedCommanders()) c.PersonalState.meal.diet = current;
                ShowDiet(target);
            });
            if (target != null)
                MenuTheme.Button(content, "공통 식단 따르기", () => { target.PersonalState.meal.diet = -1; ShowDiet(target); }).interactable = target.PersonalState.meal.diet >= 0;
            refreshDutyScreen = null;
            V5Fit(window);
        }
    }
}
