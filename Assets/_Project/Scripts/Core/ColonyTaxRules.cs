using System;

namespace AntColony.Core
{
    public static class ColonyTaxRules
    {
        public static float BaseSentiment(float rate)
        {
            var over = Math.Max(0f, rate - .3f);
            return 60f - (rate - .2f) * 100f - over * over * 500f;
        }

        public static float FlightRatio(float rate) => Math.Max(0f, Math.Min(1f,
            (rate - GameBalance.HighTaxFlightRate) / (GameBalance.MaxTaxRate - GameBalance.HighTaxFlightRate)));
    }
}
