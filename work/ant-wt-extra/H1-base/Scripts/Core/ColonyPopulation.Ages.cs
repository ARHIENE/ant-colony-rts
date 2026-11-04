using UnityEngine;

namespace AntColony.Core
{
    public sealed partial class ColonyPopulation
    {
        private void AddAgeGroup(int first, int end, int count)
        {
            for (var age = first; age < end; age++)
                S.ages[age] += count / (end - first) + (age - first < count % (end - first) ? 1 : 0);
        }

        // 구 저장과 외부 Breed/탈주는 개체 나이가 없으므로 집계 차이만 맞춘다.
        private void EnsureAges()
        {
            if (S.ages == null || S.ages.Length != 65) S.ages = new int[65];
            MatchAges(0, 18, S.young); MatchAges(18, 65, Adults);
        }

        private void MatchAges(int first, int end, int count)
        {
            var total = 0;
            for (var age = first; age < end; age++) total += S.ages[age];
            if (total < count) AddAgeGroup(first, end, count - total);
            for (var age = end - 1; age >= first && total > count; age--)
            {
                var remove = Mathf.Min(S.ages[age], total - count);
                S.ages[age] -= remove; total -= remove;
            }
        }

        public void AgeYear()
        {
            var pool = AntPool.Instance; if (pool == null) return;
            EnsureAges();
            var grown = S.ages[17]; var elders = S.ages[64];
            if (grown > 0) ApplyEducationAtAdulthood();
            for (var age = 64; age > 0; age--) S.ages[age] = S.ages[age - 1];
            S.ages[0] = 0; S.young -= grown;
            // 현재 대기 인원부터 노년 전환. 동원·예약 몫도 나이는 즉시 전환한다.
            var removed = pool.RemoveFree(elders, false);
            S.agingDue += elders - removed; S.old += elders;
            pool.Breed(grown);
        }
    }
}
