using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.World;

namespace AntColony.Core
{
    public sealed partial class TalentAssessment
    {
        public static bool ValidateReferences(AntColony.Save.SaveFileV1 file)
        {
            var state = file.assessment;
            if (state == null || !state.present) return true;
            var enemyIds = file.monsters.Where(m => !string.IsNullOrEmpty(m.investigationId)).Select(m => m.investigationId).ToArray();
            if (enemyIds.Distinct().Count() != enemyIds.Length || enemyIds.Any(id => id.Length > 200)) return false;
            foreach (var mission in state.missions)
            {
                var ship = file.world.transports[mission.ship];
                if (mission.outcome == InvestigationOutcome.Investigating || mission.outcome == InvestigationOutcome.Completed)
                {
                    if (ship.siteIndex != mission.site) return false;
                    var evaluator = file.commanders.Find(c => c.personalState.id == mission.evaluator);
                    if (evaluator == null || evaluator.location != 1 || evaluator.transportIndex != mission.ship) return false;
                    if (mission.escort != "" && !file.commanders.Any(c => c.personalState.id == mission.escort && c.location == 1 && c.transportIndex == mission.ship)) return false;
                }
                if (mission.outcome == InvestigationOutcome.Investigating && ship.state != 1 && ship.state != 2) return false;
            }
            return true;
        }
        public static bool Validate(TalentAssessmentState s, int ships, int sites)
        {
            // JsonUtility may turn absent/null state into an empty object. Only present opts in.
            if (s == null || !s.present) return true;
            bool N(float v) => !float.IsNaN(v) && !float.IsInfinity(v) && v >= 0;
            bool Id(string v) => !string.IsNullOrEmpty(v) && v.Length <= 200;
            bool Report(TalentReport r) => r != null && Id(r.target) && Id(r.evaluator) && r.name != null
                && N(r.at) && r.at <= s.clock && N(r.current) && r.current <= 260 && N(r.potentialFloor) && r.potentialFloor <= 200
                && N(r.potential) && r.potential >= r.potentialFloor && r.potential <= 200
                && N(r.potentialError) && r.potentialError <= 30 && (!r.exactPotential || r.potentialError == 0)
                && r.skills != null && r.skills.Length == 16 && r.skills.All(v => N(v) && v <= 26)
                && r.basis != null && r.basis.Length == 18 && r.basis.All(N) && r.basis[0] >= 32 && r.basis[17] >= 40
                && r.basis.Skip(1).Take(16).All(v => v >= 2)
                && r.basisVersion > 0 && r.basisVersion <= s.basisVersion;
            if (!N(s.clock) || !N(s.progress) || s.progress >= 10 || s.basisVersion < 0
                || s.evaluator == null || s.pendingTarget == null || s.evaluator.Length > 200 || s.pendingTarget.Length > 200
                || s.pendingTarget == "" && s.progress != 0
                || s.basis == null || s.basis.Length != 18 || !s.basis.All(N)
                || s.basisVersion > 0 && (s.basis[0] < 32 || s.basis[17] < 40 || s.basis.Skip(1).Take(16).Any(v => v < 2))
                || s.biases == null || s.reports == null || s.missions == null
                || s.biases.Count > 100000 || s.reports.Count > 100000 || s.missions.Count > ships) return false;
            var pairs = new HashSet<string>();
            foreach (var b in s.biases)
                if (b == null || !Id(b.evaluator) || !Id(b.target) || !pairs.Add(b.evaluator + ":" + b.target)
                    || b.values == null || b.values.Length != 18 || b.values.Any(v => float.IsNaN(v) || v < -1 || v > 1)) return false;
            if (s.reports.Any(r => !Report(r)) || s.reports.Select(r => r.target).Distinct().Count() != s.reports.Count) return false;
            var used = new HashSet<int>();
            foreach (var m in s.missions)
            {
                if (m == null || m.ship < 0 || m.ship >= ships || !used.Add(m.ship) || m.site < 0 || m.site >= sites
                    || !Id(m.evaluator) || m.escort == null || m.escort.Length > 200 || m.escort == m.evaluator || !Id(m.faction)
                    || !Enum.IsDefined(typeof(InvestigationOutcome), m.outcome) || m.outcome == InvestigationOutcome.Reported
                    || !N(m.elapsed) || m.elapsed > 150 || m.riskChecked && m.elapsed < 75
                    || !m.riskChecked && m.elapsed >= 75 && m.outcome == InvestigationOutcome.Investigating
                    || m.targets == null || m.targets.Count < 1 || m.targets.Count > 3 || m.targets.Any(t => !Id(t)) || m.targets.Distinct().Count() != m.targets.Count
                    || m.pending == null || m.pending.Any(r => !Report(r) || r.evaluator != m.evaluator || !m.targets.Contains(r.target))
                    || m.pending.Select(r => r.target).Distinct().Count() != m.pending.Count
                    || m.outcome == InvestigationOutcome.Completed && (m.elapsed != 150 || !m.riskChecked || m.pending.Count != m.targets.Count)
                    || m.outcome == InvestigationOutcome.Expelled && m.pending.Count > 0 && (m.elapsed != 150 || !m.riskChecked || m.pending.Count != m.targets.Count)
                    || m.outcome != InvestigationOutcome.Completed && m.outcome != InvestigationOutcome.Expelled && m.pending.Count != 0) return false;
            }
            return true;
        }
    }
}
