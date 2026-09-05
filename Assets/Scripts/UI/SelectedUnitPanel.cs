using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public class SelectedUnitPanel : MonoBehaviour
    {
        private SelectionManager selection;
        private GameObject panel;
        private Text title;
        private Text healthText;
        private RectTransform healthFill;

        private void Start()
        {
            selection = FindFirstObjectByType<SelectionManager>();
            var background = CreateImage("SelectedUnitPanel", transform, new Color(0.08f, 0.12f, 0.09f, 0.95f));
            panel = background.gameObject;
            var rect = background.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(10f, 60f);
            rect.sizeDelta = new Vector2(300f, 96f);
            title = CreateText("UnitName", rect, new Vector2(12f, -10f));
            healthText = CreateText("Health", rect, new Vector2(12f, -38f));

            var track = CreateImage("HealthTrack", rect, new Color(0.2f, 0.25f, 0.22f));
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = track.rectTransform.pivot = Vector2.zero;
            track.rectTransform.anchoredPosition = new Vector2(12f, 12f);
            track.rectTransform.sizeDelta = new Vector2(276f, 12f);
            healthFill = CreateImage("HealthFill", track.transform, new Color(0.3f, 0.85f, 0.4f)).rectTransform;
            healthFill.anchorMin = Vector2.zero;
            healthFill.anchorMax = Vector2.one;
            healthFill.offsetMin = healthFill.offsetMax = Vector2.zero;
            panel.SetActive(false);
        }

        private void LateUpdate()
        {
            if (panel == null) return;
            int count = 0;
            float current = 0f;
            float maximum = 0f;
            AntUnitBase first = null;
            if (selection != null)
            {
                foreach (var selectable in selection.GetSelectedObjects())
                {
                    if (selectable == null || !selectable.isActiveAndEnabled || !selectable.IsSelected) continue;
                    var unit = selectable.GetComponent<AntUnitBase>();
                    if (unit == null || !unit.isActiveAndEnabled || unit.IsDead || unit.Data == null) continue;
                    if (first == null) first = unit;
                    count++;
                    current += unit.CurrentHealth;
                    maximum += unit.Data.maxHealth;
                }
            }
            panel.SetActive(count > 0);
            if (count == 0) return;
            title.text = count == 1 ? first.Data.displayName : $"Selected Units: {count}";
            healthText.text = $"HP {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            healthFill.anchorMax = new Vector2(maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f, 1f);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.raycastTarget = false;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(276f, 24f);
            return text;
        }
    }
}
