using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using UnityEngine;
using Object = UnityEngine.Object;

// 2026-10-05 6시대·로켓: 시대 매핑, 연구소 6등급, v13 저장 이전, 발사 문구.
public static class EraChecks
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception("FAIL " + message); checks++; }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        string original = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Era-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261005, mapSize = MapSize.Small }); await Ready();
            Check(CampaignResearch.EraNames.SequenceEqual(new[] { "소굴", "증기", "석유", "전기", "원자", "미래" }), "six eras");
            Check(GameBalance.ScienceWork.SequenceEqual(new float[] { 200, 400, 800, 1500, 2800, 5000 }), "era research work");
            var defs = CampaignResearch.Technologies;
            int Tier(ScienceTechnology t) => defs[(int)t].Tier;
            Check(Tier(ScienceTechnology.FungalFarming) == 1 && Tier(ScienceTechnology.Vehicle) == 2 && Tier(ScienceTechnology.Aircraft) == 4
                && Tier(ScienceTechnology.HeavyTransport) == 4 && Tier(ScienceTechnology.Engine) == 6 && Tier(ScienceTechnology.MigrationTheory) == 6, "old eras mapped");
            Check(!defs.Any(d => d.Tier == 3 || d.Tier == 5), "oil/atomic eras empty (TODO)");
            Check(defs[(int)ScienceTechnology.MigrationTheory].Name == "로켓 이론", "rocket theory");

            var template = Object.FindObjectsByType<ScienceLab>(FindObjectsInactive.Include).First(x => x.name.EndsWith("Template"));
            var lab = Object.Instantiate(template, CommanderRoster.Instance.Commanders[0].Position + Vector3.forward * 4, Quaternion.identity);
            lab.name = "ScienceLab"; lab.gameObject.SetActive(true);
            var rm = ResourceManager.Instance;
            foreach (AntColony.Data.ResourceType type in Enum.GetValues(typeof(AntColony.Data.ResourceType))) { rm.AddCapacity(type, 10000); rm.Add(type, 10000); }
            for (var i = 1; i < ScienceLab.MaxTier; i++) Check(lab.TryUpgrade(), "upgrade to " + (i + 1));
            Check(lab.Tier == 6 && !lab.TryUpgrade(), "lab max tier 6");

            // v13 저장: 연구소 3등급(항공) → 4등급(전기), 4등급 → 6등급, 줄어든 연구량에 맞춰 진행도 자르기
            lab.RestoreAssignment(3, null);
            var file = SaveSnapshot.Capture();
            file.version = 13;
            file.campaign.active = (int)ScienceTechnology.FungalFarming; file.campaign.progress = 250; // 옛 연구량 300, 새 200
            Check(SaveValidator.Validate(file, out var error), "v13 migrates: " + error);
            Check(file.version == SaveFileV1.CurrentVersion && SaveFileV1.CurrentVersion == 15, "save version 15");
            Check(file.buildings.Any(b => b.kind == "ScienceLab" && b.scienceTier == 4), "lab tier 3 -> 4");
            Check(file.campaign.progress < 200, "progress clamped");

            GameMenuController.Instance.Science();
            Check(GameMenuController.Instance.ScreenName == "Science / Rocket", "science screen renamed");
            Object.Destroy(lab.gameObject);
            return "PASS " + checks + " era checks";
        }
        finally { SaveStorage.RootOverride = original; }
    }
}
