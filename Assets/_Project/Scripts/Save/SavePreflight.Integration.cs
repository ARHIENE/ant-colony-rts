using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Units;
using UnityEngine;

namespace AntColony.Save
{
    public static partial class SavePreflight
    {
        private static bool ValidNaturalWork(ResourceNodeDto n) => n.workRequestOrder >= 0 && n.workRequestOrder < long.MaxValue / 2 && (!n.workPaused || n.workRequested);
        private static void ValidateStaged(List<ProductionInputs.Stock> staged)
        {
            Check(staged == null || staged.Count <= Enum.GetValues(typeof(AntColony.Data.ResourceType)).Length
                  && staged.All(s => s != null && Enum.IsDefined(typeof(AntColony.Data.ResourceType), s.type) && s.amount >= 0 && s.amount <= 100000000
                      && (s.lots == null || s.lots.Valid && s.lots.Total <= s.amount))
                && staged.Select(s => s.type).Distinct().Count() == staged.Count, "staged production materials");
        }
        private static void ValidateIntegration(SaveFileV1 f)
        {
            void Filter(StoreBin.Dto bin) => Check(bin?.extraForbidden == null || bin.extraForbidden.Count <= StoreBin.TypeCount
                && bin.extraForbidden.Distinct().Count() == bin.extraForbidden.Count
                && bin.extraForbidden.All(i => i >= 64 && Enum.IsDefined(typeof(AntColony.Data.ResourceType), i)), "extended storage filter");
            Filter(f.colony.baseBin); foreach (var building in f.buildings) Filter(building.storeBin);
            SaveTransportValidation.Validate(f);
            Check(FarmSeeds.Valid(f.farmSeeds), "farm seeds");
            Check(f.nodes.All(n => n.seedCrop == -1 || Enum.IsDefined(typeof(FarmCrop), n.seedCrop)), "seed source");
            Check(f.buildings.All(b => b.objectName == null || b.objectName.Length <= 128), "building name");
            Check(f.defeatReason == null || f.defeatReason.Length <= 200, "defeat reason");
            // G42 직접 지시 대기: 장수 ID 존재, 수송 간 중복 없음, 종류·상태·인덱스 범위.
            var ids = new HashSet<string>(f.commanders.Select(c => c.personalState.id)); var queued = new HashSet<string>();
            foreach (var t in f.world.transports)
                foreach (var o in t.crewOrders ?? new List<AntColony.World.CrewOrderState>())
                    Check(o != null && o.commanderId != null && ids.Contains(o.commanderId) && queued.Add(o.commanderId)
                        && Enum.IsDefined(typeof(AntColony.World.CrewOrderKind), o.kind) && Enum.IsDefined(typeof(AntColony.World.ExpeditionState), o.state)
                        && o.siteIndex >= -1 && o.siteIndex < f.world.sites.Count && o.sourceTransportIndex >= -1 && o.sourceTransportIndex < f.world.transports.Count
                        && o.sourceSettlementIndex >= -1 && o.sourceSettlementIndex < f.world.sites.Count, "crew order");
            foreach (var b in f.buildings)
            {
                if (b.kind == "Farm" && b.farm != null)
                {
                    var s = b.farm;
                    Check(s.harvests >= 0 && s.harvests <= 2 && s.seedVersion >= 0 && s.seedVersion <= 1
                        && (s.nextCrop == -1 || Enum.IsDefined(typeof(FarmCrop), s.nextCrop))
                        && N(s.uprootWork) && s.uprootWork <= FarmPlot.UprootSeconds
                        && N(s.fertilizeWork) && s.fertilizeWork <= FarmPlot.FertilizeSeconds
                        && N(s.pestTimer) && s.pestTimer < FarmPlot.PestCheckSeconds, "farm state");
                }
                if (b.kind == "Workshop")
                {
                    var s = b.workshop;
                    Check(s != null && s.materialVersion >= 0 && s.materialVersion <= 1, "craft material version");
                    ValidateStaged(s.staged);
                    Check(!s.ruined || b.health == 0 && s.crafter == -1, "workshop ruin");
                    foreach (var j in s.jobs)
                    {
                        Check(j != null && (j.makerId == null || j.makerId.Length <= 200), "craft maker");
                        // A maker may have died or left the roster; their unfinished work remains cancellable.
                        Check(s.materialVersion == 0 || (j.work == 0 || j.materialsPaid && !string.IsNullOrEmpty(j.makerId))
                            && (string.IsNullOrEmpty(j.makerId) || j.materialsPaid), "craft payment");
                    }
                    if (s.crafter >= 0 && s.jobs.Count > 0 && !string.IsNullOrEmpty(s.jobs[0].makerId))
                        Check(s.crafter < f.commanders.Count && s.jobs[0].makerId == f.commanders[s.crafter].personalState.id, "active craft maker");
                }
                if (Enum.TryParse(b.kind, out AntColony.Data.BuildingKind kind) && Processor.IsKind(kind) && b.processor != null)
                {
                    var s = b.processor; var recipes = Processor.RecipesFor(kind);
                    Check(kind is AntColony.Data.BuildingKind.GasSmelter or AntColony.Data.BuildingKind.ChemistryBench ? s.gasPaid == (s.current >= 0) : !s.gasPaid, "industrial paid fuel");
                    Check(kind == AntColony.Data.BuildingKind.ChemistryBench ? s.bufferedOil >= 0 && s.bufferedOil <= IndustrialRecipes.PlasticOil
                        && (s.current < 0 || s.current < recipes.Length && (recipes[s.current].output != AntColony.Data.ResourceType.Plastic || s.bufferedOil == IndustrialRecipes.PlasticOil)) : s.bufferedOil == 0, "industrial oil buffer");
                    Check(N(s.fermentation) && s.fermentation >= 0 && s.fermentation <= FoodProcessingRecipes.FermentSeconds
                        && (!(s.fermenting || s.fermented) || kind == AntColony.Data.BuildingKind.BrewingBarrel && s.current >= 0 && s.progress == 0)
                        && !(s.fermenting && s.fermented) && (!s.fermented || s.fermentation == FoodProcessingRecipes.FermentSeconds)
                        && (s.fermenting || s.fermented || s.fermentation == 0)
                        && (!s.fermenting || s.fermentation < FoodProcessingRecipes.FermentSeconds)
                        && (kind == AntColony.Data.BuildingKind.BrewingBarrel || s.fermentation == 0), "fermentation state");
                    ValidateStaged(s.staged);
                    Check(s.pending >= -1 && s.pending < recipes.Length, "processor pending recipe");
                    Check(s.orders != null && s.orders.Count <= Workshop.MaxOrders && s.orders.All(o => o != null
                        && o.recipe >= 0 && o.recipe < recipes.Length && Enum.IsDefined(typeof(Workshop.OrderMode), o.mode)
                        && o.amount >= 1 && o.amount <= Workshop.MaxOrderAmount), "processor orders");
                    Check(s.current >= -1 && s.current < recipes.Length && N(s.progress)
                        && (s.current < 0 ? s.progress == 0 && string.IsNullOrEmpty(s.makerId) : s.progress < recipes[s.current].work)
                        && (s.makerId == null || s.makerId.Length <= 200) && (!s.ruined || b.health == 0), "processor state");
                    if (b.serviceCommander >= 0 && !string.IsNullOrEmpty(s.makerId))
                        Check(b.serviceCommander < f.commanders.Count && !s.ruined
                            && f.commanders[b.serviceCommander].personalState.id == s.makerId, "active processor maker");
                }
                if (b.kind == "AirshipYard" && b.airship != null)
                {
                    Check(b.airship.phase != RocketPhase.Destroyed || b.health == 0, "rocket ruin");
                    Check(!b.airship.launchRequested || b.airship.passengers.Count > 0, "rocket launch passengers");
                }
            }
            if (f.rocket != null)
                Check(N(f.rocket.crisisResumeAt) && f.rocket.crisisResumeAt <= f.gameSeconds + 3 * GameCalendar.SecondsPerDay,
                    "rocket crisis grace");
            var carriers = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < f.world.transports.Count; i++)
            {
                var s = f.world.transports[i];
                var id = string.IsNullOrWhiteSpace(s.bossCarrierId) ? "transport:" + i : s.bossCarrierId;
                Check(id.Length <= 200 && carriers.Add(id), "boss carrier identity");
                var parcels = f.campaign?.bosses?.cargo;
                long load = (s.cargoAmounts != null && s.cargoAmounts.Length > 0 ? s.cargoAmounts.Sum(n => (long)n) : (long)s.cargoFood + s.cargoSoil + s.cargoSpecial)
                    + s.equipmentCargo.Count + (s.bodyCargo?.Sum(b => (long)AntColony.World.ExpeditionTransport.BodyWeight(b)) ?? 0)
                    + (s.withdrawal?.loads?.Sum(l => (long)l.amount) ?? 0)
                    + (s.withdrawal?.bodies?.Sum(j => { var b = f.corpses.FirstOrDefault(c => c.commanderId == j.bodyId); return b == null ? 0L : AntColony.World.ExpeditionTransport.BodyWeight(b); }) ?? 0)
                    + (parcels == null ? 0 : parcels.Where(p => p.carrier == id).Sum(p => (long)p.count));
                var crew = f.commanders.Where(c => c.location == 1 && c.transportIndex == i && !c.personalState.dead);
                // Same weight model as CommanderStartingSupplies / ExpeditionSupplies, evaluated on saved data.
                int capacity = s.walking ? Mathf.FloorToInt(crew.Sum(WalkingSpace) / .05f)
                    : Mathf.FloorToInt((s.aircraft ? GameBalance.AircraftCargo : GameBalance.VehicleCargo)
                        * (f.campaign.completed.Contains((int)ScienceTechnology.EfficientTransport) ? 1.5f : 1f));
                Check(load <= capacity, "boss inclusive cargo capacity");
            }
            foreach (var parcel in f.campaign?.bosses?.cargo ?? new List<AntColony.Boss.BossProgress.Parcel>())
                Check(carriers.Contains(parcel.carrier) || parcel.carrier.StartsWith("commander:", StringComparison.Ordinal)
                    && f.commanders.Any(c => "commander:" + c.personalState.id == parcel.carrier && !c.personalState.dead), "boss cargo carrier");
        }

        private static float WalkingSpace(CommanderDto c)
        {
            var p = c.personalState;
            float weight = (p.carriedRations?.Total ?? 0) * .05f + (p.equipment.Any(e => e.slot == EquipmentSlot.Weapon) ? 1 : 0)
                + (p.toolTier > 0 ? 1.5f : 0) + (string.IsNullOrEmpty(p.ordinaryClothes) ? 0 : .5f);
            return Mathf.Max(0, (GameBalance.CarryBase + c.talents.Level(CommanderActivity.Strength) * GameBalance.CarryPerStrength)
                * SaveCatalog.Traits(c.traits).CarryMultiplier - weight);
        }
    }
}
