using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Save
{
    internal static class SaveUnderground
    {
        internal static void Capture(SaveFileV1 file, List<CommanderAnt> commanders)
        {
            foreach (var d in file.buildings)
            {
                if (d.kind != "UndergroundScanner") continue;
                var scanner = SaveReferences.Get<UndergroundScanner>("building:" + d.key);
                if (scanner == null) continue;
                d.surveyPresent = true; d.surveyCommander = commanders.IndexOf(scanner.Target);
                d.surveyCandidate = scanner.CandidateId;
            }
        }
        internal static void Restore(List<(BuildingBase building, BuildingDto dto)> buildings, List<CommanderAnt> commanders)
        {
            foreach (var pair in buildings)
                if (pair.building is UndergroundScanner scanner && pair.dto.surveyPresent)
                    scanner.RestoreAssignment(pair.dto.surveyCommander < 0 ? null : commanders[pair.dto.surveyCommander], pair.dto.surveyCandidate);
        }
        internal static bool Valid(SaveFileV1 f)
        {
            if (!UndergroundSurvey.Valid(f.underground, f.world.sites.Count + 1)) return false;
            var assigned = new HashSet<int>(); var reserved = new HashSet<int>();
            foreach (var b in f.buildings)
            {
                if (!b.surveyPresent) { if (b.kind == "UndergroundScanner") return false; continue; }
                if (f.underground?.present != true || b.kind != "UndergroundScanner" || b.surveyCommander < -1
                    || b.surveyCommander >= f.commanders.Count || b.surveyCandidate < -1 || b.health <= 0 && b.surveyCommander >= 0) return false;
                if (b.surveyCommander >= 0)
                {
                    var c = f.commanders[b.surveyCommander];
                    if (!assigned.Add(b.surveyCommander) || !c.activeInScene || c.personalState.dead || c.location != 0
                        || b.scientist >= 0 || b.labResearchCommanderId >= 0
                        || f.buildings.Any(other => other.scientist == b.surveyCommander || other.labResearchCommanderId == b.surveyCommander
                            || other.serviceCommander == b.surveyCommander || other.workshop?.crafter == b.surveyCommander)) return false;
                }
                if (b.surveyCandidate < 0) continue;
                if (b.surveyCommander < 0 || !reserved.Add(b.surveyCandidate)) return false;
                var layer = f.underground.layers.FirstOrDefault(l => l.id == b.surveyCandidate);
                if (layer == null || layer.discovered || b.packed) return false;
                var delta = layer.position - b.position.ToVector3(); delta.y = 0;
                if (delta.sqrMagnitude > UndergroundScanner.Radius * UndergroundScanner.Radius) return false;
            }
            return true;
        }
    }
}
