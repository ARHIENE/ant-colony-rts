using System;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.UI;
using AntColony.Units;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-24 UI 육안 점검: 주요 화면을 하나씩 열어 Temp/ui-shots/<이름>.png로 찍는다(게임 진행 중 장면 그대로).
public static class UiShots
{
    static async Task Frames(int n) { var f = Time.frameCount; for (var i = 0; i < 200 && Time.frameCount < f + n; i++) await Task.Delay(20); }

    public static async Task<string> Main()
    {
        var dir = System.IO.Path.GetFullPath("Temp/ui-shots"); System.IO.Directory.CreateDirectory(dir);
        var menu = GameMenuController.Instance; var c = CommanderRoster.Instance.Commanders.FirstOrDefault();
        var map = Object.FindFirstObjectByType<WorldMapPanel>();
        var shots = new (string name, Action open)[]
        {
            ("00_hud", () => { }), ("01_pause", menu.Pause), ("02_roster", menu.Roster), ("03_population", menu.Population),
            ("04_science", menu.Science), ("05_diplomacy", menu.Diplomacy), ("06_work", menu.WorkSchedule), ("07_events", menu.EventLog),
            ("08_guide", menu.Guide), ("09_civic", menu.CivicServices), ("10_crafting", menu.GroundCrafting), ("11_conscription", menu.OpenConscription),
            ("12_health", () => menu.ShowHealth(c)), ("13_equipment", () => menu.ShowEquipment(c)), ("14_supplies", menu.CarriedSupplies),
            ("15_management", () => menu.Management("작업")), ("16_worldmap", () => map.Toggle()),
        };
        var done = 0;
        foreach (var (name, open) in shots)
        {
            try { open(); } catch (Exception e) { Debug.LogWarning("UiShots " + name + ": " + e.Message); }
            await Frames(4);
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, name + ".png")); await Frames(3); done++;
            if (name == "16_worldmap") map.Toggle(); else menu.Resume();
            await Frames(2);
        }
        return "SHOTS " + done;
    }
}
