using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Data;
using AntColony.Save;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using TradeResource = AntColony.Data.ResourceType;
public static class TradeGrowth1028Checks
{
    static int count;
    static void Check(bool value, string label) { if (!value) throw new Exception("Trade " + count + ": " + label); count++; }
    static async Task Ready() { var end = DateTime.UtcNow.AddSeconds(100); while (SaveSystem.Busy && DateTime.UtcNow < end) await Task.Delay(80); Check(!SaveSystem.Busy, "ready"); }
    public static async Task<string> Main(bool baseline = false)
    {
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "TradeGrowth-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false; UserSettings.Apply(quiet, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 281028, mapSize = MapSize.Small }); await Ready();
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var world = WorldMapManager.Instance;
            typeof(WorldMapManager).GetMethod("RestoreUnlock", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(world, new object[] { true });
            var d = DiplomacyManager.Instance; var c = d.Data.civilizations[0]; c.contacted = true; c.affinity = 100;
            d.Data.trust = 100; c.treaties[(int)TreatyKind.Trade] = d.Data.elapsed + 3600;
            var rm = ResourceManager.Instance;
            foreach (var type in new[] { TradeResource.Food, TradeResource.Soil }) { rm.AddCapacity(type, 10000); rm.Add(type, 1000); }
            var start = rm.GetAmount(TradeResource.Food) + rm.GetAmount(TradeResource.Soil);
            var give = new TradeOffer(); give.resources[0] = 50;
            var take = new TradeOffer(); take.resources[1] = 100;
            var first = d.TryTrade(c, give, take, out var error);
            give = new TradeOffer(); give.resources[1] = 100;
            take = new TradeOffer(); take.resources[0] = 200;
            var second = first && d.TryTrade(c, give, take, out error);
            var gain = rm.GetAmount(TradeResource.Food) + rm.GetAmount(TradeResource.Soil) - start;
            var report = $"friendly factor={d.PriceFactor(c):0.######}, first={first}, second={second}, roundtrip gain={gain}";
            if (baseline) return "BASELINE " + report;
            Check(!first && gain == 0, "ordinary trade cannot create value by roundtrip");
            give = new TradeOffer(); give.resources[0] = 100; take = new TradeOffer(); take.resources[1] = 100;
            Check(d.TryTrade(c, give, take, out error), "friendly fair exchange " + error);
            for (var affinity = -100; affinity <= 100; affinity += 20)
            {
                c.affinity = affinity;
                Check(d.PriceFactor(c) >= 1, "trade floor " + affinity);
            }
            c.affinity = 100;
            var n = d.StartTalk(c, TalkKind.Trade); n.give = give; n.take = take;
            var negotiator = CommanderRoster.Instance.Commanders[0];
            negotiator.Talents.levels[(int)CommanderActivity.Politics] = 20; n.negotiator = negotiator.PersonalState.id;
            Check(DiplomacyManager.Persuasion(n) > .09, "maximum politics fixture");
            n.give.resources[0] = 90;
            Check(d.Ratio(c, n) < 1, "negotiation also rejects below fair value");
            n.give.resources[0] = 100;
            Check(d.Ratio(c, n) >= 1, "negotiation accepts fair value");
            c.affinity = -100; var required = 100 * (1 + (d.PriceFactor(c) - 1) * .9);
            n.give.resources[0] = (int)Math.Ceiling(required);
            Check(d.Ratio(c, n) >= 1, "politics reduces hostile markup");
            n.give.resources[0] = -1;
            Check(!d.TryTrade(c, n.give, n.take, out error), "invalid trade remains atomic");
            c.war = true; c.warStarted = d.Data.elapsed - DiplomacyRules.PeaceDelay; c.enemyScore = 0; c.playerScore = 40;
            c.resources[0] = 100; rm.Add(TradeResource.Food, rm.GetCapacity(TradeResource.Food));
            Check(rm.GetCapacity(TradeResource.Food) == rm.GetAmount(TradeResource.Food) && d.CanNegotiatePeace(c), "full warehouse and peace eligible");
            float FloorFood() => ResourceNode.Available.Where(node => node.IsLooseCargo && node.ResourceType == TradeResource.Food).Sum(node => node.AmountRemaining);
            var floor = FloorFood(); var foodBeforePeace = rm.GetAmount(TradeResource.Food);
            Check(d.MakePeace(c) && !c.war && c.resources[0] == 60, "peace despite full warehouse");
            Check(rm.GetAmount(TradeResource.Food) - foodBeforePeace + FloorFood() - floor == 40, "reparations conserved in warehouse/floor");
            return "PASS " + count + " trade checks; " + report;
        }
        finally { Time.timeScale = 0; SaveStorage.RootOverride = root; UserSettings.Apply(settings, false); }
    }
}
