using AntColony.World;

namespace AntColony.UI
{
    public enum MapAffiliation { Ours, Ally, Neutral, Enemy, Rebel }

    public static class WorldMapSymbols
    {
        public const string Legend = "세력: [=] 우리 · [+] 동맹 · [-] 중립 · [!] 적 · [X] 반란\n적·반란은 마름모, 나머지는 사각형. D숫자: 위험 1 쉬움 / 2 보통 / 3 어려움 / 4+ 매우 어려움\n공격 중 ATK · 상실 LOST · 포기 OFF · 선택은 굵은 외곽";
        public const string SelectionLegend = "선택: 굵은 원형 링 / 흰 사각 외곽선 · 배치: + 가능 / X 불가 (미리보기 윗면)";

        public static MapAffiliation Affiliation(ConquestDisposition disposition, ExpeditionSiteKind kind, bool cleared, Civilization faction, float now)
        {
            if (disposition == ConquestDisposition.Annexed) return MapAffiliation.Ours;
            if (faction != null)
            {
                if (faction.rebel) return MapAffiliation.Rebel;
                if (faction.war) return MapAffiliation.Enemy;
                return faction.HasTreaty(TreatyKind.Alliance, now) ? MapAffiliation.Ally : MapAffiliation.Neutral;
            }
            return disposition == ConquestDisposition.Lost || (!cleared && kind != ExpeditionSiteKind.ResourceSite && kind != ExpeditionSiteKind.Empty)
                ? MapAffiliation.Enemy : MapAffiliation.Neutral;
        }
        public static string Symbol(MapAffiliation affiliation) => affiliation switch
        {
            MapAffiliation.Ours => "=", MapAffiliation.Ally => "+", MapAffiliation.Neutral => "-", MapAffiliation.Enemy => "!", _ => "X"
        };
        public static bool Diamond(MapAffiliation affiliation) => affiliation == MapAffiliation.Enemy || affiliation == MapAffiliation.Rebel;
        public static string Risk(int difficulty) => "D" + System.Math.Max(1, difficulty);
        public static string State(ConquestDisposition disposition, bool attacked) => attacked ? " ATK" : disposition == ConquestDisposition.Lost ? " LOST"
            : disposition == ConquestDisposition.Abandoned ? " OFF" : "";
    }
}
