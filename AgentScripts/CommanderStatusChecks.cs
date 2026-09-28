using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AntColony.Core;
using AntColony.Map;
using AntColony.Save;
using AntColony.UI;
using AntColony.Units;
using AntColony.World;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using AntSel = AntColony.Units.SelectionManager;

public static class CommanderStatusChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    static int checks;
    static void Check(bool ok, string text) { if (!ok) throw new Exception("FAIL " + text); checks++; }
    static async Task Ready()
    {
        var until = DateTime.UtcNow.AddSeconds(90);
        while (SaveSystem.Busy && DateTime.UtcNow < until) await Task.Delay(50);
        Check(!SaveSystem.Busy, "scene ready"); Time.timeScale = 0;
    }
    static Button Card(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(b => b.name == name && b.transform.parent.name == "CommanderCommands");
    public static async Task<string> Main()
    {
        checks = 0; Check(Application.isPlaying, "Play mode"); var root = SaveStorage.RootOverride;
        SaveStorage.RootOverride = Path.Combine(Application.temporaryCachePath, "CmdStatus-" + Guid.NewGuid().ToString("N"));
        GameObject intruder = null;
        try
        {
            await Ready(); SaveSystem.NewGame(new NewGameOptions { seed = 260927, mapSize = MapSize.Small }); await Ready(); await Task.Delay(300);
            foreach (var x in CommanderRoster.Instance.Commanders) { x.SetJobEnabled(CommanderJobs.All, false); x.CommandStop(); }
            var c = CommanderRoster.Instance.Commanders[0];
            AntSel selection = null;
            for (var i = 0; i < 100 && (selection = Object.FindAnyObjectByType<AntSel>()) == null; i++) await Task.Delay(50);
            Check(selection != null, "selection manager");
            selection.ClearSelection();
            typeof(AntSel).GetMethod("AddToSelection", Private).Invoke(selection, new object[] { c.GetComponent<AntColony.Units.SelectableObject>() });
            var f0 = Time.frameCount; for (var i = 0; i < 100 && Time.frameCount < f0 + 5; i++) await Task.Delay(50);
            Check(Object.FindAnyObjectByType<CommandCard>().Commander == c, "card sees selected commander frames=" + (Time.frameCount - f0) + " cards=" + Object.FindObjectsByType<CommandCard>(FindObjectsSortMode.None).Length);
            await Task.Delay(200);

            // 커맨드 카드: 평시와 출전 중 버튼이 분리된다.
            Check(Card("Work Schedule").gameObject.activeSelf && Card("Reward").gameObject.activeSelf && Card("Send To Rest").gameObject.activeSelf
                && Card("Send To Treatment").gameObject.activeSelf && Card("Weapon").gameObject.activeSelf, "civilian buttons visible");
            // HUD v2: Q 스킬은 평시에 잠긴 채 보인다.
            Check(!Card("Attack Move").gameObject.activeSelf && !Card("Stop").gameObject.activeSelf && !Card("Skill").interactable
                && !Card("Return To Post").gameObject.activeSelf, "combat buttons hidden in peace " + string.Join(",", new[]{"Attack Move","Stop","Skill","Return To Post"}.Select(n => n + ":" + Card(n).gameObject.activeSelf)) + " dep=" + c.IsDeployed + " troops=" + c.TroopCount);
            Check(Card("Details").gameObject.activeSelf, "details always visible");

            // 휴식 지시: 피로할 때만, 피로가 풀릴 때까지 자율 작업을 쉰다.
            Check(!c.CanRest && !c.SendToRest(), "rest needs fatigue");
            c.PersonalState.sleep.fatigue = 60; // 2026-09-28: 피로는 게이지(50 이상이면 휴식 가능).
            Check(c.CanRest, "fatigued commander can rest");
            Card("Send To Rest").onClick.Invoke();
            Check(c.WorkState.resting && CommanderOverhead.Activity(c) == "휴식", "rest order and overhead tag");
            c.CommandStop(); c.SetJobEnabled(CommanderJobs.Gathering, true); c.TickDuty(2);
            Check(!c.IsWorking && c.WorkState.resting, "resting commander skips autonomous work");
            c.PersonalState.sleep.fatigue = 0; c.TickDuty(2);
            Check(!c.WorkState.resting, "rest ends when fatigue is gone");
            c.SetJobEnabled(CommanderJobs.All, false); c.CommandStop();
            c.PersonalState.sleep.fatigue = 60; c.SendToRest(); c.CommandMove(c.Position + Vector3.right);
            Check(!c.WorkState.resting, "manual move cancels rest");
            c.PersonalState.sleep.fatigue = 0; c.CommandStop();

            // 머리 위 하는 일 아이콘: 모든 상태 이름에 픽셀 아이콘이 있다.
            foreach (var name in new[] { "채집", "건설", "농사", "낚시", "제작", "연구", "휴식", "치료", "출전", "귀환", "붕괴" })
                Check(ActivityIcons.Get(name) != null && ActivityIcons.Get(name).width == 12, "activity icon " + name);
            // 치료 지시: 부상이 없거나 빈 의무실이 없으면 불가.
            Check(!c.CanSendToTreatment && !c.SendToTreatment(), "treatment needs injury and infirmary");

            // 머리 위 기분 경고: 위험 구간에서만.
            Check(!CommanderOverhead.MoodAlert(c) || c.Mood <= CommanderOverhead.MoodWarning, "mood alert consistent");
            c.PersonalState.AddMood("Test grief", -200, 60);
            Check(c.Mood <= CommanderOverhead.MoodWarning && CommanderOverhead.MoodAlert(c), "low mood shows warning");
            c.PersonalState.moodFactors.RemoveAll(f => f.reason == "Test grief");

            // 출전 중에는 전투 버튼만.
            AntPool.Instance.GetType();
            typeof(CommanderAnt).GetMethod("Mobilize", Private).Invoke(c, new object[] { 0, c.Position });
            await Task.Delay(400);
            Check(c.IsDeployed && CommanderOverhead.Activity(c) == "출전", "deployed overhead tag");
            Check(Card("Attack Move").gameObject.activeSelf && Card("Stop").gameObject.activeSelf && Card("Return To Post").gameObject.activeSelf
                && !Card("Work Schedule").gameObject.activeSelf && !Card("Send To Rest").gameObject.activeSelf && !Card("Build").gameObject.activeSelf, "deployed card layout");
            Check(!c.CanRest && !c.SendToRest(), "deployed commander cannot rest");
            Card("Attack Move").onClick.Invoke();
            Check(Object.FindAnyObjectByType<AttackMoveController>().IsAttackMode, "attack move button starts targeting");
            typeof(AttackMoveController).GetProperty("IsAttackMode").SetValue(Object.FindAnyObjectByType<AttackMoveController>(), false);
            typeof(CommanderAnt).GetMethod("FinishReturn", Private).Invoke(c, null);
            Check(!c.IsDeployed, "back to civilian");

            // 적 발견 경보: 처음 스캔은 기존 적을 기억만, 새 적이 들어오면 위기 토스트 + 경보 훅.
            var alert = Object.FindAnyObjectByType<EnemyAlert>(); var hooked = 0; Action onAlarm = () => hooked++;
            EnemyAlert.Alarm += onAlarm;
            try
            {
                alert.Scan(); var before = EnemyAlert.AlarmCount;
                alert.Scan(); Check(EnemyAlert.AlarmCount == before, "no alarm without new enemy");
                var template = WildMonster.All.FirstOrDefault(m => m != null && !m.Allied && !m.IsDead);
                Check(template != null, "monster template exists");
                var home = HomeMapBuilder.CurrentWorldBounds.center;
                intruder = Object.Instantiate(template.gameObject, new Vector3(home.x, template.Position.y, home.z), Quaternion.identity);
                intruder.name = "Alert intruder"; await Task.Delay(100);
                alert.Scan();
                // 새 적 등장 + (본거지 한가운데라) 곧바로 전투 시작 경보가 대기 시간 안에 같이 울릴 수 있다.
                var raised = EnemyAlert.AlarmCount - before;
                Check(raised >= 1 && hooked == raised && EnemyAlert.CrisisActive, $"new enemy raises alarm and crisis count={raised} hooked={hooked}");
                alert.Scan(); Check(EnemyAlert.AlarmCount == before + raised, "same enemy does not re-alarm");
                Object.Destroy(intruder); intruder = null; await Task.Delay(100);
                alert.Scan(); Check(!EnemyAlert.CrisisActive, "crisis clears when enemy is gone");
            }
            finally { EnemyAlert.Alarm -= onAlarm; }
            selection.ClearSelection();
            return "PASS CommanderStatusChecks " + checks;
        }
        finally { if (intruder != null) Object.Destroy(intruder); SaveStorage.RootOverride = root; Time.timeScale = 0; }
    }
}
