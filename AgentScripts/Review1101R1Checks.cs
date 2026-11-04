using System;
using System.Threading.Tasks;
using AntColony.Boss;

public static class Review1101R1Checks
{
    public static async Task<string> Main()
    {
        foreach (var id in new[] { BossId.B, BossId.C, BossId.D })
        {
            var expected = $"열 피해{BossEncounterRules.Heat(id):0}";
            if (!BossProgress.Hazard(id).Contains(expected))
                throw new Exception($"R1: 보스 {id} 안내에서 실제 {expected} 누락");
        }
        await Task.CompletedTask;
        return "R1 3검사 통과 (보스별 실제 열 피해와 위험 안내 일치)";
    }
}