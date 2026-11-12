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
        {  }
    }
}
