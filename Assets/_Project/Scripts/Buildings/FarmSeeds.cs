using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AntColony.Buildings
{
    // 작물별 종자는 식량과 분리한다. 시작/야생 생성 담당은 FarmSeedSource.Configure를 호출한다.
    public static class FarmSeeds
    {
        [Serializable] public sealed class State
        {
            public int[] counts = new int[Enum.GetValues(typeof(FarmCrop)).Length];
            public List<string> collected = new List<string>();
        }
        private static State state = new State();
        [RuntimeInitializeOnLoadMethod]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= ResetScene;
            SceneManager.sceneLoaded += ResetScene;
        }
        private static void ResetScene(Scene scene, LoadSceneMode mode) { if (mode == LoadSceneMode.Single) Restore(null); }
        public static int Count(FarmCrop crop) => state.counts[(int)crop];
        public static void Add(FarmCrop crop, int amount) { if (amount > 0) state.counts[(int)crop] += amount; }
        public static bool Take(FarmCrop crop, int amount)
        {
            if (amount < 1 || Count(crop) < amount) return false;
            state.counts[(int)crop] -= amount; return true;
        }
        public static bool Collect(FarmSeedSource source)
        {
            if (source == null || state.collected.Contains(source.Key)) return false;
            state.collected.Add(source.Key); Add(source.Crop, 1); return true;
        }
        public static State Capture() => JsonUtility.FromJson<State>(JsonUtility.ToJson(state));
        public static bool Valid(State s) => s == null || s.counts != null && (s.counts.Length == 3 || s.counts.Length == Enum.GetValues(typeof(FarmCrop)).Length)
            && Array.TrueForAll(s.counts, n => n >= 0) && s.collected != null
            && !s.collected.Exists(string.IsNullOrEmpty) && new HashSet<string>(s.collected).Count == s.collected.Count;
        public static void Restore(State s)
        {
            if (!Valid(s)) throw new ArgumentException("Invalid farm seed state");
            state = s == null ? new State() : JsonUtility.FromJson<State>(JsonUtility.ToJson(s));
            Array.Resize(ref state.counts, Enum.GetValues(typeof(FarmCrop)).Length);
        }
    }

}
