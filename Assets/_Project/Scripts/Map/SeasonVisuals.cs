using AntColony.Core;
using UnityEngine;

namespace AntColony.Map
{
    // 계절 연출(2026-10-03, 기획 '배경·계절'): 바닥·잎 색(봄 연두 → 여름 진녹 → 가을 주황 → 겨울 회갈색),
    // 햇빛(여름 강한 노란빛, 겨울 차갑고 낮은 빛), 떨어지는 것(봄 꽃잎, 가을 낙엽, 겨울 눈). 계절 끝 20% 동안 다음 계절로 섞는다.
    public sealed class SeasonVisuals : MonoBehaviour
    {
        static readonly Color[] Ground = { new Color(1f, 1.05f, .9f), new Color(.85f, 1f, .8f), new Color(1.1f, .9f, .7f), new Color(.72f, .68f, .62f) };
        static readonly Color[] Leaf = { new Color(.95f, 1.1f, .85f), new Color(.8f, 1f, .75f), new Color(1.35f, .75f, .35f), new Color(.95f, .95f, 1f) };
        static readonly Color[] Sun = { new Color(1f, .98f, .92f), new Color(1.08f, 1f, .82f), new Color(1.05f, .9f, .78f), new Color(.85f, .92f, 1.05f) };
        static readonly float[] SunPower = { 1f, 1.12f, .95f, .8f };
        private MapGenerator terrain;
        private Color biomeTint = Color.white;
        private Precipitation fall;

        public static void Ensure(MapGenerator terrain, Color biomeTint)
        {
            var v = terrain.GetComponent<SeasonVisuals>() ?? terrain.gameObject.AddComponent<SeasonVisuals>();
            v.terrain = terrain; v.biomeTint = biomeTint;
        }

        // 0~4 연속 계절 위치에서 이번 계절과 다음 계절 섞는 비율.
        public static (int season, int next, float blend) Position()
        {
            var t = GameCalendar.GameSeconds / GameCalendar.SecondsPerDay % 4f;
            var s = Mathf.FloorToInt(t);
            return (s, (s + 1) % 4, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.8f, 1f, t - s)));
        }
        static Color Mix(Color[] table, (int s, int n, float b) p) => Color.Lerp(table[p.s], table[p.n], p.b);

        private void Awake() => fall = Precipitation.Create("Season Fall", transform);
        private void OnDestroy() { DayNightLighting.SeasonTint = Color.white; DayNightLighting.SeasonIntensity = 1; }

        private void LateUpdate()
        {
            if (terrain == null) return;
            var p = Position();
            terrain.SetTint(biomeTint * Mix(Ground, p));
            var leaf = Mix(Leaf, p);
            foreach (var (material, baseColor) in terrain.Foliage)
                if (material != null) material.SetColor("_BaseColor", baseColor * leaf);
            DayNightLighting.SeasonTint = Mix(Sun, p);
            DayNightLighting.SeasonIntensity = Mathf.Lerp(SunPower[p.season], SunPower[p.next], p.blend) * WeatherSystem.Light * (BiomeRules.Current == MapBiome.Cave ? .6f : 1); // 동굴은 어둡다
            // 비·눈 날씨가 오면 계절 입자는 쉰다. 동굴은 땅속이라 없음.
            var quiet = WeatherSystem.Current != WeatherKind.Clear && WeatherSystem.Current != WeatherKind.Fog || BiomeRules.Current == MapBiome.Cave;
            var season = (Season)p.season;
            if (quiet) fall.Set(0, Color.clear, .1f, 5);
            else if (season == Season.Spring) fall.Set(25, new Color(1f, .75f, .85f, .9f), .25f, 1.5f, 0, 1);
            else if (season == Season.Autumn) fall.Set(35, new Color(.95f, .5f, .15f, .95f), .3f, 1.8f, 0, 1.5f);
            else if (season == Season.Winter) fall.Set(120, new Color(1, 1, 1, .85f), .15f, 2.5f, 0, .3f);
            else fall.Set(0, Color.clear, .1f, 5);
        }
    }
}
