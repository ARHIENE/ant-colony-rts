using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 기존 MaxSoldiers/TryDraft는 이 계산으로 위임한다.
        public int MobilizationLimit(bool homeThreat) => Unrest ? 0
            : Mathf.Max(Mathf.FloorToInt(Adults * PolicyRate(S.policy, homeThreat)), Adults >= 10 ? 5 : 0);

        public int AvailableMilitary(bool homeThreat)
        {
            var pool = AntPool.Instance;
            if (pool == null) return 0;
            var available = pool.Free + (S.elderlyService ? S.old : 0);
            return Mathf.Max(0, Mathf.Min(available, MobilizationLimit(homeThreat) - pool.Assigned));
        }

        public bool TryMilitaryDraft(int count, bool homeThreat)
        {
            if (count <= 0 || count > AvailableMilitary(homeThreat)) return false;
            var pool = AntPool.Instance;
            if (pool.Free < count && S.elderlyService)
            {
                var elders = Mathf.Min(S.old, count - pool.Free);
                S.old -= elders; pool.Breed(elders);
            }
            return pool.Free >= count;
        }
    }
}
