using System;
using System.Threading.Tasks;
using AntColony.World;

// 2026-11-01 사건 감독(Storyteller) 판정: 위태/보통/번성 분류와 습격 간격·위기 확률 조절.
public static class Review1101StoryChecks
{
    public static Task<string> Main()
    {
        int n = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); n++; }
        Check(Storyteller.Classify(3, 2, 100, 5) == Storyteller.Mood.Distressed, "부상자 절반 이상 = 위태");
        Check(Storyteller.Classify(3, 0, 10, 2) == Storyteller.Mood.Distressed, "식량 바닥 = 위태");
        Check(Storyteller.Classify(3, 0, 10, 0) == Storyteller.Mood.Normal, "첫날 식량 0은 시작 상태라 위태 아님");
        Check(Storyteller.Classify(3, 0, 90, 4) == Storyteller.Mood.Thriving, "넉넉한 식량·무부상·4일 = 번성");
        Check(Storyteller.Classify(3, 0, 90, 3) == Storyteller.Mood.Normal, "4일 전은 번성 아님");
        Check(Storyteller.Classify(3, 1, 90, 6) == Storyteller.Mood.Normal, "부상자 있으면 번성 아님");
        Check(Storyteller.RaidDelayScale(Storyteller.Mood.Distressed) > 1 && Storyteller.RaidDelayScale(Storyteller.Mood.Thriving) < 1, "습격 간격: 위태 늦춤·번성 당김");
        Check(Storyteller.CrisisChance(.6f, Storyteller.Mood.Distressed) < .6f && Storyteller.CrisisChance(.6f, Storyteller.Mood.Thriving) > .6f, "위기 확률: 위태 낮춤·번성 높임");
        Check(Storyteller.CrisisChance(.95f, Storyteller.Mood.Thriving) <= 1, "확률 상한 1");
        return Task.FromResult($"Story {n}검사 통과");
    }
}
