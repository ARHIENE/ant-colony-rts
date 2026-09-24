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
        [SerializeField] private BuildingPlacementController buildingPlacementController;
        [SerializeField] private DigSite digSite;
        [SerializeField] private BossHealth boss;

        private readonly Text[] resourceTexts = new Text[4];
        private Text messageText;
        private Text bossHealthText;
        private Text antProductionButtonText;
        private Text barracksUpgradeButtonText;
        private Text attackResearchButtonText;
        private Text armorResearchButtonText;
        private Text roleButtonText;
        private Text buildBarracksButtonText;
        private Text buildLabButtonText;
        private Text buildFarmButtonText;
        private Text buildStorageButtonText;
        private Text buildNurseryButtonText;
        private Text buildScoutPostButtonText;
        private Text buildPrisonButtonText;
        private Text buildAcidTowerButtonText;
        private Text fishingResearchButtonText;
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
            if (buildingPlacementController == null) buildingPlacementController = FindFirstObjectByType<BuildingPlacementController>();
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
            var commander = SelectedCommander;
            if (messageText != null) messageText.text = commander != null
                ? commander.CommanderName + " / " + commander.Role
                : "Select one commander to research";
            if (antProductionButtonText != null)
                antProductionButtonText.text = queenChamber != null ? queenChamber.GetProductionLabel() : "No Queen Chamber";
            if (barracksUpgradeButtonText != null)
                barracksUpgradeButtonText.text = barracks != null ? barracks.GetUpgradeLabel() : $"No {selectedRole} Barracks";
            if (attackResearchButtonText != null)
                attackResearchButtonText.text = GetLabResearchLabel(commander, true);
            if (armorResearchButtonText != null)
                armorResearchButtonText.text = GetLabResearchLabel(commander, false);
            if (roleButtonText != null) roleButtonText.text = $"Training: {selectedRole}";
            if (buildBarracksButtonText != null && buildingPlacementController != null)
                buildBarracksButtonText.text = buildingPlacementController.GetBarracksBuildLabel(selectedRole);
            if (buildLabButtonText != null && buildingPlacementController != null)
                buildLabButtonText.text = buildingPlacementController.GetResearchLabBuildLabel(selectedRole);
            if (buildFarmButtonText != null && buildingPlacementController != null)
                buildFarmButtonText.text = buildingPlacementController.GetFarmBuildLabel();
            if (buildStorageButtonText != null && buildingPlacementController != null)
                buildStorageButtonText.text = buildingPlacementController.GetStorageBuildLabel();
            if (buildNurseryButtonText != null && buildingPlacementController != null)
                buildNurseryButtonText.text = buildingPlacementController.GetNurseryBuildLabel();
            if (buildScoutPostButtonText != null && buildingPlacementController != null)
                buildScoutPostButtonText.text = buildingPlacementController.GetScoutPostBuildLabel();
            if (buildPrisonButtonText != null && buildingPlacementController != null)
                buildPrisonButtonText.text = buildingPlacementController.GetPrisonerCampBuildLabel();
            if (buildAcidTowerButtonText != null && buildingPlacementController != null)
                buildAcidTowerButtonText.text = buildingPlacementController.GetAcidTowerBuildLabel();
            if (fishingResearchButtonText != null)
            {

                fishingResearchButtonText.text = GameManager.Instance != null && GameManager.Instance.FishingUnlocked
                    ? "Fishing Unlocked" : queenChamber != null ? queenChamber.GetFishingResearchLabel() : "Fishing: Build Queen Chamber";
            }
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<SelectedUnitPanel>();
            canvasGO.AddComponent<CommanderAcquisitionPanel>();
            canvasGO.AddComponent<WorldMapPanel>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            var top = MenuTheme.Panel(canvasGO.transform, "ResourceBar", new Vector2(0, 1), new Vector2(1280, 64), Vector2.zero);
            for (var i = 0; i < resourceTexts.Length; i++)
            {
                resourceTexts[i] = CreateText(top, new Vector2(0, 1), new Vector2(153, 48), new Vector2(14 + i * 163, -9));
                resourceTexts[i].fontSize = 14;
            }
            resourceTexts[3].fontSize = 12;
            CommandPanel(canvasGO.transform, "COLONY", 0, 420);
            CommandPanel(canvasGO.transform, "COMMANDER RESEARCH", 420, 280);
            CommandPanel(canvasGO.transform, "CONSTRUCTION", 700, 580);
            messageText = CreateText(canvasGO.transform, Vector2.zero, new Vector2(260, 32), new Vector2(430, 10));
            messageText.text = "Select one commander to research";
            messageText.fontSize = 12; messageText.color = MenuTheme.Muted;
            bossHealthText = CreateText(canvasGO.transform, new Vector2(1f, 1f), new Vector2(220f, 20f), new Vector2(-10f, -10f));
            bossHealthText.alignment = TextAnchor.UpperRight;

            roleButtonText = CreateButton(canvasGO.transform, new Vector2(150f, 55f), $"Training: {selectedRole}", CycleCombatRole,
                "Choose the equipment category for training and lab construction.");
            fishingResearchButtonText = CreateButton(canvasGO.transform, new Vector2(290f, 55f), "Unlock Fishing", () => queenChamber?.TryResearchFishing(),
                "Research fishing at the Queen Chamber to gather food from fishing spots.");
            antProductionButtonText = CreateButton(canvasGO.transform, new Vector2(10f, 55f), "Produce Ant", () => queenChamber?.TryProduceWorker(),
                "Produce an unassigned ant at the Queen Chamber. Select a commander and use +1 Ant to assign troops.");
            var upgradeLabel = barracks != null ? barracks.GetUpgradeLabel() : "Upgrade Barracks";
            barracksUpgradeButtonText = CreateButton(canvasGO.transform, new Vector2(10f, 10f), upgradeLabel, () => barracks?.TryUpgrade(),
                "Upgrade training for the chosen equipment category. Costs are shown on the button.");
            CreateButton(canvasGO.transform, new Vector2(150f, 10f), "Dig Expansion", () => digSite?.TryExpand(),
                "Spend soil at the dig site to open the expansion zone.");
            attackResearchButtonText = CreateButton(canvasGO.transform, new Vector2(430f, 55f), GetLabResearchLabel(null, true), () => TryLabResearch(true),
                "Select one commander and build a lab matching their weapon. Research improves that commander's attack.");
            armorResearchButtonText = CreateButton(canvasGO.transform, new Vector2(570f, 55f), GetLabResearchLabel(null, false), () => TryLabResearch(false),
                "Select one commander and build a lab matching their weapon. Research improves that commander's armor.");
            var buildBarracksLabel = buildingPlacementController != null ? buildingPlacementController.GetBarracksBuildLabel() : "Build Barracks";
            buildBarracksButtonText = CreateButton(canvasGO.transform, new Vector2(710f, 55f), buildBarracksLabel, () => buildingPlacementController?.BeginBarracksPlacement(selectedRole), PlacementTip);
            var buildLabLabel = buildingPlacementController != null ? buildingPlacementController.GetResearchLabBuildLabel() : "Build Lab";
            buildLabButtonText = CreateButton(canvasGO.transform, new Vector2(850f, 55f), buildLabLabel, () => buildingPlacementController?.BeginResearchLabPlacement(selectedRole), PlacementTip);
            var buildFarmLabel = buildingPlacementController != null ? buildingPlacementController.GetFarmBuildLabel() : "Build Farm";
            buildFarmButtonText = CreateButton(canvasGO.transform, new Vector2(990f, 55f), buildFarmLabel, () => buildingPlacementController?.BeginFarmPlacement(), PlacementTip);

            buildNurseryButtonText = CreateButton(canvasGO.transform, new Vector2(710f, 10f), "Build Nursery",
                () => buildingPlacementController?.BeginNurseryPlacement(), PlacementTip);
            buildScoutPostButtonText = CreateButton(canvasGO.transform, new Vector2(850f, 10f), "Build Scout Post",
                () => buildingPlacementController?.BeginScoutPostPlacement(), PlacementTip);
            buildPrisonButtonText = CreateButton(canvasGO.transform, new Vector2(990f, 10f), "Build Prison",
                () => buildingPlacementController?.BeginPrisonerCampPlacement(), PlacementTip);
            buildAcidTowerButtonText = CreateButton(canvasGO.transform, new Vector2(1130f, 55f), "Acid Tower", () => {
                if (buildingPlacementController == null || !buildingPlacementController.BeginAcidTowerPlacement())
                    ToastManager.Show("Select a commander with troops at home to build an acid tower.");
            }, "Single-target acid tower: automatically attacks nearby enemies. No upkeep. " + PlacementTip);
            buildStorageButtonText = CreateButton(canvasGO.transform, new Vector2(1130f, 10f), "Build Storage",
                () => buildingPlacementController?.BeginStoragePlacement(),
                "Expand food, soil and special storage and add a nearby drop-off point. " + PlacementTip);
        }

        private void CommandPanel(Transform parent, string title, float x, float width)
        {
            var panel = MenuTheme.Panel(parent, title, Vector2.zero, new Vector2(width - 4, 128), new Vector2(x + 2, 0));
            var heading = CreateText(panel, new Vector2(0, 1), new Vector2(width - 24, 22), new Vector2(10, -9));
            heading.text = title; heading.fontSize = 12; heading.fontStyle = FontStyle.Bold; heading.color = MenuTheme.Accent;
        }

        private void CycleCombatRole()
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
        private string GetLabResearchLabel(CommanderAnt commander, bool attack)
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
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperLeft;
            text.raycastTarget = false;
            return text;
        }

        private const string PlacementTip = "Choose a build location, then left-click to place. Right-click cancels. Resources and free ants are required.";

        private Text CreateButton(Transform parent, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick, string tip)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(130f, 40f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);

            var button = go.AddComponent<Button>();
            MenuTheme.StyleButton(button);
            button.onClick.AddListener(onClick);
            go.AddComponent<MenuTooltip>().Message = tip;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(go.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = label;
            text.raycastTarget = false;
            return text;
        }

        private void UpdateResourceText()
        {
            if (resourceTexts[0] == null || ResourceManager.Instance == null) return;
            var rm = ResourceManager.Instance;
            resourceTexts[0].text = $"<color=#8ED69C>FOOD</color>\n<b>{rm.GetAmount(ResourceType.Food)}</b> <color=#A6BABA>/ {rm.GetCapacity(ResourceType.Food)}</color>";
            resourceTexts[1].text = $"<color=#D8B889>SOIL</color>\n<b>{rm.GetAmount(ResourceType.Soil)}</b> <color=#A6BABA>/ {rm.GetCapacity(ResourceType.Soil)}</color>";
            resourceTexts[2].text = $"<color=#C2ADF2>SPECIAL</color>\n<b>{rm.GetAmount(ResourceType.Special)}</b> <color=#A6BABA>/ {rm.GetCapacity(ResourceType.Special)}</color>";
            if (AntPool.Instance != null) resourceTexts[3].text = $"<color=#84CDBA>COLONY {AntPool.Instance.Total}</color>\n<b>{AntPool.Instance.Free}</b> free / {AntPool.Instance.Assigned} troops\n{AntPool.Instance.Reserved} building";
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

