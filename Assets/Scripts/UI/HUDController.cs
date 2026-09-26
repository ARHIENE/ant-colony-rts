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
        [SerializeField] private QueenChamber queenChamber;
        [SerializeField] private Barracks barracks;
        [SerializeField] private DigSite digSite;
        [SerializeField] private BossHealth boss;

        private readonly Text[] resourceTexts = new Text[4];
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
            if (queenChamber == null) queenChamber = FindFirstObjectByType<QueenChamber>();
            if (barracks == null) barracks = FindFirstObjectByType<Barracks>();
            if (digSite == null) digSite = FindFirstObjectByType<DigSite>();
            if (boss == null) boss = FindFirstObjectByType<BossHealth>();

            BuildCanvas();
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
        public void ProduceAnt() => queenChamber?.TryProduceWorker();
        public string ProduceAntLabel() => (queenChamber != null ? queenChamber.GetProductionLabel() : "No Queen Chamber")
            + "\n여왕방에서 대기 개미를 낳습니다. 장수를 고르고 병력 +1로 배정합니다.";
        public void UpgradeBarracks() => barracks?.TryUpgrade();
        public string BarracksUpgradeLabel() => (barracks != null ? barracks.GetUpgradeLabel() : $"No {selectedRole} Barracks")
            + "\n훈련 보직으로 고른 병영의 훈련을 강화합니다.";
        public void ResearchFishing() => queenChamber?.TryResearchFishing();
        public string FishingLabel() => (GameManager.Instance != null && GameManager.Instance.FishingUnlocked
            ? "Fishing Unlocked" : queenChamber != null ? queenChamber.GetFishingResearchLabel() : "Fishing: Build Queen Chamber")
            + "\n여왕방에서 낚시를 연구하면 낚시터에서 식량을 모읍니다.";
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
            canvasGO.AddComponent<WorldMapPanel>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            // 상단 40px 바: 왼쪽 메뉴(GameMenuController), 가운데 날짜·속도, 오른쪽 자원.
            var top = MenuTheme.Panel(canvasGO.transform, "ResourceBar", new Vector2(0, 1), new Vector2(0, 40), Vector2.zero);
            top.anchorMax = new Vector2(1, 1);
            float[] widths = { 128, 118, 110, 236 };
            var right = -8f;
            for (var i = resourceTexts.Length - 1; i >= 0; i--)
            {
                resourceTexts[i] = CreateText(top, new Vector2(1, 1), new Vector2(widths[i], 32), new Vector2(right, -4));
                resourceTexts[i].fontSize = 14; resourceTexts[i].alignment = TextAnchor.MiddleRight;
                right -= widths[i] + 12;
            }
            bossHealthText = CreateText(canvasGO.transform, new Vector2(.5f, 1f), new Vector2(260f, 22f), new Vector2(0f, -46f));
            bossHealthText.alignment = TextAnchor.UpperCenter;

            HudConsole.Build(canvasGO.transform);
            canvasGO.AddComponent<SelectedUnitPanel>();
            HudConsole.Right.gameObject.AddComponent<CommandCard>().Build(this);
            canvasGO.AddComponent<BuildScreen>();
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

        private void UpdateResourceText()
        {
            if (resourceTexts[0] == null || ResourceManager.Instance == null) return;
            var rm = ResourceManager.Instance;
            string Stock(string label, ResourceType type) =>
                $"<color=#968976>{label}</color> <b>{rm.GetAmount(type):N0}</b><color=#968976>/{rm.GetCapacity(type):N0}</color>";
            resourceTexts[0].text = Stock("식량", ResourceType.Food);
            resourceTexts[1].text = Stock("흙", ResourceType.Soil);
            resourceTexts[2].text = Stock("특수", ResourceType.Special);
            var pool = AntPool.Instance;
            if (pool != null) resourceTexts[3].text = $"<color=#968976>대기</color> <b>{pool.Free}</b>  <color=#968976>배정</color> <b>{pool.Assigned}</b>  <color=#968976>예약</color> <b>{pool.Reserved}</b>  <color=#968976>총</color> <b>{pool.Total}</b>";
        }

        private void ShowVictoryMessage()
        {
            ToastManager.Show("Wild Monster Defeated");
        }

        private void ShowDefeatMessage()
        {
            ToastManager.Show("All colony buildings have been destroyed.");
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

