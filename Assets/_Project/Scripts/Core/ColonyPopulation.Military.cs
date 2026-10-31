using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 기존 MaxSoldiers/TryDraft는 이 계산으로 위임한다.
        // Adults가 이미 agingDue를 제외하므로 노년 소속은 한 번만 더한다.
        public static int MilitaryAgeCount(int adults, State state) => adults + (state.elderlyService ? state.old : 0);
        private int MilitaryAge => MilitaryAgeCount(Adults, S);
        public int MobilizationLimit(bool homeThreat) => Unrest ? 0
            : Mathf.Max(Mathf.FloorToInt(MilitaryAge * PolicyRate(S.policy, homeThreat)), MilitaryAge >= 10 ? 5 : 0);

        public int AvailableMilitary(bool homeThreat)
        {
            var pool = AntPool.Instance;
            if (pool == null) return 0;
            var available = pool.Free + (S.elderlyService ? Mathf.Max(0, S.old - S.agingDue) : 0);
            return Mathf.Max(0, Mathf.Min(available, MobilizationLimit(homeThreat) - pool.Assigned));
        }

        public bool TryMilitaryDraft(int count, bool homeThreat)
        {
            if (count <= 0 || count > AvailableMilitary(homeThreat)) return false;
            var pool = AntPool.Instance;
            if (pool.Free < count && S.elderlyService)
            {
                var elders = Mathf.Min(Mathf.Max(0, S.old - S.agingDue), count - pool.Free);
                // 노년을 성체로 되돌리지 않고 기존 복귀/손실 정산을 재사용한다.
                S.agingDue += elders; pool.Breed(elders);
            }
            return pool.Free >= count;
        }
    }
}
