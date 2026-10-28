using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Boss.AoE;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
public static class BossSpeed1028Checks
{
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main()
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone(); var pause = SimulationPause.Capture();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "BossSpeed-" + Guid.NewGuid().ToString("N"));
        var objects = new List<GameObject>();
        void Cast(Type type)
        {
            var go = new GameObject("Boss timing check"); objects.Add(go); var aoe = go.AddComponent(type);
            if (aoe is AntColony.Boss.AoE.BossCircleAoE circle) circle.CastAt(new Vector3(300, 0, 300));
            else type.GetMethod("CastFrom").Invoke(aoe, new object[] { new Vector3(300, 0, 300), Vector3.forward });
        }
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; quiet.autoPauseThreat = true; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready();
            UserSettings.Apply(quiet, false);
            var menu = GameMenuController.Instance; menu.Resume();
            foreach (var type in new[] { typeof(AntColony.Boss.AoE.BossCircleAoE), typeof(AntColony.Boss.AoE.BossConeAoE), typeof(AntColony.Boss.AoE.BossLineAoE) })
            {
                menu.SetSpeed(5); Check(Time.timeScale == 5, "5x fixture"); Cast(type);
                Check(Time.timeScale == 1 && SimulationPause.Speed == 1, "real " + type.FullName + $" telegraph slows whole simulation: scale={Time.timeScale},stored={SimulationPause.Speed},policy={UserSettings.Current.autoPauseThreat},game={GameSession.Instance.GameStarted},busy={SaveSystem.Busy},menu={menu.ScreenName}");
                menu.ToggleSimulation(); Check(Time.timeScale == 0, "manual pause"); menu.ToggleSimulation();
                Check(Time.timeScale == 1, "resume uses slowed speed");
            }
            quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false); menu.SetSpeed(5); Cast(typeof(AntColony.Boss.AoE.BossCircleAoE));
            Check(Time.timeScale == 5 && SimulationPause.Speed == 5, "opt out preserves player speed");
            quiet.autoPauseThreat = true; UserSettings.Apply(quiet, false); SimulationPause.UserSpeed(0); Cast(typeof(AntColony.Boss.AoE.BossCircleAoE));
            Check(Time.timeScale == 0 && SimulationPause.Reasons.Contains("manual"), "telegraph cannot unpause game");
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var tab = typeof(GameMenuController).GetField("settingsTab", flags); var oldTab = tab.GetValue(menu); tab.SetValue(menu, 3);
            typeof(GameMenuController).GetMethod("Settings", flags).Invoke(menu, null);
            Check(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text.Contains("새 위협 자동 정지 · 보스 예고 1배속")), "setting explains policy"); tab.SetValue(menu, oldTab); menu.Resume();
            return "PASS " + count + " boss timing checks";
        }
        finally
        {
            foreach (var go in objects) if (go != null)
            {
                foreach (var aoe in go.GetComponents<MonoBehaviour>())
                    if (aoe.GetType().GetField("telegraph", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(aoe) is Component telegraph) UnityEngine.Object.DestroyImmediate(telegraph.gameObject);
                UnityEngine.Object.DestroyImmediate(go);
            }
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; SimulationPause.Restore(pause); Time.timeScale = 0;
        }
    }
}
