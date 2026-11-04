using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        // 기존 MaxSoldiers/TryDraft는 이 계산으로 위임한다.
        // 병역 나이 확대(늙은 시민 포함) 중에는 정산 대기분을 뺀 노년도 병역 인구로 센다.
        private int MilitaryAge => Adults + (S.elderlyService ? Mathf.Max(0, S.old - S.agingDue) : 0);
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
                S.old -= elders; pool.Breed(elders);
            }
            return pool.Free >= count;
        }
    }
}
