using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AntColony.UI
{
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Barracks barracks;
        [SerializeField] private DigSite digSite;
        [SerializeField] private BossHealth boss;

        private readonly Text[] resourceTexts = new Text[3];
        private Image demandFill;
        private Text bossHealthText;
        private SelectionManager selectionManager;
        private UnitRole selectedRole = UnitRole.Melee;
        private static readonly UnitRole[] CombatRoles =
        {
            UnitRole.Melee,
            UnitRole.Ranged,
            UnitRole.Defense,
            UnitRole.Flying,
            UnitRole.Support
        };

        private void Start()
        {
            if (barracks == null) barracks = FindFirstObjectByType<Barracks>();
            if (digSite == null) digSite = FindFirstObjectByType<DigSite>();
            if (boss == null) boss = FindFirstObjectByType<BossHealth>();

            BuildCanvas();
            gameObject.AddComponent<AntColony.Map.DayNightLighting>();
            if (AntPool.Instance != null) AntPool.Instance.OnPoolChanged += UpdateResourceText;

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourcesChanged += UpdateResourceText;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoopComplete += ShowVictoryMessage;
                GameManager.Instance.OnBossDefeated += ShowBossDefeatedMessage;
                GameManager.Instance.OnDefeat += ShowDefeatMessage;
            }
            if (boss != null)
            {
                boss.onHPChanged.AddListener(UpdateBossHealthText);
                UpdateBossHealthText(boss.CurrentHp, boss.MaxHp);
            }

            UpdateResourceText();
        }

        private void Update()
        {
            if (Time.frameCount % 30 == 0) UpdateResourceText(); // 민심·수요는 시간에 따라 바뀐다.
            if (AntColony.World.WorldMapManager.Instance != null)
            {
                var site = AntColony.World.WorldMapManager.Instance.ViewedSite;
                boss = site != null ? site.Boss : null;
                if (boss != null) UpdateBossHealthText(boss.CurrentHp, boss.MaxHp);
                else if (bossHealthText != null) bossHealthText.text = "";
            }
            if (barracks == null || !barracks.isActiveAndEnabled || barracks.Role != selectedRole)
                barracks = FindBarracks(selectedRole);
        }

        // 둥지 명령(커맨드 카드에서 장수를 고르지 않았을 때). 버튼 문구는 짧게, 비용·상태는 도움말로 보인다.
        public UnitRole SelectedRole => selectedRole;
        public void UpgradeBarracks() => barracks?.TryUpgrade();
        public string BarracksUpgradeLabel() => (barracks != null ? barracks.GetUpgradeLabel() : $"No {selectedRole} Barracks")
            + "\n훈련 보직으로 고른 병영의 훈련을 강화합니다.";
        public void ResearchFishing() => GameMenuController.Instance?.Science();
        public string FishingLabel() => (GameManager.Instance != null && GameManager.Instance.FishingUnlocked
            ? "Fishing Unlocked" : "과학 트리에서 낚시 연구")
            + "\n과학 연구소에서 낚시를 연구하면 낚시터에서 식량을 모읍니다.";
        public void DigExpansion() => digSite?.TryExpand();

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440f, 900f);
            scaler.matchWidthOrHeight = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<CommanderAcquisitionPanel>();
            canvasGO.AddComponent<WorkTargetPanel>();
            canvasGO.AddComponent<WorldMapPanel>();
            canvasGO.AddComponent<MigrationOfferPanel>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            // 상단 40px 바(HUD v3): 왼쪽 메뉴(GameMenuController), 가운데 장수 바, 오른쪽 식량 · 재료(목록) | 인구.
            // 저장 한도는 마우스 오버, 재료는 눌러서 목록. 인구 숫자 색 = 민심, 아래 막대 = 이주 수요, 누르면 인구 창(Phase 4).
            var top = MenuTheme.Panel(canvasGO.transform, "ResourceBar", new Vector2(0, 1), new Vector2(0, 40), Vector2.zero);
            top.anchorMax = new Vector2(1, 1);
            resourceTexts[2] = ResourceButton(top, "Population", 86, -8, () => GameMenuController.Instance?.Population());
            demandFill = MenuLayout.Box(resourceTexts[2].transform.parent, "DemandBar", 6, 24, 74, 3, MenuTheme.Hp).GetComponent<Image>();
            demandFill.raycastTarget = false;
            var sep = MenuTheme.Rect("Separator", top); sep.anchorMin = sep.anchorMax = new Vector2(1, 1); sep.pivot = new Vector2(1, .5f);
            sep.sizeDelta = new Vector2(1, 20); sep.anchoredPosition = new Vector2(-104, -27);
            sep.gameObject.AddComponent<Image>().color = MenuTheme.Line;
            resourceTexts[1] = ResourceButton(top, "Materials", 92, -114, ToggleMaterials);
            resourceTexts[0] = ResourceButton(top, "Food", 96, -214, null);
            materialsList = MenuTheme.Panel(canvasGO.transform, "MaterialsList", new Vector2(1, 1), new Vector2(200, 80), new Vector2(-114, -100));
            var listText = MenuTheme.Text(materialsList, "", 12); MenuTheme.Stretch(listText.rectTransform);
            listText.rectTransform.offsetMin = new Vector2(10, 6); listText.rectTransform.offsetMax = new Vector2(-10, -6);
            listText.alignment = TextAnchor.UpperLeft; listText.supportRichText = true; listText.name = "MaterialsListText";
            materialsList.gameObject.SetActive(false);
            bossHealthText = CreateText(canvasGO.transform, new Vector2(.5f, 1f), new Vector2(260f, 22f), new Vector2(0f, -46f));
            bossHealthText.alignment = TextAnchor.UpperCenter;
            HudClock.Create(canvasGO.transform);
            RosterBar.Create(canvasGO.transform);

            HudConsole.Build(canvasGO.transform);
            DetailTabs.Create(canvasGO.transform);
            canvasGO.AddComponent<SelectedUnitPanel>();
            HudConsole.Right.gameObject.AddComponent<CommandCard>().Build(this);
            canvasGO.AddComponent<BuildScreen>();
            canvasGO.AddComponent<HudOverview>();
            canvasGO.AddComponent<HudResponsiveLayout>();
        }

        public void CycleCombatRole()
        {
            var index = System.Array.IndexOf(CombatRoles, selectedRole);
            selectedRole = CombatRoles[(index + 1) % CombatRoles.Length];
            barracks = null;
        }

        private CommanderAnt SelectedCommander
        {
            get
            {
                if (selectionManager == null) selectionManager = FindFirstObjectByType<SelectionManager>();
                return SelectedUnitPanel.FindSingleSelectedCommander(selectionManager);
            }
        }

        // 연구소 강화는 선택된 장수 한 명의 현재 보직과 같은 역할의 연구소에서 진행한다.
        public string LabResearchLabel(CommanderAnt commander, bool attack)
        {
            if (commander == null) return attack ? "ATK: Select 1 Commander" : "Armor: Select 1 Commander";
            var lab = FindResearchLab(commander.Role);
            if (lab == null)
            {
                var level = attack ? commander.LabAttackLevel : commander.LabArmorLevel;
                return $"{(attack ? "ATK" : "Armor")} Lv{level}\nNo {commander.Role} Lab";
            }
            return attack ? lab.GetAttackResearchLabel(commander) : lab.GetArmorResearchLabel(commander);
        }

        public bool TryLabResearch(bool attack)
        {
            var commander = SelectedCommander;
            var lab = commander != null ? FindResearchLab(commander.Role) : null;
            if (lab == null) return false;
            return attack ? lab.TryResearchAttack(commander) : lab.TryResearchArmor(commander);
        }

        private static Barracks FindBarracks(UnitRole role)
        {
            foreach (var candidate in FindObjectsByType<Barracks>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.Role == role) return candidate;
            return null;
        }

        // 같은 역할 연구소가 여럿이면 쉬고 있는 곳을 우선한다.
        private static ResearchLab FindResearchLab(UnitRole role)
        {
            ResearchLab busy = null;
            foreach (var candidate in FindObjectsByType<ResearchLab>(FindObjectsSortMode.None))
            {
                if (!candidate.isActiveAndEnabled || candidate.Role != role) continue;
                if (!candidate.IsResearching) return candidate;
                if (busy == null) busy = candidate;
            }
            return busy;
        }

        private Text CreateText(Transform parent, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var text = go.AddComponent<Text>();
            text.font = MenuTheme.Font;
            text.fontSize = 16;
            text.color = MenuTheme.TextColor;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }

        // 상단 자원 칸: 오른쪽 끝 기준 x에 놓는 글자 버튼. 클릭 동작이 없으면 버튼 없이 도움말만.
        private Text ResourceButton(RectTransform top, string name, float width, float right, UnityEngine.Events.UnityAction click)
        {
            var rect = MenuTheme.Rect(name, top);
            // 위쪽 54px 줄에 고정: 좁은 화면에서 상단 바가 두 줄(아래 줄 = 장수 바)이 돼도 겹치지 않게.
            rect.anchorMin = rect.anchorMax = new Vector2(1, 1); rect.pivot = new Vector2(1, .5f);
            rect.sizeDelta = new Vector2(width, 28); rect.anchoredPosition = new Vector2(right, -27);
            var image = rect.gameObject.AddComponent<Image>();
            if (click != null) { var button = rect.gameObject.AddComponent<Button>(); MenuTheme.StyleButton(button); button.onClick.AddListener(click); }
            else image.color = Color.clear;
            rect.gameObject.AddComponent<MenuTooltip>();
            var text = MenuTheme.Text(rect, "", 14); MenuTheme.Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(6, 0); text.rectTransform.offsetMax = new Vector2(-6, 0);
            text.alignment = TextAnchor.MiddleRight; text.supportRichText = true;
            return text;
        }

        private RectTransform materialsList;
        public void ToggleMaterials() => materialsList.gameObject.SetActive(!materialsList.gameObject.activeSelf);

        private void UpdateResourceText()
        {
            if (resourceTexts[0] == null || ResourceManager.Instance == null) return;
            var rm = ResourceManager.Instance;
            string Tip(ResourceType type, string label) => $"{label} {rm.GetAmount(type):N0} / 저장 한도 {rm.GetCapacity(type):N0}";
            resourceTexts[0].text = $"<color=#e5bd6b>식량</color> <b>{rm.GetAmount(ResourceType.Food):N0}</b>";
            resourceTexts[0].transform.parent.GetComponent<MenuTooltip>().Message = Tip(ResourceType.Food, "식량");
            resourceTexts[1].text = $"<color=#968976>재료</color> <b>{rm.GetAmount(ResourceType.Soil):N0}</b> ▾";
            var list = $"{Tip(ResourceType.Soil, "재료")}\n{Tip(ResourceType.Special, "특수")}";
            resourceTexts[1].transform.parent.GetComponent<MenuTooltip>().Message = "재료 종류는 미정입니다.\n" + list;
            materialsList.GetComponentInChildren<Text>().text = "<b>자원</b> (재료 종류 미정)\n" + list;
            var pool = AntPool.Instance;
            if (pool == null) return;
            // Phase 4: 인구 = 어린·성체·늙은 개미 합. 숫자 색 = 민심, 막대 = 이주 수요. 내역은 도움말로.
            var pop = ColonyPopulation.Instance;
            if (pop == null) { resourceTexts[2].text = $"<color=#968976>인구</color> <b>{pool.Total}</b>"; return; }
            var color = pop.Unrest ? "#d9534f" : pop.S.sentiment < 40 ? "#e5bd6b" : "#efe7da";
            resourceTexts[2].text = $"<color=#968976>인구</color> <b><color={color}>{pop.Total}</color></b>";
            demandFill.rectTransform.sizeDelta = new Vector2(74 * Mathf.Clamp01(pop.Demand / 100f), 3);
            resourceTexts[2].transform.parent.GetComponent<MenuTooltip>().Message = $"인구 {pop.Total} / 살 자리 {pop.HousingCapacity} · 어린 {pop.S.young} · 성체 {pool.Total} · 늙은 {pop.S.old}"
                + $"\n민심 {pop.S.sentiment:0} · 이주 수요 {pop.Demand:0} · 세금 {pop.S.taxRate:P0} (다음 납세 {pop.NextTaxSeconds:0}초: 식량 +{pop.WeeklyFood} · 재료 +{pop.WeeklySoil}) · {ColonyPopulation.PolicyName(pop.S.policy)}\n누르면 인구 창";
        }

        private void ShowVictoryMessage()
        {
            ToastManager.Show("Wild Monster Defeated");
        }

        private void ShowDefeatMessage()
        {
            ToastManager.Show("활동 가능한 장수가 없습니다.");
        }

        private void UpdateBossHealthText(float current, float max)
        {
            if (bossHealthText == null) return;
            bossHealthText.text = $"Boss HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void ShowBossDefeatedMessage()
        {
            if (bossHealthText != null) bossHealthText.text = "Boss Defeated!";
            ToastManager.Show("Raid Complete: Boss Defeated!");
        }
    }
}

