using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    [Serializable] public sealed class AssessmentBias
    {
        public string evaluator, target;
        public float[] values = new float[18]; // CA, 16 basic skills, PA
    }
    [Serializable] public sealed class TalentReport
    {
        public string target, name, evaluator;
        public float at, current, potential, potentialError, potentialFloor;
        public float[] skills = new float[16];
        public float[] basis = new float[18];
        public int basisVersion;
        public bool exactPotential, interested;
    }
    [Serializable] public sealed class TalentAssessmentState
    {
        public bool present;
        public string evaluator = "", pendingTarget = "";
        public float progress, clock;
        public int basisVersion;
        public float[] basis = new float[18];
        public List<AssessmentBias> biases = new List<AssessmentBias>();
        public List<TalentReport> reports = new List<TalentReport>();
        public List<InvestigationMission> missions = new List<InvestigationMission>();
    }
    public readonly struct TalentRating
    {
        public readonly float Current, PotentialLow, PotentialHigh, At;
        public readonly float[] Skills;
        public readonly bool ExactPotential;
        public readonly int BasisVersion, PotentialBasisVersion, CurrentBasisVersion;
        internal TalentRating(TalentReport r, float[] basis, int version)
        {
            Current = AssessmentRules.Stars(r.current, r.basis[0]);
            Skills = r.skills.Select((v, i) => AssessmentRules.Stars(v, r.basis[i + 1])).ToArray();
            var potentialBasis = r.exactPotential ? basis[17] : r.basis[17];
            PotentialLow = AssessmentRules.Stars(Mathf.Clamp(r.potential - r.potentialError, r.potentialFloor, 200), potentialBasis);
            PotentialHigh = AssessmentRules.Stars(Mathf.Clamp(r.potential + r.potentialError, r.potentialFloor, 200), potentialBasis);
            ExactPotential = r.exactPotential; At = r.at; BasisVersion = r.basisVersion; CurrentBasisVersion = version; PotentialBasisVersion = r.exactPotential ? version : r.basisVersion;
        }
    }
    public static class AssessmentRules
    {
        public const float InitialSeconds = 10, RefreshSeconds = 150;
        public static float Stars(float value, float median) => Mathf.Floor(Mathf.Clamp(3 + 2 * (value / median - 1), 1, 5) * 2 + .5f) / 2;
        public static float Error(int skill, CommanderTraits traits) => (30 - Mathf.Clamp(skill, 0, 20)) * TraitRules.InvestigationMultiplier(traits);
        public static float Median(IEnumerable<float> values, float floor)
        {
            var v = values.OrderBy(x => x).ToArray();
            return Mathf.Max(floor, v.Length == 0 ? 0 : (v[(v.Length - 1) / 2] + v[v.Length / 2]) * .5f);
        }
    }

    // Owned by the world; all saved references are stable personal IDs or persistent enemy IDs.
    public sealed partial class TalentAssessment
    {
        private readonly WorldMapManager world;
        internal TalentAssessmentState data = new TalentAssessmentState();
        public TalentAssessment(WorldMapManager world) { this.world = world; }
        public string EvaluatorId => data.evaluator;
        public string PendingTarget => data.pendingTarget;
        public float Progress => data.progress;
        public IReadOnlyList<TalentReport> Reports => data.reports;
        public IReadOnlyList<InvestigationMission> Missions => data.missions;
        private IEnumerable<CommanderAnt> Members => CommanderRoster.Instance == null ? Enumerable.Empty<CommanderAnt>()
            : CommanderRoster.Instance.Commanders.Where(c => c != null && c.IsColonyMember && !c.IsHostile && !c.IsCaptive && !c.IsChild);
        private CommanderAnt Person(string id) => CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c != null && c.PersonalState.id == id);
        public bool AssignEvaluator(CommanderAnt c)
        {
            if (c != null && !Members.Contains(c)) return false;
            var id = c == null ? "" : c.PersonalState.id;
            if (data.evaluator != id) { data.evaluator = id; data.pendingTarget = ""; data.progress = 0; }
            return true;
        }
        public void SetInterested(string target, bool value) { var r = data.reports.Find(x => x.target == target); if (r != null) r.interested = value; }
        private void UpdateBasis()
        {
            var members = Members.ToArray(); var basis = new float[18];
            basis[0] = AssessmentRules.Median(members.Select(c => c.Talents.Current), 32);
            for (int i = 0; i < 16; i++) { int skill = i; basis[i + 1] = AssessmentRules.Median(members.Select(c => (float)c.Talents.levels[skill]), 2); }
            basis[17] = AssessmentRules.Median(members.Select(c => (float)c.Talents.potential), 40);
            if (!basis.SequenceEqual(data.basis)) { data.basis = basis; data.basisVersion++; }
        }
        public TalentRating? Rating(string id)
        {
            var r = data.reports.Find(x => x.target == id); if (r == null) return null;
            UpdateBasis(); return new TalentRating(r, data.basis, data.basisVersion);
        }
        public void Tick(float seconds)
        {
            if (!(seconds > 0) || float.IsInfinity(seconds)) return;
            data.clock += seconds; UpdateBasis();
            var evaluator = Person(data.evaluator);
            if (evaluator == null || !Members.Contains(evaluator) || !evaluator.CivilianWorkReady || evaluator.IsEmbarked
                || evaluator.IsEating || evaluator.IsWashing || evaluator.IsPlaying || evaluator.IsInCombat) return;
            var remaining = seconds;
            while (remaining > 0)
            {
                var candidates = Members.Where(c => !c.IsAwayFromHome && !c.IsEmbarked).ToArray();
                bool Due(CommanderAnt c) { var r = data.reports.Find(x => x.target == c.PersonalState.id); return r == null || r.evaluator != data.evaluator || data.clock - r.at >= 150; }
                var target = candidates.FirstOrDefault(c => c.PersonalState.id == data.pendingTarget && Due(c));
                if (target == null) { target = candidates.FirstOrDefault(Due); data.pendingTarget = target?.PersonalState.id ?? ""; data.progress = 0; }
                if (target == null) break;
                var old = data.reports.Find(r => r.target == target.PersonalState.id);
                float required = old == null || old.evaluator != data.evaluator ? 10 : 0;
                float work = Mathf.Min(remaining, required - data.progress); data.progress += work; remaining -= work;
                if (data.progress < required) break;
                Publish(Evaluate(evaluator, target.PersonalState.id, target.CommanderName, target.Talents));
                data.pendingTarget = ""; data.progress = 0;
            }
        }
        internal TalentReport Evaluate(CommanderAnt evaluator, string id, string name, CommanderTalents talents)
        {
            UpdateBasis();
            var bias = data.biases.Find(b => b.evaluator == evaluator.PersonalState.id && b.target == id);
            if (bias == null)
            {
                bias = new AssessmentBias { evaluator = evaluator.PersonalState.id, target = id };
                for (int i = 0; i < 18; i++) bias.values[i] = UnityEngine.Random.Range(-1f, 1f);
                data.biases.Add(bias);
            }
            float error = AssessmentRules.Error(evaluator.Talents.Level(CommanderActivity.Investigation), evaluator.Traits);
            bool exact = evaluator.Traits.Has(CommanderTrait.DiamondInvestigation);
            var r = new TalentReport { target = id, name = name, evaluator = evaluator.PersonalState.id, at = data.clock,
                basisVersion = data.basisVersion, basis = (float[])data.basis.Clone(), current = Mathf.Max(0, talents.Current * (1 + bias.values[0] * error / 100)),
                potential = Mathf.Clamp(talents.potential + (exact ? 0 : bias.values[17] * error), talents.Current, 200), potentialError = exact ? 0 : error,
                exactPotential = exact, potentialFloor = talents.Current };
            for (int i = 0; i < 16; i++) r.skills[i] = Mathf.Max(0, talents.levels[i] * (1 + bias.values[i + 1] * error / 100));
            var prior = data.reports.Find(x => x.target == id);
            if (prior != null && prior.exactPotential) { r.potential = prior.potential; r.potentialError = 0; r.exactPotential = true; }
            return r;
        }
        internal void Publish(TalentReport report)
        {
            var prior = data.reports.Find(x => x.target == report.target);
            if (prior != null)
            {
                report.interested = prior.interested;
                if (prior.exactPotential) { report.potential = prior.potential; report.exactPotential = true; report.potentialError = 0; }
                data.reports.Remove(prior);
            }
            data.reports.Add(report);
        }
        public TalentAssessmentState Capture()
        {
            data.present = true;
            foreach (var mission in data.missions.ToArray())
            {
                var ship = world.Transports.FirstOrDefault(t => t != null && t.Investigation == mission);
                if (ship == null) data.missions.Remove(mission);
                else mission.ship = AntColony.Save.SaveCatalog.ShipIndex(ship);
            }
            return JsonUtility.FromJson<TalentAssessmentState>(JsonUtility.ToJson(data));
        }
        public void Restore(TalentAssessmentState state)
        {
            data = state != null && state.present ? JsonUtility.FromJson<TalentAssessmentState>(JsonUtility.ToJson(state)) : new TalentAssessmentState();
            foreach (var ship in world.Transports) if (ship != null)
            {
                ship.Investigation = data.missions.Find(m => m.ship == AntColony.Save.SaveCatalog.ShipIndex(ship));
                ship.ConcealInvestigators();
            }
        }
    }
}
