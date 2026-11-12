using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.AI;

// 2026-11-12 카이팅 전투 검사: 공격 준비(선딜) 중 이동하면 피해 없이 취소, 타격 뒤 이동해도 다음 공격 시점은 앞당겨지지 않음,
// 일반 이동은 옆의 적을 무시, 같은 대상 재지시는 공격을 끊지 않음. 실제 새 게임에서 장수 1명과 무리 1개로 확인한다. 결과 Temp/kite-log.txt.
public static class Kite1112Checks
{
    static string log = ""; static int pass, fail;
    static void Note(string s) { log += s + "\n"; File.WriteAllText("Temp/kite-log.txt", log); }
    static void Check(bool ok, string what) { if (ok) pass++; else fail++; Note((ok ? "PASS " : "FAIL ") + what); }
    static float Prop(CommanderAnt c, string name) => (float)typeof(SoldierAnt).GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(c);
    static async Task<bool> Until(Func<bool> done, float seconds)
    {
        var end = Time.time + seconds;
        while (Time.time < end) { if (done()) return true; await Task.Delay(5); }
        return done();
    }
    static void Near(CommanderAnt c, Vector3 p) { if (NavMesh.SamplePosition(p, out var hit, 6, NavMesh.AllAreas)) c.Agent.Warp(hit.position); }
    static Vector3 Away(CommanderAnt c, WildMonster m, float meters)
    {
        var d = c.Position - m.Position; d.y = 0; if (d.sqrMagnitude < .01f) d = Vector3.right;
        return NavMesh.SamplePosition(c.Position + d.normalized * meters, out var hit, 4, NavMesh.AllAreas) ? hit.position : c.Position;
    }

    public static async Task<string> Main()
    {
        log = ""; pass = fail = 0; var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Kite-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            await Run();
            Time.timeScale = 0;
            return $"DONE pass {pass} fail {fail}\n" + log;
        }
        catch (Exception e) { Note("EXCEPTION " + e); return $"ERROR pass {pass} fail {fail}\n" + log; }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }

    static async Task Run()
    {
        while (SaveSystem.Busy) await Task.Delay(50);
        SaveSystem.NewGame(new NewGameOptions { seed = 261112, mapSize = MapSize.Small, startingCount = 3, startingTeam = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray() });
        while (SaveSystem.Busy || WaveDefense.Instance == null) await Task.Delay(50);
        GameMenuController.Instance.Resume(); Time.timeScale = 1; await Task.Delay(500);
        var home = (Vector3)typeof(ResourceManager).GetProperty("HomePosition", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var team = CommanderRoster.Instance.Commanders.Where(c => c != null && !c.IsDead && c.IsColonyMember).ToArray();
        foreach (var c in team) { c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop(); }
        var local = SceneObjects.Any<LocalIncursions>();
        var before = local.Visitors.ToArray();
        for (var i = 0; i < 20 && !local.TrySpawnHorde(2000); i++) { }
        var enemy = local.Visitors.FirstOrDefault(v => !before.Contains(v));
        Check(enemy != null, "준비: 무리 생성");
        if (enemy == null) return;
        // 다른 장수는 멀리 보내 무리가 검사 장수만 상대하게 한다.
        for (var i = 1; i < team.Length; i++) Near(team[i], home + new Vector3(-40, 0, -40));
        var me = team[0];
        Near(me, enemy.Position + (home - enemy.Position).normalized * 1.2f);
        var interval = Prop(me, "AttackInterval"); var windup = Prop(me, "AttackWindup");
        Note($"공격 간격 {interval:0.00}초 · 준비 {windup:0.00}초");
        Check(windup > 0 && windup < interval, "준비 시간이 공격 간격보다 짧음");

        // 1) 준비 뒤에 타격한다.
        var hp = enemy.CurrentHealth; me.CommandAttack(enemy);
        var sawWindup = await Until(() => me.IsWindingUp, 6); var hpAtWindup = enemy.CurrentHealth; var windupAt = Time.time;
        Check(sawWindup && hpAtWindup >= hp - .01f, $"공격 준비 시작 시점에는 피해 없음 (체력 {hp:0}→{hpAtWindup:0})");
        var hit = await Until(() => enemy.CurrentHealth < hpAtWindup - .01f, 3); var firstHit = Time.time;
        Check(hit && firstHit - windupAt >= windup * .6f, $"준비가 끝난 뒤 타격 ({firstHit - windupAt:0.00}초 뒤)");
        Check(me.AttackCooldownLeft > 0 && !me.IsWindingUp, $"타격 뒤 다음 공격까지 대기 {me.AttackCooldownLeft:0.00}초");

        // 2) 타격 뒤 이동해도 대기는 그대로 흐르고, 다시 붙어도 간격보다 빨리 치지 못한다.
        var cooldown = me.AttackCooldownLeft; me.CommandMove(Away(me, enemy, 3));
        await Task.Delay(200);
        Check(!me.IsAttacking(enemy) && me.AttackCooldownLeft < cooldown && me.AttackCooldownLeft > 0, $"이동 중에도 대기가 흐름 ({cooldown:0.00}→{me.AttackCooldownLeft:0.00}초, 0으로 초기화되지 않음)");
        hp = enemy.CurrentHealth; me.CommandAttack(enemy);
        var again = await Until(() => enemy.CurrentHealth < hp - .01f, 6); var secondHit = Time.time;
        Check(again && secondHit - firstHit >= interval - .08f, $"타격 간 시간 {secondHit - firstHit:0.00}초 ≥ 공격 간격 {interval:0.00}초(앞당겨지지 않음)");

        // 3) 준비 중 이동하면 피해 없이 취소되고 대기도 소모되지 않는다.
        var ready = await Until(() => me.IsWindingUp, 6); hp = enemy.CurrentHealth;
        me.CommandMove(Away(me, enemy, 3));
        await Task.Delay(Mathf.CeilToInt((windup + .3f) * 1000));
        Check(ready && enemy.CurrentHealth >= hp - .01f, $"준비 중 이동 → 피해 없음 (체력 {hp:0}→{enemy.CurrentHealth:0})");
        Check(!me.IsWindingUp && me.AttackCooldownLeft <= 0, "취소된 공격은 대기를 쓰지 않음");

        // 4) 일반 이동은 옆의 적을 무시하고 목적지까지 간다.
        var goal = Away(me, enemy, 8); var from = me.Position; me.CommandMove(goal);
        await Task.Delay(1500);
        Check(!me.IsAttacking(enemy) && Vector3.Distance(me.Position, from) > 2f, $"일반 이동이 추격으로 바뀌지 않음 ({Vector3.Distance(me.Position, from):0.0}m 이동)");

        // 5) 같은 대상을 0.1초마다 다시 지시해도 공격이 끊기지 않는다.
        Near(me, enemy.Position + (home - enemy.Position).normalized * 1.2f);
        hp = enemy.CurrentHealth; var hits = 0; var end = Time.time + interval * 3.5f + 1f;
        while (Time.time < end)
        {
            me.CommandAttack(enemy);
            if (enemy.CurrentHealth < hp - .01f) { hits++; hp = enemy.CurrentHealth; }
            await Task.Delay(100);
        }
        Check(hits >= 2, $"같은 대상 재지시 중에도 공격 {hits}회");
        Check(!me.IsDead, "검사 장수 생존");
        enemy.Wound(1e9f);
        Time.timeScale = 0; await Task.Delay(200);
    }
}
