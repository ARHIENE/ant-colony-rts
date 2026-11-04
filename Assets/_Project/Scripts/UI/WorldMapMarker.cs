using AntColony.World;
using UnityEngine;
using UnityEngine.UI;

namespace AntColony.UI
{
    public sealed class WorldMapMarker : MonoBehaviour
    {
        private Image shape;
        private Text label;
        public Text Label => label;
        public void Initialize(Text label)
        {
            this.label = label;
            // 구름·사막 위에서도 읽히게 검은 외곽선(2026-11-01 캡처에서 흰 구름 위 라벨이 묻힘).
            if (label != null && label.GetComponent<Outline>() == null) label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, .85f);
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
            label.text = $"[{WorldMapSymbols.Symbol(affiliation)}] {SiteNames.Name(site)} · {WorldMapSymbols.Risk(site.Difficulty)}{WorldMapSymbols.State(site.Disposition, site.Defense != null && site.Defense.UnderAttack)}";
            GetComponent<MenuTooltip>().Message = label.text + "\n" + WorldMapSymbols.Legend;
            var faction = diplomacy?.Faction(site);
            if (faction != null && site.Colony != null && site.Disposition != ConquestDisposition.Annexed)
                GetComponent<MenuTooltip>().Message += "\n" + AIIndustry.Summary(site.Colony, faction.era)
                    + "\n본부: 식량·자재 / 정련소: 자재 / 공방: 교체 장비 / 연구소: 시대 연구 / 지휘소: 장갑. 시설 파괴로 생산·연구·방어를 약화시킬 수 있습니다.";
        }
    }
}
