using System;
using AntColony.Core;
using UnityEngine;

namespace AntColony.Map
{
    public enum WeatherKind { Clear, Rain, Storm, Fog, Snow, Blizzard, Sandstorm }

    // 날씨(2026-10-03, 수치 잠정): 5분(한 달)마다 계절·바이옴에 따라 다시 정한다. 효과는 방 밖에만(방 = 비·바람 막음).
    // 악천후(폭풍·눈보라·모래폭풍)는 작업·이동이 크게 느려지고 기분이 떨어진다.
    public sealed class WeatherSystem : MonoBehaviour
    {
        [Serializable] public class State { public int kind; public float remaining; }
        public const float RollSeconds = 300f;
        public static WeatherSystem Instance { get; private set; }
        public static WeatherKind Current => Instance != null ? (WeatherKind)Instance.state.kind : WeatherKind.Clear;
        private State state = new State();
        private Precipitation fall;

        public static string Name(WeatherKind k) => k switch
        {
            WeatherKind.Rain => "비", WeatherKind.Storm => "폭풍우", WeatherKind.Fog => "안개", WeatherKind.Snow => "눈",
            WeatherKind.Blizzard => "눈보라", WeatherKind.Sandstorm => "모래폭풍", _ => "맑음"
        };
        public static bool Severe => Current == WeatherKind.Storm || Current == WeatherKind.Blizzard || Current == WeatherKind.Sandstorm;
        public static bool Wet => Current == WeatherKind.Rain || Current == WeatherKind.Storm;

        // 방 밖 배율. 방 안은 1.
        public static float WorkAt(bool indoors) => indoors ? 1 : Current switch
        {
            WeatherKind.Rain or WeatherKind.Snow => .9f, WeatherKind.Fog => .95f, WeatherKind.Storm or WeatherKind.Sandstorm => .75f, WeatherKind.Blizzard => .7f, _ => 1
        };
        public static float MoveAt(bool indoors) => indoors ? 1 : Current switch
        {
            WeatherKind.Snow => .9f, WeatherKind.Storm => .85f, WeatherKind.Sandstorm => .75f, WeatherKind.Blizzard => .7f, _ => 1
        };
        public static float FatigueAt(bool indoors) => !indoors && Current == WeatherKind.Blizzard ? 1.5f : 1;
        public static int Mood => Current switch
        {
            WeatherKind.Rain => -3, WeatherKind.Snow => -2, WeatherKind.Storm or WeatherKind.Sandstorm => -6, WeatherKind.Blizzard => -8, _ => 0
        };
        public static float FarmGrowth => Wet ? 1.1f : 1;
        // 안개·모래폭풍은 시야(적 탐지 반경)를 줄인다.
        public static float Visibility => Current == WeatherKind.Fog ? .6f : Current == WeatherKind.Sandstorm ? .5f : 1;
        public static float Light => Current switch
        {
            WeatherKind.Rain => .75f, WeatherKind.Storm => .55f, WeatherKind.Fog => .85f, WeatherKind.Snow => .9f, WeatherKind.Blizzard => .65f, WeatherKind.Sandstorm => .7f, _ => 1
        };

