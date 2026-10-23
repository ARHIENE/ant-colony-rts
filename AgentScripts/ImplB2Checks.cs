using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplB2Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("B2 FAIL " + checks + ": " + label); checks++; }
    static void Counts(int free, int assigned = 0, int reserved = 0) => typeof(AntPool)
        .GetMethod("RestoreCounts", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(AntPool.Instance, new object[] { free, assigned, reserved });
    static ColonyPopulation Reset(int free = 0, int assigned = 0, int reserved = 0)
    {
        Counts(free, assigned, reserved);
        var pop = ColonyPopulation.Instance;
        pop.Restore(new ColonyPopulation.State { taxRate = 0 });
        return pop;
    }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode required");
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261024, mapSize = MapSize.Small });
        while (SaveSystem.Busy) await Task.Delay(50);
        Time.timeScale = 0;
        var random = UnityEngine.Random.state;
        var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { BuildingKind.Hut, UnitRole.Worker });
        var hut = Object.Instantiate(template, Object.FindAnyObjectByType<Stockpile>().Position + Vector3.right * 12, Quaternion.identity);
        hut.SetActive(true); await Task.Yield();
        try
        {
            var pop = Reset(); var pool = AntPool.Instance;
            Check(AntColony.Core.GameBalancePopulation.ImmigrationSeconds == 450 && GameCalendar.SecondsPerYear == 3600, "G33/G38 clocks");
            Check(pop.HousingCapacity > 0 && pop.ImmigrationDemand == 1 && pop.Administration == 0, "G33 empty colony neutral corrections and zero admin denominator");
            Check(AntColony.Core.GameBalancePopulation.Demand(-9, 0, 0, 0) == 0 && AntColony.Core.GameBalancePopulation.Demand(9, 0, 0, 0) == 2, "G33 demand clamp");
            var capacity = pop.HousingCapacity;
            pop.Tick(449);
            Check(pop.Total == 0 && pop.S.immigrationSeconds == 449, "G33 no arrivals before three days");
            pop.Tick(1);
            Check(pop.Total == Mathf.FloorToInt(capacity * .1f) && Mathf.Abs(pop.S.immigrationRemainder - (capacity * .1f % 1)) < .001f, "G33 empty seats times ten percent, integer arrivals only");
            pop = Reset(capacity - 1); pop.S.immigrationRemainder = .01f;
            var expected = .01f + .1f * pop.ImmigrationDemand;
            pop.Immigrate(); Check(pop.Total == capacity - 1 && Mathf.Abs(pop.S.immigrationRemainder - expected) < .001f, "G33 fractional accumulation without minimum-one arrival");
            pop.S.immigrationRemainder = .99f; pop.Immigrate();
            Check(pop.Total == capacity && pop.S.immigrationRemainder < 1, "G33 accumulated fraction admits one and respects housing");
            var remainder = pop.S.immigrationRemainder;
            pop.Immigrate(); Check(pop.Total == capacity && pop.S.immigrationRemainder == remainder, "G33 full housing adds no backlog");
            pop = Reset(); pop.S.immigrationSeconds = 12; pop.S.immigrationRemainder = .4f;
            pop.SetImmigrationStopped(true); pop.Tick(900); pop.Immigrate();
            Check(pop.Total == 0 && pop.S.immigrationSeconds == 12 && pop.S.immigrationRemainder == .4f, "G33 stopped reception accrues neither time nor arrivals");
            var saved = pop.Capture(); pop.Restore(saved);
            Check(pop.S.immigrationStopped && pop.S.immigrationRemainder == .4f && pop.S.immigrationSeconds == 12, "G33 state JSON roundtrip");

            var split = AntColony.Core.GameBalancePopulation.ArrivalAges(10, .1f, .1f);
            Check(split.SequenceEqual(new[] { 1, 8, 1 }), "G38 ten eighty ten split");
            split = AntColony.Core.GameBalancePopulation.ArrivalAges(7, .1f, .1f);
            Check(split.SequenceEqual(new[] { 1, 5, 1 }), "G38 largest remainders");
            for (var i = 0; i < 50; i++)
            {
                split = AntColony.Core.GameBalancePopulation.RollArrivalAges(10000);
                Check(split.Sum() == 10000 && split[0] >= 500 && split[0] <= 1500 && split[1] >= 7500 && split[1] <= 8500
                    && split[2] >= 500 && split[2] <= 1500, "G38 all three shares stay within five percentage points");
                pop = Reset(); pop.Immigrate();
                Check(pop.S.ages != null && pop.S.ages.Take(18).Sum() == pop.S.young && pop.S.ages.Skip(18).Sum() == ColonyPopulation.Adults,
                    "G38 arrivals populate age aggregates");
            }
            pop = Reset(1, 1); pop.S.young = 1; pop.S.ages = new int[65];
            pop.S.ages[17] = 1; pop.S.ages[63] = 1; pop.S.ages[64] = 1;
            pop.SetImmigrationStopped(true); pop.Tick(3599);
            Check(pop.S.young == 1 && pop.S.old == 0 && pool.Total == 2, "G38 no monthly age transition or old death");
            pop.Tick(1);
            Check(pop.S.young == 0 && pop.S.old == 1 && ColonyPopulation.Adults == 2 && pop.Total == 3, "G38 eighteen adult and sixty-five elder at year boundary");
            Check(pop.S.ages[18] == 1 && pop.S.ages[64] == 1, "G38 age cohorts shift exactly one year");
            pop = Reset(0, 2); pop.S.ages = new int[65]; pop.S.ages[64] = 2;
            pop.AgeYear();
            Check(pop.S.old == 2 && pop.S.agingDue == 2 && ColonyPopulation.Adults == 0 && pop.Total == 2 && pop.Taxpayers == 0, "G38 mobilized ants age and remain in population, excluded from tax");
            pool.LoseAssigned(1);
            Check(pop.Total == 1 && pop.S.old == 1 && pop.S.agingDue == 1, "G38 combat death immediately removes population");
            pool.ReturnAssigned(1);
            Check(pop.Total == 1 && pop.S.old == 1 && pop.S.agingDue == 0 && pool.Total == 0 && pop.Taxpayers == 1, "G38 elder return settles pool without double aging");
            pop = Reset(2, 3, 1);
            Check(pop.Total == 6 && pop.Taxpayers == 3, "G38 assigned included in total and excluded from tax");
            pool.LoseAssigned(2); Check(pop.Total == 4 && pop.Taxpayers == 3, "G38 adult combat deaths immediate");
            pop.S.young = 2; pop.Monthly(); Check(pop.S.young == 2, "G38 Monthly no longer grows all children");
            pop.AgeYear(); saved = pop.Capture(); pop.Restore(saved);
            Check(pop.S.ages.SequenceEqual(saved.ages), "G38 age aggregates survive state JSON");
            Counts(0, 2);
            pop.Restore(new ColonyPopulation.State { agingDue = 2, old = 3 });
            Check(pop.S.old == 5 && pop.Total == 5, "G38 legacy pending elders added to elder count once");
            saved = pop.Capture(); pop.Restore(saved);
            Check(pop.S.old == 5 && pop.Total == 5, "G38 migrated state restore is idempotent");

            foreach (FoodPreservation kind in Enum.GetValues(typeof(FoodPreservation)))
            {
                var days = kind == FoodPreservation.RawMeat || kind == FoodPreservation.Fish || kind == FoodPreservation.Cooked ? 3
                    : kind == FoodPreservation.Berry || kind == FoodPreservation.Mushroom ? 6 : 24;
                var lots = new FoodLots(); lots.Add(2, FoodLots.ShelfLife(kind));
                Check(lots.Age(days * 150 - 1) == 0 && lots.Age(1) == 2, "G30 ambient expiry " + kind);
            }
            Check(FoodLots.ShelfLife(AntColony.Data.ResourceType.SimpleMeal) == 450 && FoodLots.ShelfLife(AntColony.Data.ResourceType.FineMeal) == 450,
                "G30 both cooked grades last three days");
            var source = new FoodLots(); source.Add(4, FoodLots.ShelfLife(FoodPreservation.Grain)); source.Age(123);
            var taken = source.Take(2); var moved = new FoodLots(); moved.Add(taken);
            Check(moved.Stalest == 3600 - 123, "G30 moving food preserves remaining shelf life");
            Check(new StoreBin().DecayRate == 1, "G30 ordinary storage has no preservation bonus");
            return "PASS " + checks + " ImplB2 checks (source-specific production wiring pending)";
        }
        finally { UnityEngine.Random.state = random; Object.Destroy(hut); }
    }
}
