using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;

namespace AntColony.Core
{
    public enum ResourceReason { Gathering, Construction, Research, Upkeep, Expedition, Trade, Crafting, Production, Treatment, Reward, Refund, Other }

    // 최근 알림과 엔딩용 누적 기록은 별도 보관한다. 복원 중의 생성/반납은 기록하지 않는다.
    public sealed class CampaignHistory : MonoBehaviour
    {
        [Serializable] public class Entry { public float seconds; public string kind, name, result; }
        [Serializable] public class State
        {
            public long[] acquired = new long[3], spent = new long[3];
            public long[] spentByReason = new long[3 * Enum.GetValues(typeof(ResourceReason)).Length];
            public long antsProduced, antsLost;
            public long[] events = new long[11];
            public List<Entry> recent = new List<Entry>(), milestones = new List<Entry>();
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
            var d = Instance.Data; var i = (int)type;
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
            var e = new Entry { seconds = GameCalendar.GameSeconds, kind = kind, name = name, result = result };
            var d = Instance.Data; d.recent.Add(e); if (d.recent.Count > 20) d.recent.RemoveAt(0);
            d.milestones.Add(e);
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
                && s.antsProduced >= 0 && s.antsLost >= 0 && Entries(s.recent, 20) && Entries(s.milestones, 100000);
        }
    }
}
