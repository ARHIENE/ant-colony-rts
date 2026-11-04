using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ImplB5Checks
{
    static int count;
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool value, string label) { if (!value) throw new Exception("B5 FAIL " + count + ": " + label); count++; }
    static bool Near(float a, float b) => Mathf.Abs(a - b) < .02f;
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Hidden).Invoke(target, args);
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "load finished"); Time.timeScale = 0;
    }

    public static async Task<string> Main()
    {
        count = 0; Check(Application.isPlaying, "Play mode required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var random = UnityEngine.Random.state; var scale = Time.timeScale;
        var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ImplB5-" + Guid.NewGuid().ToString("N"));
        GameObject pileObject = null; BuildingData pileData = null;
        try
        {
            await Ready();
            var options = new NewGameOptions { startingCount = 2, seed = 261024, mapSize = MapSize.Small,
                biome = MapBiome.Forest, startingTeam = new[] { StartingCandidate.Roll(0), StartingCandidate.Roll(1) } };
            foreach (var candidate in options.startingTeam) candidate.traits.values.Clear();
            CrashLandingStart.Begin(options); await Ready();
            var world = WorldMapManager.Instance; var diplomacy = DiplomacyManager.Instance;
            var commanders = CommanderRoster.Instance.Commanders.ToArray();
            Check(commanders.Length == 2 && commanders.All(c => c.TroopCount == 0), "crash start two commanders, no troops");
            foreach (var c in commanders) { c.CommandStop(); c.SetJobEnabled(CommanderJobs.All, false); }
            // 새 시작에 없는 비축더미를 검사에서 직접 만든다.
            pileObject = GameObject.CreatePrimitive(PrimitiveType.Cube); pileObject.SetActive(false);
            pileObject.name = "B5 check stockpile"; pileObject.transform.position = world.HomePosition;
            var pile = pileObject.AddComponent<Stockpile>(); pileData = ScriptableObject.CreateInstance<BuildingData>();
            pileData.kind = BuildingKind.QueenChamber;
            typeof(BuildingBase).GetMethod("ConfigureRuntime", Hidden).Invoke(pile, new object[] { pileData });
            pileObject.SetActive(true);
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Food, 200);

            Check(GameCalendar.SecondsPerDay == 150 && DiplomacyRules.OfferSeconds == 900, "G08 calendar reused");
            var firstBoss = world.Sites.First(s => s.Kind == ExpeditionSiteKind.BossNest);
            Check(ExpeditionRules.WalkSeconds(firstBoss) <= GameCalendar.SecondsPerDay, "G05 first boss within one day");
            Check(ExpeditionRules.RecommendedFood(150, 4) == 16, "G05 round trip + local day + spare day");
            var first = world.CreateWalkingParty(new[] { commanders[0] });
            var second = world.CreateWalkingParty(new[] { commanders[1] });
            Check(first != null && second != null && first.Walking && first.CommanderLoad == 1 && first.Load == 0, "G05 walking without vehicle or conscription");
            Check(first.LeavesHomeUnattended && ExpeditionRules.HomeCommanders == 0, "G21 all away warning");
            Check(!diplomacy.TryDispatchIndependentRaid(), "G21 generic incursion deferred with no home commander");
            var incursions = Object.FindAnyObjectByType<LocalIncursions>();
            Check(incursions != null && !incursions.TrySpawnIndependent(3), "G21 direct generic spawn deferred");
            Check(!first.TryBoard(new[] { commanders[0] }), "G15 no duplicate passenger");
            Check(!HomeDefenseReturn.Allows(commanders[0]), "G48 expedition excluded");
            var oldDaily = first.DailyFood;
            Call(commanders[0], "RestoreTroops", 3, 0f);
            Check(Near(first.DailyFood - oldDaily, 6 * GameBalance.MealFood), "G05 soldiers consume two meals per day");
            Call(commanders[0], "RestoreTroops", 0, 0f);
            Check(first.TryLoadFood(20), "G05 supply loaded from own fixture");
            Check(first.ReturnCargoSpace == first.CargoCapacity - first.CargoLoad, "G05 return cargo space");
            var destination = world.Sites.First(s => s.Kind == ExpeditionSiteKind.ResourceSite);
            var before = first.AvailableFood;
            Check(first.TryDepart(destination), "G05 walking departure");
            var oneWay = first.TravelSeconds;
            first.Tick(oneWay / 2);
            Check(first.State == ExpeditionState.Outbound && first.Remaining > 0, "G21 no instant arrival");
            first.Tick(oneWay / 2);
            Check(first.State == ExpeditionState.Deployed && first.AvailableFood < before, "G05 actual travel consumes supplies");
            Check(second.TryDepart(destination), "G15 second party same destination"); second.Tick(second.TravelSeconds);
            Check(second.State == ExpeditionState.Deployed && destination.Visitor != null, "G15 reinforcements enter occupied battlefield");
            Check(first.TryBoard(new[] { commanders[1] }) && second.Crew.Count == 0 && commanders[1].Transport == first, "G15 merge crews on site");
            Check(!HomeDefenseReturn.Allows(commanders[1]), "G48 local expedition excluded");
            Check(first.TryReturn(), "G21 begin physical return"); first.Tick(oneWay / 2);
            Check(first.State == ExpeditionState.Returning, "G21 return keeps travel time"); first.Tick(oneWay / 2);
            Check(first.State == ExpeditionState.Home && commanders.All(c => c.Transport == null), "G05 home unload preserves crew");

            var vehicle = world.CreateTransport(false, world.HomePosition);
            var aircraft = world.CreateTransport(true, world.HomePosition + Vector3.right * 5);
            for (var i = 0; i < commanders.Length; i++) Call(commanders[i], "SetEmbarked", false, world.HomePosition + Vector3.right * (2 + i));
            Check(vehicle.Capacity == Mathf.FloorToInt(GameBalance.VehicleTroops * ScienceEffects.TroopCapacityMultiplier)
                && aircraft.Capacity == Mathf.FloorToInt(GameBalance.AircraftTroops * ScienceEffects.TroopCapacityMultiplier), "G15 40/100 are per-trip capacities");
            Check(vehicle.TryBoard(new[] { commanders[0] }) && aircraft.TryBoard(new[] { commanders[1] }), "G15 separate transports board home crew");
            Check(vehicle.TryDepart(destination) && aircraft.TryDepart(destination), "G15 multiple transports can depart to same site");
            aircraft.Tick(aircraft.TravelSeconds); vehicle.Tick(vehicle.TravelSeconds);
            Check(aircraft.State == ExpeditionState.Deployed && vehicle.State == ExpeditionState.Outbound, "G15 only one docking operation at a time");
            aircraft.Tick(ExpeditionRules.DockSeconds); vehicle.Tick(.01f);
            Check(vehicle.State == ExpeditionState.Deployed, "G15 dock frees after unloading, deployed unit does not block");
            Check(vehicle.TryReturn() && aircraft.TryReturn(), "G15 both transports return");
            vehicle.Tick(vehicle.TravelSeconds); aircraft.Tick(aircraft.TravelSeconds);
            var disabled = world.DefenseReturn;
            Check(disabled.Enabled(commanders[0]), "G48 enabled by default"); disabled.Set(commanders[0], false);
            var defenseCopy = JsonUtility.FromJson<HomeDefenseReturn>(JsonUtility.ToJson(disabled));
            Check(!defenseCopy.Enabled(commanders[0]) && defenseCopy.Enabled(commanders[1]), "G48 per formation setting serializes"); disabled.Set(commanders[0], true);

            Check(diplomacy.TryDispatchIndependentRaid(), "G62 independent army departs from a real settlement");
            var independent = diplomacy.Data.travellingRaids.Single();
            Check(independent.faction == "" && independent.arrive > diplomacy.Data.elapsed && independent.count > 0,
                "G62 independent army has source, strength and travel time");
            Check(!diplomacy.TryDispatchIndependentRaid(), "G62 no duplicate independent departure");
            first = world.CreateWalkingParty(new[] { commanders[0] }); second = world.CreateWalkingParty(new[] { commanders[1] });
            Check(first != null && second != null, "G21 prepare unattended home during enemy travel");
            diplomacy.Data.elapsed = independent.arrive;
            Call(diplomacy, "TickRaidTravel");
            Check(diplomacy.Data.travellingRaids.Contains(independent), "G21 independent arrival waits with zero home commanders");
            Check(first.TryUnloadCrew() && second.TryUnloadCrew(), "G21 home defenders become available again");
            var independentVisitors = incursions.Visitors.Count;
            Call(diplomacy, "TickRaidTravel");
            Check(!diplomacy.Data.travellingRaids.Contains(independent) && incursions.Visitors.Count == independentVisitors + independent.count,
                "G62 deferred independent army arrives once");
            foreach (var raider in incursions.Visitors.ToArray()) if (raider != null) Object.DestroyImmediate(raider.gameObject);

            var owned = world.Sites.First(s => diplomacy.Faction(s) != null);
            var civ = diplomacy.Faction(owned); diplomacy.Data.elapsed = 123;
            Check(!civ.contacted && !diplomacy.DeclareWar(civ), "G62 no first contact from remote declaration");
            Call(diplomacy, "Monthly", civ); Check(!civ.war && diplomacy.Data.travellingRaids.Count == 0, "G62 uncontacted civilization never initiates invasion");
            diplomacy.Contact(destination); Check(!civ.contacted, "G62 neutral resource arrival is not civilization contact");
            diplomacy.Contact(owned);
            Check(civ.contacted && Near(civ.contactedAt, 123) && Near(civ.nextConquestCheck, 1023), "G62 contact starts six-day grace");
            diplomacy.Contact(owned); Check(Near(civ.nextConquestCheck, 1023), "G62 repeat contact does not reset grace");
            Check(!DiplomacyManager.IsWeakAgainst(149.99f, 100) && DiplomacyManager.IsWeakAgainst(150, 100), "G62 1.5 ratio boundary");
            Check(!DiplomacyManager.IsWeakAgainst(0, 0), "G62 empty army cannot invade");
            civ.agenda = LeaderAgenda.Conqueror; civ.hiddenAgenda = LeaderAgenda.Warrior;
            Call(diplomacy, "TickConquerors"); Check(!civ.war, "G62 conqueror cannot attack during grace");
            Check(diplomacy.DeclareWar(civ) && !(bool)Call(diplomacy, "Raid", civ, true), "G62 even declared war observes conqueror grace");
            diplomacy.Data.elapsed = civ.nextConquestCheck;
            civ.war = false;
            var source = world.Sites.First(s => diplomacy.Faction(s) == civ && s.Colony != null && !s.Colony.IsDefeated);
            var stockBefore = source.Colony.GetStock(AntColony.Data.ResourceType.Food);
            Call(diplomacy, "TickConquerors");
            Check(civ.war && diplomacy.Data.travellingRaids.Count == 1 && Near(civ.nextConquestCheck, diplomacy.Data.elapsed + DiplomacyRules.Season),
                "G62 conqueror declares and dispatches only at seasonal check");
            Call(diplomacy, "TickConquerors");
            Check(diplomacy.Data.travellingRaids.Count == 1, "G62 seasonal check does not repeat within the season");
            var travelling = diplomacy.Data.travellingRaids.Single();
            Check(Near(travelling.arrive, diplomacy.Data.elapsed + ExpeditionRules.WalkSeconds(source))
                && Near(stockBefore - source.Colony.GetStock(AntColony.Data.ResourceType.Food), travelling.count * 10), "G62 travel and actual source payment");
            Call(diplomacy, "TickRaidTravel"); Check(diplomacy.Data.travellingRaids.Count == 1, "G62 no immediate home invasion");
            var stateCopy = JsonUtility.FromJson<DiplomacyManager.State>(JsonUtility.ToJson(diplomacy.Capture()));
            Check(stateCopy.travellingRaids.Count == 1 && Near(stateCopy.travellingRaids[0].arrive, travelling.arrive), "G62 travel survives JSON roundtrip");
            Check(DiplomacyManager.Validate(stateCopy, world.Sites.Count), "G62 new saved fields validate");
            diplomacy.Restore(stateCopy); civ = diplomacy.Faction(owned); travelling = diplomacy.Data.travellingRaids.Single();
            Check(Near(source.Colony.GetStock(AntColony.Data.ResourceType.Food), stockBefore - travelling.count * 10),
                "G62 restoring travelling army does not pay departure again");
            stateCopy.travellingRaids[0].arrive = float.NaN;
            Check(!DiplomacyManager.Validate(stateCopy, world.Sites.Count), "G62 corrupt travel rejected");
            var arrival = travelling.arrive;
            diplomacy.Data.elapsed = arrival;
            var visitorsBefore = incursions.Visitors.Count(v => v != null && !v.IsDead); Call(diplomacy, "TickRaidTravel");
            Check(diplomacy.Data.travellingRaids.Count == 0 && incursions.Visitors.Count(v => v != null && !v.IsDead) == visitorsBefore + travelling.count, "G62 army enters home only on arrival");
            Call(diplomacy, "TickRaidTravel");
            Check(incursions.Visitors.Count(v => v != null && !v.IsDead) == visitorsBefore + travelling.count, "G62 arrival cannot spawn the same army twice");
            foreach (var raider in incursions.Visitors.ToArray()) if (raider != null && raider.DiplomaticFactionId == civ.id) Object.DestroyImmediate(raider.gameObject);
            civ.war = false;

            var offer = diplomacy.StartTalk(civ, TalkKind.TreatyOffer); offer.take.treaties.Add(TreatyKind.Trade);
            Check(Near(offer.expires - diplomacy.Data.elapsed, GameCalendar.SecondsPerSeason), "G67 proposal lasts a season");
            Check(diplomacy.Respond(offer, true).Contains("담당") && diplomacy.Data.talks.Contains(offer), "G67 no acceptance without assigned negotiator");
            Check(diplomacy.Respond(offer, false).Contains("거절") && !diplomacy.Data.talks.Contains(offer), "G67 refusal without negotiator");
            var demand = diplomacy.StartTalk(civ, TalkKind.Demand); demand.negotiator = commanders[0].PersonalState.id;
            demand.ultimatum = true; demand.take.resources[0] = 200;
            civ.resources[0] = 1000; civ.threatCred = -100; diplomacy.Data.threat = -100;
            Check(DiplomacyManager.CanNegotiate(commanders[0]), "G67 active home negotiator eligible");
            var bloodLoss = commanders[0].PersonalState.health.bloodLoss;
            var homePower = diplomacy.AvailableHomePower;
            commanders[0].PersonalState.health.bloodLoss = 1;
            Check(commanders[0].PersonalState.health.Downed && !DiplomacyManager.CanNegotiate(commanders[0]), "G67 downed negotiator excluded even with personal health remaining");
            Check(Near(homePower - diplomacy.AvailableHomePower, 20 + commanders[0].TroopCount), "G62 downed commander is not available home power");
            commanders[0].PersonalState.health.bloodLoss = bloodLoss;
            diplomacy.Propose(demand);
            Check(demand.deadline > 0 && Near(demand.deadline - diplomacy.Data.elapsed,
                diplomacy.EstimatedTravel(civ) + 3 * GameCalendar.SecondsPerDay) && demand.expires == 0, "G67 rejected ultimatum travel plus three days");
            diplomacy.Data.elapsed = demand.deadline; Call(diplomacy, "TickTalks");
            Check(!diplomacy.Data.talks.Contains(demand), "G67 ultimatum expires once");
            var credibility = civ.threatCred; var threat = diplomacy.Data.threat;
            diplomacy.WithdrawUltimatum(demand); Call(diplomacy, "TickTalks");
            Check(civ.threatCred == credibility && diplomacy.Data.threat == threat, "G67 ended ultimatum cannot apply withdrawal twice");
            var expired = diplomacy.StartTalk(civ, TalkKind.TreatyOffer);
            diplomacy.Data.elapsed = expired.expires; Check(diplomacy.Respond(expired, true).Contains("기한"), "G67 expired acceptance rejected before next tick");
            Call(diplomacy, "TickTalks"); Check(!diplomacy.Data.talks.Contains(expired), "G67 normal offer expires");

            var legacy = diplomacy.Capture();
            legacy.travellingRaids = null;
            var oldContact = legacy.civilizations.First(c => c.contacted);
            oldContact.contactedAt = oldContact.nextConquestCheck = 0;
            Check(DiplomacyManager.Validate(legacy, world.Sites.Count), "G62 old save accepts absent travel and contact timers");
            diplomacy.Restore(legacy);
            var restored = diplomacy.Data.civilizations.First(c => c.id == oldContact.id);
            Check(diplomacy.Data.travellingRaids.Count == 0 && Near(restored.contactedAt, diplomacy.Data.elapsed)
                && Near(restored.nextConquestCheck, diplomacy.Data.elapsed + DiplomacyRules.Season), "G62 old save receives grace without spawning or paying a raid");

            SimulationPause.Restore(null); SimulationPause.UserSpeed(3);
            Check(SimulationPause.Incident("b5:attack", "위협", true) && Time.timeScale == 0, "G44 new attack auto pause");
            SimulationPause.Hold("manual"); SimulationPause.Hold("negotiation"); SimulationPause.Release("negotiation");
            Check(Time.timeScale == 0 && SimulationPause.Reasons.Contains("manual"), "G44 closing talks preserves other reasons");
            SimulationPause.UserSpeed(3);
            Check(!SimulationPause.Incident("b5:attack", "후속 타격", true) && Time.timeScale == 3, "G44 same incident does not pause twice");
            SimulationPause.Hold("negotiation"); SimulationPause.UserSpeed(2);
            Check(Time.timeScale == 0, "G44 forced negotiation pause survives speed request"); SimulationPause.Release("negotiation");
            Check(Time.timeScale == 2, "G44 chosen speed after negotiation release");
            SimulationPause.Incident("b5:notice", "일반 알림", false); Check(Time.timeScale == 2, "G44 ordinary notice does not pause");
            SimulationPause.Observe(new Dictionary<string, string> { ["threat:b5-home"] = "본거지", ["threat:b5-site"] = "현지" }, true);
            var incidentCount = SimulationPause.Capture().incidents.Count;
            SimulationPause.UserSpeed(1);
            SimulationPause.Observe(new Dictionary<string, string> { ["threat:b5-home"] = "본거지", ["threat:b5-site"] = "현지" }, true);
            Check(Time.timeScale == 1 && SimulationPause.Capture().incidents.Count == incidentCount, "G21 simultaneous threats coalesce by region");
            var pauseCopy = JsonUtility.FromJson<SimulationPause.State>(JsonUtility.ToJson(SimulationPause.Capture()));
            Check(SimulationPause.Valid(pauseCopy) && pauseCopy.active.Count == 2, "G21/G44 incident state serializes");
            Check(JsonUtility.FromJson<TransportDto>("{}").walking == false, "old transport defaults to vehicle");
            return "PASS " + count;
        }
        finally
        {
            if (pileObject != null) Object.Destroy(pileObject);
            if (pileData != null) Object.Destroy(pileData);
            SimulationPause.Restore(null); Time.timeScale = scale;
            UnityEngine.Random.state = random; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false);
        }
    }
}
