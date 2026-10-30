using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Units;
using AntColony.World;

namespace AntColony.Core
{
    public sealed partial class CampaignResearch
    {
        [Serializable] public sealed class PoliticalPresence { public string id; public int site = -1; public float remaining; }
        internal static int SettlementOf(CommanderAnt c) => c.Garrison != null ? WorldMapManager.Instance.Sites.ToList().IndexOf(c.Garrison.Site)
            : c.Transport?.State == ExpeditionState.Deployed ? WorldMapManager.Instance.Sites.ToList().IndexOf(c.Transport.Site) : -1;
        internal static bool Present(CommanderAnt c) => c != null && c.IsColonyMember && !c.IsDead && !c.IsCaptive
            && (c.Transport == null || c.Transport.State == ExpeditionState.Deployed) && !c.PersonalState.social.departure.Equals(DepartureState.Left);
        private void TickPoliticalPresence(float seconds)
        {
            state.politicalPresence ??= new List<PoliticalPresence>();
            foreach (var p in state.politicalPresence) p.remaining = Math.Max(0, p.remaining - seconds);
            foreach (var c in CommanderRoster.Instance?.Commanders.AsEnumerable() ?? Enumerable.Empty<CommanderAnt>())
            {
                if (!Present(c) || !c.Traits.Has(CommanderTrait.DiamondPolitics)) continue;
                var site = SettlementOf(c);
                var p = state.politicalPresence.Find(p => p.id == c.PersonalState.id && p.site == site);
                if (p == null) { p = new PoliticalPresence { id = c.PersonalState.id, site = site }; state.politicalPresence.Add(p); }
                p.remaining = GameCalendar.SecondsPerDay * 3;
            }
            state.politicalPresence.RemoveAll(p => p.remaining <= 0);
        }
        public static float PoliticalMultiplier(int count) => (float)(1 - .6 * (1 - Math.Pow(.5, Math.Max(0, count))));
        public float SettlementDiscontentMultiplier(int site)
        {
            // 다른 정착지에 현존하면 이전 정착지의 부재 잔여보다 현지 효과를 우선한다.
            var locals = CommanderRoster.Instance?.Commanders.Where(Present).ToDictionary(c => c.PersonalState.id, SettlementOf);
            return PoliticalMultiplier(state.politicalPresence?.Count(p => p.site == site && p.remaining > 0
                && (locals == null || !locals.TryGetValue(p.id, out var current) || current == site)) ?? 0);
        }
        public float DiscontentMultiplier(string id)
        {
            var c = CommanderRoster.Instance?.Commanders.FirstOrDefault(c => c.PersonalState.id == id);
            return Present(c) ? SettlementDiscontentMultiplier(SettlementOf(c)) : 1;
        }
        private static bool ValidPoliticalPresence(State s) => s.politicalPresence == null || s.politicalPresence.Count <= 10000
            && s.politicalPresence.All(p => p != null && !string.IsNullOrEmpty(p.id) && p.id.Length <= 1000 && p.site >= -1
                && p.remaining >= 0 && p.remaining <= GameCalendar.SecondsPerDay * 3)
            && s.politicalPresence.Select(p => (p.id, p.site)).Distinct().Count() == s.politicalPresence.Count;
    }
}
