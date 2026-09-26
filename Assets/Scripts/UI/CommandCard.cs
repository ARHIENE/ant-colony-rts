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

            Add(commanderGrid, 0, "Q", "Skill", () => SkillLabel(Commander), () => UseWeaponSkill(Commander),
                () => "무기 스킬: 큰턱 강타(근접 Lv2), 갑각 방어 태세(근접 Lv3), 산성비(원거리 5, 지면 클릭), 집결(지휘 5).", () => SkillReady(Commander));
            Add(commanderGrid, 1, "W", "Dive", () => "급강하" + Cooldown(Commander != null ? Commander.Social.diveCooldown : 0f),
                () => SkillTargeting.Begin(new[] { Commander }, true), () => "날개 + 근접/원거리 5. 지면 클릭 후 급강하, 3초 착지. 재사용 30초.",
                () => Commander != null && Commander.CanDive, () => Commander != null && Commander.EquippedArmor?.armor == ArmorKind.Wings);
            Add(commanderGrid, 2, "E", "+1 Ant", () => "병력 +1", () => Commander?.TryAssign(1),
                () => "둥지에서 대기 개미 1마리를 지휘 한도 안에서 배정합니다.");
            Add(commanderGrid, 3, "R", "Weapon", () => "무기 교체", () => Commander?.CycleWeapon(),
                () => "가진 무기로 바꾸거나, 여분이 없으면 무기를 해제합니다. 병력은 유지됩니다.");
            Add(commanderGrid, 4, "", "Attack Research", () => ResearchLabel("공격 연구"), () => hud.TryLabResearch(true),
                () => hud.LabResearchLabel(Commander, true) + "\n무기와 같은 보직의 연구소에서 이 장수의 공격을 올립니다.");
            Add(commanderGrid, 7, "D", "Return 1", () => "병력 -1", () => Commander?.ReturnTroops(1),
                () => "건강한 병력 1마리를 대기 개미로 돌려보냅니다. 작업 중에는 바꿀 수 없습니다.");
            Add(commanderGrid, 8, "", "Armor Research", () => ResearchLabel("방어 연구"), () => hud.TryLabResearch(false),
                () => hud.LabResearchLabel(Commander, false) + "\n무기와 같은 보직의 연구소에서 이 장수의 방어를 올립니다.");
            Add(commanderGrid, 9, "", "Details", () => "상세", () => { if (Commander != null) GameMenuController.Instance?.Details(Commander); },
                () => "기술 9종, 열정, 장비를 봅니다. 지휘 한도 = 10 + 지휘 기술 x 2.");
            Add(commanderGrid, 14, "B", "Build", () => "건설", BuildScreen.Open, () => "건설 화면: 건물을 고르고 맡길 장수를 정합니다.");

            Add(colonyGrid, 0, "", "Produce Ant", () => "개미 생산", hud.ProduceAnt, hud.ProduceAntLabel);
            Add(colonyGrid, 1, "", "Upgrade Barracks", () => "병영 강화", hud.UpgradeBarracks, hud.BarracksUpgradeLabel);
            Add(colonyGrid, 2, "", "Training Role", () => "훈련\n" + RoleName(hud.SelectedRole), hud.CycleCombatRole,
                () => "병영 강화에 쓸 보직을 고릅니다.");
            Add(colonyGrid, 3, "", "Unlock Fishing", () => "낚시", hud.ResearchFishing, hud.FishingLabel);
            Add(colonyGrid, 5, "", "Dig Expansion", () => "굴착 확장", hud.DigExpansion, () => "굴착지에 흙을 써서 확장 구역을 엽니다.");
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

        private static string Cooldown(float seconds) => seconds > 0f ? $"\n{Mathf.CeilToInt(seconds)}s" : "";
    }
}
