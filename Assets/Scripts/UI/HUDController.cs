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
        [SerializeField] private DigSite digSite;

        private Text resourceText;
        private Text messageText;

        private void Start()
        {
            BuildCanvas();

            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourcesChanged += UpdateResourceText;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLoopComplete += ShowVictoryMessage;
            }

            UpdateResourceText();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>();
            }

            resourceText = CreateText(canvasGO.transform, new Vector2(0f, 1f), new Vector2(160f, 20f), new Vector2(10f, -10f));
            messageText = CreateText(canvasGO.transform, new Vector2(0.5f, 1f), new Vector2(300f, 20f), new Vector2(0f, -10f));

            CreateButton(canvasGO.transform, new Vector2(10f, 10f), "Produce Worker", () => queenChamber?.TryProduceWorker());
            CreateButton(canvasGO.transform, new Vector2(150f, 10f), "Produce Soldier", () => barracks?.TryProduceSoldier());
            CreateButton(canvasGO.transform, new Vector2(290f, 10f), "Dig Expansion", () => digSite?.TryExpand());
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

        private void CreateButton(Transform parent, Vector2 anchoredPosition, string label, UnityEngine.Events.UnityAction onClick)
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
    }
}
