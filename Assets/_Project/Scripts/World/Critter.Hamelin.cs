using System.Linq;
using AntColony.Core;

namespace AntColony.World
{
    public sealed partial class Critter
    {
        internal void NoteTamer(AntColony.Units.CommanderAnt c)
        {
            if (s.tame) return;
            s.tamer ??= new SoleWorker { mixed = s.tameProgress > 0 };
            s.tamer.Note(c.PersonalState.id, c.Traits.Has(AntColony.Units.CommanderTrait.DiamondHusbandry));
        }
        public bool HamelinProtected => s.tame && s.tamer?.Empowered == true && CommanderRoster.Instance != null
            && CommanderRoster.Instance.Commanders.Any(c => c.PersonalState.id == s.tamer.id && CampaignResearch.Present(c)
                && CampaignResearch.SettlementOf(c) == AnimalSettlement);
        private int AnimalSettlement => WorldMapManager.Instance == null ? -1 : WorldMapManager.Instance.Sites.ToList().FindIndex(site =>
            (site.transform.position - transform.position).sqrMagnitude < 100 * 100);
    }
}
