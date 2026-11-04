using System.Linq;

namespace AntColony.Core
{
    public static class EraPromotionResearch
    {
        public static readonly ScienceTechnology[] Promotions = {
            ScienceTechnology.SettlementPromotion, ScienceTechnology.SteamPromotion,
            ScienceTechnology.PetroleumPromotion, ScienceTechnology.ElectricPromotion,
            ScienceTechnology.AtomicPromotion, ScienceTechnology.FuturePromotion };

        // 현재 등급 연구소에서 완료할 수 있어 다음 등급 승급과 순환하지 않는다.
        public static ScienceDefinition[] Definitions() => Promotions.Select((technology, index) =>
            new ScienceDefinition(technology, CampaignResearch.EraNames[index] + " 승급 연구", index + 1,
                index == 0 ? new ScienceTechnology[0] : new[] { Promotions[index - 1] })).ToArray();
    }
}
