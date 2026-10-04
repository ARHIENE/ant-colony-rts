using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 계절 연출·날씨·물속 감속(2026-10-03).
public static class SeasonWeatherChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static async Task Ready() { while (SaveSystem.Busy) await Task.Delay(30); Time.timeScale = 0; }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Weather-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small, biome = MapBiome.Forest }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var gen = Object.FindAnyObjectByType<MapGenerator>();
            var season = gen.GetComponent<SeasonVisuals>();
            Check(season != null && WeatherSystem.Instance != null && WeatherSystem.Current == WeatherKind.Clear, "season and weather attached, start clear");

            // 계절: 가을이면 잎 색이 주황 쪽(빨강 > 초록 배율), 겨울 햇빛은 차갑고 약하다.
            GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, GameCalendar.SecondsPerDay * 2 + 10); await Task.Yield(); await Task.Yield();
            // 나무·풀은 SeasonFoliage 셰이더가 전역 계절 값으로 잎만 물들인다.
            Check(gen.GetComponentsInChildren<Renderer>().Any(r => r.sharedMaterial != null && r.sharedMaterial.shader.name == "AntColony/SeasonFoliage"), "foliage uses season shader");
            Check(Shader.GetGlobalFloat("_SeasonAutumn") > .9f && Shader.GetGlobalFloat("_SeasonWinter") < .1f, "autumn leaf color on");
            Check(Object.FindObjectsByType<ParticleSystem>().Any(p => p.name == "Season Fall" && p.emission.rateOverTime.constant > 0), "autumn leaves falling");
            GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, GameCalendar.SecondsPerDay * 3 + 10); await Task.Yield(); await Task.Yield();
            Check(DayNightLighting.SeasonIntensity < 1 && DayNightLighting.SeasonTint.b > DayNightLighting.SeasonTint.r, "winter sun cold and low");
            Check(Shader.GetGlobalFloat("_SeasonWinter") > .9f, "winter snow on trees");

            // 날씨 굴림: 사막은 비·눈 대신 모래폭풍, 동굴은 비·눈 없음, 겨울에만 눈.
            for (var v = 0f; v < 1; v += .01f)
            {
                var d = WeatherSystem.Roll(Season.Summer, MapBiome.Desert, v); Check(d == WeatherKind.Clear || d == WeatherKind.Fog || d == WeatherKind.Sandstorm, "desert weather " + d);
                var c = WeatherSystem.Roll(Season.Winter, MapBiome.Cave, v); Check(c == WeatherKind.Clear || c == WeatherKind.Fog, "cave weather " + c);
                var s = WeatherSystem.Roll(Season.Summer, MapBiome.Forest, v); Check(s != WeatherKind.Snow && s != WeatherKind.Blizzard, "no summer snow");
            }
            Check(Enumerable.Range(0, 100).Any(i => WeatherSystem.Roll(Season.Winter, MapBiome.Forest, i / 100f) == WeatherKind.Blizzard), "winter can blizzard");

            // 효과: 눈보라는 방 밖 작업·이동 크게 감소, 방 안은 면제, 기분 하락. 비는 산불을 막는다.
            var c0 = CommanderRoster.Instance.Commanders.First(c => c.IsColonyMember && !c.IsEmbarked);
            var clearWork = c0.WorkRate(CommanderActivity.Gathering);
            WeatherSystem.Instance.Set(WeatherKind.Blizzard);
            Check(WeatherSystem.Severe && Mathf.Approximately(WeatherSystem.WorkAt(true), 1) && WeatherSystem.MoveAt(false) < .8f, "blizzard outdoors only");
            Check(RoomSystem.IsIndoors(c0.Position) || Mathf.Abs(c0.WorkRate(CommanderActivity.Gathering) - clearWork * .7f) < .001f, "blizzard slows outdoor work");
            c0.TickPersonal(.1f);
            Check(RoomSystem.IsIndoors(c0.Position) || c0.PersonalState.moodFactors.Any(f => f.reason == "날씨: 눈보라"), "blizzard mood");
            Check(RenderSettings.fog && Object.FindObjectsByType<ParticleSystem>().Any(p => p.name == "Weather Fall" && p.emission.rateOverTime.constant > 1000), "blizzard visuals");
            Check(HudClock.DayLabel.Contains("눈보라"), "hud shows weather");
            WeatherSystem.Instance.Set(WeatherKind.Rain);
            Check(!ColonyEvents.Instance.Eligible(ColonyEvent.Wildfire) && WeatherSystem.FarmGrowth > 1, "rain blocks wildfire, helps farms");
            WeatherSystem.Instance.Set(WeatherKind.Fog);
            Check(WeatherSystem.Visibility < 1 && !Object.FindObjectsByType<ParticleSystem>().Any(p => p.name == "Weather Fall" && p.emission.rateOverTime.constant > 0), "fog: low visibility, no rain");

            // 물속은 느리게.
            if (!float.IsNaN(MapGenerator.WaterLevel))
            {
                Check(MapGenerator.InWater(new Vector3(0, MapGenerator.WaterLevel - 1, 0)) && !MapGenerator.InWater(new Vector3(0, MapGenerator.WaterLevel + 1, 0)), "water level test");
                WeatherSystem.Instance.Set(WeatherKind.Clear);
                var dry = c0.Position; var speedDry = MoveSpeed(c0);
                c0.Agent.enabled = false; c0.transform.position = new Vector3(dry.x, MapGenerator.WaterLevel - .5f, dry.z);
                Check(Mathf.Abs(MoveSpeed(c0) - speedDry * MapGenerator.WaterMoveMultiplier) < .01f, "water slows movement");
                c0.transform.position = dry; c0.Agent.enabled = true;
            }

            // 저장·불러오기: 날씨 유지.
            WeatherSystem.Instance.Set(WeatherKind.Storm, 123);
            Check(SaveSystem.TrySave(false, 1, out var error), "save: " + error);
            WeatherSystem.Instance.Set(WeatherKind.Clear);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out error), "load: " + error); await Ready();
            Check(WeatherSystem.Current == WeatherKind.Storm && WeatherSystem.Instance.CaptureState().remaining > 100, "weather survives reload");
            return "PASS " + checks + " season/weather checks";
        }
        finally
        {
            WeatherSystem.Instance?.Set(WeatherKind.Clear);
            GameMenuController.Instance?.Resume(); Time.timeScale = 0;
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }

    static float MoveSpeed(CommanderAnt c) => (float)typeof(CommanderAnt).GetProperty("MovementSpeed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(c);
}
