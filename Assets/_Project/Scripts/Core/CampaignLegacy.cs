using System.Collections.Generic;
using System.Linq;
using AntColony.World;

namespace AntColony.Core
{
    public static class CampaignLegacy
    {
        public static List<string> Capture(CampaignOutcome.State ending)
        {
            var lines = new List<string>();
            var entries = ending.commanders.ToDictionary(c => c.id);
            var pairs = new HashSet<string>();
            if (CommanderRoster.Instance != null) foreach (var c in CommanderRoster.Instance.Commanders)
                foreach (var r in c.PersonalState.relations)
                {
                    if (!entries.TryGetValue(r.otherId, out var other) || !entries.TryGetValue(c.PersonalState.id, out var own)) continue;
                    var key = string.CompareOrdinal(own.id, other.id) < 0 ? own.id + ":" + other.id : other.id + ":" + own.id;
                    string relationship = r.spouse ? "배우자" : r.family ? "가족" : r.value >= 40 ? "친구" : r.value <= -40 ? "라이벌" : null;
                    if (relationship != null && pairs.Add(key)) lines.Add($"관계: {own.name}({Label(own.category)}) · {other.name}({Label(other.category)}) — {relationship}");
                }
            foreach (var death in Memorial.Deaths)
                if (entries.ContainsKey(death.id)) lines.Add($"기억하는 이: {death.name} · {death.cause} · {GameCalendar.Format(death.date)}");
            if (DiplomacyManager.Instance != null) foreach (var faction in DiplomacyManager.Instance.Data.civilizations.Where(c => c.contacted || c.extinct))
                lines.Add($"세력: {faction.name} · {(faction.extinct ? "소멸" : faction.war ? "전쟁 중" : "평화")} · 관계 {faction.affinity:0}");
            if (WorldMapManager.Instance != null)
                lines.Add($"남은 정착지: 편입 영토 {WorldMapManager.Instance.Sites.Count(s => s.Disposition == ConquestDisposition.Annexed)}곳 · 시민 {ending.citizensStaying}명");
            return lines;
        }
        public static string Label(EndingCategory category) => category switch {
            EndingCategory.Escaped => "탈출", EndingCategory.Staying => "잔류", EndingCategory.Outside => "외부",
            EndingCategory.Captive => "포로", EndingCategory.Dead => "사망", _ => "이탈" };
    }
}
