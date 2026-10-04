namespace AntColony.Regression
{
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using ResourceType = AntColony.Data.ResourceType;

// 기존 검사 실패를 현재 규칙과 분리해서 재현한다. 사용자 저장 슬롯은 사용하지 않는다.
public static class RegressionTriageChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL: " + label); count++; }
    static async Task Ready()
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(30);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    public static async Task<string> Main()
    {
        count = 0;
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Triage-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { mapSize = MapSize.Small, seed = 261004, biome = MapBiome.Garden }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            foreach (var c in CommanderRoster.Instance.Commanders) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
            var commander = CommanderRoster.Instance.Commanders[0];
            commander.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced));
            Check(commander.CanStartConstruction, "idle civilian can build; AcidTower fixture must stop autonomous work");
            Check(commander.SetJobEnabled(CommanderJobs.Cleaning, true), "4096 is now the valid cleaning job");
            Check(!commander.SetJobEnabled((CommanderJobs)8192, true), "unknown job still rejected");
            commander.SetJobEnabled(CommanderJobs.All, false);
            var node = new GameObject("Triage cargo").AddComponent<ResourceNode>(); node.ConfigureLoot(ResourceType.Soil, 100);
            var cargo = typeof(WorkerAnt).GetField("carriedAmount", Private);
            float carried = commander.LoadCapacity + 5, stock = node.AmountRemaining;
            cargo.SetValue(commander, carried);
            typeof(WorkerAnt).GetField("targetNode", Private).SetValue(commander, node);
            typeof(WorkerAnt).GetMethod("TickGathering", Private).Invoke(commander, null);
            Check(commander.CarriedAmount == carried && node.AmountRemaining == stock, "overcapacity cargo preserved without extracting");
            cargo.SetValue(commander, 0f); commander.CommandStop(); Object.Destroy(node.gameObject);

            var lab = new GameObject("Triage research lab").AddComponent<ResearchLab>();
            typeof(ResearchLab).GetField("role", Private).SetValue(lab, commander.Role);
            typeof(ResearchLab).GetField("researchTimeSeconds", Private).SetValue(lab, 3f);
            ResourceManager.Instance.Add(ResourceType.Food, 1000); ResourceManager.Instance.Add(ResourceType.Soil, 1000);
            Check(lab.TryResearchAttack(commander), "start timed research"); lab.Tick(1); lab.CancelResearch();
            int level = commander.LabAttackLevel;
            Check(lab.TryResearchAttack(commander), "restart canceled research"); lab.Tick(2);
            Check(lab.IsResearching && commander.LabAttackLevel == level, "old research time cannot finish restarted research early");
            lab.Tick(1); Check(!lab.IsResearching && commander.LabAttackLevel == level + 1, "restarted research completes exactly once");
            lab.Tick(10); Check(commander.LabAttackLevel == level + 1, "completion cannot repeat");
            Object.Destroy(lab.gameObject);

            var science = new GameObject("Triage science").AddComponent<ScienceLab>(); science.transform.position = commander.Position;
            Check(science.TryAssign(commander), "science fixture assigned"); commander.CommandStop();
            Check(science.Target == null && commander.ScienceAssignment == null, "CommandStop intentionally releases researcher before CampaignChecks saves");
            Object.Destroy(science.gameObject);
            var hospital = new GameObject("Triage infirmary").AddComponent<Infirmary>();
            Check(hospital.Nurse == null && Mathf.Approximately(hospital.TreatmentRate, GameBalance.UnnursedTreatment), "unattended treatment uses current nursing penalty");
            Object.Destroy(hospital.gameObject);

            var skill = Object.FindFirstObjectByType<CommandCard>().GetComponentsInChildren<UnityEngine.UI.Button>(true).Single(b => b.name == "Skill");
            Check(skill.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t => t.alignment == TextAnchor.MiddleCenter), "HUD skill label is centered, not LowerCenter");
            Check(Object.FindFirstObjectByType<WorkTargetPanel>() != null, "resource selection uses current HUD work target panel");
            var support = CommanderRoster.Instance.Commanders[1]; support.CommandStop();
            support.ApplyTraits(new CommanderTraits(CommanderPersonality.Balanced));
            var pheromone = new EquipmentItem { slot = EquipmentSlot.Weapon, weapon = WeaponKind.Pheromone, quality = 1 };
            Check(EquipmentInventory.Instance.Add(pheromone) && EquipmentInventory.Instance.Equip(support, pheromone), "support weapon equipped");
            Check(support.Agent.Warp(commander.Position), "support within range");
            Check(!commander.HasSupportAura, "civilian without troops has no support aura");
            support.WorkState.duty = CommanderDuty.Deployed; AntPool.Instance.Breed(2);
            Check(support.TryAssign(1) && commander.HasSupportAura, "deployed support troop grants aura");
            support.ReturnTroops(support.TroopCount);
            Check(!commander.HasSupportAura, "aura ends after troop return");
            var toast = Object.FindFirstObjectByType<ToastManager>();
            foreach (var field in new[] { "toasts", "crises", "dismissed" })
            {
                var collection = typeof(ToastManager).GetField(field, Private).GetValue(toast);
                collection.GetType().GetMethod("Clear").Invoke(collection, null);
            }
            ToastManager.Show("normal triage"); ToastManager.SetCrisis("triage", "crisis triage");
            Check(ToastManager.VisibleLines[0] == "crisis triage", "isolated crisis sorts before normal toast");
            ToastManager.Dismiss(0); Check(!ToastManager.VisibleLines.Contains("crisis triage"), "crisis dismissal works");
            ToastManager.SetCrisis("triage", null);
            return "PASS " + count + " regression triage checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}

}
