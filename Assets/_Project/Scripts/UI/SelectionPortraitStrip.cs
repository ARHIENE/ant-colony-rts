using System.Linq;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed class SelectionPortraitStrip : MonoBehaviour
    {
        private CommanderAnt[] displayed = System.Array.Empty<CommanderAnt>();
        private RectTransform strip;
        private SelectionManager selection;
        private readonly System.Collections.Generic.Dictionary<CommanderAnt, RectTransform> health = new();
        private void Start()
        {
            selection = SceneObjects.Any<SelectionManager>();
            strip = MenuTheme.Rect("Selected Portraits", transform);
            strip.anchorMin = strip.anchorMax = strip.pivot = Vector2.zero;
            strip.anchoredPosition = new Vector2(0, 132);
        }
        private void LateUpdate()
        {
            if (strip == null) return;
            var units = HudOverview.Selected(selection);
            strip.gameObject.SetActive(units.Length > 1);
            if (units.Length <= 1) return;
            float width = ((RectTransform)transform).rect.width;
            int slots = Mathf.Max(2, Mathf.Min(8, Mathf.FloorToInt(width / 50) - (units.Length > 8 ? 1 : 0)));
            var shown = units.Take(slots).ToArray();
            if (!displayed.SequenceEqual(shown) || strip.childCount != shown.Length + (units.Length > slots ? 1 : 0))
            {
                displayed = shown;
                health.Clear();
                foreach (Transform child in strip) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                for (int i = 0; i < shown.Length; i++)
                {
                    var c = shown[i];
                    var button = MenuLayout.Button(strip, "Selected " + c.CommanderName, "", i * 50, 0, 46, 46,
                        () => selection.SelectOnly(c.GetComponent<SelectableObject>()), c.CommanderName + " · " + CommanderOverhead.Activity(c));
                    var face = CharacterPortraits.Get(c);
                    var rt = MenuLayout.Place(MenuTheme.Rect("Face", button.transform), 3, 2, 40, 40);
                    var image = rt.gameObject.AddComponent<RawImage>(); image.texture = face; image.raycastTarget = false;
                    var meter = MenuLayout.Meter(button.transform, 3, 42, 40, 3, c.PersonalHealth / GameBalance.CommanderHealth, MenuTheme.Hp);
                    health[c] = (RectTransform)meter.Find("Fill");
                }
                if (units.Length > slots)
                    MenuLayout.Button(strip, "More Selected", "+" + (units.Length - slots), slots * 50, 0, 46, 46,
                        () => GameMenuController.Instance.Roster());
            }
            strip.sizeDelta = new Vector2(width, 46);
            strip.anchoredPosition = new Vector2(0, 66);
            foreach (var item in health)
                if (item.Key != null && item.Value != null) item.Value.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 40 * Mathf.Clamp01(item.Key.PersonalHealth / GameBalance.CommanderHealth));
        }
    }
}
