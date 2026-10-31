using System;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 연간 결정론적 15%·정수 미만 이월. 동원/예약 노년은 복귀 정산 뒤부터 대상이다.
        public static int NaturalDeaths(State state)
        {
            var eligible = Math.Max(0, state.old - state.agingDue);
            if (eligible == 0) return 0;
            var expected = eligible * (double)GameBalance.OldDeath + state.oldDeathRemainder;
            var deaths = Math.Min(eligible, (int)Math.Floor(expected));
            state.oldDeathRemainder = expected - deaths;
            state.old -= deaths;
            return deaths;
        }

        public static bool ValidNaturalDeaths(State state) => state != null
            && state.oldDeathRemainder >= 0 && state.oldDeathRemainder < 1;
    }
}
