using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
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
        [SerializeField] private ResearchLab researchLab;
        [SerializeField] private BuildingPlacementController buildingPlacementController;
        [SerializeField] private DigSite digSite;
        [SerializeField] private BossHealth boss;

        private Text resourceText;
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
        private Text buildNurseryButtonText;
        private Text buildScoutPostButtonText;
        private Text buildPrisonButtonText;
        private Text fishingResearchButtonText;
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
            if (researchLab == null) researchLab = FindFirstObjectByType<ResearchLab>();
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
            if (barracks == null || !barracks.isActiveAndEnabled || barracks.Role != selectedRole)
                barracks = FindBarracks(selectedRole);
            if (researchLab == null || !researchLab.isActiveAndEnabled || researchLab.Role != selectedRole)
                researchLab = FindResearchLab(selectedRole);
            if (antProductionButtonText != null)
                antProductionButtonText.text = queenChamber != null ? queenChamber.GetProductionLabel() : "No Queen Chamber";
            if (barracksUpgradeButtonText != null)
                barracksUpgradeButtonText.text = barracks != null ? barracks.GetUpgradeLabel() : $"No {selectedRole} Barracks";
            if (attackResearchButtonText != null)
                attackResearchButtonText.text = researchLab != null ? researchLab.GetAttackResearchLabel() : $"No {selectedRole} Lab";
            if (armorResearchButtonText != null)
                armorResearchButtonText.text = researchLab != null ? researchLab.GetArmorResearchLabel() : $"No {selectedRole} Lab";
            if (roleButtonText != null) roleButtonText.text = $"Role: {selectedRole}";
            if (buildBarracksButtonText != null && buildingPlacementController != null)
                buildBarracksButtonText.text = buildingPlacementController.GetBarracksBuildLabel(selectedRole);
            if (buildLabButtonText != null && buildingPlacementController != null)
                buildLabButtonText.text = buildingPlacementController.GetResearchLabBuildLabel(selectedRole);
            if (buildFarmButtonText != null && buildingPlacementController != null)
                buildFarmButtonText.text = buildingPlacementController.GetFarmBuildLabel();
            if (buildNurseryButtonText != null && buildingPlacementController != null)
                buildNurseryButtonText.text = buildingPlacementController.GetNurseryBuildLabel();
            if (buildScoutPostButtonText != null && buildingPlacementController != null)
                buildScoutPostButtonText.text = buildingPlacementController.GetScoutPostBuildLabel();
            if (buildPrisonButtonText != null && buildingPlacementController != null)
                buildPrisonButtonText.text = buildingPlacementController.GetPrisonerCampBuildLabel();
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

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            resourceText = CreateText(canvasGO.transform, new Vector2(0f, 1f), new Vector2(650f, 44f), new Vector2(10f, -10f));
            messageText = CreateText(canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(300f, 20f), new Vector2(0f, -35f));
            bossHealthText = CreateText(canvasGO.transform, new Vector2(1f, 1f), new Vector2(220f, 20f), new Vector2(-10f, -10f));
            bossHealthText.alignment = TextAnchor.UpperRight;

            roleButtonText = CreateButton(canvasGO.transform, new Vector2(10f, 55f), $"Role: {selectedRole}", CycleCombatRole);
            fishingResearchButtonText = CreateButton(canvasGO.transform, new Vector2(150f, 55f), "Unlock Fishing", () => queenChamber?.TryResearchFishing());
            antProductionButtonText = CreateButton(canvasGO.transform, new Vector2(10f, 10f), "Produce Ant", () => queenChamber?.TryProduceWorker());
            var upgradeLabel = barracks != null ? barracks.GetUpgradeLabel() : "Upgrade Barracks";
            barracksUpgradeButtonText = CreateButton(canvasGO.transform, new Vector2(290f, 10f), upgradeLabel, () => barracks?.TryUpgrade());
            CreateButton(canvasGO.transform, new Vector2(430f, 10f), "Dig Expansion", () => digSite?.TryExpand());
            var attackResearchLabel = researchLab != null ? researchLab.GetAttackResearchLabel() : "No Research Lab";
            attackResearchButtonText = CreateButton(canvasGO.transform, new Vector2(570f, 10f), attackResearchLabel, () => researchLab?.TryResearchAttack());
            var armorResearchLabel = researchLab != null ? researchLab.GetArmorResearchLabel() : "No Research Lab";
            armorResearchButtonText = CreateButton(canvasGO.transform, new Vector2(710f, 10f), armorResearchLabel, () => researchLab?.TryResearchArmor());
            var buildBarracksLabel = buildingPlacementController != null ? buildingPlacementController.GetBarracksBuildLabel() : "Build Barracks";
            buildBarracksButtonText = CreateButton(canvasGO.transform, new Vector2(850f, 10f), buildBarracksLabel, () => buildingPlacementController?.BeginBarracksPlacement(selectedRole));
            var buildLabLabel = buildingPlacementController != null ? buildingPlacementController.GetResearchLabBuildLabel() : "Build Lab";
            buildLabButtonText = CreateButton(canvasGO.transform, new Vector2(990f, 10f), buildLabLabel, () => buildingPlacementController?.BeginResearchLabPlacement(selectedRole));
            var buildFarmLabel = buildingPlacementController != null ? buildingPlacementController.GetFarmBuildLabel() : "Build Farm";
            buildFarmButtonText = CreateButton(canvasGO.transform, new Vector2(1130f, 10f), buildFarmLabel, () => buildingPlacementController?.BeginFarmPlacement());

            // 장수 획득 건물 3종. 아래 줄이 가득 차 병영 줄 위에 놓는다.
            buildNurseryButtonText = CreateButton(canvasGO.transform, new Vector2(290f, 55f), "Build Nursery",
                () => buildingPlacementController?.BeginNurseryPlacement());
            buildScoutPostButtonText = CreateButton(canvasGO.transform, new Vector2(430f, 55f), "Build Scout Post",
                () => buildingPlacementController?.BeginScoutPostPlacement());
            buildPrisonButtonText = CreateButton(canvasGO.transform, new Vector2(570f, 55f), "Build Prison",
                () => buildingPlacementController?.BeginPrisonerCampPlacement());
        }

        private void CycleCombatRole()
        {
            var index = System.Array.IndexOf(CombatRoles, selectedRole);
            selectedRole = CombatRoles[(index + 1) % CombatRoles.Length];
            barracks = null;
            researchLab = null;
        }

        private static Barracks FindBarracks(UnitRole role)
        {
            foreach (var candidate in FindObjectsByType<Barracks>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.Role == role) return candidate;
            return null;
        }

        private static ResearchLab FindResearchLab(UnitRole role)
        {
            foreach (var candidate in FindObjectsByType<ResearchLab>(FindObjectsSortMode.None))
                if (candidate.isActiveAndEnabled && candidate.Role == role) return candidate;
            return null;
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
            return text;
        }

        private Text CreateButton(Transform parent, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
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
            button.onClick.AddListener(onClick);

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
            return text;
        }

        private void UpdateResourceText()
        {
            if (resourceText == null || ResourceManager.Instance == null) return;
            var rm = ResourceManager.Instance;
            resourceText.text =
                $"Food {rm.GetAmount(ResourceType.Food)} / {rm.GetCapacity(ResourceType.Food)}   " +
                $"Soil {rm.GetAmount(ResourceType.Soil)} / {rm.GetCapacity(ResourceType.Soil)}   " +
                $"Special {rm.GetAmount(ResourceType.Special)} / {rm.GetCapacity(ResourceType.Special)}";
            if (AntPool.Instance != null) resourceText.text += $"\nAnts {AntPool.Instance.Total} total / {AntPool.Instance.Free} free / {AntPool.Instance.Assigned} assigned / {AntPool.Instance.Reserved} building";
        }

        private void ShowVictoryMessage()
        {
            if (messageText == null) return;
            messageText.text = "Loop Complete: Wild Monster Defeated!";
        }

        private void ShowDefeatMessage()
        {
            if (messageText == null) return;
            messageText.text = "Defeat: All Buildings Destroyed!";
        }

        private void UpdateBossHealthText(float current, float max)
        {
            if (bossHealthText == null) return;
            bossHealthText.text = $"Boss HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void ShowBossDefeatedMessage()
        {
            if (bossHealthText != null) bossHealthText.text = "Boss Defeated!";
            if (messageText != null) messageText.text = "Raid Complete: Boss Defeated!";
        }
    }
}

