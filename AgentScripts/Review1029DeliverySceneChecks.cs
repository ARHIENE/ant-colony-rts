using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// Claude의 Unity 전용 단계에서 실행. 오프라인 검사와 구분하며 기존 사용자 저장에 쓰지 않는다.
public static class Review1029DeliverySceneChecks
{
    static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static async Task Ready()
    {
        var deadline = DateTime.UtcNow.AddSeconds(100);
        while (AntColony.Save.SaveSystem.Busy && DateTime.UtcNow < deadline) await Task.Delay(80);
        if (AntColony.Save.SaveSystem.Busy) throw new Exception("새 게임/불러오기 대기 초과");
    }

    public static async Task<string> Main()
    {
        var count = 0;
        void Check(bool ok, string why) { if (!ok) throw new Exception(why); count++; }
        var root = AntColony.Save.SaveStorage.RootOverride;
        var settings = AntColony.Core.UserSettings.Current.Clone();
        AntColony.Save.SaveStorage.RootOverride = Path.Combine(UnityEngine.Application.temporaryCachePath, "Review1029J1-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = false;
            AntColony.Core.UserSettings.Apply(quiet, false);
            await Ready();
            AntColony.Save.SaveSystem.NewGame(new AntColony.Core.NewGameOptions { seed = 291029, mapSize = AntColony.Core.MapSize.Small });
            await Ready(); AntColony.UI.GameMenuController.Instance.Resume(); UnityEngine.Time.timeScale = 0;
            foreach (var c in AntColony.Core.CommanderRoster.Instance.Commanders)
            { c.CommandStop(); c.SetJobEnabled(AntColony.Units.CommanderJobs.All, false); }
            var world = AntColony.World.WorldMapManager.Instance;
            var ship = world.CreateTransport(false, world.HomePosition + UnityEngine.Vector3.right * 8);
            var index = world.Sites.ToList().FindIndex(s => s.Kind == AntColony.World.ExpeditionSiteKind.TradePost);
            Check(index >= 0, "교역소 존재"); var site = world.Sites[index];
            var restore = typeof(AntColony.World.ExpeditionTransport).GetMethod("RestoreState", Hidden);
            restore.Invoke(ship, new object[] { AntColony.World.ExpeditionState.Deployed, 0f, site, site.Landing, world.HomePosition, 0, 0, 0 });
            var d = AntColony.World.DiplomacyManager.Instance; d.Data.blueprintSite = index;
            var market = d.Market(index); Check(market != null && d.MarketTransport(market) == ship, "현지 방문 수송 선택");
            var research = AntColony.Core.CampaignResearch.Instance;
            research.RecordSampleDelivery(AntColony.Data.ResourceType.Food, 1);
            research.RecordSampleDelivery(AntColony.Data.ResourceType.IronOre, 1);
            Check(!research.PendingSamples.Contains(AntColony.Data.ResourceType.Food)
                && research.PendingSamples.Contains(AntColony.Data.ResourceType.IronOre), "표본 목록의 실제 채굴 자원 필터");
            Check(!research.TryStartResourceResearch(AntColony.Data.ResourceType.Food), "무의미한 표본 신규 시작 거부");
            AntColony.UI.GameMenuController.Instance.ShowDiet();
            UnityEngine.UI.Button DietButton(string name) => UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>()
                .Single(b => b.name == name && b.isActiveAndEnabled);
            DietButton("Save Fine Meals").onClick.Invoke();
            Check(AntColony.Units.CommanderAnt.CommonDiet == 3, "실제 아끼기 버튼");
            DietButton("Allow All Meals").onClick.Invoke();
            Check(AntColony.Units.CommanderAnt.CommonDiet == 7, "실제 모두 허용 버튼");
            var rm = AntColony.Core.ResourceManager.Instance;
            foreach (var type in new[] { AntColony.Data.ResourceType.Food, AntColony.Data.ResourceType.Soil, AntColony.Data.ResourceType.Special })
            { rm.AddCapacity(type, 5000); rm.Add(type, 2000); }
            var item = market.equipment[0]; var itemId = item.id;
            var give = new AntColony.World.TradeOffer(); give.resources[0] = 100; give.resources[2] = 150;
            var take = new AntColony.World.TradeOffer(); take.resources[1] = 2; take.equipment.Add(itemId); var bpOnly = new AntColony.World.TradeOffer(); bpOnly.blueprint = true;
            Check(!d.TryTrade(market, give, bpOnly, out var bpError) && bpError.Contains("보스 D"), "보스 D 전 설계도 구매 거부 " + bpError);
            var soil = rm.GetAmount(AntColony.Data.ResourceType.Soil); var food = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Check(d.TryTrade(market, give, take, out var error), "현지 교역 성립 " + error);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == food - 100 && rm.GetAmount(AntColony.Data.ResourceType.Soil) == soil, "결제만 본거지 반영");
            Check(ship.GetCargo(AntColony.Data.ResourceType.Soil) == 2 && ship.EquipmentCargo.Any(e => e.id == itemId), "자원·장비 화물 보관");
            Check(!ship.BlueprintCargo && !AntColony.Core.CampaignResearch.Instance.HasBlueprint, "설계도 화물 없음");
            Check(!AntColony.Units.EquipmentInventory.Instance.Items.Any(e => e.id == itemId), "장비 귀환 전 본거지 미지급");
            var tooMuch = new AntColony.World.TradeOffer(); tooMuch.resources[0] = ship.ReturnCargoSpace + 1;
            var before = rm.GetAmount(AntColony.Data.ResourceType.Food);
            Check(!d.TryTrade(market, give, tooMuch, out error) && error.Contains("수송칸"), "만재 거래 거부 " + error);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Food) == before, "거부 시 미결제");
            Check(AntColony.Save.SaveSystem.TrySave(false, 0, out error), "실제 저장 " + error);
            var saved = AntColony.Save.SaveSlots.PathFor(false, 0);
            Check(AntColony.Save.SaveSystem.TryLoad(saved, out error), "실제 불러오기 " + error); await Ready();
            UnityEngine.Time.timeScale = 0; world = AntColony.World.WorldMapManager.Instance;
            ship = world.Transports.Single();
            Check(ship.GetCargo(AntColony.Data.ResourceType.Soil) == 2
                && ship.EquipmentCargo.Any(e => e.id == itemId), "현지 구매 자원·장비 저장 왕복");
            rm = AntColony.Core.ResourceManager.Instance; soil = rm.GetAmount(AntColony.Data.ResourceType.Soil);
            typeof(AntColony.World.ExpeditionSite).GetProperty("Visitor").SetValue(ship.Site, null);
            restore.Invoke(ship, new object[] { AntColony.World.ExpeditionState.Home, 0f, null, world.HomePosition, world.HomePosition, 0, 2, 0 });
            typeof(AntColony.World.ExpeditionTransport).GetMethod("UnloadCargo", Hidden).Invoke(ship, null);
            Check(rm.GetAmount(AntColony.Data.ResourceType.Soil) == soil + 2, "귀환 하역");
            Check(AntColony.Units.EquipmentInventory.Instance.Items.Any(e => e.id == itemId) && !ship.HasCargo, "장비 하역·화물 소모");
            return $"PASS {count} 현지교역·실제저장 검사";
        }
        finally
        {
            UnityEngine.Time.timeScale = 0; AntColony.Save.SaveStorage.RootOverride = root;
            AntColony.Core.UserSettings.Apply(settings, false);
        }
    }
}
