namespace AntColony.Regression
{
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

    using ResourceType = AntColony.Data.ResourceType;
    // 새 Play 세션에서 실행. 종료 시 테스트에서 바꾼 런타임 상태가 복구된다.
    public static class AnnexedSettlementChecks
    {
        static int checks;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        static void Check(bool ok, string message)
        { if (!ok) throw new Exception("FAIL: " + message); checks++; }
        static void Set(object target, string field, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var member = type.GetField(field, Flags);
                if (member != null) { member.SetValue(target, value); return; }
            }
            throw new Exception("Missing field " + field);
        }
        static async Task<bool> Wait(Func<bool> condition, int seconds = 30)
        {
            var end = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition() && DateTime.UtcNow < end) await Task.Delay(25);
            return condition();
        }
        static void Move(CommanderAnt commander, Vector3 position)
        {
            commander.CommandStop();
            commander.Agent.enabled = false;
            Check(NavMesh.SamplePosition(position, out var hit, 8, NavMesh.AllAreas), "test position on navmesh");
            commander.transform.position = hit.position;
            commander.ApplyMovementMode();
            Physics.SyncTransforms();
        }
        static T Template<T>() where T : Component => Object.FindObjectsByType<T>(FindObjectsInactive.Include)
            .First(t => t.name.EndsWith("Template") && t.GetComponentInParent<ExpeditionSite>(true) == null);

        public static async Task<string> Main()
        {
            checks = 0;
            Check(Application.isPlaying, "Play mode required");
            // 새 Play 세션은 메인 메뉴(일시정지)로 시작하므로 필요하면 게임을 직접 시작한다.
            if (!GameSession.Instance.GameStarted)
            {
                while (AntColony.Save.SaveSystem.Busy) await Task.Delay(50);
                AntColony.Save.SaveSystem.NewGame(new NewGameOptions());
                while (AntColony.Save.SaveSystem.Busy) await Task.Delay(50);
                AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            }
            var world = WorldMapManager.Instance;
            var site = world.Sites.First(s => s.Kind == ExpeditionSiteKind.Settlement);
            var colony = site.Colony;
            Object.FindAnyObjectByType<UpkeepManager>().enabled = false;
            var resources = ResourceManager.Instance;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            { resources.AddCapacity(type, 10000); resources.Add(type, 8000); }
            AntPool.Instance.Breed(100);
            var commander = CommanderRoster.Instance.Commanders[0];
            commander.CommandStop();
            if (commander.TroopCount < 5) Check(commander.TryAssign(5 - commander.TroopCount), "crew has troops");
            Check(world.CanCreateTransport(commander.Position, out var position), "transport placement available");
            var ship = world.CreateTransport(false, position);
            Move(commander, ship.Position + Vector3.right * 3);
            Check(ship.TryBoard(new[] { commander }) && ship.TryDepart(site), "crew departs");
            Check(!ship.TryStation(ship.Crew), "cannot station during travel");
            ship.Tick(ship.TravelSeconds);
            Check(!ship.TryStation(ship.Crew), "hostile site rejects garrison");
            foreach (var building in colony.GetComponentsInChildren<BuildingBase>()) building.TakeDamage(float.MaxValue);
            foreach (var guard in site.GetComponentsInChildren<WildMonster>()) guard.TakeDamage(float.MaxValue);
            Check(await Wait(() => site.Cleared), "settlement conquered");
            var nodes = colony.GetComponentsInChildren<ResourceNode>(true);
            var food = nodes.First(n => n.ResourceType == ResourceType.Food);
            var soil = nodes.First(n => n.ResourceType == ResourceType.Soil);
            food.Extract(food.AmountRemaining);
            soil.Extract(soil.AmountRemaining);
            Check(food.IsDepleted && soil.IsDepleted && !food.gameObject.activeSelf, "depleted local stock disabled");
            var homeFood = resources.GetAmount(ResourceType.Food);
            var homeSoil = resources.GetAmount(ResourceType.Soil);
            Check(site.TryResolveConquest(ConquestDisposition.Annexed), "annexation enables production");
            var settlement = site.Settlement;
            settlement.Tick(float.NaN); settlement.Tick(float.PositiveInfinity); settlement.Tick(-1);
            settlement.Tick(59);
            Check(food.IsDepleted && soil.IsDepleted, "invalid time rejected and production waits a full period");
            settlement.Tick(1);
            Check(food.AmountRemaining == 10 && soil.AmountRemaining == 5 && food.CanGather && soil.CanGather,
                "production revives depleted stock as gatherable nodes");
            settlement.Tick(120);
            Check(food.AmountRemaining == 30 && soil.AmountRemaining == 15, "multiple production periods accounted once");
            settlement.Tick(2400);
            Check(food.AmountRemaining == 300 && soil.AmountRemaining == 200, "production respects existing caps");
            Check(resources.GetAmount(ResourceType.Food) == homeFood && resources.GetAmount(ResourceType.Soil) == homeSoil
                && ship.GetCargo(ResourceType.Food) == 0, "production stays local without crew requirement or automatic shipping");
            settlement.enabled = false;
            food.Extract(food.AmountRemaining);
            settlement.Tick(60);
            Check(food.IsDepleted, "disabled settlement does not produce");
            settlement.enabled = true;
            settlement.Tick(60);
            Check(food.AmountRemaining == 10 && food.CanGather, "production resumes after depletion");

            var troops = commander.TroopCount;
            var assigned = AntPool.Instance.Assigned;
            var level = commander.Talents.Level(CommanderActivity.Melee);
            Check(!ship.TryStation(new[] { commander, commander }) && ship.Crew.Count == 1, "duplicate station rejected atomically");
            Move(commander, ship.Position + Vector3.right * 12);
            Check(!ship.TryStation(new[] { commander }), "station requires nearby crew");
            Move(commander, ship.Position + Vector3.right * 3);
            var selection = Object.FindAnyObjectByType<SelectionManager>();
            selection.ClearSelection();
            typeof(SelectionManager).GetMethod("AddToSelection", Flags).Invoke(selection, new object[] { commander.GetComponent<SelectableObject>() });
            var ui = Object.FindAnyObjectByType<AntColony.UI.WorldMapPanel>();
            if (!ui.IsOpen) ui.Toggle();
            Set(ui, "selectedShip", ship); Set(ui, "selectedSite", site);
            ui.PanelRect.Find("Station Selected").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(ship.Crew.Count == 0 && settlement.Garrison.Count == 1 && commander.Garrison == settlement
                && commander.Transport == null && !commander.IsEmbarked, "station UI detaches visible commander from transport");
            Check(commander.TroopCount == troops && AntPool.Instance.Assigned == assigned && commander.Talents.Level(CommanderActivity.Melee) == level,
                "station preserves workforce and progression");
            Check(!commander.TryAssign(1) && commander.ReturnTroops(1) == 0 && !commander.CanStartConstruction,
                "garrison cannot use home workforce or construct home buildings");
            var lab = Object.Instantiate(Template<ResearchLab>());
            Set(lab, "role", commander.Role);
            lab.gameObject.SetActive(true);
            Check(!lab.TryResearchAttack(commander) && !lab.TryResearchArmor(commander), "garrison cannot research remotely");
            commander.CommandMove(site.Landing + Vector3.right * 6);
            var movementState = typeof(SoldierAnt).GetField("state", Flags);
            Check(movementState.GetValue(commander).ToString() == "MovingToTarget", "garrison movement order accepted");
            Check(ship.TryReturn(), "transport returns without stationed crew");
            Check(movementState.GetValue(commander).ToString() == "MovingToTarget", "return preserves garrison movement orders");
            ship.Tick(ship.TravelSeconds);
            Check(settlement.Garrison.Count == 1 && Vector3.Distance(commander.Position, site.Landing) < 10
                && commander.isActiveAndEnabled && !commander.IsEmbarked, "garrison remains present after ship leaves");
            Check(world.ViewSite(site), "annexed battlefield remains accessible without transport");
            commander.CommandGather(food);
            await Task.Delay(100);
            Check(!commander.IsCarrying && food.AmountRemaining == 10, "garrison does not harvest without a docked transport");

            // 실제 기존 자동 교전 확인. 새 침공 스케줄이나 패배 규칙은 만들지 않는다.
            var enemy = Object.Instantiate(Template<EnemyCommander>(), commander.Position + Vector3.right, Quaternion.identity);
            Set(enemy.GetComponent<WildMonster>(), "maxHealth", 1f);
            Set(enemy.GetComponent<WildMonster>(), "attackDamage", 1f);
            enemy.gameObject.SetActive(true);
            Check(await Wait(() => enemy.IsDead, 10), "stationed commander automatically defends using existing combat");
            Object.Destroy(enemy.gameObject);
            Check(!ship.TryBoard(new[] { commander }), "home transport cannot remotely board garrison");
            Check(ship.TryDepart(site), "empty transport can revisit annexed site for pickup");
            ship.Tick(ship.TravelSeconds);
            Move(commander, food.transform.position + Vector3.left);
            commander.CommandGather(food);
            Check(await Wait(() => commander.IsCarrying), "stationed commander gathers local production when ship is docked");
            Check(!ship.TryReturn() && !ship.TryBoard(new[] { commander }), "carried garrison resources block departure and recall");
            Check(await Wait(() => food.IsDepleted && !commander.IsCarrying, 45), "garrison deposits local production into visiting transport");
            Check(ship.GetCargo(ResourceType.Food) == 10 && resources.GetAmount(ResourceType.Food) == homeFood,
                "harvest is cargo until ship returns");
            Move(commander, ship.Position + Vector3.right * 3);
            Check(ship.TryBoard(settlement.Garrison) && settlement.Garrison.Count == 0 && ship.Crew.Count == 1
                && commander.Garrison == null && commander.Transport == ship, "live garrison list can be recalled without iteration corruption");
            Check(ship.TryStation(ship.Crew) && ship.Crew.Count == 0 && settlement.Garrison.Count == 1,
                "live crew list can be stationed without iteration corruption");
            // 병력이 전멸한 장수도 섬에 영구 방치되지 않고 회수할 수 있다.
            commander.TakeDamage(float.MaxValue);
            Check(!commander.HasTroops && ship.TryBoard(settlement.Garrison), "zero-troop garrison can be rescued");
            Check(ship.TryReturn(), "recalled commander can return home");
            ship.Tick(ship.TravelSeconds);
            Check(commander.Garrison == null && commander.Transport == null && !commander.IsAwayFromHome
                && resources.GetAmount(ResourceType.Food) == homeFood + 10, "return delivers cargo and clears all remote memberships");
            ship.Tick(ship.TravelSeconds);
            Check(resources.GetAmount(ResourceType.Food) == homeFood + 10 && commander.TryAssign(1),
                "delivery is once only and returned commander can replenish");
            Canvas.ForceUpdateCanvases();
            var stationButton = (RectTransform)ui.PanelRect.Find("Station Selected");
            var corners = new Vector3[4]; stationButton.GetWorldCorners(corners);
            Check(corners.All(c => ui.PanelRect.rect.Contains(ui.PanelRect.InverseTransformPoint(c))), "station button inside panel");
            Check(Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>().Length == 1
                && ui.GetComponentInParent<Canvas>().GetComponent<UnityEngine.UI.GraphicRaycaster>() != null,
                "UI has event system and raycaster");
            return "PASS: " + checks + " annexed production / garrison / recall / defense checks";
        }
    }
}

