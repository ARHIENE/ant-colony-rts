using System;
using System.Threading.Tasks;

public static class Review1101S3Checks
{
    private static int checks;
    private static void Check(bool ok, string name) { if (!ok) throw new Exception("S3 실패: " + name); checks++; }
    public static async Task<string> Main()
    {
        checks = 0;
        var s = new AntColony.Core.CampaignHistory.State();
        var key = AntColony.Core.CampaignHistory.StoryKind("one");
        Check(AntColony.Core.CampaignHistory.StoryLines(s, "one", "같은 이름").Contains("없습니다"), "구저장 빈 이야기 기본값");
        s.milestones.Add(new AntColony.Core.CampaignHistory.Entry { kind = "관계 변화", name = "옛 장수", result = "옛 친구" });
        Check(AntColony.Core.CampaignHistory.StoryLines(s, "old", "옛 장수").Contains("옛 친구"), "구기록 이름 기반 보조 표시");
        for (int i = 0; i < 8; i++)
            s.milestones.Add(new AntColony.Core.CampaignHistory.Entry { seconds = i, kind = key, name = "이전 이름", result = "사건" + i });
        s.milestones.Add(new AntColony.Core.CampaignHistory.Entry { kind = AntColony.Core.CampaignHistory.StoryKind("two"), name = "같은 이름", result = "다른 사람" });
        var lines = AntColony.Core.CampaignHistory.StoryLines(s, "one", "같은 이름");
        Check(lines.Split('\n').Length == 5 && lines.StartsWith("· 사건7") && lines.Contains("사건3") && !lines.Contains("사건2"), "최신 5개 역순");
        Check(!lines.Contains("다른 사람"), "동명이인 분리·개명 후 ID 유지");
        for (int i = 0; i < 2100; i++)
            s.milestones.Add(new AntColony.Core.CampaignHistory.Entry { seconds = 10 + i, kind = "작업", name = "일", result = "완료" });
        AntColony.Core.CampaignHistory.Compact(s);
        Check(AntColony.Core.CampaignHistory.StoryLines(s, "one", "개명") == lines, "전체 기록 압축 후 개인 최근5 보존");
        Check(s.summaries.Exists(a => a.kind == key && a.count == 3 && a.first == 0 && a.last == 2), "압축 집계 시각 보존");
        Check(s.milestones.Count < 2000, "압축 상한 회복");
        int count = s.milestones.Count; AntColony.Core.CampaignHistory.Compact(s);
        Check(s.milestones.Count == count, "재압축 중복 제거 없음");
        Check(AntColony.Core.CampaignHistory.Validate(s), "기존 저장 Preflight 기록 검증 통과");
        var personal = new AntColony.Units.CommanderPersonalState(); int changed = 0;
        Action<AntColony.Units.CommanderPersonalState, string, float> onMood = (p, reason, value) => { if (p == personal) changed++; };
        AntColony.Units.CommanderPersonalState.MoodChanged += onMood;
        try
        {
            personal.AddMood("S3 검사", 3, 10); personal.AddMood("S3 검사", 3, 10);
            Check(changed == 1, "같은 기분 갱신 반복 발화 억제");
            personal.AddMood("S3 검사", 5, 10);
            Check(changed == 2, "실제 기분 값 변화 알림");
        }
        finally { AntColony.Units.CommanderPersonalState.MoodChanged -= onMood; }
        var friend = new AntColony.Units.CommanderRelation { value = AntColony.Units.SocialRules.Friend };
        Check(AntColony.Units.CommanderAnt.InjuryConcern(friend, false) == -15, "친구 중상 반응");
        friend.value--; Check(AntColony.Units.CommanderAnt.InjuryConcern(friend, false) == -8, "친구 경계 아래 일반 반응");
        friend.spouse = true; Check(AntColony.Units.CommanderAnt.InjuryConcern(friend, false) == -15, "관계 낮은 배우자도 깊은 반응");
        Check(AntColony.Units.CommanderAnt.InjuryConcern(null, true) == -16, "겁쟁이 기존 반응 유지");
        Check(AntColony.UI.SpeechBubbles.Limit == 3 && AntColony.UI.SpeechBubbles.Duration >= 2 && AntColony.UI.SpeechBubbles.Duration <= 3, "말풍선 동시 수·수명");
        Check(AntColony.UI.DetailTabs.Tabs[0] == "개요" && AntColony.UI.DetailTabs.Tabs[6] == "이야기", "기존 개요 보존·이야기 탭");
        await Task.CompletedTask;
        return $"S3 오프라인 {checks}검사 통과 · Unity 화면 미실행";
    }
}
