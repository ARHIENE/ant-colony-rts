using System.Linq;

namespace AntColony.World
{
    public sealed partial class DiplomacyManager
    {
        // 결제는 기존 본거지 재고를 사용한다. 현지 구매품은 방문 수송 수단이 가져와야 한다.
        public ExpeditionTransport MarketTransport(Civilization market)
        {
            if (market == null || !Data.markets.Contains(market) || !market.id.StartsWith("market:")
                || !int.TryParse(market.id.Substring(7), out var index) || index < 0 || world == null || index >= world.Sites.Count) return null;
            var visitor = world.Sites[index].Visitor;
            return visitor != null && visitor.State == ExpeditionState.Deployed ? visitor : null;
        }

        internal static string MarketCargoReason(TradeOffer take, int space, bool blueprintAboard)
        {
            if (take.blueprint && blueprintAboard) return "설계도가 이미 수송칸에 있습니다. 귀환해 반입하세요.";
            // 설계도는 기존 수송 규칙의 무게 없는 핵심물이다.
            var load = take.resources.Sum(n => (long)n) + take.equipment.Count;
            return load <= space ? "" : $"현지 구매품 수송칸 부족 · 필요 {load} / 여유 {space}. 수량을 줄이거나 먼저 귀환하세요.";
        }
    }
}
