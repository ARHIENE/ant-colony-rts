using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.Units;
using UnityEngine;

// G01(2026-10-23): 병력 0은 부대 소진일 뿐, 장수는 1인분으로 계속 교전한다. 피해는 병력→개인 체력 순.
public static class ImplG01Checks
{
    static int count;
    static void Check(bool value, string text) { if (!value) throw new Exception("G01 FAIL " + count + ": " + text); count++; }

    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "G01-" + Guid.NewGuid().ToString("N"));
        try
        {
            var o = new NewGameOptions { startingCount = 1, seed = 261023, mapSize = MapSize.Small, biome = MapBiome.Forest,
                startingTeam = new[] { StartingCandidate.Roll(0) } };
            o.startingTeam[0].traits.values.Clear();
            CrashLandingStart.Begin(o);
            var end = DateTime.UtcNow.AddSeconds(90);
            while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(50);
            Check(!SaveSystem.Busy, "load complete"); Time.timeScale = 0;

            var c = CommanderRoster.Instance.Commanders.First();
            Check(!c.HasTroops, "crash start commander has no troops");
            Check(c.AttackDamage >= 1, "troopless commander deals solo damage: " + c.AttackDamage);
            var auto = typeof(SoldierAnt).GetProperty("AutoEngages", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(!(bool)auto.GetValue(c), "civilian troopless commander does not auto-engage");
            c.WorkState.duty = CommanderDuty.Deployed;
            Check((bool)auto.GetValue(c), "deployed troopless commander auto-engages");
            c.WorkState.duty = CommanderDuty.Civilian;

            var before = c.PersonalHealth;
            c.TakeDamage(10 + c.Armor);
            Check(c.PersonalHealth < before && c.PersonalHealth > 0 && !c.PersonalState.health.Downed, "damage goes to personal health, not downed: " + c.PersonalHealth);
            return "PASS ImplG01Checks " + count;
        }
        finally { SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
