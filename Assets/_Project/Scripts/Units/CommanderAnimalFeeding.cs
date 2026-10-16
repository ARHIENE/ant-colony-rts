using AntColony.Core;
using AntColony.Data;
using AntColony.World;
using System.Linq;

namespace AntColony.Units
{
    public partial class CommanderAnt
    {
        private StoreBin patientFoodSource;

        private bool TickAnimalFeeding(Critter a, float seconds)
        {
            if (a == null || a.Clinic == null || !a.NeedsClinicFood) { CommandStop(); return false; }
            if (!feedLoaded)
            {
                var rm = ResourceManager.Instance;
                if (rm == null || !AnimalMedicalStock.Holds(this, feedType)) { CommandStop(); return false; }
                if (patientFoodSource == null || !patientFoodSource.Usable || patientFoodSource.Amount(feedType) < 1)
                    patientFoodSource = rm.Bins.Where(b => b.Spendable && b.Amount(feedType) > 0 && TryWorkApproach(b.Position, out _))
                        .OrderBy(b => (b.Position - Position).sqrMagnitude).FirstOrDefault();
                if (patientFoodSource == null) { CommandStop(); return false; }
                if (!Near(patientFoodSource.Position, 2.6f)) return true;
                if (rm.TakeFrom(patientFoodSource, feedType, 1, out var lots) != 1) { CommandStop(); return false; }
                AnimalMedicalStock.Release(this);
                RestoreCargo(1, feedType, lots);
                feedLoaded = true;
            }
            if (!Near(a.transform.position, 2f)) return true;
            if (!Work(seconds, GameBalance.CritterEatSeconds, AnimalTask.FeedPatient)) return true;
            a.Feed(1); RestoreCargo(0, feedType); feedLoaded = false; CommandStop(); return false;
        }
    }
}