        public static WeatherKind Roll(Season season, MapBiome biome, float value)
        {
            // 맑음·비·폭풍·안개·눈·눈보라 가중치(계절), 그다음 바이옴 보정.
            float[] w = season switch
            {
                Season.Spring => new float[] { 45, 30, 8, 17, 0, 0 }, Season.Summer => new float[] { 55, 15, 20, 10, 0, 0 },
                Season.Autumn => new float[] { 40, 25, 10, 25, 0, 0 }, _ => new float[] { 40, 0, 0, 10, 35, 15 }
            };
            float sand = 0;
            if (biome == MapBiome.Waterside) { w[1] *= 1.5f; w[3] *= 1.5f; }
            if (biome == MapBiome.Desert) { w[0] += w[1] + w[4]; sand = w[2] + w[5]; w[1] = w[2] = w[4] = w[5] = 0; }
            if (biome == MapBiome.Cave) { w[0] += w[1] + w[2] + w[4] + w[5]; w[1] = w[2] = w[4] = w[5] = 0; } // 땅속: 비·눈 없음
            var total = sand; foreach (var x in w) total += x;
            var pick = value * total;
            for (var i = 0; i < w.Length; i++) { if (pick < w[i]) return Order[i]; pick -= w[i]; }
            return sand > 0 ? WeatherKind.Sandstorm : WeatherKind.Clear;
        }
        private static readonly WeatherKind[] Order = { WeatherKind.Clear, WeatherKind.Rain, WeatherKind.Storm, WeatherKind.Fog, WeatherKind.Snow, WeatherKind.Blizzard };

        public static void Ensure(GameObject host) { if (Instance == null) host.AddComponent<WeatherSystem>(); }
        private void Awake() { Instance = this; state.remaining = RollSeconds; fall = Precipitation.Create("Weather Fall", transform); } // 첫 5분은 맑음
        private void OnDestroy() { if (Instance == this) Instance = null; RenderSettings.fog = false; }

        public State CaptureState() => new State { kind = state.kind, remaining = state.remaining };
        public void RestoreState(State s) { state = s != null ? new State { kind = Mathf.Clamp(s.kind, 0, 6), remaining = Mathf.Max(0, s.remaining) } : new State(); Apply(); }
        public void Set(WeatherKind kind, float seconds = RollSeconds)
        {
            var was = Current; state.kind = (int)kind; state.remaining = seconds; Apply();
            if (kind != was && kind != WeatherKind.Clear) AntColony.UI.ToastManager.Show($"날씨: {Name(kind)}" + (Severe ? " — 방 밖 작업·이동이 크게 느려집니다." : ""));
            if (kind == WeatherKind.Storm && !float.IsNaN(MapGenerator.WaterLevel) && UnityEngine.Random.value < .3f)
                AntColony.World.ColonyEvents.Instance?.TryTrigger(AntColony.World.ColonyEvent.Flood); // 폭풍우는 물가 홍수를 부를 수 있다.
        }

        private void Update()
        {
            if (!GameSession.Exists || !GameSession.Instance.GameStarted || AntColony.Save.SaveSystem.Busy) return;
            state.remaining -= Time.deltaTime;
            if (state.remaining <= 0) Set(Roll(GameCalendar.CurrentSeason, BiomeRules.Current, UnityEngine.Random.value));
        }

        private void Apply()
        {
            if (fall == null) return;
            var k = Current;
            switch (k)
            {
                case WeatherKind.Rain: fall.Set(1800, new Color(.8f, .88f, 1f, .85f), .16f, 22, .12f); break;
                case WeatherKind.Storm: fall.Set(3500, new Color(.8f, .88f, 1f, .9f), .18f, 30, .12f, 6); break;
                case WeatherKind.Snow: fall.Set(900, new Color(1, 1, 1, .95f), .35f, 3, 0, .5f); break;
                case WeatherKind.Blizzard: fall.Set(3000, new Color(1, 1, 1, .95f), .4f, 6, 0, 9); break;
                case WeatherKind.Sandstorm: fall.Set(3000, new Color(.85f, .7f, .45f, .7f), .4f, 3, 0, 14); break;
                default: fall.Set(0, Color.clear, .1f, 5); break;
            }
            RenderSettings.fog = k == WeatherKind.Fog || k == WeatherKind.Blizzard || k == WeatherKind.Sandstorm || k == WeatherKind.Storm;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = k == WeatherKind.Sandstorm ? new Color(.78f, .62f, .4f) : new Color(.72f, .75f, .8f);
            RenderSettings.fogDensity = k == WeatherKind.Fog ? .018f : k == WeatherKind.Storm ? .008f : .014f;
        }
    }
}
