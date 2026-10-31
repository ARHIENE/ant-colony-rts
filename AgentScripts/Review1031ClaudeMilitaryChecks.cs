using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Buildings;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;
// 2026-10-31 Claude: M2 실행 잔여 Play 검사 — AI 시대 편성의 실제 저장→전체 복원 유지(리뷰 수정), 집 파괴 시 주민 주소 무효화.
public static class Review1031ClaudeMilitaryChecks
{
    static int count;
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Review1031Military " + count + ": " + label); count++; }
    static async Task Wait(Func<bool> condition) { var end = DateTime.UtcNow.AddSeconds(100); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); Check(condition(), "wait completed"); }
    static async Task Drain(IEnumerator routine) { while (routine.MoveNext()) if (routine.Current is IEnumerator nested) await Drain(nested); else await Task.Yield(); }
    static float F(WildMonster m, string name) => (float)typeof(WildMonster).GetField(name, Any).GetValue(m);
    static WildMonster[] Guards(ExpeditionSite site) => site.GetComponentsInChildren<WildMonster>(true)
        .Where(g => !g.IsDead && !g.Allied && g.GuardBoss < 0).OrderBy(g => g.name).ToArray();

    public static Task<string> Main() => Run(true);
    public static Task<string> NoWait() => Run(false);
    static async Task<string> Run(bool frameWait)
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; var root = SaveStorage.RootOverride; var settings = UserSettings.Current.Clone();
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Review1031Military-" + Guid.NewGuid().ToString("N"));
        var notes = "";
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 311033, mapSize = MapSize.Small, startingCount = 2 }); await Wait(() => !SaveSystem.Busy);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            if (frameWait) await Task.Delay(1000); // 새 게임 직후 지연 Destroy가 끝난 뒤 저장한다.
            var d = DiplomacyManager.Instance; var world = WorldMapManager.Instance;

            // AI 시대 편성: 4시대로 승급 → 실제 Capture→JSON→Restore 후에도 사거리/체력 상한·저장 체력 유지.
            var site = world.Sites.FirstOrDefault(s => d.Faction(s) != null && Guards(s).Length >= 3);
            if (site == null) notes += " 세력 수비대3+ 현장 없음(편성 검사 생략)";
            else
            {
                var faction = d.Faction(site); faction.era = 4; faction.eraWork = 0; faction.eraPaid = false;
                AIEraMilitary.UpgradeGuards(site, faction);
                var guards = Guards(site);
                var expected = guards.Select(g => (F(g, "attackRange"), F(g, "maxHealth"))).ToArray();
                Check(expected.Any(e => Math.Abs(e.Item1 - 6) < .01f) && expected.Any(e => Math.Abs(e.Item2 - 180) < .01f), "4시대 산 분사기·방패 편성 적용");
                var hurt = guards[0]; typeof(WildMonster).GetField("currentHealth", Any).SetValue(hurt, 37f);
                var restore = typeof(SaveSnapshot).GetMethod("Restore", BindingFlags.NonPublic | BindingFlags.Static);
                Check(SaveValidator.TryParse(JsonUtility.ToJson(SaveSnapshot.Capture()), out var parsed, out var error), "JSON " + error);
                await Drain((IEnumerator)restore.Invoke(null, new object[] { parsed }));
                site = WorldMapManager.Instance.Sites[world.Sites.ToList().IndexOf(site)];
                faction = DiplomacyManager.Instance.Faction(site);
                Check(faction != null && faction.era == 4, "세력 시대 복원");
                var after = Guards(site);
                Check(after.Length == guards.Length, "수비대 수 유지");
                Check(after.Select(g => (F(g, "attackRange"), F(g, "maxHealth"))).SequenceEqual(expected), "복원 후 시대 편성 유지(1시대로 되돌아가지 않음)");
                Check(after.Any(g => Math.Abs(g.CurrentHealth - 37f) < .01f), "저장 체력 그대로(회복·절삭 없음)");
            }

            // 집 파괴: 그 좌표 주민 그룹의 주소가 끊겨 같은 자리 신축으로 승계되지 않는다.
            var house = SceneObjects.All<Housing>().FirstOrDefault(h => h != null && !h.IsDead);
            if (house == null)
            {
                // 새 게임엔 집이 없어 저장 복원과 같은 템플릿 경로로 오두막 1동을 세운다.
                var template = (GameObject)typeof(BuildingPlacementController).GetMethod("GetTemplate", Any)
                    .Invoke(null, new object[] { AntColony.Data.BuildingKind.Hut, AntColony.Data.UnitRole.Worker });
                var go = UnityEngine.Object.Instantiate(template, world.HomePosition + new Vector3(6, 0, 6), Quaternion.identity); go.SetActive(true);
                house = go.GetComponent<Housing>();
            }
            if (house == null) notes += " 집 없음(주소 검사 생략)";
            else
            {
                var pop = ColonyPopulation.Instance;
                var group = new CivilianLocation { id = 999001, count = 3, position = house.Position, housing = true, sheltered = true, arrived = true };
                pop.S.civilianLocations.Add(group);
                house.TakeDamage(float.MaxValue);
                Check(!group.housing && !group.sheltered, "집 파괴 시 주민 주소 무효화");
                pop.S.civilianLocations.Remove(group);
            }
            return "Review1031Military passed " + count + notes;
        }
        finally
        {
            UserSettings.Apply(settings, false); SaveStorage.RootOverride = root;
        }
    }
}
