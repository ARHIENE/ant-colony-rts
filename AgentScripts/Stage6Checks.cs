using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using Object = UnityEngine.Object;
using Resource = AntColony.Data.ResourceType;

public static class Stage6Checks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL " + checks + ": " + label); checks++; }
    static async Task Ready()
    { var until = DateTime.UtcNow.AddSeconds(90); while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50); Check(!SaveSystem.Busy, "ready"); Time.timeScale = 0; }
    static T Copy<T>(T value) => JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play required"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Stage6-" + Guid.NewGuid().ToString("N"));
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260926, mapSize = MapSize.Small }); await Ready();
            var world = WorldMapManager.Instance; var d = DiplomacyManager.Instance;
            Check(world.Sites.Count == 33, "33 sites"); Check(d.Data.civilizations.Count == 4, "four leaders");
            Check(d.Data.civilizations.Select(c => c.agenda).Distinct().Count() == 4, "unique public agendas");
            Check(d.Data.civilizations.All(c => c.agenda != c.hiddenAgenda), "different hidden agendas");
            Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.Settlement) == 18, "18 settlements");
            Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.TradePost) == 3, "three trade posts");
            Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.BossNest) == 6, "six bosses");
            Check(world.Sites.Count(s => s.Kind == ExpeditionSiteKind.ResourceSite) == 6, "six resources");
            foreach (var civ in d.Data.civilizations) Check(world.Sites.Count(s => d.Faction(s) == civ) == 3, "three adjacent holdings " + civ.name);
            Check(DiplomacyManager.InitialState(260926, false).blueprintSite == d.Data.blueprintSite, "seed fixes blueprint market");
            var site = world.Sites.First(s => d.Faction(s) != null); var c = d.Faction(site);
            Check(!d.Available && !d.DeclareWar(c), "diplomacy locked"); d.Contact(site); Check(!c.contacted, "no early contact");
            typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(world, new object[] { true });
            d.Contact(site); Check(c.contacted, "arrival contact");
            var guard = site.GetComponentInChildren<EnemyCommander>();
            Check(!CombatTargeting.CanAttack(UnitRole.Melee, guard), "peace blocks automatic attack");
            Check(d.SignTreaty(c, TreatyKind.Trade) && c.hiddenRevealed, "trade reveals hidden agenda");
            Check(c.treaties[0] == d.Data.elapsed + DiplomacyRules.Year, "one year treaty");
            Check(d.SignTreaty(c, TreatyKind.NonAggression) && !d.DeclareWar(c, true), "NAP prevents surprise war");
            c.treaties[1] = 0;
            var affinities = d.Data.civilizations.Select(x => x.affinity).ToArray();
            Check(d.DeclareWar(c, true) && c.war, "surprise war");
            for (var i = 1; i < 4; i++) Check(d.Data.civilizations[i].affinity == affinities[i] - 30, "global surprise penalty");
            Check(c.treaties.All(t => t == 0) && CombatTargeting.CanAttack(UnitRole.Melee, guard), "war breaks pacts and enables attacks");
            d.CommanderDowned(guard.Position); Check(c.enemyScore == 10, "downed commander scores for enemy");
            Check(!d.MakePeace(c), "ten minute peace delay"); d.Data.elapsed += 600;
            var rm = ResourceManager.Instance;
            foreach (Resource type in Enum.GetValues(typeof(Resource))) { rm.AddCapacity(type, 10000); rm.Add(type, 5000); }
            c.enemyScore = 50; c.playerScore = 10; var food = rm.GetAmount(Resource.Food);
            Check(d.MakePeace(c) && !c.war && rm.GetAmount(Resource.Food) == food - 40, "peace pays score difference");
            Check(DiplomacyRules.Accepts(120, 100, 0) && !DiplomacyRules.Accepts(119, 100, 0), "AI threshold");
            Check(Mathf.Abs(DiplomacyRules.PriceMultiplier(-100) - 1.3f) < .001f && Mathf.Abs(DiplomacyRules.PriceMultiplier(100) - .7f) < .001f, "price limits");
            c.affinity = 100;
            var give = new TradeOffer(); var take = new TradeOffer(); give.resources[0] = 100; take.resources[2] = 1;
            food = rm.GetAmount(Resource.Food); var special = rm.GetAmount(Resource.Special);
            Check(d.TryTrade(c, give, take, out var error), "resource exchange " + error);
            Check(rm.GetAmount(Resource.Food) == food - 100 && rm.GetAmount(Resource.Special) == special + 1, "exact resource exchange");
            give.resources[0] = -1; food = rm.GetAmount(Resource.Food);
            Check(!d.TryTrade(c, give, take, out error) && food == rm.GetAmount(Resource.Food), "invalid quantity atomic rejection");
            give.resources[0] = 100; take.resources[2] = c.resources[2] + 1;
            Check(!d.TryTrade(c, give, take, out error), "counterparty stock check");
            take = new TradeOffer(); var item = c.equipment[0]; take.equipment.Add(item.id);
            Check(d.TryTrade(c, give, take, out error) && EquipmentInventory.Instance.Items.Contains(item) && !c.equipment.Contains(item), "equipment ownership transfer " + error);
            Check(!d.TryTrade(c, give, take, out error), "stale item cannot duplicate");
            give = new TradeOffer(); take = new TradeOffer(); give.resources[0] = 1000; take.treaties.Add(TreatyKind.Alliance);
            Check(d.TryTrade(c, give, take, out error) && c.HasTreaty(TreatyKind.Alliance, d.Data.elapsed), "alliance offer " + error);
            d.Data.caravanUntil = d.Data.elapsed + 90; var market = d.Market(-1);
            Check(market != null && market.equipment.Count == 10, "caravan catalog");
            give = new TradeOffer(); take = new TradeOffer(); give.resources[0] = 10; take.resources[2] = 1;
            Check(d.TryTrade(market, give, take, out error), "market ten food per special " + error);
            d.Data.elapsed += 90; Check(!d.TryTrade(market, give, take, out error), "expired caravan cannot trade");
            GameMenuController.Instance.Diplomacy(c); Canvas.ForceUpdateCanvases();
            Check(GameMenuController.Instance.ScreenName.Contains("외교"), "J diplomacy screen");
            Check(Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(t => t.text.Contains(c.leader)), "leader visible");
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            foreach (var unit in CommanderRoster.Instance.Commanders) unit.CommandStop();
            var file = SaveSnapshot.Capture(); Check(SaveValidator.Validate(file, out error), "v7 snapshot " + error);
            var bad = Copy(file); bad.diplomacy.civilizations[0].resources[0] = -1; Check(!SaveValidator.Validate(bad, out error), "negative foreign stock rejected");
            bad = Copy(file); bad.diplomacy.civilizations[0].treaties[0] = float.NaN; Check(!SaveValidator.Validate(bad, out error), "invalid expiry rejected");
            var old = Copy(file); old.version = 6; old.diplomacy = null;
            Check(SaveValidator.Validate(old, out error) && old.version == 7 && old.diplomacy.civilizations.Count == 4, "v6 migration " + error);
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "roundtrip " + error); await Ready();
            d = DiplomacyManager.Instance; c = d.Data.civilizations[0];
            Check(c.contacted && c.hiddenRevealed && c.HasTreaty(TreatyKind.Alliance, d.Data.elapsed), "relations and treaty survive load");
            Check(d.Data.markets.Count == 1 && d.Data.elapsed == file.diplomacy.elapsed, "market and clock survive load");
            var rebel = CommanderRoster.Instance.Commanders[0]; rebel.TryAssign(1); var id = rebel.PersonalState.id;
            typeof(CommanderAnt).GetMethod("BeginDeparture", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(rebel, new object[] { true });
            Check(d.ReceiveDeparture(rebel, true), "rebellion creates faction"); await Task.Delay(60);
            Check(d.Data.civilizations.Last().rebel && d.Data.civilizations.Last().war && d.Data.civilizations.Last().affinity == -60, "rebel initial diplomacy");
            Check(d.Data.extraSites == 1 && WorldMapManager.Instance.Sites.Count == 34, "no empty site creates camp");
            Check(SaveSystem.TrySave(false, 0, out error) && SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "rebel save roundtrip " + error); await Ready();
            d = DiplomacyManager.Instance;
            Check(d.Data.rebelMembers.Count == 1 && d.Data.rebelMembers[0].commander.PersonalState.id == id, "rebel identity survives");
            return "PASS " + checks + " stage 6 checks";
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; }
    }
}

