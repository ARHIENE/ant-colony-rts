using AntColony.World;

namespace AntColony.UI
{
    public static class DiplomacyPlanning
    {
        public static string PeaceLabel(Civilization civ, float now)
        {
            if (!civ.war) return "평화 협상 · 현재 평화 상태";
            var remaining = System.Math.Max(0, DiplomacyRules.PeaceDelay - (now - civ.warStarted));
            if (remaining > 0) return $"평화 협상 · {System.Math.Ceiling(remaining):0}초 후 가능";
            var due = civ.enemyScore - civ.playerScore;
            return "평화 협상 · " + (due > 0 ? $"식량 {due} 지불" : due < 0 ? $"식량 {-(long)due} 수령" : "배상 없음");
        }
    }
}
