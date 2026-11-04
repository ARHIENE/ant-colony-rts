using System;
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
using UnityEngine.UI;
using Object = UnityEngine.Object;

// 정찰 동행(2026-10-03): HUD에서 초소 선택 → 파견 창에서 장수 선택 → 장수가 소굴을 비움 → 저장·불러오기 유지 → 귀환.
public static class ScoutChecks
{
    static int checks;
    static void Check(bool value, string label) { if (!value) throw new Exception("FAIL: " + label); checks++; }
    static RectTransform Rect(string name) => Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include).FirstOrDefault(r => r.name == name);
    static async Task Frames() { var frame = Time.frameCount; for (int i = 0; i < 200 && Time.frameCount < frame + 4; i++) await Task.Delay(20); }
    static async Task Ready() { for (int i = 0; i < 1800 && SaveSystem.Busy; i++) await Task.Delay(50); Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0; }

    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "play mode");
        var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Scout-" + Guid.NewGuid().ToString("N"));
        try
        {
            var isolated = settings.Clone(); isolated.autoSaveEnabled = false; UserSettings.Apply(isolated, false);
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261003, mapSize = MapSize.Small }); await Ready();
            GameMenuController.Instance.Resume(); Time.timeScale = 0; await Frames();
            ResourceManager.Instance.Add(AntColony.Data.ResourceType.Food, 500);
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { BuildingKind.ScoutPost, UnitRole.Worker });
            var go = Object.Instantiate(template, WorldMapManager.Instance.HomePosition + Vector3.right * 10, Quaternion.identity); go.name = "ScoutPost"; go.SetActive(true);
            var post = go.GetComponent<ScoutPost>(); var building = go.GetComponent<BuildingBase>();
            Check(post != null && building != null, "scout post built");

            // HUD: 초소를 고르면 정보 칸에 상태, 명령 칸 첫 버튼이 '정찰 파견'.
            WorkTargetPanel.Select(building); await Frames();
            Check(Rect("TargetCommands").gameObject.activeSelf && WorkTargetPanel.Scout == post, "scout post selected in HUD");
            var first = Rect("Target Residents").GetComponent<Button>();
            Check(first.GetComponentInChildren<Text>().text == "정찰 파견" && first.interactable, "command card offers dispatch");
            Check(Rect("TargetAction").GetComponentInChildren<Text>().text == "정찰 파견", "target panel offers dispatch");

            // 파견 창: 장수 목록에서 1명 고르고 파견. 고르기 전에는 파견 불가.
            first.onClick.Invoke(); await Frames();
            Check(Rect("ScoutDispatch") != null && Rect("ScoutDispatch").gameObject.activeInHierarchy, "dispatch panel opens");
            var go2 = Rect("Scout Dispatch").GetComponent<Button>();
            Check(!go2.interactable, "dispatch needs a companion");
            var mate = CommanderRoster.Instance.Commanders.First(c => c.CanScout);
            Rect("Pick " + mate.CommanderName).GetComponent<Toggle>().isOn = true; await Frames();
            Check(go2.interactable, "companion picked enables dispatch");
            var free = AntPool.Instance.Free;
            go2.onClick.Invoke(); await Frames();
            Check(post.IsDispatched && post.Companion == mate && mate.IsEmbarked && mate.IsAwayFromHome && !mate.CanScout, "companion leaves home");
            Check(AntPool.Instance.Free == free - post.DispatchAnts, "scout ants assigned");
            Check(!HudOverview.HomeCommanders.Contains(mate), "away companion excluded from home roster");
            WorkTargetPanel.Select(building); await Frames();
            Check(first.GetComponentInChildren<Text>().text == "정찰 중" && !first.interactable, "HUD shows scouting");
            Check(!post.TryDispatch(CommanderRoster.Instance.Commanders.First(c => c.CanScout)), "no double dispatch");

            // 저장·불러오기: 동행 장수가 계속 나가 있다.
            post.Tick(5f);
            Check(SaveSystem.TrySave(false, 1, out var error), "save: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 1), out error), "load: " + error); await Ready();
            post = Object.FindObjectsByType<ScoutPost>().Single(p => p.isActiveAndEnabled);
            mate = CommanderRoster.Instance.Commanders.Single(c => c.CommanderName == mate.CommanderName);
            Check(post.IsDispatched && post.Companion == mate && mate.IsEmbarked && mate.ScoutMission == post, "companion survives reload");

            // 귀환: 장수가 초소 옆에 다시 나타난다.
            post.Tick(1000f);
            Check(!post.IsDispatched && post.Companion == null && !mate.IsEmbarked && !mate.IsAwayFromHome, "companion returns");

            // 파견 중 초소가 꺼지면 장수와 개미를 돌려받는다.
            Check(post.TryDispatch(mate), "redispatch");
            free = AntPool.Instance.Free; post.gameObject.SetActive(false);
            Check(!mate.IsEmbarked && !mate.IsAwayFromHome && AntPool.Instance.Free == free + post.DispatchAnts, "destroyed post returns companion");
            return "PASS " + checks + " scout checks";
        }
        finally
        {
            GameMenuController.Instance?.Resume(); Time.timeScale = 0;
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
