using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;

namespace AntColony.Core
{
    public sealed partial class CampaignResearch
    {
        [Serializable] public sealed class Contribution { public int technology; public WorkContribution work = new WorkContribution(); }
        private WorkContribution WorkFor(int technology)
        {
            state.contributions ??= new List<Contribution>();
            var record = state.contributions.Find(c => c.technology == technology);
            if (record == null) { record = new Contribution { technology = technology }; state.contributions.Add(record); }
            return record.work;
        }
        private void CreditResearch(CommanderAnt c, float work)
        {
            work = Math.Max(0, Math.Min(work, Active.Work - state.progress));
            WorkFor(state.active).Add(c.PersonalState.id, work, c.Traits.Has(CommanderTrait.DiamondResearch));
            state.progress += work;
        }
        private void GrantInsight(ScienceDefinition completed)
        {
            if (WorkFor((int)completed.Technology).Majority(completed.Work) == null) return;
            state.insights ??= new List<int>();
            var next = InsightSuccessor(completed.Technology, state.completed, state.insights);
            if (next == null) return;
            state.insights.Add((int)next.Technology);
            AntColony.UI.ToastManager.Show("천기 · " + next.Name + " 필요 연구량20% 통찰(선택/해금 후 적용)");
        }
        public static ScienceDefinition InsightSuccessor(ScienceTechnology completed, IEnumerable<int> known, IEnumerable<int> rewarded) =>
            Technologies.FirstOrDefault(t => t.Prerequisites.Contains(completed) && !known.Contains((int)t.Technology) && !rewarded.Contains((int)t.Technology));
        public float Insight(ScienceTechnology technology) => state.insights?.Contains((int)technology) == true ? Technologies[(int)technology].Work * .2f : 0;
        public float PendingInsight(ScienceTechnology technology) => state.insightApplied?.Contains((int)technology) == true ? 0 : Insight(technology);
        private void ApplyInsight()
        {
            state.insightApplied ??= new List<int>();
            if (state.insights?.Contains(state.active) != true || state.insightApplied.Contains(state.active)) return;
            state.insightApplied.Add(state.active); state.progress += Insight((ScienceTechnology)state.active);
        }
        private static bool ValidInsight(State s) => (s.insights == null || s.insights.Count <= Technologies.Length && s.insights.Distinct().Count() == s.insights.Count && s.insights.All(i => i >= 0 && i < Technologies.Length))
            && (s.insightApplied == null || s.insightApplied.Distinct().Count() == s.insightApplied.Count && s.insightApplied.All(i => s.insights?.Contains(i) == true))
            && (s.contributions == null || s.contributions.Count <= Technologies.Length && s.contributions.Select(c => c?.technology).Distinct().Count() == s.contributions.Count
                && s.contributions.All(c => c != null && c.technology >= 0 && c.technology < Technologies.Length && c.work?.Valid == true));
    }
}
