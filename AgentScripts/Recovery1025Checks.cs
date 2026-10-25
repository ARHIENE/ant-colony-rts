using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Recovery1025Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + label); checks++; }
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static Button Button(GameMenuController menu, string name) => menu.GetComponentsInChildren<Button>().First(b => b.name == name);
    static NewGameOptions Options(GameMenuController menu) => (NewGameOptions)typeof(GameMenuController).GetField("options", Private).GetValue(menu);

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "play mode");
        var deadline = Time.realtimeSinceStartup + 50;
        while (SaveSystem.Busy && Time.realtimeSinceStartup < deadline) await Task.Delay(50);
        Check(!SaveSystem.Busy, "initial scene ready");
        var menu = GameMenuController.Instance;
        Check(menu != null, "menu singleton survives fresh Play start");
        typeof(GameMenuController).GetMethod("NewGameScreen", Private).Invoke(menu, null);
        await Task.Yield(); await Task.Yield();
        Canvas.ForceUpdateCanvases();
        Check(GameMenuController.BlocksInput, "map settings block world input");
        Check(menu.GetComponentsInChildren<ScrollRect>().All(s => !s.vertical && !s.horizontal), "map settings cannot scroll");
        var mapSeed = Options(menu).seed;
        Button(menu, "Biome City").onClick.Invoke(); await Task.Yield();
        Check(Options(menu).seed != mapSeed, "changing biome changes map seed");
        Canvas.ForceUpdateCanvases();
        var reroll = Button(menu, "Reroll Candidate 0");
        var corners = new Vector3[4]; ((RectTransform)reroll.transform).GetWorldCorners(corners);
        var data = new PointerEventData(EventSystem.current) { position = (corners[0] + corners[2]) * .5f };
        var hits = new System.Collections.Generic.List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
        Check(hits.Count > 0 && hits[0].gameObject == reroll.gameObject, "reroll receives pointer raycast");
        var before = JsonUtility.ToJson(Options(menu).startingTeam[0]);
        ExecuteEvents.Execute(reroll.gameObject, data, ExecuteEvents.pointerClickHandler);
        Check(before != JsonUtility.ToJson(Options(menu).startingTeam[0]), "pointer click rerolls candidate");
        await Task.Yield();
        var shown = menu.GetComponentsInChildren<Text>().Select(t => t.text).ToArray();
        Check(shown.Any(t => t.StartsWith(Options(menu).startingTeam[0].name)), "rerolled candidate appears");

        var owner = new GameObject("Recovery weather check");
        var scale = Time.timeScale;
        try
        {
            Time.timeScale = 0;
            var weather = Precipitation.Create("Recovery rain", owner.transform);
            weather.Set(100, Color.white, .2f, 8);
            var particles = weather.GetComponent<ParticleSystem>();
            var time = particles.time;
            var calendar = GameCalendar.GameSeconds;
            await Task.Delay(400);
            Check(particles.main.useUnscaledTime && particles.time > time, "rain simulates during pause");
            Check(GameCalendar.GameSeconds == calendar, "calendar remains paused");
            weather.Set(25, Color.white, .4f, 1.5f, petals: true);
            var texture = (Texture2D)weather.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_BaseMap");
            Check(texture.GetPixel(16, 16).a > .9f && texture.GetPixel(2, 16).a == 0 && texture.GetPixel(16, 31).a == 0, "petal silhouette has tapered edges and notch");
            Check(particles.rotationOverLifetime.enabled, "petals rotate");
            weather.Set(120, Color.white, .15f, 2.5f);
            Check(!particles.rotationOverLifetime.enabled, "snow resets petal rotation");
        }
        finally { Time.timeScale = scale; UnityEngine.Object.Destroy(owner); }
        return "PASS " + checks + " recovery menu/weather checks";
    }
}
