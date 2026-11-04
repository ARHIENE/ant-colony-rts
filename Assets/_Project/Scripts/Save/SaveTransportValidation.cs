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
        static void Check(bool ok, string name) { if (!ok) throw new FormatException("수송 저장 정보 오류: " + name); }
        static bool N(float n) => !float.IsNaN(n) && !float.IsInfinity(n) && n >= 0 && n <= 1e8f;
        static bool N(double n) => !double.IsNaN(n) && !double.IsInfinity(n) && n >= 0 && n <= 1e8;
        internal static void Validate(SaveFileV1 f)
        {
            var workers = new HashSet<string>();
            var bodies = new HashSet<string>(f.corpses.Where(c => !string.IsNullOrEmpty(c.commanderId)).Select(c => c.commanderId));
            for (int i = 0; i < f.world.transports.Count; i++)
            {
                var t = f.world.transports[i]; Check(t != null, "수송 항목");
                var crew = f.commanders.Where(c => c.location == 1 && c.transportIndex == i && !c.personalState.dead).ToArray();
                var ids = new HashSet<string>(crew.Select(c => c.personalState.id));
                var d = t.damage;
                if (d != null && d.present)
                {
                    Check(N(d.health) && d.health <= (t.walking ? 100000 : t.aircraft ? TransportDamage.AircraftHealth : TransportDamage.VehicleHealth)
                        && N(d.repairedFraction) && d.repairedFraction <= 1000000 && N(d.salvageProgress) && d.salvageProgress <= TransportDamage.SalvageSeconds
                        && (!d.salvaged || d.health == 0 && d.salvageProgress == TransportDamage.SalvageSeconds), "손상");
                    Check(d.health > 0 || !t.route.running && t.remaining == 0 && t.state != (int)ExpeditionState.Outbound && t.state != (int)ExpeditionState.Returning, "잔해 수송 경로");
                    foreach (var id in new[] { d.repairWorker, d.salvageWorker }.Where(id => !string.IsNullOrEmpty(id)))
                        Check(workers.Add(id) && f.commanders.Any(c => c.personalState.id == id && !c.personalState.dead), "현지 작업자");
                    Check(string.IsNullOrEmpty(d.repairWorker) || d.health > 0, "잔해 수리");
                    Check(string.IsNullOrEmpty(d.salvageWorker) || d.health == 0 && !d.salvaged, "해체 담당자");
                }
                var w = t.withdrawal;
                if (w != null && (w.active || (w.loads?.Count ?? 0) > 0 || (w.jobs?.Count ?? 0) > 0 || (w.bodies?.Count ?? 0) > 0 || w.troops != 0))
                {
                    Check(w.troops >= 0 && w.troops <= 100000000, "탑승 병력");
                    Check(w.loads != null && w.loads.Count <= 10000 && w.loads.All(l => l != null && Enum.IsDefined(typeof(AntColony.Data.ResourceType), l.type)
                        && l.amount > 0 && N(l.progress) && l.progress <= ExpeditionWithdraw.LoadSeconds && N(l.rate) && l.rate > 0), "적재 대기");
                    foreach (var l in w.loads.Where(l => !string.IsNullOrEmpty(l.workerId)))
                        Check(workers.Add(l.workerId) && f.commanders.Any(c => c.personalState.id == l.workerId && !c.personalState.dead
                            && c.carriedType == (int)l.type && c.carriedAmount + .001f >= l.amount), "적재 예약자");
                    Check(w.loads.Count == 0 || t.state == (int)ExpeditionState.Deployed && (d == null || !d.present || d.health > 0), "적재 위치");
                    if (w.active)
                    {
                        Check(t.state == (int)ExpeditionState.Deployed && (d == null || !d.present || d.health > 0)
                            && w.nextSite >= -1 && w.nextSite < f.world.sites.Count && (w.nextSite < 0 || w.nextSite != t.siteIndex)
                            && w.jobs != null && w.jobs.Count == crew.Length && w.jobs.Select(j => j?.commanderId).Distinct().Count() == crew.Length, "철수 담당");
                        Check(w.jobs.Sum(j => (long)j.boardedTroops) <= w.troops, "탑승 병력 집계");
                        var helpers = new HashSet<string>();
                        foreach (var j in w.jobs)
                        {
                            Check(j != null && ids.Contains(j.commanderId) && N(j.progress) && j.progress < ExpeditionWithdraw.TroopGroupSize * ExpeditionWithdraw.BoardSeconds + ExpeditionWithdraw.AssistedBoardSeconds
                                && j.boardedTroops >= 0 && j.boardedTroops <= w.troops, "탑승 진행");
                            Check(string.IsNullOrEmpty(j.helperId) || ids.Contains(j.helperId) && j.helperId != j.commanderId && (j.boarded || helpers.Add(j.helperId)), "구조 보조자");
                            Check(!j.lifted || !string.IsNullOrEmpty(j.helperId), "운반 중 승객");
                        }
                    }
                    else Check(w.jobs == null || w.jobs.Count == 0, "비활성 철수");
                    Check(w.bodies != null && w.bodies.Count <= crew.Length && w.bodies.Select(b => b?.bodyId).Distinct().Count() == w.bodies.Count, "시신 운반 작업");
                    foreach (var j in w.bodies)
                        Check(j != null && ids.Contains(j.carrierId) && workers.Add(j.carrierId)
                            && f.corpses.Any(b => b.commanderId == j.bodyId && b.count == 1
                                && (b.kind == CorpseKind.ColonyCommander || b.kind == CorpseKind.EnemyCommander))
                            && N(j.progress) && j.progress < ExpeditionWithdraw.LoadSeconds, "시신 회수");
                }
                if (t.bodyCargo != null)
                {
                    Check(t.bodyCargo.Count <= 10000, "시신 화물 수");
                    foreach (var b in t.bodyCargo)
                        Check(b != null && !string.IsNullOrEmpty(b.commanderId) && bodies.Add(b.commanderId) && b.count == 1 && N(b.remaining)
                            && b.remaining <= Corpse.Lifetime && b.food == 0 && !string.IsNullOrEmpty(b.name)
                            && (b.kind == CorpseKind.ColonyCommander || b.kind == CorpseKind.EnemyCommander)
                            && string.IsNullOrEmpty(b.workerId), "시신 화물");
                }
                var route = t.route;
                if (route.stops != null && route.stops.Count > 0)
                    Check(route.stops.Count <= TransportRoute.MaxStops && route.stops.Distinct().Count() == route.stops.Count
                        && route.stops.All(s => s >= 0 && s < f.world.sites.Count) && route.leg >= 0 && route.leg < route.stops.Count
                        && route.destinationIndex == route.stops[route.leg], "수송 경유지·구간");
                else Check(route.leg == 0, "구저장 수송 구간");
                long load = t.cargoAmounts != null && t.cargoAmounts.Length > 0 ? t.cargoAmounts.Sum(n => (long)n) : (long)t.cargoFood + t.cargoSoil + t.cargoSpecial;
                if (t.cargoAmounts != null && t.cargoAmounts.Length > 0)
                    Check(t.cargoAmounts.Length == Enum.GetValues(typeof(AntColony.Data.ResourceType)).Length && t.cargoAmounts.All(n => n >= 0), "화물 수량");
                load += w?.loads?.Sum(l => (long)l.amount) ?? 0;
                load += t.equipmentCargo.Count;
                if (!t.walking)
                    Check(load <= Mathf.FloorToInt((t.aircraft ? GameBalance.AircraftCargo : GameBalance.VehicleCargo)
                        * (f.campaign.completed.Contains((int)ScienceTechnology.EfficientTransport) ? 1.5f : 1)), "예약 적재 한도");
            }
        }
    }
}
