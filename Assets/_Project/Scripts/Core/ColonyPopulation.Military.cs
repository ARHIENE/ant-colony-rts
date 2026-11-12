using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 기존 MaxSoldiers/TryDraft는 이 계산으로 위임한다.
        // Adults가 이미 agingDue를 제외하므로 노년 소속은 한 번만 더한다.
        public static int MilitaryAgeCount(int adults, State state) => adults + (state.elderlyService ? state.old : 0);
        private int MilitaryAge => MilitaryAgeCount(Adults, S);
        public int MobilizationLimit(bool homeThreat) => 0;

        public int AvailableMilitary(bool homeThreat)
        { return 0; }

        public bool TryMilitaryDraft(int count, bool homeThreat)
        { return false; }
    }
}
