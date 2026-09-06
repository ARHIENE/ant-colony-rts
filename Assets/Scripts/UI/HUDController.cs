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
        private Text barracksProductionButtonText;
        private Text barracksUpgradeButtonText;
        private Text attackResearchButtonText;
        private Text armorResearchButtonText;

        private void Start()
        {
            if (queenChamber == null) queenChamber = FindFirstObjectByType<QueenChamber>();
            if (barracks == null) barracks = FindFirstObjectByType<Barracks>();
            if (researchLab == null) researchLab = FindFirstObjectByType<ResearchLab>();
            if (buildingPlacementController == null) buildingPlacementController = FindFirstObjectByType<BuildingPlacementController>();
            if (digSite == null) digSite = FindFirstObjectByType<DigSite>();
            if (boss == null) boss = FindFirstObjectByType<BossHealth>();

            BuildCanvas();

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourcesChanged += UpdateResourceText;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoopComplete += ShowVictoryMessage;
                GameManager.Instance.OnBossDefeated += ShowBossDefeatedMessage;
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
            if (barracks == null) barracks = FindFirstObjectByType<Barracks>();
            if (researchLab == null) researchLab = FindFirstObjectByType<ResearchLab>();
            if (barracksProductionButtonText != null && barracks != null)
                barracksProductionButtonText.text = barracks.GetProductionLabel();
            if (barracksUpgradeButtonText != null && barracks != null)
                barracksUpgradeButtonText.text = barracks.GetUpgradeLabel();
            if (attackResearchButtonText != null && researchLab != null)
                attackResearchButtonText.text = researchLab.GetAttackResearchLabel();
            if (armorResearchButtonText != null && researchLab != null)
                armorResearchButtonText.text = researchLab.GetArmorResearchLabel();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<SelectedUnitPanel>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            resourceText = CreateText(canvasGO.transform, new Vector2(0f, 1f), new Vector2(160f, 20f), new Vector2(10f, -10f));
            messageText = CreateText(canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(300f, 20f), new Vector2(0f, -10f));
            bossHealthText = CreateText(canvasGO.transform, new Vector2(1f, 1f), new Vector2(220f, 20f), new Vector2(-10f, -10f));
            bossHealthText.alignment = TextAnchor.UpperRight;

            CreateButton(canvasGO.transform, new Vector2(10f, 10f), "Produce Worker", () => queenChamber?.TryProduceWorker());
            var barracksLabel = barracks != null ? barracks.GetProductionLabel() : "Produce Combat Ant";
            barracksProductionButtonText = CreateButton(canvasGO.transform, new Vector2(150f, 10f), barracksLabel, () => barracks?.TryProduceSoldier());
            var upgradeLabel = barracks != null ? barracks.GetUpgradeLabel() : "Upgrade Barracks";
            barracksUpgradeButtonText = CreateButton(canvasGO.transform, new Vector2(290f, 10f), upgradeLabel, () => barracks?.TryUpgrade());
            CreateButton(canvasGO.transform, new Vector2(430f, 10f), "Dig Expansion", () => digSite?.TryExpand());
            var attackResearchLabel = researchLab != null ? researchLab.GetAttackResearchLabel() : "No Research Lab";
            attackResearchButtonText = CreateButton(canvasGO.transform, new Vector2(570f, 10f), attackResearchLabel, () => researchLab?.TryResearchAttack());
            var armorResearchLabel = researchLab != null ? researchLab.GetArmorResearchLabel() : "No Research Lab";
            armorResearchButtonText = CreateButton(canvasGO.transform, new Vector2(710f, 10f), armorResearchLabel, () => researchLab?.TryResearchArmor());
            var buildBarracksLabel = buildingPlacementController != null ? buildingPlacementController.GetBarracksBuildLabel() : "Build Barracks";
            CreateButton(canvasGO.transform, new Vector2(850f, 10f), buildBarracksLabel, () => buildingPlacementController?.BeginBarracksPlacement());
            var buildLabLabel = buildingPlacementController != null ? buildingPlacementController.GetResearchLabBuildLabel() : "Build Lab";
            CreateButton(canvasGO.transform, new Vector2(990f, 10f), buildLabLabel, () => buildingPlacementController?.BeginResearchLabPlacement());
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
                $"Soil {rm.GetAmount(ResourceType.Soil)} / {rm.GetCapacity(ResourceType.Soil)}";
        }

        private void ShowVictoryMessage()
        {
            if (messageText == null) return;
            messageText.text = "Loop Complete: Wild Monster Defeated!";
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
