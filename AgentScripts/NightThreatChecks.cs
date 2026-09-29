using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

// 6단계 밤 위협 (2026-09-28): 밤마다 야행성 포식자 출현·새벽 퇴각·저장, 침공 밤 1.5배 상수.
public static class NightThreatChecks
{
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static void At(float timeOfDay, int day = 0) => GameSession.Instance.MarkStarted(GameSession.Instance.PlaySeconds, day * GameCalendar.SecondsPerDay + timeOfDay);
    static EventActor[] Predators() => Object.FindObjectsByType<EventActor>(FindObjectsSortMode.None)
        .Where(a => a.isActiveAndEnabled && a.Kind == EventActorKind.NightPredator && a.Alive).ToArray();

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Night-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261001, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var events = ColonyEvents.Instance; Check(events != null, "events");
            Check(EventRules.NightThreatScale == 1.5f, "night invasion x1.5");

            // 낮에는 나오지 않는다.
            At(10); events.Tick(1);
            Check(Predators().Length == 0, "no predators by day");

            // 밤이 되면 한 번 나온다.
            At(GameCalendar.DaySeconds + 1); events.Tick(1);
            var first = Predators();
            Check(first.Length == EventRules.NightPredators, "predators at nightfall " + first.Length);
            var monster = first[0].GetComponent<WildMonster>();
            Check(monster.CurrentHealth == EventRules.NightPredatorHealth && !monster.Huntable && !monster.IsFlying, "predator stats");
            Check(first[0].GetComponent<Light>() != null, "glowing eyes");
            events.Tick(5);
            Check(Predators().Length == EventRules.NightPredators, "only once per night");

            // 저장 왕복.
            var saved = events.Capture();
            Check(ColonyEvents.Validate(saved) && saved.actors.Count(a => a.kind == EventActorKind.NightPredator) == EventRules.NightPredators, "capture validates");
            events.Restore(saved); await Task.Delay(100);
            Check(Predators().Length == EventRules.NightPredators, "restore respawns predators");
            var tooMany = events.Capture(); tooMany.actors.Add(tooMany.actors.First(a => a.kind == EventActorKind.NightPredator));
            Check(!ColonyEvents.Validate(tooMany), "too many predators rejected");

            // 새벽이 되면 물러간다. 다음 밤에 다시 나온다.
            At(1, 1); events.Tick(1); await Task.Delay(100);
            Check(Predators().Length == 0, "predators retreat at dawn");
            At(GameCalendar.DaySeconds + 1, 1); events.Tick(1);
            Check(Predators().Length == EventRules.NightPredators, "next night spawns again");
            At(1, 2); events.Tick(1); await Task.Delay(100);
            Check(Predators().Length == 0, "cleanup");
            return $"NightThreatChecks passed: {checks}";
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 1; }
    }
}
