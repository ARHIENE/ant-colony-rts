namespace AntColony.World
{
    public sealed partial class DiplomacyManager
    {
        public static bool PeriodicTradeEnabled(Civilization civilization, float now)
            => civilization != null && civilization.contacted && !civilization.extinct
                && !civilization.periodicTradePaused && civilization.HasTreaty(TreatyKind.Trade, now);

        public bool SetPeriodicTradePaused(Civilization civilization, bool paused)
        {
            if (civilization == null || !Data.civilizations.Contains(civilization)
                || !civilization.contacted || civilization.extinct) return false;
            civilization.periodicTradePaused = paused;
            return true;
        }
    }
}
