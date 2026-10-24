using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Units;
using AntColony.World;
using UnityEngine;

namespace AntColony.Core
{
    [Serializable] public sealed class RocketSnapshot
    {
        public float at, currentWealth, currentPopulation, averageWealth, averagePopulation;
        public bool taken; // JsonUtility는 null 대신 빈 객체로 복원하므로 실제로 고정된 스냅샷인지 따로 저장한다.
        public static bool Present(RocketSnapshot s) => s != null && s.taken;
        public float Wealth => Mathf.Max(currentWealth, averageWealth);
        public float Population => Mathf.Max(currentPopulation, averagePopulation);
        public static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0;
        public bool Valid => Finite(at) && Finite(currentWealth) && Finite(currentPopulation)
            && Finite(averageWealth) && Finite(averagePopulation);
    }

    // 소유권 집계자는 예약을 더하지 않은 전 지역 실물 평가를 전달한다. 공세는 가동 시 복사본만 사용한다.
    [Serializable] public sealed class RocketEconomy
    {
        [Serializable] public sealed class Sample { public float at, wealth, population; }
        public List<Sample> samples = new List<Sample>();
        // 외교 기존 평가 단위(일반 자원 1, 특수 10, 장비 품질별 가격)를 재사용한다.
        public static float ResourceValue(AntColony.Data.ResourceType type, float amount) => amount * (type == AntColony.Data.ResourceType.Special ? 10 : 1);
        public static Sample CaptureOwned()
        {
            float wealth = 0;
            var types = (AntColony.Data.ResourceType[])Enum.GetValues(typeof(AntColony.Data.ResourceType));
            var gearIds = new HashSet<string>();
            void Gear(IEnumerable<EquipmentItem> items)
            {
                if (items == null) return;
                foreach (var e in items)
                    if (e != null && gearIds.Add(e.id)) wealth += DiplomacyRules.EquipmentValues[Mathf.Clamp(e.quality, 0, 3)];
            }
            if (ResourceManager.Instance != null)
                foreach (var bin in ResourceManager.Instance.Bins.Where(b => b.Usable))
                    foreach (var t in bin.Stocked) wealth += ResourceValue(t, bin.Amount(t));
            foreach (var node in UnityEngine.Object.FindObjectsByType<ResourceNode>(FindObjectsSortMode.None))
            {
                var site = node.GetComponentInParent<ExpeditionSite>();
                // 자연 매장량은 소유 재산이 아니다. 편입 거점 재고와 바닥 실물만 센다.
                if (!node.IsRaidLocked && (node.IsLooseCargo || node.IsRaidLoot && site != null && site.Disposition == ConquestDisposition.Annexed))
                    wealth += ResourceValue(node.ResourceType, node.AmountRemaining);
            }
            foreach (var worker in UnityEngine.Object.FindObjectsByType<WorkerAnt>(FindObjectsSortMode.None))
                if (!worker.IsDead && (!(worker is CommanderAnt c) || c.IsColonyMember && !c.IsCaptive && !c.IsDeparting && !c.IsHostile))
                    wealth += ResourceValue(worker.CarriedType, worker.CarriedAmount);
            var commanders = CommanderRoster.Instance?.Commanders;
            if (commanders != null) foreach (var c in commanders.Where(CampaignOutcome.Survivor))
            {
                Gear(c.PersonalState.equipment);
                wealth += c.PersonalState.carriedRations?.Total ?? 0;
            }
            if (DiplomacyManager.Instance != null)
                foreach (var h in DiplomacyManager.Instance.Data.homecomings.Where(CampaignOutcome.SafeHomecoming))
                {
                    Gear(h.gear); Gear(h.commander.PersonalState.equipment);
                    wealth += h.commander.PersonalState.carriedRations?.Total ?? 0;
                }
            Gear(EquipmentInventory.Instance?.Items);
            foreach (var loot in UnityEngine.Object.FindObjectsByType<EquipmentLoot>(FindObjectsSortMode.None)) Gear(loot.Items);
            foreach (var ship in UnityEngine.Object.FindObjectsByType<ExpeditionTransport>(FindObjectsSortMode.None).Where(s => !s.IsDead))
            {
                foreach (var t in types) wealth += ResourceValue(t, ship.GetCargo(t));
                Gear(ship.EquipmentCargo);
                if (ship.BlueprintCargo) wealth += DiplomacyRules.BlueprintPrice * 10;
            }
            if (CampaignResearch.Instance?.HasBlueprint == true) wealth += DiplomacyRules.BlueprintPrice * 10;
            foreach (var b in UnityEngine.Object.FindObjectsByType<BuildingBase>(FindObjectsSortMode.None))
            {
                var site = b.GetComponentInParent<ExpeditionSite>();
                if (b.IsDead || b.Data == null || !(site != null ? site.Disposition == ConquestDisposition.Annexed : b.CountsTowardPlayerDefeat)) continue;
                if (b is SupplyFacility natural && natural.IsNaturalSource) continue;
                if (EarlyConstruction.HasFixedCost(b.Data.kind))
                    foreach (var cost in EarlyConstruction.Cost(b.Data.kind)) wealth += ResourceValue(cost.type, cost.amount);
                else wealth += b.Data.foodCost + ResourceValue(b.MainMaterial, b.Data.soilCost) + ResourceValue(AntColony.Data.ResourceType.Special, b.Data.specialCost);
                if (b is SupplyFacility supply) wealth += supply.Supply.amount;
                if (b is Processor processor && processor.Current != null)
                    foreach (var input in processor.Current.inputs) wealth += ResourceValue(input.type, input.amount);
                if (b is Kitchen kitchen)
                    wealth += kitchen.Meals.staged + (kitchen.Current?.food ?? 0) + kitchen.Meals.meals.Count * GameBalance.MealFood;
                if (b is Workshop workshop)
                    foreach (var job in workshop.Jobs.Where(j => j.materialsPaid))
                    {
                        var cost = EquipmentRecipes.Cost(job.recipe);
                        wealth += cost.x + cost.y + cost.z * 10;
                    }
                if (b is AirshipYard yard)
                {
                    var s = yard.CaptureState(System.Array.Empty<CommanderAnt>());
                    wealth += ResourceValue((AntColony.Data.ResourceType)s.fuelType, s.fuelUnits);
                    float coreCost = 100 + 150 + ResourceValue(AntColony.Data.ResourceType.Special, 150);
                    wealth += (s.hull ? coreCost : 0) + (s.engine ? coreCost : 0)
                        + s.cocoons * (GameBalance.CocoonFood + GameBalance.CocoonSoil + GameBalance.CocoonSpecial * 10);
                    if (s.building >= 0) wealth += s.building == (int)AirshipPart.Cocoon
                        ? GameBalance.CocoonFood + GameBalance.CocoonSoil + GameBalance.CocoonSpecial * 10 : coreCost;
                }
            }
            foreach (var site in UnityEngine.Object.FindObjectsByType<BuildingConstructionSite>(FindObjectsSortMode.None))
                foreach (var need in site.Needs) wealth += ResourceValue(need.type, need.delivered);
            // 수송/배속/건설 예약 병력은 이미 전역 풀에 있다. 일반 시민 로켓 승객만 풀 밖이다.
            int population = ColonyPopulation.Instance?.Total ?? AntPool.Instance?.Total ?? 0;
            population += UnityEngine.Object.FindObjectsByType<AirshipYard>(FindObjectsSortMode.None)
                .Where(y => !y.IsDead && y.Phase != RocketPhase.Launched).Sum(y => y.CivilianPassengers);
            population += commanders?.Count(CampaignOutcome.Survivor) ?? 0;
            if (DiplomacyManager.Instance != null)
                population += DiplomacyManager.Instance.Data.homecomings.Where(CampaignOutcome.SafeHomecoming)
                    .Select(h => h.commander.PersonalState.id).Distinct()
                    .Count(id => commanders == null || !commanders.Any(c => c.PersonalState.id == id));
            return new Sample { at = GameCalendar.GameSeconds, wealth = wealth, population = population };
        }
        public void Observe(float at, float wealth, float population)
        {
            if (!RocketSnapshot.Finite(at) || !RocketSnapshot.Finite(wealth) || !RocketSnapshot.Finite(population)
                || samples.Count > 0 && at < samples[samples.Count - 1].at) throw new ArgumentOutOfRangeException();
            if (samples.Count > 0 && samples[samples.Count - 1].at == at) samples.RemoveAt(samples.Count - 1);
            if (samples.Count == 0 || samples[samples.Count - 1].wealth != wealth || samples[samples.Count - 1].population != population)
                samples.Add(new Sample { at = at, wealth = wealth, population = population });
            while (samples.Count > 1 && samples[1].at <= at - GameCalendar.SecondsPerSeason) samples.RemoveAt(0);
        }
        public RocketSnapshot Preview(float at, float currentWealth, float currentPopulation)
        {
            if (!RocketSnapshot.Finite(at) || !RocketSnapshot.Finite(currentWealth) || !RocketSnapshot.Finite(currentPopulation)
                || samples.Count == 0 || at < samples[samples.Count - 1].at) return null;
            float start = Mathf.Max(samples[0].at, at - GameCalendar.SecondsPerSeason);
            double wealth = 0, population = 0;
            for (int i = 0; i < samples.Count; i++)
            {
                float end = i + 1 < samples.Count ? samples[i + 1].at : at;
                float duration = Mathf.Max(0, end - Mathf.Max(start, samples[i].at));
                wealth += samples[i].wealth * (double)duration;
                population += samples[i].population * (double)duration;
            }
            return new RocketSnapshot { taken = true, at = at, currentWealth = currentWealth, currentPopulation = currentPopulation,
                averageWealth = at > start ? (float)(wealth / (at - start)) : currentWealth,
                averagePopulation = at > start ? (float)(population / (at - start)) : currentPopulation };
        }
        public bool Valid()
        {
            if (samples == null) return false;
            float previous = -1;
            foreach (var s in samples)
            {
                if (s == null || !RocketSnapshot.Finite(s.at) || !RocketSnapshot.Finite(s.wealth)
                    || !RocketSnapshot.Finite(s.population) || s.at <= previous) return false;
                previous = s.at;
            }
            return true;
        }
    }
}
