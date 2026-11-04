using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.World
{
    public enum InvestigationOutcome { Investigating, Completed, Expelled, Captured, Interrupted, Reported }
    [Serializable] public sealed class InvestigationMission
    {
        public int ship, site;
        public string evaluator, escort = "", faction;
        public List<string> targets = new List<string>();
        public List<TalentReport> pending = new List<TalentReport>();
        public float elapsed;
        public bool riskChecked;
        public InvestigationOutcome outcome;
    }
    public sealed class InvestigationTarget
    {
        public string Id { get; internal set; }
        public string Name { get; internal set; }
        internal CommanderTalents Talents;
    }
    public static class InvestigationRules
    {
        public const float LocalSeconds = 150, RiskAt = 75;
        public const int SearchCount = 3;
        public static float Discovery(int skill, float relation, CommanderTraits traits) => Mathf.Clamp01((30 - Mathf.Clamp(skill, 0, 20)) * .01f * relation * TraitRules.InvestigationMultiplier(traits));
        public static float Capture(int skill, CommanderTraits traits) => Mathf.Clamp01((50 - 2 * Mathf.Clamp(skill, 0, 20)) * .01f * TraitRules.InvestigationMultiplier(traits));
    }
    public partial class WorldMapManager
    {
        private TalentAssessment assessment;
        public TalentAssessment Assessment => assessment ?? (assessment = new TalentAssessment(this));
        private void Update() { if (!AntColony.Save.SaveSystem.Busy) Assessment.Tick(Time.deltaTime); }
    }
    public partial class ExpeditionTransport
    {
        public InvestigationMission Investigation { get; internal set; }
        public bool InvestigationActive => Investigation?.outcome == InvestigationOutcome.Investigating;
        public int RecommendedInvestigationFood(ExpeditionSite destination) => ExpeditionRules.RecommendedFood(
            Walking ? ExpeditionRules.WalkSeconds(destination) / FormationWalkMultiplier : Aircraft ? 10 : 20, DailyFood);
        // Crew boarding is the existing physical order queue: call once it has finished.
        public bool TryInvestigate(ExpeditionSite destination, CommanderAnt evaluator, CommanderAnt escort = null, string focus = null, bool allowInsufficientFood = false)
            => WorldMapManager.Instance != null && WorldMapManager.Instance.Assessment.TryDispatch(this, destination, evaluator, escort, focus, allowInsufficientFood);
        private bool TickInvestigation(float seconds)
        {
            bool local = InvestigationActive && State == ExpeditionState.Deployed && !IsWithdrawing;
            WorldMapManager.Instance?.Assessment.TickMission(this, seconds);
            return local;
        }
        private void ReportInvestigation() => WorldMapManager.Instance?.Assessment.Report(this);
        internal void ConcealInvestigators()
        {
            if (InvestigationActive && State == ExpeditionState.Deployed && !IsWithdrawing)
                foreach (var c in crew) if (c != null && !c.IsDead && !c.IsEmbarked) c.SetEmbarked(true, Position);
        }
        internal void ReturnInvestigators()
        {
            foreach (var c in crew) if (c != null && !c.IsDead) c.SetEmbarked(false, Position + Vector3.right * 2);
            TryReturn();
        }
    }
}
namespace AntColony.Core
{
    public sealed partial class TalentAssessment
    {
        public IReadOnlyList<InvestigationTarget> Targets(ExpeditionSite site)
        {
            var diplomacy = DiplomacyManager.Instance; var faction = diplomacy?.Faction(site);
            var result = new List<InvestigationTarget>();
            if (faction == null || faction.extinct) return result;
            var enemies = SceneObjects.All<EnemyCommander>().Where(e => !e.IsDead && !e.Allied
                && diplomacy.Faction(e.GetComponentInParent<ExpeditionSite>()) == faction);
            foreach (var e in enemies)
            {
                if (!string.IsNullOrEmpty(e.RebelId)) e.InvestigationId = e.RebelId;
                if (string.IsNullOrEmpty(e.InvestigationId)) e.InvestigationId = Guid.NewGuid().ToString("N");
                result.Add(new InvestigationTarget { Id = e.InvestigationId, Name = e.CommanderName, Talents = e.Talents });
            }
            foreach (var p in faction.prisoners.Concat(faction.rebels))
                if (p != null && !result.Any(t => t.Id == p.PersonalState.id))
                    result.Add(new InvestigationTarget { Id = p.PersonalState.id, Name = p.Name, Talents = p.Talents });
            return result;
        }
        public bool TryDispatch(ExpeditionTransport ship, ExpeditionSite site, CommanderAnt evaluator, CommanderAnt escort, string focus, bool allowInsufficientFood)
        {
            if (ship == null || ship.IsDead || ship.State != ExpeditionState.Home || ship.Route?.IsRunning == true || ship.PendingCrewOrderCount != 0
                || ship.Investigation != null || evaluator == null || evaluator.IsDead || evaluator.IsChild || evaluator.IsCaptive || !evaluator.IsColonyMember
                || evaluator.PersonalHealth <= 0 || evaluator.Health.Downed || evaluator.IsHostile || escort == evaluator
                || escort != null && (escort.IsDead || escort.IsChild || escort.IsCaptive || !escort.IsColonyMember || escort.IsHostile)
                || ship.Crew.Count != (escort == null ? 1 : 2) || !ship.Crew.Contains(evaluator) || escort != null && !ship.Crew.Contains(escort)
                || !allowInsufficientFood && ship.AvailableFood < ship.RecommendedInvestigationFood(site)) return false;
            var candidates = Targets(site).Where(t => string.IsNullOrEmpty(focus) ? !data.reports.Any(r => r.target == t.Id) : t.Id == focus).ToList();
            if (candidates.Count == 0) return false;
            var selected = new List<string>();
            while (candidates.Count > 0 && selected.Count < (string.IsNullOrEmpty(focus) ? 3 : 1))
            {
                int i = UnityEngine.Random.Range(0, candidates.Count); selected.Add(candidates[i].Id); candidates.RemoveAt(i);
            }
            var mission = new InvestigationMission { ship = AntColony.Save.SaveCatalog.ShipIndex(ship), site = AntColony.Save.SaveCatalog.SiteIndex(site),
                evaluator = evaluator.PersonalState.id, escort = escort?.PersonalState.id ?? "", faction = DiplomacyManager.Instance.Faction(site).id, targets = selected };
            if (mission.ship < 0 || !ship.TryDepart(site)) return false;
            data.missions.Add(mission); ship.Investigation = mission; return true;
        }
        internal void TickMission(ExpeditionTransport ship, float seconds)
        {
            var mission = ship.Investigation;
            if (mission == null || mission.outcome == InvestigationOutcome.Reported) return;
            var evaluator = Person(mission.evaluator);
            if (ship.IsDead || evaluator == null || evaluator.IsDead || evaluator.IsCaptive || !ship.Crew.Contains(evaluator))
            {
                mission.pending.Clear(); mission.outcome = evaluator != null && evaluator.IsCaptive ? InvestigationOutcome.Captured : InvestigationOutcome.Interrupted;
                return;
            }
            if (mission.outcome != InvestigationOutcome.Investigating)
            {
                if (ship.State == ExpeditionState.Deployed && !ship.IsWithdrawing) ship.ReturnInvestigators();
                return;
            }
            if (ship.State == ExpeditionState.Returning || ship.IsWithdrawing || ship.Site != world.Sites[mission.site])
            { mission.pending.Clear(); mission.outcome = InvestigationOutcome.Interrupted; return; }
            if (ship.State != ExpeditionState.Deployed) return;
            ship.ConcealInvestigators();
            mission.elapsed = Mathf.Min(150, mission.elapsed + seconds);
            if (!mission.riskChecked && mission.elapsed >= 75)
            {
                mission.riskChecked = true;
                var diplomacy = DiplomacyManager.Instance; var faction = diplomacy.Faction(ship.Site);
                int skill = evaluator.Talents.Level(CommanderActivity.Investigation);
                float relation = faction == null ? 1 : faction.war || faction.affinity < 0 ? 1.5f
                    : faction.affinity > 0 || faction.HasTreaty(TreatyKind.Alliance, diplomacy.Data.elapsed) ? .5f : 1;
                if (UnityEngine.Random.value < InvestigationRules.Discovery(skill, relation, evaluator.Traits))
                {
                    mission.pending.Clear();
                    if (UnityEngine.Random.value < InvestigationRules.Capture(skill, evaluator.Traits))
                    {
                        mission.outcome = InvestigationOutcome.Captured;
                        ship.DetachCommander(evaluator); evaluator.SetEmbarked(false, ship.Position);
                        if (evaluator.HasTroops) evaluator.TakeDamage(float.MaxValue);
                        evaluator.OnCaptured(); diplomacy.CapturedBy(evaluator, faction);
                        ship.Site.HoldInvestigator(evaluator);
                    }
                    else mission.outcome = InvestigationOutcome.Expelled;
                    ship.ReturnInvestigators(); return;
                }
            }
            if (mission.elapsed < 150) return;
            var targets = Targets(ship.Site);
            foreach (var id in mission.targets)
            {
                var t = targets.FirstOrDefault(x => x.Id == id);
                if (t == null) { mission.pending.Clear(); mission.outcome = InvestigationOutcome.Interrupted; ship.ReturnInvestigators(); return; }
                mission.pending.Add(Evaluate(evaluator, t.Id, t.Name, t.Talents));
            }
            mission.outcome = InvestigationOutcome.Completed;
            ship.ReturnInvestigators();
        }
        // Complete information is delivered only at the transport's successful home landing.
        internal void Report(ExpeditionTransport ship)
        {
            var mission = ship.Investigation; if (mission == null) return;
            var evaluator = Person(mission.evaluator);
            if (evaluator != null && !evaluator.IsDead && !evaluator.IsCaptive
                && (mission.outcome == InvestigationOutcome.Completed || mission.outcome == InvestigationOutcome.Expelled))
                foreach (var r in mission.pending) Publish(r);
            data.missions.Remove(mission); ship.Investigation = null;
        }
        public bool ExpelInvestigation(ExpeditionTransport ship)
        {
            var mission = ship?.Investigation; if (mission == null || mission.outcome == InvestigationOutcome.Captured) return false;
            if (mission.outcome != InvestigationOutcome.Completed && mission.outcome != InvestigationOutcome.Expelled) mission.pending.Clear();
            mission.outcome = InvestigationOutcome.Expelled;
            if (ship.State == ExpeditionState.Deployed) ship.ReturnInvestigators();
            return true;
        }
        public bool CancelInvestigation(ExpeditionTransport ship)
        {
            var mission = ship?.Investigation; if (mission == null) return false;
            if (mission.outcome == InvestigationOutcome.Captured) return false;
            if (mission.outcome != InvestigationOutcome.Completed && !(mission.outcome == InvestigationOutcome.Expelled && mission.pending.Count > 0))
            { mission.pending.Clear(); mission.outcome = InvestigationOutcome.Interrupted; }
            if (ship.State == ExpeditionState.Deployed) ship.ReturnInvestigators();
            return true;
        }
    }
}
