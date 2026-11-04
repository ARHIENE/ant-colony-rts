using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// 2026-11-01 Claude 2차: 밤에 자던 장수에게 이동 지시 → 걷는 동안 깨어 있고, 도착 뒤 다시 잔다(픽 쓰러짐 방지).
// Play 모드 + 게임 시작 상태에서 실행.
public static class Review1101Claude2Checks
{
    public static async Task<string> Main()
    {
        var sb = new StringBuilder(); var fail = 0;
        void Check(bool ok, string name) { sb.Append(ok ? "PASS " : "FAIL ").Append(name).Append('\n'); if (!ok) fail++; }
        AntColony.UI.GameMenuController.Instance.Resume(); Time.timeScale = 5;
        var c = AntColony.Core.CommanderRoster.Instance.Commanders.First(x => x != null && !x.IsDead);
        var t = Time.realtimeSinceStartup;
        while (!(AntColony.Core.GameCalendar.IsNight && c.IsAsleep) && Time.realtimeSinceStartup - t < 90) await Task.Delay(100);
        Check(c.IsAsleep, "밤에 잠듦");
        if (AntColony.Core.GameCalendar.SecondsUntilPhaseChange < 25) { t = Time.realtimeSinceStartup; while (Time.realtimeSinceStartup - t < 60 && !(AntColony.Core.GameCalendar.IsNight && AntColony.Core.GameCalendar.SecondsUntilPhaseChange > 30 && c.IsAsleep)) await Task.Delay(100); }
        Time.timeScale = 1;
        c.CommandMove(c.Position + new Vector3(5, 0, 2));
        await Task.Delay(400);
        var awakeWhileWalking = true; var walked = false;
        t = Time.realtimeSinceStartup;
        while ((c.Agent.pathPending || c.Agent.hasPath) && Time.realtimeSinceStartup - t < 15) { walked = true; if (c.IsAsleep) awakeWhileWalking = false; await Task.Delay(100); }
        Check(walked, "이동 지시로 걷기 시작");
        Check(awakeWhileWalking, "걷는 동안 깨어 있음");
        t = Time.realtimeSinceStartup;
        while (!c.IsAsleep && Time.realtimeSinceStartup - t < 5) await Task.Delay(100);
        Check(c.IsAsleep || !AntColony.Core.GameCalendar.IsNight, "도착 뒤 다시 잠");
        Time.timeScale = 0;
        return (fail == 0 ? "PASS" : $"FAIL {fail}") + "\n" + sb;
    }
}
