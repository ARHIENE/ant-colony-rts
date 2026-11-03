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

// 새 Play 세션 전용. 실제 저장 경로·설정을 보존하고 캡처 없이 실행한다.
public static class ExpeditionPause1103Checks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); checks++; }
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static void Set(object target, string name, object value) => target.GetType().GetProperty(name, Flags).SetValue(target, value);
    static void Pause(bool value) => typeof(ExpeditionRules).GetProperty("Paused").GetSetMethod(true).Invoke(null, new object[] { value });
    static void Restore(ExpeditionTransport ship, ExpeditionState state, ExpeditionSite site, Vector3 home)
        => Call(ship, "RestoreState", state, .01f, site, state == ExpeditionState.Deployed ? site.Landing : home, home, 0, 0, 0);
    static async Task Wait(Func<bool> ready)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (!ready() && DateTime.UtcNow < deadline) await Task.Delay(50);
        Check(ready(), "new game finishes within timeout");
    }

    public static async Task<string> Main()
    {
        checks = 0;
        Check(Application.isPlaying, "Play mode required");
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var paused = ExpeditionRules.Paused; var time = Time.timeScale;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "ExpeditionPause-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 261103, mapSize = MapSize.Small, startingCount = 3,
                startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
            await Wait(() => !SaveSystem.Busy && WorldMapManager.Instance != null && CommanderRoster.Instance?.Count == 3);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var world = WorldMapManager.Instance;
            var commanders = CommanderRoster.Instance.Commanders.ToList();
            var site = world.Sites.First(s => WorldMapManager.SiteRevealed(s) && world.Assessment.Targets(s).Count > 0);
            Check(paused, "production switch defaults to paused");
            Check(ExpeditionRules.PauseReason == "원정은 추가 기획 전까지 잠시 닫혀 있습니다", "exact reason");
            Pause(true);
            Check(world.CreateWalkingParty(new[] { commanders[0] }) == null, "walking creation blocked");
            foreach (var mode in new[] { 0, 1, 2 })
            {
                Check(world.CanCreateTransport(world.HomePosition, out var home), "transport home position");
                var ship = world.CreateTransport(mode == 2, home, mode == 0);
                Call(ship, "RestoreCrew", new List<CommanderAnt> { commanders[0] });
                Check(ship.DepartureBlock(null) == ExpeditionRules.PauseReason, "pause reason takes priority " + mode);
                Check(!ship.TryDepart(site) && ship.State == ExpeditionState.Home && ship.Site == null, "departure blocked without mutation " + mode);
                var random = JsonUtility.ToJson(UnityEngine.Random.state);
                Check(!world.Assessment.TryDispatch(ship, site, commanders[0], null, null, true)
                    && ship.Investigation == null && random == JsonUtility.ToJson(UnityEngine.Random.state), "investigation blocked before random/mission changes");
                Pause(false);
                Check(ship.DepartureBlock(site) == "", "original departure conditions restored " + mode + ": " + ship.DepartureBlock(site));
                Check(ship.TryDepart(site) && ship.State == ExpeditionState.Outbound, "original departure restored " + mode);
                Pause(true);
                // 저장 복원과 같은 내부 API. 이미 출발한 저장은 도착·철수·귀환을 유지한다.
                Restore(ship, ExpeditionState.Outbound, site, home);
                ship.Tick(.1f);
                Check(ship.State == ExpeditionState.Deployed && ship.TryReturn(), "restored outbound arrives and withdraws while paused " + mode);
                ship.CancelWithdrawal();
                Restore(ship, ExpeditionState.Returning, site, home);
                Call(ship, "RestoreCrew", new List<CommanderAnt> { commanders[0] });
                ship.Tick(.1f);
                Check(ship.State == ExpeditionState.Home, "restored return arrives while paused " + mode);
                Call(ship, "DetachCommander", commanders[0]);
                Call(commanders[0], "SetEmbarked", false, home);
                Call(ship, "RestoreCrew", new List<CommanderAnt>());
                Object.DestroyImmediate(ship.gameObject);
            }
            Check(world.CanCreateTransport(world.HomePosition, out var point), "walking fixture position");
            Pause(false);
            var walking = world.CreateWalkingParty(new[] { commanders[0] });
            Check(walking != null, "walking creation restored");
            // 편성의 물리 탑승 대기 대신 저장 복원 API로 탑승 완료 상태를 만든다.
            ExpeditionTransport.CancelCrewOrdersFor(commanders[0]);
            Call(walking, "RestoreCrew", new List<CommanderAnt> { commanders[0] });
            Pause(true);
            Check(!walking.TryInvestigate(site, commanders[0], allowInsufficientFood: true), "investigation wrapper blocked");
            Pause(false);
            Check(walking.TryInvestigate(site, commanders[0], allowInsufficientFood: true), "original valid investigation restored");
            world.Assessment.CancelInvestigation(walking);
            Set(walking, "Investigation", null);
            Restore(walking, ExpeditionState.Deployed, site, point);
            Pause(true);
            Check(walking.TryReturn(), "normal withdrawal accepted while paused");
            walking.CancelWithdrawal();
            var next = world.Sites.First(s => s != site && s.Kind == ExpeditionSiteKind.Settlement);
            Set(next, "Disposition", ConquestDisposition.Annexed);
            Set(next, "Settlement", next.gameObject.AddComponent<AnnexedSettlement>());
            Check(!(bool)Call(walking, "TryContinueTo", next), "new inter-site departure blocked");
            Pause(false);
            Check((bool)Call(walking, "TryContinueTo", next), "inter-site departure restored");
            Pause(true);
            // 구저장에서 이미 예약된 경유는 다음 거점으로 가지 않고 본거지로 귀환한다.
            Call(walking, "StartWithdrawTravel", next);
            Check(walking.State == ExpeditionState.Returning && walking.Site == site, "restored pending leg falls back to home return");
            walking.CancelWithdrawal();
            Restore(walking, ExpeditionState.Home, null, point);
            Call(walking, "RestoreCrew", new List<CommanderAnt> { commanders[0] });
            Call(commanders[0], "RestoreTroops", 1, 0f);
            Check(!walking.Route.TryStart(next) && walking.Route.Status == ExpeditionRules.PauseReason, "automatic route start blocked with reason");
            Pause(false);
            Check(walking.Route.TryStart(next), "automatic route start restored");
            Pause(true); walking.Route.Tick(1);
            Check(walking.State == ExpeditionState.Home && walking.Route.Status == ExpeditionRules.PauseReason, "restored automatic route cannot depart again");
            walking.Route.Stop();
            Call(commanders[0], "RestoreTroops", 0, 0f);

            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { BuildingKind.AirshipYard, UnitRole.Worker });
            var yard = Object.Instantiate(template, point + Vector3.left * 12, Quaternion.identity).GetComponent<AirshipYard>();
            yard.gameObject.SetActive(true);
            // 발사 확정의 기존 조건만 갖춘 격리 fixture. 실제 발사/승리 처리는 실행하지 않는다.
            var rocket = (AirshipYard.State)typeof(AirshipYard).GetField("state", Flags).GetValue(yard);
            rocket.hull = rocket.engine = true; rocket.phase = RocketPhase.Boarding;
            ((List<CommanderAnt>)typeof(AirshipYard).GetField("passengers", Flags).GetValue(yard)).Add(commanders[1]);
            Call(commanders[1], "SetEmbarked", true, yard.Position);
            // 로켓 발사는 원정이 아니다(2026-11-03 Claude 정정): 원정 중지 중에도 발사 확정은 그대로 된다.
            Check(yard.TryDepart() && rocket.launchRequested, "rocket launch unaffected by expedition pause");
            rocket.launchRequested = false;
            GoalChain.Refresh(Time.unscaledTime);
            Check(GoalChain.Reward(3) == ExpeditionRules.PauseReason && !CampaignNextSteps.ParallelGoal(2).Contains("원정"), "recommendations suppressed");
            return "PASS " + checks + " · 출발 차단/사유/해제/귀환 검사 (Unity 실행 시 결과)";
        }
        finally
        {
            Pause(paused); Time.timeScale = time;
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
