using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Save;
using AntColony.UI;
using AntColony.World;
using UnityEngine;

// Play에서 실행. 새 게임·격리 저장 슬롯을 사용한다. Unity 실행은 Claude 담당.
public static class WaveRetreat1103Checks
{
    const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    static int count;
    static string log;
    static void Check(bool ok, string label)
    { if (!ok) throw new Exception("WaveRetreat " + count + ": " + label + "\n" + log); count++; log += "PASS " + label + "\n"; }
    static async Task Wait(Func<bool> condition)
    {
        var end = DateTime.UtcNow.AddSeconds(90);
        while (!condition() && DateTime.UtcNow < end) await Task.Delay(50);
        Check(condition(), "비동기 작업 완료");
    }
    static object Call(object target, Type type, string method, params object[] args)
        => type.GetMethod(method, Hidden).Invoke(target, args);
    static void Field(WildMonster m, string name, object value) => typeof(WildMonster).GetField(name, Hidden).SetValue(m, value);
    static WaveDefense.State State(WaveDefense w) => (WaveDefense.State)typeof(WaveDefense).GetField("s", Hidden).GetValue(w);
    static void Tick(WaveDefense w) => Call(w, typeof(WaveDefense), "Update");
    static void Wave(WaveDefense w, int total, int pending = 0)
    { var s = State(w); s.inWave = true; s.wave = 20; s.total = total; s.pending = pending; s.sinceStart = 6; }
    static WildMonster Spawn(LocalIncursions local, int heads, string name)
    {
        var p = CommanderRoster.Instance.Commanders.First(c => !c.IsDead).Position;
        var m = (WildMonster)Call(local, typeof(LocalIncursions), "RestoreSpawn", false, p);
        m.name = name; m.MakeRaider(); m.gameObject.SetActive(true);
        Call(m, typeof(WildMonster), "ConfigureHorde", heads);
        Call(local, typeof(LocalIncursions), "RestoreVisitor", m);
        return m;
    }
    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; log = "";
        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        var scale = Time.timeScale;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "WaveRetreat1103-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false;
            UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            SaveSystem.NewGame(new NewGameOptions { seed = 261103, mapSize = MapSize.Small, startingCount = 3,
                startingTeam = Enumerable.Range(0, 3).Select(StartingCandidate.Roll).ToArray() });
            await Wait(() => !SaveSystem.Busy && WaveDefense.Instance != null);
            GameMenuController.Instance.Resume(); Time.timeScale = 0;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();

            var small = Spawn(local, 1, "RetreatCheck-small"); Wave(w, 99); Tick(w);
            Check(w.InWave && !small.Retreating, "99마리 웨이브는 잔당 1마리도 전멸 필요");
            small.Wound(1e9f); Tick(w);
            Check(!w.InWave, "소규모 전멸 시 정상 종료");

            var boundary = Spawn(local, 4, "RetreatCheck-boundary"); Wave(w, 100); Tick(w);
            Check(w.InWave && !boundary.Retreating && w.RemainingEnemies == 4, "100마리 기준 임계값 3·무리 마릿수 계산");
            boundary.Wound(boundary.CurrentHealth / 4); Tick(w);
            Check(!w.InWave && boundary.Retreating && w.RemainingEnemies == 0, "3마리에서 퇴각·종료·집계 제외");
            Check(Mathf.Approximately(w.PeaceLeft, WaveDefense.Peace), "퇴각 즉시 평화 4일");
            Field(boundary, "retreatLeft", .01f); Call(boundary, typeof(WildMonster), "TickRetreat", .02f);
            Check(!boundary.gameObject.activeSelf, "시간 제한 제거·사망 처리 없음");

            var arrival = Spawn(local, 1, "RetreatCheck-arrival");
            Call(arrival, typeof(WildMonster), "BeginRetreat"); Field(arrival, "retreatExit", arrival.Position);
            Call(arrival, typeof(WildMonster), "TickRetreat", 0f);
            Check(!arrival.gameObject.activeSelf, "출구 도착 제거");

            var large = Spawn(local, 61, "RetreatCheck-save"); Wave(w, 3000); Tick(w);
            Check(w.InWave && !large.Retreating, "3000마리 기준 임계값 60 초과 유지");
            large.Wound(large.CurrentHealth / 61);
            State(w).pending = 1;
            typeof(WaveDefense).GetField("groupTimer", Hidden).SetValue(w, 10f); Tick(w);
            Check(w.InWave && !large.Retreating, "생성 대기 중에는 퇴각 금지");
            State(w).pending = 0; State(w).sinceStart = 0; Tick(w);
            Check(w.InWave, "기존 EndGrace 유지");
            State(w).sinceStart = 6; Tick(w);
            Check(!w.InWave && large.Retreating && large.HordeCount == 60 && !large.Threatening, "2% 퇴각·위협 해제");

            var target = CommanderRoster.Instance.Commanders.First(c => !c.IsDead);
            var health = target.PersonalHealth;
            Check(CombatTargeting.IsAlive(target), "공격 중단 검사 대상은 실제 생존 장수");
            Field(large, "currentTarget", target); Field(large, "attackTimer", 0f); Field(large, "targetSearchTimer", 999f);
            Call(large, typeof(WildMonster), "Update");
            Check(target.PersonalHealth == health && typeof(WildMonster).GetField("currentTarget", Hidden).GetValue(large) == null,
                "퇴각 중 근접 대상·공격 타이머가 있어도 공격하지 않음");
            var savedWave = w.Wave;
            Check(SaveSystem.TrySave(false, 0, out var error), "퇴각 중 실제 저장: " + error);
            Check(SaveSystem.TryLoad(SaveSlots.PathFor(false, 0), out error), "퇴각 저장 로드 수락: " + error);
            await Wait(() => !SaveSystem.Busy && WaveDefense.Instance != null);
            Time.timeScale = 0; w = WaveDefense.Instance; local = SceneObjects.Any<LocalIncursions>();
            Check(!w.InWave && w.Wave == savedWave && Mathf.Abs(w.PeaceLeft - WaveDefense.Peace) < 1,
                "왕복 후 성공·평화 상태 보존");
            var restored = local.Visitors.FirstOrDefault(v => v != null && v.name == "RetreatCheck-save");
            Check(restored != null && restored.IsDead && !restored.gameObject.activeSelf && w.RemainingEnemies == 0,
                "퇴각 개체는 체력 0·비활성 복원·재공격 없음");
            Check(SaveSystem.TrySave(false, 1, out error), "왕복 후 다시 저장 가능: " + error);
            return "PASS " + count + "\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; Time.timeScale = scale; }
    }
}
