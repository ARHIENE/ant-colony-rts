using System.Text.RegularExpressions;
using AntColony.World;

namespace AntColony.UI
{
    // 거점 Title은 바이옴·기록 키라 그대로 두고, 화면 표시만 한국어로 바꾼다.
    public static class SiteNames
    {
        private static readonly (string from, string to)[] Words =
        {
            ("MiniBird Nest", "미니버드 둥지"), ("Resource Field", "자원 지대"), ("ResourceSite", "자원지"), ("TradePost", "교역소"),
            ("BossNest", "보스 둥지"), ("Settlement", "정착지"), ("Outpost", "전초기지"), ("Colony", "소굴"), ("Empty", "빈 거점"),
            ("Amber", "호박"), ("Azure", "청람"), ("Jade", "비취"), ("Crimson", "진홍"), ("Trade", "교역"), ("Neutral", "중립"), ("Wildlife", "야생"),
        };

        public static string Name(ExpeditionSite site) => site == null ? "" : Korean(site.Title);
        public static string Faction(ExpeditionSite site) => site == null ? "" : Korean(site.Faction);

        public static string Korean(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = Regex.Replace(text, @"Independent \d+", "독립");
            foreach (var (from, to) in Words) text = text.Replace(from, to);
            return text;
        }
    }
}
