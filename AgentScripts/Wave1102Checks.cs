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

// 2026-11-02 웨이브 수성 골격: 평화 → 웨이브(전멸 종료) → 평화 4일, 앞당기기, 평화 중 습격 없음, 저장 왕복·구저장 이관.
public static class Wave1102Checks
{
    static int count; static string log = "";
    static void Check(bool ok, string label) { if (!ok) throw new Exception("Wave " + count + ": " + label + "\n" + log); count++; log += "PASS " + label + "\n"; }
    static async Task Wait(Func<bool> condition, int seconds = 60) { var end = DateTime.UtcNow.AddSeconds(seconds); while (!condition() && DateTime.UtcNow < end) await Task.Delay(80); }
    static WaveDefense.State S(WaveDefense w) => (WaveDefense.State)typeof(WaveDefense).GetField("s", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(w);
    static void KillAll() { foreach (var v in SceneObjects.Any<LocalIncursions>().Visitors.ToArray()) if (v != null && !v.IsDead) v.Wound(1e9f); }

    public static async Task<string> Main()
    {
        if (!Application.isPlaying) throw new Exception("Run in Play");
        count = 0; log = "";
        // 물량 곡선(2026-11-03 준비 기대치 기준, WaveBalance1103Checks 실측): 1웨이브는 장수만으로 막을 규모, 3웨이브는 방어시설이 필요한 규모.
        Check(WaveDefense.Count(1) <= 20 && WaveDefense.Count(3) >= 60, "1웨이브 장수 규모·3웨이브 시설 필요 규모");
        Check(Enumerable.Range(1, 40).All(i => WaveDefense.Count(i + 1) >= WaveDefense.Count(i)), "웨이브마다 감소 없음");
        Check(WaveDefense.Count(20) >= 100 && WaveDefense.Count(60) == WaveDefense.MaxEnemies, "후반 100~200·상한");

        var settings = UserSettings.Current.Clone(); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "Wave1102-" + Guid.NewGuid().ToString("N"));
        try
        {
            var quiet = settings.Clone(); quiet.autoSaveEnabled = quiet.autoPauseThreat = quiet.autoPauseDowned = false; UserSettings.Apply(quiet, false);
            await Wait(() => !SaveSystem.Busy);
            var team = Enumerable.Range(0, 3).Select(i => StartingCandidate.Roll(i)).ToArray();
            SaveSystem.NewGame(new NewGameOptions { seed = 261102, mapSize = MapSize.Small, startingCount = 3, startingTeam = team });
            await Wait(() => !SaveSystem.Busy && WaveDefense.Instance != null);
            GameMenuController.Instance.Resume(); Time.timeScale = 1;
            var w = WaveDefense.Instance; var local = SceneObjects.Any<LocalIncursions>();
            Check(w != null && local != null, "웨이브 관리자 생성");
            Check(!w.InWave && w.Wave == 0 && w.PeaceLeft > WaveDefense.FirstPeace - 30, "새 게임은 첫 평화 6일");
            Check(!WaveDefense.AcceptsArrivals, "평화 중 이동 공격대 도착 대기");

            // 평화 중에는 기존 독립 방문 주기로 침입자가 나오지 않는다(옛 주기 300초를 5배속으로 넘겨 본다).
            Time.timeScale = 5; await Task.Delay(2000); Time.timeScale = 1;
            Check(!w.InWave && local.Visitors.Count(v => v != null && !v.IsDead) == 0, "평화 중 습격 없음");

            // 평화 만료 → 자동 시작.
            S(w).peaceLeft = .5f;
            await Wait(() => w.InWave, 10);
            Check(w.InWave && w.Wave == 1, "평화 만료 시 웨이브 1 자동 시작");
            Check(WaveDefense.AcceptsArrivals, "웨이브 중 이동 공격대 합류 허용");
            await Wait(() => S(w).pending == 0, 15);
            Check(S(w).pending == 0 && w.AliveEnemies == WaveDefense.Count(1), "웨이브 1 병력 생성 " + w.AliveEnemies);

            // 저장 왕복: 웨이브 중 상태 보존.
            var snap = w.Capture();
            Check(snap.saved && snap.inWave && snap.wave == 1, "저장 캡처");
            KillAll();
            await Wait(() => !w.InWave, 15);
            Check(!w.InWave && Mathf.Abs(w.PeaceLeft - WaveDefense.Peace) < 30, "전멸 시 종료 → 평화 4일");

            w.Restore(snap);
            Check(w.InWave && w.Wave == 1, "저장 복원(웨이브 중)");
            await Wait(() => !w.InWave, 15); // 복원된 웨이브의 적이 없으므로 다시 종료
            Check(!w.InWave, "복원 후 적 없으면 종료");

            // 앞당기기 + 다음 웨이브 증가.
            Check(w.StartNow() && w.InWave && w.Wave == 2, "지금 시작으로 앞당기기");
            await Wait(() => S(w).pending == 0, 15);
            Check(w.AliveEnemies == WaveDefense.Count(2), "웨이브 2 병력 " + w.AliveEnemies);
            Check(!w.StartNow(), "웨이브 중 중복 시작 불가");
            KillAll(); await Wait(() => !w.InWave, 15);
            Check(!w.InWave, "웨이브 2 종료");

            // 큰 웨이브: 무리 나눠 생성(20마리씩).
            S(w).wave = 9; w.StartNow();
            Check(w.Wave == 10 && S(w).total == WaveDefense.Count(10), "웨이브 10 규모 " + S(w).total);
            await Wait(() => S(w).pending == 0, 30);
            Check(S(w).pending == 0 && w.AliveEnemies >= S(w).total - 1, "큰 웨이브 생성 " + w.AliveEnemies + "/" + S(w).total);
            KillAll(); await Wait(() => !w.InWave, 15);

            // 무리 유닛: 웨이브 20(약 350마리) = 개별 30 + 50마리 무리들. 마릿수 체력·앞줄 공격·저장 필드.
            S(w).wave = 19; w.StartNow();
            await Wait(() => S(w).pending == 0, 60);
            var hordes = local.Visitors.Where(v => v != null && !v.IsDead && v.IsHorde).ToArray();
            Check(S(w).pending == 0 && w.AliveEnemies == S(w).total, "무리 포함 마릿수 " + w.AliveEnemies + "/" + S(w).total);
            Check(hordes.Length == Mathf.CeilToInt((S(w).total - WaveDefense.IndividualCap) / (float)WaveDefense.HordeSize), "무리 수 " + hordes.Length);
            var h = hordes[0]; var before = h.HordeCount;
            h.Wound(h.CurrentHealth / before * 10.5f);
            Check(h.HordeCount == before - 10, "피해만큼 마릿수 감소 " + before + "→" + h.HordeCount);
            Check((float)typeof(WildMonster).GetField("hordeBodyHealth", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(h) > 0, "무리 한 마리 체력 설정");
            KillAll(); await Wait(() => !w.InWave, 15);
            Check(!w.InWave, "무리 웨이브 종료");

            // 구저장(웨이브 필드 없음) 이관.
            w.Restore(new WaveDefense.State());
            Check(!w.InWave && w.Wave == 0 && w.PeaceLeft >= 60, "구저장 이관");
            Check(JsonUtility.FromJson<SaveFileV1>("{}").wave == null || !JsonUtility.FromJson<SaveFileV1>("{}").wave.saved, "구저장 JSON 기본값");
            Time.timeScale = 0;
            return "PASS " + count + "\n" + log;
        }
        finally { UserSettings.Apply(settings, false); SaveStorage.RootOverride = root; }
    }
}
