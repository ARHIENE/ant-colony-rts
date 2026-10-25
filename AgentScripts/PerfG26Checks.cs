using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Buildings;
using AntColony.Core;
using AntColony.Data;
using AntColony.Map;
using AntColony.Save;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

// G26 성능 기준(2026-10-24): 시민 3만·주거 300동·장수 30·활성 적 200·바닥 묶음 1만에서
// 1배속 30fps, 저장 5초·로드 10초. 측정만 하고 기준 미달도 결과 문자열에 남긴다(실패 예외는 준비 실패에만).
public static class PerfG26Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    const int Citizens = 30000, Homes = 300, Commanders = 30, Enemies = 200, Stacks = 10000;
    const float TargetFps = 30, SaveLimit = 5, LoadLimit = 10;

    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(120);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        if (SaveSystem.Busy) throw new Exception("G26 FAIL: scene not ready");
    }
    static Vector3 Ground(Vector3 p) => NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas) ? hit.position : p;

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("G26 FAIL: Play mode required");
        var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "G26-" + Guid.NewGuid().ToString("N"));
        var settings = UserSettings.Current.Clone(); var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 261026, mapSize = MapSize.Large }); await Ready();
            Time.timeScale = 0;
            var bounds = HomeMapBuilder.CurrentWorldBounds; var home = (Vector3)typeof(ResourceManager).GetProperty("HomePosition", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var size = Mathf.Min(bounds.extents.x, bounds.extents.z) * .85f;

            // 시민: 성체는 AntPool 수, 어린·늙은 시민은 숫자 집계(10/80/10%).
            var pop = ColonyPopulation.Instance; pop.S.young += Citizens / 10; pop.S.old += Citizens / 10;
            AntPool.Instance.Breed(Citizens - Citizens / 5);

            // 주거 300동: 본거지 주변 격자.
            var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Hidden)
                .Invoke(null, new object[] { BuildingKind.Hut, UnitRole.Worker });
            var side = Mathf.CeilToInt(Mathf.Sqrt(Homes));
            for (var i = 0; i < Homes; i++)
            {
                var p = home + new Vector3((i % side - side / 2) * 6f, 0, (i / side - side / 2) * 6f + 40);
                Object.Instantiate(template, Ground(p), Quaternion.identity).SetActive(true);
            }

            // 장수 30명.
            var roster = CommanderRoster.Instance;
            for (var i = 0; i < 60 && roster.Commanders.Count < Commanders; i++)
                roster.Create(null, AntColony.Data.CommanderRank.Corporal, new[] { UnitRole.Worker }, UnitRole.Worker, AntColony.Units.CommanderTraits.Random(),
                    Ground(home + new Vector3(i % 6 * 2f, 0, i / 6 * 2f - 10)));

            // 활성 적 200: 맵 가장자리 쪽에 흩어 둔다(본거지와 거리 유지).
            var incursions = Object.FindAnyObjectByType<LocalIncursions>();
            var spawn = typeof(LocalIncursions).GetMethod("RestoreSpawn", Hidden);
            for (var i = 0; i < Enemies; i++)
            {
                var a = i * Mathf.PI * 2 / Enemies;
                ((WildMonster)spawn.Invoke(incursions, new object[] { false, Ground(bounds.center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * size) })).gameObject.SetActive(true); // 템플릿이 비활성이라 켜 준다
            }

            // 바닥 묶음 1만: 2m 안 같은 종류는 합쳐지므로 2.2m 격자에 둔다.
            var drop = typeof(DiplomacyManager).GetMethod("DropFloor", Hidden, null, new[] { typeof(AntColony.Data.ResourceType), typeof(int), typeof(Vector3) }, null);
            var grid = Mathf.CeilToInt(Mathf.Sqrt(Stacks));
            for (var i = 0; i < Stacks; i++)
                drop.Invoke(null, new object[] { AntColony.Data.ResourceType.Wood, 1, bounds.center + new Vector3((i % grid - grid / 2) * 2.2f, 0, (i / grid - grid / 2) * 2.2f) });

            // 식량 0이면 측정 중 굶주림 탈주로 장수가 모두 떠난다(이전 측정의 장수 0/30).
            ResourceManager.Instance.AddCapacity(AntColony.Data.ResourceType.Food, 5000); ResourceManager.Instance.Add(AntColony.Data.ResourceType.Food, 5000);
            var regions = WorldMapManager.Instance != null ? WorldMapManager.Instance.Sites.Count : 0;
            var stacks = ResourceNode.Available.Count(n => n != null && n.IsLooseCargo);
            var enemies = Object.FindObjectsByType<WildMonster>(FindObjectsSortMode.None).Count(m => m.isActiveAndEnabled);
            var commanders = roster.Commanders.Count(c => !c.IsDead && c.IsColonyMember);

            // 1배속 프레임 측정: 안정화 60프레임 뒤 300프레임.
            AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 1;
            for (var i = 0; i < 60; i++) await Task.Yield();
            var frames = new float[300];
            for (var i = 0; i < frames.Length; i++) { await Task.Yield(); frames[i] = Time.unscaledDeltaTime; }
            Time.timeScale = 0;
            var avgFps = frames.Length / frames.Sum();
            var low = 1f / frames.OrderByDescending(d => d).Take(frames.Length / 100).Average(); // 1% 저점

            var watch = Stopwatch.StartNew();
            if (!SaveSystem.TrySave(false, 0, out var error)) throw new Exception("G26 FAIL: save " + error);
            var saveSeconds = watch.Elapsed.TotalSeconds;
            watch.Restart();
            if (!SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error)) throw new Exception("G26 FAIL: load " + error);
            await Ready();
            var loadSeconds = watch.Elapsed.TotalSeconds;

            string Mark(bool ok) => ok ? "OK" : "미달";
            return $"G26 citizens {pop.Total} homes {Homes} commanders {commanders} (측정 후 {roster.Commanders.Count}) enemies {enemies} stacks {stacks} regions {regions} | "
                + $"fps avg {avgFps:0.0} low1% {low:0.0} {Mark(avgFps >= TargetFps)} | save {saveSeconds:0.00}s {Mark(saveSeconds <= SaveLimit)} | load {loadSeconds:0.00}s {Mark(loadSeconds <= LoadLimit)}";
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
