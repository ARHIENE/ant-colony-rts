using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class Phase2Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    public static async Task<string> Main()
    {
        checks = 0;
        if (!Application.isPlaying) throw new Exception("Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        if (!GameSession.Instance.GameStarted)
        {
            SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small });
            while (SaveSystem.Busy) await Task.Delay(50);
        }
        Time.timeScale = 0;
        var gm = GameManager.Instance;
        var research = CampaignResearch.Instance;
        var savedResearch = research.CaptureState();
        var savedFishing = gm.FishingUnlocked;
        var commanders = CommanderRoster.Instance.Commanders.ToArray();
        var dead = commanders.Select(c => c.PersonalState.dead).ToArray();
        var c0 = commanders[0];
        var health = c0.PersonalState.work.health;
        var captor = c0.Captor;
        var embarked = c0.IsEmbarked;
        var treating = c0.PersonalState.treating;
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var defeated = typeof(GameManager).GetField("defeated", flags);
        var wasDefeated = (bool)defeated.GetValue(gm);
        var evaluate = typeof(GameManager).GetMethod("LateUpdate", flags);
        void Evaluate() => evaluate.Invoke(gm, null);
        void Reset() => defeated.SetValue(gm, false);
        bool Lost() => (bool)defeated.GetValue(gm);
        var testBarracks = new GameObject("Phase2Barracks");
        try
        {
            Check(AntColony.Data.ResourceType.Soil.DisplayName() == "재료" && (int)AntColony.Data.ResourceType.Soil == 1, "material label and serialized enum");
            Check((int)ScienceTechnology.Engine == 29 && (int)ScienceTechnology.Fishing == 30, "existing science IDs preserved");
            Check(CampaignResearch.Technologies.All(d => CampaignResearch.Technologies[(int)d.Technology] == d), "science array matches IDs");
            research.RestoreState(new CampaignResearch.State());
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, false);
            var barracks = testBarracks.AddComponent<Barracks>();
            typeof(Barracks).GetField("currentTier", flags).SetValue(barracks, 2);
            AntPool.Instance.Breed(Math.Max(0, ScienceLab.RequiredPopulation - AntPool.Instance.Total));
            Check(ScienceLab.PrerequisitesMet, "science lab does not require fishing");
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, true);
            Check(research.Has(ScienceTechnology.Fishing) && !research.TryStart(ScienceTechnology.Fishing), "legacy fishing unlock blocks duplicate charge");
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, false);
            Check(!research.BlockReason(ScienceTechnology.Fishing).Contains("이전 저장"), "queen fishing research removed (Phase 4)");

            Reset();
            foreach (var building in Object.FindObjectsByType<BuildingBase>()) gm.UnregisterBuilding(building);
            Evaluate(); Check(!Lost(), "no buildings with surviving commanders is not defeat");
            foreach (var building in Object.FindObjectsByType<BuildingBase>()) gm.RegisterBuilding(building);
            foreach (var c in commanders) c.PersonalState.dead = true;
            c0.PersonalState.dead = false;
            c0.PersonalState.work.health = 0;
            Evaluate(); Check(!Lost(), "recoverable downed commander counts");
            c0.PersonalState.work.health = health;
            c0.PersonalState.treating = true;
            Evaluate(); Check(!Lost(), "treatment counts");
            c0.PersonalState.treating = false;
            typeof(CommanderAnt).GetProperty("IsEmbarked").SetValue(c0, true);
            Evaluate(); Check(!Lost(), "transport passenger counts");
            typeof(CommanderAnt).GetProperty("IsEmbarked").SetValue(c0, false);
            typeof(CommanderAnt).GetProperty("Captor").SetValue(c0, WorldMapManager.Instance.Sites[0]);
            Evaluate(); Check(Lost(), "all dead or captive defeats despite buildings");
            Reset();
            typeof(CommanderAnt).GetProperty("Captor").SetValue(c0, null);
            c0.PersonalState.dead = true;
            Evaluate(); Check(Lost(), "all dead defeats");
            Reset();
            GameSession.Instance.MarkNotStarted();
            Evaluate(); Check(!Lost(), "main menu does not defeat");
            GameSession.Instance.MarkStarted();
            return "PASS " + checks + " Phase 2 checks";
        }
        finally
        {
            for (var i = 0; i < commanders.Length; i++) commanders[i].PersonalState.dead = dead[i];
            c0.PersonalState.work.health = health; c0.PersonalState.treating = treating;
            typeof(CommanderAnt).GetProperty("Captor").SetValue(c0, captor);
            typeof(CommanderAnt).GetProperty("IsEmbarked").SetValue(c0, embarked);
            defeated.SetValue(gm, wasDefeated);
            research.RestoreState(savedResearch);
            typeof(GameManager).GetProperty("FishingUnlocked").SetValue(gm, savedFishing);
            foreach (var building in Object.FindObjectsByType<BuildingBase>()) gm.RegisterBuilding(building);
            Object.Destroy(testBarracks);
            Time.timeScale = 0;
        }
    }
}
