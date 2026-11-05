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

// 공성 회귀 검사: 직접 배치한 방어시설로 기존 벽 파괴·웨이브 종료가 유지되는지 확인한다.
// 기준: 1웨이브=장수만으로 막음 / 2~3웨이브=방어시설 없으면 못 막거나 큰 피해 / 이후=준비 따라 피해 차등.
// 매 판 같은 시드로 새 게임 → 본거지에 초가집·창고 → (있음) 흙벽 고리+산성탑 → 웨이브 시작 → 플레이어처럼 장수에게 가까운 적 공격 지시.
// 결과 Temp/fun-priority-probe.txt. RUNS 의 마릿수(0=현재 공식)로 곡선 후보를 시험한다.
public static class FunPriority1105Probe
{
    // (웨이브, 마릿수 덮어쓰기 0=공식, 장수당 병력, 산성탑 수)
    static readonly (int wave, int count, int troops, int towers)[] RUNS = { (2, 80, 0, 2), (8, 242, 4, 4) };
    const float Limit = 240, Half = 7; // 제한 시간(초)·흙벽 고리 반폭(m)

    static string log = "";
    static void Note(string s) { log += s + "\n"; File.WriteAllText("Temp/fun-priority-probe.txt", log); }
    static readonly List<GameObject> made = new List<GameObject>();
    static GameObject Template(BuildingKind k) => (GameObject)typeof(BuildingPlacementController)
        .GetMethod("GetTemplate", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public).Invoke(null, new object[] { k, UnitRole.Worker });
    static BuildingBase Put(BuildingKind k, Vector3 p, float yaw = 0)
    {
        if (!NavMesh.SamplePosition(p, out var g, 3, NavMesh.AllAreas)) return null;
        var go = Object.Instantiate(Template(k), new Vector3(p.x, g.position.y, p.z), Quaternion.Euler(0, yaw, 0)); go.SetActive(true); made.Add(go);
        return go.GetComponent<BuildingBase>();
    }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run in Play");
        log = ""; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WaveBal-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            foreach (var r in RUNS) foreach (var (towers, ring) in new[] { (r.towers, true) }) await Run(r.wave, r.count, r.troops, towers, ring);
            Time.timeScale = 0;
            return "DONE\n" + log;
        }
        finally { Time.timeScale = 0; UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }

    static bool Down(CommanderAnt c) => c == null || c.IsDead || c.Health.Downed || c.PersonalHealth <= 0;
    static bool Lost => GameMenuController.Instance != null && GameMenuController.Instance.ScreenName == "COLONY LOST";
    static async Task Run(int wave, int count, int troops, int towers, bool ring)
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        made.Clear();
        SaveSystem.NewGame(new NewGameOptions { seed = 261103, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
        while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 4; await Task.Delay(500);
        var home = (Vector3)typeof(ResourceManager).GetProperty("HomePosition", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null); var defended = towers >= 0;
        var homes = new[] { Put(BuildingKind.Hut, home + new Vector3(2.5f, 0, 0)), Put(BuildingKind.Storage, home + new Vector3(-2.5f, 0, 0)) }.Where(b => b != null).ToArray();
        var walls = new List<BuildingBase>(); var guns = new List<BuildingBase>();
        if (defended)
        {
            if (ring) for (var i = -Half + 1; i < Half; i += 2)
            {
                walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(i, 0, Half))); walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(i, 0, -Half)));
                walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(Half, 0, i), 90)); walls.Add(Put(BuildingKind.SoilWall, home + new Vector3(-Half, 0, i), 90));
            }
            walls.RemoveAll(w => w == null);
            for (var i = 0; i < towers; i++) { var a = i * Mathf.PI * 2 / towers + .7f; guns.Add(Put(BuildingKind.AcidTower, home + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (Half - 2.5f))); }
            guns.RemoveAll(g => g == null);
        }
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead && c.IsColonyMember).ToArray();
        var restore = typeof(CommanderAnt).GetMethod("RestoreTroops", BindingFlags.Instance | BindingFlags.NonPublic);
        for (var i = 0; i < team.Length; i++)
        {
            var c = team[i]; c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop();
            c.Agent.Warp(home + new Vector3(i - 1, 0, 2)); if (troops > 0) restore.Invoke(c, new object[] { troops, 0f });
        }
        await Task.Delay(500);
        var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();
        var s = (WaveDefense.State)typeof(WaveDefense).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(w);
        s.wave = wave - 1; w.StartNow(); if (count > 0) s.total = s.pending = count;
        var total = s.total;
        var hp0 = team.Sum(c => c.PersonalHealth); var tr0 = team.Sum(c => c.TroopCount);
        float BHp(IEnumerable<BuildingBase> bs) => bs.Sum(b => b == null || b.IsDead ? 0 : b.CurrentHealth);
        float BMax(IEnumerable<BuildingBase> bs) => bs.Sum(b => b == null ? 0 : b.MaxHealth);
        var t = Time.time;
        while (w.InWave && !Lost && Time.time - t < Limit && !team.All(c => Down(c)))
        {
            foreach (var c in team)
            {
                if (Down(c)) continue;
                // 방어시설 판: 고리 밖으로 나가 싸우지 않는다(벽 안쪽 3m 이내로 붙은 적만 공격).
                var enemy = local.Visitors.Where(v => v != null && !v.IsDead && (!defended || Mathf.Max(Mathf.Abs(v.Position.x - home.x), Mathf.Abs(v.Position.z - home.z)) <= Half + 3))
                    .OrderBy(v => (v.Position - c.Position).sqrMagnitude).FirstOrDefault();
                if (enemy != null) c.CommandAttack(enemy);
            }
            await Task.Delay(500);
        }
        var lost = Lost; var cleared = !w.InWave && !lost; var secs = Time.time - t;
        Note($"웨이브 {wave} 적 {total} 병력 {troops}/장수 · {(defended ? $"방어(흙벽 {walls.Count}·탑 {guns.Count})" : "방어 없음")}: "
            + $"{(cleared ? $"막음 {secs:0}초" : $"실패{(lost ? "·정착지 붕괴" : "")}(남은 적 {w.RemainingEnemies}, {secs:0}초)")} · 장수 체력 {hp0:0}→{team.Sum(c => c.PersonalHealth):0} 쓰러짐 {team.Count(c => Down(c))}/{team.Length}"
            + $" · 병력 {tr0}→{team.Sum(c => c.TroopCount)} · 집 {BHp(homes):0}/{BMax(homes):0} 파괴 {homes.Count(b => b == null || b.IsDead)}"
            + (defended ? $" · 벽 {BHp(walls):0}/{BMax(walls):0} 파괴 {walls.Count(b => b == null || b.IsDead)} · 탑 파괴 {guns.Count(b => b == null || b.IsDead)}" : ""));
        if (w.InWave) Note("  남은 적 위치: " + string.Join(", ", local.Visitors.Where(v => v != null && !v.IsDead).Select(v => $"{Vector3.Distance(v.Position, home):0}m")));
        foreach (var v in local.Visitors.Where(v => v != null && !v.IsDead).Take(6))
        {
            var a = v.GetComponent<NavMeshAgent>();
            var target = typeof(WildMonster).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(v) as IDamageable;
            var barrier = typeof(WildMonster).GetMethod("FindBarrier", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(v, null);
            Note($"DIAG pos={v.Position} target={target} targetPos={target?.Position} barrier={barrier} path={a.pathStatus} pending={a.pathPending} stopped={a.isStopped} remaining={a.remainingDistance} vel={a.velocity} dest={a.destination} corners={string.Join(";",a.path.corners.Select(p=>p.ToString()))}");
        }
        if (w.InWave) foreach (var v in local.Visitors.ToArray()) if (v != null && !v.IsDead) v.Wound(1e9f);
        Time.timeScale = 0; await Task.Delay(200);
    }
}
