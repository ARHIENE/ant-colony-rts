using UnityEngine;

namespace AntColony.Core
{
    public static class FinalAssaultPlan
    {
        public static int Phase(float elapsed) => Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0, elapsed) / (5 * GameCalendar.SecondsPerDay)), 0, 2);
        public static int Fronts(float elapsed) => Phase(elapsed) == 0 ? 1 : Phase(elapsed) == 1 ? 2 : 3;
        public static string Description(float elapsed) => Phase(elapsed) switch
        {
            0 => "초기 방어 · 한 방향 접근",
            1 => "중기 방어 · 양방향 접근 · 방어 인력 분산",
            _ => "후기 방어 · 다방향 접근 · 탑승 철수 준비"
        };
        // 인덱스/저장 시간으로 재현한다. 병력을 늘리지 않고 한 파동의 기존 인원을 나눈다.
        public static Vector3 Direction(float elapsed, int wave, int member)
        {
            var fronts = Fronts(elapsed); var side = wave % 4;
            var angle = (side * 90 + member % fronts * (360f / fronts)) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
        }
    }
}
