using AntColony.Core;

namespace AntColony.Boss
{
    // 추천안 자동 선택(사용자 미확인): 목표 시간·장비 역할의 계산 기준은 DECISIONS_1029_J2.md.
    public static class BossEncounterRules
    {
        public const float CycleSeconds = 8, FlightSeconds = 3, WarningSeconds = 1.5f, StrikeRadius = 3;
        public static float Health(BossId id) => id == BossId.A ? 12000 : id == BossId.D ? 24000 : 18000;
        public static bool Flying(float elapsed) => elapsed % CycleSeconds < FlightSeconds;
        public static int AddLimit(BossId id) => id == BossId.A ? 0 : id == BossId.B ? 2 : 4;
        public static float Heat(BossId id) => id == BossId.A ? 0 : id == BossId.C ? 6 : 12;
        public static string BranchAdvice => "B 먼저: 방열 갑옷으로 C·D의 열 피해 완화. C 먼저: 범위 분사기로 B·D의 호위 무리 대응. 둘 다 필수이며 순서는 자유입니다.";
        public static string Guide(BossId id) => $"체력 {Health(id):0} · 8초 중 비행3초/착지5초 · 비행 공격 예고1.5초, 반경3m 밖 회피"
            + (id == BossId.A ? " · 초반 장수1명+병력4~6명 기준, 피격 없이 공격할 때 약5~8분(공격10/인·간격1초)"
                : $" · 호위 최대{AddLimit(id)}마리 · 열 피해{Heat(id):0}(방열로 절반)");
    }
}
