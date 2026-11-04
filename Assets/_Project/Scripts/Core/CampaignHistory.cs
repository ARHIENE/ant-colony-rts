using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Core
{
    public enum ResourceReason { Gathering, Construction, Research, Upkeep, Expedition, Trade, Crafting, Production, Treatment, Reward, Refund, Other, Tax }

    // 최근 알림과 엔딩용 누적 기록은 별도 보관한다. 복원 중의 생성/반납은 기록하지 않는다.
    public sealed class CampaignHistory : MonoBehaviour
    {
        [Serializable] public class Entry { public float seconds; public string kind, name, result; }
        [Serializable] public class Summary { public string kind; public long count; public float first, last; }
        [Serializable] public class State
        {
            public long[] acquired = new long[3], spent = new long[3];
            public long[] spentByReason = new long[3 * Enum.GetValues(typeof(ResourceReason)).Length];
            public long antsProduced, antsLost;
            public int scoutRecruitSlot = -1, scoutRecruitFailures;
            public long[] events = new long[11];
            public List<Entry> recent = new List<Entry>(), milestones = new List<Entry>();
            public List<Summary> summaries = new List<Summary>();
        }
        public static CampaignHistory Instance { get; private set; }
        public State Data { get; private set; } = new State();
        public static bool Recording => Instance != null && !SaveSystem.Busy && GameSession.Exists && GameSession.Instance.GameStarted;
        private void Awake() => Instance = this;
        private void OnDestroy() { if (Instance == this) Instance = null; }
        public void Restore(State state) => Data = JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(Data));
        public static void Resource(ResourceType type, int amount, bool spending, ResourceReason reason)
        {
            if (!Recording || amount <= 0) return;
            var d = Instance.Data; var i = type.Group(); // 재료 종류는 '재료' 칸 하나로 묶는다.
            if (spending) { d.spent[i] += amount; d.spentByReason[i * Enum.GetValues(typeof(ResourceReason)).Length + (int)reason] += amount; }
            else d.acquired[i] += amount;
        }
        public static void Ants(int count, bool lost)
        {
            if (!Recording || count <= 0) return;
            if (lost) Instance.Data.antsLost += count; else Instance.Data.antsProduced += count;
        }
        public static void Record(string kind, string name, string result, bool notify = false)
        {
            if (!Recording) return;
            FirstHints.OnRecord(kind, result);
            var e = new Entry { seconds = GameCalendar.GameSeconds, kind = kind, name = name, result = result };
            var d = Instance.Data; d.recent.Add(e); if (d.recent.Count > 20) d.recent.RemoveAt(0);
            d.milestones.Add(e);
            Compact(d);
            if (notify) ToastManager.Show(name + ": " + result);
        }
        public static bool Validate(State s)
        {
            bool Totals(long[] a, int n) => a != null && a.Length == n && a.All(v => v >= 0 && v <= 1000000000000L);
            bool Entries(List<Entry> list, int limit) => list != null && list.Count <= limit && list.All(e => e != null
                && !float.IsNaN(e.seconds) && !float.IsInfinity(e.seconds) && e.seconds >= 0
                && e.kind != null && e.kind.Length <= 100 && e.name != null && e.name.Length <= 300 && e.result != null && e.result.Length <= 1000);
            return s != null && Totals(s.acquired, 3) && Totals(s.spent, 3)
                && Totals(s.spentByReason, 3 * Enum.GetValues(typeof(ResourceReason)).Length) && Totals(s.events, 11)
                && s.antsProduced >= 0 && s.antsLost >= 0 && s.scoutRecruitSlot >= -1
                && s.scoutRecruitFailures >= 0 && s.scoutRecruitFailures <= 3
                && Entries(s.recent, 20) && Entries(s.milestones, 100000)
                && (s.summaries == null || s.summaries.Count <= 10000 && s.summaries.All(a => a != null && a.kind != null
                    && a.kind.Length <= 100 && a.count > 0 && a.count <= 1000000000000L && a.first >= 0 && a.last >= a.first && !float.IsInfinity(a.last))
                    && s.summaries.Select(a => a.kind).Distinct().Count() == s.summaries.Count);
        }
        public static void Compact(State s)
        {
            s.summaries ??= new List<Summary>();
            if (s.milestones.Count <= 2000) return;
            int count = s.milestones.Count - 1500;
            foreach (var e in s.milestones.Take(count))
            {
                var a = s.summaries.Find(a => a.kind == e.kind);
                if (a == null) { a = new Summary { kind = e.kind, first = e.seconds, last = e.seconds }; s.summaries.Add(a); }
                a.count++; a.first = Math.Min(a.first, e.seconds); a.last = Math.Max(a.last, e.seconds);
            }
            s.milestones.RemoveRange(0, count);
        }
    }
}
