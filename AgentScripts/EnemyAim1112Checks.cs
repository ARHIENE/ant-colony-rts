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
using UnityEngine.AI;
using Object = UnityEngine.Object;

// 2026-11-12 적 목표 우선순위(사용자 확정: 1순위가 없으면 2·3순위로 내려가 계속 공격) 검사.
// 판 1(벽 없음): 유형별 1순위 선택, 표적 파괴 후 차순위, 건물 전멸 후 장수. 판 2(흙벽 고리): 벽 파괴형·길 막힘·일반형의 벽↔장수 전환.
// 실제 새 게임에서 무리를 직접 생성해 확인한다. 결과 Temp/enemyaim-log.txt.
public static class EnemyAim1112Checks
{
    const float Half = 7;
    static string log = ""; static int pass, fail;
    static void Note(string s) { log += s + "\n"; File.WriteAllText("Temp/enemyaim-log.txt", log); }
    static void Check(bool ok, string what) { if (ok) pass++; else fail++; Note((ok ? "PASS " : "FAIL ") + what); }
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, new object[] { k, UnitRole.Worker });
    static BuildingBase Put(BuildingKind k, Vector3 p, float yaw = 0)
    {
        if (!NavMesh.SamplePosition(p, out var g, 3, NavMesh.AllAreas)) return null;
        var go = Object.Instantiate(Template(k), new Vector3(p.x, g.position.y, p.z), Quaternion.Euler(0, yaw, 0)); go.SetActive(true);
        return go.GetComponent<BuildingBase>();
    }
    static readonly FieldInfo TargetField = typeof(WildMonster).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
    static IDamageable Target(WildMonster m) => m == null ? null : (IDamageable)TargetField.GetValue(m);
    static int SavedAim(WildMonster m) => ((WildMonsterDto)typeof(WildMonster).GetMethod("CaptureRuntime", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m, null)).aim;
    static string Name(IDamageable t) => t == null ? "없음" : ((Component)t).name;
    static async Task<bool> Until(Func<bool> done, float seconds)
    {
        var end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) { if (done()) return true; await Task.Delay(100); }
        return done();
    }

    public static async Task<string> Main()
    {
        log = ""; pass = fail = 0; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "EnemyAim-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            Table();
            await Open(false);
            await Ring(false);
            await Ring(true);
            Time.timeScale = 0;
            return $"DONE pass {pass} fail {fail}\n" + log;
        }
        catch (Exception e) { Note("EXCEPTION " + e); return $"ERROR pass {pass} fail {fail}\n" + log; }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }

    static void Table()
    {
        Check(WaveDefense.HordeAim(2, 2) == EnemyAim.General && WaveDefense.HordeAim(2, 5) == EnemyAim.General, "2웨이브까지 특수 유형 없음");
        Check(WaveDefense.HordeAim(3, 2) == EnemyAim.Wall && WaveDefense.HordeAim(3, 5) == EnemyAim.General, "3웨이브부터 벽 파괴형만");
        Check(WaveDefense.HordeAim(4, 5) == EnemyAim.Building && WaveDefense.HordeAim(4, 8) == EnemyAim.General, "4웨이브부터 건물 공격형");
        Check(WaveDefense.HordeAim(5, 8) == EnemyAim.Defense, "5웨이브부터 방어시설 파괴형");
        Check(Enumerable.Range(0, 40).Count(i => WaveDefense.HordeAim(9, i) != EnemyAim.General) == 12, "무리 40개 중 특수 12개(30%)");
    }

    static Vector3 home; static LocalIncursions local; static CommanderAnt[] team;
    static BuildingBase hut, store, tower; static readonly List<BuildingBase> walls = new List<BuildingBase>();
    static async Task Setup(bool ring)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261112, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
        while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 1; await Task.Delay(500);
        home = (Vector3)typeof(ResourceManager).GetProperty("HomePosition", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        // 시작 잔해 등 기존 건물은 치워 표적 후보를 검사용 건물로만 둔다.
        foreach (var b in SceneObjects.All<BuildingBase>().Where(b => b != null && b.CountsTowardPlayerDefeat && !b.IsDead).ToArray()) Object.Destroy(b.gameObject);
        await Task.Delay(200);
        hut = Put(BuildingKind.Hut, home + new Vector3(2.5f, 0, 0)); store = Put(BuildingKind.Storage, home + new Vector3(-2.5f, 0, 0));
        tower = Put(BuildingKind.AcidTower, home + new Vector3(0, 0, 3.5f));
        walls.Clear();
        if (ring) for (var i = -Half + 1; i < Half; i += 2)
        {
            walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(i, 0, Half))); walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(i, 0, -Half)));
            walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(Half, 0, i), 90)); walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(-Half, 0, i), 90));
        }
        walls.RemoveAll(w => w == null);
        team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead && c.IsColonyMember).ToArray();
        for (var i = 0; i < team.Length; i++) { var c = team[i]; c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); c.Agent.Warp(home + new Vector3(i - 1, 0, -1)); }
        local = SceneObjects.Any<LocalIncursions>();
        await Task.Delay(800); // 벽 장애물이 NavMesh에 반영될 시간
        Time.timeScale = 4;
    }
    static WildMonster Horde(EnemyAim aim)
    {
        var before = local.Visitors.ToArray();
        for (var i = 0; i < 20 && !local.TrySpawnHorde(400, aim); i++) { }
        return local.Visitors.FirstOrDefault(v => !before.Contains(v));
    }
    static void Near(CommanderAnt c, Vector3 p) { if (NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas)) c.Agent.Warp(hit.position); }
    static void Kill(BuildingBase b) { if (b != null && !b.IsDead) b.TakeDamage(1e9f); }

    static async Task Open(bool ring)
    {
        await Setup(ring);
        Check(hut != null && store != null && tower != null, "판 1 준비: 초가집·창고·산성탑 배치");
        var building = Horde(EnemyAim.Building); var defense = Horde(EnemyAim.Defense); var wall = Horde(EnemyAim.Wall); var general = Horde(EnemyAim.General);
        Check(building != null && defense != null && wall != null && general != null, "판 1 준비: 유형별 무리 4개 생성");
        if (building == null || defense == null || wall == null || general == null) return;
        Check(building.name.StartsWith("건물 공격") && defense.name.StartsWith("방어시설 파괴") && wall.name.StartsWith("벽 파괴") && general.name.StartsWith("침입"), "무리 이름에 유형 표시");
        Check(SavedAim(defense) == (int)EnemyAim.Defense && SavedAim(general) == 0, "저장 기록에 유형 포함(일반형 0)");

        await Until(() => Target(defense) == (IDamageable)tower, 6);
        Check(Target(defense) == (IDamageable)tower, $"방어시설 파괴형 1순위 = 산성탑 (실제 {Name(Target(defense))})");
        await Until(() => Target(building) is BuildingBase, 6);
        Check(Target(building) is BuildingBase b1 && !WildMonster.IsBarrier(b1), $"건물 공격형 1순위 = 건물 (실제 {Name(Target(building))})");
        await Until(() => Target(wall) is BuildingBase, 6);
        Check(Target(wall) is BuildingBase b2 && !WildMonster.IsBarrier(b2), $"벽 파괴형: 막는 벽이 없으면 차순위 건물 (실제 {Name(Target(wall))})");
        await Until(() => Target(general) != null, 6);
        Check(Target(general) is BuildingBase, $"일반 공격형: 탐지 범위에 장수가 없으면 건물 (실제 {Name(Target(general))})");

        // 장수가 옆에 와도 구조물 우선형은 표적을 바꾸지 않고, 일반형은 장수로 바꾼다.
        team[0].Agent.Warp(building.Position + (home - building.Position).normalized * 2); team[1].Agent.Warp(general.Position + (home - general.Position).normalized * 2);
        await Until(() => !(Target(general) is BuildingBase), 6);
        Check(Target(general) == (IDamageable)team[1], $"일반 공격형: 장수가 탐지되면 장수로 전환 (실제 {Name(Target(general))})");
        Check(Target(building) is BuildingBase, $"건물 공격형: 옆에 장수가 있어도 건물 유지 (실제 {Name(Target(building))})");
        // 카이팅으로 장수가 멀어지면 일반형은 다시 구조물로 내려간다.
        // 무리끼리 가까이 생성될 수 있으므로 다른 장수도 탐지 범위 밖으로 뺀다.
        foreach (var c in team) Near(c, home + new Vector3(30, 0, 30));
        await Until(() => Target(general) is BuildingBase, 8);
        Check(Target(general) is BuildingBase, $"일반 공격형: 장수가 벗어나면 구조물로 복귀 (실제 {Name(Target(general))})");

        Kill(tower);
        await Until(() => Target(defense) is BuildingBase t && !t.IsDead && t != tower, 6);
        Check(Target(defense) is BuildingBase b3 && !b3.IsDead && b3 != tower, $"방어시설 파괴형: 탑이 없어지면 차순위 건물 (실제 {Name(Target(defense))})");

        Kill(hut); Kill(store);
        await Task.Delay(1500);
        // 표적이 없는 무리는 본거지로 계속 걸어가므로 장수를 무리 옆에 계속 붙여 둔다.
        await Until(() => { Near(team[2], building.Position + Vector3.right * 2); return Target(building) == (IDamageable)team[2]; }, 8);
        Check(Target(building) == (IDamageable)team[2], $"건물 공격형: 건물이 전부 없어지면 장수 (실제 {Name(Target(building))})");
        Check(!building.IsDead && !defense.IsDead && building.Agent.enabled, "표적이 없어도 무리가 멈춰 사라지지 않음");
        Time.timeScale = 0; await Task.Delay(200);
    }

    static async Task Ring(bool wallOnly)
    {
        await Setup(true);
        Check(walls.Count >= 24, $"판 2 준비: 흙벽 고리 {walls.Count}개");
        var probe = new NavMeshPath();
        var outside = home + new Vector3(Half + 6, 0, 0); NavMesh.SamplePosition(outside, out var o, 4, NavMesh.AllAreas);
        NavMesh.CalculatePath(o.position, home, NavMesh.AllAreas, probe);
        Check(probe.status != NavMeshPathStatus.PathComplete, $"판 2 준비: 고리가 길을 막음 ({probe.status})");
        if (probe.status == NavMeshPathStatus.PathComplete) return;

        // 벽 파괴형이 낸 구멍으로 다른 무리가 들어가면 길 막힘 검사가 성립하지 않으므로 판을 나눈다.
        if (wallOnly)
        {
            var wall = Horde(EnemyAim.Wall);
            Check(wall != null, "판 3 준비: 벽 파괴 무리 생성");
            if (wall == null) return;
            await Until(() => WildMonster.IsBarrier(Target(wall)), 8);
            Check(WildMonster.IsBarrier(Target(wall)), $"벽 파괴형 1순위 = 길을 막는 벽 (실제 {Name(Target(wall))})");
            var first = Target(wall);
            await Task.Delay(2000);
            Check(Target(wall) == first || first == null || first.IsDead, "벽 파괴형: 같은 벽을 계속 노림(표적 흔들림 없음)");
            // 벽이 뚫리면 벽 파괴형도 안쪽 건물로 넘어간다.
            foreach (var w in walls) Kill(w);
            await Until(() => Target(wall) is BuildingBase t && !t.IsDead && !WildMonster.IsBarrier(t), 10);
            Check(Target(wall) is BuildingBase t2 && !t2.IsDead && !WildMonster.IsBarrier(t2), $"벽 파괴형: 벽이 없어지면 차순위 건물 (실제 {Name(Target(wall))})");
            Time.timeScale = 0; await Task.Delay(200);
            return;
        }
        var building = Horde(EnemyAim.Building); var general = Horde(EnemyAim.General);
        Check(building != null && general != null, "판 2 준비: 무리 2개 생성");
        if (building == null || general == null) return;

        // 안쪽 건물이 1순위지만 길이 막혀 있으므로 벽 앞에 도착하면 막는 벽을 부순다.
        // 두 무리를 함께 지켜본다. 한쪽이 벽을 뚫으면 다른 쪽 검사가 성립하지 않으므로 벽 앞에 처음 붙은 순간을 기록한다.
        bool buildingAtWall = false, generalAtWall = false; IDamageable held = null; var heldAt = 0f; var steady = false;
        await Until(() =>
        {
            buildingAtWall |= WildMonster.IsBarrier(Target(building)) && Vector3.Distance(building.Position, home) < Half + 6;
            if (!generalAtWall && WildMonster.IsBarrier(Target(general)) && Vector3.Distance(general.Position, home) < Half + 4)
            { generalAtWall = true; held = Target(general); heldAt = Time.realtimeSinceStartup; }
            if (generalAtWall && !steady && Time.realtimeSinceStartup - heldAt > 1.5f) steady = Target(general) == held || held.IsDead;
            return buildingAtWall && generalAtWall && Time.realtimeSinceStartup - heldAt > 1.6f;
        }, 60);
        Check(buildingAtWall, $"건물 공격형: 길이 막히면 막는 벽으로 (실제 {Name(Target(building))}, 거리 {Vector3.Distance(building.Position, home):0}m)");
        Check(generalAtWall, $"일반 공격형: 벽 안 장수에게 닿을 수 없으면 벽 (실제 {Name(Target(general))}, 거리 {Vector3.Distance(general.Position, home):0}m)");
        Check(steady, "일반 공격형: 벽 너머 장수 때문에 표적이 오락가락하지 않음");

        // 장수가 벽 밖으로 나와 닿을 수 있게 되면 벽을 치다가도 장수로 돌아간다.
        var away = (general.Position - home); away.y = 0;
        if (NavMesh.SamplePosition(general.Position + away.normalized * 2.5f, out var spot, 3, NavMesh.AllAreas)) team[0].Agent.Warp(spot.position);
        await Until(() => Target(general) == (IDamageable)team[0], 8);
        Check(Target(general) == (IDamageable)team[0], $"일반 공격형: 벽을 치다가 닿을 수 있는 장수가 오면 장수로 (실제 {Name(Target(general))})");
        Time.timeScale = 0; await Task.Delay(200);
    }
}
