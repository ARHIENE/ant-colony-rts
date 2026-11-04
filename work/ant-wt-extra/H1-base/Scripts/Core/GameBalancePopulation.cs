using UnityEngine;

namespace AntColony.Core
{
    public static class GameBalancePopulation
    {
        public const float ImmigrationShare = .1f;
        public const float ImmigrationSeconds = GameCalendar.SecondsPerDay * 3f;

        public static float Demand(float service, float safety, float sentiment, float administration)
            => Mathf.Clamp(1f + service + safety + sentiment + administration, 0f, 2f);

        public static int[] RollArrivalAges(int count)
        {
            var childShare = Random.Range(.05f, .15f);
            var oldShare = Random.Range(Mathf.Max(.05f, .15f - childShare), Mathf.Min(.15f, .25f - childShare));
            return ArrivalAges(count, childShare, oldShare);
        }

        // 최대 나머지 방식. 동률이면 어린·성인·노년 순으로 배분한다.
        public static int[] ArrivalAges(int count, float childShare, float oldShare)
        {
            var shares = new[] { childShare, 1f - childShare - oldShare, oldShare };
            var result = new int[3]; var remainders = new float[3]; var left = count;
            for (var i = 0; i < 3; i++)
            {
                var exact = count * shares[i]; result[i] = Mathf.FloorToInt(exact);
                remainders[i] = exact - result[i]; left -= result[i];
            }
            while (left-- > 0)
            {
                var best = 0;
                for (var i = 1; i < 3; i++) if (remainders[i] > remainders[best]) best = i;
                result[best]++; remainders[best] = -1f;
            }
            return result;
        }
    }
}
