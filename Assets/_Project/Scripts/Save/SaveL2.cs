using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Boss;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.World;

namespace AntColony.Save
{
    public static class SaveL2
    {
        public static void Normalize(SaveFileV1 f)
        {
            foreach (var c in (f.diplomacy?.civilizations ?? new List<Civilization>()).Concat(f.diplomacy?.markets ?? new List<Civilization>()))
                if (c != null && c.era == 0 && c.eraWork == 0 && !c.eraPaid) c.era = 1;
            if (f.history != null) { f.history.summaries ??= new List<CampaignHistory.Summary>(); if (CampaignHistory.Validate(f.history)) CampaignHistory.Compact(f.history); }
            if (f.campaign?.bosses != null && (f.campaign.bosses.guardRewards == null || f.campaign.bosses.guardRewards.Length == 0)) f.campaign.bosses.guardRewards = new int[4];
            foreach (var m in f.monsters ?? new List<MonsterDto>())
                if (m != null && m.guardVersion == 0) { m.guardBoss = -1; m.guardCycle = 0; m.guardXpClaimed = false; }
            foreach (var b in f.buildings)
                if (b?.airship != null) { b.airship.reservedCommanders ??= new List<string>(); b.airship.reservedGroups ??= new List<int>(); }
        }
        public static bool Valid(SaveFileV1 f)
        {
            if (f.construction?.Any(c => c.rocket?.reservedCommanders?.Count > 0 || c.rocket?.reservedGroups?.Count > 0) == true) return false;
            var boarded = new HashSet<string>(f.buildings.Where(b => b.kind == "AirshipYard" && b.airship != null)
                .SelectMany(b => b.airship.passengers).Select(i => f.commanders[i].personalState.id));
            var commanders = new HashSet<string>(); var groups = new HashSet<int>();
            foreach (var b in f.buildings.Where(b => b.kind == "AirshipYard"))
            {
                var s = b.airship; if (s == null) continue;
                if (s.reservedCommanders == null || s.reservedGroups == null || s.reservedCommanders.Count > 10000 || s.reservedGroups.Count > 10000) return false;
                if ((b.health <= 0 || s.phase == RocketPhase.Launched || s.phase == RocketPhase.Destroyed)
                    && (s.reservedCommanders.Count > 0 || s.reservedGroups.Count > 0)) return false;
                foreach (var id in s.reservedCommanders)
                    if (id == null || !commanders.Add(id) || !f.commanders.Any(c => c.personalState.id == id && !c.personalState.dead && c.location == 0)
                        || boarded.Contains(id)) return false;
                foreach (var id in s.reservedGroups)
                    if (!groups.Add(id) || f.population.civilianLocations?.Any(g => g.id == id && g.count > 0 && (g.moving || g.arrived) && !g.housing && !g.temporary && !g.rehousing) != true) return false;
            }
            var jobs = f.buildings.Where(b => b.kind == "Workshop").SelectMany(b => b.workshop.jobs).Where(j => j.bossJob).ToArray();
            if (jobs.Length > 1) return false;
            foreach (var j in jobs)
            {
                var s = f.campaign.bosses;
                if (s == null || j.boss < 0 || j.boss > 3 || s.craftBoss != j.boss || s.craftRecovery != j.recovery
                    || Math.Abs(BossProgress.RecoverySeconds - j.work - s.craftRemaining) > .01f) return false;
            }
            foreach (var m in f.monsters)
                if (m.guardVersion < 0 || m.guardVersion > 1 || m.guardBoss < -1 || m.guardBoss > 3 || m.guardCycle < 0 || m.guardBoss < 0 && (m.guardCycle != 0 || m.guardXpClaimed)
                    || m.guardBoss >= 0 && (!m.hasRuntime || m.runtime == null || m.runtime.parentSite < 0
                        || f.campaign.bosses == null || m.guardCycle > f.campaign.bosses.cycle[m.guardBoss])) return false;
            return true;
        }
    }
}
