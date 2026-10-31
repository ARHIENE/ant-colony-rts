using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed class WorldMapMarker : MonoBehaviour
    {
        private Image shape;
        private Text label;
        public void Initialize(Text label)
        {
            this.label = label;
            var rect = MenuTheme.Rect("Faction Shape", transform);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(18, 18);
            shape = rect.gameObject.AddComponent<Image>();
            shape.raycastTarget = false;
            rect.SetAsFirstSibling();
        }
        public void Refresh(ExpeditionSite site, Color color)
        {
            var diplomacy = DiplomacyManager.Instance;
            var affiliation = WorldMapSymbols.Affiliation(site.Disposition, site.Kind, site.Cleared, diplomacy?.Faction(site), diplomacy?.Data.elapsed ?? 0);
            shape.rectTransform.localRotation = Quaternion.Euler(0, 0, WorldMapSymbols.Diamond(affiliation) ? 45 : 0);
            shape.color = color;
            GetComponent<Image>().color = Color.clear;
            label.text = $"[{WorldMapSymbols.Symbol(affiliation)}] {site.Title} · {WorldMapSymbols.Risk(site.Difficulty)}{WorldMapSymbols.State(site.Disposition, site.Defense != null && site.Defense.UnderAttack)}";
            GetComponent<MenuTooltip>().Message = label.text + "\n" + WorldMapSymbols.Legend;
        }
    }
}
