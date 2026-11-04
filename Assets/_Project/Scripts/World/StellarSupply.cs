using System.Collections.Generic;
using System.Linq;
using AntColony.Data;

namespace AntColony.World
{
    public static class StellarSupply
    {
        // 승급 합금80 = 금속80 = 광석160, 표본1. 다른 로켓 필수 비용에는 성휘가 없다.
        public const int RocketOreRequired = 161;
        public static void EnsureHome(List<UndergroundSurvey.Layer> layers)
        {
            int total = layers.Where(l => l.type == (int)ResourceType.StellarOre).Sum(l => l.total);
            // 빈 후보4개·좌표·ID·난수 소비·층당80은 유지하고 유자원 후보만 치환한다.
            // 같은 종류가 2개 이상인 광층부터 바꿔 다른 자원의 유일한 광층 소실을 최소화한다.
            while (total < RocketOreRequired)
            {
                var layer = layers.AsEnumerable().Reverse().Where(l => l.type >= 0 && l.type != (int)ResourceType.StellarOre)
                    .OrderBy(l => layers.Count(o => o.type == l.type) > 1 ? 0 : 1)
                    .ThenBy(l => IndustrialInput((ResourceType)l.type) ? 1 : 0).FirstOrDefault();
                if (layer == null) break;
                layer.type = (int)ResourceType.StellarOre;
                layer.total = UndergroundSurvey.Amount(ResourceType.StellarOre);
                layer.remaining = layer.total;
                total += layer.total;
            }
            // 합금 제련·전자부품 생산의 부트스트랩 원료. 빈 후보와 필수 성휘3층은 보존한다.
            foreach (var type in new[] { ResourceType.NaturalGas, ResourceType.CrudeOil, ResourceType.TitaniumOre })
            {
                while (layers.Count(l => l.type == (int)type) < MinimumLayers(type))
                {
                    var layer = layers.AsEnumerable().Reverse().Where(l => l.type >= 0
                        && (l.type != (int)ResourceType.StellarOre || layers.Count(o => o.type == l.type) > 3)
                        && layers.Count(o => o.type == l.type) > MinimumLayers((ResourceType)l.type))
                        .OrderBy(l => layers.Count(o => o.type == l.type) > 1 ? 0 : 1).FirstOrDefault();
                    if (layer == null) break;
                    layer.type = (int)type; layer.total = UndergroundSurvey.Amount(type); layer.remaining = layer.total;
                }
            }
        }
        private static bool IndustrialInput(ResourceType type) => type is ResourceType.NaturalGas or ResourceType.CrudeOil or ResourceType.TitaniumOre;
        // 승급 티타늄120 + 합금 채굴기10 = 광석260, 표본1. 160층 두 곳으로 확보한다.
        private static int MinimumLayers(ResourceType type) => type == ResourceType.TitaniumOre ? 2 : IndustrialInput(type) ? 1 : 0;
        public static string Notice(IEnumerable<UndergroundSurvey.Layer> layers) =>
            layers.Sum(l => l.type == (int)ResourceType.StellarOre ? l.total : 0) < RocketOreRequired
                ? "성휘 공급 주의: 이 저장의 최초 매장 총량은 로켓 경로 최소 광석161(승급160+표본1)보다 적습니다. 기존 광층은 보충하지 않습니다. 진행 불가 가능성이 있으므로 새 게임을 권장합니다."
                : null;
    }
}
