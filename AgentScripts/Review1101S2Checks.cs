using System;
using System.Threading.Tasks;

public static class Review1101S2Checks
{
    private static int checks;
    private static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("S2: " + message);
        checks++;
    }

    public static async Task<string> Main()
    {
        checks = 0;
        Check(AntColony.UI.GoalChain.Validate(null), "구저장 목표 없음 허용");
        Check(AntColony.UI.GoalChain.Validate(new AntColony.UI.GoalChain.State()), "새 게임 기본값");
        Check(!AntColony.UI.GoalChain.Validate(new AntColony.UI.GoalChain.State { completed = -1, initialized = true }), "음수 거부");
        Check(!AntColony.UI.GoalChain.Validate(new AntColony.UI.GoalChain.State { completed = 1 << AntColony.UI.GoalChain.StageCount, initialized = true }), "알 수 없는 단계 거부");
        Check(!AntColony.UI.GoalChain.Validate(new AntColony.UI.GoalChain.State { completed = 1 }), "초기화 전 완료 마스크 거부");
        Check(AntColony.UI.GoalChain.FirstIncomplete(0) == 0, "불시착부터 시작");
        Check(AntColony.UI.GoalChain.FirstIncomplete(AntColony.UI.GoalChain.AllCompleted) == AntColony.UI.GoalChain.StageCount, "전체 완료 종점");
        // 뒤 목표를 먼저 달성해도 앞의 미완료 목표 하나를 유지해야 한다.
        for (var stage = 0; stage < AntColony.UI.GoalChain.StageCount; stage++)
        {
            var mask = AntColony.UI.GoalChain.AllCompleted ^ (1 << stage);
            Check(AntColony.UI.GoalChain.FirstIncomplete(mask) == stage, "순서 밖 달성 후 남은 목표 " + stage);
            Check(AntColony.UI.GoalChain.Validate(new AntColony.UI.GoalChain.State { completed = mask, initialized = true }), "저장 가능한 달성 순서 " + stage);
            Check(AntColony.UI.GoalChain.Title(stage).Length > 0 && AntColony.UI.GoalChain.Reward(stage).Length > 0, "목표·열리는 기능 표시 " + stage);
        }
        Check(AntColony.UI.GoalChain.TargetTier(3) == 2, "첫 승급은 증기");
        Check(AntColony.UI.GoalChain.Title(5).Contains("A"), "첫 원정 뒤 A 목표");
        Check(AntColony.UI.GoalChain.TargetTier(6) == 3 && AntColony.UI.GoalChain.TargetTier(11) == 6, "산업·미래 목표 연결");
        Check(AntColony.UI.GoalChain.Title(14).Contains("방어") && AntColony.UI.GoalChain.Title(15).Contains("발사"), "방어와 수동 발사 분리");
        await Task.CompletedTask;
        return $"S2 오프라인 {checks}검사 통과 · Unity 화면·저장 왕복 별도";
    }

    // Claude 전용. 실제 씬에서 완료 반응·중복 방지·복원 후 회귀 방지를 확인한다.
    public static async Task<string> RunScene()
    {
        var research = AntColony.Core.CampaignResearch.Instance;
        if (research == null || AntColony.Save.SaveSystem.Busy) throw new Exception("준비 완료된 Play 장면 필요");
        var oldResearch = research.CaptureState();
        var oldGoal = AntColony.UI.GoalChain.Capture();
        try
        {
            var fixture = research.CaptureState();
            fixture.completed.Clear();
            fixture.completed.Add((int)AntColony.Core.ScienceTechnology.FungalFarming);
            research.RestoreState(fixture);
            AntColony.UI.GoalChain.Restore(new AntColony.UI.GoalChain.State { completed = 3, initialized = true });
            AntColony.UI.GoalChain.Refresh(100);
            Check((AntColony.UI.GoalChain.Capture().completed & 4) != 0, "실제 연구 완료와 목표 연결");
            Check(AntColony.UI.GoalChain.EmphasisUntil == 104, "달성 4초 강조");
            Check(AntColony.UI.GoalChain.CurrentText.Contains("현재 목표"), "다음 목표 즉시 표시");
            var count = AntColony.UI.ToastManager.Count;
            AntColony.UI.GoalChain.Refresh(101);
            Check(AntColony.UI.ToastManager.Count == count, "중복 알림 없음");
            var saved = UnityEngine.JsonUtility.FromJson<AntColony.Save.SaveFileV1>(UnityEngine.JsonUtility.ToJson(
                new AntColony.Save.SaveFileV1 { goalChain = AntColony.UI.GoalChain.Capture() }));
            fixture.completed.Clear(); research.RestoreState(fixture);
            AntColony.UI.GoalChain.Restore(saved.goalChain);
            AntColony.UI.GoalChain.Refresh(200);
            Check((AntColony.UI.GoalChain.Capture().completed & 4) != 0, "복원 후 달성 유지");
            Check(AntColony.UI.GoalChain.EmphasisUntil == 0, "복원 후 알림 재생 없음");
            await Task.CompletedTask;
            return "S2 장면 검사 통과 · 육안·전체 저장 왕복은 별도";
        }
        finally
        {
            research.RestoreState(oldResearch);
            AntColony.UI.GoalChain.Restore(oldGoal);
        }
    }
}

