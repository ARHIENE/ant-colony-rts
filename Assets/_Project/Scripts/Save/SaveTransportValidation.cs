using System;
using System.Collections.Generic;
using System.Linq;
using AntColony.Core;
using AntColony.World;
using UnityEngine;

namespace AntColony.Save
{
    internal static class SaveTransportValidation
    {
        static void Check(bool ok, string name) { if (!ok) throw new FormatException("Invalid transport " + name); }
        static bool N(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0 && n <= 1e8f;
        static bool N(double n) => !double.IsNaN(n) && !double.IsInfinity(n) && n >= 0 && n <= 1e8;
        internal static void Validate(SaveFileV1 f)
        {
            var workers = new HashSet<string>();
            var bodies = new HashSet<string>(f.corpses.Where(c => !string.IsNullOrEmpty(c.commanderId)).Select(c => c.commanderId));
            for (int i = 0; i < f.world.transports.Count; i++)
            {
                var t = f.world.transports[i]; Check(t != null, "entry");
                var crew = f.commanders.Where(c => c.location == 1 && c.transportIndex == i && !c.personalState.dead).ToArray();
                var ids = new HashSet<string>(crew.Select(c => c.personalState.id));
                var d = t.damage;
                if (d != null && d.present)
                {
                    Check(N(d.health) && d.health <= (t.walking ? 100000 : t.aircraft ? TransportDamage.AircraftHealth : TransportDamage.VehicleHealth)
                        && N(d.repairedFraction) && d.repairedFraction <= 1000000 && N(d.salvageProgress) && d.salvageProgress <= TransportDamage.SalvageSeconds
                        && (!d.salvaged || d.health == 0 && d.salvageProgress == TransportDamage.SalvageSeconds), "damage");
                    Check(d.health > 0 || !t.route.running && t.remaining == 0 && t.state != (int)ExpeditionState.Outbound && t.state != (int)ExpeditionState.Returning, "wreck route");
                    foreach (var id in new[] { d.repairWorker, d.salvageWorker }.Where(id => !string.IsNullOrEmpty(id)))
                        Check(workers.Add(id) && f.commanders.Any(c => c.personalState.id == id && !c.personalState.dead), "field worker");
                    Check(string.IsNullOrEmpty(d.repairWorker) || d.health > 0, "wreck repair");
                    Check(string.IsNullOrEmpty(d.salvageWorker) || d.health == 0 && !d.salvaged, "salvage worker");
                }
                var w = t.withdrawal;
                if (w != null && (w.active || (w.loads?.Count ?? 0) > 0 || (w.jobs?.Count ?? 0) > 0 || (w.bodies?.Count ?? 0) > 0 || w.troops != 0))
                {
                    Check(w.troops >= 0 && w.troops <= 100000000, "troop passengers");
                    Check(w.loads != null && w.loads.Count <= 10000 && w.loads.All(l => l != null && Enum.IsDefined(typeof(AntColony.Data.ResourceType), l.type)
                        && l.amount > 0 && N(l.progress) && l.progress <= ExpeditionWithdraw.LoadSeconds && N(l.rate) && l.rate > 0), "pending load");
                    foreach (var l in w.loads.Where(l => !string.IsNullOrEmpty(l.workerId)))
                        Check(workers.Add(l.workerId) && f.commanders.Any(c => c.personalState.id == l.workerId && !c.personalState.dead
                            && c.carriedType == (int)l.type && c.carriedAmount + .001f >= l.amount), "load reservation owner");
                    Check(w.loads.Count == 0 || t.state == (int)ExpeditionState.Deployed && (d == null || !d.present || d.health > 0), "load location");
                    if (w.active)
                    {
                        Check(t.state == (int)ExpeditionState.Deployed && (d == null || !d.present || d.health > 0)
                            && w.nextSite >= -1 && w.nextSite < f.world.sites.Count && (w.nextSite < 0 || w.nextSite != t.siteIndex)
                            && w.jobs != null && w.jobs.Count == crew.Length && w.jobs.Select(j => j?.commanderId).Distinct().Count() == crew.Length, "withdrawal ownership");
                        Check(w.jobs.Sum(j => (long)j.boardedTroops) <= w.troops, "boarding troop ledger");
                        var helpers = new HashSet<string>();
                        foreach (var j in w.jobs)
                        {
                            Check(j != null && ids.Contains(j.commanderId) && N(j.progress) && j.progress < ExpeditionWithdraw.TroopGroupSize * ExpeditionWithdraw.BoardSeconds + ExpeditionWithdraw.AssistedBoardSeconds
                                && j.boardedTroops >= 0 && j.boardedTroops <= w.troops, "boarding progress");
                            Check(string.IsNullOrEmpty(j.helperId) || ids.Contains(j.helperId) && j.helperId != j.commanderId && (j.boarded || helpers.Add(j.helperId)), "helper");
                            Check(!j.lifted || !string.IsNullOrEmpty(j.helperId), "lifted passenger");
                        }
                    }
                    else Check(w.jobs == null || w.jobs.Count == 0, "inactive withdrawal");
                    Check(w.bodies != null && w.bodies.Count <= crew.Length && w.bodies.Select(b => b?.bodyId).Distinct().Count() == w.bodies.Count, "body jobs");
                    foreach (var j in w.bodies)
                        Check(j != null && ids.Contains(j.carrierId) && workers.Add(j.carrierId)
                            && f.corpses.Any(b => b.commanderId == j.bodyId && b.count == 1
                                && (b.kind == CorpseKind.ColonyCommander || b.kind == CorpseKind.EnemyCommander))
                            && N(j.progress) && j.progress < ExpeditionWithdraw.LoadSeconds, "body recovery");
                }
                if (t.bodyCargo != null)
                {
                    Check(t.bodyCargo.Count <= 10000, "body cargo count");
                    foreach (var b in t.bodyCargo)
                        Check(b != null && !string.IsNullOrEmpty(b.commanderId) && bodies.Add(b.commanderId) && b.count == 1 && N(b.remaining)
                            && b.remaining <= Corpse.Lifetime && b.food == 0 && !string.IsNullOrEmpty(b.name)
                            && (b.kind == CorpseKind.ColonyCommander || b.kind == CorpseKind.EnemyCommander)
                            && string.IsNullOrEmpty(b.workerId), "body cargo");
                }
                var route = t.route;
                if (route.stops != null && route.stops.Count > 0)
                    Check(route.stops.Count <= TransportRoute.MaxStops && route.stops.Distinct().Count() == route.stops.Count
                        && route.stops.All(s => s >= 0 && s < f.world.sites.Count) && route.leg >= 0 && route.leg < route.stops.Count
                        && route.destinationIndex == route.stops[route.leg], "route stops/leg");
                else Check(route.leg == 0, "legacy route leg");
                long load = t.cargoAmounts != null && t.cargoAmounts.Length > 0 ? t.cargoAmounts.Sum(n => (long)n) : (long)t.cargoFood + t.cargoSoil + t.cargoSpecial;
                if (t.cargoAmounts != null && t.cargoAmounts.Length > 0)
                    Check(t.cargoAmounts.Length == Enum.GetValues(typeof(AntColony.Data.ResourceType)).Length && t.cargoAmounts.All(n => n >= 0), "cargo amounts");
                load += w?.loads?.Sum(l => (long)l.amount) ?? 0;
                load += t.equipmentCargo.Count;
                if (!t.walking)
                    Check(load <= Mathf.FloorToInt((t.aircraft ? GameBalance.AircraftCargo : GameBalance.VehicleCargo)
                        * (f.campaign.completed.Contains((int)ScienceTechnology.EfficientTransport) ? 1.5f : 1)), "reserved capacity");
            }
        }
    }
}
