using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 오른쪽 명령 칸. 모든 상태 3×3(최대 9칸, 2026-10-04 사용자 요청). 평시 장수 = 일상 명령, 출전 장수 = 전투 명령, 선택 없음 = 둥지 명령.
    // 단축키는 전체 미정(2026-10-01)이라 칸에 키 글자를 표시하지 않는다.
    public sealed class CommandCard : MonoBehaviour
    {
        private sealed class Slot { public Button button; public Text label; public MenuTooltip tip; public Func<string> text, help; public Func<bool> ready, visible; }

        private const float Pad = 14f, Gap = 8f, SmallH = 50f;
        private readonly List<Slot> slots = new List<Slot>();
        private RectTransform civilianGrid, deployedGrid, colonyGrid, multiGrid, targetGrid;
        private readonly Dictionary<RectTransform, int> columnsOf = new Dictionary<RectTransform, int>();
        private SelectionManager selection;
        private HUDController hud;

        public static string RoleName(UnitRole role) => role switch
        {
            UnitRole.Melee => "큰턱", UnitRole.Ranged => "산샘", UnitRole.Defense => "갑각",
            UnitRole.Support => "페로몬", UnitRole.Flying => "날개", _ => "일개미"
        };

        public CommanderAnt Commander => SelectedUnitPanel.FindSingleSelectedCommander(selection);

        public void Build(HUDController owner)
        {
            hud = owner;
            selection = FindFirstObjectByType<SelectionManager>();
            civilianGrid = Grid("CivilianCommands", 3);
            deployedGrid = Grid("DeployedCommands", 3);
            colonyGrid = Grid("ColonyCommands", 3);
            multiGrid = Grid("MultiCommands", 3);
            targetGrid = Grid("TargetCommands", 3);

            Add(civilianGrid, 0, "Priority Work", () => "우선", PriorityHint,
                () => "우선 작업: 장수를 고른 채 대상을 우클릭하면 그 일부터 하고, 끝나면 다시 자율 작업으로 돌아갑니다.");
            Add(civilianGrid, 1, "Send To Rest", () => Commander != null && Commander.WorkState.resting ? "휴식 중" : Commander != null && Commander.PersonalState.treating ? "치료 중" : "휴식",
                RestOrTreat, () => "휴식: 숙소로 보내 쉬게 합니다. 부상이 있으면 빈 침상이 있는 의무실로 보냅니다.",
                () => Commander != null && (Commander.CanSendToTreatment || Commander.CanRest));
            Add(civilianGrid, 2, "Conscription", () => "징집소", () => GameMenuController.Instance?.OpenConscription(), () => "징집소: 출전 장수와 병력을 편성합니다.");
            Add(civilianGrid, 3, "Build", () => "건설", BuildScreen.Open, () => "건설: 벽·문 · 가구 · 작업 · 방어.", null, null, true);
            Add(civilianGrid, 4, "Civilian Work Schedule", () => "작업표", () => GameMenuController.Instance?.WorkSchedule(), () => "장수의 자율 작업을 설정합니다.");
            Add(civilianGrid, 5, "Civilian Details", () => "상세", ShowDetails, () => "장수의 기분·건강·장비·기술을 확인합니다.");
            Add(civilianGrid, 6, "Civilian Cycle Weapon", () => "무기", CycleWeapon, () => "보유한 다음 무기로 바꿉니다.");
            Add(civilianGrid, 7, "Civilian Science", () => "연구", () => GameMenuController.Instance?.Science(), () => "과학 연구를 확인합니다.");
            Add(civilianGrid, 8, "Civilian Stop", () => "정지", () => Commander?.CommandStop(), () => "하던 일을 멈춥니다. 자율 작업은 다시 이어집니다.");

            // 출전 중 카드는 기획 미정(2026-10-01) — 기존 전투 명령을 유지한다.
            Add(deployedGrid, 0, "Skill", () => SkillLabel(Commander), () => UseWeaponSkill(Commander),
                () => "무기 스킬: 큰턱 강타(근접 Lv2), 갑각 방어 태세(근접 Lv3), 산성비(원거리 5, 지면 클릭), 집결(지휘 5).", () => SkillReady(Commander));
            Add(deployedGrid, 1, "Dive", () => "급강하" + Cooldown(Commander != null ? Commander.Social.diveCooldown : 0f),
                () => SkillTargeting.Begin(new[] { Commander }, true), () => "날개 스킬: 날개 + 근접/원거리 5. 지면 클릭 후 급강하, 3초 착지. 재사용 30초.",
                () => Commander != null && Commander.CanDive, () => Commander?.EquippedArmor?.armor == ArmorKind.Wings);
            Add(deployedGrid, 3, "Attack Move", () => "공격 이동", () => FindFirstObjectByType<AttackMoveController>()?.BeginAttackMode(),
                () => "지면을 클릭하면 이동하면서 만나는 적을 공격합니다.");
            Add(deployedGrid, 4, "Stop", () => "정지", () => Commander?.CommandStop(), () => "이동과 공격을 멈춥니다.");
            Add(deployedGrid, 5, "Return To Post", () => Commander != null && Commander.IsReturning ? "귀환 중" : "귀환", () => {
                if (Commander != null && !Commander.ReturnToPost()) ToastManager.Show("지금은 징집소로 귀환할 수 없습니다.");
            }, () => "징집소로 돌아가 생존 병력을 반납하고 자율 작업을 재개합니다.",
                () => Commander != null && !Commander.IsReturning && !Commander.IsAwayFromHome);
            Add(deployedGrid, 6, "Deployed Details", () => "상세", ShowDetails, () => "장수의 기분·건강·장비·기술을 확인합니다.");
            Add(deployedGrid, 7, "Deployed Cycle Weapon", () => "무기", CycleWeapon, () => "보유한 다음 무기로 바꿉니다.");

            Add(colonyGrid, 0, "Colony Work Schedule", () => "작업표", () => GameMenuController.Instance?.WorkSchedule(), () => "장수의 자율 작업을 설정합니다.");
            Add(colonyGrid, 1, "Colony Roster", () => "장수 관리", () => GameMenuController.Instance?.Roster(), () => "장수의 기분·건강·장비를 확인합니다.");
            Add(colonyGrid, 2, "Colony Conscription", () => "징집소", () => GameMenuController.Instance?.OpenConscription(), () => "출전 장수와 병력을 편성합니다.");
            Add(colonyGrid, 3, "Colony Build", () => "건설", BuildScreen.Open, () => "가구·건물·벽을 건설합니다.", null, null, true);
            Add(colonyGrid, 4, "Colony Science", () => "연구", () => GameMenuController.Instance?.Science(), () => "과학 연구를 확인합니다.");
            Add(colonyGrid, 5, "Colony Population", () => "인구", () => GameMenuController.Instance?.Population(), () => "개미 인구와 방을 확인합니다.");
            Add(colonyGrid, 6, "Colony Diplomacy", () => "외교", () => GameMenuController.Instance?.Diplomacy(), () => "다른 세력과의 관계를 확인합니다.");

            Add(multiGrid, 0, "Multi Priority", () => AllDeployed() ? "공격 이동" : "우선", () => {
                if (AllDeployed()) FindFirstObjectByType<AttackMoveController>()?.BeginAttackMode(); else PriorityHint();
            }, () => "선택한 장수 모두에게 가능한 명령만 사용할 수 있습니다.", () => AllDeployed() || AllCivilian());
            Add(multiGrid, 1, "Multi Stop", () => AllDeployed() ? "정지" : "개별 선택 필요", () => {
                foreach (var c in HudOverview.Selected(selection)) if (c.IsDeployed) c.CommandStop();
            }, () => "평시 휴식·치료는 개별 장수를 선택하세요.", AllDeployed);
            Add(multiGrid, 2, "Multi Conscription", () => AllDeployed() ? "귀환" : "징집소", () => {
                if (AllDeployed()) foreach (var c in HudOverview.Selected(selection)) c.ReturnToPost();
                else GameMenuController.Instance?.OpenConscription();
            }, () => "출전 부대는 징집소로 귀환합니다.", () => AllCivilian() || AllDeployed() && HudOverview.Selected(selection).All(c => !c.IsReturning && !c.IsAwayFromHome));
            Add(multiGrid, 3, "Multi Clear", () => "선택 해제", () => selection?.ClearSelection(), () => "선택을 해제합니다.");

            Add(targetGrid, 0, "Target Residents", () => WorkTargetPanel.Scout != null ? (WorkTargetPanel.Scout.IsDispatched ? "정찰 중" : "정찰 파견") : WorkTargetPanel.Target is Dormitory ? "배정 보기" : "대상 정보", () => {
                if (WorkTargetPanel.Scout is ScoutPost scout) GameMenuController.Instance?.ShowScout(scout); else WorkTargetPanel.ShowAssignments();
            }, () => WorkTargetPanel.Scout != null ? "동행 장수를 골라 정찰을 보냅니다." : "선택 대상의 배정·인력 정보를 확인합니다.",
                () => WorkTargetPanel.Scout == null || !WorkTargetPanel.Scout.IsDispatched);
            Add(targetGrid, 1, "Target Build", () => WorkTargetPanel.Target is Dormitory ? "숙소 건설" : "건설", () => {
                if (WorkTargetPanel.Target is Dormitory) BuildScreen.OpenDormitory(); else BuildScreen.Open();
            }, () => "현재 숙소 가구 하나가 침대 4개를 제공합니다.", null, null, true);
            Add(targetGrid, 2, "Target Room", () => "방 정보", WorkTargetPanel.ShowRoomInfo, () => "방 종류·등급을 확인합니다.", () => WorkTargetPanel.Target is BuildingBase);
            Add(targetGrid, 3, "Target Clear", () => "선택 해제", () => selection?.ClearSelection(), () => "선택을 해제합니다.");
        }

        private void LateUpdate()
        {
            if (civilianGrid == null) return;
            var building = BuildScreen.IsOpen;
            var commander = Commander;
            civilianGrid.gameObject.SetActive(!building && commander != null && !commander.IsDeployed);
            deployedGrid.gameObject.SetActive(!building && commander != null && commander.IsDeployed);
            var selected = HudOverview.Selected(selection);
            var target = WorkTargetPanel.Target;
            targetGrid.gameObject.SetActive(!building && target != null && target.gameObject.activeInHierarchy);
            multiGrid.gameObject.SetActive(!building && target == null && selected.Length > 1);
            colonyGrid.gameObject.SetActive(!building && commander == null && target == null && selected.Length == 0);
            foreach (var slot in slots)
            {
                if (!slot.button.transform.parent.gameObject.activeSelf) continue;
                var visible = slot.visible == null || slot.visible();
                slot.button.gameObject.SetActive(visible);
                if (!visible) continue;
                slot.label.text = slot.text();
                slot.button.interactable = slot.ready == null || slot.ready();
                slot.tip.Message = slot.help();
            }
            foreach (var pair in columnsOf)
            {
                var grid = pair.Key; if (!grid.gameObject.activeSelf) continue;
                var layout = grid.GetComponent<GridLayoutGroup>();
                var count = grid.GetComponentsInChildren<Button>().Length;
                int columns = grid.rect.height < 110 ? Mathf.Max(1, count) : pair.Value;
                int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
                layout.constraintCount = columns;
                layout.cellSize = new Vector2((grid.rect.width - Gap * (columns - 1)) / columns, (grid.rect.height - Gap * (rows - 1)) / rows);
            }
        }

        // 판 안쪽(여백 14)을 채우는 격자. 칸 크기는 LateUpdate에서 버튼 수에 맞춘다.
        private RectTransform Grid(string name, int columns)
        {
            if (transform.Find("CommandScreen") == null)
            {
                var screen = MenuTheme.Rect("CommandScreen", transform);
                MenuTheme.Stretch(screen); screen.offsetMin = new Vector2(12, 14); screen.offsetMax = new Vector2(-12, -HudConsole.WingTop);
                MenuTheme.InsetScreen(screen.gameObject).raycastTarget = false;
            }
            var grid = MenuTheme.Rect(name, transform);
            MenuTheme.Stretch(grid); grid.offsetMin = new Vector2(18, 20); grid.offsetMax = new Vector2(-18, -HudConsole.WingTop - 6);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = columns; layout.spacing = new Vector2(Gap, Gap);
            columnsOf[grid] = columns;
            return grid;
        }

        private void Add(RectTransform grid, int cell, string name, Func<string> text, UnityAction action,
            Func<string> help, Func<bool> ready = null, Func<bool> visible = null, bool primary = false)
        {
            var columns = columnsOf[grid];
            var width = (HudConsole.RightWidth - Pad * 2 - Gap * (columns - 1)) / columns;
            var height = columns == 2 ? (HudConsole.CenterHeight - Pad * 2 - Gap) / 2 : SmallH;
            var rect = MenuTheme.Rect(name, grid);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(cell % columns * (width + Gap), -(cell / columns) * (height + Gap));
            rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            MenuTheme.StyleFrameButton(button, primary);
            button.onClick.AddListener(action);
            var label = Label(rect, "", columns == 2 ? 14 : 12, TextAnchor.MiddleCenter, primary ? MenuTheme.Hex(0xffe2ab) : MenuTheme.TextColor);
            label.rectTransform.offsetMin = new Vector2(2, 2); label.rectTransform.offsetMax = new Vector2(-2, -2);
            if (primary) label.fontStyle = FontStyle.Bold;
            slots.Add(new Slot { button = button, label = label, tip = rect.gameObject.AddComponent<MenuTooltip>(), text = text, help = help, ready = ready, visible = visible });
        }

        private static Text Label(RectTransform parent, string value, int size, TextAnchor align, Color color)
        {
            var text = MenuTheme.Text(parent, value, size);
            MenuTheme.Stretch(text.rectTransform);
            text.alignment = align; text.color = color; text.lineSpacing = .9f;
            return text;
        }

        private static void UseWeaponSkill(CommanderAnt c)
        {
            if (c == null) return;
            if (c.Role == UnitRole.Melee) c.TryPowerStrike();
            else if (c.Role == UnitRole.Defense) c.TryDefensiveStance();
            else SkillTargeting.Begin(new[] { c }, false);
        }

        private static bool SkillReady(CommanderAnt c) => c != null && c.Role switch
        {
            UnitRole.Melee => c.CanPowerStrike,
            UnitRole.Defense => c.CanDefensiveStance,
            UnitRole.Ranged => c.CanAcidRain,
            UnitRole.Support => c.CanRally,
            _ => false
        };

        // 잠김(Lv n) → 발동 중 → 재사용 대기(초) → 준비 순으로 표시한다.
        private static string SkillLabel(CommanderAnt c)
        {
            if (c == null) return "";
            var talents = c.Talents; var skills = c.Skills;
            switch (c.Role)
            {
                case UnitRole.Melee:
                    if (talents.Level(CommanderActivity.Melee) < CommanderSkills.PowerStrikeLevel) return "강타\nLv" + CommanderSkills.PowerStrikeLevel;
                    return skills.PowerStrikeArmed ? "강타 ON" : "강타" + Cooldown(skills.PowerStrikeCooldownLeft);
                case UnitRole.Defense:
                    if (talents.Level(CommanderActivity.Melee) < CommanderSkills.DefensiveStanceLevel) return "방어 태세\nLv" + CommanderSkills.DefensiveStanceLevel;
                    return skills.DefensiveStanceActive ? $"방어 태세\n{Mathf.CeilToInt(skills.DefensiveStanceTimeLeft)}s" : "방어 태세" + Cooldown(skills.DefensiveStanceCooldownLeft);
                case UnitRole.Ranged:
                    return (talents.Level(CommanderActivity.Ranged) < 5 ? "산성비\nLv5" : "산성비" + Cooldown(c.Social.acidCooldown));
                case UnitRole.Support:
                    return (talents.Level(CommanderActivity.Command) < 5 ? "집결\nLv5" : "집결" + Cooldown(c.Social.rallyCooldown));
                default:
                    return "스킬 없음";
            }
        }

        private void ShowDetails() { if (Commander != null) GameMenuController.Instance?.Details(Commander); }
        private void CycleWeapon() { if (Commander != null && !Commander.CycleWeapon()) ToastManager.Show("바꿀 무기가 없습니다."); }

        // v3 휴식 = 휴식 + 치료: 부상이면 의무실, 아니면 휴식.
        public void RestOrTreat()
        {
            var c = Commander; if (c == null) return;
            if (c.CanSendToTreatment) { if (!c.SendToTreatment()) ToastManager.Show("빈 침상이 있는 의무실로 갈 수 없습니다."); }
            else c.SendToRest();
        }
        // ponytail: 우선 작업은 기존 우클릭 지시를 쓴다. 클릭 대상 지정 모드가 필요해지면 GatherDesignation처럼 모드를 만든다.
        public static void PriorityHint() => ToastManager.Show("우선 작업: 대상을 우클릭하면 그 일부터 합니다.", ToastKind.Hint);

        private bool AllDeployed() { var list = HudOverview.Selected(selection); return list.Length > 1 && list.All(c => c.IsDeployed && c.CanReceiveOrders); }
        private bool AllCivilian() { var list = HudOverview.Selected(selection); return list.Length > 1 && list.All(c => !c.IsDeployed && c.CanReceiveOrders); }

        private static string Cooldown(float seconds) => seconds > 0f ? $"\n{Mathf.CeilToInt(seconds)}s" : "";
    }
}
