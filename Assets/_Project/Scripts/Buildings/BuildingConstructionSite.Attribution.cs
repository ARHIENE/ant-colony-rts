using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Buildings
{
    public partial class BuildingConstructionSite
    {
        internal WorkContribution Contributions = new WorkContribution();
        internal SoleWorker Artist = new SoleWorker();
        internal bool BabelUsed;
        public void ApplyWork(WorkerAnt worker, float work)
        {
            var applied = Mathf.Clamp(work, 0, RemainingWork);
            if (!(applied > 0)) return;
            if (IsArt && Contributions.total == 0 && RemainingWork < BuildTimeSeconds) Artist.mixed = true;
            var c = worker as CommanderAnt;
            Contributions.Add(c?.PersonalState.id, applied, c != null && c.Traits.Has(CommanderTrait.DiamondBuilding));
            if (IsArt) Artist.Note(c?.PersonalState.id ?? "", c != null && c.Traits.Has(CommanderTrait.DiamondArt));
            RemainingWork -= applied;
        }
        private void CompleteAttribution(BuildingBase building)
        {
            if (building == null || TryGetComponent<DemolitionSite>(out _)) return;
            building.Babel = new BabelState { used = BabelUsed, eligible = Contributions.Majority(BuildTimeSeconds) != null,
                work = BuildTimeSeconds, costs = Needs.ConvertAll(n => new BabelState.Cost { type = n.type, amount = n.need }) };
            if (building is Decoration decor)
            {
                decor.ArtistId = Artist.id;
                decor.DiamondArtwork = Artist.Empowered && decor.Quality == 3;
            }
        }
    }
}
