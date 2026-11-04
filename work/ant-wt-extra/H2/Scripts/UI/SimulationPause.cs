using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AntColony.UI
{
    public static class SimulationPause
    {
        [Serializable] public class State
        {
            public float speed = 1;
            public List<string> reasons = new List<string>();
            public List<string> incidents = new List<string>();
            public List<string> active = new List<string>();
            public int sequence;
        }
        private static State state = new State();
        public static IReadOnlyList<string> Reasons => state.reasons;
        public static float Speed => state.speed;
        public static void Hold(string reason)
        {
            if (!state.reasons.Contains(reason)) state.reasons.Add(reason);
            Apply();
        }
        public static void Release(string reason) { state.reasons.Remove(reason); Apply(); }
        public static void UserSpeed(float speed)
        {
            state.reasons.RemoveAll(r => r == "manual" || r.StartsWith("incident:"));
            if (speed <= 0) Hold("manual");
            else { state.speed = speed; Apply(); }
        }
        public static bool Incident(string id, string message, bool autoPause)
        {
            ToastManager.SetCrisis("incident:" + id, message);
            if (state.incidents.Contains(id)) return false;
            state.incidents.Add(id);
            if (autoPause) Hold("incident:" + id);
            return true;
        }
        public static void Observe(Dictionary<string, string> active, bool announce)
        {
            foreach (var ended in state.active.Where(key => !active.ContainsKey(key)))
                foreach (var id in state.incidents.Where(id => id.StartsWith(ended + ":")))
                    ToastManager.SetCrisis("incident:" + id, null);
            foreach (var entry in active)
                if (!state.active.Contains(entry.Key) && announce)
                {
                    var pause = entry.Key.StartsWith("down:") ? AntColony.Core.UserSettings.Current.autoPauseDowned
                        : AntColony.Core.UserSettings.Current.autoPauseThreat;
                    Incident(entry.Key + ":" + ++state.sequence, entry.Value, pause);
                }
            state.active = active.Keys.ToList();
        }
        public static State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public static void Restore(State value)
        {
            state = value == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(value));
            // 메뉴·협상창은 저장 파일이 아니라 현재 화면이 소유한다.
            state.reasons.RemoveAll(r => r == "menu" || r == "negotiation");
            Apply();
        }
        private static void Apply() => Time.timeScale = state.reasons.Count > 0 ? 0 : state.speed;
        public static bool Valid(State value) => value == null || value.reasons != null && value.incidents != null
            && new[] { 1f, 2f, 3f, 5f }.Contains(value.speed)
            && value.active != null && value.sequence >= 0 && value.active.Count <= 10000
            && value.reasons.Count <= 10000 && value.incidents.Count <= 100000
            && value.reasons.All(r => !string.IsNullOrEmpty(r)) && value.incidents.All(r => !string.IsNullOrEmpty(r));
    }
}
