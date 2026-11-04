using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Review1029BossChecks
{
    public static async Task<string> Main()
    {
        await Task.CompletedTask;
        int count = 0;
        void Check(bool ok, string label) { if (!ok) throw new Exception(label); count++; }
        var report = new StringBuilder();
        foreach (int troops in new[] { 4, 5, 6 })
        {
            float hp = AntColony.Boss.BossEncounterRules.Health(AntColony.Boss.BossId.A), elapsed = 0;
            while (hp > 0 && elapsed < 1000)
            {
                if (!AntColony.Boss.BossEncounterRules.Flying(elapsed)) hp -= troops * 10;
                elapsed++;
            }
            Check(elapsed >= 300 && elapsed <= 600, "A melee exposure target 4~6 troops");
            report.AppendLine($"A melee, {troops} troops ×10 damage/1s, perfect dodge: {elapsed}s");
        }
        Check(!AntColony.Boss.BossEncounterRules.Flying(3) && AntColony.Boss.BossEncounterRules.Flying(8), "flight/landing boundaries");
        Check(AntColony.Boss.BossEncounterRules.Heat(AntColony.Boss.BossId.C) > 0, "B armor helps C heat");
        Check(AntColony.Boss.BossEncounterRules.AddLimit(AntColony.Boss.BossId.B) > 1, "C area weapon helps B guards");
        Check(AntColony.Units.EquipmentItem.BossReward(AntColony.Boss.BossId.B, "b").HeatResistance == .5f, "actual B heat reduction");
        Check(AntColony.Units.EquipmentItem.BossReward(AntColony.Boss.BossId.C, "c").AttackRadius == 3, "actual C area radius");
        var state = new AntColony.Boss.BossProgress.State(); var progress = new AntColony.Boss.BossProgress(state);
        Check(progress.Revealed(AntColony.Boss.BossId.A) && !progress.Revealed(AntColony.Boss.BossId.B), "exploration retains required detection");
        Check(progress.RecordKill(AntColony.Boss.BossId.A, 10), "first kill");
        Check(!progress.RecordKill(AntColony.Boss.BossId.A, 11), "kill once per cycle");
        Check(progress.TryAcquire(AntColony.Boss.BossId.A, "ship", out var parcel) && parcel.count == 2, "A gives both branch cores");
        Check(!progress.Recipe(AntColony.Boss.BossId.B), "acquisition does not replace home delivery");
        Check(progress.TryDeliver(parcel.id) && progress.Home(AntColony.Boss.BossId.A) == 2, "actual home delivery");
        Check(progress.Recipe(AntColony.Boss.BossId.B) && progress.Recipe(AntColony.Boss.BossId.C), "both orders available");
        Check(!progress.TryRespawn(AntColony.Boss.BossId.A, 1809, false, false, false), "respawn 1s early");
        Check(!progress.TryRespawn(AntColony.Boss.BossId.A, 1810, true, false, false), "allies hold respawn");
        Check(!progress.TryRespawn(AntColony.Boss.BossId.A, 1810, false, true, false), "transport holds respawn");
        Check(!progress.TryRespawn(AntColony.Boss.BossId.A, 1810, false, false, true), "combat holds respawn");
        Check(progress.TryRespawn(AntColony.Boss.BossId.A, 1810, false, false, false), "12 day respawn");
        Check(progress.RecordKill(AntColony.Boss.BossId.A, 1820) && progress.TryAcquire(AntColony.Boss.BossId.A, "ship", out var repeat)
            && repeat.id != parcel.id && repeat.cycle == 1, "repeat yields new single parcel");
        Check(!progress.TryAcquire(AntColony.Boss.BossId.A, "ship", out _), "repeat reward cannot duplicate");
        Check(AntColony.Boss.BossProgress.Validate(state), "campaign progression state valid");
        foreach (bool bFirst in new[] { true, false })
        {
            var branches = new AntColony.Boss.BossProgress.State(); var p = new AntColony.Boss.BossProgress(branches);
            foreach (var id in bFirst ? new[] { AntColony.Boss.BossId.B, AntColony.Boss.BossId.C } : new[] { AntColony.Boss.BossId.C, AntColony.Boss.BossId.B })
            {
                branches.revealed[(int)id] = true;
                Check(p.RecordKill(id, 0) && p.TryAcquire(id, "party", out var item) && p.TryDeliver(item.id), "branch delivery " + id);
            }
            Check(p.Recipe(AntColony.Boss.BossId.D) && p.DetectionBlock(AntColony.Boss.BossId.D) == "", "merge independent of branch order");
        }
        var encounter = new AntColony.Boss.BossEncounter.State { taken = true };
        Check(!AntColony.Boss.BossEncounter.Present(new AntColony.Boss.BossEncounter.State())
            && AntColony.Boss.BossEncounter.Validate(new AntColony.Boss.BossEncounter.State()), "null-to-empty JSON placeholder is not a captured encounter");
        Check(AntColony.Boss.BossEncounter.Validate(null) && AntColony.Boss.BossEncounter.Validate(encounter), "old and new encounter defaults");
        encounter.warning = true; Check(!AntColony.Boss.BossEncounter.Validate(encounter), "warning requires remaining time");
        encounter.warningRemaining = 1; Check(AntColony.Boss.BossEncounter.Validate(encounter), "saved telegraph resumes");
        encounter.elapsed = float.NaN; Check(!AntColony.Boss.BossEncounter.Validate(encounter), "invalid boss phase rejected");
        foreach (float distance in new[] { .16f, .31f, .46f })
        {
            float vehicle = AntColony.World.ExpeditionRules.TransportSeconds(distance, false);
            float aircraft = AntColony.World.ExpeditionRules.TransportSeconds(distance, true);
            Check(vehicle == aircraft * 2, "transport ring ratio");
            report.AppendLine($"distance={distance}: vehicle={vehicle}s aircraft={aircraft}s");
        }
        Check(AntColony.World.ExpeditionRules.TransportSeconds(.46f, false) == 70, "outer destination meaningful distance");
        return $"PASS {count} boss/expedition checks\n" + report;
    }
}
