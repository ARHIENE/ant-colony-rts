using System.Collections.Generic;
using System.Text;

namespace AntColony.Core
{
    public sealed partial class CampaignHistory
    {
        public static string StoryKind(string id) => "이야기:" + id;
        public static void Story(string id, string name, string result) => Record(StoryKind(id), name, result);
        public static string StoryLines(State state, string id, string name)
        {
            if (state == null) return "아직 기록된 이야기가 없습니다";
            var text = new StringBuilder(); int count = 0;
            string kind = StoryKind(id);
            bool identified = false;
            foreach (var e in state.milestones) if (e.kind == kind) { identified = true; break; }
            for (int i = state.milestones.Count - 1; i >= 0 && count < 5; i--)
            {
                var e = state.milestones[i];
                if (e.kind != kind && (identified || !(e.name == name && LegacyStory(e.kind)))) continue;
                if (count++ > 0) text.Append('\n');
                text.Append("· ").Append(e.kind == kind ? e.result : e.kind + " · " + e.result);
            }
            return count == 0 ? "아직 기록된 이야기가 없습니다" : text.ToString();
        }
        private static bool LegacyStory(string kind) => kind == "관계 변화" || kind == "말다툼" || kind == "사망" || kind == "수술" || kind == "특성 변화" || kind == "구조 필요" || kind == "이혼";

        // 기존 압축이 오래된 원문을 지워도 개인별 최근 다섯 이야기는 남긴다.
        private static void CompactStories(State s, int count)
        {
            var kept = new Dictionary<string, int>();
            for (int i = s.milestones.Count - 1; i >= 0; i--)
            {
                var e = s.milestones[i];
                if (e.kind.StartsWith("이야기:", System.StringComparison.Ordinal))
                {
                    kept.TryGetValue(e.kind, out int n); kept[e.kind] = n + 1;
                    if (n < 5) continue;
                }
                if (i >= count) continue;
                var a = s.summaries.Find(a => a.kind == e.kind);
                if (a == null) { a = new Summary { kind = e.kind, first = e.seconds, last = e.seconds }; s.summaries.Add(a); }
                a.count++; a.first = System.Math.Min(a.first, e.seconds); a.last = System.Math.Max(a.last, e.seconds);
                s.milestones.RemoveAt(i);
            }
        }
    }
}
