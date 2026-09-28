using System;
using System.Collections.Generic;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AntColony.UI
{
    // 오른쪽 날개의 5×3 명령 칸. 장수 1명을 고르면 장수 명령, 아니면 둥지 명령을 보인다.
    public sealed class CommandCard : MonoBehaviour
    {
        private sealed class Slot { public Button button; public Text label; public MenuTooltip tip; public Func<string> text, help; public Func<bool> ready, visible; }

        private const float CellW = 60f, CellH = 52f, GapX = 5f, GapY = 4f;
        private readonly List<Slot> slots = new List<Slot>();
        private RectTransform commanderGrid, colonyGrid;
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
            commanderGrid = Grid("CommanderCommands");
            colonyGrid = Grid("ColonyCommands");

            // HUD v2 배치(3×5, 칸 = 키보드 줄): Q W · R T / A S D F G / · X · V B.
            // 평시에는 Q/W가 잠긴 채 보이고, 출전 전용(A·S·귀환)은 숨긴다. 연구소 강화는 연구소 커맨드 카드에서 한다.
            Add(commanderGrid, 0, "Q", "Skill", () => SkillLabel(Commander), () => UseWeaponSkill(Commander),
                () => "무기 스킬(출전 중에만): 큰턱 강타(근접 Lv2), 갑각 방어 태세(근접 Lv3), 산성비(원거리 5, 지면 클릭), 집결(지휘 5).", () => Deployed() && SkillReady(Commander));
            Add(commanderGrid, 1, "W", "Dive", () => "급강하" + Cooldown(Commander != null ? Commander.Social.diveCooldown : 0f),
                () => SkillTargeting.Begin(new[] { Commander }, true), () => "날개 스킬(출전 중에만): 날개 + 근접/원거리 5. 지면 클릭 후 급강하, 3초 착지. 재사용 30초.",
                () => Deployed() && Commander.CanDive, () => Commander?.EquippedArmor?.armor == ArmorKind.Wings);
            Add(commanderGrid, 3, "R", "Weapon", () => "무기 교체", () => Commander?.CycleWeapon(),
                () => "가진 무기로 바꾸거나, 여분이 없으면 무기를 해제합니다. 병력은 유지됩니다.", null, Civilian);
            Add(commanderGrid, 4, "T", "Work Schedule", () => "작업표", () => GameMenuController.Instance?.WorkSchedule(),
                () => "장수별로 자율 작업을 켜거나 끕니다.", null, Civilian);
            Add(commanderGrid, 5, "A", "Attack Move", () => "어택무브", () => FindFirstObjectByType<AttackMoveController>()?.BeginAttackMode(),
                () => "지면을 클릭하면 이동하면서 만나는 적을 공격합니다.", null, Deployed);
            Add(commanderGrid, 6, "S", "Stop", () => "정지", () => Commander?.CommandStop(), () => "이동과 공격을 멈춥니다.", null, Deployed);
            Add(commanderGrid, 7, "D", "Return To Post", () => Commander != null && Commander.IsReturning ? "귀환 중" : "귀환", () => {
                if (Commander != null && !Commander.ReturnToPost()) ToastManager.Show("지금은 징집소로 귀환할 수 없습니다.");
            }, () => "징집소로 돌아가 생존 병력을 반납하고 자율 작업을 재개합니다.",
                () => Commander != null && Commander.IsDeployed && !Commander.IsReturning && !Commander.IsAwayFromHome, Deployed);
            Add(commanderGrid, 7, "D", "Send To Treatment", () => "치료", SendToTreatment,
                () => "부상 장수를 빈 침상이 있는 가장 가까운 의무실로 보내 입원시킵니다.", () => Commander != null && Commander.CanSendToTreatment, Civilian);
            Add(commanderGrid, 8, "F", "Priority Work", () => "우선 작업", PriorityHint,
                () => "우선 작업: 장수를 고른 채 대상을 우클릭하면 그 일부터 하고, 끝나면 다시 자율 작업으로 돌아갑니다.", null, Civilian);
            Add(commanderGrid, 9, "G", "Details", () => "상세", () => { if (Commander != null) GameMenuController.Instance?.Details(Commander); },
                () => "기술·열정·장비를 봅니다. 지휘 한도 = 10 + 지휘 기술 x 2.");
            // 연구소 강화는 건물 커맨드 카드가 생기기 전까지 디자인의 빈 칸(10·12)에 둔다.
            Add(commanderGrid, 10, "", "Attack Research", () => ResearchLabel("공격 연구"), () => hud.TryLabResearch(true),
                () => hud.LabResearchLabel(Commander, true) + "\n무기와 같은 보직의 연구소에서 이 장수의 공격을 올립니다.", null, Civilian);
            Add(commanderGrid, 12, "", "Armor Research", () => ResearchLabel("방어 연구"), () => hud.TryLabResearch(false),
                () => hud.LabResearchLabel(Commander, false) + "\n무기와 같은 보직의 연구소에서 이 장수의 방어를 올립니다.", null, Civilian);
            Add(commanderGrid, 11, "X", "Send To Rest", () => Commander != null && Commander.WorkState.resting ? "휴식 중" : "휴식", () => Commander?.SendToRest(),
                () => "피로한 장수를 가까운 휴게실(없으면 제자리)로 보내 피로가 풀릴 때까지 쉬게 합니다.", () => Commander != null && Commander.CanRest, Civilian);
            Add(commanderGrid, 13, "V", "Reward", () => "포상", Reward,
                () => "Food 30을 써서 충성심을 올립니다. 게임 달마다 1번.", () => Commander != null && Commander.PersonalState.rewardCooldown <= 0, Civilian);
            Add(commanderGrid, 14, "B", "Build", () => "건설", BuildScreen.Open, () => "건설 화면: 건물을 고르고 맡길 장수를 정합니다.", null, Civilian);

            Add(colonyGrid, 0, "", "Produce Ant", () => "개미 생산", hud.ProduceAnt, hud.ProduceAntLabel);
            Add(colonyGrid, 1, "", "Upgrade Barracks", () => "병영 강화", hud.UpgradeBarracks, hud.BarracksUpgradeLabel);
            Add(colonyGrid, 2, "", "Training Role", () => "훈련\n" + RoleName(hud.SelectedRole), hud.CycleCombatRole,
                () => "병영 강화에 쓸 보직을 고릅니다.");
            Add(colonyGrid, 3, "", "Unlock Fishing", () => "낚시", hud.ResearchFishing, hud.FishingLabel);
            Add(colonyGrid, 5, "", "Dig Expansion", () => "굴착 확장", hud.DigExpansion, () => "굴착지에 흙을 써서 확장 구역을 엽니다.");
            Add(colonyGrid, 6, "", "Work Schedule", () => "작업표", () => GameMenuController.Instance?.WorkSchedule(), () => "장수별 자율 작업을 설정합니다.");
            Add(colonyGrid, 7, "", "Conscription", () => "징집소", () => GameMenuController.Instance?.OpenConscription(), () => "징집소에서 출전 장수와 병력을 편성합니다.");
            Add(colonyGrid, 10, "", "Forbid Gathering", () => GatherDesignation.Forbidding ? "금지 지정\n중" : "채집 금지", () => GatherDesignation.Begin(true),
                () => "채집 금지 지정: 노드를 클릭하거나 드래그로 묶어 장수의 채집 대상에서 뺍니다. 우클릭·Esc로 끝냅니다.");
            Add(colonyGrid, 11, "", "Clear Designation", () => GatherDesignation.IsActive && !GatherDesignation.Forbidding ? "취소 지정\n중" : "지정 취소", () => GatherDesignation.Begin(false),
                () => "지정 취소: 클릭하거나 드래그한 노드의 채집 금지를 풉니다. 우클릭·Esc로 끝냅니다.");
            Add(colonyGrid, 14, "B", "Build", () => "건설", BuildScreen.Open, () => "건설 화면: 건물을 고르고 맡길 장수를 정합니다.");
        }

        private void LateUpdate()
        {
            if (commanderGrid == null) return;
            var building = BuildScreen.IsOpen;
            var commander = Commander;
            commanderGrid.gameObject.SetActive(!building && commander != null);
            colonyGrid.gameObject.SetActive(!building && commander == null);
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
        }

        private RectTransform Grid(string name)
        {
            var grid = MenuTheme.Rect(name, transform);
            grid.anchorMin = grid.anchorMax = grid.pivot = new Vector2(.5f, .5f);
            grid.sizeDelta = new Vector2(CellW * 5 + GapX * 4, CellH * 3 + GapY * 2);
            return grid;
        }

        private void Add(RectTransform grid, int cell, string key, string name, Func<string> text, UnityAction action,
            Func<string> help, Func<bool> ready = null, Func<bool> visible = null)
        {
            var rect = MenuTheme.Rect(name, grid);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(CellW, CellH);
            rect.anchoredPosition = new Vector2(cell % 5 * (CellW + GapX), -(cell / 5) * (CellH + GapY));
            rect.gameObject.AddComponent<Image>();
            rect.gameObject.AddComponent<Outline>().effectColor = MenuTheme.Line2;
            var button = rect.gameObject.AddComponent<Button>();
            MenuTheme.StyleButton(button);
            button.onClick.AddListener(action);
            if (key != "")
            {
                var kbd = Label(rect, key, 10, TextAnchor.UpperLeft, MenuTheme.Dim);
                kbd.rectTransform.offsetMin = new Vector2(4, 0); kbd.rectTransform.offsetMax = new Vector2(0, -2);
            }
            var label = Label(rect, "", 11, TextAnchor.LowerCenter, MenuTheme.TextColor);
            label.rectTransform.offsetMin = new Vector2(2, 4); label.rectTransform.offsetMax = new Vector2(-2, 0);
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

        // 연구소가 이 장수를 강화하는 중이면 버튼 문구로 바로 보인다.
        private string ResearchLabel(string idle) => Commander != null && Commander.LabUpgradeBusy ? "연구\n진행 중" : idle;
        public void SendToTreatment() { if (Commander != null && !Commander.SendToTreatment()) ToastManager.Show("빈 침상이 있는 의무실로 갈 수 없습니다."); }
        public void Reward() { if (Commander != null && !Commander.TryReward()) ToastManager.Show("본거지에서 게임 달마다 1번, Food 30이 필요합니다."); }
        // ponytail: 우선 작업은 기존 우클릭 지시를 쓴다. 클릭 대상 지정 모드가 필요해지면 GatherDesignation처럼 모드를 만든다.
        public static void PriorityHint() => ToastManager.Show("우선 작업: 대상을 우클릭하면 그 일부터 합니다.", ToastKind.Hint);

        private bool Deployed() => Commander != null && Commander.IsDeployed;
        private bool Civilian() => Commander != null && !Commander.IsDeployed;

        private static string Cooldown(float seconds) => seconds > 0f ? $"\n{Mathf.CeilToInt(seconds)}s" : "";
    }
}
